using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public interface IReloj { DateTimeOffset Ahora { get; } }

public interface IHasheadorContrasena
{
    string Hashear(string contrasena);
    bool Verificar(string hash, string contrasena);
}

public interface IContrasenasFiltradas { bool EstaFiltrada(string contrasena); }

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task GuardarAsync(Usuario usuario, CancellationToken ct = default);
}

/// <summary>Cifrado reversible del secreto TOTP en reposo (módulo 24: cifrado de columna).</summary>
public interface IProtectorSecretos
{
    string Proteger(string secreto);
    string Desproteger(string protegido);
}

/// <summary>Puerto hacia el módulo Auditoría (RF-IAM-009). Nunca recibe contraseñas ni tokens.</summary>
public interface IAuditoriaIdentidad
{
    Task RegistrarAsync(EventoIdentidad evento, CancellationToken ct = default);
}

/// <summary>Aviso al usuario sin información clínica (RN-016).</summary>
public interface IAvisosSeguridad
{
    Task NotificarBloqueoAsync(Usuario usuario, DateTimeOffset hasta, CancellationToken ct = default);
}

public enum TipoEventoIdentidad
{
    InicioSesionExitoso,
    InicioSesionFallido,
    CuentaBloqueada,
    MfaHabilitado,
    CodigoRecuperacionUsado,
}

public sealed record EventoIdentidad(
    TipoEventoIdentidad Tipo,
    DateTimeOffset OcurridoEn,
    Guid? UsuarioId = null,
    Guid? OrganizacionId = null,
    string? Detalle = null);
