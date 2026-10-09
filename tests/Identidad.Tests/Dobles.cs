using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

internal sealed class RelojFalso : IReloj
{
    public DateTimeOffset Ahora { get; set; } = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    public void Avanzar(TimeSpan t) => Ahora += t;
}

// Hash de juguete solo para pruebas; el real usa el hasher de ASP.NET Core Identity.
internal sealed class HasheadorFalso : IHasheadorContrasena
{
    public string Hashear(string contrasena) => "hash:" + contrasena;
    public bool Verificar(string hash, string contrasena) => hash == "hash:" + contrasena;
}

internal sealed class RepositorioFalso : IRepositorioUsuarios
{
    private readonly List<Usuario> _usuarios = [];
    public void Agregar(Usuario u) => _usuarios.Add(u);
    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Correo == Usuario.NormalizarCorreo(correo)));
    public Task GuardarAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class AuditoriaFalsa : IAuditoriaIdentidad
{
    public List<EventoIdentidad> Eventos { get; } = [];
    public Task RegistrarAsync(EventoIdentidad e, CancellationToken ct = default)
    {
        Eventos.Add(e);
        return Task.CompletedTask;
    }
}

internal sealed class AvisosFalsos : IAvisosSeguridad
{
    public List<(Guid UsuarioId, DateTimeOffset Hasta)> Bloqueos { get; } = [];
    public Task NotificarBloqueoAsync(Usuario u, DateTimeOffset hasta, CancellationToken ct = default)
    {
        Bloqueos.Add((u.Id, hasta));
        return Task.CompletedTask;
    }
}

internal sealed class FiltradasFalsas(params string[] filtradas) : IContrasenasFiltradas
{
    public bool EstaFiltrada(string contrasena) => filtradas.Contains(contrasena);
}
