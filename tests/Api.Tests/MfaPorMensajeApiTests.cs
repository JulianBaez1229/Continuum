using System.Net;
using System.Net.Http.Json;

namespace Continuum.Api.Tests;

public class MfaPorMensajeApiTests(FabricaApi fabrica) : SesionesApiBase(fabrica), IClassFixture<FabricaApi>
{
    private const string Clave = "una clave larga y segura";
    private sealed record Reaut(string TokenAcceso);

    private async Task<Guid> IdDeAsync(string correo) => (await Fabrica.Repositorio.ObtenerPorCorreoAsync(correo))!.Id;
    private string CodigoEnviado(Guid id) => Fabrica.Mensajeria.UltimoCodigoMfa[id];

    private Task<HttpResponseMessage> Login(string correo) =>
        Http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = Clave });

    private Task<HttpResponseMessage> Verificar(string? desafio, string codigo) =>
        Http.PostAsJsonAsync("/api/auth/mfa/verificar", new { tokenDesafio = desafio, codigo });

    /// <summary>Paciente con el segundo factor por mensaje ya activo.</summary>
    private async Task<(string Correo, Guid Id)> PacienteConFactorAsync()
    {
        var correo = NuevoUsuario(personal: false);
        var id = await IdDeAsync(correo);
        var t = await LoginPacienteAsync(correo);
        Assert.Equal(HttpStatusCode.Accepted, (await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso)).StatusCode);
        var ok = await Post("/api/auth/mfa/mensaje/confirmar", t.TokenAcceso, new { codigo = CodigoEnviado(id) });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        return (correo, id);
    }

    [Fact]
    public async Task RF_IAM_003_el_paciente_activa_el_factor_y_en_el_siguiente_login_se_le_pide_el_codigo()
    {
        var (correo, id) = await PacienteConFactorAsync();

        var r = await Login(correo);
        var login = (await r.Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Equal("RequiereSegundoFactor", login.Estado);
        Assert.Equal("CodigoPorMensaje", login.Metodo);
        Assert.Null(login.TokenAcceso);

        var verificado = await Verificar(login.TokenDesafio, CodigoEnviado(id));
        Assert.Equal(HttpStatusCode.OK, verificado.StatusCode);
        var tokens = (await verificado.Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Equal("Exitoso", tokens.Estado);
        Assert.Equal(HttpStatusCode.OK, (await Yo(tokens.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_incorrecto_devuelve_401()
    {
        var (correo, _) = await PacienteConFactorAsync();
        var login = (await (await Login(correo)).Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verificar(login.TokenDesafio, "abcdef")).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_004_los_codigos_incorrectos_bloquean_la_cuenta()
    {
        var (correo, id) = await PacienteConFactorAsync();
        var login = (await (await Login(correo)).Content.ReadFromJsonAsync<Tokens>())!;
        var incorrecto = CodigoEnviado(id) == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++) await Verificar(login.TokenDesafio, incorrecto);

        Assert.Equal(HttpStatusCode.Locked, (await Verificar(login.TokenDesafio, CodigoEnviado(id))).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_se_puede_reenviar_el_codigo_despues_de_un_minuto()
    {
        var (correo, id) = await PacienteConFactorAsync();
        var login = (await (await Login(correo)).Content.ReadFromJsonAsync<Tokens>())!;
        Fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(61));

        var r = await Http.PostAsJsonAsync("/api/auth/mfa/mensaje/reenviar", new { tokenDesafio = login.TokenDesafio });
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Verificar(login.TokenDesafio, CodigoEnviado(id))).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_pedir_otro_codigo_enseguida_devuelve_429()
    {
        var t = await LoginPacienteAsync();
        Assert.Equal(HttpStatusCode.Accepted, (await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_un_codigo_de_confirmacion_incorrecto_devuelve_422()
    {
        var t = await LoginPacienteAsync();
        await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await Post("/api/auth/mfa/mensaje/confirmar", t.TokenAcceso, new { codigo = "abcdef" })).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_el_personal_no_puede_sustituir_su_totp_por_un_codigo_por_mensaje()
    {
        var t = await LoginPersonalAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_003_activarlo_cuando_ya_esta_activo_devuelve_409()
    {
        var correo = NuevoUsuario(personal: false);
        var id = await IdDeAsync(correo);
        var abierta = await LoginPacienteAsync(correo); // sesión abierta antes de activar el factor
        await Post("/api/auth/mfa/mensaje/solicitar", abierta.TokenAcceso);
        await Post("/api/auth/mfa/mensaje/confirmar", abierta.TokenAcceso, new { codigo = CodigoEnviado(id) });

        Assert.Equal(HttpStatusCode.Conflict, (await Post("/api/auth/mfa/mensaje/solicitar", abierta.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_010_desactivar_exige_reautenticacion_reciente_y_luego_vuelve_el_login_directo()
    {
        var correo = NuevoUsuario(personal: false);
        var id = await IdDeAsync(correo);
        var t = await LoginPacienteAsync(correo);
        await Post("/api/auth/mfa/mensaje/solicitar", t.TokenAcceso);
        await Post("/api/auth/mfa/mensaje/confirmar", t.TokenAcceso, new { codigo = CodigoEnviado(id) });

        var sinReauth = await Post("/api/auth/mfa/mensaje/desactivar", t.TokenAcceso);
        Assert.Equal(HttpStatusCode.Forbidden, sinReauth.StatusCode);
        Assert.Equal("reautenticacion_requerida", await CodigoDe(sinReauth));

        var reaut = (await (await Post("/api/auth/reautenticar", t.TokenAcceso, new { contrasena = Clave }))
            .Content.ReadFromJsonAsync<Reaut>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await Post("/api/auth/mfa/mensaje/desactivar", reaut.TokenAcceso)).StatusCode);

        var login = (await (await Login(correo)).Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Equal("Exitoso", login.Estado);
    }

    [Fact]
    public async Task RF_IAM_003_sin_sesion_no_se_puede_activar() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/mfa/mensaje/solicitar", null)).StatusCode);
}
