namespace Continuum.Identidad.Aplicacion;

public enum MotivoContrasenaInvalida { Corta, Filtrada }

public readonly record struct ResultadoContrasena(bool EsValida, MotivoContrasenaInvalida? Motivo = null);

/// <summary>RF-IAM-001: mínimo 12 caracteres y no filtrada; sin reglas de composición.</summary>
public sealed class PoliticaContrasena(IContrasenasFiltradas filtradas)
{
    public const int LongitudMinima = 12;

    public ResultadoContrasena Validar(string? contrasena)
    {
        if (contrasena is null || contrasena.Length < LongitudMinima)
            return new(false, MotivoContrasenaInvalida.Corta);
        if (filtradas.EstaFiltrada(contrasena))
            return new(false, MotivoContrasenaInvalida.Filtrada);
        return new(true);
    }
}
