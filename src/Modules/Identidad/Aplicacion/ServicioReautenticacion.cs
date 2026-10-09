using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public sealed record RespuestaReautenticacion(
    EstadoInicioSesion Estado, string? TokenAcceso = null, DateTimeOffset? BloqueadoHasta = null);

/// <summary>
/// Reautenticación (contraseña + segundo factor) antes de una acción crítica (RF-IAM-010). Mantiene la misma sesión y
/// emite un acceso con la marca <c>rea</c>; las acciones críticas la exigen con la política
/// <c>ReautenticacionReciente</c>. Los fallos cuentan para el mismo bloqueo que el inicio de sesión (RF-IAM-004).
/// </summary>
public sealed class ServicioReautenticacion(
    IRepositorioUsuarios usuarios,
    IHasheadorContrasena hasheador,
    ServicioMfa mfa,
    IEmisorTokens emisor,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IAvisosSeguridad avisos)
{
    private const string Detalle = "reautenticacion";
    private static readonly RespuestaReautenticacion Invalida = new(EstadoInicioSesion.CredencialesInvalidas);

    private readonly RegistradorFallos _fallos = new(usuarios, reloj, auditoria, avisos);

    public async Task<RespuestaReautenticacion> ReautenticarAsync(
        Guid usuarioId, Guid sesionId, string contrasena, string? codigoMfa, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo) return Invalida;

        var ahora = reloj.Ahora;
        if (usuario.EstaBloqueado(ahora))
        {
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
                usuario.Id, usuario.OrganizacionId, "cuenta_bloqueada"), ct);
            return new(EstadoInicioSesion.CuentaBloqueada, BloqueadoHasta: usuario.BloqueadoHasta);
        }

        var claveValida = hasheador.Verificar(usuario.HashContrasena, contrasena);
        // Quien debe tener MFA sin haberlo configurado no puede reautenticarse: antes debe configurarlo.
        var segundoFactorValido = !(usuario.RequiereMfa || usuario.MfaHabilitado)
                                  || (claveValida && await mfa.ComprobarCodigoAsync(usuario, codigoMfa, ct));

        if (!claveValida || !segundoFactorValido)
        {
            await _fallos.RegistrarAsync(usuario, Detalle, ct);
            return Invalida;
        }

        usuario.ReiniciarFallos();
        await usuarios.GuardarAsync(usuario, ct); // incluye el paso TOTP o el código de recuperación consumido
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.Reautenticacion, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return new(EstadoInicioSesion.Exitoso, emisor.EmitirAcceso(usuario, sesionId, ahora));
    }
}
