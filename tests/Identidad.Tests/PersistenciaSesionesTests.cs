using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Tests;

/// <summary>Mapeo de sesiones sobre SQLite en memoria; Npgsql se valida en el ambiente de pruebas.</summary>
public sealed class PersistenciaSesionesTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly DbContextOptions<IdentidadDbContext> _opciones;
    private readonly Usuario _usuario = Usuario.Crear(Guid.NewGuid(), "x@clinica.test", "h");

    public PersistenciaSesionesTests()
    {
        _conexion.Open();
        _opciones = new DbContextOptionsBuilder<IdentidadDbContext>().UseSqlite(_conexion).Options;
        using var db = new IdentidadDbContext(_opciones);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conexion.Dispose();

    private RepositorioSesionesEf Repo(out IdentidadDbContext db)
    {
        db = new IdentidadDbContext(_opciones);
        return new RepositorioSesionesEf(db);
    }

    private async Task<Sesion> InsertarAsync(string hash, Usuario? usuario = null)
    {
        var sesion = Sesion.Iniciar(usuario ?? _usuario, hash, T0);
        var repo = Repo(out var db);
        await repo.AgregarAsync(sesion);
        await db.DisposeAsync();
        return sesion;
    }

    [Fact]
    public async Task RF_IAM_006_la_sesion_se_persiste_completa()
    {
        var s = await InsertarAsync("hash-1");

        var r = (await Repo(out _).ObtenerPorIdAsync(s.Id))!;
        Assert.Equal(_usuario.Id, r.UsuarioId);
        Assert.Equal(_usuario.OrganizacionId, r.OrganizacionId);
        Assert.True(r.EsPersonal);
        Assert.Equal("hash-1", r.HashRenovacion);
        Assert.Equal(T0, r.CreadaEn);
        Assert.Null(r.RevocadaEn);
    }

    [Fact]
    public async Task RF_IAM_006_se_busca_por_el_hash_vigente_y_por_el_anterior()
    {
        var s = await InsertarAsync("hash-1");

        var repo = Repo(out var db);
        var cargada = (await repo.ObtenerPorIdAsync(s.Id))!;
        cargada.Rotar("hash-2", T0.AddMinutes(1));
        await repo.GuardarAsync([cargada]);
        await db.DisposeAsync();

        var lectura = Repo(out _);
        Assert.Equal(s.Id, (await lectura.ObtenerPorHashRenovacionAsync("hash-2"))!.Id);
        Assert.Equal(s.Id, (await lectura.ObtenerPorHashRenovacionAsync("hash-1"))!.Id);
        Assert.Null(await lectura.ObtenerPorHashRenovacionAsync("otro"));
    }

    [Fact]
    public async Task RF_IAM_006_listar_no_revocadas_excluye_las_revocadas_y_las_de_otros()
    {
        var abierta = await InsertarAsync("h-abierta");
        var cerrada = await InsertarAsync("h-cerrada");
        var ajena = await InsertarAsync("h-ajena", Usuario.Crear(Guid.NewGuid(), "otro@clinica.test", "h"));

        var repo = Repo(out var db);
        var a = (await repo.ObtenerPorIdAsync(cerrada.Id))!;
        a.Revocar(T0);
        await repo.GuardarAsync([a]);
        await db.DisposeAsync();

        var lista = await Repo(out _).ObtenerNoRevocadasDeUsuarioAsync(_usuario.Id);
        Assert.Equal([abierta.Id], lista.Select(s => s.Id));
        Assert.DoesNotContain(lista, s => s.Id == ajena.Id);
    }

    [Fact]
    public async Task RF_IAM_006_dos_renovaciones_simultaneas_del_mismo_token_no_pueden_ganar_ambas()
    {
        var s = await InsertarAsync("hash-1");

        var repoA = Repo(out var dbA);
        var repoB = Repo(out var dbB);
        var a = (await repoA.ObtenerPorIdAsync(s.Id))!;
        var b = (await repoB.ObtenerPorIdAsync(s.Id))!;
        a.Rotar("hash-a", T0);
        b.Rotar("hash-b", T0);

        await repoA.GuardarAsync([a]);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.GuardarAsync([b]));
        await dbA.DisposeAsync();
        await dbB.DisposeAsync();
    }

    [Fact]
    public async Task RF_IAM_006_el_hash_de_renovacion_es_unico()
    {
        await InsertarAsync("repetido");
        var repo = Repo(out _);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.AgregarAsync(Sesion.Iniciar(_usuario, "repetido", T0)));
    }
}
