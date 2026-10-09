using Continuum.Identidad.Aplicacion.Autorizacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Administracion;

public sealed record RegistrarProfesional(
    Guid UsuarioId, string Nombres, string Apellidos, string TipoProfesional, string? Licencia = null, string? Exequatur = null);

/// <summary>
/// RF-ROL-003: ficha del profesional y sus habilitaciones (especialidad, procedimientos, licencia o exequátur y
/// vencimiento). Solo <c>ADMIN_FUNCIONAL</c>. Cada cambio y su evento de bitácora (RF-ROL-009) van en la misma transacción.
/// El bloqueo de escrituras con la habilitación vencida (RF-ROL-004, RN-012) lo aplica la política de acceso (R8) con
/// lo que devuelve <see cref="IHabilitaciones"/>.
/// </summary>
public sealed class ServicioHabilitaciones(
    GuardiaAdministracion guardia,
    IRepositorioUsuarios usuarios,
    IRepositorioProfesionales profesionales,
    IRepositorioHabilitaciones habilitaciones,
    ICatalogoClinico catalogo,
    IZonaHorariaSede zonaHoraria,
    IUnidadDeTrabajo unidadDeTrabajo,
    IAuditoriaAdministracion auditoria,
    IReloj reloj)
{
    public async Task<ResultadoOperacion> RegistrarProfesionalAsync(
        ActorAdministracion actor, RegistrarProfesional datos, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Crear, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var usuario = await usuarios.ObtenerPorIdAsync(datos.UsuarioId, ct);
        if (usuario is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, usuario.OrganizacionId, usuario.Id, ct) is { } ajena) return ajena;

        if (Profesional.Validar(datos.Nombres, datos.Apellidos, datos.TipoProfesional, datos.Licencia, datos.Exequatur) is { } invalido)
            return ResultadoOperacion.Invalida(invalido);

        var profesional = Profesional.Crear(usuario.Id, perfil.OrganizacionId, datos.Nombres, datos.Apellidos,
            datos.TipoProfesional, datos.Licencia, datos.Exequatur);

        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (await profesionales.ObtenerPorUsuarioAsync(usuario.Id, c) is not null)
                return ResultadoOperacion.Conflicto("PROFESIONAL_YA_REGISTRADO");
            await profesionales.AgregarAsync(profesional, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.ProfesionalRegistrado, actor, perfil, usuario.Id, profesional.Id), c);
            return ResultadoOperacion.Exito(profesional.Id);
        }, "PROFESIONAL_YA_REGISTRADO", ct);
    }

    public async Task<ResultadoOperacion> RegistrarHabilitacionAsync(
        ActorAdministracion actor, Guid profesionalId, Guid especialidadId,
        IReadOnlyCollection<Guid> procedimientosPermitidos, DateOnly vigenteHasta, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Crear, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var profesional = await profesionales.ObtenerPorIdAsync(profesionalId, ct);
        if (profesional is null) return ResultadoOperacion.NoEncontrado();
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, profesional.OrganizacionId, profesional.UsuarioId, ct) is { } ajena) return ajena;

        if (await Validar(actor, perfil.OrganizacionId, procedimientosPermitidos, vigenteHasta, ct) is { } invalida) return invalida;
        if (!await catalogo.EspecialidadVigenteAsync(perfil.OrganizacionId, especialidadId, ct))
            return ResultadoOperacion.Invalida("ESPECIALIDAD_INEXISTENTE");

        var habilitacion = Habilitacion.Registrar(profesional.Id, perfil.OrganizacionId, especialidadId, procedimientosPermitidos, vigenteHasta);

        return await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (await habilitaciones.ObtenerAsync(profesional.Id, especialidadId, c) is not null)
                return ResultadoOperacion.Conflicto("HABILITACION_DUPLICADA");
            await habilitaciones.AgregarAsync(habilitacion, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.HabilitacionRegistrada, actor, perfil,
                profesional.UsuarioId, habilitacion.Id, Detalle(habilitacion)), c);
            return ResultadoOperacion.Exito(habilitacion.Id);
        }, "HABILITACION_DUPLICADA", ct);
    }

    /// <summary>Cambia procedimientos y vencimiento (renovación). Sobre una habilitación revocada, la reactiva.</summary>
    public async Task<ResultadoOperacion> ActualizarHabilitacionAsync(
        ActorAdministracion actor, Guid habilitacionId, IReadOnlyCollection<Guid> procedimientosPermitidos,
        DateOnly vigenteHasta, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Actualizar, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var actual = await habilitaciones.ObtenerPorIdAsync(habilitacionId, ct);
        if (actual is null) return ResultadoOperacion.NoEncontrado();
        var profesional = await profesionales.ObtenerPorIdAsync(actual.ProfesionalId, ct);
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, actual.OrganizacionId, profesional?.UsuarioId, ct) is { } ajena) return ajena;

        if (await Validar(actor, perfil.OrganizacionId, procedimientosPermitidos, vigenteHasta, ct) is { } invalida) return invalida;

        var reactivada = actual.Estado == EstadoHabilitacion.Revocada;
        var nueva = actual.Actualizar(procedimientosPermitidos, vigenteHasta);
        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await habilitaciones.GuardarAsync(nueva, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.HabilitacionActualizada, actor, perfil,
                profesional?.UsuarioId, nueva.Id, reactivada ? $"{Detalle(nueva)};reactivada=true" : Detalle(nueva)), c);
        }, ct);
        return ResultadoOperacion.Exito(nueva.Id);
    }

    /// <summary>Revoca la habilitación sin borrarla (RN-006). Repetir la operación no cambia nada ni duplica el evento.</summary>
    public async Task<ResultadoOperacion> RevocarHabilitacionAsync(ActorAdministracion actor, Guid habilitacionId, CancellationToken ct = default)
    {
        var acceso = await guardia.AutorizarAsync(actor, Accion.Anular, ct);
        if (acceso.Denegacion is { } denegada) return denegada;
        var perfil = acceso.Perfil!;

        var actual = await habilitaciones.ObtenerPorIdAsync(habilitacionId, ct);
        if (actual is null) return ResultadoOperacion.NoEncontrado();
        var profesional = await profesionales.ObtenerPorIdAsync(actual.ProfesionalId, ct);
        if (await guardia.VerificarOrganizacionAsync(actor, perfil, actual.OrganizacionId, profesional?.UsuarioId, ct) is { } ajena) return ajena;
        if (actual.Estado == EstadoHabilitacion.Revocada) return ResultadoOperacion.Exito(actual.Id);

        var revocada = actual.Revocar();
        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await habilitaciones.GuardarAsync(revocada, c);
            await auditoria.RegistrarAsync(Evento(TipoEventoAdministracion.HabilitacionRevocada, actor, perfil,
                profesional?.UsuarioId, revocada.Id, $"especialidad={revocada.EspecialidadId}"), c);
        }, ct);
        return ResultadoOperacion.Exito(revocada.Id);
    }

    /// <summary>El vencimiento no puede ser anterior a hoy en la zona horaria de la sede activa, y los procedimientos deben estar en el catálogo.</summary>
    private async Task<ResultadoOperacion?> Validar(
        ActorAdministracion actor, Guid organizacionId, IReadOnlyCollection<Guid> procedimientos, DateOnly vigenteHasta, CancellationToken ct)
    {
        if (procedimientos is null) return ResultadoOperacion.Invalida("PROCEDIMIENTOS_INVALIDOS");
        var zona = await zonaHoraria.ObtenerAsync(actor.SedeActivaId, ct);
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.Ahora, zona).DateTime);
        if (vigenteHasta < hoy) return ResultadoOperacion.Invalida("VENCIMIENTO_PASADO");
        if (!await catalogo.ProcedimientosVigentesAsync(organizacionId, procedimientos, ct))
            return ResultadoOperacion.Invalida("PROCEDIMIENTO_INEXISTENTE");
        return null;
    }

    private static string Detalle(Habilitacion h) => $"especialidad={h.EspecialidadId};vigente_hasta={h.VigenteHasta:yyyy-MM-dd}";

    private EventoAdministracion Evento(TipoEventoAdministracion tipo, ActorAdministracion actor, PerfilActor perfil,
        Guid? usuarioObjetivoId, Guid recursoId, string? detalle = null) =>
        new(tipo, reloj.Ahora, actor.UsuarioId, perfil.OrganizacionId, actor.SedeActivaId, usuarioObjetivoId, recursoId, detalle);
}
