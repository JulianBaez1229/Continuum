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
        Sede,           // R3
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

    /// <summary>Evalúa un rol por las etapas R2 en adelante y se detiene en la primera condición que no se cumple.</summary>
    private static ResultadoRol EvaluarRol(Rol rol, Accion accion, TipoRecurso recurso, ContextoRecurso contexto, HechosAcceso hechos)
    {
        // R2: el alcance solo forma parte de la celda en los reportes.
        var alcance = recurso == TipoRecurso.Reporte ? contexto.AlcanceReporte : null;
        var celda = MatrizPermisos.Buscar(rol, recurso, accion, alcance);
        if (celda is null)
            return Denegado(rol, Etapa.Celda, MotivoDenegacion.SinPermisoDeRol);

        var condiciones = celda.Condiciones;

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

        // Provisional (Task 4): R6 aún no evalúa la relación clínica, así que toda celda [T] se deniega.
        if (condiciones.HasFlag(Condiciones.RelacionClinica))
            return Denegado(rol, Etapa.Relacion, MotivoDenegacion.SinRelacionClinica);

        return new ResultadoRol(rol, Decision.Permitir(celda.Nivel), Etapa.Concedido);
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
