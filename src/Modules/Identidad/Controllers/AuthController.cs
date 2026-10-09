using System.ComponentModel.DataAnnotations;
using Continuum.Identidad.Aplicacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Controllers;

public sealed record SolicitudLogin([Required] string Correo, [Required] string Contrasena);
public sealed record SolicitudCodigo([Required] string TokenDesafio, [Required] string Codigo);
public sealed record SolicitudDesafio([Required] string TokenDesafio);
public sealed record SolicitudRenovacion([Required] string TokenRenovacion);
public sealed record SolicitudRecuperacion([Required] string Correo);
public sealed record SolicitudRestablecimiento([Required] string Token, [Required] string NuevaContrasena);

public sealed record RespuestaLogin(
    string Estado, string? TokenAcceso, string? TokenDesafio, DateTimeOffset? BloqueadoHasta, string? TokenRenovacion = null);
public sealed record RespuestaInicioMfa(string Secreto, string UriOtpAuth);
public sealed record RespuestaConfirmacionMfa(IReadOnlyList<string> CodigosRecuperacion);

/// <summary>Dos solicitudes simultáneas sobre la misma cuenta: se deniega la perdedora (nunca se trata como éxito).</summary>
public sealed class ConflictoConcurrenciaAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto de concurrencia",
            Detail = "Vuelve a intentarlo.",
        }) { StatusCode = StatusCodes.Status409Conflict };
        context.ExceptionHandled = true;
    }
}

[ApiController]
[Route("api/auth")]
[ConflictoConcurrencia]
public sealed class AuthController(ServicioAutenticacion auth, ServicioRecuperacionContrasena recuperacion) : ControllerBase
{
    private static ObjectResult Credenciales(string? codigo = null)
    {
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No autenticado",
            Detail = codigo is null ? "Credenciales inválidas." : "La sesión expiró; inicia sesión de nuevo.",
        };
        if (codigo is not null) problema.Extensions["codigo"] = codigo;
        return new ObjectResult(problema) { StatusCode = StatusCodes.Status401Unauthorized };
    }

    private IActionResult Responder(RespuestaAutenticacion r) => r.Estado switch
    {
        EstadoInicioSesion.CredencialesInvalidas => Credenciales(),
        EstadoInicioSesion.SesionExpirada => Credenciales("sesion_expirada"),
        EstadoInicioSesion.CuentaBloqueada => StatusCode(StatusCodes.Status423Locked,
            new RespuestaLogin(r.Estado.ToString(), null, null, r.BloqueadoHasta)),
        _ => Ok(new RespuestaLogin(r.Estado.ToString(), r.TokenAcceso, r.TokenDesafio, null, r.TokenRenovacion)),
    };

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(SolicitudLogin s, CancellationToken ct) =>
        Responder(await auth.IniciarSesionAsync(s.Correo, s.Contrasena, ct));

    [HttpPost("mfa/verificar")]
    [AllowAnonymous]
    public async Task<IActionResult> VerificarMfa(SolicitudCodigo s, CancellationToken ct) =>
        Responder(await auth.VerificarSegundoFactorAsync(s.TokenDesafio, s.Codigo, ct));

    [HttpPost("mfa/configuracion/iniciar")]
    [AllowAnonymous]
    public async Task<IActionResult> IniciarConfiguracionMfa(SolicitudDesafio s, CancellationToken ct) =>
        await auth.IniciarConfiguracionMfaAsync(s.TokenDesafio, ct) is { } r
            ? Ok(new RespuestaInicioMfa(r.SecretoBase32, r.UriOtpAuth))
            : Credenciales();

    [HttpPost("mfa/configuracion/confirmar")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmarConfiguracionMfa(SolicitudCodigo s, CancellationToken ct) =>
        await auth.ConfirmarConfiguracionMfaAsync(s.TokenDesafio, s.Codigo, ct) is { Exito: true } r
            ? Ok(new RespuestaConfirmacionMfa(r.CodigosRecuperacion))
            : Credenciales();

    /// <summary>Responde 202 siempre, exista o no el correo, para no revelar qué cuentas existen.</summary>
    [HttpPost("recuperacion/solicitar")]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarRecuperacion(SolicitudRecuperacion s, CancellationToken ct)
    {
        await recuperacion.SolicitarAsync(s.Correo, ct);
        return Accepted();
    }

    [HttpPost("recuperacion/restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> RestablecerContrasena(SolicitudRestablecimiento s, CancellationToken ct)
    {
        var r = await recuperacion.RestablecerAsync(s.Token, s.NuevaContrasena, ct);
        return r.Resultado switch
        {
            ResultadoRestablecimiento.Exito => NoContent(),
            ResultadoRestablecimiento.ContrasenaInvalida => UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Contraseña no válida",
                Detail = r.Motivo == MotivoContrasenaInvalida.Filtrada
                    ? "Esa contraseña es muy común; elige otra."
                    : $"La contraseña debe tener al menos {PoliticaContrasena.LongitudMinima} caracteres.",
                Extensions = { ["motivo"] = r.Motivo.ToString() },
            }),
            _ => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Enlace no válido",
                Detail = "El enlace no es válido o ya venció. Solicita uno nuevo.",
            }),
        };
    }

    /// <summary>Canjea el token de renovación (rotativo). Un token ya usado revoca la sesión.</summary>
    [HttpPost("renovar")]
    [AllowAnonymous]
    public async Task<IActionResult> Renovar(SolicitudRenovacion s, CancellationToken ct) =>
        Responder(await auth.RenovarAsync(s.TokenRenovacion, ct));

    [HttpPost("cerrar-sesion")]
    [Authorize]
    public async Task<IActionResult> CerrarSesion(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirst("sid")?.Value, out var sesionId))
            await auth.CerrarSesionAsync(sesionId, ct);
        return NoContent();
    }

    /// <summary>"Cerrar sesión en todos mis dispositivos" (RF-IAM-006).</summary>
    [HttpPost("cerrar-todas")]
    [Authorize]
    public async Task<IActionResult> CerrarTodas(CancellationToken ct)
    {
        if (Guid.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
            await auth.CerrarTodasLasSesionesAsync(usuarioId, ct);
        return NoContent();
    }

    /// <summary>Identidad del token de acceso vigente. Sirve de comprobación y de base para el middleware de tenant.</summary>
    [HttpGet("yo")]
    [Authorize]
    public IActionResult Yo() => Ok(new
    {
        UsuarioId = User.FindFirst("sub")?.Value,
        OrganizacionId = User.FindFirst("org")?.Value,
    });
}
