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
    CierreSesion,
    CierreSesionTodas,
    TokenRenovacionReutilizado,
    RecuperacionContrasenaSolicitada,
    ContrasenaCambiada,
    InvitacionPacienteEnviada,
    CuentaPacienteActivada,
    Reautenticacion,
    MfaCorreoHabilitado,
    MfaCorreoDeshabilitado,
}

public sealed record EventoIdentidad(
    TipoEventoIdentidad Tipo,
    DateTimeOffset OcurridoEn,
    Guid? UsuarioId = null,
    Guid? OrganizacionId = null,
    string? Detalle = null);

public enum PropositoDesafio { SegundoFactor, ConfigurarMfa }

/// <summary>
/// Tokens de acceso (15 min, RF-IAM-006) y desafíos de corta vida que atan el segundo factor a un
/// primer factor ya superado. Un desafío nunca sirve como token de acceso.
/// </summary>
public interface IEmisorTokens
{
    string EmitirAcceso(Usuario usuario, Guid sesionId);
    string EmitirDesafio(Guid usuarioId, PropositoDesafio proposito);
    Guid? ValidarDesafio(string token, PropositoDesafio proposito);
}

public interface ISesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken ct = default);
    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Busca por el hash vigente o por el inmediatamente anterior (detección de reutilización).</summary>
    Task<Sesion?> ObtenerPorHashRenovacionAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<Sesion>> ObtenerNoRevocadasDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task GuardarAsync(IEnumerable<Sesion> sesiones, CancellationToken ct = default);
}

/// <summary>Política de sesión por organización (módulo 06: <c>duracion_sesion_inactiva_min</c>).</summary>
public interface IPoliticaSesion
{
    TimeSpan Inactividad(Guid organizacionId, bool esPersonal);
    TimeSpan DuracionMaxima(Guid organizacionId, bool esPersonal);
}

public interface ITokensAccion
{
    Task AgregarAsync(TokenAccion token, CancellationToken ct = default);
    Task<TokenAccion?> ObtenerPorHashAsync(string hash, CancellationToken ct = default);
    /// <summary>Tokens de un usuario y propósito que aún no se han usado (aunque puedan haber caducado).</summary>
    Task<IReadOnlyList<TokenAccion>> ObtenerPendientesDeUsuarioAsync(Guid usuarioId, PropositoToken proposito, CancellationToken ct = default);
    Task GuardarAsync(IEnumerable<TokenAccion> tokens, CancellationToken ct = default);
}

public enum CanalInvitacion { Correo, Sms }

/// <summary>
/// Mensajes que salen del módulo de identidad hacia la persona (módulo 17). Reciben el token en claro solo para
/// construir el enlace o el código: el texto nunca incluye información clínica (RN-016) y no debe registrarse en logs.
/// </summary>
public interface IMensajeriaIdentidad
{
    Task EnviarEnlaceRecuperacionAsync(Usuario usuario, string token, DateTimeOffset expiraEn, CancellationToken ct = default);
    Task NotificarCambioContrasenaAsync(Usuario usuario, CancellationToken ct = default);
    Task EnviarInvitacionPacienteAsync(Guid pacienteId, CanalInvitacion canal, string destino, string token, DateTimeOffset expiraEn, CancellationToken ct = default);
    Task EnviarCodigoMfaAsync(Usuario usuario, string codigo, DateTimeOffset expiraEn, CancellationToken ct = default);
}
