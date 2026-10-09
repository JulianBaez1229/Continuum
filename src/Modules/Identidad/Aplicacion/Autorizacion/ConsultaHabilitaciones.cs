using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Aplicacion.Autorizacion;

public sealed class ConsultaHabilitaciones(IRepositorioHabilitaciones habilitaciones) : IHabilitaciones
{
    public async Task<HabilitacionProfesional?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default)
    {
        var habilitacion = await habilitaciones.ObtenerAsync(profesionalId, especialidadId, ct);
        return habilitacion is { Estado: EstadoHabilitacion.Vigente } ? habilitacion.ComoHecho() : null;
    }
}
