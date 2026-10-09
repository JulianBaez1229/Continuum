using Continuum.Identidad.Aplicacion;

namespace Continuum.Identidad.Infraestructura.Seguridad;

public sealed class OpcionesSesion
{
    public const string Seccion = "Sesion";

    /// <summary>Módulo 06: <c>duracion_sesion_inactiva_min</c> = 15 (personal) / 30 (paciente).</summary>
    public int InactividadPersonalMinutos { get; set; } = 15;
    public int InactividadPacienteMinutos { get; set; } = 30;

    /// <summary>Supuesto (el módulo 07 no lo define): tope de una sesión aunque haya actividad continua.</summary>
    public int DuracionMaximaHoras { get; set; } = 12;
}

/// <summary>
/// Política única para todas las organizaciones, tomada de la configuración. Cuando el módulo 06 exponga la
/// política por organización, se sustituye esta implementación sin tocar el servicio.
/// </summary>
public sealed class PoliticaSesionConfigurada(OpcionesSesion opciones) : IPoliticaSesion
{
    public TimeSpan Inactividad(Guid organizacionId, bool esPersonal) =>
        TimeSpan.FromMinutes(esPersonal ? opciones.InactividadPersonalMinutos : opciones.InactividadPacienteMinutos);

    public TimeSpan DuracionMaxima(Guid organizacionId, bool esPersonal) => TimeSpan.FromHours(opciones.DuracionMaximaHoras);
}
