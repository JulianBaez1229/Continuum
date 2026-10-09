using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Administracion;

/// <summary>
/// Garantías transversales de los casos de uso de administración: la bitácora va dentro de la transacción
/// (RF-AUD-008), las comprobaciones de unicidad y de último administrador se hacen dentro de ella (sin carreras entre
/// leer y escribir) y los roles de personal exigen MFA (RF-IAM-002).
/// </summary>
public class RevisionAtomicidadYConsistenciaTests
{
    private static readonly Guid Sede = EntornoAdministracion.Sede;
    private readonly EntornoAdministracion _e = new();

    // ---- RF-ROL-009 / RF-AUD-008: el evento se registra dentro de la transacción ----

    [Fact]
    public async Task RF_ROL_009_todos_los_eventos_de_exito_se_registran_dentro_de_la_transaccion()
    {
        var nuevo = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "nuevo@clinica.test");
        await _e.ServicioUsuarios.EditarCorreoAsync(_e.Actor, nuevo.Id!.Value, "otro@clinica.test");
        await _e.ServicioRoles.AsignarAsync(_e.Actor, nuevo.Id.Value, Rol.Profesional, Sede);
        var prof = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(
            _e.Actor, new(nuevo.Id.Value, "Ana", "Pérez", "Médico", Licencia: "LIC-0002"));
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, prof.Id!.Value, EntornoAdministracion.Cardiologia, [EntornoAdministracion.Electrocardiograma],
            _e.HoyEnSede.AddDays(90));
        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(
            _e.Actor, hab.Id!.Value, [EntornoAdministracion.Ecocardiograma], _e.HoyEnSede.AddDays(120));
        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id.Value);
        await _e.ServicioRoles.RetirarAsync(_e.Actor, nuevo.Id.Value, Rol.Profesional, Sede);
        await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, nuevo.Id.Value);

        Assert.Equal(9, _e.Auditoria.Registros.Count);
        Assert.All(_e.Auditoria.Registros, r => Assert.True(r.Dentro, $"{r.Evento.Tipo} se registró fuera de la transacción"));
    }

    [Fact]
    public async Task RF_ROL_009_la_denegacion_se_audita_aunque_no_haya_transaccion()
    {
        var recepcion = _e.ActorConRol(Rol.Recepcion);

        var r = await _e.ServicioUsuarios.CrearAsync(recepcion, "x@clinica.test");

        Assert.Equal(EstadoOperacion.Prohibido, r.Estado);
        var (evento, dentro) = Assert.Single(_e.Auditoria.Registros);
        Assert.Equal(TipoEventoAdministracion.AccesoAdministracionDenegado, evento.Tipo);
        Assert.False(dentro);
    }

    // ---- RF-ROL-001: sin carreras entre leer y escribir ----

    [Fact]
    public async Task RF_ROL_001_el_ultimo_administrador_se_comprueba_dentro_de_la_transaccion()
    {
        var otro = _e.SembrarUsuario("admin2@clinica.test");
        _e.Roles.Sembrar(new RolAsignado(otro.Id, Rol.AdminFuncional, EntornoAdministracion.SedeNorte));
        // Otra transacción desactiva al primer administrador justo antes de que corra esta.
        _e.UnidadDeTrabajo.AntesDelTrabajo = () => _e.Administrador.Desactivar();

        var r = await _e.ServicioUsuarios.DesactivarAsync(_e.Actor, otro.Id);

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("ULTIMO_ADMINISTRADOR", r.Codigo);
        Assert.Equal(EstadoUsuario.Activo, otro.Estado);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_001_retirar_el_rol_al_ultimo_administrador_se_comprueba_dentro_de_la_transaccion()
    {
        var otro = _e.SembrarUsuario("admin2@clinica.test");
        var asignacion = new RolAsignado(otro.Id, Rol.AdminFuncional, EntornoAdministracion.SedeNorte);
        _e.Roles.Sembrar(asignacion);
        _e.UnidadDeTrabajo.AntesDelTrabajo = () => _e.Administrador.Desactivar();

        var r = await _e.ServicioRoles.RetirarAsync(_e.Actor, otro.Id, Rol.AdminFuncional, EntornoAdministracion.SedeNorte);

        Assert.Equal("ULTIMO_ADMINISTRADOR", r.Codigo);
        Assert.Contains(asignacion, await _e.Roles.ListarDeUsuarioAsync(otro.Id));
    }

    [Fact]
    public async Task RF_ROL_001_un_correo_tomado_justo_antes_de_escribir_devuelve_conflicto_sin_auditar()
    {
        _e.UnidadDeTrabajo.AntesDelTrabajo = () => _e.SembrarUsuario("carrera@clinica.test");

        var r = await _e.ServicioUsuarios.CrearAsync(_e.Actor, "carrera@clinica.test");

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("CORREO_DUPLICADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_001_una_violacion_de_unicidad_del_almacen_se_traduce_a_conflicto_y_revierte()
    {
        // El repositorio no ve el duplicado al comprobar, pero la restricción única de la base lo rechaza al escribir.
        var servicio = new ServicioUsuarios(_e.Guardia, new UsuariosConRestriccionUnica(_e.Usuarios), _e.Roles,
            new HasheadorFalso(), _e.ServicioSesiones, _e.UnidadDeTrabajo, _e.Auditoria, _e.Reloj);

        var r = await servicio.CrearAsync(_e.Actor, "nuevo@clinica.test");

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("CORREO_DUPLICADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_002_una_asignacion_repetida_por_carrera_devuelve_conflicto()
    {
        var u = _e.SembrarUsuario("recepcion@clinica.test");
        _e.UnidadDeTrabajo.AntesDelTrabajo = () => _e.Roles.Sembrar(new RolAsignado(u.Id, Rol.Recepcion, Sede));

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Recepcion, Sede);

        Assert.Equal("ROL_YA_ASIGNADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_003_un_profesional_registrado_por_carrera_devuelve_conflicto()
    {
        var u = _e.SembrarUsuario("dr@clinica.test");
        _e.UnidadDeTrabajo.AntesDelTrabajo = () =>
            _e.Profesionales.Sembrar(Profesional.Crear(u.Id, EntornoAdministracion.Org, "Luis", "Gómez", "Médico", "LIC-9", null));

        var r = await _e.ServicioHabilitaciones.RegistrarProfesionalAsync(_e.Actor, new(u.Id, "Luis", "Gómez", "Médico", Licencia: "LIC-9"));

        Assert.Equal("PROFESIONAL_YA_REGISTRADO", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_003_una_habilitacion_duplicada_por_carrera_devuelve_conflicto()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        _e.UnidadDeTrabajo.AntesDelTrabajo = () => _e.Habilitaciones.Sembrar(Habilitacion.Registrar(
            profesionalId, EntornoAdministracion.Org, EntornoAdministracion.Cardiologia, [], _e.HoyEnSede.AddDays(10)));

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, EntornoAdministracion.Cardiologia, [], _e.HoyEnSede.AddDays(90));

        Assert.Equal("HABILITACION_DUPLICADA", r.Codigo);
        Assert.Empty(_e.Auditoria.Eventos);
    }

    // ---- RF-IAM-002: roles de personal exigen MFA ----

    [Theory]
    [InlineData(Rol.Profesional)]
    [InlineData(Rol.Recepcion)]
    [InlineData(Rol.AdminFuncional)]
    [InlineData(Rol.Director)]
    public async Task RF_IAM_002_un_rol_de_personal_no_se_asigna_a_una_cuenta_sin_mfa(Rol rol)
    {
        var sinMfa = Usuario.Crear(EntornoAdministracion.Org, "paciente@clinica.test", "hash", requiereMfa: false);
        _e.Usuarios.Sembrar(sinMfa);

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, sinMfa.Id, rol, Sede);

        Assert.Equal(EstadoOperacion.Conflicto, r.Estado);
        Assert.Equal("CUENTA_SIN_MFA_OBLIGATORIO", r.Codigo);
        Assert.Empty(await _e.Roles.ListarDeUsuarioAsync(sinMfa.Id));
        Assert.Empty(_e.Auditoria.Eventos);
    }

    [Theory]
    [InlineData(Rol.Paciente)]
    [InlineData(Rol.RedApoyo)]
    public async Task RF_IAM_002_paciente_y_red_de_apoyo_se_asignan_a_cuentas_sin_mfa(Rol rol)
    {
        var sinMfa = Usuario.Crear(EntornoAdministracion.Org, "paciente@clinica.test", "hash", requiereMfa: false);
        _e.Usuarios.Sembrar(sinMfa);

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, sinMfa.Id, rol, Sede);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_002_un_rol_de_personal_se_asigna_a_una_cuenta_con_mfa()
    {
        var u = _e.SembrarUsuario("personal@clinica.test");

        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, u.Id, Rol.Profesional, Sede);

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
    }

    [Fact]
    public async Task RF_ROL_002_un_rol_inexistente_se_rechaza_antes_de_buscar_el_usuario()
    {
        var r = await _e.ServicioRoles.AsignarAsync(_e.Actor, Guid.NewGuid(), (Rol)999, Sede);

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("ROL_INVALIDO", r.Codigo);
    }

    // ---- RF-ROL-003: entradas y reactivación ----

    [Fact]
    public async Task RF_ROL_003_procedimientos_nulos_se_rechazan_como_invalidos()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();

        var r = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, EntornoAdministracion.Cardiologia, null!, _e.HoyEnSede.AddDays(30));

        Assert.Equal(EstadoOperacion.Invalida, r.Estado);
        Assert.Equal("PROCEDIMIENTOS_INVALIDOS", r.Codigo);
    }

    [Fact]
    public async Task RF_ROL_003_actualizar_una_habilitacion_revocada_la_reactiva_y_la_bitacora_lo_dice()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, EntornoAdministracion.Cardiologia, [], _e.HoyEnSede.AddDays(30));
        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, hab.Id!.Value);
        _e.Auditoria.Eventos.Clear();

        var r = await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(
            _e.Actor, hab.Id.Value, [], _e.HoyEnSede.AddDays(60));

        Assert.Equal(EstadoOperacion.Exitosa, r.Estado);
        var evento = Assert.Single(_e.Auditoria.Eventos);
        Assert.Equal(TipoEventoAdministracion.HabilitacionActualizada, evento.Tipo);
        Assert.Contains("reactivada=true", evento.Detalle);
    }

    [Fact]
    public async Task RF_ROL_003_actualizar_una_habilitacion_vigente_no_marca_reactivacion()
    {
        var profesionalId = await _e.SembrarProfesionalAsync();
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, EntornoAdministracion.Cardiologia, [], _e.HoyEnSede.AddDays(30));
        _e.Auditoria.Eventos.Clear();

        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, hab.Id!.Value, [], _e.HoyEnSede.AddDays(60));

        Assert.DoesNotContain("reactivada", Assert.Single(_e.Auditoria.Eventos).Detalle);
    }

    /// <summary>Repositorio cuyo <c>ObtenerPorCorreoAsync</c> no ve el duplicado, como en una carrera real.</summary>
    private sealed class UsuariosConRestriccionUnica(IRepositorioUsuarios interno) : IRepositorioUsuarios
    {
        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) => interno.ObtenerPorIdAsync(id, ct);
        public Task GuardarAsync(Usuario usuario, CancellationToken ct = default) => interno.GuardarAsync(usuario, ct);
        public Task AgregarAsync(Usuario usuario, CancellationToken ct = default) =>
            throw new ViolacionDeUnicidadException("Restricción única de correo.");
    }
}
