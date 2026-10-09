using Continuum.Identidad.Dominio;
using Continuum.Identidad.Infraestructura.Persistencia;
using Continuum.Identidad.Infraestructura.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Continuum.Identidad.Tests;

/// <summary>Mapeo de tokens de un solo uso sobre SQLite en memoria; Npgsql se valida en el ambiente de pruebas.</summary>
public sealed class PersistenciaTokensTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly DbContextOptions<IdentidadDbContext> _opciones;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly Guid _organizacionId = Guid.NewGuid();

    public PersistenciaTokensTests()
    {
        _conexion.Open();
        _opciones = new DbContextOptionsBuilder<IdentidadDbContext>().UseSqlite(_conexion).Options;
        using var db = new IdentidadDbContext(_opciones);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conexion.Dispose();

    private RepositorioTokensAccionEf Repo(out IdentidadDbContext db)
    {
        db = new IdentidadDbContext(_opciones);
        return new RepositorioTokensAccionEf(db);
    }

    private async Task<TokenAccion> InsertarAsync(string hash, PropositoToken proposito = PropositoToken.RecuperacionContrasena, Guid? usuarioId = null, Guid? pacienteId = null)
    {
        var token = TokenAccion.Crear(proposito, usuarioId ?? _usuarioId, pacienteId, _organizacionId, hash, T0, TimeSpan.FromMinutes(30));
        var repo = Repo(out var db);
        await repo.AgregarAsync(token);
        await db.DisposeAsync();
        return token;
    }

    [Fact]
    public async Task RF_IAM_005_el_token_se_persiste_completo()
    {
        var t = await InsertarAsync("hash-1", pacienteId: Guid.NewGuid());

        var r = (await Repo(out _).ObtenerPorHashAsync("hash-1"))!;
        Assert.Equal(t.Id, r.Id);
        Assert.Equal(PropositoToken.RecuperacionContrasena, r.Proposito);
        Assert.Equal(_usuarioId, r.UsuarioId);
        Assert.Equal(t.PacienteId, r.PacienteId);
        Assert.Equal(_organizacionId, r.OrganizacionId);
        Assert.Equal(T0.AddMinutes(30), r.ExpiraEn);
        Assert.Null(r.UsadoEn);
        Assert.Null(await Repo(out _).ObtenerPorHashAsync("otro"));
    }

    [Fact]
    public async Task RF_IAM_005_listar_pendientes_filtra_por_usuario_proposito_y_uso()
    {
        var pendiente = await InsertarAsync("h-pendiente");
        var usado = await InsertarAsync("h-usado");
        await InsertarAsync("h-otro-proposito", PropositoToken.ActivacionCuenta);
        await InsertarAsync("h-ajeno", usuarioId: Guid.NewGuid());

        var repo = Repo(out var db);
        var u = (await repo.ObtenerPorHashAsync("h-usado"))!;
        u.MarcarUsado(T0);
        await repo.GuardarAsync([u]);
        await db.DisposeAsync();

        var lista = await Repo(out _).ObtenerPendientesDeUsuarioAsync(_usuarioId, PropositoToken.RecuperacionContrasena);
        Assert.Equal([pendiente.Id], lista.Select(t => t.Id));
        Assert.DoesNotContain(lista, t => t.Id == usado.Id);
    }

    [Fact]
    public async Task RF_IAM_005_dos_usos_simultaneos_del_mismo_token_no_pueden_ganar_ambos()
    {
        await InsertarAsync("hash-1");

        var repoA = Repo(out var dbA);
        var repoB = Repo(out var dbB);
        var a = (await repoA.ObtenerPorHashAsync("hash-1"))!;
        var b = (await repoB.ObtenerPorHashAsync("hash-1"))!;
        a.MarcarUsado(T0);
        b.MarcarUsado(T0);

        await repoA.GuardarAsync([a]);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.GuardarAsync([b]));
        await dbA.DisposeAsync();
        await dbB.DisposeAsync();
    }

    [Fact]
    public async Task RF_IAM_005_el_hash_del_token_es_unico()
    {
        await InsertarAsync("repetido");
        await Assert.ThrowsAsync<DbUpdateException>(() => InsertarAsync("repetido"));
    }
}

public class ContrasenasFiltradasListaTests
{
    private readonly ContrasenasFiltradasLista _lista = new();

    [Theory]
    [InlineData("password1234")]
    [InlineData("PASSWORD1234")]
    [InlineData("qwertyuiop12")]
    [InlineData("  contrasena123 ")]
    public void RF_IAM_001_rechaza_contrasenas_comunes_sin_distinguir_mayusculas(string contrasena) =>
        Assert.True(_lista.EstaFiltrada(contrasena));

    [Theory]
    [InlineData("una clave larga y segura")]
    [InlineData("# comentario")]
    public void RF_IAM_001_acepta_contrasenas_que_no_estan_en_la_lista(string contrasena) =>
        Assert.False(_lista.EstaFiltrada(contrasena));
}
