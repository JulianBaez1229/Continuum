using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Infraestructura.Persistencia;

/// <summary>
/// <c>row_version</c> garantiza que dos solicitudes simultáneas no consuman el mismo token dos veces: la segunda
/// recibe <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
public sealed class RepositorioTokensAccionEf(IdentidadDbContext db) : ITokensAccion
{
    public async Task AgregarAsync(TokenAccion token, CancellationToken ct = default)
    {
        db.TokensAccion.Add(token);
        db.Entry(token).Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }

    public Task<TokenAccion?> ObtenerPorHashAsync(string hash, CancellationToken ct = default) =>
        db.TokensAccion.FirstOrDefaultAsync(t => t.HashToken == hash, ct);

    public async Task<IReadOnlyList<TokenAccion>> ObtenerPendientesDeUsuarioAsync(
        Guid usuarioId, PropositoToken proposito, CancellationToken ct = default) =>
        await db.TokensAccion.Where(t => t.UsuarioId == usuarioId && t.Proposito == proposito && t.UsadoEn == null).ToListAsync(ct);

    public async Task GuardarAsync(IEnumerable<TokenAccion> tokens, CancellationToken ct = default)
    {
        db.ChangeTracker.DetectChanges();
        foreach (var token in tokens)
        {
            var entrada = db.Entry(token);
            if (entrada.State == EntityState.Modified)
                entrada.Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        }
        await db.SaveChangesAsync(ct);
    }
}
