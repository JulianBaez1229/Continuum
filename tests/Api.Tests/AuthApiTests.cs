using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Continuum.Api.Tests;

internal sealed class RelojDePrueba : IReloj
{
    public DateTimeOffset Ahora { get; private set; } = DateTimeOffset.UtcNow;
    public void Avanzar(TimeSpan t) => Ahora += t;
}

internal sealed class RepositorioEnMemoria : IRepositorioUsuarios
{
    private readonly List<Usuario> _usuarios = [];
    public void Agregar(Usuario u) => _usuarios.Add(u);
    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Correo == Usuario.NormalizarCorreo(correo)));
    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));
    public Task GuardarAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class FabricaApi : WebApplicationFactory<Program>
{
    internal RelojDePrueba Reloj { get; } = new();
    internal RepositorioEnMemoria Repositorio { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Datos ficticios de prueba; el repositorio real (PostgreSQL) se sustituye por uno en memoria.
        builder.UseSetting("Jwt:Clave", new string('t', 48));
        builder.UseSetting("ConnectionStrings:Continuum", "Host=no-se-usa;Database=prueba");
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<IReloj>();
            s.AddSingleton<IReloj>(Reloj);
            s.RemoveAll<IRepositorioUsuarios>();
            s.AddSingleton<IRepositorioUsuarios>(Repositorio);
        });
    }
}

public class AuthApiTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const string Clave = "una clave larga y segura";
    private readonly HttpClient _http = fabrica.CreateClient();

    private string NuevoPersonal(bool requiereMfa = true)
    {
        var correo = $"u{Guid.NewGuid():N}@clinica.test";
        fabrica.Repositorio.Agregar(Usuario.Crear(
            Guid.NewGuid(), correo, new Infraestructura().Hash(Clave), requiereMfa));
        return correo;
    }

    private Task<HttpResponseMessage> Login(string correo, string clave = Clave) =>
        _http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = clave });

    private static async Task<T> Leer<T>(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<T>())!;

    private sealed record Login_(string Estado, string? TokenAcceso, string? TokenDesafio, DateTimeOffset? BloqueadoHasta);
    private sealed record InicioMfa_(string Secreto, string UriOtpAuth);
    private sealed record Confirmacion_(string[] CodigosRecuperacion);

    private static string Codigo(string secreto, DateTimeOffset ahora) =>
        Totp.Generar(Base32.Decodificar(secreto), Totp.PasoDe(ahora));

    /// <summary>Recorre el alta de MFA y devuelve el secreto.</summary>
    private async Task<string> ConfigurarMfa(string correo)
    {
        var login = await Leer<Login_>(await Login(correo));
        var inicio = await Leer<InicioMfa_>(await _http.PostAsJsonAsync(
            "/api/auth/mfa/configuracion/iniciar", new { tokenDesafio = login.TokenDesafio }));
        var confirmar = await _http.PostAsJsonAsync("/api/auth/mfa/configuracion/confirmar",
            new { tokenDesafio = login.TokenDesafio, codigo = Codigo(inicio.Secreto, fabrica.Reloj.Ahora) });
        Assert.Equal(HttpStatusCode.OK, confirmar.StatusCode);
        Assert.Equal(10, (await Leer<Confirmacion_>(confirmar)).CodigosRecuperacion.Length);
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(Totp.PasoSegundos));
        return inicio.Secreto;
    }

    [Fact]
    public async Task RF_IAM_001_credenciales_incorrectas_devuelven_401_sin_detalle()
    {
        var r = await Login(NuevoPersonal(), "incorrecta");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        var cuerpo = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("TokenAcceso", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RF_IAM_001_usuario_inexistente_devuelve_el_mismo_401()
    {
        var r = await Login("nadie@clinica.test");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task RF_IAM_001_falta_de_campos_devuelve_400()
    {
        var r = await _http.PostAsJsonAsync("/api/auth/login", new { correo = "a@b.test" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task CA_IAM_001_el_primer_login_del_profesional_exige_configurar_mfa_y_no_da_acceso()
    {
        var r = await Leer<Login_>(await Login(NuevoPersonal()));
        Assert.Equal("RequiereConfiguracionMfa", r.Estado);
        Assert.Null(r.TokenAcceso);
        Assert.NotNull(r.TokenDesafio);
    }

    [Fact]
    public async Task RF_IAM_002_flujo_completo_clave_mfa_y_acceso_a_un_recurso_protegido()
    {
        var correo = NuevoPersonal();
        var secreto = await ConfigurarMfa(correo);

        var login = await Leer<Login_>(await Login(correo));
        Assert.Equal("RequiereSegundoFactor", login.Estado);
        Assert.Null(login.TokenAcceso);

        var verificado = await Leer<Login_>(await _http.PostAsJsonAsync("/api/auth/mfa/verificar",
            new { tokenDesafio = login.TokenDesafio, codigo = Codigo(secreto, fabrica.Reloj.Ahora) }));
        Assert.Equal("Exitoso", verificado.Estado);

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", verificado.TokenAcceso);
        var yo = await _http.SendAsync(peticion);
        Assert.Equal(HttpStatusCode.OK, yo.StatusCode);
        Assert.Contains("organizacionId", await yo.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RF_IAM_002_un_codigo_incorrecto_no_da_acceso()
    {
        var correo = NuevoPersonal();
        await ConfigurarMfa(correo);
        var login = await Leer<Login_>(await Login(correo));
        var r = await _http.PostAsJsonAsync("/api/auth/mfa/verificar",
            new { tokenDesafio = login.TokenDesafio, codigo = "000000" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_un_token_de_desafio_no_sirve_como_token_de_acceso()
    {
        var login = await Leer<Login_>(await Login(NuevoPersonal()));
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.TokenDesafio);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.SendAsync(peticion)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_006_sin_token_el_recurso_protegido_devuelve_401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.GetAsync("/api/auth/yo")).StatusCode);

    [Fact]
    public async Task RF_IAM_006_un_token_manipulado_se_rechaza()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "aaa.bbb.ccc");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.SendAsync(peticion)).StatusCode);
    }

    [Fact]
    public async Task CA_IAM_002_cinco_fallos_bloquean_y_el_siguiente_intento_devuelve_423()
    {
        var correo = NuevoPersonal();
        for (var i = 0; i < 5; i++) await Login(correo, "incorrecta");

        var r = await Login(correo);
        Assert.Equal(HttpStatusCode.Locked, r.StatusCode);
        Assert.NotNull((await Leer<Login_>(r)).BloqueadoHasta);
    }

    [Fact]
    public async Task RF_IAM_001_un_paciente_sin_mfa_obtiene_acceso_directo()
    {
        var r = await Leer<Login_>(await Login(NuevoPersonal(requiereMfa: false)));
        Assert.Equal("Exitoso", r.Estado);
        Assert.NotNull(r.TokenAcceso);
    }

    private sealed class Infraestructura
    {
        public string Hash(string clave) =>
            new Continuum.Identidad.Infraestructura.Seguridad.HasheadorContrasenaIdentity().Hashear(clave);
    }
}
