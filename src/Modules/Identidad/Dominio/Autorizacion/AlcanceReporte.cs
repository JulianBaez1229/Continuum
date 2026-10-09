namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Alcance de un reporte. Es obligatorio cuando el recurso es <see cref="TipoRecurso.Reporte"/>.</summary>
public enum AlcanceReporte
{
    Propio,
    Operativo,
    Sede,
    Global,
    Cumplimiento
}
