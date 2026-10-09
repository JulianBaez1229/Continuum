namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Amplitud con la que se permite el acceso a un recurso.</summary>
public enum NivelAcceso
{
    /// <summary>Todo el recurso.</summary>
    Completo,

    /// <summary>Asistente clínico: solo las secciones marcadas como visibles (la historia clínica filtra con el indicador de plantilla).</summary>
    PorSeccion,

    /// <summary>Solo diagnóstico y plan (equipo sin permiso en un episodio sensible).</summary>
    Resumen,

    /// <summary>Sin datos identificables (dirección).</summary>
    Agregado
}
