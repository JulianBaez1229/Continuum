using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Infraestructura.Memoria;

public sealed class RepositorioRolesAsignadosMemoria(AlmacenMemoria almacen) : IRepositorioRolesAsignados
{
    public void Sembrar(RolAsignado asignacion) => almacen.Roles.Add(asignacion);

    public Task<IReadOnlyList<RolAsignado>> ListarDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RolAsignado>>(almacen.Roles.Where(r => r.UsuarioId == usuarioId).ToList());

    public Task<IReadOnlyList<Guid>> ListarUsuariosActivosConRolAsync(Guid organizacionId, Rol rol, CancellationToken ct = default)
    {
        var activos = almacen.Usuarios
            .Where(u => u.OrganizacionId == organizacionId && u.Estado == EstadoUsuario.Activo)
            .Select(u => u.Id)
            .ToHashSet();
        return Task.FromResult<IReadOnlyList<Guid>>(almacen.Roles
            .Where(r => r.Rol == rol && activos.Contains(r.UsuarioId))
            .Select(r => r.UsuarioId)
            .Distinct()
            .ToList());
    }

    public Task AgregarAsync(RolAsignado asignacion, CancellationToken ct = default)
    {
        if (almacen.Roles.Contains(asignacion))
            throw new ViolacionDeUnicidadException("El usuario ya tiene ese rol en esa sede.");
        almacen.Roles.Add(asignacion);
        return Task.CompletedTask;
    }

    public Task QuitarAsync(RolAsignado asignacion, CancellationToken ct = default)
    {
        almacen.Roles.Remove(asignacion);
        return Task.CompletedTask;
    }
}
