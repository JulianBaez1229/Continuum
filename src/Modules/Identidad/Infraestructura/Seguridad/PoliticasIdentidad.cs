namespace Continuum.Identidad.Infraestructura.Seguridad;

/// <summary>
/// Nombres de políticas de autorización del módulo. El claim <c>permiso</c> lo emitirá el módulo 03 (roles y
/// permisos); mientras no exista, estas políticas niegan el acceso a todos (fallan cerrado).
/// </summary>
public static class PoliticasIdentidad
{
    public const string ClavePermiso = "permiso";
    public const string InvitarPacientes = "identidad.invitar-pacientes";
    public const string PermisoInvitarPacientes = "pacientes.invitar";
}
