using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

internal sealed class EmisorFalso : IEmisorTokens
{
    public string EmitirAcceso(Usuario usuario, Guid sesionId, DateTimeOffset? reautenticadoEn = null) =>
        $"acceso:{usuario.Id}:{sesionId}" + (reautenticadoEn is { } r ? $":rea={r.ToUnixTimeSeconds()}" : "");
    public string EmitirDesafio(Guid usuarioId, PropositoDesafio proposito) => $"desafio:{proposito}:{usuarioId}";
    public Guid? ValidarDesafio(string token, PropositoDesafio proposito)
    {
        var partes = token.Split(':');
        return partes is ["desafio", var p, var id] && p == proposito.ToString() && Guid.TryParse(id, out var guid)
            ? guid : null;
    }
}

public class AutenticacionTests
{
    private const string Correo = "psi@clinica.test";
    private const string Clave = "una clave larga y segura";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _repo = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly ServicioAutenticacion _auth;
    private readonly Usuario _personal;
    private readonly SesionesFalsas _sesiones = new();
    private string? _secreto;

    public AutenticacionTests()
    {
        _personal = Usuario.Crear(Guid.NewGuid(), Correo, _hasher.Hashear(Clave));
        _repo.Agregar(_personal);
        var auditoria = new AuditoriaFalsa();
        var avisos = new AvisosFalsos();
        _auth = new ServicioAutenticacion(
            new ServicioInicioSesion(_repo, _hasher, _reloj, auditoria, avisos),
            new ServicioMfa(_repo, _hasher, new ProtectorFalso(), _reloj, auditoria, avisos),
            new ServicioSesiones(_sesiones, new PoliticaSesionFalsa(), _reloj, auditoria),
            _repo, new EmisorFalso());
    }

