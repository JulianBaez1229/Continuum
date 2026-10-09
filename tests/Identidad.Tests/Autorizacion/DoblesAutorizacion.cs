using Continuum.Identidad.Aplicacion.Autorizacion;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Autorizacion;

// Dobles de los puertos del Autorizador. Cada uno cuenta las llamadas, guarda el último token (y los argumentos)
// para verificar la carga perezosa, y lanza Fallo —después de registrar la llamada— si está asignado.

internal sealed class RolesPorSedeFalso : IRolesPorSede
{
    public PerfilActor? Perfil { get; set; }
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public (Guid UsuarioId, Guid SedeId)? UltimosArgumentos { get; private set; }
    public Exception? Fallo { get; set; }

    public Task<PerfilActor?> ObtenerPerfilAsync(Guid usuarioId, Guid sedeId, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        UltimosArgumentos = (usuarioId, sedeId);
        if (Fallo is not null) throw Fallo;
        return Task.FromResult(Perfil);
    }
}

internal sealed class VinculoPacienteFalso : IVinculoPaciente
{
    public Guid? PacienteId { get; set; }
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public Guid? UltimoUsuarioId { get; private set; }
    public Exception? Fallo { get; set; }

    public Task<Guid?> ObtenerPacienteIdAsync(Guid usuarioId, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        UltimoUsuarioId = usuarioId;
        if (Fallo is not null) throw Fallo;
        return Task.FromResult(PacienteId);
    }
}

internal sealed class RelacionClinicaFalsa : IRelacionClinica
{
    public RelacionClinica? Relacion { get; set; }
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public (Guid ProfesionalId, Guid PacienteId, Guid? EpisodioId, DateTimeOffset Ahora)? UltimosArgumentos { get; private set; }
    public Exception? Fallo { get; set; }

    public Task<RelacionClinica?> ObtenerAsync(
        Guid profesionalId, Guid pacienteId, Guid? episodioId, DateTimeOffset ahora, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        UltimosArgumentos = (profesionalId, pacienteId, episodioId, ahora);
        if (Fallo is not null) throw Fallo;
        return Task.FromResult(Relacion);
    }
}

internal sealed class HabilitacionesFalsas : IHabilitaciones
{
    public HabilitacionProfesional? Habilitacion { get; set; }
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public (Guid ProfesionalId, Guid EspecialidadId)? UltimosArgumentos { get; private set; }
    public Exception? Fallo { get; set; }

    public Task<HabilitacionProfesional?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        UltimosArgumentos = (profesionalId, especialidadId);
        if (Fallo is not null) throw Fallo;
        return Task.FromResult(Habilitacion);
    }
}

internal sealed class ZonaHorariaSedeFalsa : IZonaHorariaSede
{
    public TimeZoneInfo Zona { get; set; } = TimeZoneInfo.Utc;
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public Guid? UltimaSedeId { get; private set; }
    public Exception? Fallo { get; set; }

    public Task<TimeZoneInfo> ObtenerAsync(Guid sedeId, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        UltimaSedeId = sedeId;
        if (Fallo is not null) throw Fallo;
        return Task.FromResult(Zona);
    }
}

internal sealed class AuditoriaAutorizacionFalsa : IAuditoriaAutorizacion
{
    public List<EventoAutorizacion> Eventos { get; } = [];
    public int Llamadas { get; private set; }
    public CancellationToken UltimoToken { get; private set; }
    public Exception? Fallo { get; set; }

    public Task RegistrarDenegacionAsync(EventoAutorizacion evento, CancellationToken ct = default)
    {
        Llamadas++;
        UltimoToken = ct;
        if (Fallo is not null) throw Fallo;
        Eventos.Add(evento);
        return Task.CompletedTask;
    }
}
