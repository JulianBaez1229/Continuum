namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Tipos de recurso sobre los que la matriz de permisos del módulo 03 decide.</summary>
public enum TipoRecurso
{
    DatosDemograficos,
    Cita,
    AgendaProfesional,
    NotaClinica,
    ResumenSeguridad,
    SignosVitalesTriaje,
    Orden,
    AsignacionFormulario,
    RespuestaFormulario,
    Mensaje,
    Reporte,
    Catalogo,
    UsuarioRol,
    Bitacora
}