    /// <summary>Lleva a un profesional hasta el token de acceso y su token de renovación.</summary>
    private async Task<RespuestaAutenticacion> IniciarCompletoAsync()
    {
        await ConfigurarMfaAsync();
        var login = await _auth.IniciarSesionAsync(Correo, Clave);
        return await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, Codigo());
    }

    private string Codigo() => Totp.Generar(Base32.Decodificar(_secreto!), Totp.PasoDe(_reloj.Ahora));

    private async Task ConfigurarMfaAsync()
    {
        var login = await _auth.IniciarSesionAsync(Correo, Clave);
        var inicio = await _auth.IniciarConfiguracionMfaAsync(login.TokenDesafio!);
        _secreto = inicio!.SecretoBase32;
        await _auth.ConfirmarConfiguracionMfaAsync(login.TokenDesafio!, Codigo());
        _reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos));
    }

    [Fact]
    public async Task CA_IAM_001_sin_mfa_el_login_entrega_solo_un_desafio_de_configuracion()
    {
        var r = await _auth.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.RequiereConfiguracionMfa, r.Estado);
        Assert.Null(r.TokenAcceso);
        Assert.NotNull(r.TokenDesafio);
    }

    [Fact]
    public async Task RF_IAM_002_con_mfa_el_login_entrega_un_desafio_de_segundo_factor_y_no_acceso()
    {
        await ConfigurarMfaAsync();
        var r = await _auth.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.RequiereSegundoFactor, r.Estado);
        Assert.Null(r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_002_el_segundo_factor_correcto_entrega_el_token_de_acceso()
    {
        await ConfigurarMfaAsync();
        var login = await _auth.IniciarSesionAsync(Correo, Clave);
        var r = await _auth.VerificarSegundoFactorAsync(login.TokenDesafio!, Codigo());
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.StartsWith($"acceso:{_personal.Id}:", r.TokenAcceso);
        Assert.NotNull(r.TokenRenovacion);
    }

    [Fact]
    public async Task RF_IAM_006_el_token_de_acceso_queda_atado_a_una_sesion_registrada()
    {
        var r = await IniciarCompletoAsync();
        var sesion = Assert.Single(_sesiones.Todas);
        Assert.EndsWith(sesion.Id.ToString(), r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_006_ni_el_desafio_ni_las_credenciales_invalidas_crean_sesion()
    {
        await _auth.IniciarSesionAsync(Correo, Clave);
        await _auth.IniciarSesionAsync(Correo, "incorrecta");
        Assert.Empty(_sesiones.Todas);
    }

    [Fact]
    public async Task RF_IAM_006_renovar_entrega_nuevo_acceso_y_nuevo_token_de_renovacion()
    {
        var inicial = await IniciarCompletoAsync();
        var r = await _auth.RenovarAsync(inicial.TokenRenovacion!);

        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.NotNull(r.TokenAcceso);
        Assert.NotEqual(inicial.TokenRenovacion, r.TokenRenovacion);
    }

    [Fact]
    public async Task RF_IAM_006_renovar_con_un_usuario_desactivado_se_rechaza()
    {
        var inicial = await IniciarCompletoAsync();
        _personal.Desactivar();
        var r = await _auth.RenovarAsync(inicial.TokenRenovacion!);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Null(r.TokenAcceso);
    }

    [Fact]
    public async Task CA_IAM_003_renovar_tras_la_inactividad_informa_que_la_sesion_expiro()
    {
        var inicial = await IniciarCompletoAsync();
        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        var r = await _auth.RenovarAsync(inicial.TokenRenovacion!);
        Assert.Equal(EstadoInicioSesion.SesionExpirada, r.Estado);
        Assert.Null(r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_sesion_impide_renovar()
    {
        var inicial = await IniciarCompletoAsync();
        await _auth.CerrarSesionAsync(_sesiones.Todas.Single().Id);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await _auth.RenovarAsync(inicial.TokenRenovacion!)).Estado);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_todas_impide_renovar_desde_cualquier_dispositivo()
    {
        var inicial = await IniciarCompletoAsync();
        await _auth.CerrarTodasLasSesionesAsync(_personal.Id);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await _auth.RenovarAsync(inicial.TokenRenovacion!)).Estado);
    }

    [Fact]
    public async Task RF_IAM_002_un_desafio_de_configuracion_no_sirve_para_el_segundo_factor()
    {
        await ConfigurarMfaAsync();
        var r = await _auth.VerificarSegundoFactorAsync($"desafio:{PropositoDesafio.ConfigurarMfa}:{_personal.Id}", Codigo());
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Null(r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_002_un_desafio_de_segundo_factor_no_sirve_para_configurar_mfa()
    {
        await ConfigurarMfaAsync();
        var login = await _auth.IniciarSesionAsync(Correo, Clave);
        Assert.Null(await _auth.IniciarConfiguracionMfaAsync(login.TokenDesafio!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("basura")]
    public async Task RF_IAM_002_un_desafio_invalido_se_rechaza(string token)
    {
        await ConfigurarMfaAsync();
        var r = await _auth.VerificarSegundoFactorAsync(token, Codigo());
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_001_credenciales_invalidas_no_entregan_tokens()
    {
        var r = await _auth.IniciarSesionAsync(Correo, "incorrecta");
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Null(r.TokenAcceso);
        Assert.Null(r.TokenDesafio);
    }

    [Fact]
    public async Task RF_IAM_001_un_paciente_sin_mfa_obtiene_acceso_directo()
    {
        _repo.Agregar(Usuario.Crear(Guid.NewGuid(), "pac@correo.test", _hasher.Hashear(Clave), requiereMfa: false));
        var r = await _auth.IniciarSesionAsync("pac@correo.test", Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.StartsWith("acceso:", r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_004_una_cuenta_bloqueada_informa_hasta_cuando()
    {
        for (var i = 0; i < 5; i++) await _auth.IniciarSesionAsync(Correo, "incorrecta");
        var r = await _auth.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, r.Estado);
        Assert.Equal(_reloj.Ahora.AddMinutes(15), r.BloqueadoHasta);
    }
}
