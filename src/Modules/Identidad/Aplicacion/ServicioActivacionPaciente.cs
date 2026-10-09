using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum ResultadoInvitacion { Enviada, PacienteNoEncontrado, YaTieneCuenta, SinContacto }

public enum ResultadoActivacion
{
    Exito,
    TokenInvalido,
    IdentidadNoCoincide,
    ConsentimientoRequerido,
    ContrasenaInvalida,
    CorreoRequerido,
    CorreoNoDisponible,
}

public sealed record SolicitudActivacion(
    string Token,
    DateOnly FechaNacimiento,
    string Ultimos4Documento,
    string? Correo,
    string Contrasena,
    string VersionTerminos,
    string VersionAvisoPrivacidad);

public readonly record struct RespuestaActivacion(
    ResultadoActivacion Resultado, MotivoContrasenaInvalida? Motivo = null, Guid? UsuarioId = null);

/// <summary>
/// Activación de la cuenta del paciente por invitación (RF-IAM-007): recepción genera el enlace (72 h), el paciente
/// confirma fecha de nacimiento y los últimos 4 dígitos del documento, crea su contraseña y acepta los textos legales.
/// </summary>
public sealed class ServicioActivacionPaciente(
    IRepositorioUsuarios usuarios,
    ITokensAccion tokens,
    IDirectorioPacientes directorio,
    IConsentimientos consentimientos,
    PoliticaContrasena politica,
    IHasheadorContrasena hasheador,
    IReloj reloj,
    IAuditoriaIdentidad auditoria,
    IMensajeriaIdentidad mensajeria)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(72);
    public const int IntentosMaximos = 5;

    /// <summary>
    /// <paramref name="organizacionSolicitante"/> es la del personal que invita: un paciente de otra organización
    /// se trata como inexistente (RN-015). La autorización del solicitante la valida el endpoint.
    /// </summary>
    public async Task<ResultadoInvitacion> InvitarAsync(
        Guid pacienteId, Guid organizacionSolicitante, Guid solicitanteId,
        CanalInvitacion? canalPreferido = null, CancellationToken ct = default)
    {
        var contacto = await directorio.ObtenerContactoAsync(pacienteId, ct);
        if (contacto is null || contacto.OrganizacionId != organizacionSolicitante) return ResultadoInvitacion.PacienteNoEncontrado;
        if (contacto.TieneCuenta) return ResultadoInvitacion.YaTieneCuenta;

        var tieneCorreo = !string.IsNullOrWhiteSpace(contacto.Correo);
        var tieneTelefono = !string.IsNullOrWhiteSpace(contacto.Telefono);
        CanalInvitacion canal;
        if (canalPreferido == CanalInvitacion.Sms && tieneTelefono) canal = CanalInvitacion.Sms;
        else if (tieneCorreo) canal = CanalInvitacion.Correo;
        else if (tieneTelefono) canal = CanalInvitacion.Sms;
        else return ResultadoInvitacion.SinContacto;

        var ahora = reloj.Ahora;
        var anteriores = await tokens.ObtenerPendientesDePacienteAsync(pacienteId, PropositoToken.ActivacionCuenta, ct);
        foreach (var anterior in anteriores) anterior.Invalidar(ahora);
        if (anteriores.Count > 0) await tokens.GuardarAsync(anteriores, ct);

        var plano = GeneradorTokens.Nuevo();
        var token = TokenAccion.Crear(PropositoToken.ActivacionCuenta, null, pacienteId, contacto.OrganizacionId,
            GeneradorTokens.HashDe(plano), ahora, Vigencia);
        await tokens.AgregarAsync(token, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.InvitacionPacienteEnviada, ahora,
            solicitanteId, contacto.OrganizacionId, canal.ToString()), ct);
        await mensajeria.EnviarInvitacionPacienteAsync(pacienteId, canal,
            canal == CanalInvitacion.Correo ? contacto.Correo! : contacto.Telefono!, plano, token.ExpiraEn, ct);
        return ResultadoInvitacion.Enviada;
    }

    public async Task<RespuestaActivacion> ActivarAsync(SolicitudActivacion s, CancellationToken ct = default)
    {
        var invalido = new RespuestaActivacion(ResultadoActivacion.TokenInvalido);
        if (string.IsNullOrWhiteSpace(s.Token)) return invalido;

        var ahora = reloj.Ahora;
        var token = await tokens.ObtenerPorHashAsync(GeneradorTokens.HashDe(s.Token), ct);
        if (token is not { Proposito: PropositoToken.ActivacionCuenta, PacienteId: { } pacienteId } || !token.EstaVigente(ahora))
            return invalido;

        var contacto = await directorio.ObtenerContactoAsync(pacienteId, ct);
        if (contacto is null || contacto.TieneCuenta) return invalido;

        if (!await directorio.VerificarIdentidadAsync(pacienteId, s.FechaNacimiento, s.Ultimos4Documento, ct))
        {
            token.RegistrarIntentoFallido(ahora, IntentosMaximos);
            await tokens.GuardarAsync([token], ct);
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.ActivacionPacienteFallida, ahora,
                null, token.OrganizacionId, "identidad"), ct);
            return new(ResultadoActivacion.IdentidadNoCoincide);
        }

        var vigentes = consentimientos.Vigentes(token.OrganizacionId);
        if (s.VersionTerminos != vigentes.Terminos || s.VersionAvisoPrivacidad != vigentes.AvisoPrivacidad)
            return new(ResultadoActivacion.ConsentimientoRequerido);

        var validacion = politica.Validar(s.Contrasena);
        if (!validacion.EsValida) return new(ResultadoActivacion.ContrasenaInvalida, validacion.Motivo);

        var correo = Usuario.NormalizarCorreo(string.IsNullOrWhiteSpace(contacto.Correo) ? s.Correo ?? "" : contacto.Correo);
        if (!correo.Contains('@')) return new(ResultadoActivacion.CorreoRequerido);
        if (await usuarios.ObtenerPorCorreoAsync(correo, ct) is not null) return new(ResultadoActivacion.CorreoNoDisponible);

        // Primero se consume la invitación; si algo falla después, recepción genera otra.
        token.MarcarUsado(ahora);
        await tokens.GuardarAsync([token], ct);

        var usuario = Usuario.Crear(token.OrganizacionId, correo, hasheador.Hashear(s.Contrasena), requiereMfa: false);
        await usuarios.AgregarAsync(usuario, ct);
        await directorio.VincularCuentaAsync(pacienteId, usuario.Id, ct);
        await consentimientos.RegistrarAceptacionAsync(pacienteId, "terminos", vigentes.Terminos, usuario.Id, ahora, ct);
        await consentimientos.RegistrarAceptacionAsync(pacienteId, "aviso_privacidad", vigentes.AvisoPrivacidad, usuario.Id, ahora, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CuentaPacienteActivada, ahora,
            usuario.Id, usuario.OrganizacionId), ct);
        return new(ResultadoActivacion.Exito, UsuarioId: usuario.Id);
    }
}
