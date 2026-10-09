using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

/// <summary>Resuelve el <see cref="PerfilActor"/> desde usuarios, <c>rol_asignado</c> y la ficha de profesional.</summary>
public sealed class ConsultaRolesPorSede(
    IRepositorioUsuarios usuarios, IRepositorioRolesAsignados roles, IRepositorioProfesionales profesionales) : IRolesPorSede
{
    public async Task<PerfilActor?> ObtenerPerfilAsync(Guid usuarioId, Guid sedeId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null || usuario.Estado != EstadoUsuario.Activo) return null;

        var rolesEnSede = (await roles.ListarDeUsuarioAsync(usuarioId, ct))
            .Where(r => r.SedeId == sedeId)
            .Select(r => r.Rol)
            .ToHashSet();
        var profesional = await profesionales.ObtenerPorUsuarioAsync(usuarioId, ct);

        return new PerfilActor(usuario.Id, usuario.OrganizacionId, rolesEnSede, profesional?.Id);
    }
}
