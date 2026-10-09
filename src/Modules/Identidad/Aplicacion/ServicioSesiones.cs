

using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Aplicacion;

public enum EstadoRenovacion { Exitosa, Invalida, SesionExpirada }

public sealed record ResultadoRenovacion(EstadoRenovacion Estado, Sesion? Sesion = null, string? TokenRenovacion = null);

/// <summary>
/// Sesiones con token de renovación rotativo y cierre por inactividad (RF-IAM-006, CA-IAM-003).
/// Reutilizar un token ya rotado indica robo o clon: se revoca toda la sesión.
/// </summary>
public sealed class ServicioSesiones(
    ISesiones sesiones, IPoliticaSesion politica, IReloj reloj, IAuditoriaIdentidad auditoria)
{
    /// <summary>La actividad se guarda como máximo una vez por minuto para no escribir en cada solicitud.</summary>
    public static readonly TimeSpan UmbralActividad = TimeSpan.FromMinutes(1);

    public async Task<(Sesion Sesion, string TokenRenovacion)> IniciarAsync(Usuario usuario, CancellationToken ct = default)
    {
        var token = NuevoToken();
        var sesion = Sesion.Iniciar(usuario, HashDe(token), reloj.Ahora);
        await sesiones.AgregarAsync(sesion, ct);
        return (sesion, token);
    }

    public async Task<ResultadoRenovacion> RenovarAsync(string tokenRenovacion, CancellationToken ct = default)
    {
        var invalida = new ResultadoRenovacion(EstadoRenovacion.Invalida);
        if (string.IsNullOrWhiteSpace(tokenRenovacion)) return invalida;

        var hash = HashDe(tokenRenovacion);
        var sesion = await sesiones.ObtenerPorHashRenovacionAsync(hash, ct);
        if (sesion is null || sesion.RevocadaEn is not null) return invalida;

        var ahora = reloj.Ahora;
        if (hash != sesion.HashRenovacion)
        {
            sesion.Revocar(ahora);
            await sesiones.GuardarAsync([sesion], ct);
            await auditoria.RegistrarAsync(new(TipoEventoIdentidad.TokenRenovacionReutilizado, ahora,
                sesion.UsuarioId, sesion.OrganizacionId), ct);
            return invalida;
        }

        if (EstadoDe(sesion, ahora) == EstadoSesion.Expirada) return new(EstadoRenovacion.SesionExpirada);

        var nuevo = NuevoToken();
        sesion.Rotar(HashDe(nuevo), ahora);
        await sesiones.GuardarAsync([sesion], ct);
        return new(EstadoRenovacion.Exitosa, sesion, nuevo);
    }

    /// <summary>Se invoca en cada solicitud autenticada; además de validar, cuenta como actividad.</summary>
    public async Task<EstadoSesion> ValidarAsync(Guid sesionId, CancellationToken ct = default)
    {
        var sesion = await sesiones.ObtenerPorIdAsync(sesionId, ct);
        if (sesion is null) return EstadoSesion.Inexistente;

        var ahora = reloj.Ahora;
        var estado = EstadoDe(sesion, ahora);
        if (estado == EstadoSesion.Activa && ahora - sesion.UltimaActividad >= UmbralActividad)
        {
            sesion.RegistrarActividad(ahora);
            await sesiones.GuardarAsync([sesion], ct);
        }
        return estado;
    }

    public async Task CerrarAsync(Guid sesionId, CancellationToken ct = default)
    {
        var sesion = await sesiones.ObtenerPorIdAsync(sesionId, ct);
        if (sesion is null || sesion.RevocadaEn is not null) return;

        sesion.Revocar(reloj.Ahora);
        await sesiones.GuardarAsync([sesion], ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CierreSesion, reloj.Ahora,
            sesion.UsuarioId, sesion.OrganizacionId), ct);
    }

    /// <summary>"Cerrar sesión en todos mis dispositivos".</summary>
    public async Task CerrarTodasAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var abiertas = await sesiones.ObtenerNoRevocadasDeUsuarioAsync(usuarioId, ct);
        if (abiertas.Count == 0) return;

        var ahora = reloj.Ahora;
        foreach (var s in abiertas) s.Revocar(ahora);
        await sesiones.GuardarAsync(abiertas, ct);
        await auditoria.RegistrarAsync(new(TipoEventoIdentidad.CierreSesionTodas, ahora,
            usuarioId, abiertas[0].OrganizacionId), ct);
    }

    private EstadoSesion EstadoDe(Sesion s, DateTimeOffset ahora) =>
        s.EstadoEn(ahora, politica.Inactividad(s.OrganizacionId, s.EsPersonal), politica.DuracionMaxima(s.OrganizacionId, s.EsPersonal));

    public static string HashDe(string token) => GeneradorTokens.HashDe(token);

    private static string NuevoToken() => GeneradorTokens.Nuevo();
}
