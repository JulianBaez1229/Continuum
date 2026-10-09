using System.ComponentModel.DataAnnotations;
using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Continuum.Identidad.Controllers;

public sealed record SolicitudInvitacion([Required] Guid PacienteId, CanalInvitacion? Canal);

public sealed record SolicitudActivarCuenta(
    [Required] string Token,
    [Required] DateOnly FechaNacimiento,
    [Required] string Ultimos4Documento,
    string? Correo,
    [Required] string Contrasena,
    [Required] string VersionTerminos,
    [Required] string VersionAvisoPrivacidad);

/// <summary>Invitación (personal de recepción) y activación (paciente) de cuentas de paciente, RF-IAM-007.</summary>
[ApiController]
[Route("api/auth/activacion")]
[ConflictoConcurrencia]
public sealed class ActivacionPacienteController(ServicioActivacionPaciente activacion) : ControllerBase
{
    private static ObjectResult Problema(int estado, string titulo, string detalle, string? motivo = null)
    {
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        if (motivo is not null) problema.Extensions["motivo"] = motivo;
        return new ObjectResult(problema) { StatusCode = estado };
    }

    /// <summary>Solo personal con el permiso <c>pacientes.invitar</c> (módulo 03). Sin ese permiso, 403.</summary>
    [HttpPost("invitar")]
    [Authorize(Policy = PoliticasIdentidad.InvitarPacientes)]
    public async Task<IActionResult> Invitar(SolicitudInvitacion s, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("org")?.Value, out var organizacionId)
            || !Guid.TryParse(User.FindFirst("sub")?.Value, out var solicitanteId))
            return Forbid();

        return await activacion.InvitarAsync(s.PacienteId, organizacionId, solicitanteId, s.Canal, ct) switch
        {
            ResultadoInvitacion.Enviada => Accepted(),
            ResultadoInvitacion.YaTieneCuenta => Problema(StatusCodes.Status409Conflict, "Cuenta existente", "El paciente ya tiene una cuenta activa."),
            ResultadoInvitacion.SinContacto => Problema(StatusCodes.Status422UnprocessableEntity, "Sin medio de contacto", "El paciente no tiene correo ni teléfono registrado."),
            _ => NotFound(), // inexistente o de otra organización: misma respuesta (RN-015)
        };
    }

    [HttpPost("activar")]
    [AllowAnonymous]
    public async Task<IActionResult> Activar(SolicitudActivarCuenta s, CancellationToken ct)
    {
        var r = await activacion.ActivarAsync(new SolicitudActivacion(
            s.Token, s.FechaNacimiento, s.Ultimos4Documento, s.Correo, s.Contrasena, s.VersionTerminos, s.VersionAvisoPrivacidad), ct);

        return r.Resultado switch
        {
            ResultadoActivacion.Exito => NoContent(),
            ResultadoActivacion.IdentidadNoCoincide => Problema(StatusCodes.Status422UnprocessableEntity,
                "Datos no coinciden", "Revisa la fecha de nacimiento y los últimos 4 dígitos del documento.", "identidad"),
            ResultadoActivacion.ConsentimientoRequerido => Problema(StatusCodes.Status422UnprocessableEntity,
                "Consentimiento requerido", "Debes aceptar la versión vigente de los términos y del aviso de privacidad.", "consentimiento"),
            ResultadoActivacion.ContrasenaInvalida => Problema(StatusCodes.Status422UnprocessableEntity,
                "Contraseña no válida",
                r.Motivo == MotivoContrasenaInvalida.Filtrada
                    ? "Esa contraseña es muy común; elige otra."
                    : $"La contraseña debe tener al menos {PoliticaContrasena.LongitudMinima} caracteres.",
                r.Motivo.ToString()),
            ResultadoActivacion.CorreoRequerido => Problema(StatusCodes.Status422UnprocessableEntity,
                "Correo requerido", "Indica un correo válido para tu cuenta.", "correo"),
            ResultadoActivacion.CorreoNoDisponible => Problema(StatusCodes.Status409Conflict,
                "Correo no disponible", "Ese correo ya está en uso."),
            _ => Problema(StatusCodes.Status400BadRequest, "Invitación no válida", "El enlace no es válido o ya venció. Pide una nueva invitación en recepción."),
        };
    }
}
