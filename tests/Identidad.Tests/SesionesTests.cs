using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

internal sealed class SesionesFalsas : ISesiones
{
    public List<Sesion> Todas { get; } = [];
    public int Guardados { get; private set; }

    public Task AgregarAsync(Sesion s, CancellationToken ct = default) { Todas.Add(s); return Task.CompletedTask; }
    public Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Todas.FirstOrDefault(s => s.Id == id));
    public Task<Sesion?> ObtenerPorHashRenovacionAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(Todas.FirstOrDefault(s => s.HashRenovacion == hash || s.HashRenovacionAnterior == hash));
    public Task<IReadOnlyList<Sesion>> ObtenerNoRevocadasDeUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Sesion>>(Todas.Where(s => s.UsuarioId == usuarioId && s.RevocadaEn is null).ToList());
    public Task GuardarAsync(IEnumerable<Sesion> sesiones, CancellationToken ct = default) { Guardados++; return Task.CompletedTask; }
}

internal sealed class PoliticaSesionFalsa : IPoliticaSesion
{
    public TimeSpan Inactividad(Guid organizacionId, bool esPersonal) =>
        TimeSpan.FromMinutes(esPersonal ? 15 : 30);   // módulo 06: duracion_sesion_inactiva_min
    public TimeSpan DuracionMaxima(Guid organizacionId, bool esPersonal) => TimeSpan.FromHours(12);
}

public class SesionesTests
{
    private readonly RelojFalso _reloj = new();
    private readonly SesionesFalsas _repo = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly ServicioSesiones _servicio;
    private readonly Usuario _personal = Usuario.Crear(Guid.NewGuid(), "rec@clinica.test", "h");
    private readonly Usuario _paciente = Usuario.Crear(Guid.NewGuid(), "pac@correo.test", "h", requiereMfa: false);

    public SesionesTests() =>
        _servicio = new ServicioSesiones(_repo, new PoliticaSesionFalsa(), _reloj, _auditoria);

    // ---- Inicio y rotación ----

