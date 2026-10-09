using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Infraestructura.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Continuum.Identidad.Infraestructura.Seguridad;

/// <summary>El acceso debe llevar una reautenticación (contraseña + MFA) de hace menos de N minutos (RF-IAM-010).</summary>
public sealed class ReautenticacionRecienteRequirement : IAuthorizationRequirement;

public sealed class ReautenticacionRecienteHandler(IReloj reloj, OpcionesSesion opciones)
    : AuthorizationHandler<ReautenticacionRecienteRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto, ReautenticacionRecienteRequirement requisito)
    {
        var valor = contexto.User.FindFirst(EmisorTokensJwt.ClaveReautenticacion)?.Value;
        if (!long.TryParse(valor, out var segundos)) return Task.CompletedTask;

        var transcurrido = reloj.Ahora.ToUnixTimeSeconds() - segundos;
        if (transcurrido >= 0 && transcurrido < opciones.ReautenticacionMinutos * 60L)
            contexto.Succeed(requisito);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Cuando una acción crítica se niega solo por falta de reautenticación, responde 403 con
/// <c>codigo: reautenticacion_requerida</c> para que el cliente pida la contraseña y el segundo factor.
/// </summary>
public sealed class ResultadoAutorizacionIdentidad : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _predeterminado = new();

    public async Task HandleAsync(RequestDelegate siguiente, HttpContext contexto, AuthorizationPolicy politica, PolicyAuthorizationResult resultado)
    {
        if (resultado.Forbidden && resultado.AuthorizationFailure is { } fallo
            && fallo.FailedRequirements.OfType<ReautenticacionRecienteRequirement>().Any())
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Reautenticación requerida",
                Detail = "Confirma tu contraseña y segundo factor para continuar.",
            };
            problema.Extensions["codigo"] = "reautenticacion_requerida";
            await contexto.Response.WriteAsJsonAsync(problema, contexto.RequestAborted);
            return;
        }
        await _predeterminado.HandleAsync(siguiente, contexto, politica, resultado);
    }
}
