using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Infraestructura.Persistencia;

/// <summary>
/// Como en <see cref="RepositorioUsuariosEf"/>, cada escritura renueva <c>row_version</c>: dos renovaciones
/// simultáneas del mismo token no pueden ambas tener éxito (la perdedora recibe <see cref="DbUpdateConcurrencyException"/>).
/// </summary>
public sealed class RepositorioSesionesEf(IdentidadDbContext db) : ISesiones
{
    public async Task AgregarAsync(Sesion sesion, CancellationToken ct = default)
    {
        db.Sesiones.Add(sesion);
        db.Entry(sesion).Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }

    public Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Sesiones.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Sesion?> ObtenerPorHashRenovacionAsync(string hash, CancellationToken ct = default) =>
        db.Sesiones.FirstOrDefaultAsync(s => s.HashRenovacion == hash || s.HashRenovacionAnterior == hash, ct);

    public async Task<IReadOnlyList<Sesion>> ObtenerNoRevocadasDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        await db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.RevocadaEn == null).ToListAsync(ct);

    public async Task GuardarAsync(IEnumerable<Sesion> sesiones, CancellationToken ct = default)
    {
        db.ChangeTracker.DetectChanges();
        foreach (var sesion in sesiones)
        {
            var entrada = db.Entry(sesion);
            if (entrada.State == EntityState.Modified)
                entrada.Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        }
        await db.SaveChangesAsync(ct);
    }
}
