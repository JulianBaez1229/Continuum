using System.Net;
using System.Net.Http.Json;
using Continuum.Identidad.Aplicacion;

namespace Continuum.Api.Tests;

public class ReautenticacionApiTests(FabricaApi fabrica) : SesionesApiBase(fabrica), IClassFixture<FabricaApi>
{
    private const string Clave = "una clave larga y segura";
    private sealed record Reaut(string TokenAcceso);

    private Task<HttpResponseMessage> Reautenticar(string? token, string contrasena = Clave, string? codigo = null) =>
        Post("/api/auth/reautenticar", token, new { contrasena, codigo });

    private async Task<HttpResponseMessage> Estado(string? token)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/reautenticacion/estado");
        if (token is not null) peticion.Headers.Authorization = new("Bearer", token);
        return await Http.SendAsync(peticion);
    }

    [Fact]
    public async Task RF_IAM_010_una_accion_critica_sin_reautenticar_devuelve_403_con_codigo()
    {
        var t = await LoginPacienteAsync();
        var r = await Estado(t.TokenAcceso);

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("reautenticacion_requerida", await CodigoDe(r));
    }

    [Fact]
    public async Task RF_IAM_010_tras_reautenticar_la_accion_critica_se_permite_con_el_acceso_nuevo()
    {
        var t = await LoginPacienteAsync();
        var r = await Reautenticar(t.TokenAcceso);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var nuevo = (await r.Content.ReadFromJsonAsync<Reaut>())!;

        Assert.Equal(HttpStatusCode.NoContent, (await Estado(nuevo.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Estado(t.TokenAcceso)).StatusCode); // el acceso viejo sigue sin marca
        Assert.Equal(HttpStatusCode.OK, (await Yo(nuevo.TokenAcceso)).StatusCode);        // misma sesión
    }

    [Fact]
    public async Task RF_IAM_010_la_reautenticacion_vale_menos_de_5_minutos()
    {
        var t = await LoginPacienteAsync();
        var nuevo = (await (await Reautenticar(t.TokenAcceso)).Content.ReadFromJsonAsync<Reaut>())!;

        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(4));
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(nuevo.TokenAcceso)).StatusCode);

        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(2));
        var r = await Estado(nuevo.TokenAcceso);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("reautenticacion_requerida", await CodigoDe(r));
    }

    [Fact]
    public async Task RF_IAM_010_una_clave_incorrecta_devuelve_401_con_codigo_propio()
    {
        var t = await LoginPacienteAsync();
        var r = await Reautenticar(t.TokenAcceso, "incorrecta");

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("reautenticacion_invalida", await CodigoDe(r));
    }

    [Fact]
    public async Task RF_IAM_010_el_personal_con_mfa_debe_aportar_tambien_el_codigo()
    {
        var t = await LoginPersonalAsync();
        Fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos)); // el código del inicio de sesión ya se usó

        Assert.Equal(HttpStatusCode.Unauthorized, (await Reautenticar(t.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Reautenticar(t.TokenAcceso, codigo: "000000")).StatusCode);

        Fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos));
        var ok = await Reautenticar(t.TokenAcceso, codigo: CodigoActual());
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var nuevo = (await ok.Content.ReadFromJsonAsync<Reaut>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(nuevo.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_004_los_fallos_al_reautenticar_bloquean_la_cuenta()
    {
        var correo = NuevoUsuario(false);
        var t = await LoginPacienteAsync(correo);
        for (var i = 0; i < 5; i++) await Reautenticar(t.TokenAcceso, "incorrecta");

        Assert.Equal(HttpStatusCode.Locked, (await Reautenticar(t.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.Locked,
            (await Http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = Clave })).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_010_sin_sesion_no_se_puede_reautenticar() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Reautenticar(null)).StatusCode);

    [Fact]
    public async Task RF_IAM_010_sin_sesion_la_comprobacion_devuelve_401_no_403() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Estado(null)).StatusCode);
}
