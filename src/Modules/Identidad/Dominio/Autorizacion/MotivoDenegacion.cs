namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Razón por la que la política denegó el acceso. Cada motivo tiene un código estable para la auditoría.</summary>
public enum MotivoDenegacion
{
    OtraOrganizacion,
    SinRolEnSede,
    SinPermisoDeRol,
    SedeDistinta,
    NoEsPropio,
    NoLiberado,
    SinRelacionClinica,
    EpisodioSensible,
    SinHabilitacion,
    HabilitacionVencida,
    DatosInsuficientes
}

public static class MotivoDenegacionExtensiones
{
    /// <summary>Código textual del motivo para el evento de auditoría (módulo 23). No contiene datos clínicos.</summary>
    public static string Codigo(this MotivoDenegacion motivo) => motivo switch
    {
        MotivoDenegacion.OtraOrganizacion => "OTRA_ORGANIZACION",
        MotivoDenegacion.SinRolEnSede => "SIN_ROL_EN_SEDE",
        MotivoDenegacion.SinPermisoDeRol => "SIN_PERMISO_DE_ROL",
        MotivoDenegacion.SedeDistinta => "SEDE_DISTINTA",
        MotivoDenegacion.NoEsPropio => "NO_ES_PROPIO",
        MotivoDenegacion.NoLiberado => "NO_LIBERADO",
        MotivoDenegacion.SinRelacionClinica => "SIN_RELACION_CLINICA",
        MotivoDenegacion.EpisodioSensible => "EPISODIO_SENSIBLE",
        MotivoDenegacion.SinHabilitacion => "SIN_HABILITACION",
        MotivoDenegacion.HabilitacionVencida => "HABILITACION_VENCIDA",
        MotivoDenegacion.DatosInsuficientes => "DATOS_INSUFICIENTES",
        _ => throw new ArgumentOutOfRangeException(nameof(motivo), motivo, "Motivo de denegación sin código de auditoría."),
    };
}
