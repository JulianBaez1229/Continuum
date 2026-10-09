namespace Continuum.Identidad.Aplicacion;

public sealed record RespuestaAutenticacion(
    EstadoInicioSesion Estado,
    string? TokenAcceso = null,
    string? TokenDesafio = null,
    DateTimeOffset? BloqueadoHasta = null,
    string? TokenRenovacion = null);

/// <summary>
/// Orquesta el flujo de inicio de sesión del personal: clave → (configurar MFA | segundo factor) → sesión y tokens.
/// Los tokens solo se emiten cuando se han superado todos los factores exigidos al usuario.
/// </summary>
public sealed class ServicioAutenticacion(
    ServicioInicioSesion inicioSesion,
    ServicioMfa mfa,
    ServicioSesiones sesiones,
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

    /// <summary>Canjea el token de renovación por un acceso nuevo y un token de renovación nuevo (rotación).</summary>
    public async Task<RespuestaAutenticacion> RenovarAsync(string tokenRenovacion, CancellationToken ct = default)
    {
        var r = await sesiones.RenovarAsync(tokenRenovacion, ct);
        if (r.Estado == EstadoRenovacion.SesionExpirada) return new(EstadoInicioSesion.SesionExpirada);
        if (r.Estado != EstadoRenovacion.Exitosa) return Invalida;

        var usuario = await usuarios.ObtenerPorIdAsync(r.Sesion!.UsuarioId, ct);
        if (usuario is null || usuario.Estado != Dominio.EstadoUsuario.Activo)
        {
            await sesiones.CerrarAsync(r.Sesion.Id, ct);
            return Invalida;
        }
        return new(EstadoInicioSesion.Exitoso,
            TokenAcceso: emisor.EmitirAcceso(usuario, r.Sesion.Id), TokenRenovacion: r.TokenRenovacion);
    }

    public Task CerrarSesionAsync(Guid sesionId, CancellationToken ct = default) => sesiones.CerrarAsync(sesionId, ct);

    public Task CerrarTodasLasSesionesAsync(Guid usuarioId, CancellationToken ct = default) =>
        sesiones.CerrarTodasAsync(usuarioId, ct);

    private async Task<RespuestaAutenticacion> ConAccesoAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return Invalida;

        var (sesion, tokenRenovacion) = await sesiones.IniciarAsync(usuario, ct);
        return new(EstadoInicioSesion.Exitoso,
            TokenAcceso: emisor.EmitirAcceso(usuario, sesion.Id), TokenRenovacion: tokenRenovacion);
    }
}
