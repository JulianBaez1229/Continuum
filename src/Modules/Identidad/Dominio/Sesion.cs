namespace Continuum.Identidad.Dominio;

public enum EstadoSesion { Activa, Expirada, Revocada, Inexistente }

/// <summary>
/// Sesión de un dispositivo (RF-IAM-006). El token de renovación nunca se guarda: solo su hash.
/// Se conserva el hash anterior para detectar la reutilización de un token ya rotado.
/// </summary>
public sealed class Sesion
{
    private Sesion(Guid id, Guid usuarioId, Guid organizacionId, bool esPersonal, string hashRenovacion, DateTimeOffset creadaEn)
    {
        Id = id;
        UsuarioId = usuarioId;
        OrganizacionId = organizacionId;
        EsPersonal = esPersonal;
        HashRenovacion = hashRenovacion;
        CreadaEn = creadaEn;
        UltimaActividad = creadaEn;
    }

    public Guid Id { get; }
    public Guid UsuarioId { get; }
    public Guid OrganizacionId { get; }
    /// <summary>Personal (15 min de inactividad por defecto) o paciente (30 min); ver <c>IPoliticaSesion</c>.</summary>
    public bool EsPersonal { get; }
    public string HashRenovacion { get; private set; }
    public string? HashRenovacionAnterior { get; private set; }
    public DateTimeOffset CreadaEn { get; }
    public DateTimeOffset UltimaActividad { get; private set; }
    public DateTimeOffset? RevocadaEn { get; private set; }

    public static Sesion Iniciar(Usuario usuario, string hashRenovacion, DateTimeOffset ahora) =>
        new(Guid.NewGuid(), usuario.Id, usuario.OrganizacionId, usuario.RequiereMfa, hashRenovacion, ahora);

    public EstadoSesion EstadoEn(DateTimeOffset ahora, TimeSpan inactividad, TimeSpan duracionMaxima)
    {
        if (RevocadaEn is not null) return EstadoSesion.Revocada;
        if (ahora - CreadaEn >= duracionMaxima || ahora - UltimaActividad >= inactividad) return EstadoSesion.Expirada;
        return EstadoSesion.Activa;
    }

    public void RegistrarActividad(DateTimeOffset ahora) => UltimaActividad = ahora;

    public void Rotar(string nuevoHash, DateTimeOffset ahora)
    {
        HashRenovacionAnterior = HashRenovacion;
        HashRenovacion = nuevoHash;
        UltimaActividad = ahora;
    }

    public void Revocar(DateTimeOffset ahora) => RevocadaEn ??= ahora;
}
