using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

/// <summary>
/// Lo que el módulo consumidor pregunta antes de actuar. La organización del actor no viaja aquí:
/// la devuelve <see cref="IRolesPorSede"/>, para no confiar en el llamador (spec §5).
/// </summary>
public sealed record SolicitudAcceso(
    Guid UsuarioId, Guid SedeActivaId, Accion Accion, TipoRecurso Recurso, ContextoRecurso Contexto);

/// <summary>Punto de entrada único de la autorización: los módulos lo llaman en cada solicitud, antes de leer o escribir.</summary>
public interface IAutorizador
{
    Task<Decision> AutorizarAsync(SolicitudAcceso solicitud, CancellationToken ct = default);
}
