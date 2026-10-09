using System.ComponentModel.DataAnnotations;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Infraestructura.Seguridad;
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
public sealed record SolicitudReautenticacion([Required] string Contrasena, string? Codigo);
public sealed record SolicitudCodigoMensaje([Required] string Codigo);
public sealed record RespuestaReautenticacionDto(string TokenAcceso);
public sealed record SolicitudRecuperacion([Required] string Correo);
public sealed record SolicitudRestablecimiento([Required] string Token, [Required] string NuevaContrasena);

public sealed record RespuestaLogin(
    string Estado, string? TokenAcceso, string? TokenDesafio, DateTimeOffset? BloqueadoHasta, string? TokenRenovacion = null,
    string? Metodo = null);
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
public sealed class AuthController(
    ServicioAutenticacion auth, ServicioRecuperacionContrasena recuperacion, ServicioReautenticacion reautenticacion,
    ServicioMfaPorMensaje mfaPorMensaje) : ControllerBase
{
    private static ObjectResult Credenciales(string? codigo = null)
    {
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No autenticado",
            Detail = codigo switch
            {
                null => "Credenciales inválidas.",
                "reautenticacion_invalida" => "Contraseña o código incorrectos.",
                _ => "La sesión expiró; inicia sesión de nuevo.",
            },
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
        _ => Ok(new RespuestaLogin(r.Estado.ToString(), r.TokenAcceso, r.TokenDesafio, null, r.TokenRenovacion, r.Metodo?.ToString())),
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

    /// <summary>
    /// Confirma contraseña y segundo factor antes de una acción crítica (RF-IAM-010). Devuelve un acceso nuevo, de la
    /// misma sesión, con la marca de reautenticación. El cliente debe usar ese acceso en la acción crítica.
    /// </summary>
    [HttpPost("reautenticar")]
    [Authorize]
    public async Task<IActionResult> Reautenticar(SolicitudReautenticacion s, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var usuarioId)
            || !Guid.TryParse(User.FindFirst("sid")?.Value, out var sesionId))
            return Forbid();

        var r = await reautenticacion.ReautenticarAsync(usuarioId, sesionId, s.Contrasena, s.Codigo, ct);
        return r.Estado switch
        {
            EstadoInicioSesion.Exitoso => Ok(new RespuestaReautenticacionDto(r.TokenAcceso!)),
            EstadoInicioSesion.CuentaBloqueada => StatusCode(StatusCodes.Status423Locked,
                new RespuestaLogin(r.Estado.ToString(), null, null, r.BloqueadoHasta)),
            _ => Credenciales("reautenticacion_invalida"),
        };
    }

    /// <summary>Permite al cliente saber si la reautenticación sigue vigente antes de mostrar una acción crítica.</summary>
    [HttpGet("reautenticacion/estado")]
    [Authorize(Policy = PoliticasIdentidad.ReautenticacionReciente)]
    public IActionResult EstadoReautenticacion() => NoContent();

    // ---- Segundo factor opcional del paciente: código por mensaje (RF-IAM-003) ----

    private Guid? UsuarioActual => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    /// <summary>Envía un código para activar el segundo factor. 409 si no procede (personal con TOTP o ya activo).</summary>
    [HttpPost("mfa/mensaje/solicitar")]
    [Authorize]
    public async Task<IActionResult> SolicitarMfaMensaje(CancellationToken ct) =>
        UsuarioActual is not { } id ? Forbid()
        : await mfaPorMensaje.IniciarActivacionAsync(id, ct) switch
        {
            ResultadoSolicitudCodigo.Enviado => Accepted(),
            ResultadoSolicitudCodigo.EsperaRequerida => StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Espera un momento",
                Detail = "Ya se envió un código hace menos de un minuto.",
            }),
            _ => Problema409("El segundo factor por mensaje no está disponible para esta cuenta."),
        };

    [HttpPost("mfa/mensaje/confirmar")]
    [Authorize]
    public async Task<IActionResult> ConfirmarMfaMensaje(SolicitudCodigoMensaje s, CancellationToken ct) =>
        UsuarioActual is not { } id ? Forbid()
        : await mfaPorMensaje.ConfirmarActivacionAsync(id, s.Codigo, ct) switch
        {
            ResultadoConfirmacionCodigo.Exito => NoContent(),
            ResultadoConfirmacionCodigo.NoPermitido => Problema409("El segundo factor por mensaje no está disponible para esta cuenta."),
            _ => UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Código no válido",
                Detail = "El código es incorrecto o venció. Solicita uno nuevo.",
            }),
        };

    /// <summary>Exige una reautenticación reciente (RF-IAM-010).</summary>
    [HttpPost("mfa/mensaje/desactivar")]
    [Authorize(Policy = PoliticasIdentidad.ReautenticacionReciente)]
    public async Task<IActionResult> DesactivarMfaMensaje(CancellationToken ct)
    {
        if (UsuarioActual is not { } id) return Forbid();
        await mfaPorMensaje.DesactivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Reenvía el código del inicio de sesión. Responde 202 siempre para no revelar nada.</summary>
    [HttpPost("mfa/mensaje/reenviar")]
    [AllowAnonymous]
    public async Task<IActionResult> ReenviarCodigo(SolicitudDesafio s, CancellationToken ct)
    {
        await auth.ReenviarCodigoAsync(s.TokenDesafio, ct);
        return Accepted();
    }

    private static ObjectResult Problema409(string detalle) => new(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "No disponible",
        Detail = detalle,
    }) { StatusCode = StatusCodes.Status409Conflict };

    /// <summary>Identidad del token de acceso vigente. Sirve de comprobación y de base para el middleware de tenant.</summary>
    [HttpGet("yo")]
    [Authorize]
    public IActionResult Yo() => Ok(new
    {
        UsuarioId = User.FindFirst("sub")?.Value,
        OrganizacionId = User.FindFirst("org")?.Value,
    });
}
