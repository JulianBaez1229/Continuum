namespace Continuum.Identidad.Dominio;

public enum PropositoToken { RecuperacionContrasena, ActivacionCuenta, CodigoMfaCorreo }

/// <summary>
/// Token o código de un solo uso con caducidad (recuperación de contraseña, activación de cuenta, código por correo).
/// Solo se guarda su hash; quien lo recibe lo recibe una única vez por el canal de entrega.
/// </summary>
public sealed class TokenAccion
{
    private TokenAccion(Guid id, PropositoToken proposito, Guid? usuarioId, Guid? pacienteId, Guid organizacionId,
        string hashToken, DateTimeOffset creadoEn, DateTimeOffset expiraEn)
    {
        Id = id;
        Proposito = proposito;
        UsuarioId = usuarioId;
        PacienteId = pacienteId;
        OrganizacionId = organizacionId;
        HashToken = hashToken;
        CreadoEn = creadoEn;
        ExpiraEn = expiraEn;
    }

    public Guid Id { get; }
    public PropositoToken Proposito { get; }
    public Guid? UsuarioId { get; }
    /// <summary>Paciente al que corresponde una invitación (la cuenta aún no existe).</summary>
    public Guid? PacienteId { get; }
    public Guid OrganizacionId { get; }
    public string HashToken { get; }
    public DateTimeOffset CreadoEn { get; }
    public DateTimeOffset ExpiraEn { get; }
    public DateTimeOffset? UsadoEn { get; private set; }
    public int Intentos { get; private set; }

    public static TokenAccion Crear(PropositoToken proposito, Guid? usuarioId, Guid? pacienteId, Guid organizacionId,
        string hashToken, DateTimeOffset ahora, TimeSpan vigencia) =>
        new(Guid.NewGuid(), proposito, usuarioId, pacienteId, organizacionId, hashToken, ahora, ahora + vigencia);

    public bool EstaVigente(DateTimeOffset ahora) => UsadoEn is null && ahora < ExpiraEn;

    public void MarcarUsado(DateTimeOffset ahora) => UsadoEn ??= ahora;

    /// <summary>Deja de servir sin haberse usado (p. ej. al emitir uno nuevo). Misma marca que el uso.</summary>
    public void Invalidar(DateTimeOffset ahora) => UsadoEn ??= ahora;

    /// <summary>Cuenta un intento fallido; al llegar al máximo el token se invalida. Devuelve true si quedó invalidado.</summary>
    public bool RegistrarIntentoFallido(DateTimeOffset ahora, int maximo)
    {
        Intentos++;
        if (Intentos < maximo) return false;
        Invalidar(ahora);
        return true;
    }
}
