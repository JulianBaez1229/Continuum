using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Dominio;

/// <summary>
/// Fila de <c>rol_asignado</c> (módulo 05): un usuario con un rol en una sede (RF-ROL-002).
/// Un usuario puede tener varios roles, y el mismo rol en varias sedes.
/// </summary>
public sealed record RolAsignado(Guid UsuarioId, Rol Rol, Guid SedeId);
