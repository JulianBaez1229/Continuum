namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Condiciones que una celda de la matriz exige además del rol. La política las evalúa con los hechos del contexto.</summary>
[Flags]
public enum Condiciones
{
    Ninguna = 0,

    /// <summary>El recurso es del propio actor (paciente vinculado o profesional dueño).</summary>
    Propio = 1,

    /// <summary>El recurso pertenece a la sede activa del actor (RN-002).</summary>
    MismaSede = 2,

    /// <summary>La orden fue liberada al paciente.</summary>
    Liberado = 4,

    /// <summary>El actor tiene relación clínica vigente (tratante o equipo) con el episodio o paciente (RN-001).</summary>
    RelacionClinica = 8
}

/// <summary>Una celda de la matriz: lo que un rol puede hacer sobre un recurso, con sus condiciones y el nivel de acceso.</summary>
/// <param name="Alcance">Solo para <see cref="TipoRecurso.Reporte"/>; es <c>null</c> en los demás recursos.</param>
public sealed record Permiso(
    Rol Rol,
    TipoRecurso Recurso,
    Accion Accion,
    AlcanceReporte? Alcance,
    Condiciones Condiciones,
    NivelAcceso Nivel);

/// <summary>
/// Matriz de permisos del módulo 03 §4 más las ampliaciones de la spec de autorización §7.
/// Todo lo que no figura aquí está denegado por defecto.
/// </summary>
public static class MatrizPermisos
{
    private static readonly Dictionary<(Rol, TipoRecurso, Accion, AlcanceReporte?), Permiso> Indice;

    /// <summary>Todas las celdas permitidas de la matriz.</summary>
    public static IReadOnlyList<Permiso> Todos { get; }

    static MatrizPermisos()
    {
        Todos = Construir();
        // ToDictionary falla al iniciar el tipo si alguna celda se declara dos veces.
        Indice = Todos.ToDictionary(p => (p.Rol, p.Recurso, p.Accion, p.Alcance));
    }

    /// <summary>
    /// Busca la celda de un rol sobre un recurso y una acción. El alcance se compara exactamente:
    /// solo los reportes lo llevan. Devuelve <c>null</c> si no hay celda (denegado por defecto).
    /// </summary>
    public static Permiso? Buscar(Rol rol, TipoRecurso recurso, Accion accion, AlcanceReporte? alcance = null) =>
        Indice.GetValueOrDefault((rol, recurso, accion, alcance));

