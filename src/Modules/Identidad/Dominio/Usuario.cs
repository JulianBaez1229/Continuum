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

    private List<DateTimeOffset> _fallosRecientes = [];

    private List<string> _codigosRecuperacionHash = [];

    private Usuario(Guid id, Guid organizacionId, string correo, string hashContrasena, bool requiereMfa)
    {
        Id = id;
        OrganizacionId = organizacionId;
        Correo = correo;
        HashContrasena = hashContrasena;
        RequiereMfa = requiereMfa;
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

    /// <summary>RF-IAM-002: obligatorio para todos los roles salvo PACIENTE y RED_APOYO.</summary>
    public bool RequiereMfa { get; }
    public bool MfaHabilitado { get; private set; }
    public string? SecretoTotpProtegido { get; private set; }
    public string? SecretoTotpPendienteProtegido { get; private set; }
    public long? UltimoPasoTotp { get; private set; }
    public IReadOnlyList<string> CodigosRecuperacionHash => _codigosRecuperacionHash;

    /// <param name="requiereMfa">Seguro por defecto: solo pacientes y red de apoyo se crean con false.</param>
    public static Usuario Crear(Guid organizacionId, string correo, string hashContrasena, bool requiereMfa = true) =>
        new(Guid.NewGuid(), organizacionId, NormalizarCorreo(correo), hashContrasena, requiereMfa);

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

    /// <summary>Guarda un secreto pendiente de confirmar. Falla si el MFA ya está activo (no se puede sustituir).</summary>
    public bool PrepararMfa(string secretoProtegido)
    {
        if (MfaHabilitado) return false;
        SecretoTotpPendienteProtegido = secretoProtegido;
        return true;
    }

    public void ActivarMfa(long pasoConfirmado, IEnumerable<string> codigosRecuperacionHash)
    {
        if (SecretoTotpPendienteProtegido is null) throw new InvalidOperationException("No hay una configuración de MFA pendiente.");
        SecretoTotpProtegido = SecretoTotpPendienteProtegido;
        SecretoTotpPendienteProtegido = null;
        MfaHabilitado = true;
        UltimoPasoTotp = pasoConfirmado;
        _codigosRecuperacionHash.Clear();
        _codigosRecuperacionHash.AddRange(codigosRecuperacionHash);
    }

    public void RegistrarPasoTotp(long paso) => UltimoPasoTotp = paso;

    /// <summary>Cambia la contraseña y levanta el bloqueo por intentos fallidos. No toca el MFA.</summary>
    public void CambiarContrasena(string nuevoHash)
    {
        HashContrasena = nuevoHash;
        ReiniciarFallos();
    }

    /// <summary>Levanta el bloqueo y borra los fallos (tras una recuperación o una reautenticación correctas).</summary>
    public void ReiniciarFallos()
    {
        _fallosRecientes.Clear();
        BloqueadoHasta = null;
        BloqueosConsecutivos = 0;
    }

    public void ConsumirCodigoRecuperacion(string hash) => _codigosRecuperacionHash.Remove(hash);

    /// <summary>Cierra el inicio de sesión: solo tras superar todos los factores exigidos.</summary>
    public void RegistrarExito(DateTimeOffset ahora)
    {
        _fallosRecientes.Clear();
        BloqueadoHasta = null;
        BloqueosConsecutivos = 0;
        UltimoAcceso = ahora;
    }
}
