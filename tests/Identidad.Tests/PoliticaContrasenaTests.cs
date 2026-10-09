using Continuum.Identidad.Aplicacion;

namespace Continuum.Identidad.Tests;

public class PoliticaContrasenaTests
{
    private readonly PoliticaContrasena _politica = new(new FiltradasFalsas("passwordpassword"));

    [Fact]
    public void RF_IAM_001_contrasena_de_12_caracteres_sin_composicion_es_valida() =>
        Assert.True(_politica.Validar("solo minusculas").EsValida);

    [Fact]
    public void RF_IAM_001_contrasena_de_11_caracteres_se_rechaza()
    {
        var r = _politica.Validar("abcdefghijk");
        Assert.False(r.EsValida);
        Assert.Equal(MotivoContrasenaInvalida.Corta, r.Motivo);
    }

    [Fact]
    public void RF_IAM_001_contrasena_filtrada_se_rechaza()
    {
        var r = _politica.Validar("passwordpassword");
        Assert.False(r.EsValida);
        Assert.Equal(MotivoContrasenaInvalida.Filtrada, r.Motivo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RF_IAM_001_contrasena_vacia_se_rechaza(string? valor) =>
        Assert.False(_politica.Validar(valor).EsValida);
}
