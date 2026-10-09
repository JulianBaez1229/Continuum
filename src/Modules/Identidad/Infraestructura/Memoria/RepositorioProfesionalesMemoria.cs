using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Infraestructura.Memoria;

public sealed class RepositorioProfesionalesMemoria(AlmacenMemoria almacen) : IRepositorioProfesionales
{
    public void Sembrar(Profesional profesional) => almacen.Profesionales[profesional.Id] = profesional;

    public Task<Profesional?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(almacen.Profesionales.GetValueOrDefault(id));

    public Task<Profesional?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult(almacen.Profesionales.Values.FirstOrDefault(p => p.UsuarioId == usuarioId));

    public Task AgregarAsync(Profesional profesional, CancellationToken ct = default)
    {
        if (almacen.Profesionales.Values.Any(p => p.UsuarioId == profesional.UsuarioId))
            throw new InvalidOperationException("El usuario ya tiene una ficha de profesional.");
        almacen.Profesionales[profesional.Id] = profesional;
        return Task.CompletedTask;
    }
}
