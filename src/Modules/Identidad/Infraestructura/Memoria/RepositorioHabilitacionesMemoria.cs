using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Infraestructura.Memoria;

public sealed class RepositorioHabilitacionesMemoria(AlmacenMemoria almacen) : IRepositorioHabilitaciones
{
    public void Sembrar(Habilitacion habilitacion) => almacen.Habilitaciones[habilitacion.Id] = habilitacion;

    public Task<Habilitacion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(almacen.Habilitaciones.GetValueOrDefault(id));

    public Task<Habilitacion?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default) =>
        Task.FromResult(almacen.Habilitaciones.Values
            .FirstOrDefault(h => h.ProfesionalId == profesionalId && h.EspecialidadId == especialidadId));

    public Task<IReadOnlyList<Habilitacion>> ListarVigentesQueVencenAsync(
        Guid organizacionId, DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Habilitacion>>(almacen.Habilitaciones.Values
            .Where(h => h.OrganizacionId == organizacionId
                        && h.Estado == EstadoHabilitacion.Vigente
                        && h.VigenteHasta >= desde && h.VigenteHasta <= hasta)
            .ToList());

    public Task AgregarAsync(Habilitacion habilitacion, CancellationToken ct = default)
    {
        if (almacen.Habilitaciones.Values.Any(h => h.ProfesionalId == habilitacion.ProfesionalId && h.EspecialidadId == habilitacion.EspecialidadId))
            throw new InvalidOperationException("El profesional ya tiene una habilitación en esa especialidad.");
        almacen.Habilitaciones[habilitacion.Id] = habilitacion;
        return Task.CompletedTask;
    }

    public Task GuardarAsync(Habilitacion habilitacion, CancellationToken ct = default)
    {
        almacen.Habilitaciones[habilitacion.Id] = habilitacion;
        return Task.CompletedTask;
    }
}
