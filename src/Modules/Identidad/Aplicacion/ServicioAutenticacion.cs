namespace Continuum.Identidad.Aplicacion;

public sealed record RespuestaAutenticacion(
    EstadoInicioSesion Estado,
    string? TokenAcceso = null,
    string? TokenDesafio = null,
    DateTimeOffset? BloqueadoHasta = null);

/// <summary>
/// Orquesta el flujo de inicio de sesión del personal: clave → (configurar MFA | segundo factor) → token de acceso.
/// El token de acceso solo se emite cuando se han superado todos los factores exigidos al usuario.
/// </summary>
public sealed class ServicioAutenticacion(
    ServicioInicioSesion inicioSesion,
    ServicioMfa mfa,
    IRepositorioUsuarios usuarios,
    IEmisorTokens emisor)
{
    private static readonly RespuestaAutenticacion Invalida = new(EstadoInicioSesion.CredencialesInvalidas);

    public async Task<RespuestaAutenticacion> IniciarSesionAsync(string correo, string contrasena, CancellationToken ct = default)
    {
        var r = await inicioSesion.IniciarSesionAsync(correo, contrasena, ct);
        return r.Estado switch
        {
            EstadoInicioSesion.Exitoso => await ConAccesoAsync(r.UsuarioId!.Value, ct),
            EstadoInicioSesion.RequiereSegundoFactor =>
                new(r.Estado, TokenDesafio: emisor.EmitirDesafio(r.UsuarioId!.Value, PropositoDesafio.SegundoFactor)),
            EstadoInicioSesion.RequiereConfiguracionMfa =>
                new(r.Estado, TokenDesafio: emisor.EmitirDesafio(r.UsuarioId!.Value, PropositoDesafio.ConfigurarMfa)),
            EstadoInicioSesion.CuentaBloqueada => new(r.Estado, BloqueadoHasta: r.BloqueadoHasta),
            _ => Invalida,
        };
    }

    public async Task<RespuestaAutenticacion> VerificarSegundoFactorAsync(
        string tokenDesafio, string codigo, CancellationToken ct = default)
    {
        if (emisor.ValidarDesafio(tokenDesafio, PropositoDesafio.SegundoFactor) is not { } usuarioId) return Invalida;

        var r = await mfa.VerificarSegundoFactorAsync(usuarioId, codigo, ct);
        return r.Estado switch
        {
            EstadoInicioSesion.Exitoso => await ConAccesoAsync(usuarioId, ct),
            EstadoInicioSesion.CuentaBloqueada => new(r.Estado, BloqueadoHasta: r.BloqueadoHasta),
            _ => Invalida,
        };
    }

    public async Task<InicioConfiguracionMfa?> IniciarConfiguracionMfaAsync(string tokenDesafio, CancellationToken ct = default) =>
        emisor.ValidarDesafio(tokenDesafio, PropositoDesafio.ConfigurarMfa) is { } id
            ? await mfa.IniciarConfiguracionAsync(id, ct)
            : null;

    /// <summary>Tras confirmar, el usuario vuelve a iniciar sesión con su segundo factor.</summary>
    public async Task<ResultadoConfirmacionMfa> ConfirmarConfiguracionMfaAsync(
        string tokenDesafio, string codigo, CancellationToken ct = default) =>
        emisor.ValidarDesafio(tokenDesafio, PropositoDesafio.ConfigurarMfa) is { } id
            ? await mfa.ConfirmarConfiguracionAsync(id, codigo, ct)
            : new(false, []);

    private async Task<RespuestaAutenticacion> ConAccesoAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        return usuario is null
            ? Invalida
            : new(EstadoInicioSesion.Exitoso, TokenAcceso: emisor.EmitirAcceso(usuario));
    }
}
