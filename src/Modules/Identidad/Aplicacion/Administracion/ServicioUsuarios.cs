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
        if (await usuarios.ObtenerPorCorreoAsync(correo, ct) is not null) return ResultadoOperacion.Conflicto("CORREO_DUPLICADO");

        var contrasenaDescartable = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var usuario = Usuario.Crear(perfil.OrganizacionId, correo, hasheador.Hashear(contrasenaDescartable), requiereMfa: true);

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await usuarios.AgregarAsync(usuario, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioCreado, actor, perfil, usuario.Id), c);
        }, ct);
        return ResultadoOperacion.Exito(usuario.Id);
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
        if (await usuarios.ObtenerPorCorreoAsync(nuevoCorreo, ct) is not null) return ResultadoOperacion.Conflicto("CORREO_DUPLICADO");

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            usuario.CambiarCorreo(nuevoCorreo);
            await usuarios.GuardarAsync(usuario, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioEditado, actor, perfil, usuario.Id, "campo=correo"), c);
        }, ct);
        return ResultadoOperacion.Exito(usuario.Id);
    }

    /// <summary>
    /// Desactiva la cuenta (no la elimina) y cierra todas sus sesiones: sin esto, un token de renovación ya emitido
    /// seguiría funcionando. Repetir la operación no cambia nada ni duplica el evento.
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

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            usuario.Desactivar();
            await usuarios.GuardarAsync(usuario, c);
            await sesiones.CerrarTodasAsync(usuario.Id, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.UsuarioDesactivado, actor, perfil, usuario.Id), c);
        }, ct);
        return ResultadoOperacion.Exito(usuario.Id);
    }

    private EventoAdministracion Evento(TipoEventoAdministracion tipo, ActorAdministracion actor, PerfilActor perfil,
        Guid objetivoId, string? detalle = null) =>
        new(tipo, reloj.Ahora, actor.UsuarioId, perfil.OrganizacionId, actor.SedeActivaId, objetivoId, objetivoId, detalle);
}
