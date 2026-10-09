using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Administracion;

public class ServicioHabilitacionesTests
{
    private static readonly Guid Org = EntornoAdministracion.Org, Sede = EntornoAdministracion.Sede;
    private static readonly Guid Cardiologia = EntornoAdministracion.Cardiologia, Psicologia = EntornoAdministracion.Psicologia;
    private static readonly Guid Electro = EntornoAdministracion.Electrocardiograma, Eco = EntornoAdministracion.Ecocardiograma;
    private static readonly DateOnly Hoy = new(2026, 10, 9), Fin2027 = new(2027, 12, 31);
    private readonly EntornoAdministracion _e = new();

    // ---- RF-ROL-003: ficha de profesional ----

    [Fact]
    public async Task RF_ROL_003_registra_al_profesional_con_licencia_y_exequatur()
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(u.Id, "Ana", "Pérez", "Médico", Licencia: "LIC-0001", Exequatur: "EXQ-0001"));

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var p = await _e.Profesionales.ObtenerPorIdAsync(r.Id!.Value);
        Assert.NotNull(p);
        Assert.Equal(u.Id, p.UsuarioId);
        Assert.Equal(Org, p.OrganizacionId);
        Assert.Equal("LIC-0001", p.Licencia);
        Assert.Equal("EXQ-0001", p.Exequatur);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", "")]
    public async Task RF_ROL_003_exige_licencia_o_exequatur(string? licencia, string? exequatur)
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(u.Id, "Ana", "Pérez", "Médico", licencia, exequatur));

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("SIN_LICENCIA_NI_EXEQUATUR", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_basta_con_el_exequatur()
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(u.Id, "Ana", "Pérez", "Médico", Exequatur: "EXQ-0001"));

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
    }

    [Fact]
    public async Task RF_ROL_003_exige_nombres_y_apellidos()
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(u.Id, " ", "Pérez", "Médico", Licencia: "LIC-0001"));

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("NOMBRE_REQUERIDO", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_un_usuario_tiene_a_lo_sumo_una_ficha_de_profesional()
    {
        await _e.SembrarProfesionalAsync();
        var existente = (await _e.Usuarios.ObtenerPorCorreoAsync("dra.perez@clinica.test"))!;

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(existente.Id, "Ana", "Pérez", "Médico", Licencia: "LIC-0002"));

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("PROFESIONAL_YA_REGISTRADO", r.Codigo);
    }

    [Fact]
    public async Task RN_015_no_se_registra_como_profesional_a_un_usuario_de_otra_organizacion()
    {
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(ajeno.Id, "Luis", "Gómez", "Médico", Licencia: "LIC-9"));

        Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado);
        Assert.Null(await _e.Profesionales.ObtenerPorUsuarioAsync(ajeno.Id));
    }

    // ---- RF-ROL-003: habilitaciones ----

    [Fact]
    public async Task RF_ROL_003_registra_la_habilitacion_con_especialidad_procedimientos_y_vencimiento()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, Cardiologia, [Electro, Eco], Fin2027);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var hecho = await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia);
        Assert.NotNull(hecho);
        Assert.Equal(Cardiologia, hecho.EspecialidadId);
        Assert.Equal(Fin2027, hecho.VigenteHasta);
        Assert.Equal(new[] { Electro, Eco }.Order(), hecho.ProcedimientosPermitidos.Order());
    }

    [Fact]
    public async Task RF_ROL_003_el_puerto_no_devuelve_habilitaciones_de_otra_especialidad()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);

        Assert.Null(await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Psicologia));
        Assert.Null(await _e.ConsultaHabilitaciones.ObtenerAsync(Guid.NewGuid(), Cardiologia));
    }

    [Fact]
    public async Task RF_ROL_003_una_especialidad_ajena_al_catalogo_de_la_organizacion_es_invalida()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, Guid.NewGuid(), [], Fin2027);

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("ESPECIALIDAD_INEXISTENTE", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_un_procedimiento_ajeno_al_catalogo_de_la_organizacion_es_invalido()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, Cardiologia, [Electro, Guid.NewGuid()], Fin2027);

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("PROCEDIMIENTO_INEXISTENTE", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_no_se_registra_una_habilitacion_ya_vencida()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, Cardiologia, [], Hoy.AddDays(-1));

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("VENCIMIENTO_PASADO", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_la_fecha_de_hoy_se_evalua_en_la_zona_horaria_de_la_sede_no_en_utc()
    {
        // 02:00 UTC del 10 de octubre son las 22:00 del 9 en Santo Domingo: «hoy» sigue siendo el 9.
        _e.Reloj.Ahora = new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Hoy);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
    }

    [Fact]
    public async Task RF_ROL_003_una_especialidad_se_habilita_una_sola_vez_por_profesional()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [Electro], Fin2027);

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("HABILITACION_DUPLICADA", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_habilitar_a_un_profesional_inexistente_o_de_otra_organizacion_es_no_encontrado()
    {
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);
        var fichaAjena = Profesional.Crear(ajeno.Id, EntornoAdministracion.OtraOrg, "Luis", "Gómez", "Médico", "LIC-9", null);
        _e.Profesionales.Sembrar(fichaAjena);

        var inexistente = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, Guid.NewGuid(), Cardiologia, [], Fin2027);
        var deOtraOrg = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, fichaAjena.Id, Cardiologia, [], Fin2027);

        Assert.All([inexistente, deOtraOrg], r => Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado));
        Assert.Null(await _e.Habilitaciones.ObtenerAsync(fichaAjena.Id, Cardiologia));
    }

    [Fact]
    public async Task RF_ROL_003_actualizar_cambia_procedimientos_y_vencimiento()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [Electro], Hoy.AddDays(10));

        var r = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [Eco], Fin2027);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var hecho = await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia);
        Assert.Equal(Fin2027, hecho!.VigenteHasta);
        Assert.Equal([Eco], hecho.ProcedimientosPermitidos);
    }

    [Fact]
    public async Task RF_ROL_003_actualizar_valida_el_catalogo_y_la_fecha()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);

        var procedimiento = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [Guid.NewGuid()], Fin2027);
        var fecha = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [], Hoy.AddDays(-1));

        Assert.Equal("PROCEDIMIENTO_INEXISTENTE", procedimiento.Codigo);
        Assert.Equal("VENCIMIENTO_PASADO", fecha.Codigo);
        Assert.Equal(Fin2027, (await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia))!.VigenteHasta);
    }

    [Fact]
    public async Task RF_ROL_003_revocar_conserva_el_registro_y_el_puerto_deja_de_devolverla()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);

        var r = await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id!.Value);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        Assert.Null(await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia));
        var registro = await _e.Habilitaciones.ObtenerPorIdAsync(hab.Id.Value);
        Assert.NotNull(registro);
        Assert.Equal(EstadoHabilitacion.Revocada, registro.Estado);
    }

    [Fact]
    public async Task RF_ROL_003_actualizar_una_habilitacion_revocada_la_reactiva()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);
        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id!.Value);

        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id.Value, [Electro], Fin2027);

        Assert.NotNull(await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia));
    }

    [Fact]
    public async Task RF_ROL_003_revocar_dos_veces_es_idempotente_y_audita_una_vez()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);

        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id!.Value);
        var segunda = await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id.Value);

        Assert.Equal(EstadoOperacion.Exitosa, segunda.Estado);
        Assert.Single(_e.Auditoria.Eventos, e => e.Tipo == TipoEventoAdministracion.HabilitacionRevocada);
    }

    [Fact]
    public async Task RN_015_una_habilitacion_de_otra_organizacion_responde_no_encontrado()
    {
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);
        var ficha = Profesional.Crear(ajeno.Id, EntornoAdministracion.OtraOrg, "Luis", "Gómez", "Médico", "LIC-9", null);
        _e.Profesionales.Sembrar(ficha);
        var habAjena = Habilitacion.Registrar(ficha.Id, EntornoAdministracion.OtraOrg, Cardiologia, [], Fin2027);
        _e.Habilitaciones.Sembrar(habAjena);

        var actualizar = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, habAjena.Id, [], Fin2027.AddYears(1));
        var revocar = await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, habAjena.Id);

        Assert.All([actualizar, revocar], r => Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado));
        Assert.Equal(EstadoHabilitacion.Vigente, (await _e.Habilitaciones.ObtenerPorIdAsync(habAjena.Id))!.Estado);
    }

    // ---- Solo ADMIN_FUNCIONAL ----

    [Theory]
    [MemberData(nameof(ServicioUsuariosTests.RolesQueNoSonAdministrador), MemberType = typeof(ServicioUsuariosTests))]
    public async Task RF_ROL_003_solo_admin_funcional_gestiona_profesionales_y_habilitaciones(Rol rolDelActor)
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027);
        var otro = _e.SembrarUsuario("otro.medico@clinica.test");
        var actor = _e.ActorConRol(rolDelActor);

        var ficha = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(actor, new(otro.Id, "Luis", "Gómez", "Médico", Licencia: "L-1"));
        var nueva = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(actor, profesionalId, Psicologia, [], Fin2027);
        var actualizar = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(actor, hab.Id!.Value, [], Fin2027.AddYears(1));
        var revocar = await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(actor, hab.Id.Value);

        Assert.All([ficha, nueva, actualizar, revocar], r => Assert.Equal(EstadoOperacion.Prohibido, r.Estado));
        Assert.Null(await _e.Profesionales.ObtenerPorUsuarioAsync(otro.Id));
        Assert.Null(await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Psicologia));
        Assert.Equal(Fin2027, (await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia))!.VigenteHasta);
    }

    // ---- RF-ROL-009 ----

    [Fact]
    public async Task RF_ROL_009_cada_cambio_de_credencial_genera_un_evento_con_ids_y_fechas_nunca_datos_personales()
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");
        var ficha = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(u.Id, "Ana", "Pérez", "Médico", Licencia: "LIC-SECRETA-1", Exequatur: "EXQ-SECRETO-1"));
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, ficha.Id!.Value, Cardiologia, [Electro], Fin2027);
        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [Eco], Fin2027.AddYears(1));
        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id.Value);

        Assert.Equal(
        [
            TipoEventoAdministracion.ProfesionalRegistrado,
            TipoEventoAdministracion.HabilitacionRegistrada,
            TipoEventoAdministracion.HabilitacionActualizada,
            TipoEventoAdministracion.HabilitacionRevocada,
        ], _e.Auditoria.Eventos.Select(e => e.Tipo));
        Assert.All(_e.Auditoria.Eventos, e =>
        {
            Assert.Equal(_e.Administrador.Id, e.ActorId);
            Assert.Equal(u.Id, e.UsuarioObjetivoId);
            Assert.Equal(Org, e.OrganizacionId);
            Assert.Equal(NivelAuditoria.Advertencia, e.Nivel);
            Assert.DoesNotContain("LIC-", e.Detalle ?? "");
            Assert.DoesNotContain("EXQ-", e.Detalle ?? "");
            Assert.DoesNotContain("Pérez", e.Detalle ?? "");
        });
        Assert.Equal($"especialidad={Cardiologia};vigente_hasta=2027-12-31", _e.Auditoria.Eventos[1].Detalle);
        Assert.Equal($"especialidad={Cardiologia};vigente_hasta=2028-12-31", _e.Auditoria.Eventos[2].Detalle);
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_la_habilitacion_no_se_guarda()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], Fin2027));

        Assert.Null(await _e.Habilitaciones.ObtenerAsync(profesionalId, Cardiologia));
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_la_actualizacion_y_la_revocacion_se_revierten()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [Electro], Fin2027);
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [Eco], Fin2027.AddYears(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id!.Value));

        var vigente = await _e.ConsultaHabilitaciones.ObtenerAsync(profesionalId, Cardiologia);
        Assert.NotNull(vigente);
        Assert.Equal(Fin2027, vigente.VigenteHasta);
        Assert.Equal([Electro], vigente.ProcedimientosPermitidos);
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_el_alta_del_profesional_se_revierte()
    {
        var u = _e.SembrarUsuario("dra.perez@clinica.test");
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _e.ServicioHabilitaciones.RegistrarProfesionalAsync(_e.Actor, new(u.Id, "Ana", "Pérez", "Médico", Licencia: "L-1")));

        Assert.Null(await _e.Profesionales.ObtenerPorUsuarioAsync(u.Id));
    }

    // ---- RF-ROL-004 / CA-ROL-003: la habilitación vencida bloquea escrituras clínicas ----

    private async Task<Decision> FirmarNotaAsync(Guid usuarioId)
    {
        var perfil = (await _e.ConsultaRoles.ObtenerPerfilAsync(usuarioId, Sede))!;
        var habilitacion = await _e.ConsultaHabilitaciones.ObtenerAsync(perfil.ProfesionalId!.Value, Cardiologia);
        var hechos = new HechosAcceso(perfil, Sede, null,
            new RelacionClinica(TipoRelacion.Tratante, PermiteSensible: false, EpisodioSensible: false, Cardiologia),
            habilitacion, _e.HoyEnSede);
        var contexto = new ContextoRecurso(Org, PacienteId: Guid.NewGuid(), EpisodioId: Guid.NewGuid());
        return PoliticaAcceso.Evaluar(Accion.Crear, TipoRecurso.NotaClinica, contexto, hechos);
    }

    private async Task<Guid> ProfesionalConHabilitacionHastaAsync(DateOnly vigenteHasta)
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var usuarioId = (await _e.Profesionales.ObtenerPorIdAsync(profesionalId))!.UsuarioId;
        await _e.ServicioRoles.AsignarAsync(_e.Actor, usuarioId, Rol.Profesional, Sede);
        await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], vigenteHasta);
        return usuarioId;
    }

    [Fact]
    public async Task CA_ROL_003_licencia_vencida_ayer_impide_firmar_nota_con_motivo()
    {
        var usuarioId = await ProfesionalConHabilitacionHastaAsync(Hoy);
        _e.Reloj.Avanzar(TimeSpan.FromDays(1));   // la habilitación venció ayer

        var decision = await FirmarNotaAsync(usuarioId);

        Assert.False(decision.EstaPermitido);
        Assert.Equal(MotivoDenegacion.HabilitacionVencida, decision.Motivo);
        Assert.Equal("HABILITACION_VENCIDA", decision.Motivo!.Value.Codigo());
    }

    [Fact]
    public async Task CA_ROL_003_licencia_que_vence_hoy_sigue_vigente_hoy()
    {
        var usuarioId = await ProfesionalConHabilitacionHastaAsync(Hoy);

        var decision = await FirmarNotaAsync(usuarioId);

        Assert.True(decision.EstaPermitido);
    }

    [Fact]
    public async Task RF_ROL_004_renovar_la_habilitacion_restablece_la_escritura_clinica()
    {
        var usuarioId = await ProfesionalConHabilitacionHastaAsync(Hoy);
        _e.Reloj.Avanzar(TimeSpan.FromDays(1));
        Assert.False((await FirmarNotaAsync(usuarioId)).EstaPermitido);

        var profesional = (await _e.Profesionales.ObtenerPorUsuarioAsync(usuarioId))!;
        var hab = (await _e.Habilitaciones.ObtenerAsync(profesional.Id, Cardiologia))!;
        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id, [], Fin2027);

        Assert.True((await FirmarNotaAsync(usuarioId)).EstaPermitido);
    }

    [Fact]
    public async Task RF_ROL_004_una_habilitacion_revocada_impide_escribir_con_el_motivo_sin_habilitacion()
    {
        var usuarioId = await ProfesionalConHabilitacionHastaAsync(Fin2027);
        var profesional = (await _e.Profesionales.ObtenerPorUsuarioAsync(usuarioId))!;
        var hab = (await _e.Habilitaciones.ObtenerAsync(profesional.Id, Cardiologia))!;

        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id);

        var decision = await FirmarNotaAsync(usuarioId);
        Assert.False(decision.EstaPermitido);
        Assert.Equal(MotivoDenegacion.SinHabilitacion, decision.Motivo);
    }
}
