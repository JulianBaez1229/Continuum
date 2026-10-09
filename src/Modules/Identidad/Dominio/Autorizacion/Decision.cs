namespace Continuum.Identidad.Dominio.Autorizacion;

/// <summary>Cómo debe responder la API a una denegación: recurso inexistente para el actor (404) o prohibido (403).</summary>
public enum TipoDenegacion { NoEncontrado, Prohibido }

/// <summary>
/// Resultado de evaluar una solicitud de acceso. Se crea solo con <see cref="Permitir"/> o <see cref="Denegar"/>,
/// de modo que un permiso nunca lleva motivo y una denegación nunca lleva nivel.
/// </summary>
public sealed record Decision
{
    private Decision() { }

    public bool EstaPermitido { get; private init; }

    /// <summary>Amplitud del acceso. Solo tiene valor si está permitido.</summary>
    public NivelAcceso? Nivel { get; private init; }

    /// <summary>404 o 403. Solo tiene valor si está denegado.</summary>
    public TipoDenegacion? Tipo { get; private init; }

    /// <summary>Razón de la denegación. Solo tiene valor si está denegado.</summary>
    public MotivoDenegacion? Motivo { get; private init; }

    public static Decision Permitir(NivelAcceso nivel) => new() { EstaPermitido = true, Nivel = nivel };

    public static Decision Denegar(TipoDenegacion tipo, MotivoDenegacion motivo) =>
        new() { EstaPermitido = false, Tipo = tipo, Motivo = motivo };
}
