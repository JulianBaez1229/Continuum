using System.Security.Cryptography;
using System.Text;

namespace Continuum.Identidad.Aplicacion;

/// <summary>Tokens opacos de 256 bits para renovación, recuperación e invitaciones. Solo se persiste el hash.</summary>
public static class GeneradorTokens
{
    public static string Nuevo() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>SHA-256: basta porque el token tiene 256 bits de entropía (no es una contraseña elegida por una persona).</summary>
    public static string HashDe(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
