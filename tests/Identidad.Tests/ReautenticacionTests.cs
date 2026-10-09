using System.Security.Claims;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;

namespace Continuum.Identidad.Tests;

public class ReautenticacionTests
{
    private const string Correo = "firma@clinica.test";
    private const string Clave = "una clave larga y segura";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _repo = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly AvisosFalsos _avisos = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly Usuario _usuario;
    private readonly Guid _sesionId = Guid.NewGuid();
    private readonly ServicioMfa _mfa;
    private readonly ServicioReautenticacion _servicio;
    private string? _secreto;

    public ReautenticacionTests()
    {
        _usuario = Usuario.Crear(Guid.NewGuid(), Correo, _hasher.Hashear(Clave));
        _repo.Agregar(_usuario);
        _mfa = new ServicioMfa(_repo, _hasher, new ProtectorFalso(), _reloj, _auditoria, _avisos);
        _servicio = new ServicioReautenticacion(_repo, _hasher, _mfa, new EmisorFalso(), _reloj, _auditoria, _avisos);
    }

    private string Codigo() => Totp.Generar(Base32.Decodificar(_secreto!), Totp.PasoDe(_reloj.Ahora));

    private async Task<IReadOnlyList<string>> ActivarMfaAsync()
    {
        _secreto = (await _mfa.IniciarConfiguracionAsync(_usuario.Id))!.SecretoBase32;
        var r = await _mfa.ConfirmarConfiguracionAsync(_usuario.Id, Codigo());
        _reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos));
        return r.CodigosRecuperacion;
    }

    private Task<RespuestaReautenticacion> ReautenticarAsync(string clave = Clave, string? codigo = null) =>
        _servicio.ReautenticarAsync(_usuario.Id, _sesionId, clave, codigo);

    [Fact]
    public async Task RF_IAM_010_con_clave_y_segundo_factor_correctos_se_emite_un_acceso_con_la_marca_de_reautenticacion()
    {
        await ActivarMfaAsync();
        var r = await ReautenticarAsync(codigo: Codigo());

        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.Equal($"acceso:{_usuario.Id}:{_sesionId}:rea={_reloj.Ahora.ToUnixTimeSeconds()}", r.TokenAcceso);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.Reautenticacion && e.UsuarioId == _usuario.Id);
    }

    [Fact]
    public async Task RF_IAM_010_quien_no_exige_mfa_se_reautentica_solo_con_la_clave()
    {
        var paciente = Usuario.Crear(Guid.NewGuid(), "pac@correo.test", _hasher.Hashear(Clave), requiereMfa: false);
        _repo.Agregar(paciente);
        var r = await _servicio.ReautenticarAsync(paciente.Id, _sesionId, Clave, null);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_010_una_clave_incorrecta_no_reautentica()
    {
        await ActivarMfaAsync();
        var r = await ReautenticarAsync("incorrecta", Codigo());
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
        Assert.Null(r.TokenAcceso);
    }

    [Fact]
    public async Task RF_IAM_010_con_mfa_activo_la_clave_sola_no_basta()
    {
        await ActivarMfaAsync();
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await ReautenticarAsync()).Estado);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await ReautenticarAsync(codigo: "000000")).Estado);
    }

    [Fact]
    public async Task RF_IAM_010_un_codigo_de_recuperacion_tambien_reautentica_y_se_consume()
    {
        var codigos = await ActivarMfaAsync();
        Assert.Equal(EstadoInicioSesion.Exitoso, (await ReautenticarAsync(codigo: codigos[0])).Estado);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await ReautenticarAsync(codigo: codigos[0])).Estado);
    }

    [Fact]
    public async Task RF_IAM_010_un_codigo_totp_ya_usado_no_sirve_para_reautenticar()
    {
        await ActivarMfaAsync();
        var codigo = Codigo();
        Assert.Equal(EstadoInicioSesion.Exitoso, (await ReautenticarAsync(codigo: codigo)).Estado);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await ReautenticarAsync(codigo: codigo)).Estado);
    }

    [Fact]
    public async Task RF_IAM_004_los_fallos_al_reautenticar_cuentan_para_el_bloqueo()
    {
        await ActivarMfaAsync();
        for (var i = 0; i < 5; i++) await ReautenticarAsync("incorrecta", Codigo());

        var r = await ReautenticarAsync(codigo: Codigo());
        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, r.Estado);
        Assert.Equal(_reloj.Ahora.AddMinutes(15), r.BloqueadoHasta);
        Assert.Single(_avisos.Bloqueos);
    }

    [Fact]
    public async Task RF_IAM_010_un_usuario_inactivo_no_se_reautentica()
    {
        _usuario.Desactivar();
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, (await ReautenticarAsync()).Estado);
    }

    [Fact]
    public async Task RF_IAM_009_los_fallos_de_reautenticacion_quedan_en_la_bitacora_sin_secretos()
    {
        await ActivarMfaAsync();
        await ReautenticarAsync("clave-secreta-incorrecta", Codigo());

        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.InicioSesionFallido && e.Detalle == "reautenticacion");
        Assert.DoesNotContain(_auditoria.Eventos, e => e.ToString()!.Contains("clave-secreta-incorrecta"));
    }
}

public class ReautenticacionRecienteTests
{
    private readonly RelojFalso _reloj = new();
    private readonly OpcionesSesion _opciones = new() { ReautenticacionMinutos = 5 };

    private async Task<bool> EvaluarAsync(string? reautenticadoUnix)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()) };
        if (reautenticadoUnix is not null) claims.Add(new Claim("rea", reautenticadoUnix));
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "prueba"));

        var requisito = new ReautenticacionRecienteRequirement();
        var contexto = new AuthorizationHandlerContext([requisito], usuario, null);
        await new ReautenticacionRecienteHandler(_reloj, _opciones).HandleAsync(contexto);
        return contexto.HasSucceeded;
    }

    private string Hace(TimeSpan t) => _reloj.Ahora.Add(-t).ToUnixTimeSeconds().ToString();

    [Fact]
    public async Task RF_IAM_010_acepta_una_reautenticacion_de_hace_menos_de_5_minutos()
    {
        Assert.True(await EvaluarAsync(Hace(TimeSpan.Zero)));
        Assert.True(await EvaluarAsync(Hace(TimeSpan.FromMinutes(4).Add(TimeSpan.FromSeconds(59)))));
    }

    [Fact]
    public async Task RF_IAM_010_rechaza_una_reautenticacion_de_5_minutos_o_mas()
    {
        Assert.False(await EvaluarAsync(Hace(TimeSpan.FromMinutes(5))));
        Assert.False(await EvaluarAsync(Hace(TimeSpan.FromHours(1))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-un-numero")]
    public async Task RF_IAM_010_rechaza_la_ausencia_o_un_valor_invalido(string? valor) =>
        Assert.False(await EvaluarAsync(valor));

    [Fact]
    public async Task RF_IAM_010_rechaza_una_marca_en_el_futuro() =>
        Assert.False(await EvaluarAsync(_reloj.Ahora.AddMinutes(10).ToUnixTimeSeconds().ToString()));
}
