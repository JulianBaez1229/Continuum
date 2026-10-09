using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

/// <summary>
/// Resuelve los hechos que la política necesita y delega la decisión en <see cref="PoliticaAcceso"/> (spec §6).
/// La carga es perezosa: solo se consulta un puerto si <see cref="PoliticaAcceso.Necesidades"/> declara que su hecho
/// puede intervenir. Si un puerto lanza una excepción, esta se propaga y nunca se concede acceso (spec §10).
/// </summary>
// Transitorio: la auditoría de denegaciones (spec §9) la añade la Task 7; hasta entonces `auditoria` no se lee.
// La Task 7 debe quitar este pragma.
#pragma warning disable CS9113
public sealed class Autorizador(
    IRolesPorSede roles,
    IVinculoPaciente vinculo,
    IRelacionClinica relacion,
    IHabilitaciones habilitaciones,
    IZonaHorariaSede zonas,
    IReloj reloj,
    IAuditoriaAutorizacion auditoria) : IAutorizador
#pragma warning restore CS9113
{
    public async Task<Decision> AutorizarAsync(SolicitudAcceso solicitud, CancellationToken ct = default)
    {
        // Una sola lectura del reloj: la relación clínica y «hoy» de la habilitación parten del mismo instante.
        var ahora = reloj.Ahora;
        var contexto = solicitud.Contexto;

        // R0: sin perfil (usuario inexistente o inactivo) no hay nada más que cargar.
        var perfil = await roles.ObtenerPerfilAsync(solicitud.UsuarioId, solicitud.SedeActivaId, ct);
        if (perfil is null)
            return Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinRolEnSede);

        // R1: un recurso de otra organización se resuelve solo con el perfil. Consultar más hechos para un 404
        // seguro sería trabajo inútil y, con datos de otra organización, un riesgo.
        var hechos = contexto.OrganizacionId != perfil.OrganizacionId
            ? new HechosAcceso(perfil, solicitud.SedeActivaId, null, null, null, DateOnly.FromDateTime(ahora.UtcDateTime))
            : await CargarHechosAsync(perfil, solicitud, ahora, ct);

        return PoliticaAcceso.Evaluar(solicitud.Accion, solicitud.Recurso, contexto, hechos);
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
