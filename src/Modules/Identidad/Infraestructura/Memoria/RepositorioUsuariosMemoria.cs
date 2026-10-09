using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Infraestructura.Memoria;

public sealed class RepositorioUsuariosMemoria(AlmacenMemoria almacen) : IRepositorioUsuarios
{
    /// <summary>Siembra datos de prueba sin pasar por un caso de uso.</summary>
    public void Sembrar(Usuario usuario) => almacen.Usuarios.Add(usuario);

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default)
    {
        var normalizado = Usuario.NormalizarCorreo(correo);
        return Task.FromResult(almacen.Usuarios.FirstOrDefault(u => u.Correo == normalizado));
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(almacen.Usuarios.FirstOrDefault(u => u.Id == id));

    public Task AgregarAsync(Usuario usuario, CancellationToken ct = default)
    {
        if (almacen.Usuarios.Any(u => u.Correo == usuario.Correo))
            throw new ViolacionDeUnicidadException("Ya existe una cuenta con ese correo.");
        almacen.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task GuardarAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;
}
