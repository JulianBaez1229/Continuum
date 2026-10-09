namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>
/// Política de autorización (spec §6): función pura que decide con los hechos ya resueltos, sin I/O.
/// Deniega por defecto: solo concede lo que una celda de <see cref="MatrizPermisos"/> permite y cuyas condiciones se cumplen.
/// </summary>
public static class PoliticaAcceso
{
    /// <summary>Niveles del más amplio al más restringido. Con varios roles se concede el primero que alguno obtenga.</summary>
    private static readonly NivelAcceso[] DelMasAmplioAlMasRestringido =
        [NivelAcceso.Completo, NivelAcceso.PorSeccion, NivelAcceso.Resumen, NivelAcceso.Agregado];

    /// <summary>
    /// Etapa de la evaluación de un rol (spec §6, R2–R8). El orden es la profundidad: si ningún rol concede,
    /// el motivo es el del rol que llegó más lejos.
    /// </summary>
    private enum Etapa
    {
        Celda,          // R2
        Sede,           // R3, y la falta de episodio en recursos de episodio
        Propio,         // R4
        Liberado,       // R5
        Relacion,       // R6
        Sensibilidad,   // R7
        Habilitacion,   // R8
        Concedido       // pasó todas las etapas
    }

    /// <summary>Resultado de evaluar un rol: la decisión y la etapa en la que terminó la evaluación.</summary>
    private readonly record struct ResultadoRol(Rol Rol, Decision Decision, Etapa Etapa);

    /// <summary>Decide si el actor de <paramref name="hechos"/> puede ejecutar la acción sobre el recurso descrito por <paramref name="contexto"/>.</summary>
    public static Decision Evaluar(Accion accion, TipoRecurso recurso, ContextoRecurso contexto, HechosAcceso hechos)
    {
        // R0: sin roles en la sede activa no hay celda que evaluar.
        if (hechos.Actor.Roles.Count == 0)
            return Prohibido(MotivoDenegacion.SinRolEnSede);

        // R1 (RN-015): para el actor, un recurso de otra organización no existe (404, no 403).
        if (contexto.OrganizacionId != hechos.Actor.OrganizacionId)
            return Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion);

        // Sin alcance, ningún rol tiene celda de reporte: se corta aquí para no confundirlo con SinPermisoDeRol.
        if (recurso == TipoRecurso.Reporte && contexto.AlcanceReporte is null)
            return Prohibido(MotivoDenegacion.DatosInsuficientes);

