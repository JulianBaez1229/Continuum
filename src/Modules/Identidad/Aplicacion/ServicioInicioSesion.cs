using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum EstadoInicioSesion
{
    Exitoso,
    CredencialesInvalidas,
    CuentaBloqueada,
    /// <summary>Primer factor correcto; falta el código TOTP o de recuperación (RF-IAM-002).</summary>
    RequiereSegundoFactor,
    /// <summary>Primer factor correcto; debe configurar MFA antes de cualquier otra función (CA-IAM-001).</summary>
    RequiereConfiguracionMfa,
}

public sealed record ResultadoInicioSesion(
    EstadoInicioSesion Estado,
    Guid? UsuarioId = null,
    DateTimeOffset? BloqueadoHasta = null);

/// <summary>
/// Primer factor (RF-IAM-001) con bloqueo temporal (RF-IAM-004) y bitácora (RF-IAM-009).
/// Usuario inexistente, inactivo o con clave errónea producen la misma respuesta para no revelar cuentas.
/// Solo devuelve <see cref="EstadoInicioSesion.Exitoso"/> si el usuario no exige MFA; en otro caso el
/// inicio se completa en <see cref="ServicioMfa"/>. La emisión de tokens se añade en RF-IAM-006.
/// </summary>
public sealed class ServicioInicioSesion(
    IRepositorioUsuarios usuarios,
    IHasheadorContrasena hasheador,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IAvisosSeguridad avisos)
{
    private readonly RegistradorFallos _fallos = new(usuarios, reloj, auditoria, avisos);

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(
        string correo, string contrasena, CancellationToken ct = default)
    {
        var ahora = reloj.Ahora;
        var usuario = await usuarios.ObtenerPorCorreoAsync(correo, ct);

        if (usuario is null || usuario.Estado != EstadoUsuario.Activo)
        {
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora), ct);
            return new(EstadoInicioSesion.CredencialesInvalidas);
        }

        if (usuario.EstaBloqueado(ahora))
        {
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
                usuario.Id, usuario.OrganizacionId, "cuenta_bloqueada"), ct);
            return new(EstadoInicioSesion.CuentaBloqueada, BloqueadoHasta: usuario.BloqueadoHasta);
        }

        if (!hasheador.Verificar(usuario.HashContrasena, contrasena))
        {
            await _fallos.RegistrarAsync(usuario, null, ct);
            return new(EstadoInicioSesion.CredencialesInvalidas);
        }

        // La clave correcta NO reinicia el contador de fallos si falta el segundo factor:
        // de lo contrario, quien conozca la clave podría adivinar el código TOTP sin límite.
        if (usuario.RequiereMfa)
        {
            return new(usuario.MfaHabilitado
                ? EstadoInicioSesion.RequiereSegundoFactor
                : EstadoInicioSesion.RequiereConfiguracionMfa, usuario.Id);
        }

        usuario.RegistrarExito(ahora);
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionExitoso, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return new(EstadoInicioSesion.Exitoso, usuario.Id);
    }
}
