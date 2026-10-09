using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

// Puertos de la política de autorización (spec §8). Aquí solo viven los que implementa Identidad o que el
// tramo A necesita; el resto (IVinculoPaciente, IRelacionClinica, IAuditoriaAutorizacion) llega con el Autorizador.

/// <summary>Quién actúa: organización, roles en la sede activa y ficha de profesional. Lo implementa Identidad.</summary>
public interface IRolesPorSede
{
    /// <summary>
    /// Perfil del usuario en la sede. <c>null</c> si el usuario no existe o está inactivo; con <c>Roles</c> vacío si
    /// no tiene ningún rol en esa sede (la política lo deniega como <c>SIN_ROL_EN_SEDE</c>).
    /// </summary>
    Task<PerfilActor?> ObtenerPerfilAsync(Guid usuarioId, Guid sedeId, CancellationToken ct = default);
}

/// <summary>Habilitación vigente del profesional en una especialidad (RN-012). Lo implementa Identidad.</summary>
public interface IHabilitaciones
{
    /// <summary>
    /// La habilitación de la especialidad, <b>aunque esté vencida</b>: el vencimiento lo decide la política (R8) con la
    /// fecha local de la sede. <c>null</c> si nunca se registró o fue revocada.
    /// </summary>
    Task<HabilitacionProfesional?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default);
}

/// <summary>Zona horaria de la sede (módulo 06; por defecto <c>America/Santo_Domingo</c>). Lo implementa Organización.</summary>
public interface IZonaHorariaSede
{
    Task<TimeZoneInfo> ObtenerAsync(Guid sedeId, CancellationToken ct = default);
}
