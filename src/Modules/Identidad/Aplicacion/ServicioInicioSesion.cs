using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum EstadoInicioSesion { Exitoso, CredencialesInvalidas, CuentaBloqueada }

public sealed record ResultadoInicioSesion(
    EstadoInicioSesion Estado,
    Guid? UsuarioId = null,
    DateTimeOffset? BloqueadoHasta = null);

/// <summary>
/// Primer factor (RF-IAM-001) con bloqueo temporal (RF-IAM-004) y bitácora (RF-IAM-009).
/// Usuario inexistente, inactivo o con clave errónea producen la misma respuesta para no revelar cuentas.
/// El segundo factor y la emisión de tokens se añaden en RF-IAM-002 y RF-IAM-006.
/// </summary>
public sealed class ServicioInicioSesion(
    IRepositorioUsuarios usuarios,
    IHasheadorContrasena hasheador,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IAvisosSeguridad avisos)
{
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
            var bloqueoNuevo = usuario.RegistrarFallo(ahora);
            await usuarios.GuardarAsync(usuario, ct);
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
                usuario.Id, usuario.OrganizacionId), ct);
            if (bloqueoNuevo)
            {
                await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CuentaBloqueada, ahora,
                    usuario.Id, usuario.OrganizacionId), ct);
                await avisos.NotificarBloqueoAsync(usuario, usuario.BloqueadoHasta!.Value, ct);
            }
            return new(EstadoInicioSesion.CredencialesInvalidas);
        }

        usuario.RegistrarExito(ahora);
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionExitoso, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return new(EstadoInicioSesion.Exitoso, usuario.Id);
    }
}
