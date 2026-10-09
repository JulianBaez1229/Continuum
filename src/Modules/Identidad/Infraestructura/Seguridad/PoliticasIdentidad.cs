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

    /// <summary>
    /// Para acciones críticas (firmar una nota, acceso de emergencia, exportar datos clínicos, cambiar roles; RF-IAM-010).
    /// Los módulos 12, 13 y 03 la aplican con <c>[Authorize(Policy = PoliticasIdentidad.ReautenticacionReciente)]</c>.
    /// </summary>
    public const string ReautenticacionReciente = "identidad.reautenticacion-reciente";
}