    [Fact]
    public async Task RF_IAM_006_iniciar_crea_una_sesion_activa_y_guarda_solo_el_hash_del_token()
    {
        var (sesion, token) = await _servicio.IniciarAsync(_personal);

        Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(sesion.Id));
        Assert.Equal(_personal.Id, sesion.UsuarioId);
        Assert.NotEqual(token, sesion.HashRenovacion);
        Assert.DoesNotContain(token, sesion.HashRenovacion);
    }

    [Fact]
    public async Task RF_IAM_006_cada_sesion_recibe_un_token_distinto()
    {
        var (_, a) = await _servicio.IniciarAsync(_personal);
        var (_, b) = await _servicio.IniciarAsync(_personal);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task RF_IAM_006_renovar_entrega_un_token_nuevo_y_rota_el_hash()
    {
        var (sesion, token) = await _servicio.IniciarAsync(_personal);
        var anterior = sesion.HashRenovacion;

        var r = await _servicio.RenovarAsync(token);

        Assert.Equal(EstadoRenovacion.Exitosa, r.Estado);
        Assert.NotEqual(token, r.TokenRenovacion);
        Assert.NotEqual(anterior, sesion.HashRenovacion);
        Assert.Equal(sesion.Id, r.Sesion!.Id);
    }

    [Fact]
    public async Task RF_IAM_006_reutilizar_un_token_ya_rotado_revoca_toda_la_sesion()
    {
        var (sesion, token) = await _servicio.IniciarAsync(_personal);
        var rotado = await _servicio.RenovarAsync(token);

        var reuso = await _servicio.RenovarAsync(token); // el atacante (o el cliente) presenta el viejo

        Assert.Equal(EstadoRenovacion.Invalida, reuso.Estado);
        Assert.Equal(EstadoSesion.Revocada, await _servicio.ValidarAsync(sesion.Id));
        Assert.Equal(EstadoRenovacion.Invalida, (await _servicio.RenovarAsync(rotado.TokenRenovacion!)).Estado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.TokenRenovacionReutilizado && e.UsuarioId == _personal.Id);
    }

    [Fact]
    public async Task RF_IAM_006_un_token_desconocido_o_vacio_es_invalido()
    {
        Assert.Equal(EstadoRenovacion.Invalida, (await _servicio.RenovarAsync("desconocido")).Estado);
        Assert.Equal(EstadoRenovacion.Invalida, (await _servicio.RenovarAsync("")).Estado);
    }

    // ---- Inactividad (CA-IAM-003) ----

    [Fact]
    public async Task CA_IAM_003_el_personal_inactivo_15_minutos_pierde_la_sesion()
    {
        var (sesion, _) = await _servicio.IniciarAsync(_personal);
        _reloj.Avanzar(TimeSpan.FromMinutes(14));
        Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(sesion.Id));

        // el uso anterior cuenta como actividad, así que se vuelve a medir desde allí
        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Assert.Equal(EstadoSesion.Expirada, await _servicio.ValidarAsync(sesion.Id));
    }

    [Fact]
    public async Task CA_IAM_003_renovar_tras_15_minutos_de_inactividad_exige_autenticarse_de_nuevo()
    {
        var (_, token) = await _servicio.IniciarAsync(_personal);
        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        var r = await _servicio.RenovarAsync(token);
        Assert.Equal(EstadoRenovacion.SesionExpirada, r.Estado);
        Assert.Null(r.TokenRenovacion);
    }

    [Fact]
    public async Task RF_IAM_006_el_paciente_tiene_30_minutos_de_inactividad()
    {
        var (sesion, token) = await _servicio.IniciarAsync(_paciente);
        _reloj.Avanzar(TimeSpan.FromMinutes(29));
        Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(sesion.Id));
        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        Assert.Equal(EstadoRenovacion.SesionExpirada, (await _servicio.RenovarAsync(token)).Estado);
    }

    [Fact]
    public async Task RF_IAM_006_la_actividad_prolonga_la_sesion()
    {
        var (sesion, _) = await _servicio.IniciarAsync(_personal);
        for (var i = 0; i < 6; i++)
        {
            _reloj.Avanzar(TimeSpan.FromMinutes(10));
            Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(sesion.Id));
        }
    }

    [Fact]
    public async Task RF_IAM_006_la_actividad_se_guarda_como_maximo_una_vez_por_minuto()
    {
        var (sesion, _) = await _servicio.IniciarAsync(_personal);
        var antes = _repo.Guardados;

        _reloj.Avanzar(TimeSpan.FromSeconds(30));
        await _servicio.ValidarAsync(sesion.Id);
        Assert.Equal(antes, _repo.Guardados);

        _reloj.Avanzar(TimeSpan.FromSeconds(31));
        await _servicio.ValidarAsync(sesion.Id);
        Assert.Equal(antes + 1, _repo.Guardados);
    }

    [Fact]
    public async Task RF_IAM_006_la_sesion_tiene_una_duracion_maxima_aunque_haya_actividad()
    {
        var (sesion, _) = await _servicio.IniciarAsync(_personal);
        for (var i = 0; i < 71; i++) // actividad cada 10 min durante casi 12 h
        {
            _reloj.Avanzar(TimeSpan.FromMinutes(10));
            Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(sesion.Id));
        }
        _reloj.Avanzar(TimeSpan.FromMinutes(10)); // completa 12 h desde el inicio
        Assert.Equal(EstadoSesion.Expirada, await _servicio.ValidarAsync(sesion.Id));
    }

    // ---- Cierre ----

    [Fact]
    public async Task RF_IAM_006_cerrar_revoca_la_sesion_y_su_token_de_renovacion()
    {
        var (sesion, token) = await _servicio.IniciarAsync(_personal);
        await _servicio.CerrarAsync(sesion.Id);

        Assert.Equal(EstadoSesion.Revocada, await _servicio.ValidarAsync(sesion.Id));
        Assert.Equal(EstadoRenovacion.Invalida, (await _servicio.RenovarAsync(token)).Estado);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.CierreSesion);
    }

    [Fact]
    public async Task RF_IAM_006_cerrar_todas_revoca_cada_sesion_del_usuario_y_solo_las_suyas()
    {
        var (a, _) = await _servicio.IniciarAsync(_personal);
        var (b, _) = await _servicio.IniciarAsync(_personal);
        var (ajena, _) = await _servicio.IniciarAsync(_paciente);

        await _servicio.CerrarTodasAsync(_personal.Id);

        Assert.Equal(EstadoSesion.Revocada, await _servicio.ValidarAsync(a.Id));
        Assert.Equal(EstadoSesion.Revocada, await _servicio.ValidarAsync(b.Id));
        Assert.Equal(EstadoSesion.Activa, await _servicio.ValidarAsync(ajena.Id));
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.CierreSesionTodas && e.UsuarioId == _personal.Id);
    }

    [Fact]
    public async Task RF_IAM_006_una_sesion_inexistente_no_es_valida() =>
        Assert.Equal(EstadoSesion.Inexistente, await _servicio.ValidarAsync(Guid.NewGuid()));
}
