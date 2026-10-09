using System.Text.Json;
using Continuum.Identidad.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Continuum.Identidad.Infraestructura.Persistencia;

/// <summary>
/// Mapeo de la identidad. <c>usuario</c> es la entidad del módulo 05; las credenciales viven en
/// <c>credencial_usuario</c> ("credenciales en el servicio de identidad") compartiendo la clave.
/// Las migraciones son un contrato protegido del Dev B: este contexto solo define el mapeo.
/// </summary>
public sealed class IdentidadDbContext(DbContextOptions<IdentidadDbContext> opciones) : DbContext(opciones)
{
    public const string ColumnaVersion = "row_version";

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Usuario>(u =>
        {
            u.ToTable("usuario");
            u.HasKey(x => x.Id);
            u.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            u.Property(x => x.OrganizacionId).HasColumnName("organizacion_id");
            u.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(254).IsRequired();
            u.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
            u.Property(x => x.RequiereMfa).HasColumnName("requiere_mfa");
            u.Property(x => x.MfaHabilitado).HasColumnName("mfa_habilitado");
            u.Property(x => x.UltimoAcceso).HasColumnName("ultimo_acceso");
            u.Property<Guid>(ColumnaVersion).IsConcurrencyToken();
            u.HasIndex(x => x.Correo).IsUnique();
            u.HasIndex(x => x.OrganizacionId);

            u.SplitToTable("credencial_usuario", t =>
            {
                t.Property(x => x.Id).HasColumnName("usuario_id");
                t.Property(x => x.HashContrasena).HasColumnName("hash_contrasena");
                t.Property(x => x.SecretoTotpProtegido).HasColumnName("secreto_totp_protegido");
                t.Property(x => x.SecretoTotpPendienteProtegido).HasColumnName("secreto_totp_pendiente_protegido");
                t.Property(x => x.UltimoPasoTotp).HasColumnName("ultimo_paso_totp");
                t.Property(x => x.BloqueadoHasta).HasColumnName("bloqueado_hasta");
                t.Property(x => x.BloqueosConsecutivos).HasColumnName("bloqueos_consecutivos");
                t.Property<List<DateTimeOffset>>("_fallosRecientes").HasColumnName("fallos_recientes");
                t.Property<List<string>>("_codigosRecuperacionHash").HasColumnName("codigos_recuperacion_hash");
            });

            u.Property(x => x.HashContrasena).IsRequired();
            u.Property<List<DateTimeOffset>>("_fallosRecientes").HasConversion(Json<DateTimeOffset>(), Comparador<DateTimeOffset>());
            u.Property<List<string>>("_codigosRecuperacionHash").HasConversion(Json<string>(), Comparador<string>());
        });
    }

    private static ValueConverter<List<T>, string> Json<T>() => new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        s => JsonSerializer.Deserialize<List<T>>(s, (JsonSerializerOptions?)null) ?? new List<T>());

    private static ValueComparer<List<T>> Comparador<T>() => new(
        (a, b) => a!.SequenceEqual(b!),
        v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x)),
        v => v.ToList());
}
