using System.Reflection;
using Continuum.Identidad.Aplicacion;

namespace Continuum.Identidad.Infraestructura.Seguridad;

/// <summary>
/// Lista local de contraseñas comunes (RF-IAM-001). Es un mínimo: la verificación completa contra bases de
/// contraseñas filtradas (p. ej. Pwned Passwords por k-anonimato) requiere un servicio externo y se aprobará aparte.
/// </summary>
public sealed class ContrasenasFiltradasLista : IContrasenasFiltradas
{
    private readonly HashSet<string> _filtradas;

    public ContrasenasFiltradasLista()
    {
        using var flujo = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Continuum.Identidad.Recursos.contrasenas-filtradas.txt")
            ?? throw new InvalidOperationException("Falta el recurso contrasenas-filtradas.txt.");
        using var lector = new StreamReader(flujo);
        _filtradas = new HashSet<string>(
            lector.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => !l.StartsWith('#')),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool EstaFiltrada(string contrasena) => _filtradas.Contains(contrasena.Trim());
}
