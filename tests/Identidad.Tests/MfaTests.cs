using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

public class MfaTests
{
    private const string Correo = "odontologo@clinica.test";
    private const string Clave = "una clave larga y segura";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _repo = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly AvisosFalsos _avisos = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly Usuario _personal;
    private readonly ServicioInicioSesion _login;
    private readonly ServicioMfa _mfa;

    public MfaTests()
    {
        _personal = Usuario.Crear(Guid.NewGuid(), Correo, _hasher.Hashear(Clave), requiereMfa: true);
        _repo.Agregar(_personal);
        _login = new ServicioInicioSesion(_repo, _hasher, _reloj, _auditoria, _avisos);
        _mfa = new ServicioMfa(_repo, _hasher, new ProtectorFalso(), _reloj, _auditoria, _avisos);
    }

    private string CodigoActual() => Totp.Generar(Base32.Decodificar(_secreto!), Totp.PasoDe(_reloj.Ahora));
    private string? _secreto;

    /// <summary>Configura MFA de punta a punta y devuelve los códigos de recuperación.</summary>
    private async Task<IReadOnlyList<string>> ConfigurarMfaAsync()
    {
        var inicio = await _mfa.IniciarConfiguracionAsync(_personal.Id);
        _secreto = inicio!.SecretoBase32;
        var r = await _mfa.ConfirmarConfiguracionAsync(_personal.Id, CodigoActual());
        _reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos)); // el siguiente paso, para poder iniciar sesión
        return r.CodigosRecuperacion;
    }

    // ---- CA-IAM-001 y primer factor ----

    [Fact]
    public async Task CA_IAM_001_profesional_sin_mfa_debe_configurarlo_antes_de_acceder()
    {
        var r = await _login.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.RequiereConfiguracionMfa, r.Estado);
        Assert.Equal(_personal.Id, r.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_002_profesional_con_mfa_debe_presentar_el_segundo_factor()
    {
        await ConfigurarMfaAsync();
        var r = await _login.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.RequiereSegundoFactor, r.Estado);
        Assert.Equal(_personal.Id, r.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_002_paciente_no_requiere_segundo_factor()
    {
        var paciente = Usuario.Crear(Guid.NewGuid(), "paciente@correo.test", _hasher.Hashear(Clave), requiereMfa: false);
        _repo.Agregar(paciente);
        var r = await _login.IniciarSesionAsync("paciente@correo.test", Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_002_la_clave_correcta_sola_no_registra_inicio_exitoso()
    {
        await ConfigurarMfaAsync();
        _auditoria.Eventos.Clear();
        await _login.IniciarSesionAsync(Correo, Clave);
        Assert.DoesNotContain(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.InicioSesionExitoso);
    }

    // ---- Configuración ----

    [Fact]
    public async Task RF_IAM_002_iniciar_configuracion_devuelve_secreto_y_uri_otpauth()
    {
        var inicio = await _mfa.IniciarConfiguracionAsync(_personal.Id);
        Assert.NotNull(inicio);
        Assert.StartsWith("otpauth://totp/Continuum:", inicio.UriOtpAuth);
        Assert.Contains("secret=" + inicio.SecretoBase32, inicio.UriOtpAuth);
        Assert.Contains("issuer=Continuum", inicio.UriOtpAuth);
    }

    [Fact]
    public async Task RF_IAM_002_confirmar_con_codigo_valido_activa_mfa_y_entrega_10_codigos_de_recuperacion()
    {
        var codigos = await ConfigurarMfaAsync();
        Assert.True(_personal.MfaHabilitado);
        Assert.Equal(10, codigos.Count);
        Assert.Equal(10, codigos.Distinct().Count());
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.MfaHabilitado && e.UsuarioId == _personal.Id);
    }

    [Fact]
    public async Task RF_IAM_002_confirmar_con_codigo_invalido_no_activa_mfa()
    {
        var inicio = await _mfa.IniciarConfiguracionAsync(_personal.Id);
        var r = await _mfa.ConfirmarConfiguracionAsync(_personal.Id, "000000x");
        Assert.False(r.Exito);
        Assert.False(_personal.MfaHabilitado);
        Assert.NotNull(inicio);
    }

    [Fact]
    public async Task RF_IAM_002_no_se_puede_reconfigurar_un_mfa_ya_activo()
    {
        await ConfigurarMfaAsync();
        Assert.Null(await _mfa.IniciarConfiguracionAsync(_personal.Id));
    }

    [Fact]
    public async Task RF_IAM_002_el_secreto_se_guarda_protegido_y_los_codigos_de_recuperacion_con_hash()
    {
        var codigos = await ConfigurarMfaAsync();
        Assert.NotEqual(_secreto, _personal.SecretoTotpProtegido);
        Assert.DoesNotContain(_personal.CodigosRecuperacionHash, h => codigos.Contains(h));
    }

    // ---- Segundo factor ----

    [Fact]
    public async Task RF_IAM_002_codigo_totp_correcto_completa_el_inicio_de_sesion()
    {
        await ConfigurarMfaAsync();
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, CodigoActual());
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.InicioSesionExitoso);
        Assert.Equal(_reloj.Ahora, _personal.UltimoAcceso);
    }

    [Fact]
    public async Task RF_IAM_002_un_codigo_totp_no_se_puede_reutilizar()
    {
        await ConfigurarMfaAsync();
        var codigo = CodigoActual();
        await _mfa.VerificarSegundoFactorAsync(_personal.Id, codigo);
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, codigo);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_002_codigo_totp_incorrecto_se_rechaza_y_se_registra()
    {
        await ConfigurarMfaAsync();
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, "123456");
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.InicioSesionFallido && e.Detalle == "segundo_factor");
    }

    [Fact]
    public async Task RF_IAM_004_los_fallos_del_segundo_factor_bloquean_la_cuenta()
    {
        await ConfigurarMfaAsync();
        for (var i = 0; i < 5; i++) await _mfa.VerificarSegundoFactorAsync(_personal.Id, "123456");

        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, CodigoActual());
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, r.Estado);
        Assert.Single(_avisos.Bloqueos);
    }

    [Fact]
    public async Task RF_IAM_004_la_clave_correcta_no_reinicia_el_contador_de_fallos_del_segundo_factor()
    {
        await ConfigurarMfaAsync();
        for (var i = 0; i < 5; i++)
        {
            await _login.IniciarSesionAsync(Correo, Clave); // primer factor correcto cada vez
            await _mfa.VerificarSegundoFactorAsync(_personal.Id, "123456");
        }
        var r = await _login.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, r.Estado);
    }

    // ---- Códigos de recuperación ----

    [Fact]
    public async Task RF_IAM_002_un_codigo_de_recuperacion_completa_el_inicio_y_se_consume()
    {
        var codigos = await ConfigurarMfaAsync();
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, codigos[0]);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.Equal(9, _personal.CodigosRecuperacionHash.Count);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.CodigoRecuperacionUsado);

        var otraVez = await _mfa.VerificarSegundoFactorAsync(_personal.Id, codigos[0]);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, otraVez.Estado);
    }

    [Fact]
    public async Task RF_IAM_002_el_codigo_de_recuperacion_tolera_minusculas_y_guion()
    {
        var codigos = await ConfigurarMfaAsync();
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, codigos[1].ToLowerInvariant().Replace("-", ""));
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_002_usuario_sin_mfa_activo_no_puede_pasar_el_segundo_factor()
    {
        var r = await _mfa.VerificarSegundoFactorAsync(_personal.Id, "123456");
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
    }
}
