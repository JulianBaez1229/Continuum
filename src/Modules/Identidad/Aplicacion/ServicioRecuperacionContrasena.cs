using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum ResultadoRestablecimiento { Exito, TokenInvalido, ContrasenaInvalida }

public readonly record struct RespuestaRestablecimiento(
    ResultadoRestablecimiento Resultado, MotivoContrasenaInvalida? Motivo = null);

/// <summary>
/// Recuperación de contraseña con enlace de un solo uso que caduca en 30 minutos (RF-IAM-005).
/// Nunca revela si el correo existe y nunca desactiva el segundo factor.
/// </summary>
public sealed class ServicioRecuperacionContrasena(
    IRepositorioUsuarios usuarios,
    ITokensAccion tokens,
    ServicioSesiones sesiones,
    PoliticaContrasena politica,
    IHasheadorContrasena hasheador,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IMensajeriaIdentidad mensajeria)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);
    /// <summary>Evita usar la función para inundar de correos a una persona.</summary>
    public static readonly TimeSpan EsperaEntreSolicitudes = TimeSpan.FromMinutes(1);

    /// <summary>Siempre termina igual, exista o no la cuenta, para no permitir descubrir qué correos están registrados.</summary>
    public async Task SolicitarAsync(string correo, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorCorreoAsync(correo, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo) return;

        var ahora = reloj.Ahora;
        var pendientes = await tokens.ObtenerPendientesDeUsuarioAsync(usuario.Id, PropositoToken.RecuperacionContrasena, ct);
        if (pendientes.Any(t => ahora - t.CreadoEn < EsperaEntreSolicitudes)) return;

        foreach (var anterior in pendientes) anterior.Invalidar(ahora);
        if (pendientes.Count > 0) await tokens.GuardarAsync(pendientes, ct);

        var plano = GeneradorTokens.Nuevo();
        var token = TokenAccion.Crear(PropositoToken.RecuperacionContrasena, usuario.Id, null, usuario.OrganizacionId,
            GeneradorTokens.HashDe(plano), ahora, Vigencia);
        await tokens.AgregarAsync(token, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.RecuperacionContrasenaSolicitada, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        await mensajeria.EnviarEnlaceRecuperacionAsync(usuario, plano, token.ExpiraEn, ct);
    }

    public async Task<RespuestaRestablecimiento> RestablecerAsync(string tokenPlano, string nuevaContrasena, CancellationToken ct = default)
    {
        var invalido = new RespuestaRestablecimiento(ResultadoRestablecimiento.TokenInvalido);
        if (string.IsNullOrWhiteSpace(tokenPlano)) return invalido;

        var ahora = reloj.Ahora;
        var token = await tokens.ObtenerPorHashAsync(GeneradorTokens.HashDe(tokenPlano), ct);
        if (token is not { Proposito: PropositoToken.RecuperacionContrasena, UsuarioId: { } usuarioId } || !token.EstaVigente(ahora))
            return invalido;

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo) return invalido;

        // Una contraseña débil no gasta el enlace: la persona puede corregirla dentro de la vigencia.
        var validacion = politica.Validar(nuevaContrasena);
        if (!validacion.EsValida) return new(ResultadoRestablecimiento.ContrasenaInvalida, validacion.Motivo);

        // Primero se consume el enlace: si algo falla después, la persona pide otro; nunca queda uno reutilizable.
        token.MarcarUsado(ahora);
        await tokens.GuardarAsync([token], ct);

        usuario.CambiarContrasena(hasheador.Hashear(nuevaContrasena));
        await usuarios.GuardarAsync(usuario, ct);
        await sesiones.CerrarTodasAsync(usuario.Id, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.ContrasenaCambiada, ahora,
            usuario.Id, usuario.OrganizacionId, "recuperacion"), ct);
        await mensajeria.NotificarCambioContrasenaAsync(usuario, ct);
        return new(ResultadoRestablecimiento.Exito);
    }
}
