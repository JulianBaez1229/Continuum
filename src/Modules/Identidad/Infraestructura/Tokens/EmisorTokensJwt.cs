using System.Security.Claims;
using System.Text;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Continuum.Identidad.Infraestructura.Tokens;

public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";
    private const int LongitudMinimaClave = 32;

    /// <summary>Secreto HMAC. Solo por variable de entorno o gestor de secretos, nunca en el repositorio.</summary>
    public string Clave { get; set; } = "";
    public string Emisor { get; set; } = "continuum";
    public string AudienciaAcceso { get; set; } = "continuum-api";
    public string AudienciaDesafio { get; set; } = "continuum-desafio";
    public int MinutosAcceso { get; set; } = 15;
    public int MinutosDesafio { get; set; } = 5;

    public SymmetricSecurityKey LlaveFirma => new(Encoding.UTF8.GetBytes(Clave));

    public void Validar()
    {
        if (Encoding.UTF8.GetByteCount(Clave) < LongitudMinimaClave)
            throw new InvalidOperationException(
                $"Jwt:Clave debe tener al menos {LongitudMinimaClave} bytes (defínela con la variable Jwt__Clave).");
    }
}

public sealed class EmisorTokensJwt : IEmisorTokens
{
    private const string ClaveOrganizacion = "org";
    private const string ClaveProposito = "prp";
    public const string ClaveSesion = "sid";
    /// <summary>Segundos Unix de la última reautenticación (contraseña + MFA), RF-IAM-010.</summary>
    public const string ClaveReautenticacion = "rea";
    private readonly OpcionesJwt _opciones;
    private readonly IReloj _reloj;
    private readonly JsonWebTokenHandler _manejador = new();

    public EmisorTokensJwt(OpcionesJwt opciones, IReloj reloj)
    {
        opciones.Validar();
        _opciones = opciones;
        _reloj = reloj;
    }

    public string EmitirAcceso(Usuario usuario, Guid sesionId, DateTimeOffset? reautenticadoEn = null)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
            [ClaveOrganizacion] = usuario.OrganizacionId.ToString(),
            [ClaveSesion] = sesionId.ToString(),
        };
        if (reautenticadoEn is { } rea) claims[ClaveReautenticacion] = rea.ToUnixTimeSeconds();
        return Emitir(_opciones.AudienciaAcceso, TimeSpan.FromMinutes(_opciones.MinutosAcceso), claims);
    }

    public string EmitirDesafio(Guid usuarioId, PropositoDesafio proposito) => Emitir(
        _opciones.AudienciaDesafio, TimeSpan.FromMinutes(_opciones.MinutosDesafio),
        new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = usuarioId.ToString(),
            [ClaveProposito] = proposito.ToString(),
        });

    public Guid? ValidarDesafio(string token, PropositoDesafio proposito)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parametros = new TokenValidationParameters
        {
            ValidIssuer = _opciones.Emisor,
            ValidAudience = _opciones.AudienciaDesafio,
            IssuerSigningKey = _opciones.LlaveFirma,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (desde, hasta, _, _) => VigenteEn(desde, hasta, _reloj.Ahora),
        };

        var resultado = _manejador.ValidateTokenAsync(token, parametros).GetAwaiter().GetResult();
        if (!resultado.IsValid) return null;

        var claims = resultado.ClaimsIdentity;
        if (claims.FindFirst(ClaveProposito)?.Value != proposito.ToString()) return null;
        return Guid.TryParse(claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
    }

    /// <summary>Vigencia contra el reloj de la aplicación (misma fuente de tiempo al emitir y al validar).</summary>
    public static bool VigenteEn(DateTime? desde, DateTime? hasta, DateTimeOffset ahora) =>
        hasta is not null && hasta.Value > ahora.UtcDateTime && (desde is null || desde.Value <= ahora.UtcDateTime);

    private string Emitir(string audiencia, TimeSpan vigencia, Dictionary<string, object> claims)
    {
        var ahora = _reloj.Ahora.UtcDateTime;
        claims[JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString();
        return _manejador.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = audiencia,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = ahora + vigencia,
            Claims = claims,
            SigningCredentials = new SigningCredentials(_opciones.LlaveFirma, SecurityAlgorithms.HmacSha256),
        });
    }
}
