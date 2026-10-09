using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Infraestructura.Memoria;

public sealed class RepositorioRolesAsignadosMemoria(AlmacenMemoria almacen) : IRepositorioRolesAsignados
{
    public void Sembrar(RolAsignado asignacion) => almacen.Roles.Add(asignacion);

    public Task<IReadOnlyList<RolAsignado>> ListarDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RolAsignado>>(almacen.Roles.Where(r => r.UsuarioId == usuarioId).ToList());

    public Task AgregarAsync(RolAsignado asignacion, CancellationToken ct = default)
    {
        almacen.Roles.Add(asignacion);
        return Task.CompletedTask;
    }

    public Task QuitarAsync(RolAsignado asignacion, CancellationToken ct = default)
    {
        almacen.Roles.Remove(asignacion);
        return Task.CompletedTask;
    }
}
