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

        if (!Enum.IsDefined(rol)) return ResultadoOperacion.Invalida("ROL_INVALIDO");

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;

        // RF-IAM-002: todo rol distinto de PACIENTE y RED_APOYO exige MFA. Una cuenta creada sin esa exigencia
        // (las de paciente y red de apoyo) no puede operar con un rol de personal.
        if (rol is not (Rol.Paciente or Rol.RedApoyo) && !usuario.RequiereMfa)
            return ResultadoOperacion.Conflicto("CUENTA_SIN_MFA_OBLIGATORIO");

        if (!await sedes.ExisteEnOrganizacionAsync(perfil.OrganizacionId, sedeId, ct)) return ResultadoOperacion.Invalida("SEDE_INEXISTENTE");

        var asignacion = new RolAsignado(usuario.Id, rol, sedeId);
        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if ((await roles.ListarDeUsuarioAsync(usuario.Id, c)).Contains(asignacion)) return ResultadoOperacion.Conflicto("ROL_YA_ASIGNADO");
            await roles.AgregarAsync(asignacion, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.RolAsignado, actor, perfil, asignacion), c);
            return ResultadoOperacion.Exito(usuario.Id);
        }, "ROL_YA_ASIGNADO", ct);
    }

    /// <summary>Retira el rol de la sede. No permite quitar el último <c>ADMIN_FUNCIONAL</c> activo (<c>ULTIMO_ADMINISTRADOR</c>).</summary>
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
        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            var delUsuario = await roles.ListarDeUsuarioAsync(usuario.Id, c);
            if (!delUsuario.Contains(asignacion)) return ResultadoOperacion.NoEncontrado("ROL_NO_ASIGNADO");

            // Protección contra el bloqueo: no se retira el último rol ADMIN_FUNCIONAL activo de la organización.
            // Se lee dentro de la transacción para que dos retiros simultáneos no dejen la organización sin administrador.
            if (rol == Rol.AdminFuncional && usuario.Estado == EstadoUsuario.Activo
                && !delUsuario.Any(r => r.Rol == Rol.AdminFuncional && r != asignacion))
            {
                var administradores = await roles.ListarUsuariosActivosConRolAsync(usuario.OrganizacionId, Rol.AdminFuncional, c);
                if (administradores.All(id => id == usuario.Id)) return ResultadoOperacion.Conflicto("ULTIMO_ADMINISTRADOR");
            }

            await roles.QuitarAsync(asignacion, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.RolRetirado, actor, perfil, asignacion), c);
            return ResultadoOperacion.Exito(usuario.Id);
        }, "ROL_NO_ASIGNADO", ct);
    }

    private EventoAdministracion Evento(TipoEventoAdministracion tipo, ActorAdministracion actor, PerfilActor perfil, RolAsignado a) =>
        new(tipo, reloj.Ahora, actor.UsuarioId, perfil.OrganizacionId, actor.SedeActivaId, a.UsuarioId, a.UsuarioId,
            $"rol={a.Rol};sede={a.SedeId}");
}