    private static Permiso[] Construir()
    {
        const Condiciones P = Condiciones.Propio;
        const Condiciones S = Condiciones.MismaSede;
        const Condiciones T = Condiciones.RelacionClinica;
        const Condiciones Lib = Condiciones.Liberado;
        const Condiciones Ninguna = Condiciones.Ninguna;
        const NivelAcceso Agregado = NivelAcceso.Agregado;

        var celdas = new List<Permiso>();

        // Acciones: C crear, L leer, A actualizar, X anular.
        void Celda(Rol rol, TipoRecurso recurso, string acciones, Condiciones condiciones, NivelAcceso nivel = NivelAcceso.Completo)
        {
            foreach (var letra in acciones)
                celdas.Add(new Permiso(rol, recurso, ComoAccion(letra), null, condiciones, nivel));
        }

        // Los reportes son siempre de lectura y el alcance es obligatorio.
        void Reporte(Rol rol, AlcanceReporte alcance, Condiciones condiciones, NivelAcceso nivel = NivelAcceso.Completo) =>
            celdas.Add(new Permiso(rol, TipoRecurso.Reporte, Accion.Leer, alcance, condiciones, nivel));

        // Paciente (8)
        Celda(Rol.Paciente, TipoRecurso.DatosDemograficos, "LA", P);
        Celda(Rol.Paciente, TipoRecurso.Cita, "L", P);
        Celda(Rol.Paciente, TipoRecurso.Orden, "L", P | Lib);
        Celda(Rol.Paciente, TipoRecurso.RespuestaFormulario, "CL", P);
        Celda(Rol.Paciente, TipoRecurso.Mensaje, "CL", P);

        // Profesional (19)
        Celda(Rol.Profesional, TipoRecurso.DatosDemograficos, "L", Ninguna);
        Celda(Rol.Profesional, TipoRecurso.Cita, "LA", P | S);
        Celda(Rol.Profesional, TipoRecurso.AgendaProfesional, "CLA", P | S);
        Celda(Rol.Profesional, TipoRecurso.NotaClinica, "CL", T);
        Celda(Rol.Profesional, TipoRecurso.ResumenSeguridad, "L", T);
        Celda(Rol.Profesional, TipoRecurso.SignosVitalesTriaje, "L", T);
        Celda(Rol.Profesional, TipoRecurso.Orden, "CLX", T);
        Celda(Rol.Profesional, TipoRecurso.AsignacionFormulario, "C", T);
        Celda(Rol.Profesional, TipoRecurso.RespuestaFormulario, "L", T);
        Celda(Rol.Profesional, TipoRecurso.Mensaje, "CL", T);
        Celda(Rol.Profesional, TipoRecurso.Catalogo, "L", Ninguna);
        Reporte(Rol.Profesional, AlcanceReporte.Propio, P);

        // Asistente clínico (10)
        Celda(Rol.AsistenteClinico, TipoRecurso.DatosDemograficos, "L", Ninguna);
        Celda(Rol.AsistenteClinico, TipoRecurso.Cita, "L", S);
        Celda(Rol.AsistenteClinico, TipoRecurso.AgendaProfesional, "L", S);
        Celda(Rol.AsistenteClinico, TipoRecurso.NotaClinica, "L", T, NivelAcceso.PorSeccion);
        Celda(Rol.AsistenteClinico, TipoRecurso.ResumenSeguridad, "L", T);
        Celda(Rol.AsistenteClinico, TipoRecurso.SignosVitalesTriaje, "CLA", T);
        Celda(Rol.AsistenteClinico, TipoRecurso.Orden, "L", T);
        Celda(Rol.AsistenteClinico, TipoRecurso.Catalogo, "L", Ninguna);

        // Recepción (10)
        Celda(Rol.Recepcion, TipoRecurso.DatosDemograficos, "CLA", Ninguna);
        Celda(Rol.Recepcion, TipoRecurso.Cita, "CLAX", S);
        Celda(Rol.Recepcion, TipoRecurso.AgendaProfesional, "L", S);
        Celda(Rol.Recepcion, TipoRecurso.Catalogo, "L", Ninguna);
        Reporte(Rol.Recepcion, AlcanceReporte.Operativo, Ninguna);

        // Coordinador de sede (14)
        Celda(Rol.CoordinadorSede, TipoRecurso.DatosDemograficos, "CLA", Ninguna);
        Celda(Rol.CoordinadorSede, TipoRecurso.Cita, "CLAX", S);
        Celda(Rol.CoordinadorSede, TipoRecurso.AgendaProfesional, "CLA", S);
        Celda(Rol.CoordinadorSede, TipoRecurso.Catalogo, "L", Ninguna);
        Celda(Rol.CoordinadorSede, TipoRecurso.UsuarioRol, "L", S);
        Reporte(Rol.CoordinadorSede, AlcanceReporte.Operativo, S);
        Reporte(Rol.CoordinadorSede, AlcanceReporte.Sede, S);

        // Director: solo ve agregados de citas, agenda y reportes (8)
        Celda(Rol.Director, TipoRecurso.Cita, "L", Ninguna, Agregado);
        Celda(Rol.Director, TipoRecurso.AgendaProfesional, "L", Ninguna, Agregado);
        Celda(Rol.Director, TipoRecurso.Catalogo, "L", Ninguna);
        Celda(Rol.Director, TipoRecurso.UsuarioRol, "L", Ninguna);
        Reporte(Rol.Director, AlcanceReporte.Operativo, Ninguna, Agregado);
        Reporte(Rol.Director, AlcanceReporte.Sede, Ninguna, Agregado);
        Reporte(Rol.Director, AlcanceReporte.Global, Ninguna, Agregado);
        Reporte(Rol.Director, AlcanceReporte.Cumplimiento, Ninguna, Agregado);

        // Administrador funcional (15)
        Celda(Rol.AdminFuncional, TipoRecurso.DatosDemograficos, "L", Ninguna);
        Celda(Rol.AdminFuncional, TipoRecurso.Cita, "L", Ninguna);
        Celda(Rol.AdminFuncional, TipoRecurso.AgendaProfesional, "L", Ninguna);
        Celda(Rol.AdminFuncional, TipoRecurso.Catalogo, "CLAX", Ninguna);
        Celda(Rol.AdminFuncional, TipoRecurso.UsuarioRol, "CLAX", Ninguna);
        Reporte(Rol.AdminFuncional, AlcanceReporte.Operativo, Ninguna);
        Reporte(Rol.AdminFuncional, AlcanceReporte.Sede, Ninguna);
        Reporte(Rol.AdminFuncional, AlcanceReporte.Global, Ninguna);
        Reporte(Rol.AdminFuncional, AlcanceReporte.Cumplimiento, Ninguna);

        // Auditor (4)
        Celda(Rol.Auditor, TipoRecurso.Catalogo, "L", Ninguna);
        Celda(Rol.Auditor, TipoRecurso.UsuarioRol, "L", Ninguna);
        Celda(Rol.Auditor, TipoRecurso.Bitacora, "L", Ninguna);
        Reporte(Rol.Auditor, AlcanceReporte.Cumplimiento, Ninguna);

        // Red de apoyo: sin celdas en el MVP (módulo 19 es Fase 3).

        return [.. celdas];
    }

    private static Accion ComoAccion(char letra) => letra switch
    {
        'C' => Accion.Crear,
        'L' => Accion.Leer,
        'A' => Accion.Actualizar,
        'X' => Accion.Anular,
        _ => throw new ArgumentOutOfRangeException(nameof(letra), letra, "Letra de acción desconocida en la matriz de permisos.")
    };
}
