using System.Security.Cryptography;
using System.Text;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum ResultadoSolicitudCodigo { Enviado, NoPermitido, EsperaRequerida }

public enum ResultadoConfirmacionCodigo { Exito, Invalido, NoPermitido }

/// <summary>
/// Segundo factor opcional para pacientes con un código de 6 dígitos enviado por mensaje (RF-IAM-003). El personal
/// no puede usarlo: su segundo factor es TOTP (RF-IAM-002). El código vale 10 minutos, se usa una sola vez y se anula
/// tras 5 intentos fallidos; solo se guarda su hash.
/// </summary>
public sealed class ServicioMfaPorMensaje(
    IRepositorioUsuarios usuarios,
    ITokensAccion tokens,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IAvisosSeguridad avisos,
    IMensajeriaIdentidad mensajeria)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan EsperaEntreEnvios = TimeSpan.FromMinutes(1);
    public const int IntentosMaximos = 5;

    private readonly RegistradorFallos _fallos = new(usuarios, reloj, auditoria, avisos);

    // ---- Activar y desactivar (paciente ya autenticado) ----

    public async Task<ResultadoSolicitudCodigo> IniciarActivacionAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo || usuario.RequiereMfa || usuario.MfaPorMensajeHabilitado)
            return ResultadoSolicitudCodigo.NoPermitido;
        return await EnviarCodigoAsync(usuario, ct);
    }

    public async Task<ResultadoConfirmacionCodigo> ConfirmarActivacionAsync(Guid usuarioId, string codigo, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo || usuario.RequiereMfa || usuario.MfaPorMensajeHabilitado)
            return ResultadoConfirmacionCodigo.NoPermitido;
        if (!await ConsumirCodigoAsync(usuario, codigo, ct)) return ResultadoConfirmacionCodigo.Invalido;

        usuario.HabilitarMfaPorMensaje();
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.MfaCorreoHabilitado, reloj.Ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return ResultadoConfirmacionCodigo.Exito;
    }

    /// <summary>El endpoint exige una reautenticación reciente (RF-IAM-010).</summary>
    public async Task DesactivarAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || !usuario.MfaPorMensajeHabilitado) return;

        usuario.DeshabilitarMfaPorMensaje();
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.MfaCorreoDeshabilitado, reloj.Ahora,
            usuario.Id, usuario.OrganizacionId), ct);
    }

    // ---- Inicio de sesión ----

    /// <summary>Envía (o reenvía) el código del inicio de sesión. Respeta la espera entre envíos.</summary>
    public async Task EnviarCodigoAccesoAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo || !usuario.MfaPorMensajeHabilitado) return;
        await EnviarCodigoAsync(usuario, ct);
    }

    public async Task<ResultadoInicioSesion> VerificarCodigoAccesoAsync(Guid usuarioId, string codigo, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo || !usuario.MfaPorMensajeHabilitado)
            return new(EstadoInicioSesion.CredencialesInvalidas);

        var ahora = reloj.Ahora;
        if (usuario.EstaBloqueado(ahora))
        {
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
                usuario.Id, usuario.OrganizacionId, "cuenta_bloqueada"), ct);
            return new(EstadoInicioSesion.CuentaBloqueada, BloqueadoHasta: usuario.BloqueadoHasta);
        }

        if (!await ConsumirCodigoAsync(usuario, codigo, ct))
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

    // ---- Códigos ----

    private async Task<ResultadoSolicitudCodigo> EnviarCodigoAsync(Usuario usuario, CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var pendientes = await tokens.ObtenerPendientesDeUsuarioAsync(usuario.Id, PropositoToken.CodigoMfaCorreo, ct);
        if (pendientes.Any(t => ahora - t.CreadoEn < EsperaEntreEnvios)) return ResultadoSolicitudCodigo.EsperaRequerida;

        foreach (var anterior in pendientes) anterior.Invalidar(ahora);
        if (pendientes.Count > 0) await tokens.GuardarAsync(pendientes, ct);

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var token = TokenAccion.Crear(PropositoToken.CodigoMfaCorreo, usuario.Id, null, usuario.OrganizacionId,
            HashDe(usuario.Id, codigo), ahora, Vigencia);
        await tokens.AgregarAsync(token, ct);
        await mensajeria.EnviarCodigoMfaAsync(usuario, codigo, token.ExpiraEn, ct);
        return ResultadoSolicitudCodigo.Enviado;
    }

    /// <summary>true si el código coincide con el último vigente; lo consume. Cada fallo cuenta contra el código.</summary>
    private async Task<bool> ConsumirCodigoAsync(Usuario usuario, string? codigo, CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var vigente = (await tokens.ObtenerPendientesDeUsuarioAsync(usuario.Id, PropositoToken.CodigoMfaCorreo, ct))
            .Where(t => t.EstaVigente(ahora)).MaxBy(t => t.CreadoEn);
        if (vigente is null) return false;

        var limpio = (codigo ?? "").Replace(" ", "");
        var coincide = limpio.Length == 6 && limpio.All(char.IsAsciiDigit)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(vigente.HashToken), Encoding.ASCII.GetBytes(HashDe(usuario.Id, limpio)));

        if (coincide) vigente.MarcarUsado(ahora);
        else vigente.RegistrarIntentoFallido(ahora, IntentosMaximos);
        await tokens.GuardarAsync([vigente], ct);
        return coincide;
    }

    /// <summary>El código tiene solo 10^6 valores: se ata al usuario para que un hash filtrado no sirva en otra cuenta.</summary>
    private static string HashDe(Guid usuarioId, string codigo) => GeneradorTokens.HashDe($"{usuarioId}:{codigo}");
}
