using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

public class MfaPorMensajeTests
{
    private const string Clave = "una clave larga y segura";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _usuarios = new();
    private readonly TokensAccionFalsos _tokens = new();
    private readonly SesionesFalsas _sesiones = new();
    private readonly MensajeriaFalsa _mensajeria = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly AvisosFalsos _avisos = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly Usuario _paciente;
    private readonly Usuario _personal;
    private readonly ServicioMfaPorMensaje _servicio;
    private readonly ServicioAutenticacion _auth;

    public MfaPorMensajeTests()
    {
        _paciente = Usuario.Crear(Guid.NewGuid(), "paciente@correo.test", _hasher.Hashear(Clave), requiereMfa: false);
        _personal = Usuario.Crear(Guid.NewGuid(), "doc@clinica.test", _hasher.Hashear(Clave));
        _usuarios.Agregar(_paciente);
        _usuarios.Agregar(_personal);

        _servicio = new ServicioMfaPorMensaje(_usuarios, _tokens, _reloj, _auditoria, _avisos, _mensajeria);
        _auth = new ServicioAutenticacion(
            new ServicioInicioSesion(_usuarios, _hasher, _reloj, _auditoria, _avisos),
            new ServicioMfa(_usuarios, _hasher, new ProtectorFalso(), _reloj, _auditoria, _avisos),
            _servicio,
            new ServicioSesiones(_sesiones, new PoliticaSesionFalsa(), _reloj, _auditoria),
            _usuarios, new EmisorFalso());
    }

    private string UltimoCodigo() => _mensajeria.Codigos.Last().Codigo;

