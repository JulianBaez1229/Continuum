using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary>
/// RF-ROL-002: uno o varios roles por usuario, asignados por sede. Solo <c>ADMIN_FUNCIONAL</c>.
/// Cada cambio y su evento de bitácora (RF-ROL-009) se guardan en la misma transacción.
/// </summary>
public sealed class ServicioRoles(
    GuardiaAdministracion guardia,
    IRepositorioUsuarios usuarios,
    IRepositorioRolesAsignados roles,
    IConsultaSedes sedes,
    IUnidadDeTrabajo unidadDeTrabajo,
    IAuditoriaAdministracion auditoria,
    IReloj reloj)
{
    public async Task<ResultadoOperacion> AsignarAsync(
        ActorAdministracion actor, Guid usuarioId, Rol rol, Guid sedeId, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Crear, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;

        if (!Enum.IsDefined(rol)) return ResultadoOperacion.Invalida("ROL_INVALIDO");
        if (!await sedes.ExisteEnOrganizacionAsync(perfil.OrganizacionId, sedeId, ct)) return ResultadoOperacion.Invalida("SEDE_INEXISTENTE");

        var asignacion = new RolAsignado(usuario.Id, rol, sedeId);
        if ((await roles.ListarDeUsuarioAsync(usuario.Id, ct)).Contains(asignacion)) return ResultadoOperacion.Conflicto("ROL_YA_ASIGNADO");

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await roles.AgregarAsync(asignacion, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.RolAsignado, actor, perfil, asignacion), c);
        }, ct);
        return ResultadoOperacion.Exito(usuario.Id);
    }

    public async Task<ResultadoOperacion> RetirarAsync(
        ActorAdministracion actor, Guid usuarioId, Rol rol, Guid sedeId, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Anular, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;

        var asignacion = new RolAsignado(usuario.Id, rol, sedeId);
        if (!(await roles.ListarDeUsuarioAsync(usuario.Id, ct)).Contains(asignacion)) return ResultadoOperacion.NoEncontrado("ROL_NO_ASIGNADO");

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await roles.QuitarAsync(asignacion, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.RolRetirado, actor, perfil, asignacion), c);
        }, ct);
        return ResultadoOperacion.Exito(usuario.Id);
    }

    private EventoAdministracion Evento(TipoEventoAdministracion tipo, ActorAdministracion actor, PerfilActor perfil, RolAsignado a) =>
        new(tipo, reloj.Ahora, actor.UsuarioId, perfil.OrganizacionId, actor.SedeActivaId, a.UsuarioId, a.UsuarioId,
            $"rol={a.Rol};sede={a.SedeId}");
}
