using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

// Puertos del Autorizador (spec §8). Cada uno lo implementa el módulo dueño del dato; aquí solo hay interfaces.

/// <summary>Perfil del actor en la sede activa. Lo implementa Identidad.</summary>
public interface IRolesPorSede
{
    /// <summary>Organización, roles en la sede y ProfesionalId del usuario; <c>null</c> si el usuario no existe o está inactivo.</summary>
    Task<PerfilActor?> ObtenerPerfilAsync(Guid usuarioId, Guid sedeId, CancellationToken ct = default);
}

/// <summary>Paciente al que está vinculado un usuario del portal. Lo implementa Pacientes.</summary>
public interface IVinculoPaciente
{
    /// <summary><c>null</c> si el usuario no es paciente.</summary>
    Task<Guid?> ObtenerPacienteIdAsync(Guid usuarioId, CancellationToken ct = default);
}

/// <summary>Relación clínica del profesional con el paciente y su episodio. Lo implementa HistoriaClinica.</summary>
public interface IRelacionClinica
{
    /// <summary>
    /// Sin <paramref name="episodioId"/> devuelve la mejor relación con cualquier episodio abierto del paciente.
    /// Aplica el <c>desde</c>/<c>hasta</c> del miembro del equipo a <paramref name="ahora"/>.
    /// </summary>
    Task<RelacionClinica?> ObtenerAsync(
        Guid profesionalId, Guid pacienteId, Guid? episodioId, DateTimeOffset ahora, CancellationToken ct = default);
}

/// <summary>Habilitación del profesional en una especialidad. Lo implementa Identidad.</summary>
public interface IHabilitaciones
{
    Task<HabilitacionProfesional?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default);
}

/// <summary>Zona horaria de una sede (por defecto <c>America/Santo_Domingo</c>). Lo implementa Organizacion.</summary>
public interface IZonaHorariaSede
{
    Task<TimeZoneInfo> ObtenerAsync(Guid sedeId, CancellationToken ct = default);
}

/// <summary>Puerto hacia el módulo Auditoría para las denegaciones (spec §9). Lo implementa Auditoria.</summary>
public interface IAuditoriaAutorizacion
{
    Task RegistrarDenegacionAsync(EventoAutorizacion evento, CancellationToken ct = default);
}

/// <summary>
/// Denegación a auditar. Lleva solo identificadores y códigos: nunca contenido clínico, nombres ni documentos de identidad.
/// </summary>
public sealed record EventoAutorizacion(
    DateTimeOffset OcurridoEn, Guid UsuarioId, Guid? OrganizacionId, Guid SedeId,
    IReadOnlyCollection<Rol> Roles, Accion Accion, TipoRecurso Recurso,
    Guid? RecursoId, Guid? PacienteId, TipoDenegacion Tipo, MotivoDenegacion Motivo);
