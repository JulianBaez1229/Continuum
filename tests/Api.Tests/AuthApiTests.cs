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
    public Task AgregarAsync(Usuario usuario, CancellationToken ct = default) { _usuarios.Add(usuario); return Task.CompletedTask; }
    public Task GuardarAsync(Usuario usuario, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class SesionesEnMemoria : ISesiones
{
    private readonly List<Sesion> _sesiones = [];
    public Task AgregarAsync(Sesion s, CancellationToken ct = default) { _sesiones.Add(s); return Task.CompletedTask; }
    public Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_sesiones.FirstOrDefault(s => s.Id == id));
    public Task<Sesion?> ObtenerPorHashRenovacionAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(_sesiones.FirstOrDefault(s => s.HashRenovacion == hash || s.HashRenovacionAnterior == hash));
    public Task<IReadOnlyList<Sesion>> ObtenerNoRevocadasDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Sesion>>(_sesiones.Where(s => s.UsuarioId == usuarioId && s.RevocadaEn is null).ToList());
    public Task GuardarAsync(IEnumerable<Sesion> sesiones, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class TokensEnMemoria : ITokensAccion
{
    private readonly List<TokenAccion> _tokens = [];
    public Task AgregarAsync(TokenAccion t, CancellationToken ct = default) { _tokens.Add(t); return Task.CompletedTask; }
    public Task<TokenAccion?> ObtenerPorHashAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(_tokens.FirstOrDefault(t => t.HashToken == hash));
    public Task<IReadOnlyList<TokenAccion>> ObtenerPendientesDePacienteAsync(Guid pacienteId, PropositoToken proposito, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TokenAccion>>(_tokens.Where(t => t.PacienteId == pacienteId && t.Proposito == proposito && t.UsadoEn is null).ToList());
    public Task<IReadOnlyList<TokenAccion>> ObtenerPendientesDeUsuarioAsync(Guid usuarioId, PropositoToken proposito, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TokenAccion>>(_tokens.Where(t => t.UsuarioId == usuarioId && t.Proposito == proposito && t.UsadoEn is null).ToList());
    public Task GuardarAsync(IEnumerable<TokenAccion> tokens, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Captura lo que el módulo 17 enviaría, para poder completar los flujos por correo en las pruebas.</summary>
internal sealed class MensajeriaCaptura : IMensajeriaIdentidad
{
    public Dictionary<Guid, string> UltimoTokenRecuperacion { get; } = [];
    public List<Guid> CambiosDeContrasena { get; } = [];
    public Dictionary<Guid, string> UltimaInvitacion { get; } = [];
    public Dictionary<Guid, string> UltimoCodigoMfa { get; } = [];

    public Task EnviarEnlaceRecuperacionAsync(Usuario u, string token, DateTimeOffset expiraEn, CancellationToken ct = default)
    { UltimoTokenRecuperacion[u.Id] = token; return Task.CompletedTask; }
    public Task NotificarCambioContrasenaAsync(Usuario u, CancellationToken ct = default)
    { CambiosDeContrasena.Add(u.Id); return Task.CompletedTask; }
    public Task EnviarInvitacionPacienteAsync(Guid pacienteId, CanalInvitacion canal, string destino, string token, DateTimeOffset expiraEn, CancellationToken ct = default)
    { UltimaInvitacion[pacienteId] = token; return Task.CompletedTask; }
    public Task EnviarCodigoMfaAsync(Usuario u, string codigo, DateTimeOffset expiraEn, CancellationToken ct = default)
    { UltimoCodigoMfa[u.Id] = codigo; return Task.CompletedTask; }
}

internal sealed class DirectorioPacientesEnMemoria : IDirectorioPacientes
{
    public sealed class Ficha(Guid organizacionId, string? correo, string? telefono, DateOnly nacimiento, string ultimos4)
    {
        public Guid OrganizacionId { get; } = organizacionId;
        public string? Correo { get; } = correo;
        public string? Telefono { get; } = telefono;
        public DateOnly Nacimiento { get; } = nacimiento;
        public string Ultimos4 { get; } = ultimos4;
        public bool TieneCuenta { get; set; }
    }

    public Dictionary<Guid, Ficha> Pacientes { get; } = [];

    public Task<ContactoPaciente?> ObtenerContactoAsync(Guid pacienteId, CancellationToken ct = default) =>
        Task.FromResult(Pacientes.TryGetValue(pacienteId, out var f)
            ? new ContactoPaciente(f.OrganizacionId, f.Correo, f.Telefono, f.TieneCuenta) : null);

    public Task<bool> VerificarIdentidadAsync(Guid pacienteId, DateOnly fechaNacimiento, string ultimos4Documento, CancellationToken ct = default) =>
        Task.FromResult(Pacientes.TryGetValue(pacienteId, out var f) && f.Nacimiento == fechaNacimiento && f.Ultimos4 == ultimos4Documento);

    public Task VincularCuentaAsync(Guid pacienteId, Guid usuarioId, CancellationToken ct = default)
    { Pacientes[pacienteId].TieneCuenta = true; return Task.CompletedTask; }
}

internal sealed class ConsentimientosEnMemoria : IConsentimientos
{
    public const string Terminos = "terminos-2026-10";
    public const string Aviso = "aviso-2026-10";
    public List<(Guid PacienteId, string Tipo, string Version)> Registrados { get; } = [];

    public VersionesLegales Vigentes(Guid organizacionId) => new(Terminos, Aviso);
    public Task RegistrarAceptacionAsync(Guid pacienteId, string tipo, string versionTexto, Guid firmadoPor, DateTimeOffset en, CancellationToken ct = default)
    { Registrados.Add((pacienteId, tipo, versionTexto)); return Task.CompletedTask; }
}

public class FabricaApi : WebApplicationFactory<Program>
{
    internal RelojDePrueba Reloj { get; } = new();
    internal RepositorioEnMemoria Repositorio { get; } = new();
    internal SesionesEnMemoria Sesiones { get; } = new();
    internal TokensEnMemoria Tokens { get; } = new();
    internal MensajeriaCaptura Mensajeria { get; } = new();
    internal DirectorioPacientesEnMemoria Directorio { get; } = new();
    internal ConsentimientosEnMemoria Consentimientos { get; } = new();

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
            s.RemoveAll<ISesiones>();
            s.AddSingleton<ISesiones>(Sesiones);
            s.RemoveAll<ITokensAccion>();
            s.AddSingleton<ITokensAccion>(Tokens);
            s.RemoveAll<IMensajeriaIdentidad>();
            s.AddSingleton<IMensajeriaIdentidad>(Mensajeria);
            s.RemoveAll<IDirectorioPacientes>();
            s.AddSingleton<IDirectorioPacientes>(Directorio);
            s.RemoveAll<IConsentimientos>();
            s.AddSingleton<IConsentimientos>(Consentimientos);
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

    private sealed record Login_(
        string Estado, string? TokenAcceso, string? TokenDesafio, DateTimeOffset? BloqueadoHasta, string? TokenRenovacion);
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
