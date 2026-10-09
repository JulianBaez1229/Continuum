using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary><c>rol_asignado</c>. La persistencia real (tablas y migración) es un PR aparte del Dev B.</summary>
public interface IRepositorioRolesAsignados
{
    Task<IReadOnlyList<RolAsignado>> ListarDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task AgregarAsync(RolAsignado asignacion, CancellationToken ct = default);
    Task QuitarAsync(RolAsignado asignacion, CancellationToken ct = default);
}

public interface IRepositorioProfesionales
{
    Task<Profesional?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Profesional?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task AgregarAsync(Profesional profesional, CancellationToken ct = default);
}

public interface IRepositorioHabilitaciones
{
    Task<Habilitacion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>La habilitación del profesional en la especialidad, vigente o revocada (a lo sumo una por par).</summary>
    Task<Habilitacion?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default);

    /// <summary>Vigentes de la organización cuyo último día válido cae entre <paramref name="desde"/> y <paramref name="hasta"/>, ambos incluidos.</summary>
    Task<IReadOnlyList<Habilitacion>> ListarVigentesQueVencenAsync(
        Guid organizacionId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task AgregarAsync(Habilitacion habilitacion, CancellationToken ct = default);
    Task GuardarAsync(Habilitacion habilitacion, CancellationToken ct = default);
}

/// <summary>Sedes de la organización (módulo 06). Lo implementa Organización (Dev B).</summary>
public interface IConsultaSedes
{
    Task<bool> ExisteEnOrganizacionAsync(Guid organizacionId, Guid sedeId, CancellationToken ct = default);
}

/// <summary>Catálogo de especialidades y procedimientos (módulo 11). Lo implementa Catálogo (Dev B).</summary>
public interface ICatalogoClinico
{
    Task<bool> EspecialidadVigenteAsync(Guid organizacionId, Guid especialidadId, CancellationToken ct = default);

    /// <summary>Verdadero si todos los procedimientos existen y están vigentes en el catálogo de la organización.</summary>
    Task<bool> ProcedimientosVigentesAsync(Guid organizacionId, IReadOnlyCollection<Guid> procedimientoIds, CancellationToken ct = default);
}

/// <summary>
/// Ejecuta el trabajo en una sola transacción: si algo lanza, no queda nada (RF-AUD-008: no hay cambios sin evento).
/// La implementación real usa la transacción de la base de datos; ver <c>UnidadDeTrabajoMemoria</c> para las pruebas.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task EjecutarAsync(Func<CancellationToken, Task> trabajo, CancellationToken ct = default);
}

public enum TipoEventoAdministracion
{
    UsuarioCreado,
    UsuarioEditado,
    UsuarioDesactivado,
    RolAsignado,
    RolRetirado,
    ProfesionalRegistrado,
    HabilitacionRegistrada,
    HabilitacionActualizada,
    HabilitacionRevocada,
    /// <summary>Un actor sin permiso intentó administrar usuarios, roles o habilitaciones (módulo 23, «Seguridad»).</summary>
    AccesoAdministracionDenegado,
}

/// <summary>Niveles del módulo 23.</summary>
public enum NivelAuditoria { Informativo, Advertencia, Critico }

/// <summary>
/// Evento de la bitácora de administración (módulo 23). Solo identificadores y códigos: nunca correos, nombres,
/// números de licencia ni contenido clínico. <c>Detalle</c> es una lista <c>clave=valor</c> de ids, roles y fechas.
/// </summary>
public sealed record EventoAdministracion(
    TipoEventoAdministracion Tipo,
    DateTimeOffset OcurridoEn,
    Guid ActorId,
    Guid? OrganizacionId,
    Guid SedeId,
    Guid? UsuarioObjetivoId = null,
    Guid? RecursoId = null,
    string? Detalle = null)
{
    /// <summary>Administración → Advertencia; accesos denegados → Crítico (módulo 23 §2).</summary>
    public NivelAuditoria Nivel => Tipo == TipoEventoAdministracion.AccesoAdministracionDenegado
        ? NivelAuditoria.Critico
        : NivelAuditoria.Advertencia;
}

/// <summary>Puerto hacia el módulo Auditoría. Se invoca dentro de <see cref="IUnidadDeTrabajo"/>.</summary>
public interface IAuditoriaAdministracion
{
    Task RegistrarAsync(EventoAdministracion evento, CancellationToken ct = default);
}

/// <summary>
/// Habilitación por vencer. Solo ids, fecha y días: el aviso externo no lleva información clínica ni personal (RN-016);
/// el administrador abre la pantalla de habilitaciones para ver quién es.
/// </summary>
public sealed record AvisoVencimiento(Guid HabilitacionId, Guid ProfesionalId, DateOnly VigenteHasta, int DiasRestantes);

/// <summary>Aviso a los administradores funcionales de la organización. Lo implementa Comunicación (Dev B).</summary>
public interface IAvisosAdministracion
{
    Task NotificarVencimientosAsync(Guid organizacionId, IReadOnlyList<AvisoVencimiento> avisos, CancellationToken ct = default);
}
