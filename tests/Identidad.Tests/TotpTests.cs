using System.Text;
using Continuum.Identidad.Aplicacion;

namespace Continuum.Identidad.Tests;

public class TotpTests
{
    // Secreto de prueba del RFC 6238 (apéndice B).
    private static readonly byte[] Secreto = Encoding.ASCII.GetBytes("12345678901234567890");
    private static readonly DateTimeOffset T59 = DateTimeOffset.FromUnixTimeSeconds(59);

    [Fact]
    public void RF_IAM_002_genera_el_codigo_del_vector_del_RFC_6238() =>
        Assert.Equal("287082", Totp.Generar(Secreto, Totp.PasoDe(T59)));

    [Fact]
    public void RF_IAM_002_acepta_el_paso_anterior_y_el_siguiente()
    {
        var paso = Totp.PasoDe(T59);
        Assert.True(Totp.TryValidar(Secreto, Totp.Generar(Secreto, paso - 1), T59, null, out _));
        Assert.True(Totp.TryValidar(Secreto, Totp.Generar(Secreto, paso + 1), T59, null, out _));
    }

    [Fact]
    public void RF_IAM_002_rechaza_codigos_fuera_de_la_ventana()
    {
        var paso = Totp.PasoDe(T59);
        Assert.False(Totp.TryValidar(Secreto, Totp.Generar(Secreto, paso + 2), T59, null, out _));
    }

    [Fact]
    public void RF_IAM_002_rechaza_un_paso_ya_usado()
    {
        var paso = Totp.PasoDe(T59);
        var codigo = Totp.Generar(Secreto, paso);
        Assert.True(Totp.TryValidar(Secreto, codigo, T59, null, out var usado));
        Assert.Equal(paso, usado);
        Assert.False(Totp.TryValidar(Secreto, codigo, T59, usado, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("abcdef")]
    public void RF_IAM_002_rechaza_formatos_invalidos(string? codigo) =>
        Assert.False(Totp.TryValidar(Secreto, codigo, T59, null, out _));

    [Fact]
    public void RF_IAM_002_acepta_el_codigo_con_espacios()
    {
        var codigo = Totp.Generar(Secreto, Totp.PasoDe(T59));
        Assert.True(Totp.TryValidar(Secreto, codigo[..3] + " " + codigo[3..], T59, null, out _));
    }

    [Fact]
    public void RF_IAM_002_base32_ida_y_vuelta()
    {
        var texto = Base32.Codificar(Secreto);
        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", texto);
        Assert.Equal(Secreto, Base32.Decodificar(texto));
    }
}
