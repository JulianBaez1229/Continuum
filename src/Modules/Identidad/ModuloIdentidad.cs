using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Controllers;
using Continuum.Identidad.Infraestructura.Persistencia;
using Continuum.Identidad.Infraestructura.Seguridad;
using Continuum.Identidad.Infraestructura.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Continuum.Identidad;

public static class ModuloIdentidad
{
    public const string CadenaConexion = "Continuum";

    /// <summary>
    /// Requiere <c>Jwt:Clave</c> (≥ 32 bytes) y <c>ConnectionStrings:Continuum</c>, ambos por variables de entorno
    /// (<c>Jwt__Clave</c>, <c>ConnectionStrings__Continuum</c>).
    /// </summary>
    public static IServiceCollection AddIdentidad(this IServiceCollection servicios, IConfiguration config)
    {
        var jwt = config.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();
        jwt.Validar();

        servicios.AddSingleton(jwt);
        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton<IHasheadorContrasena, HasheadorContrasenaIdentity>();
        servicios.AddDataProtection();
        servicios.AddSingleton<IProtectorSecretos, ProtectorSecretosDataProtection>();
        servicios.AddSingleton<IEmisorTokens, EmisorTokensJwt>();

        servicios.AddDbContext<IdentidadDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString(CadenaConexion)
                        ?? throw new InvalidOperationException("Falta ConnectionStrings:Continuum.")));
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuariosEf>();
        servicios.AddScoped<IAuditoriaIdentidad, AuditoriaIdentidadProvisional>();
        servicios.AddScoped<IAvisosSeguridad, AvisosSeguridadProvisional>();

        var opcionesSesion = config.GetSection(OpcionesSesion.Seccion).Get<OpcionesSesion>() ?? new OpcionesSesion();
        servicios.AddSingleton(opcionesSesion);
        servicios.AddSingleton<IPoliticaSesion, PoliticaSesionConfigurada>();
        servicios.AddScoped<ISesiones, RepositorioSesionesEf>();
        servicios.AddScoped<ServicioSesiones>();

        servicios.AddScoped<ITokensAccion, RepositorioTokensAccionEf>();
        servicios.AddScoped<IMensajeriaIdentidad, MensajeriaIdentidadProvisional>();
        servicios.AddSingleton<IContrasenasFiltradas, ContrasenasFiltradasLista>();
        servicios.AddSingleton<PoliticaContrasena>();
        servicios.AddScoped<ServicioRecuperacionContrasena>();

        servicios.AddScoped<IDirectorioPacientes, DirectorioPacientesNoDisponible>();
        servicios.AddScoped<IConsentimientos, ConsentimientosNoDisponibles>();
        servicios.AddScoped<ServicioActivacionPaciente>();

        servicios.AddScoped<ServicioInicioSesion>();
        servicios.AddScoped<ServicioMfa>();
        servicios.AddScoped<ServicioAutenticacion>();

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.Events = EventosJwt.Crear();
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Emisor,
                    ValidAudience = jwt.AudienciaAcceso,
                    IssuerSigningKey = jwt.LlaveFirma,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
        servicios.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IReloj>((o, reloj) => o.TokenValidationParameters.LifetimeValidator =
                (desde, hasta, _, _) => EmisorTokensJwt.VigenteEn(desde, hasta, reloj.Ahora));
        servicios.AddAuthorization(o => o.AddPolicy(PoliticasIdentidad.InvitarPacientes, p => p
            .RequireAuthenticatedUser()
            .RequireClaim(PoliticasIdentidad.ClavePermiso, PoliticasIdentidad.PermisoInvitarPacientes)));

        servicios.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        return servicios;
    }
}
