using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Continuum.Identidad.Infraestructura.Seguridad;

public sealed class RelojSistema : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}

/// <summary>Hash de contraseñas con el algoritmo de ASP.NET Core Identity (PBKDF2, con rehash futuro posible).</summary>
public sealed class HasheadorContrasenaIdentity : IHasheadorContrasena
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Contexto = new();

    public string Hashear(string contrasena) => _hasher.HashPassword(Contexto, contrasena);

    public bool Verificar(string hash, string contrasena) =>
        _hasher.VerifyHashedPassword(Contexto, hash, contrasena) != PasswordVerificationResult.Failed;
}

/// <summary>
/// Cifra el secreto TOTP en reposo con Data Protection. En producción el anillo de claves debe persistirse
/// fuera del contenedor (módulo 24); de lo contrario los secretos no se podrán descifrar tras un reinicio.
/// </summary>
public sealed class ProtectorSecretosDataProtection(IDataProtectionProvider proveedor) : IProtectorSecretos
{
    private readonly IDataProtector _protector = proveedor.CreateProtector("Continuum.Identidad.SecretoTotp.v1");

    public string Proteger(string secreto) => _protector.Protect(secreto);
    public string Desproteger(string protegido) => _protector.Unprotect(protegido);
}

/// <summary>
/// PROVISIONAL hasta el módulo 23: escribe solo tipo, ids y hora en el log de la aplicación.
/// La bitácora definitiva debe persistirse en la misma transacción (módulo 23). Nunca registra credenciales.
/// </summary>
public sealed class AuditoriaIdentidadProvisional(ILogger<AuditoriaIdentidadProvisional> log) : IAuditoriaIdentidad
{
    public Task RegistrarAsync(EventoIdentidad e, CancellationToken ct = default)
    {
        log.LogInformation("evento_identidad {Tipo} usuario={UsuarioId} organizacion={OrganizacionId} detalle={Detalle} en={OcurridoEn:o}",
            e.Tipo, e.UsuarioId, e.OrganizacionId, e.Detalle, e.OcurridoEn);
        return Task.CompletedTask;
    }
}

/// <summary>PROVISIONAL hasta el módulo 17: no envía correo; deja constancia sin datos personales.</summary>
public sealed class AvisosSeguridadProvisional(ILogger<AvisosSeguridadProvisional> log) : IAvisosSeguridad
{
    public Task NotificarBloqueoAsync(Usuario usuario, DateTimeOffset hasta, CancellationToken ct = default)
    {
        log.LogWarning("aviso_bloqueo_pendiente_de_envio usuario={UsuarioId} hasta={Hasta:o}", usuario.Id, hasta);
        return Task.CompletedTask;
    }
}
