using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Continuum.Identidad.Tests;

public class EmisorTokensJwtTests
{
    private readonly RelojFalso _reloj = new();
    private readonly OpcionesJwt _opciones = new() { Clave = new string('k', 48) };
    private readonly Usuario _usuario = Usuario.Crear(Guid.NewGuid(), "x@clinica.test", "hash");
    private readonly Guid _sesionId = Guid.NewGuid();

    private EmisorTokensJwt Emisor(OpcionesJwt? opciones = null) => new(opciones ?? _opciones, _reloj);

    [Fact]
    public void RF_IAM_006_el_token_de_acceso_dura_15_minutos_y_lleva_usuario_y_organizacion()
    {
        var token = new JsonWebTokenHandler().ReadJsonWebToken(Emisor().EmitirAcceso(_usuario, _sesionId));
        Assert.Equal(_usuario.Id.ToString(), token.Subject);
        Assert.Equal(_usuario.OrganizacionId.ToString(), token.GetClaim("org").Value);
        Assert.Equal(_sesionId.ToString(), token.GetClaim("sid").Value);
        Assert.Equal(_reloj.Ahora.AddMinutes(15).ToUnixTimeSeconds(), new DateTimeOffset(token.ValidTo).ToUnixTimeSeconds());
    }

    [Fact]
    public void RF_IAM_006_el_desafio_se_valida_para_su_proposito()
    {
        var emisor = Emisor();
        var token = emisor.EmitirDesafio(_usuario.Id, PropositoDesafio.SegundoFactor);
        Assert.Equal(_usuario.Id, emisor.ValidarDesafio(token, PropositoDesafio.SegundoFactor));
    }

    [Fact]
    public void RF_IAM_006_el_desafio_no_vale_para_otro_proposito()
    {
        var emisor = Emisor();
        var token = emisor.EmitirDesafio(_usuario.Id, PropositoDesafio.SegundoFactor);
        Assert.Null(emisor.ValidarDesafio(token, PropositoDesafio.ConfigurarMfa));
    }

    [Fact]
    public void RF_IAM_006_el_desafio_caduca_a_los_5_minutos()
    {
        var emisor = Emisor();
        var token = emisor.EmitirDesafio(_usuario.Id, PropositoDesafio.SegundoFactor);
        _reloj.Avanzar(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)));
        Assert.Null(emisor.ValidarDesafio(token, PropositoDesafio.SegundoFactor));
    }

    [Fact]
    public void RF_IAM_006_un_token_de_acceso_no_sirve_como_desafio()
    {
        var emisor = Emisor();
        Assert.Null(emisor.ValidarDesafio(emisor.EmitirAcceso(_usuario, _sesionId), PropositoDesafio.SegundoFactor));
    }

    [Fact]
    public void RF_IAM_006_un_desafio_firmado_con_otra_clave_se_rechaza()
    {
        var token = Emisor(new OpcionesJwt { Clave = new string('z', 48) })
            .EmitirDesafio(_usuario.Id, PropositoDesafio.SegundoFactor);
        Assert.Null(Emisor().ValidarDesafio(token, PropositoDesafio.SegundoFactor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no.es.un.jwt")]
    public void RF_IAM_006_texto_que_no_es_un_token_se_rechaza(string token) =>
        Assert.Null(Emisor().ValidarDesafio(token, PropositoDesafio.SegundoFactor));


    [Fact]
    public void RF_IAM_010_el_acceso_reautenticado_lleva_la_marca_y_el_normal_no()
    {
        var emisor = Emisor();
        var manejador = new JsonWebTokenHandler();

        var normal = manejador.ReadJsonWebToken(emisor.EmitirAcceso(_usuario, _sesionId));
        Assert.False(normal.TryGetClaim("rea", out _));

        var reautenticado = manejador.ReadJsonWebToken(emisor.EmitirAcceso(_usuario, _sesionId, _reloj.Ahora));
        Assert.Equal(_reloj.Ahora.ToUnixTimeSeconds(), reautenticado.GetClaim("rea").Value is { } v ? long.Parse(v) : 0);
    }
    [Fact]
    public void RF_IAM_006_una_clave_corta_impide_arrancar()
    {
        var corta = new OpcionesJwt { Clave = "corta" };
        Assert.Throws<InvalidOperationException>(() => corta.Validar());
    }
}
