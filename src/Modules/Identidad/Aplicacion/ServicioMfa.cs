using System.Security.Cryptography;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public sealed record InicioConfiguracionMfa(string SecretoBase32, string UriOtpAuth);

public sealed record ResultadoConfirmacionMfa(bool Exito, IReadOnlyList<string> CodigosRecuperacion);

/// <summary>
/// Segundo factor TOTP con códigos de recuperación (RF-IAM-002, CA-IAM-001).
/// Quien llama debe haber superado el primer factor: el endpoint tiene que atar <c>usuarioId</c> a un
/// desafío de corta vida emitido por <see cref="ServicioInicioSesion"/>, nunca a un dato enviado por el cliente.
/// </summary>
public sealed class ServicioMfa(
    IRepositorioUsuarios usuarios,
    IHasheadorContrasena hasheador,
    IProtectorSecretos protector,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IAvisosSeguridad avisos)
{
    private const string Emisor = "Continuum";
    private const int CodigosDeRecuperacion = 10;
    private const int LongitudCodigoRecuperacion = 10;

    private readonly RegistradorFallos _fallos = new(usuarios, reloj, auditoria, avisos);

    /// <summary>Devuelve null si el usuario no existe o ya tiene MFA activo (no se puede sustituir con solo la clave).</summary>
    public async Task<InicioConfiguracionMfa?> IniciarConfiguracionAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo) return null;

        var secreto = Base32.Codificar(RandomNumberGenerator.GetBytes(20));
        if (!usuario.PrepararMfa(protector.Proteger(secreto))) return null;

        await usuarios.GuardarAsync(usuario, ct);
        var uri = $"otpauth://totp/{Emisor}:{Uri.EscapeDataString(usuario.Correo)}" +
                  $"?secret={secreto}&issuer={Emisor}&algorithm=SHA1&digits={Totp.Digitos}&period={Totp.PasoSegundos}";
        return new(secreto, uri);
    }

    /// <summary>Activa el MFA si el código es válido y entrega los códigos de recuperación (se muestran una sola vez).</summary>
    public async Task<ResultadoConfirmacionMfa> ConfirmarConfiguracionAsync(
        Guid usuarioId, string codigo, CancellationToken ct = default)
    {
        var fallo = new ResultadoConfirmacionMfa(false, []);
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario?.SecretoTotpPendienteProtegido is not { } pendiente) return fallo;

        var secreto = Base32.Decodificar(protector.Desproteger(pendiente));
        if (!Totp.TryValidar(secreto, codigo, reloj.Ahora, null, out var paso)) return fallo;

        var codigos = Enumerable.Range(0, CodigosDeRecuperacion)
            .Select(_ => Base32.Aleatorio(LongitudCodigoRecuperacion)).ToList();
        usuario.ActivarMfa(paso, codigos.Select(hasheador.Hashear));
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.MfaHabilitado, reloj.Ahora,
            usuario.Id, usuario.OrganizacionId), ct);

        return new(true, codigos.Select(c => $"{c[..5]}-{c[5..]}").ToList());
    }

    public async Task<ResultadoInicioSesion> VerificarSegundoFactorAsync(
        Guid usuarioId, string codigo, CancellationToken ct = default)
    {
        var ahora = reloj.Ahora;
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo
            || !usuario.MfaHabilitado || usuario.SecretoTotpProtegido is null)
            return new(EstadoInicioSesion.CredencialesInvalidas);

        if (usuario.EstaBloqueado(ahora))
        {
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
                usuario.Id, usuario.OrganizacionId, "cuenta_bloqueada"), ct);
            return new(EstadoInicioSesion.CuentaBloqueada, BloqueadoHasta: usuario.BloqueadoHasta);
        }

        var valido = await ComprobarCodigoAsync(usuario, codigo, ct);

        if (!valido)
        {
            await _fallos.RegistrarAsync(usuario, "segundo_factor", ct);
            return new(EstadoInicioSesion.CredencialesInvalidas);
        }

        usuario.RegistrarExito(ahora);
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionExitoso, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return new(EstadoInicioSesion.Exitoso, usuario.Id);
    }

    /// <summary>
    /// Comprueba un código TOTP o de recuperación sin cerrar ningún inicio de sesión (lo usa la reautenticación).
    /// Si es válido marca el paso TOTP o consume el código de recuperación: <b>quien llama debe guardar al usuario</b>.
    /// </summary>
    public async Task<bool> ComprobarCodigoAsync(Usuario usuario, string? codigo, CancellationToken ct = default)
    {
        if (!usuario.MfaHabilitado || usuario.SecretoTotpProtegido is null) return false;

        var ahora = reloj.Ahora;
        var limpio = (codigo ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
        return limpio.Length == Totp.Digitos
            ? VerificarTotp(usuario, limpio, ahora)
            : await VerificarRecuperacionAsync(usuario, limpio, ahora, ct);
    }

    private bool VerificarTotp(Usuario usuario, string codigo, DateTimeOffset ahora)
    {
        var secreto = Base32.Decodificar(protector.Desproteger(usuario.SecretoTotpProtegido!));
        if (!Totp.TryValidar(secreto, codigo, ahora, usuario.UltimoPasoTotp, out var paso)) return false;
        usuario.RegistrarPasoTotp(paso);
        return true;
    }

    private async Task<bool> VerificarRecuperacionAsync(Usuario usuario, string codigo, DateTimeOffset ahora, CancellationToken ct)
    {
        if (codigo.Length != LongitudCodigoRecuperacion) return false;
        var hash = usuario.CodigosRecuperacionHash.FirstOrDefault(h => hasheador.Verificar(h, codigo));
        if (hash is null) return false;

        usuario.ConsumirCodigoRecuperacion(hash);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CodigoRecuperacionUsado, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return true;
    }
}
