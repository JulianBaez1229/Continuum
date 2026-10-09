namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary>
/// Quién ejecuta el caso de uso y en qué sede. El rol <c>ADMIN_FUNCIONAL</c> se comprueba contra el perfil
/// en el backend en cada llamada; el actor no declara sus roles.
/// </summary>
public sealed record ActorAdministracion(Guid UsuarioId, Guid SedeActivaId);

/// <summary>Cómo debe responder el adaptador HTTP: 2xx, 404, 403, 422 o 409.</summary>
public enum EstadoOperacion { Exitosa, NoEncontrado, Prohibido, Invalida, Conflicto }

/// <param name="Id">Identificador de lo creado, cuando aplica.</param>
/// <param name="Codigo">Código estable en mayúsculas (<c>CORREO_DUPLICADO</c>…) para mostrar el motivo al administrador.</param>
public sealed record ResultadoOperacion(EstadoOperacion Estado, Guid? Id = null, string? Codigo = null)
{
    public static ResultadoOperacion Exito(Guid? id = null) => new(EstadoOperacion.Exitosa, id);
    public static ResultadoOperacion NoEncontrado(string? codigo = null) => new(EstadoOperacion.NoEncontrado, Codigo: codigo);
    public static ResultadoOperacion Prohibido(string codigo) => new(EstadoOperacion.Prohibido, Codigo: codigo);
    public static ResultadoOperacion Invalida(string codigo) => new(EstadoOperacion.Invalida, Codigo: codigo);
    public static ResultadoOperacion Conflicto(string codigo) => new(EstadoOperacion.Conflicto, Codigo: codigo);
}
