using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Continuum.Identidad.Infraestructura.Tokens;

/// <summary>
/// Cada solicitud autenticada comprueba que su sesión siga viva (cierre por inactividad, cierre remoto) y,
/// si no, responde 401 con un <c>codigo</c> que el cliente usa para decidir qué hacer:
/// <list type="bullet">
/// <item><c>token_expirado</c>: renovar con el token de renovación.</item>
/// <item><c>sesion_expirada</c>: pedir autenticación de nuevo, conservando el formulario en curso (CA-IAM-003).</item>
/// <item><c>sesion_cerrada</c>: la sesión se cerró (p. ej. "cerrar en todos mis dispositivos").</item>
/// </list>
/// </summary>
public static class EventosJwt
{
    private const string ClaveCodigo = "codigo_autenticacion";

    public static JwtBearerEvents Crear() => new()
    {
        OnAuthenticationFailed = contexto =>
        {
            if (contexto.Exception is SecurityTokenExpiredException or SecurityTokenInvalidLifetimeException)
                contexto.HttpContext.Items[ClaveCodigo] = "token_expirado";
            return Task.CompletedTask;
        },
        OnTokenValidated = ValidarSesionAsync,
        OnChallenge = ResponderAsync,
    };

    private static async Task ValidarSesionAsync(TokenValidatedContext contexto)
    {
        if (!Guid.TryParse(contexto.Principal?.FindFirst(EmisorTokensJwt.ClaveSesion)?.Value, out var sesionId))
        {
            contexto.HttpContext.Items[ClaveCodigo] = "token_invalido";
            contexto.Fail("El token no pertenece a una sesión.");
            return;
        }

        var servicio = contexto.HttpContext.RequestServices.GetRequiredService<ServicioSesiones>();
        var estado = await servicio.ValidarAsync(sesionId, contexto.HttpContext.RequestAborted);
        if (estado == EstadoSesion.Activa) return;

        contexto.HttpContext.Items[ClaveCodigo] = estado == EstadoSesion.Expirada ? "sesion_expirada" : "sesion_cerrada";
        contexto.Fail("La sesión no está activa.");
    }

    private static async Task ResponderAsync(JwtBearerChallengeContext contexto)
    {
        if (contexto.HttpContext.Items[ClaveCodigo] is not string codigo) return; // sin token: respuesta estándar

        contexto.HandleResponse();
        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
        contexto.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        var problema = new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "No autenticado" };
        problema.Extensions["codigo"] = codigo;
        await contexto.Response.WriteAsJsonAsync(problema, contexto.HttpContext.RequestAborted);
    }
}
