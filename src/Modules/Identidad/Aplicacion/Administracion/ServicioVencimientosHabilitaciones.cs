using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary>
/// RF-ROL-004: avisa al administrador funcional cuando a una habilitación le faltan 30 días o menos. Lo ejecuta una
/// tarea programada por organización (no un usuario), así que no recibe actor. El aviso se envía una sola vez por fecha
/// de vencimiento y solo lleva identificadores y fechas (RN-016). Si el envío falla, nada se marca y se reintenta en la
/// siguiente corrida (entrega al menos una vez).
/// <para>
/// Solo para tareas programadas: <c>organizacionId</c> sale de la configuración del planificador y nunca debe llegar
/// de una solicitud de usuario, porque este servicio no autoriza (no hay actor).
/// </para>
/// </summary>
public sealed class ServicioVencimientosHabilitaciones(
    IRepositorioHabilitaciones habilitaciones, IAvisosAdministracion avisos, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
{
    /// <param name="zona">Zona horaria con la que se decide «hoy» (la de la sede; por defecto <c>America/Santo_Domingo</c>).</param>
    /// <returns>Cantidad de habilitaciones avisadas.</returns>
    public async Task<int> NotificarAsync(Guid organizacionId, TimeZoneInfo zona, CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.Ahora, zona).DateTime);
        var pendientes = (await habilitaciones.ListarVigentesQueVencenAsync(
                organizacionId, hoy, hoy.AddDays(Habilitacion.DiasDeAviso), ct))
            .Where(h => h.DebeAvisarse(hoy))
            .ToList();
        if (pendientes.Count == 0) return 0;

        await avisos.NotificarVencimientosAsync(organizacionId,
            pendientes.Select(h => new AvisoVencimiento(h.Id, h.ProfesionalId, h.VigenteHasta, h.VigenteHasta.DayNumber - hoy.DayNumber)).ToList(),
            ct);

        await unidadDeTrabajo.EjecutarAsync(async c =>
        {
            foreach (var h in pendientes) await habilitaciones.GuardarAsync(h.MarcarAvisada(), c);
        }, ct);
        return pendientes.Count;
    }
}
