using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Continuum.Identidad.Aplicacion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Continuum.Api.Tests;

public class ActivacionPacienteApiTests(FabricaApi fabrica) : SesionesApiBase(fabrica), IClassFixture<FabricaApi>
{
    private static readonly DateOnly Nacimiento = new(1990, 5, 17);
    private const string Clave = "mi clave de paciente larga";
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly string _correoPaciente = $"p{Guid.NewGuid():N}@correo.test";
    private readonly Guid _organizacionId = Guid.NewGuid();

    private void RegistrarPaciente() =>
        Fabrica.Directorio.Pacientes[_pacienteId] = new(_organizacionId, _correoPaciente, "8095550100", Nacimiento, "4321");

    /// <summary>Recepción genera la invitación (la autorización del endpoint se prueba aparte).</summary>
    private async Task<string> InvitarPorServicioAsync()
    {
        RegistrarPaciente();
        using var alcance = Fabrica.Services.CreateScope();
        var r = await alcance.ServiceProvider.GetRequiredService<ServicioActivacionPaciente>()
            .InvitarAsync(_pacienteId, _organizacionId, Guid.NewGuid());
        Assert.Equal(ResultadoInvitacion.Enviada, r);
        return Fabrica.Mensajeria.UltimaInvitacion[_pacienteId];
    }

    private Task<HttpResponseMessage> Activar(string? token, string ultimos4 = "4321", string clave = Clave,
        string terminos = ConsentimientosEnMemoria.Terminos, string? correo = null) =>
        Http.PostAsJsonAsync("/api/auth/activacion/activar", new
        {
            token, fechaNacimiento = Nacimiento.ToString("yyyy-MM-dd"), ultimos4Documento = ultimos4, correo,
            contrasena = clave, versionTerminos = terminos, versionAvisoPrivacidad = ConsentimientosEnMemoria.Aviso,
        });

    // ---- Activación (pública, con el enlace) ----

    [Fact]
    public async Task RF_IAM_007_el_paciente_activa_su_cuenta_e_inicia_sesion()
    {
        var token = await InvitarPorServicioAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await Activar(token)).StatusCode);

        var login = await Http.PostAsJsonAsync("/api/auth/login", new { correo = _correoPaciente, contrasena = Clave });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Equal("Exitoso", tokens.Estado);
        Assert.Equal(HttpStatusCode.OK, (await Yo(tokens.TokenAcceso)).StatusCode);
        Assert.Equal(2, Fabrica.Consentimientos.Registrados.Count(c => c.PacienteId == _pacienteId));
    }

    [Fact]
    public async Task RF_IAM_007_datos_de_identidad_incorrectos_devuelven_422_y_cinco_errores_anulan_el_enlace()
    {
        var token = await InvitarPorServicioAsync();

        var r = await Activar(token, ultimos4: "0000");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("identidad", await MotivoDe(r));

        for (var i = 0; i < 4; i++) await Activar(token, ultimos4: "0000");
        Assert.Equal(HttpStatusCode.BadRequest, (await Activar(token)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_007_el_enlace_vence_a_las_72_horas()
    {
        var token = await InvitarPorServicioAsync();
        Fabrica.Reloj.Avanzar(TimeSpan.FromHours(73));
        Assert.Equal(HttpStatusCode.BadRequest, (await Activar(token)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_007_el_enlace_es_de_un_solo_uso()
    {
        var token = await InvitarPorServicioAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await Activar(token)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Activar(token)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_007_sin_aceptar_la_version_vigente_de_los_terminos_devuelve_422()
    {
        var token = await InvitarPorServicioAsync();
        var r = await Activar(token, terminos: "version-vieja");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("consentimiento", await MotivoDe(r));
    }

    [Fact]
    public async Task RF_IAM_001_una_contrasena_debil_devuelve_422()
    {
        var token = await InvitarPorServicioAsync();
        var r = await Activar(token, clave: "corta");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("Corta", await MotivoDe(r));
    }

    [Fact]
    public async Task RF_IAM_007_un_token_inventado_o_ausente_devuelve_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Activar("inventado")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Activar(null)).StatusCode);
    }

    // ---- Invitación (solo con el permiso del módulo 03) ----

    [Fact]
    public async Task RN_001_invitar_sin_sesion_devuelve_401()
    {
        RegistrarPaciente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Invitar(null)).StatusCode);
    }

    [Fact]
    public async Task RN_001_invitar_sin_el_permiso_devuelve_403_aunque_haya_sesion()
    {
        RegistrarPaciente();
        var sesion = await LoginPacienteAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await Invitar(sesion.TokenAcceso)).StatusCode);
        Assert.False(Fabrica.Mensajeria.UltimaInvitacion.ContainsKey(_pacienteId));
    }

    [Fact]
    public async Task RF_IAM_007_el_personal_con_permiso_genera_la_invitacion()
    {
        RegistrarPaciente();
        var token = await TokenConPermisoAsync(_organizacionId);

        Assert.Equal(HttpStatusCode.Accepted, (await Invitar(token)).StatusCode);
        Assert.True(Fabrica.Mensajeria.UltimaInvitacion.ContainsKey(_pacienteId));
    }

    [Fact]
    public async Task RN_015_invitar_a_un_paciente_de_otra_organizacion_devuelve_404()
    {
        RegistrarPaciente();
        var token = await TokenConPermisoAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await Invitar(token)).StatusCode);
        Assert.False(Fabrica.Mensajeria.UltimaInvitacion.ContainsKey(_pacienteId));
    }

    [Fact]
    public async Task RF_IAM_007_invitar_a_un_paciente_que_ya_tiene_cuenta_devuelve_409()
    {
        RegistrarPaciente();
        Fabrica.Directorio.Pacientes[_pacienteId].TieneCuenta = true;
        Assert.Equal(HttpStatusCode.Conflict, (await Invitar(await TokenConPermisoAsync(_organizacionId))).StatusCode);
    }

    private Task<HttpResponseMessage> Invitar(string? tokenAcceso) =>
        Post("/api/auth/activacion/invitar", tokenAcceso, new { pacienteId = _pacienteId });

    /// <summary>
    /// El claim <c>permiso</c> lo emitirá el módulo 03. Aquí se firma a mano un token equivalente al de una sesión
    /// real, para comprobar que la política se aplica cuando el permiso exista.
    /// </summary>
    private async Task<string> TokenConPermisoAsync(Guid organizacionId)
    {
        var real = new JsonWebTokenHandler().ReadJsonWebToken((await LoginPacienteAsync()).TokenAcceso);
        var ahora = Fabrica.Reloj.Ahora.UtcDateTime;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "continuum",
            Audience = "continuum-api",
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = real.Subject,
                ["sid"] = real.GetClaim("sid").Value,
                ["org"] = organizacionId.ToString(),
                ["permiso"] = "pacientes.invitar",
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('t', 48))), SecurityAlgorithms.HmacSha256),
        });
    }

    private static async Task<string?> MotivoDe(HttpResponseMessage r)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("motivo", out var m) ? m.GetString() : null;
    }
}
