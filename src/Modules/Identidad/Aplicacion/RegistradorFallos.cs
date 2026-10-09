using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

/// <summary>Un único contador de fallos para primer y segundo factor (RF-IAM-004).</summary>
internal sealed class RegistradorFallos(
    IRepositorioUsuarios usuarios, IReloj reloj, IAuditoriaIdentidad auditoria, IAvisosSeguridad avisos)
{
    public async Task RegistrarAsync(Usuario usuario, string? detalle, CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var bloqueoNuevo = usuario.RegistrarFallo(ahora);
        await usuarios.GuardarAsync(usuario, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InicioSesionFallido, ahora,
            usuario.Id, usuario.OrganizacionId, detalle), ct);
        if (!bloqueoNuevo) return;

        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CuentaBloqueada, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        await avisos.NotificarBloqueoAsync(usuario, usuario.BloqueadoHasta!.Value, ct);
    }
}
