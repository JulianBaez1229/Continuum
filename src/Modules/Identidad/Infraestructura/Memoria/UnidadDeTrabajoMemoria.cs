using Continuum.Identidad.Aplicacion.Administracion;

namespace Continuum.Identidad.Infraestructura.Memoria;

/// <summary>
/// Transacción simulada: si el trabajo lanza, el almacén vuelve al estado previo y la excepción se propaga. Así se
/// verifica que un cambio sin su evento de bitácora no puede quedar (RF-AUD-008).
/// Limitación: los objetos mutables (<c>Usuario</c>) se comparten por referencia, de modo que un cambio hecho sobre
/// una cuenta ya cargada no se deshace aquí; con la base de datos real lo deshace la transacción.
/// </summary>
public sealed class UnidadDeTrabajoMemoria(AlmacenMemoria almacen) : IUnidadDeTrabajo
{
    public async Task EjecutarAsync(Func<CancellationToken, Task> trabajo, CancellationToken ct = default)
    {
        var copia = almacen.Capturar();
        try
        {
            await trabajo(ct);
        }
        catch
        {
            almacen.Restaurar(copia);
            throw;
        }
    }
}
