using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

/// <summary>
/// Resuelve los hechos que la política necesita y delega la decisión en <see cref="PoliticaAcceso"/> (spec §6).
/// La carga es perezosa: solo se consulta un puerto si <see cref="PoliticaAcceso.Necesidades"/> declara que su hecho
/// puede intervenir. Cada denegación se registra en <see cref="IAuditoriaAutorizacion"/> antes de devolverla (spec §9).
/// Falla cerrada (spec §10): no captura nada; si un puerto, o el registro de la denegación, lanza una excepción, esta
/// se propaga y el llamador nunca recibe una decisión sin su evento ni un acceso concedido.
/// </summary>
public sealed class Autorizador(
    IRolesPorSede roles,
    IVinculoPaciente vinculo,
    IRelacionClinica relacion,
    IHabilitaciones habilitaciones,
    IZonaHorariaSede zonas,
    IReloj reloj,
    IAuditoriaAutorizacion auditoria) : IAutorizador
{
    public async Task<Decision> AutorizarAsync(SolicitudAcceso solicitud, CancellationToken ct = default)
    {
        // Una sola lectura del reloj: la relación clínica y «hoy» de la habilitación parten del mismo instante.
        var ahora = reloj.Ahora;
        var contexto = solicitud.Contexto;

        // R0: sin perfil (usuario inexistente o inactivo) no hay nada más que cargar.
        var perfil = await roles.ObtenerPerfilAsync(solicitud.UsuarioId, solicitud.SedeActivaId, ct);
        if (perfil is null)
            return await AuditarSiDenegadaAsync(
                Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinRolEnSede), solicitud, null, ahora, ct);

        // R1: un recurso de otra organización se resuelve solo con el perfil. Consultar más hechos para un 404
        // seguro sería trabajo inútil y, con datos de otra organización, un riesgo.
        var hechos = contexto.OrganizacionId != perfil.OrganizacionId
            ? new HechosAcceso(perfil, solicitud.SedeActivaId, null, null, null, DateOnly.FromDateTime(ahora.UtcDateTime))
            : await CargarHechosAsync(perfil, solicitud, ahora, ct);

        var decision = PoliticaAcceso.Evaluar(solicitud.Accion, solicitud.Recurso, contexto, hechos);
        return await AuditarSiDenegadaAsync(decision, solicitud, perfil, ahora, ct);
    }

    /// <summary>
    /// Registra un único <see cref="EventoAutorizacion"/> si la decisión es una denegación (spec §9) y la devuelve; una
    /// decisión permitida no se audita aquí. Solo identificadores y códigos: nunca contenido clínico. Sin <c>try/catch</c>:
    /// si el registro falla, la excepción llega al llamador (spec §10).
    /// </summary>
    private async Task<Decision> AuditarSiDenegadaAsync(
        Decision decision, SolicitudAcceso solicitud, PerfilActor? perfil, DateTimeOffset ahora, CancellationToken ct)
    {
        if (decision.EstaPermitido)
            return decision;

        var contexto = solicitud.Contexto;
        // Los roles del evento son una copia (snapshot): el perfil es del puerto y el evento no debe cambiar con él.
        IReadOnlyCollection<Rol> rolesDelEvento = perfil is null ? [] : [.. perfil.Roles];
        await auditoria.RegistrarDenegacionAsync(
            new EventoAutorizacion(
                ahora, solicitud.UsuarioId, perfil?.OrganizacionId, solicitud.SedeActivaId,
                rolesDelEvento, solicitud.Accion, solicitud.Recurso,
                contexto.RecursoId, contexto.PacienteId, decision.Tipo!.Value, decision.Motivo!.Value),
            ct);
        return decision;
    }

    /// <summary>Carga, en este orden y solo si hacen falta: vínculo del paciente, relación clínica, y habilitación con la zona de la sede.</summary>
    private async Task<HechosAcceso> CargarHechosAsync(PerfilActor perfil, SolicitudAcceso solicitud, DateTimeOffset ahora, CancellationToken ct)
    {
        var contexto = solicitud.Contexto;
        var necesidades = PoliticaAcceso.Necesidades(solicitud.Accion, solicitud.Recurso, perfil.Roles, contexto.AlcanceReporte);

        // R4: solo el rol paciente compara con su vínculo.
        var pacienteDelActor = necesidades.VinculoPaciente
            ? await vinculo.ObtenerPacienteIdAsync(solicitud.UsuarioId, ct)
            : null;

        // R6: sin ficha de profesional, sin paciente o (en recursos de episodio) sin episodio no hay relación que
        // buscar; la política responde con el motivo que corresponde sin necesitar el hecho.
        RelacionClinica? relacionClinica = null;
        if (necesidades.Relacion
            && perfil.ProfesionalId is { } profesionalId
            && contexto.PacienteId is { } pacienteId
            && (!PoliticaAcceso.EsDeEpisodio(solicitud.Recurso) || contexto.EpisodioId is not null))
        {
            relacionClinica = await relacion.ObtenerAsync(profesionalId, pacienteId, contexto.EpisodioId, ahora, ct);
        }

        // R8: la habilitación se pide en la especialidad de la relación, así que sin relación no hay qué pedir.
        // «Hoy» es la fecha local de la sede (el borde de la licencia depende de ella); si no hace falta, la fecha UTC.
        HabilitacionProfesional? habilitacion = null;
        var hoyEnSede = DateOnly.FromDateTime(ahora.UtcDateTime);
        if (necesidades.Habilitacion && perfil.ProfesionalId is { } profesional && relacionClinica is not null)
        {
            habilitacion = await habilitaciones.ObtenerAsync(profesional, relacionClinica.EspecialidadId, ct);
            var zona = await zonas.ObtenerAsync(solicitud.SedeActivaId, ct);
            hoyEnSede = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(ahora, zona).DateTime);
        }

        return new HechosAcceso(perfil, solicitud.SedeActivaId, pacienteDelActor, relacionClinica, habilitacion, hoyEnSede);
    }
}
