using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Administracion;

public class ServicioUsuariosTests
{
    private readonly EntornoAdministracion _e = new();

    public static TheoryData<Rol> RolesQueNoSonAdministrador()
    {
        var datos = new TheoryData<Rol>();
        foreach (var rol in Enum.GetValues<Rol>().Where(r => r != Rol.AdminFuncional)) datos.Add(rol);
        return datos;
    }

    // ---- RF-ROL-001: crear ----

    [Fact]
    public async Task RF_ROL_001_admin_crea_usuario_en_su_organizacion_activo_y_con_mfa_requerido()
    {
        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "  Nuevo.Usuario@Clinica.TEST ");

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var usuario = await _e.Usuarios.ObtenerPorIdAsync(r.Id!.Value);
        Assert.NotNull(usuario);
        Assert.Equal("nuevo.usuario@clinica.test", usuario.Correo);
        Assert.Equal(EntornoAdministracion.Org, usuario.OrganizacionId);
        Assert.Equal(EstadoUsuario.Activo, usuario.Estado);
        Assert.True(usuario.RequiereMfa);
        Assert.False(usuario.MfaHabilitado);
    }

    [Fact]
    public async Task RF_ROL_001_la_cuenta_nueva_no_tiene_una_contrasena_conocida_ni_igual_a_otra_cuenta()
    {
        var a = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "a@clinica.test");
        var b = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "b@clinica.test");

        var ua = await _e.Usuarios.ObtenerPorIdAsync(a.Id!.Value);
        var ub = await _e.Usuarios.ObtenerPorIdAsync(b.Id!.Value);
        Assert.NotEqual(ua!.HashContrasena, ub!.HashContrasena);
        Assert.False(new HasheadorFalso().Verificar(ua.HashContrasena, ""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("@sin-usuario.test")]
    [InlineData("dos@@arrobas.test")]
    public async Task RF_ROL_001_correo_invalido_se_rechaza_y_no_deja_rastro(string correo)
    {
        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, correo);

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("CORREO_INVALIDO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_001_correo_duplicado_devuelve_conflicto_sin_auditar()
    {
        await _e.ServicioUsuarios.CrearAsync(_e.Actor, "repetido@clinica.test");
        _e.Auditoria.Eventos.Clear();

        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "REPETIDO@clinica.test");

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("CORREO_DUPLICADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    // ---- RF-ROL-001: editar ----

    [Fact]
    public async Task RF_ROL_001_admin_edita_el_correo_de_un_usuario_de_su_organizacion()
    {
        var u = _e.SembrarUsuario("viejo@clinica.test");

        var r = await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, u.Id, "Nuevo@Clinica.test");

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        Assert.Equal("nuevo@clinica.test", (await _e.Usuarios.ObtenerPorIdAsync(u.Id))!.Correo);
    }

    [Fact]
    public async Task RF_ROL_001_editar_el_correo_a_uno_ya_usado_devuelve_conflicto()
    {
        var u = _e.SembrarUsuario("uno@clinica.test");
        _e.SembrarUsuario("dos@clinica.test");

        var r = await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, u.Id, "dos@clinica.test");

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("uno@clinica.test", u.Correo);
    }

    [Fact]
    public async Task RF_ROL_001_editar_con_el_mismo_correo_no_cambia_nada_ni_audita()
    {
        var u = _e.SembrarUsuario("igual@clinica.test");

        var r = await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, u.Id, "IGUAL@clinica.test");

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    // ---- RF-ROL-001: desactivar, nunca eliminar ----

    [Fact]
    public async Task RF_ROL_001_desactivar_conserva_la_cuenta_y_la_marca_inactiva()
    {
        var u = _e.SembrarUsuario("baja@clinica.test");

        var r = await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, u.Id);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var guardado = await _e.Usuarios.ObtenerPorIdAsync(u.Id);
        Assert.NotNull(guardado);
        Assert.Equal(EstadoUsuario.Inactivo, guardado.Estado);
    }

    [Fact]
    public async Task RF_ROL_001_desactivar_cierra_todas_las_sesiones_abiertas_del_usuario()
    {
        var u = _e.SembrarUsuario("baja@clinica.test");
        _e.Sesiones.Todas.Add(Sesion.Iniciar(u, "token-a", _e.Reloj.Ahora));
        _e.Sesiones.Todas.Add(Sesion.Iniciar(u, "token-b", _e.Reloj.Ahora));

        await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, u.Id);

        Assert.All(_e.Sesiones.Todas, s => Assert.NotNull(s.RevocadaEn));
    }

    [Fact]
    public async Task RF_ROL_001_desactivar_dos_veces_es_idempotente_y_audita_una_sola_vez()
    {
        var u = _e.SembrarUsuario("baja@clinica.test");

        await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, u.Id);
        var segunda = await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, u.Id);

        Assert.Equal(EstadoOperacion.Exitosa, segunda.Estado);
        Assert.Single(_e.Auditoria.Eventos, e => e.Tipo == TipoEventoAdministracion.UsuarioDesactivado);
    }

    [Fact]
    public async Task RF_ROL_001_usuario_inexistente_devuelve_no_encontrado()
    {
        var r = await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, Guid.NewGuid());

        Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado);
    }

    // ---- Solo ADMIN_FUNCIONAL ----

    [Theory]
    [MemberData(nameof(RolesQueNoSonAdministrador))]
    public async Task RF_ROL_001_solo_admin_funcional_crea_edita_y_desactiva_usuarios(Rol rol)
    {
        var actor = _e.ActorConRol(rol);
        var objetivo = _e.SembrarUsuario("objetivo@clinica.test");

        var crear = await _e.ServicioUsuarios.CrearAsync(actor, "nuevo@clinica.test");
        var editar = await _e.ServicioUsuarios.EditarCorreoAsync(actor, objetivo.Id, "otro@clinica.test");
        var desactivar = await _e.ServicioUsuarios.DesactivarAsync(actor, objetivo.Id);

        Assert.All([crear, editar, desactivar], r => Assert.Equal(EstadoOperacion.Prohibido, r.Estado));
        Assert.Null(await _e.Usuarios.ObtenerPorCorreoAsync("nuevo@clinica.test"));
        Assert.Equal("objetivo@clinica.test", objetivo.Correo);
        Assert.Equal(EstadoUsuario.Activo, objetivo.Estado);
    }

    [Fact]
    public async Task RF_ROL_001_cada_denegacion_queda_como_evento_critico_con_el_motivo()
    {
        var actor = _e.ActorConRol(Rol.Recepcion);

        await _e.ServicioUsuarios.CrearAsync(actor, "nuevo@clinica.test");

        var evento = Assert.Single(_e.Auditoria.Eventos);
        Assert.Equal(TipoEventoAdministracion.AccesoAdministracionDenegado, evento.Tipo);
        Assert.Equal(NivelAuditoria.Critico, evento.Nivel);
        Assert.Equal("SIN_PERMISO_DE_ROL", evento.Detalle);
        Assert.Equal(actor.UsuarioId, evento.ActorId);
        Assert.Equal(EntornoAdministracion.Org, evento.OrganizacionId);
    }

    [Fact]
    public async Task RF_ROL_001_un_administrador_inactivo_no_puede_operar()
    {
        var admin = _e.Administrador;
        admin.Desactivar();

        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "nuevo@clinica.test");

        Assert.Equal(EstadoOperacion.Prohibido, r.Estado);
        Assert.Equal("SIN_ROL_EN_SEDE", Assert.Single(_e.Auditoria.Eventos).Detalle);
    }

    [Fact]
    public async Task RF_ROL_001_el_rol_de_administrador_en_otra_sede_no_vale_para_la_sede_activa()
    {
        var actor = _e.Actor with { SedeActivaId = EntornoAdministracion.SedeNorte };

        var r = await _e.ServicioUsuarios.CrearAsync(actor, "nuevo@clinica.test");

        Assert.Equal(EstadoOperacion.Prohibido, r.Estado);
    }

    // ---- RN-015: aislamiento por organización ----

    [Fact]
    public async Task RN_015_un_usuario_de_otra_organizacion_responde_no_encontrado_y_se_audita_como_critico()
    {
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);

        var editar = await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, ajeno.Id, "robado@clinica.test");
        var desactivar = await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, ajeno.Id);

        Assert.Equal(EstadoOperacion.NoEncontrado, editar.Estado);
        Assert.Equal(EstadoOperacion.NoEncontrado, desactivar.Estado);
        Assert.Equal(EstadoUsuario.Activo, ajeno.Estado);
        Assert.Equal("ajeno@otra.test", ajeno.Correo);
        Assert.All(_e.Auditoria.Eventos, ev =>
        {
            Assert.Equal(TipoEventoAdministracion.AccesoAdministracionDenegado, ev.Tipo);
            Assert.Equal("OTRA_ORGANIZACION", ev.Detalle);
            Assert.Equal(NivelAuditoria.Critico, ev.Nivel);
        });
        Assert.Equal(2, _e.Auditoria.Eventos.Count);
    }

    [Fact]
    public async Task RN_015_el_usuario_nuevo_nace_en_la_organizacion_del_administrador_no_en_la_de_un_parametro()
    {
        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "nuevo@clinica.test");

        Assert.Equal(EntornoAdministracion.Org, (await _e.Usuarios.ObtenerPorIdAsync(r.Id!.Value))!.OrganizacionId);
    }

    // ---- RF-ROL-009: bitácora ----

    [Fact]
    public async Task RF_ROL_009_crear_editar_y_desactivar_generan_un_evento_cada_uno_sin_datos_personales()
    {
        var creado = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "ficticio@clinica.test");
        var id = creado.Id!.Value;
        await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, id, "ficticio2@clinica.test");
        await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, id);

        Assert.Equal(
            [TipoEventoAdministracion.UsuarioCreado, TipoEventoAdministracion.UsuarioEditado, TipoEventoAdministracion.UsuarioDesactivado],
            _e.Auditoria.Eventos.Select(e => e.Tipo));
        Assert.All(_e.Auditoria.Eventos, e =>
        {
            Assert.Equal(_e.Administrador.Id, e.ActorId);
            Assert.Equal(EntornoAdministracion.Org, e.OrganizacionId);
            Assert.Equal(EntornoAdministracion.Sede, e.SedeId);
            Assert.Equal(id, e.UsuarioObjetivoId);
            Assert.Equal(_e.Reloj.Ahora, e.OcurridoEn);
            Assert.Equal(NivelAuditoria.Advertencia, e.Nivel);
            Assert.DoesNotContain("ficticio", e.Detalle ?? "");
        });
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_la_creacion_se_revierte_y_el_error_se_propaga()
    {
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _e.ServicioUsuarios.CrearAsync(_e.Actor, "nuevo@clinica.test"));

        Assert.Null(await _e.Usuarios.ObtenerPorCorreoAsync("nuevo@clinica.test"));
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_al_desactivar_el_error_se_propaga()
    {
        var u = _e.SembrarUsuario("baja@clinica.test");
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _e.ServicioUsuarios.DesactivarAsync(_e.Actor, u.Id));
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_al_denegar_el_error_se_propaga_y_nunca_se_concede()
    {
        var actor = _e.ActorConRol(Rol.Recepcion);
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _e.ServicioUsuarios.CrearAsync(actor, "nuevo@clinica.test"));

        Assert.Null(await _e.Usuarios.ObtenerPorCorreoAsync("nuevo@clinica.test"));
    }
}
