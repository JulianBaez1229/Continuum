using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Infraestructura.Persistencia;

/// <summary>
/// Cada escritura renueva <c>row_version</c>: si dos solicitudes paralelas leen el mismo estado, la segunda falla
/// con <see cref="DbUpdateConcurrencyException"/> en lugar de perder un fallo de inicio de sesión (RF-IAM-004).
/// Quien llama debe tratar esa excepción como una denegación, nunca como un éxito.
/// </summary>
public sealed class RepositorioUsuariosEf(IdentidadDbContext db) : IRepositorioUsuarios
{
    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default)
    {
        var normalizado = Usuario.NormalizarCorreo(correo);
        return db.Usuarios.FirstOrDefaultAsync(u => u.Correo == normalizado, ct);
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AgregarAsync(Usuario usuario, CancellationToken ct = default)
    {
        db.Usuarios.Add(usuario);
        db.Entry(usuario).Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }

    public async Task GuardarAsync(Usuario usuario, CancellationToken ct = default)
    {
        db.ChangeTracker.DetectChanges();
        var entrada = db.Entry(usuario);
        if (entrada.State == EntityState.Modified)
            entrada.Property<Guid>(IdentidadDbContext.ColumnaVersion).CurrentValue = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
    }
}