    private async Task ActivarAsync()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        Assert.Equal(ResultadoConfirmacionCodigo.Exito, await _servicio.ConfirmarActivacionAsync(_paciente.Id, UltimoCodigo()));
        _reloj.Avanzar(TimeSpan.FromMinutes(2));
    }

    // ---- Activación (opcional para pacientes) ----

    [Fact]
    public async Task RF_IAM_003_el_paciente_pide_activar_y_recibe_un_codigo_de_6_digitos_valido_10_minutos()
    {
        Assert.Equal(ResultadoSolicitudCodigo.Enviado, await _servicio.IniciarActivacionAsync(_paciente.Id));

        var envio = Assert.Single(_mensajeria.Codigos);
        Assert.Matches(@"^\d{6}$", envio.Codigo);
        Assert.Equal(_reloj.Ahora.AddMinutes(10), envio.ExpiraEn);
        Assert.Equal(_paciente.Id, envio.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_003_el_codigo_se_guarda_solo_como_hash()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        var guardado = Assert.Single(_tokens.Todos);
        Assert.DoesNotContain(UltimoCodigo(), guardado.HashToken);
        Assert.Equal(PropositoToken.CodigoMfaCorreo, guardado.Proposito);
    }

    [Fact]
    public async Task RF_IAM_003_el_personal_no_puede_cambiar_su_mfa_por_un_codigo_por_mensaje()
    {
        Assert.Equal(ResultadoSolicitudCodigo.NoPermitido, await _servicio.IniciarActivacionAsync(_personal.Id));
        Assert.Empty(_mensajeria.Codigos);
    }

    [Fact]
    public async Task RF_IAM_003_no_se_puede_activar_dos_veces()
    {
        await ActivarAsync();
        Assert.Equal(ResultadoSolicitudCodigo.NoPermitido, await _servicio.IniciarActivacionAsync(_paciente.Id));
    }

    [Fact]
    public async Task RF_IAM_003_pedir_codigos_seguidos_no_inunda_de_mensajes()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        _reloj.Avanzar(TimeSpan.FromSeconds(30));

        Assert.Equal(ResultadoSolicitudCodigo.EsperaRequerida, await _servicio.IniciarActivacionAsync(_paciente.Id));
        Assert.Single(_mensajeria.Codigos);

        _reloj.Avanzar(TimeSpan.FromSeconds(31));
        Assert.Equal(ResultadoSolicitudCodigo.Enviado, await _servicio.IniciarActivacionAsync(_paciente.Id));
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_correcto_activa_el_segundo_factor()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        var r = await _servicio.ConfirmarActivacionAsync(_paciente.Id, UltimoCodigo());

        Assert.Equal(ResultadoConfirmacionCodigo.Exito, r);
        Assert.True(_paciente.MfaPorMensajeHabilitado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.MfaCorreoHabilitado && e.UsuarioId == _paciente.Id);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_incorrecto_no_activa_nada()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        var incorrecto = UltimoCodigo() == "000000" ? "111111" : "000000";

        Assert.Equal(ResultadoConfirmacionCodigo.Invalido, await _servicio.ConfirmarActivacionAsync(_paciente.Id, incorrecto));
        Assert.False(_paciente.MfaPorMensajeHabilitado);
    }

    [Fact]
    public async Task RF_IAM_003_cinco_codigos_incorrectos_anulan_el_codigo_aunque_luego_se_acierte()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        var correcto = UltimoCodigo();
        var incorrecto = correcto == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++) await _servicio.ConfirmarActivacionAsync(_paciente.Id, incorrecto);

        Assert.Equal(ResultadoConfirmacionCodigo.Invalido, await _servicio.ConfirmarActivacionAsync(_paciente.Id, correcto));
        Assert.False(_paciente.MfaPorMensajeHabilitado);
    }

    [Fact]
    public async Task RF_IAM_003_el_codigo_caduca_a_los_10_minutos()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.Equal(ResultadoConfirmacionCodigo.Invalido, await _servicio.ConfirmarActivacionAsync(_paciente.Id, UltimoCodigo()));
    }

    [Fact]
    public async Task RF_IAM_003_el_codigo_de_otra_persona_no_sirve()
    {
        var otro = Usuario.Crear(Guid.NewGuid(), "otro@correo.test", "h", requiereMfa: false);
        _usuarios.Agregar(otro);
        await _servicio.IniciarActivacionAsync(_paciente.Id);

        Assert.Equal(ResultadoConfirmacionCodigo.Invalido, await _servicio.ConfirmarActivacionAsync(otro.Id, UltimoCodigo()));
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_no_se_puede_usar_dos_veces()
    {
        await _servicio.IniciarActivacionAsync(_paciente.Id);
        var codigo = UltimoCodigo();
        await _servicio.ConfirmarActivacionAsync(_paciente.Id, codigo);
        await _servicio.DesactivarAsync(_paciente.Id);

        Assert.Equal(ResultadoConfirmacionCodigo.Invalido, await _servicio.ConfirmarActivacionAsync(_paciente.Id, codigo));
    }

    [Fact]
    public async Task RF_IAM_003_desactivar_deja_al_paciente_con_inicio_de_sesion_directo()
    {
        await ActivarAsync();
        await _servicio.DesactivarAsync(_paciente.Id);

        Assert.False(_paciente.MfaPorMensajeHabilitado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.MfaCorreoDeshabilitado && e.UsuarioId == _paciente.Id);
        Assert.Equal(EstadoInicioSesion.Exitoso, (await _auth.IniciarSesionAsync("paciente@correo.test", Clave)).Estado);
    }

    // ---- Inicio de sesión con código por mensaje ----

    [Fact]
    public async Task RF_IAM_003_con_el_factor_activo_el_login_envia_un_codigo_y_no_da_acceso()
    {
        await ActivarAsync();
        var antes = _mensajeria.Codigos.Count;

        var r = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);

        Assert.Equal(EstadoInicioSesion.RequiereSegundoFactor, r.Estado);
        Assert.Equal(MetodoSegundoFactor.CodigoPorMensaje, r.Metodo);
        Assert.NotNull(r.TokenDesafio);
        Assert.Null(r.TokenAcceso);
        Assert.Equal(antes + 1, _mensajeria.Codigos.Count);
    }

    [Fact]
    public async Task RF_IAM_003_el_codigo_correcto_completa_el_inicio_de_sesion()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);

        var r = await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, UltimoCodigo());

        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.NotNull(r.TokenAcceso);
        Assert.NotNull(r.TokenRenovacion);
        Assert.Single(_sesiones.Todas);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_de_login_no_se_puede_reutilizar()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var codigo = UltimoCodigo();
        await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, codigo);

        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas,
            (await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, codigo)).Estado);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_incorrecto_no_da_acceso_ni_crea_sesion()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var incorrecto = UltimoCodigo() == "000000" ? "111111" : "000000";

        var r = await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, incorrecto);

        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Empty(_sesiones.Todas);
    }

    [Fact]
    public async Task RF_IAM_004_los_codigos_incorrectos_bloquean_la_cuenta()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var incorrecto = UltimoCodigo() == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++) await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, incorrecto);

        var r = await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, UltimoCodigo());
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, r.Estado);
        Assert.Single(_avisos.Bloqueos);
    }

    [Fact]
    public async Task RF_IAM_004_la_clave_correcta_no_reinicia_los_fallos_del_codigo()
    {
        await ActivarAsync();
        var incorrecto = "000000";
        for (var i = 0; i < 5; i++)
        {
            _reloj.Avanzar(TimeSpan.FromSeconds(61)); // respeta la espera entre envíos
            var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
            await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, incorrecto == UltimoCodigo() ? "111111" : incorrecto);
        }

        var ultimo = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, ultimo.Estado);
    }

    [Fact]
    public async Task RF_IAM_003_se_puede_reenviar_el_codigo_pasada_la_espera()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var enviados = _mensajeria.Codigos.Count;

        await _auth.ReenviarCodigoAsync(login.TokenDesafio!);
        Assert.Equal(enviados, _mensajeria.Codigos.Count); // demasiado pronto

        _reloj.Avanzar(TimeSpan.FromSeconds(61));
        await _auth.ReenviarCodigoAsync(login.TokenDesafio!);
        Assert.Equal(enviados + 1, _mensajeria.Codigos.Count);

        var r = await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, UltimoCodigo());
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_nuevo_anula_el_anterior()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var viejo = UltimoCodigo();
        _reloj.Avanzar(TimeSpan.FromSeconds(61));
        await _auth.ReenviarCodigoAsync(login.TokenDesafio!);

        if (viejo != UltimoCodigo())
            Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, viejo)).Estado);
    }

    [Fact]
    public async Task RF_IAM_009_la_bitacora_no_contiene_el_codigo()
    {
        await ActivarAsync();
        var login = await _auth.IniciarSesionAsync("paciente@correo.test", Clave);
        var codigo = UltimoCodigo();
        await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, codigo);

        Assert.DoesNotContain(_auditoria.Eventos, e => e.ToString()!.Contains($"\"{codigo}\"") || e.Detalle == codigo);
    }
}
