using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Administracion;

public class ServicioRolesTests
{
    private static readonly Guid Sede = EntornoAdministracion.Sede, SedeNorte = EntornoAdministracion.SedeNorte;
    private readonly EntornoAdministracion _e = new();

    // ---- RF-ROL-002: varios roles, por sede ----

    [Fact]
    public async Task RF_ROL_002_un_usuario_puede_tener_varios_roles_en_una_misma_sede()
    {
        var u = _e.SembrarUsuario("medico.director@clinica.test");

        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Profesional, Sede);
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Director, Sede);

        var perfil = await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, Sede);
        Assert.NotNull(perfil);
        Assert.Equal([Rol.Profesional, Rol.Director], perfil.Roles.Order());
        Assert.Equal(EntornoAdministracion.Org, perfil.OrganizacionId);
    }

    [Fact]
    public async Task RF_ROL_002_los_roles_se_asignan_por_sede_y_no_se_filtran_a_otra()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");

        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.CoordinadorSede, SedeNorte);

        Assert.Equal([Rol.Recepcion], (await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, Sede))!.Roles);
        Assert.Equal([Rol.CoordinadorSede], (await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, SedeNorte))!.Roles);
    }

    [Fact]
    public async Task RF_ROL_002_un_usuario_sin_rol_en_la_sede_tiene_perfil_con_roles_vacios()
    {
        var u = _e.SembrarUsuario("sinrol@clinica.test");

        var perfil = await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, Sede);

        Assert.NotNull(perfil);
        Assert.Empty(perfil.Roles);
    }

    [Fact]
    public async Task RF_ROL_002_el_perfil_es_nulo_si_el_usuario_no_existe_o_esta_inactivo()
    {
        var u = _e.SembrarUsuario("baja@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);
        u.Desactivar();

        Assert.Null(await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, Sede));
        Assert.Null(await _e.ConsultaRoles.ObtenerPerfilAsync(Guid.NewGuid(), Sede));
    }

    [Fact]
    public async Task RF_ROL_002_el_perfil_trae_el_profesional_id_si_el_usuario_tiene_ficha_de_profesional()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var profesional = await _e.Profesionales.ObtenerPorIdAsync(profesionalId);
        await _e.ServicioRoles.AsignarAsync(_e.Actor, profesional!.UsuarioId, Rol.Profesional, Sede);

        var perfil = await _e.ConsultaRoles.ObtenerPerfilAsync(profesional.UsuarioId, Sede);

        Assert.Equal(profesionalId, perfil!.ProfesionalId);
    }

    [Fact]
    public async Task RF_ROL_002_asignar_un_rol_ya_asignado_devuelve_conflicto_sin_auditar()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);
        _e.Auditoria.Eventos.Clear();

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("ROL_YA_ASIGNADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_002_una_sede_de_otra_organizacion_o_inexistente_es_invalida()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");

        var ajena = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, EntornoAdministracion.SedeAjena);
        var inexistente = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Guid.NewGuid());

        Assert.All([ajena, inexistente], r =>
        {
            Assert.Equal(EstadoOperacion.Invalida, r.Estado);
            Assert.Equal("SEDE_INEXISTENTE", r.Codigo);
        });
        Assert.Empty(await _e.Roles.ListarDeUsuarioAsync(u.Id));
    }

    [Fact]
    public async Task RF_ROL_002_un_rol_fuera_del_catalogo_es_invalido()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, (Rol)99, Sede);

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("ROL_INVALIDO", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_002_retirar_un_rol_lo_quita_solo_de_esa_sede()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, SedeNorte);

        var r = await _e.ServicioRoles.RetirarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        Assert.Empty((await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, Sede))!.Roles);
        Assert.Equal([Rol.Recepcion], (await _e.ConsultaRoles.ObtenerPerfilAsync(u.Id, SedeNorte))!.Roles);
    }

    [Fact]
    public async Task RF_ROL_002_retirar_un_rol_que_no_tiene_devuelve_no_encontrado()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");

        var r = await _e.ServicioRoles.RetirarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);

        Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado);
        Assert.Equal("ROL_NO_ASIGNADO", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_002_asignar_a_un_usuario_inexistente_devuelve_no_encontrado()
    {
        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, Guid.NewGuid(), Rol.Recepcion, Sede);

        Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado);
    }

    // ---- Solo ADMIN_FUNCIONAL, RN-015 ----

    [Theory]
    [MemberData(nameof(ServicioUsuariosTests.RolesQueNoSonAdministrador), MemberType = typeof(ServicioUsuariosTests))]
    public async Task RF_ROL_002_solo_admin_funcional_asigna_y_retira_roles(Rol rolDelActor)
    {
        var actor = _e.ActorConRol(rolDelActor);
        var objetivo = _e.SembrarUsuario("objetivo@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, objetivo.Id, Rol.Recepcion, Sede);

        var asignar = await _e.ServicioRoles.AsignarAsync(actor, objetivo.Id, Rol.AdminFuncional, Sede);
        var retirar = await _e.ServicioRoles.RetirarAsync(actor, objetivo.Id, Rol.Recepcion, Sede);

        Assert.All([asignar, retirar], r => Assert.Equal(EstadoOperacion.Prohibido, r.Estado));
        Assert.Equal([Rol.Recepcion], (await _e.ConsultaRoles.ObtenerPerfilAsync(objetivo.Id, Sede))!.Roles);
    }

    [Fact]
    public async Task RN_015_no_se_pueden_asignar_ni_retirar_roles_de_un_usuario_de_otra_organizacion()
    {
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);
        _e.Roles.Sembrar(new RolAsignado(ajeno.Id, Rol.Recepcion, EntornoAdministracion.SedeAjena));

        var asignar = await _e.ServicioRoles.AsignarAsync(_e.Actor, ajeno.Id, Rol.AdminFuncional, Sede);
        var retirar = await _e.ServicioRoles.RetirarAsync(_e.Actor, ajeno.Id, Rol.Recepcion, EntornoAdministracion.SedeAjena);

        Assert.All([asignar, retirar], r => Assert.Equal(EstadoOperacion.NoEncontrado, r.Estado));
        Assert.Equal([Rol.Recepcion], (await _e.Roles.ListarDeUsuarioAsync(ajeno.Id)).Select(x => x.Rol));
        Assert.All(_e.Auditoria.Eventos, ev => Assert.Equal("OTRA_ORGANIZACION", ev.Detalle));
    }

    // ---- RF-ROL-009 ----

    [Fact]
    public async Task RF_ROL_009_asignar_y_retirar_un_rol_auditan_con_actor_objetivo_rol_y_sede()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");

        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, SedeNorte);
        await _e.ServicioRoles.RetirarAsync(_e.Actor, u.Id, Rol.Recepcion, SedeNorte);

        Assert.Equal([TipoEventoAdministracion.RolAsignado, TipoEventoAdministracion.RolRetirado],
            _e.Auditoria.Eventos.Select(e => e.Tipo));
        Assert.All(_e.Auditoria.Eventos, e =>
        {
            Assert.Equal(_e.Administrador.Id, e.ActorId);
            Assert.Equal(u.Id, e.UsuarioObjetivoId);
            Assert.Equal($"rol=Recepcion;sede={SedeNorte}", e.Detalle);
            Assert.Equal(NivelAuditoria.Advertencia, e.Nivel);
        });
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_la_asignacion_se_revierte()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede));

        Assert.Empty(await _e.Roles.ListarDeUsuarioAsync(u.Id));
    }

    [Fact]
    public async Task RF_ROL_009_si_falla_la_bitacora_el_retiro_se_revierte()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);
        _e.Auditoria.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _e.ServicioRoles.RetirarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede));

        Assert.Equal([Rol.Recepcion], (await _e.Roles.ListarDeUsuarioAsync(u.Id)).Select(x => x.Rol));
    }
}
