using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Dominio;

public enum EstadoHabilitacion { Vigente, Revocada }

/// <summary>
/// Fila de <c>habilitacion</c> (módulo 05, RN-012): especialidad y procedimientos en los que un profesional puede
/// actuar, con su fecha de vencimiento. Nunca se elimina: se revoca (RN-006, RN-011). Es inmutable; cada cambio
/// devuelve una copia, de modo que un rollback en memoria basta con restaurar la referencia anterior.
/// </summary>
public sealed record Habilitacion
{
    /// <summary>RF-ROL-004: el administrador recibe un aviso cuando faltan 30 días o menos.</summary>
    public const int DiasDeAviso = 30;

    private Habilitacion() { }

    public Guid Id { get; private init; }
    public Guid ProfesionalId { get; private init; }
    public Guid OrganizacionId { get; private init; }
    public Guid EspecialidadId { get; private init; }
    public IReadOnlySet<Guid> ProcedimientosPermitidos { get; private init; } = new HashSet<Guid>();

    /// <summary>Último día válido, inclusive (spec de autorización, D7), en la zona horaria de la sede.</summary>
    public DateOnly VigenteHasta { get; private init; }
    public EstadoHabilitacion Estado { get; private init; } = EstadoHabilitacion.Vigente;

    /// <summary>Fecha de vencimiento para la que ya se avisó al administrador. Cambiar el vencimiento reabre el aviso.</summary>
    public DateOnly? AvisadaParaVencimiento { get; private init; }

    public static Habilitacion Registrar(Guid profesionalId, Guid organizacionId, Guid especialidadId,
        IEnumerable<Guid> procedimientosPermitidos, DateOnly vigenteHasta) => new()
    {
        Id = Guid.NewGuid(),
        ProfesionalId = profesionalId,
        OrganizacionId = organizacionId,
        EspecialidadId = especialidadId,
        ProcedimientosPermitidos = procedimientosPermitidos.ToHashSet(),
        VigenteHasta = vigenteHasta,
    };

    /// <summary>Cambia procedimientos y vencimiento. También reactiva una habilitación revocada.</summary>
    public Habilitacion Actualizar(IEnumerable<Guid> procedimientosPermitidos, DateOnly vigenteHasta) => this with
    {
        ProcedimientosPermitidos = procedimientosPermitidos.ToHashSet(),
        VigenteHasta = vigenteHasta,
        Estado = EstadoHabilitacion.Vigente,
        AvisadaParaVencimiento = vigenteHasta == VigenteHasta ? AvisadaParaVencimiento : null,
    };

    public Habilitacion Revocar() => this with { Estado = EstadoHabilitacion.Revocada };

    public Habilitacion MarcarAvisada() => this with { AvisadaParaVencimiento = VigenteHasta };

    /// <summary>Vigente, sin aviso previo para este vencimiento, y dentro de la ventana de 30 días (el día del vencimiento incluido).</summary>
    public bool DebeAvisarse(DateOnly hoy) =>
        Estado == EstadoHabilitacion.Vigente
        && AvisadaParaVencimiento != VigenteHasta
        && VigenteHasta >= hoy
        && VigenteHasta <= hoy.AddDays(DiasDeAviso);

    /// <summary>El hecho que consume la política de acceso (R8). El vencimiento lo decide la política, no esta capa.</summary>
    public HabilitacionProfesional ComoHecho() => new(EspecialidadId, ProcedimientosPermitidos, VigenteHasta);
}
