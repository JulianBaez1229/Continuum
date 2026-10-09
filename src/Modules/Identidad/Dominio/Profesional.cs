namespace Continuum.Identidad.Dominio;

/// <summary>
/// Ficha de <c>profesional</c> (módulo 05): la persona que atiende, ligada a su cuenta de usuario.
/// Los números de licencia y exequátur son datos de identificación profesional: no van a la bitácora ni a los logs.
/// </summary>
public sealed record Profesional
{
    private Profesional() { }

    public Guid Id { get; private init; }
    public Guid UsuarioId { get; private init; }
    public Guid OrganizacionId { get; private init; }
    public string Nombres { get; private init; } = "";
    public string Apellidos { get; private init; } = "";
    public string TipoProfesional { get; private init; } = "";
    public string? Licencia { get; private init; }
    public string? Exequatur { get; private init; }

    /// <summary>Código del primer dato inválido (<c>NOMBRE_REQUERIDO</c>, <c>SIN_LICENCIA_NI_EXEQUATUR</c>) o null si es válido.</summary>
    public static string? Validar(string nombres, string apellidos, string tipoProfesional, string? licencia, string? exequatur)
    {
        if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrWhiteSpace(tipoProfesional))
            return "NOMBRE_REQUERIDO";
        if (string.IsNullOrWhiteSpace(licencia) && string.IsNullOrWhiteSpace(exequatur))
            return "SIN_LICENCIA_NI_EXEQUATUR";
        return null;
    }

    /// <summary>RF-ROL-003: exige licencia o exequátur. Lanza si los datos no pasan <see cref="Validar"/>.</summary>
    public static Profesional Crear(Guid usuarioId, Guid organizacionId, string nombres, string apellidos,
        string tipoProfesional, string? licencia, string? exequatur)
    {
        if (Validar(nombres, apellidos, tipoProfesional, licencia, exequatur) is { } error)
            throw new ArgumentException($"Datos de profesional inválidos: {error}.");

        return new Profesional
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            OrganizacionId = organizacionId,
            Nombres = nombres.Trim(),
            Apellidos = apellidos.Trim(),
            TipoProfesional = tipoProfesional.Trim(),
            Licencia = string.IsNullOrWhiteSpace(licencia) ? null : licencia.Trim(),
            Exequatur = string.IsNullOrWhiteSpace(exequatur) ? null : exequatur.Trim(),
        };
    }
}
