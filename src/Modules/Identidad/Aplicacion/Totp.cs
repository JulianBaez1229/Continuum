using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Continuum.Identidad.Aplicacion;

/// <summary>TOTP según RFC 6238 (HMAC-SHA1, pasos de 30 s, 6 dígitos), compatible con apps de autenticación.</summary>
public static class Totp
{
    public const int PasoSegundos = 30;
    public const int Digitos = 6;
    private const int PasosDeTolerancia = 1;

    public static long PasoDe(DateTimeOffset instante) => instante.ToUnixTimeSeconds() / PasoSegundos;

    public static string Generar(byte[] secreto, long paso)
    {
        Span<byte> mensaje = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(mensaje, paso);
        var hash = HMACSHA1.HashData(secreto, mensaje);
        var desplazamiento = hash[^1] & 0x0F;
        var binario = ((hash[desplazamiento] & 0x7F) << 24)
                      | (hash[desplazamiento + 1] << 16)
                      | (hash[desplazamiento + 2] << 8)
                      | hash[desplazamiento + 3];
        return (binario % 1_000_000).ToString("D6");
    }

    /// <summary>
    /// Valida el código en la ventana ±1 paso. Solo acepta pasos posteriores a <paramref name="ultimoPaso"/>
    /// para impedir que un código observado se reutilice.
    /// </summary>
    public static bool TryValidar(byte[] secreto, string? codigo, DateTimeOffset ahora, long? ultimoPaso, out long pasoValido)
    {
        pasoValido = 0;
        var limpio = (codigo ?? "").Replace(" ", "");
        if (limpio.Length != Digitos || !limpio.All(char.IsAsciiDigit)) return false;

        var actual = PasoDe(ahora);
        for (var paso = actual - PasosDeTolerancia; paso <= actual + PasosDeTolerancia; paso++)
        {
            if (ultimoPaso is { } ultimo && paso <= ultimo) continue;
            var esperado = Encoding.ASCII.GetBytes(Generar(secreto, paso));
            if (CryptographicOperations.FixedTimeEquals(esperado, Encoding.ASCII.GetBytes(limpio)))
            {
                pasoValido = paso;
                return true;
            }
        }
        return false;
    }
}

/// <summary>Base32 de RFC 4648 sin relleno.</summary>
public static class Base32
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Codificar(ReadOnlySpan<byte> datos)
    {
        var resultado = new StringBuilder((datos.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in datos)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                resultado.Append(Alfabeto[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) resultado.Append(Alfabeto[(buffer << (5 - bits)) & 31]);
        return resultado.ToString();
    }

    public static byte[] Decodificar(string texto)
    {
        var salida = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in texto.TrimEnd('=').ToUpperInvariant())
        {
            var valor = Alfabeto.IndexOf(c);
            if (valor < 0) throw new FormatException("Carácter inválido en Base32.");
            buffer = (buffer << 5) | valor;
            bits += 5;
            if (bits >= 8)
            {
                salida.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return [.. salida];
    }

    public static string Aleatorio(int caracteres)
    {
        var resultado = new StringBuilder(caracteres);
        for (var i = 0; i < caracteres; i++)
            resultado.Append(Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]);
        return resultado.ToString();
    }
}
