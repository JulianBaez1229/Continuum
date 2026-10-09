namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Quién actúa: su organización, sus roles en la sede activa y, si es profesional, su identificador de profesional.</summary>
public sealed record PerfilActor(Guid UsuarioId, Guid OrganizacionId, IReadOnlySet<Rol> Roles, Guid? ProfesionalId);

/// <summary>Vínculo del profesional con el paciente: ninguno, equipo de atención o tratante.</summary>
public enum TipoRelacion { Ninguna, Equipo, Tratante }

/// <summary>Relación clínica entre el actor y el paciente, con las marcas de sensibilidad del episodio.</summary>
public sealed record RelacionClinica(TipoRelacion Tipo, bool PermiteSensible, bool EpisodioSensible, Guid EspecialidadId);

/// <summary>Especialidad y procedimientos para los que el profesional está habilitado, con su fecha de vencimiento.</summary>
public sealed record HabilitacionProfesional(Guid EspecialidadId, IReadOnlySet<Guid> ProcedimientosPermitidos, DateOnly VigenteHasta);

/// <summary>Datos del recurso, leídos por el módulo consumidor de la fila que ya cargó.</summary>
/// <param name="OrganizacionId">Organización dueña del recurso.</param>
/// <param name="ProfesionalId">Citas, agenda y reportes propios.</param>
/// <param name="EpisodioId">Obligatorio en notas clínicas, órdenes y signos vitales de triaje.</param>
/// <param name="SedeId">Solo para recursos atados a una sede.</param>
/// <param name="ProcedimientoId">Opcional; se usa para la habilitación profesional.</param>
/// <param name="LiberadoAlPaciente">Órdenes liberadas al paciente.</param>
/// <param name="AlcanceReporte">Obligatorio si el recurso es un reporte.</param>
public sealed record ContextoRecurso(
    Guid OrganizacionId,
    Guid? RecursoId = null,
    Guid? PacienteId = null,
    Guid? ProfesionalId = null,
    Guid? EpisodioId = null,
    Guid? SedeId = null,
    Guid? ProcedimientoId = null,
    bool? LiberadoAlPaciente = null,
    AlcanceReporte? AlcanceReporte = null);

/// <summary>Hechos del actor que la política necesita para decidir, ya resueltos por la capa de aplicación.</summary>
public sealed record HechosAcceso(
    PerfilActor Actor,
    Guid SedeActivaId,
    Guid? PacienteIdDelActor,
    RelacionClinica? Relacion,
    HabilitacionProfesional? Habilitacion,
    DateOnly HoyEnSede);
