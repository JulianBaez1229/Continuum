namespace Continuum.Identidad.Aplicacion.Administracion;

/// <summary>
/// La base de datos rechazó una escritura por un índice único (correo, rol ya asignado, habilitación repetida). Los
/// repositorios traducen la excepción de su proveedor a esta; las comprobaciones previas de los casos de uso sirven
/// para dar un mensaje claro, pero solo el índice cierra la carrera entre dos solicitudes simultáneas.
/// </summary>
public sealed class ViolacionDeUnicidadException(string mensaje) : Exception(mensaje);

public static class UnidadDeTrabajoExtensiones
{
    /// <summary>
    /// Ejecuta en una transacción un trabajo que <b>primero comprueba y después escribe</b>: si devuelve algo distinto
    /// de éxito no debe haber escrito nada. Toda lectura que decide la escritura (último administrador, duplicados) va
    /// dentro, para que la transacción real pueda protegerla con su aislamiento. Una violación de unicidad se vuelve
    /// <c>Conflicto</c> con <paramref name="codigoUnicidad"/>, no un error 500.
    /// </summary>
    public static async Task<ResultadoOperacion> EjecutarAsync(
        this IUnidadDeTrabajo unidadDeTrabajo, Func<CancellationToken, Task<ResultadoOperacion>> trabajo,
        string codigoUnicidad, CancellationToken ct = default)
    {
        var resultado = ResultadoOperacion.Exito();
        try
        {
            await unidadDeTrabajo.EjecutarAsync(async c => resultado = await trabajo(c), ct);
        }
        catch (ViolacionDeUnicidadException)
        {
            return ResultadoOperacion.Conflicto(codigoUnicidad);
        }
        return resultado;
    }
}
