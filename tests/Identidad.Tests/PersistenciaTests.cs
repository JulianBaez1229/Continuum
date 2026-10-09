using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Tests;

/// <summary>
/// Verifica el mapeo de EF Core sobre SQLite en memoria (no hay PostgreSQL en CI local).
/// Las particularidades de Npgsql se validan en el ambiente de pruebas con PostgreSQL real.
/// </summary>
public sealed class PersistenciaTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly DbContextOptions<IdentidadDbContext> _opciones;

    public PersistenciaTests()
    {
        _conexion.Open();
        _opciones = new DbContextOptionsBuilder<IdentidadDbContext>().UseSqlite(_conexion).Options;
        using var db = new IdentidadDbContext(_opciones);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conexion.Dispose();

    private RepositorioUsuariosEf NuevoRepo(out IdentidadDbContext db)
    {
        db = new IdentidadDbContext(_opciones);
        return new RepositorioUsuariosEf(db);
    }

    private async Task<Usuario> InsertarAsync(string correo = "psi@clinica.test", bool requiereMfa = true)
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), correo, "hash", requiereMfa);
        var repo = NuevoRepo(out var db);
        await repo.AgregarAsync(usuario);
        await db.DisposeAsync();
        return usuario;
    }

    [Fact]
    public async Task RF_IAM_001_se_busca_por_correo_normalizado_y_por_id()
    {
        var u = await InsertarAsync("Psi@Clinica.test");
        var repo = NuevoRepo(out _);

        Assert.Equal(u.Id, (await repo.ObtenerPorCorreoAsync("  PSI@clinica.TEST "))!.Id);
        Assert.Equal(u.Id, (await repo.ObtenerPorIdAsync(u.Id))!.Id);
        Assert.Null(await repo.ObtenerPorCorreoAsync("otro@clinica.test"));
    }

    [Fact]
    public async Task RF_IAM_004_el_estado_de_bloqueo_sobrevive_a_la_persistencia()
    {
        var u = await InsertarAsync();
        var ahora = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        var repo = NuevoRepo(out var db);
        var cargado = (await repo.ObtenerPorIdAsync(u.Id))!;
        for (var i = 0; i < 5; i++) cargado.RegistrarFallo(ahora);
        await repo.GuardarAsync(cargado);
        await db.DisposeAsync();

        var relectura = (await NuevoRepo(out _).ObtenerPorIdAsync(u.Id))!;
        Assert.True(relectura.EstaBloqueado(ahora.AddMinutes(14)));
        Assert.False(relectura.EstaBloqueado(ahora.AddMinutes(15)));
        Assert.Equal(1, relectura.BloqueosConsecutivos);
    }

    [Fact]
    public async Task RF_IAM_004_los_fallos_recientes_se_conservan_entre_solicitudes()
    {
        var u = await InsertarAsync();
        var ahora = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        var repo = NuevoRepo(out var db);
        var a = (await repo.ObtenerPorIdAsync(u.Id))!;
        a.RegistrarFallo(ahora);
        a.RegistrarFallo(ahora);
        a.RegistrarFallo(ahora);
        await repo.GuardarAsync(a);
        await db.DisposeAsync();

        var repo2 = NuevoRepo(out var db2);
        var b = (await repo2.ObtenerPorIdAsync(u.Id))!;
        Assert.False(b.RegistrarFallo(ahora)); // 4.º fallo: aún no bloquea
        Assert.True(b.RegistrarFallo(ahora));  // 5.º fallo: bloquea
        await db2.DisposeAsync();
    }

    [Fact]
    public async Task RF_IAM_002_la_configuracion_de_mfa_se_persiste_completa()
    {
        var u = await InsertarAsync();
        var repo = NuevoRepo(out var db);
        var cargado = (await repo.ObtenerPorIdAsync(u.Id))!;
        cargado.PrepararMfa("secreto-protegido");
        cargado.ActivarMfa(12345, ["h1", "h2", "h3"]);
        cargado.ConsumirCodigoRecuperacion("h2");
        await repo.GuardarAsync(cargado);
        await db.DisposeAsync();

        var r = (await NuevoRepo(out _).ObtenerPorIdAsync(u.Id))!;
        Assert.True(r.MfaHabilitado);
        Assert.Equal("secreto-protegido", r.SecretoTotpProtegido);
        Assert.Null(r.SecretoTotpPendienteProtegido);
        Assert.Equal(12345, r.UltimoPasoTotp);
        Assert.Equal(["h1", "h3"], r.CodigosRecuperacionHash);
    }

    [Fact]
    public async Task RF_IAM_001_los_datos_basicos_se_persisten()
    {
        var u = await InsertarAsync(requiereMfa: false);
        var r = (await NuevoRepo(out _).ObtenerPorIdAsync(u.Id))!;
        Assert.Equal(u.OrganizacionId, r.OrganizacionId);
        Assert.Equal("hash", r.HashContrasena);
        Assert.False(r.RequiereMfa);
        Assert.Equal(EstadoUsuario.Activo, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_004_dos_escrituras_concurrentes_no_se_pisan_en_silencio()
    {
        var u = await InsertarAsync();
        var ahora = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        var repoA = NuevoRepo(out var dbA);
        var repoB = NuevoRepo(out var dbB);
        var a = (await repoA.ObtenerPorIdAsync(u.Id))!;
        var b = (await repoB.ObtenerPorIdAsync(u.Id))!;
        a.RegistrarFallo(ahora);
        b.RegistrarFallo(ahora);

        await repoA.GuardarAsync(a);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.GuardarAsync(b));
        await dbA.DisposeAsync();
        await dbB.DisposeAsync();
    }

    [Fact]
    public async Task RF_IAM_003_el_segundo_factor_por_mensaje_se_persiste()
    {
        var u = await InsertarAsync("pac@correo.test", requiereMfa: false);
        var repo = NuevoRepo(out var db);
        var cargado = (await repo.ObtenerPorIdAsync(u.Id))!;
        Assert.False(cargado.MfaPorMensajeHabilitado);
        cargado.HabilitarMfaPorMensaje();
        await repo.GuardarAsync(cargado);
        await db.DisposeAsync();

        Assert.True((await NuevoRepo(out _).ObtenerPorIdAsync(u.Id))!.MfaPorMensajeHabilitado);
    }

    [Fact]
    public async Task RF_IAM_001_no_se_repite_el_correo()
    {
        await InsertarAsync("dup@clinica.test");
        var repo = NuevoRepo(out _);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.AgregarAsync(Usuario.Crear(Guid.NewGuid(), "DUP@clinica.test", "hash")));
    }
}
