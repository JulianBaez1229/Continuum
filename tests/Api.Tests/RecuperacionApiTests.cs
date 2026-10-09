using System.Net;
using System.Net.Http.Json;

namespace Continuum.Api.Tests;

public class RecuperacionApiTests(FabricaApi fabrica) : SesionesApiBase(fabrica), IClassFixture<FabricaApi>
{
    private const string ClaveVieja = "una clave larga y segura";
    private const string ClaveNueva = "otra clave nueva y larga";

    private Task<HttpResponseMessage> Solicitar(string correo) =>
        Http.PostAsJsonAsync("/api/auth/recuperacion/solicitar", new { correo });

    private Task<HttpResponseMessage> Restablecer(string? token, string clave) =>
        Http.PostAsJsonAsync("/api/auth/recuperacion/restablecer", new { token, nuevaContrasena = clave });

    private async Task<(string Correo, Guid Id)> UsuarioAsync()
    {
        var correo = NuevoUsuario(personal: false);
        var usuario = (await Fabrica.Repositorio.ObtenerPorCorreoAsync(correo))!;
        return (correo, usuario.Id);
    }

    private Task<HttpResponseMessage> Login(string correo, string clave) =>
        Http.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = clave });

    [Fact]
    public async Task RF_IAM_005_solicitar_responde_202_igual_exista_o_no_el_correo()
    {
        var (correo, id) = await UsuarioAsync();

        var existe = await Solicitar(correo);
        var noExiste = await Solicitar("nadie@clinica.test");

        Assert.Equal(HttpStatusCode.Accepted, existe.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, noExiste.StatusCode);
        Assert.Equal(await existe.Content.ReadAsStringAsync(), await noExiste.Content.ReadAsStringAsync());
        Assert.True(Fabrica.Mensajeria.UltimoTokenRecuperacion.ContainsKey(id));
    }

    [Fact]
    public async Task RF_IAM_005_flujo_completo_cambia_la_contrasena_y_cierra_las_sesiones()
    {
        var (correo, id) = await UsuarioAsync();
        var abierta = await LoginPacienteAsync(correo);

        await Solicitar(correo);
        var respuesta = await Restablecer(Fabrica.Mensajeria.UltimoTokenRecuperacion[id], ClaveNueva);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(correo, ClaveNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(correo, ClaveVieja)).StatusCode);
        Assert.Contains(id, Fabrica.Mensajeria.CambiosDeContrasena);

        var yo = await Yo(abierta.TokenAcceso);
        Assert.Equal(HttpStatusCode.Unauthorized, yo.StatusCode);
        Assert.Equal("sesion_cerrada", await CodigoDe(yo));
    }

    [Fact]
    public async Task RF_IAM_005_el_enlace_no_se_puede_usar_dos_veces()
    {
        var (correo, id) = await UsuarioAsync();
        await Solicitar(correo);
        var token = Fabrica.Mensajeria.UltimoTokenRecuperacion[id];

        Assert.Equal(HttpStatusCode.NoContent, (await Restablecer(token, ClaveNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Restablecer(token, "una tercera clave larga")).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_005_el_enlace_vence_a_los_30_minutos()
    {
        var (correo, id) = await UsuarioAsync();
        await Solicitar(correo);
        Fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(31));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Restablecer(Fabrica.Mensajeria.UltimoTokenRecuperacion[id], ClaveNueva)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_001_una_contrasena_debil_devuelve_422_y_no_gasta_el_enlace()
    {
        var (correo, id) = await UsuarioAsync();
        await Solicitar(correo);
        var token = Fabrica.Mensajeria.UltimoTokenRecuperacion[id];

        var corta = await Restablecer(token, "corta");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corta.StatusCode);
        Assert.Contains("Corta", await corta.Content.ReadAsStringAsync());

        var filtrada = await Restablecer(token, "password1234");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, filtrada.StatusCode);
        Assert.Contains("Filtrada", await filtrada.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await Restablecer(token, ClaveNueva)).StatusCode);
    }

    [Fact]
    public async Task RF_IAM_005_un_token_inventado_devuelve_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Restablecer("inventado", ClaveNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Restablecer(null, ClaveNueva)).StatusCode);
    }
}
