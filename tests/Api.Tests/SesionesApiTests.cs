using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Hosting;

namespace Continuum.Api.Tests;

/// <summary>Inactividad de 5 min para poder comprobar un token válido con la sesión ya inactiva.</summary>
public sealed class FabricaApiInactividadCorta : FabricaApi
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Sesion:InactividadPersonalMinutos", "5");
        builder.UseSetting("Sesion:InactividadPacienteMinutos", "5");
    }
}

public abstract class SesionesApiBase(FabricaApi fabrica)
{
    private const string Clave = "una clave larga y segura";
    protected readonly HttpClient Http = fabrica.CreateClient();
    protected FabricaApi Fabrica => fabrica;

    protected sealed record Tokens(string Estado, string? TokenAcceso, string? TokenDesafio, string? TokenRenovacion);
    private sealed record InicioMfa(string Secreto);
    protected string? UltimoSecreto { get; private set; }

    protected string NuevoUsuario(bool personal)
    {
        var correo = $"s{Guid.NewGuid():N}@clinica.test";
        fabrica.Repositorio.Agregar(Usuario.Crear(
            Guid.NewGuid(), correo, new HasheadorContrasenaIdentity().Hashear(Clave), requiereMfa: personal));
        return correo;
    }

    protected async Task<Tokens> LoginPacienteAsync(string? correo = null)
    {
        var r = await Http.PostAsJsonAsync("/api/auth/login", new { correo = correo ?? NuevoUsuario(false), contrasena = Clave });
        return (await r.Content.ReadFromJsonAsync<Tokens>())!;
    }

    /// <summary>Alta de MFA y segundo factor: devuelve los tokens de un profesional ya autenticado.</summary>
    protected async Task<Tokens> LoginPersonalAsync()
    {
        var correo = NuevoUsuario(true);
        var primero = (await (await Http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = Clave }))
            .Content.ReadFromJsonAsync<Tokens>())!;
        var inicio = (await (await Http.PostAsJsonAsync("/api/auth/mfa/configuracion/iniciar",
            new { tokenDesafio = primero.TokenDesafio })).Content.ReadFromJsonAsync<InicioMfa>())!;
        UltimoSecreto = inicio.Secreto;
        await Http.PostAsJsonAsync("/api/auth/mfa/configuracion/confirmar",
            new { tokenDesafio = primero.TokenDesafio, codigo = Codigo(inicio.Secreto) });
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos));

        var segundo = (await (await Http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = Clave }))
            .Content.ReadFromJsonAsync<Tokens>())!;
        return (await (await Http.PostAsJsonAsync("/api/auth/mfa/verificar",
            new { tokenDesafio = segundo.TokenDesafio, codigo = Codigo(inicio.Secreto) }))
            .Content.ReadFromJsonAsync<Tokens>())!;
    }

    protected string CodigoActual() => Codigo(UltimoSecreto!);

    private string Codigo(string secreto) =>
        Totp.Generar(Base32.Decodificar(secreto), Totp.PasoDe(fabrica.Reloj.Ahora));

    protected Task<HttpResponseMessage> Yo(string? tokenAcceso)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        if (tokenAcceso is not null) peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAcceso);
        return Http.SendAsync(peticion);
    }

    protected Task<HttpResponseMessage> Post(string ruta, string? tokenAcceso, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, ruta);
        if (tokenAcceso is not null) peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAcceso);
        if (cuerpo is not null) peticion.Content = JsonContent.Create(cuerpo);
        return Http.SendAsync(peticion);
    }

    protected Task<HttpResponseMessage> Renovar(string? token) =>
        Post("/api/auth/renovar", null, new { tokenRenovacion = token });

    protected static async Task<string?> CodigoDe(HttpResponseMessage r)
    {
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("codigo", out var c) ? c.GetString() : null;
    }
}

