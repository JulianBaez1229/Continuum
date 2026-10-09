using System.Security.Cryptography;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary>
/// RF-ROL-001: crear, editar y desactivar cuentas; las cuentas no se eliminan. Solo <c>ADMIN_FUNCIONAL</c>.
/// Cada cambio y su evento de bitácora (RF-ROL-009) se guardan en la misma transacción.
/// </summary>
public sealed class ServicioUsuarios(
    GuardiaAdministracion guardia,
    IRepositorioUsuarios usuarios,
    IRepositorioRolesAsignados roles,
    IHasheadorContrasena hasheador,
    ServicioSesiones sesiones,
    IUnidadDeTrabajo unidadDeTrabajo,
    IAuditoriaAdministracion auditoria,
    IReloj reloj)
{
    /// <summary>
    /// Crea una cuenta de personal en la organización del administrador. La cuenta nace con una contraseña aleatoria
    /// que nadie conoce y con MFA obligatorio (RF-IAM-002); la persona establece la suya con el enlace de recuperación
    /// (RF-IAM-005) o la invitación. Las cuentas de paciente se crean por el flujo de activación (RF-IAM-007).
    /// </summary>
    public async Task<ResultadoOperacion> CrearAsync(ActorAdministracion actor, string correo, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Crear, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        if (!Usuario.EsCorreoValido(correo)) return ResultadoOperacion.Invalida("CORREO_INVALIDO");

        var contrasenaDescartable = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var usuario = Usuario.Crear(perfil.OrganizacionId, correo, hasheador.Hashear(contrasenaDescartable), requiereMfa: true);

        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (await usuarios.ObtenerPorCorreoAsync(correo, c) is not null) return ResultadoOperacion.Conflicto("CORREO_DUPLICADO");
            await usuarios.AgregarAsync(usuario, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioCreado, actor, perfil, usuario.Id), c);
            return ResultadoOperacion.Exito(usuario.Id);
        }, "CORREO_DUPLICADO", ct);
    }

    public async Task<ResultadoOperacion> EditarCorreoAsync(
        ActorAdministracion actor, Guid usuarioId, string nuevoCorreo, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Actualizar, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;

        if (!Usuario.EsCorreoValido(nuevoCorreo)) return ResultadoOperacion.Invalida("CORREO_INVALIDO");
        if (Usuario.NormalizarCorreo(nuevoCorreo) == usuario.Correo) return ResultadoOperacion.Exito(usuario.Id);

        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (await usuarios.ObtenerPorCorreoAsync(nuevoCorreo, c) is not null) return ResultadoOperacion.Conflicto("CORREO_DUPLICADO");
            usuario.CambiarCorreo(nuevoCorreo);
            await usuarios.GuardarAsync(usuario, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioEditado, actor, perfil, usuario.Id, "campo=correo"), c);
            return ResultadoOperacion.Exito(usuario.Id);
        }, "CORREO_DUPLICADO", ct);
    }

    /// <summary>
    /// Desactiva la cuenta (no la elimina) y cierra todas sus sesiones: sin esto, un token de renovación ya emitido
    /// seguiría funcionando. Repetir la operación no cambia nada ni duplica el evento. No permite desactivar al último
    /// <c>ADMIN_FUNCIONAL</c> activo de la organización (<c>ULTIMO_ADMINISTRADOR</c>).
    /// </summary>
    public async Task<ResultadoOperacion> DesactivarAsync(ActorAdministracion actor, Guid usuarioId, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Anular, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;
        if (usuario.Estado == EstadoUsuario.Inactivo) return ResultadoOperacion.Exito(usuario.Id);

        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            // Protección contra el bloqueo: la organización no se queda sin quien administre usuarios. Se lee dentro de la
            // transacción para que dos desactivaciones simultáneas no dejen la organización sin administrador.
            var administradores = await roles.ListarUsuariosActivosConRolAsync(usuario.OrganizacionId, Rol.AdminFuncional, c);
            if (administradores.Contains(usuario.Id) && administradores.All(id => id == usuario.Id))
                return ResultadoOperacion.Conflicto("ULTIMO_ADMINISTRADOR");

            usuario.Desactivar();
            await usuarios.GuardarAsync(usuario, c);
            await sesiones.CerrarTodasAsync(usuario.Id, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioDesactivado, actor, perfil, usuario.Id), c);
            return ResultadoOperacion.Exito(usuario.Id);
        }, "USUARIO_YA_MODIFICADO", ct);
    }

    private EventoAdministracion Evento(TipoEventoAdministracion tipo, ActorAdministracion actor, PerfilActor perfil,
        Guid objetivoId, string? detalle = null) =>
        new(tipo, reloj.Ahora, actor.UsuarioId, perfil.OrganizacionId, actor.SedeActivaId, objetivoId, objetivoId, detalle);
}
