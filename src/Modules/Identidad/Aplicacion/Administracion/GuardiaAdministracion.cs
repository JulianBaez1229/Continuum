using Continuum.Identidad.Aplicacion.Autorizacion;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Administracion;

/// <param name="Perfil">Perfil del actor cuando fue autorizado.</param>
/// <param name="Denegacion">Respuesta lista para devolver cuando no fue autorizado (la denegación ya quedó auditada).</param>
public sealed record ResultadoGuardia(PerfilActor? Perfil, ResultadoOperacion? Denegacion);

/// <summary>
/// Control de acceso de los casos de uso de administración: solo <c>ADMIN_FUNCIONAL</c> (módulo 03 §4, fila «Usuarios y roles»).
/// Reutiliza la matriz de <see cref="PoliticaAcceso"/>, así que un cambio de la matriz cambia también este control.
/// Toda denegación se audita como evento crítico; si la bitácora falla, la excepción se propaga y nunca se concede.
/// </summary>
public sealed class GuardiaAdministracion(IRolesPorSede perfiles, IAuditoriaAdministracion auditoria, IReloj reloj)
{
    public async Task<ResultadoGuardia> AutorizarAsync(ActorAdministracion actor, Accion accion, CancellationToken ct = default)
    {
        var perfil = await perfiles.ObtenerPerfilAsync(actor.UsuarioId, actor.SedeActivaId, ct);

        var decision = perfil is null
            ? Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinRolEnSede)
            : PoliticaAcceso.Evaluar(accion, TipoRecurso.UsuarioRol, new ContextoRecurso(perfil.OrganizacionId),
                new HechosAcceso(perfil, actor.SedeActivaId, null, null, null, DateOnly.FromDateTime(reloj.Ahora.UtcDateTime)));

        if (decision.EstaPermitido) return new(perfil, null);

        var motivo = decision.Motivo!.Value.Codigo();
        await RegistrarDenegacionAsync(actor, perfil?.OrganizacionId, null, motivo, ct);
        return new(null, ResultadoOperacion.Prohibido(motivo));
    }

    /// <summary>
    /// RN-015: un recurso de otra organización no existe para el actor (404, no 403) y el intento queda como evento crítico.
    /// Devuelve <c>null</c> si el recurso es de la organización del actor.
    /// </summary>
    public async Task<ResultadoOperacion?> VerificarOrganizacionAsync(
        ActorAdministracion actor, PerfilActor perfil, Guid organizacionDelRecurso, Guid? objetivoId, CancellationToken ct = default)
    {
        if (organizacionDelRecurso == perfil.OrganizacionId) return null;

        await RegistrarDenegacionAsync(actor, perfil.OrganizacionId, objetivoId, MotivoDenegacion.OtraOrganizacion.Codigo(), ct);
        return ResultadoOperacion.NoEncontrado();
    }

    private Task RegistrarDenegacionAsync(ActorAdministracion actor, Guid? organizacionId, Guid? objetivoId, string motivo, CancellationToken ct) =>
        auditoria.RegistrarAsync(new EventoAdministracion(TipoEventoAdministracion.AccesoAdministracionDenegado,
            reloj.Ahora, actor.UsuarioId, organizacionId, actor.SedeActivaId, objetivoId, null, motivo), ct);
}