public class SesionesApiTests(FabricaApi fabrica) : SesionesApiBase(fabrica), IClassFixture<FabricaApi>
{
    [Fact]
    public async Task RF_IAM_006_el_login_entrega_acceso_y_token_de_renovacion()
    {
        var t = await LoginPacienteAsync();
        Assert.NotNull(t.TokenAcceso);
        Assert.NotNull(t.TokenRenovacion);
        Assert.Equal(HttpStatusCode.OK, (await Yo(t.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_renovar_rota_el_token_y_el_nuevo_acceso_funciona()
    {
        var t = await LoginPacienteAsync();
        var r = await Renovar(t.TokenRenovacion);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var nuevo = (await r.Content.ReadFromJsonAsync<Tokens>())!;
        Assert.NotEqual(t.TokenRenovacion, nuevo.TokenRenovacion);
        Assert.Equal(HttpStatusCode.OK, (await Yo(nuevo.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_reutilizar_un_token_ya_rotado_revoca_la_sesion_completa()
    {
        var t = await LoginPacienteAsync();
        var nuevo = (await (await Renovar(t.TokenRenovacion)).Content.ReadFromJsonAsync<Tokens>())!;

        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(t.TokenRenovacion)).StatusCode); // el viejo
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(nuevo.TokenRenovacion)).StatusCode); // el nuevo, ya sin sesión
        var yo = await Yo(nuevo.TokenAcceso);
        Assert.Equal(HttpStatusCode.Unauthorized, yo.StatusCode);
        Assert.Equal("sesion_cerrada", await CodigoDe(yo));
    }

    [Fact]
    public async Task RF_IAM_006_un_token_de_renovacion_desconocido_devuelve_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar("inventado")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Renovar(null)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_sesion_invalida_el_acceso_y_la_renovacion()
    {
        var t = await LoginPacienteAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await Post("/api/auth/cerrar-sesion", t.TokenAcceso)).StatusCode);

        var yo = await Yo(t.TokenAcceso);
        Assert.Equal(HttpStatusCode.Unauthorized, yo.StatusCode);
        Assert.Equal("sesion_cerrada", await CodigoDe(yo));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(t.TokenRenovacion)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_todas_cierra_las_sesiones_de_todos_los_dispositivos()
    {
        var correo = NuevoUsuario(false);
        var movil = await LoginPacienteAsync(correo);
        var portatil = await LoginPacienteAsync(correo);
        var ajeno = await LoginPacienteAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await Post("/api/auth/cerrar-todas", movil.TokenAcceso)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Yo(movil.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Yo(portatil.TokenAcceso)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(portatil.TokenRenovacion)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Yo(ajeno.TokenAcceso)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_sesion_exige_estar_autenticado() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/cerrar-sesion", null)).StatusCode);

    [Fact]
    public async Task CA_IAM_003_el_personal_inactivo_15_minutos_debe_autenticarse_de_nuevo()
    {
        var t = await LoginPersonalAsync();
        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(16));

        // el acceso (15 min) ya caducó: el cliente intenta renovar...
        var yo = await Yo(t.TokenAcceso);
        Assert.Equal(HttpStatusCode.Unauthorized, yo.StatusCode);
        Assert.Equal("token_expirado", await CodigoDe(yo));

        // ...pero la sesión también cerró por inactividad: hay que pedir las credenciales otra vez
        var renovar = await Renovar(t.TokenRenovacion);
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
        Assert.Equal("sesion_expirada", await CodigoDe(renovar));
    }

    [Fact]
    public async Task RF_IAM_006_usar_la_sesion_dentro_de_la_ventana_permite_seguir_renovando()
    {
        var t = await LoginPersonalAsync();
        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.Equal(HttpStatusCode.OK, (await Yo(t.TokenAcceso)).StatusCode); // actividad
        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(10));                       // 20 min desde el inicio, 10 desde la actividad

        var renovar = await Renovar(t.TokenRenovacion);
        Assert.Equal(HttpStatusCode.OK, renovar.StatusCode);
    }
}

public class InactividadApiTests(FabricaApiInactividadCorta fabrica)
    : SesionesApiBase(fabrica), IClassFixture<FabricaApiInactividadCorta>
{
    [Fact]
    public async Task CA_IAM_003_con_el_acceso_aun_vigente_la_sesion_inactiva_devuelve_sesion_expirada()
    {
        var t = await LoginPacienteAsync();
        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(6)); // acceso válido 15 min, inactividad 5 min

        var yo = await Yo(t.TokenAcceso);
        Assert.Equal(HttpStatusCode.Unauthorized, yo.StatusCode);
        Assert.Equal("sesion_expirada", await CodigoDe(yo));
    }

    [Fact]
    public async Task RF_IAM_006_las_solicitudes_activas_mantienen_viva_la_sesion()
    {
        var t = await LoginPacienteAsync();
        for (var i = 0; i < 3; i++)
        {
            Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(4));
            Assert.Equal(HttpStatusCode.OK, (await Yo(t.TokenAcceso)).StatusCode);
        }
    }
}
