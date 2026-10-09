namespace Continuum.Identidad.Dominio;

public enum EstadoUsuario { Activo, Inactivo }

/// <summary>
/// Cuenta de acceso. Implementa el bloqueo temporal de RF-IAM-004:
/// 5 fallos en 15 minutos bloquean la cuenta; cada bloqueo consecutivo duplica la duración.
/// </summary>
public sealed class Usuario
{
    public const int FallosParaBloquear = 5;
    public static readonly TimeSpan VentanaFallos = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan BloqueoBase = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan BloqueoMaximo = TimeSpan.FromHours(24);

    private readonly List<DateTimeOffset> _fallosRecientes = [];

    private Usuario(Guid id, Guid organizacionId, string correo, string hashContrasena)
    {
        Id = id;
        OrganizacionId = organizacionId;
        Correo = correo;
        HashContrasena = hashContrasena;
    }

    public Guid Id { get; }
    public Guid OrganizacionId { get; }
    public string Correo { get; }
    public string HashContrasena { get; private set; }
    public EstadoUsuario Estado { get; private set; } = EstadoUsuario.Activo;
    public DateTimeOffset? BloqueadoHasta { get; private set; }
    public int BloqueosConsecutivos { get; private set; }
    public DateTimeOffset? UltimoAcceso { get; private set; }

    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    public static Usuario Crear(Guid organizacionId, string correo, string hashContrasena) =>
        new(Guid.NewGuid(), organizacionId, NormalizarCorreo(correo), hashContrasena);

    public void Desactivar() => Estado = EstadoUsuario.Inactivo;

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    /// <summary>Registra un fallo de contraseña. Devuelve true si este fallo provocó un bloqueo nuevo.</summary>
    public bool RegistrarFallo(DateTimeOffset ahora)
    {
        if (EstaBloqueado(ahora)) return false;

        _fallosRecientes.RemoveAll(f => f <= ahora - VentanaFallos);
        _fallosRecientes.Add(ahora);
        if (_fallosRecientes.Count < FallosParaBloquear) return false;

        var duracion = TimeSpan.FromTicks(Math.Min(
            BloqueoBase.Ticks * (1L << Math.Min(BloqueosConsecutivos, 20)),
            BloqueoMaximo.Ticks));
        BloqueadoHasta = ahora + duracion;
        BloqueosConsecutivos++;
        _fallosRecientes.Clear();
        return true;
    }

    public void RegistrarExito(DateTimeOffset ahora)
    {
        _fallosRecientes.Clear();
        BloqueadoHasta = null;
        BloqueosConsecutivos = 0;
        UltimoAcceso = ahora;
    }
}
