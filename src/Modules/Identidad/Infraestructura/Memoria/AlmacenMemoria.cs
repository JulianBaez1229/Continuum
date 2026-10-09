using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Infraestructura.Memoria;

/// <summary>
/// Estado compartido de los repositorios en memoria del tramo A (RF-ROL-001 a 004). Existe para desarrollar y probar
/// sin base de datos: las tablas <c>rol_asignado</c>, <c>profesional</c> y <c>habilitacion</c> y su migración son un PR
/// aparte del Dev B. NO se registra en <c>ModuloIdentidad</c> ni sirve en producción: el estado se pierde al reiniciar
/// y no es seguro entre hilos.
/// </summary>
public sealed class AlmacenMemoria
{
    public List<Usuario> Usuarios { get; private set; } = [];
    public HashSet<RolAsignado> Roles { get; private set; } = [];
    public Dictionary<Guid, Profesional> Profesionales { get; private set; } = [];
    public Dictionary<Guid, Habilitacion> Habilitaciones { get; private set; } = [];

    /// <summary>Copia del estado para poder deshacer. Las entidades de roles, profesionales y habilitaciones son inmutables.</summary>
    internal Instantanea Capturar() => new(
        [.. Usuarios], new HashSet<RolAsignado>(Roles), new Dictionary<Guid, Profesional>(Profesionales),
        new Dictionary<Guid, Habilitacion>(Habilitaciones));

    internal void Restaurar(Instantanea copia)
    {
        Usuarios = copia.Usuarios;
        Roles = copia.Roles;
        Profesionales = copia.Profesionales;
        Habilitaciones = copia.Habilitaciones;
    }

    internal sealed record Instantanea(
        List<Usuario> Usuarios, HashSet<RolAsignado> Roles,
        Dictionary<Guid, Profesional> Profesionales, Dictionary<Guid, Habilitacion> Habilitaciones);
}
