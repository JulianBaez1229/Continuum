using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

internal sealed class TokensAccionFalsos : ITokensAccion
{
    public List<TokenAccion> Todos { get; } = [];
    public Task AgregarAsync(TokenAccion t, CancellationToken ct = default) { Todos.Add(t); return Task.CompletedTask; }
    public Task<TokenAccion?> ObtenerPorHashAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(Todos.FirstOrDefault(t => t.HashToken == hash));
    public Task<IReadOnlyList<TokenAccion>> ObtenerPendientesDeUsuarioAsync(Guid usuarioId, PropositoToken proposito, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TokenAccion>>(Todos.Where(t => t.UsuarioId == usuarioId && t.Proposito == proposito && t.UsadoEn is null).ToList());
    public Task GuardarAsync(IEnumerable<TokenAccion> tokens, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class MensajeriaFalsa : IMensajeriaIdentidad
{
    public List<(Guid UsuarioId, string Token, DateTimeOffset ExpiraEn)> Recuperaciones { get; } = [];
    public List<Guid> CambiosDeContrasena { get; } = [];
    public List<(Guid PacienteId, string Canal, string Token, DateTimeOffset ExpiraEn)> Invitaciones { get; } = [];
    public List<(Guid UsuarioId, string Codigo, DateTimeOffset ExpiraEn)> Codigos { get; } = [];

    public Task EnviarEnlaceRecuperacionAsync(Usuario usuario, string token, DateTimeOffset expiraEn, CancellationToken ct = default)
    {
        Recuperaciones.Add((usuario.Id, token, expiraEn));
        return Task.CompletedTask;
    }
    public Task NotificarCambioContrasenaAsync(Usuario usuario, CancellationToken ct = default)
    {
        CambiosDeContrasena.Add(usuario.Id);
        return Task.CompletedTask;
    }
    public Task EnviarInvitacionPacienteAsync(Guid pacienteId, CanalInvitacion canal, string destino, string token, DateTimeOffset expiraEn, CancellationToken ct = default)
    {
        Invitaciones.Add((pacienteId, $"{canal}:{destino}", token, expiraEn));
        return Task.CompletedTask;
    }
    public Task EnviarCodigoMfaAsync(Usuario usuario, string codigo, DateTimeOffset expiraEn, CancellationToken ct = default)
    {
        Codigos.Add((usuario.Id, codigo, expiraEn));
        return Task.CompletedTask;
    }
}

public class RecuperacionContrasenaTests
{
    private const string Correo = "rec@clinica.test";
    private const string ClaveVieja = "la clave de siempre 1";
    private const string ClaveNueva = "una clave nueva y larga";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _usuarios = new();
    private readonly TokensAccionFalsos _tokens = new();
    private readonly SesionesFalsas _sesiones = new();
    private readonly MensajeriaFalsa _mensajeria = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly Usuario _usuario;
    private readonly ServicioSesiones _servicioSesiones;
    private readonly ServicioRecuperacionContrasena _servicio;

    public RecuperacionContrasenaTests()
    {
        _usuario = Usuario.Crear(Guid.NewGuid(), Correo, _hasher.Hashear(ClaveVieja));
        _usuarios.Agregar(_usuario);
        _servicioSesiones = new ServicioSesiones(_sesiones, new PoliticaSesionFalsa(), _reloj, _auditoria);
        _servicio = new ServicioRecuperacionContrasena(
            _usuarios, _tokens, _servicioSesiones, new PoliticaContrasena(new FiltradasFalsas("passwordpassword")),
            _hasher, _reloj, _auditoria, _mensajeria);
    }

    private async Task<string> SolicitarAsync()
    {
        await _servicio.SolicitarAsync(Correo);
        return _mensajeria.Recuperaciones.Last().Token;
    }

    // ---- Solicitud ----

    [Fact]
    public async Task RF_IAM_005_un_usuario_existente_recibe_un_enlace_que_caduca_en_30_minutos()
    {
        await _servicio.SolicitarAsync(Correo);

        var envio = Assert.Single(_mensajeria.Recuperaciones);
        Assert.Equal(_usuario.Id, envio.UsuarioId);
        Assert.Equal(_reloj.Ahora.AddMinutes(30), envio.ExpiraEn);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.RecuperacionContrasenaSolicitada && e.UsuarioId == _usuario.Id);
    }

    [Fact]
    public async Task RF_IAM_005_un_correo_inexistente_no_envia_nada_ni_revela_que_no_existe()
    {
        await _servicio.SolicitarAsync("nadie@clinica.test"); // no lanza excepción
        Assert.Empty(_mensajeria.Recuperaciones);
    }

    [Fact]
    public async Task RF_IAM_005_un_usuario_inactivo_no_recibe_enlace()
    {
        _usuario.Desactivar();
        await _servicio.SolicitarAsync(Correo);
        Assert.Empty(_mensajeria.Recuperaciones);
    }

    [Fact]
    public async Task RF_IAM_005_el_token_se_guarda_solo_como_hash()
    {
        var token = await SolicitarAsync();
        var guardado = Assert.Single(_tokens.Todos);
        Assert.NotEqual(token, guardado.HashToken);
        Assert.Equal(PropositoToken.RecuperacionContrasena, guardado.Proposito);
    }

    [Fact]
    public async Task RF_IAM_005_solicitudes_seguidas_no_reenvian_correo_cada_vez()
    {
        await _servicio.SolicitarAsync(Correo);
        _reloj.Avanzar(TimeSpan.FromSeconds(30));
        await _servicio.SolicitarAsync(Correo);
        Assert.Single(_mensajeria.Recuperaciones);
    }

    [Fact]
    public async Task RF_IAM_005_una_solicitud_nueva_invalida_el_enlace_anterior()
    {
        var primero = await SolicitarAsync();
        _reloj.Avanzar(TimeSpan.FromMinutes(2));
        var segundo = await SolicitarAsync();

        Assert.Equal(ResultadoRestablecimiento.TokenInvalido, (await _servicio.RestablecerAsync(primero, ClaveNueva)).Resultado);
        Assert.Equal(ResultadoRestablecimiento.Exito, (await _servicio.RestablecerAsync(segundo, ClaveNueva)).Resultado);
    }

    // ---- Restablecimiento ----

    [Fact]
    public async Task RF_IAM_005_un_token_valido_cambia_la_contrasena()
    {
        var token = await SolicitarAsync();
        var r = await _servicio.RestablecerAsync(token, ClaveNueva);

        Assert.Equal(ResultadoRestablecimiento.Exito, r.Resultado);
        Assert.True(_hasher.Verificar(_usuario.HashContrasena, ClaveNueva));
        Assert.False(_hasher.Verificar(_usuario.HashContrasena, ClaveVieja));
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.ContrasenaCambiada && e.UsuarioId == _usuario.Id);
        Assert.Equal([_usuario.Id], _mensajeria.CambiosDeContrasena);
    }

    [Fact]
    public async Task RF_IAM_005_el_enlace_es_de_un_solo_uso()
    {
        var token = await SolicitarAsync();
        await _servicio.RestablecerAsync(token, ClaveNueva);
        var otraVez = await _servicio.RestablecerAsync(token, "otra clave todavia mas larga");

        Assert.Equal(ResultadoRestablecimiento.TokenInvalido, otraVez.Resultado);
        Assert.True(_hasher.Verificar(_usuario.HashContrasena, ClaveNueva));
    }

    [Fact]
    public async Task RF_IAM_005_el_enlace_caduca_a_los_30_minutos()
    {
        var token = await SolicitarAsync();
        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        Assert.Equal(ResultadoRestablecimiento.TokenInvalido, (await _servicio.RestablecerAsync(token, ClaveNueva)).Resultado);
        Assert.True(_hasher.Verificar(_usuario.HashContrasena, ClaveVieja));
    }

    [Fact]
    public async Task RF_IAM_005_el_enlace_sirve_justo_antes_de_caducar()
    {
        var token = await SolicitarAsync();
        _reloj.Avanzar(TimeSpan.FromMinutes(29).Add(TimeSpan.FromSeconds(59)));
        Assert.Equal(ResultadoRestablecimiento.Exito, (await _servicio.RestablecerAsync(token, ClaveNueva)).Resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("inventado")]
    public async Task RF_IAM_005_un_token_desconocido_es_invalido(string token) =>
        Assert.Equal(ResultadoRestablecimiento.TokenInvalido, (await _servicio.RestablecerAsync(token, ClaveNueva)).Resultado);

    [Fact]
    public async Task RF_IAM_005_un_token_de_otro_proposito_no_sirve_para_restablecer()
    {
        var plano = "token-de-activacion";
        _tokens.Todos.Add(TokenAccion.Crear(PropositoToken.ActivacionCuenta, _usuario.Id, null, _usuario.OrganizacionId,
            ServicioSesiones.HashDe(plano), _reloj.Ahora, TimeSpan.FromHours(72)));
        Assert.Equal(ResultadoRestablecimiento.TokenInvalido, (await _servicio.RestablecerAsync(plano, ClaveNueva)).Resultado);
    }

    [Fact]
    public async Task RF_IAM_001_una_contrasena_debil_se_rechaza_sin_consumir_el_enlace()
    {
        var token = await SolicitarAsync();

        var corta = await _servicio.RestablecerAsync(token, "corta");
        Assert.Equal(ResultadoRestablecimiento.ContrasenaInvalida, corta.Resultado);
        Assert.Equal(MotivoContrasenaInvalida.Corta, corta.Motivo);

        var filtrada = await _servicio.RestablecerAsync(token, "passwordpassword");
        Assert.Equal(MotivoContrasenaInvalida.Filtrada, filtrada.Motivo);

        Assert.Equal(ResultadoRestablecimiento.Exito, (await _servicio.RestablecerAsync(token, ClaveNueva)).Resultado);
    }

    [Fact]
    public async Task RF_IAM_005_restablecer_cierra_todas_las_sesiones_abiertas()
    {
        var (sesion, _) = await _servicioSesiones.IniciarAsync(_usuario);
        var token = await SolicitarAsync();

        await _servicio.RestablecerAsync(token, ClaveNueva);

        Assert.Equal(EstadoSesion.Revocada, await _servicioSesiones.ValidarAsync(sesion.Id));
    }

    [Fact]
    public async Task RF_IAM_005_restablecer_levanta_el_bloqueo_por_intentos_fallidos()
    {
        for (var i = 0; i < 5; i++) _usuario.RegistrarFallo(_reloj.Ahora);
        Assert.True(_usuario.EstaBloqueado(_reloj.Ahora));

        await _servicio.RestablecerAsync(await SolicitarAsync(), ClaveNueva);

        Assert.False(_usuario.EstaBloqueado(_reloj.Ahora));
        Assert.Equal(0, _usuario.BloqueosConsecutivos);
    }

    [Fact]
    public async Task RF_IAM_002_restablecer_la_contrasena_no_desactiva_el_mfa()
    {
        _usuario.PrepararMfa("secreto");
        _usuario.ActivarMfa(1, ["h"]);

        await _servicio.RestablecerAsync(await SolicitarAsync(), ClaveNueva);

        Assert.True(_usuario.MfaHabilitado);
        Assert.Equal("secreto", _usuario.SecretoTotpProtegido);
    }

    [Fact]
    public async Task RF_IAM_009_la_bitacora_no_contiene_el_token_ni_la_contrasena()
    {
        var token = await SolicitarAsync();
        await _servicio.RestablecerAsync(token, ClaveNueva);
        Assert.DoesNotContain(_auditoria.Eventos, e => e.ToString()!.Contains(token) || e.ToString()!.Contains(ClaveNueva));
    }
}