        return Combinar(hechos.Actor.Roles.Select(rol => EvaluarRol(rol, accion, recurso, contexto, hechos)));
    }

    /// <summary>
    /// Recursos que pertenecen a un episodio de atención: exigen <see cref="ContextoRecurso.EpisodioId"/>
    /// y están sujetos a la sensibilidad del episodio (R7, RN-016).
    /// </summary>
    public static bool EsDeEpisodio(TipoRecurso recurso) =>
        recurso is TipoRecurso.NotaClinica or TipoRecurso.Orden or TipoRecurso.SignosVitalesTriaje;

    /// <summary>Evalúa un rol por las etapas R2 en adelante y se detiene en la primera condición que no se cumple.</summary>
    private static ResultadoRol EvaluarRol(Rol rol, Accion accion, TipoRecurso recurso, ContextoRecurso contexto, HechosAcceso hechos)
    {
        // R2: el alcance solo forma parte de la celda en los reportes.
        var alcance = recurso == TipoRecurso.Reporte ? contexto.AlcanceReporte : null;
        var celda = MatrizPermisos.Buscar(rol, recurso, accion, alcance);
        if (celda is null)
            return Denegado(rol, Etapa.Celda, MotivoDenegacion.SinPermisoDeRol);

        // El episodio es obligatorio en los recursos de episodio, sea cual sea el rol (spec §5 y §10).
        // Tiene el mismo rango que R3: la celda existe, pero falta un dato del contexto para seguir.
        if (EsDeEpisodio(recurso) && contexto.EpisodioId is null)
            return Denegado(rol, Etapa.Sede, MotivoDenegacion.DatosInsuficientes);

        var condiciones = celda.Condiciones;
        var nivel = celda.Nivel;

        // R3 (RN-002): el recurso debe pertenecer a la sede activa.
        if (condiciones.HasFlag(Condiciones.MismaSede))
        {
            if (contexto.SedeId is null)
                return Denegado(rol, Etapa.Sede, MotivoDenegacion.DatosInsuficientes);
            if (contexto.SedeId != hechos.SedeActivaId)
                return Denegado(rol, Etapa.Sede, MotivoDenegacion.SedeDistinta);
        }

        // R4: el recurso es del propio actor.
        if (condiciones.HasFlag(Condiciones.Propio) && !EsPropio(rol, contexto, hechos))
            return Denegado(rol, Etapa.Propio, MotivoDenegacion.NoEsPropio);

        // R5: la orden fue liberada al paciente.
        if (condiciones.HasFlag(Condiciones.Liberado) && contexto.LiberadoAlPaciente != true)
            return Denegado(rol, Etapa.Liberado, MotivoDenegacion.NoLiberado);

        if (condiciones.HasFlag(Condiciones.RelacionClinica))
        {
            // R6 (RN-001): el actor debe ser tratante o miembro del equipo del paciente.
            // Sin ficha de profesional no puede tener relación, y no hace falta mirar los hechos.
            if (hechos.Actor.ProfesionalId is null)
                return Denegado(rol, Etapa.Relacion, MotivoDenegacion.SinRelacionClinica);
            if (hechos.Relacion is not { } relacion)
                return Denegado(rol, Etapa.Relacion, MotivoDenegacion.DatosInsuficientes);
            if (relacion.Tipo is not (TipoRelacion.Tratante or TipoRelacion.Equipo))
                return Denegado(rol, Etapa.Relacion, MotivoDenegacion.SinRelacionClinica);

            // R7 (RN-016): en un episodio sensible el nivel puede reducirse o negarse.
            if (EsDeEpisodio(recurso) && relacion.EpisodioSensible)
            {
                if (NivelEnEpisodioSensible(rol, relacion, accion, recurso, nivel) is not { } nivelSensible)
                    return Denegado(rol, Etapa.Sensibilidad, MotivoDenegacion.EpisodioSensible);
                nivel = nivelSensible;
            }

            // R8 (RN-012): la escritura clínica del profesional exige habilitación vigente.
            if (ExigeHabilitacion(rol, accion, recurso)
                && MotivoDeHabilitacion(hechos.Habilitacion, relacion, contexto, hechos.HoyEnSede) is { } motivoHabilitacion)
                return Denegado(rol, Etapa.Habilitacion, motivoHabilitacion);
        }

        return new ResultadoRol(rol, Decision.Permitir(nivel), Etapa.Concedido);
    }

    /// <summary>
    /// R8 (RN-012): solo el rol <see cref="Rol.Profesional"/> necesita habilitación, y solo para crear, actualizar o
    /// anular notas clínicas y órdenes. Firmar una nota es <see cref="Accion.Crear"/> (spec §12, I3). Las lecturas
    /// y los demás roles nunca la necesitan.
    /// </summary>
    private static bool ExigeHabilitacion(Rol rol, Accion accion, TipoRecurso recurso) =>
        rol == Rol.Profesional
        && (accion is Accion.Crear or Accion.Actualizar or Accion.Anular)
        && (recurso is TipoRecurso.NotaClinica or TipoRecurso.Orden);

    /// <summary>
    /// R8 (RN-012): motivo por el que la habilitación no basta para escribir, o <c>null</c> si basta. Orden de
    /// comprobación: sin habilitación, de otra especialidad que la del episodio, o con el procedimiento informado
    /// fuera de su lista (<see cref="MotivoDenegacion.SinHabilitacion"/>); y solo después el vencimiento.
    /// <see cref="HabilitacionProfesional.VigenteHasta"/> es el último día válido, inclusive: la licencia que vence
    /// hoy sigue vigente hoy (CA-ROL-003).
    /// </summary>
    private static MotivoDenegacion? MotivoDeHabilitacion(
        HabilitacionProfesional? habilitacion, RelacionClinica relacion, ContextoRecurso contexto, DateOnly hoyEnSede)
    {
        if (habilitacion is null || habilitacion.EspecialidadId != relacion.EspecialidadId)
            return MotivoDenegacion.SinHabilitacion;
        if (contexto.ProcedimientoId is { } procedimiento && !habilitacion.ProcedimientosPermitidos.Contains(procedimiento))
            return MotivoDenegacion.SinHabilitacion;
        return hoyEnSede > habilitacion.VigenteHasta ? MotivoDenegacion.HabilitacionVencida : null;
    }

    /// <summary>
    /// R7 (RN-016): nivel con que un actor con relación clínica (ya validada en R6) accede a un recurso de un
    /// episodio sensible, o <c>null</c> si no accede. El asistente nunca accede. El tratante y el equipo con
    /// permiso sensible conservan el nivel de la celda. El equipo sin permiso solo lee la nota, en resumen.
    /// </summary>
    private static NivelAcceso? NivelEnEpisodioSensible(Rol rol, RelacionClinica relacion, Accion accion, TipoRecurso recurso, NivelAcceso nivelDeCelda)
    {
        if (rol == Rol.AsistenteClinico)
            return null;
        if (relacion.Tipo == TipoRelacion.Tratante || relacion.PermiteSensible)
            return nivelDeCelda;
        return recurso == TipoRecurso.NotaClinica && accion == Accion.Leer ? NivelAcceso.Resumen : null;
    }

    /// <summary>
    /// Varios roles: gana el nivel más amplio concedido. Si ningún rol concede, el motivo es el del rol que llegó
    /// a la etapa más profunda; en empate, el del rol con menor valor en <see cref="Rol"/>.
    /// </summary>
    private static Decision Combinar(IEnumerable<ResultadoRol> porRol)
    {
        var resultados = porRol.ToList();

        var nivelesConcedidos = resultados.Where(r => r.Decision.EstaPermitido).Select(r => r.Decision.Nivel).ToHashSet();
        foreach (var nivel in DelMasAmplioAlMasRestringido)
        {
            if (nivelesConcedidos.Contains(nivel))
                return Decision.Permitir(nivel);
        }

        return resultados
            .OrderByDescending(r => r.Etapa)
            .ThenBy(r => r.Rol)
            .First()
            .Decision;
    }

    /// <summary>
    /// Paciente: el recurso es de su paciente vinculado. Profesional: el recurso es suyo.
    /// Ambos identificadores deben existir: dos nulos nunca son «propio». Ningún otro rol tiene recursos propios.
    /// </summary>
    private static bool EsPropio(Rol rol, ContextoRecurso contexto, HechosAcceso hechos) => rol switch
    {
        Rol.Paciente => MismoId(contexto.PacienteId, hechos.PacienteIdDelActor),
        Rol.Profesional => MismoId(contexto.ProfesionalId, hechos.Actor.ProfesionalId),
        _ => false
    };

    private static bool MismoId(Guid? delRecurso, Guid? delActor) =>
        delRecurso is { } recurso && delActor is { } actor && recurso == actor;

    private static ResultadoRol Denegado(Rol rol, Etapa etapa, MotivoDenegacion motivo) => new(rol, Prohibido(motivo), etapa);

    private static Decision Prohibido(MotivoDenegacion motivo) => Decision.Denegar(TipoDenegacion.Prohibido, motivo);
}
