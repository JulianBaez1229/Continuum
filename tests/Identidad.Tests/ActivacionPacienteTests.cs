using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

internal sealed class DirectorioPacientesFalso : IDirectorioPacientes
{
    public record Ficha(Guid OrganizacionId, string? Correo, string? Telefono, DateOnly Nacimiento, string Ultimos4)
    {
        public bool TieneCuenta { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public Dictionary<Guid, Ficha> Pacientes { get; } = [];

    public Task<ContactoPaciente?> ObtenerContactoAsync(Guid pacienteId, CancellationToken ct = default) =>
        Task.FromResult(Pacientes.TryGetValue(pacienteId, out var f)
            ? new ContactoPaciente(f.OrganizacionId, f.Correo, f.Telefono, f.TieneCuenta)
            : null);

    public Task<bool> VerificarIdentidadAsync(Guid pacienteId, DateOnly fechaNacimiento, string ultimos4Documento, CancellationToken ct = default) =>
        Task.FromResult(Pacientes.TryGetValue(pacienteId, out var f) && f.Nacimiento == fechaNacimiento && f.Ultimos4 == ultimos4Documento);

    public Task VincularCuentaAsync(Guid pacienteId, Guid usuarioId, CancellationToken ct = default)
    {
        Pacientes[pacienteId].TieneCuenta = true;
        Pacientes[pacienteId].UsuarioId = usuarioId;
        return Task.CompletedTask;
    }
}

internal sealed class ConsentimientosFalsos : IConsentimientos
{
    public const string Terminos = "terminos-2026-10";
    public const string Aviso = "aviso-2026-10";

    public VersionesLegales Vigentes(Guid organizacionId) => new(Terminos, Aviso);
    public List<(Guid PacienteId, string Tipo, string Version, DateTimeOffset En)> Registrados { get; } = [];

    public Task RegistrarAceptacionAsync(Guid pacienteId, string tipo, string versionTexto, Guid firmadoPor, DateTimeOffset en, CancellationToken ct = default)
    {
        Registrados.Add((pacienteId, tipo, versionTexto, en));
        return Task.CompletedTask;
    }
}

public class ActivacionPacienteTests
{
    private static readonly DateOnly Nacimiento = new(1990, 5, 17);
    private const string Ultimos4 = "4321";
    private const string Clave = "mi clave de paciente larga";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _usuarios = new();
    private readonly TokensAccionFalsos _tokens = new();
    private readonly MensajeriaFalsa _mensajeria = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly DirectorioPacientesFalso _directorio = new();
    private readonly ConsentimientosFalsos _consentimientos = new();
    private readonly HasheadorFalso _hasher = new();
    private readonly ServicioActivacionPaciente _servicio;
    private readonly Guid _organizacionId = Guid.NewGuid();
    private readonly Guid _recepcionId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();

    public ActivacionPacienteTests()
    {
        _directorio.Pacientes[_pacienteId] = new(_organizacionId, "paciente@correo.test", "8095550100", Nacimiento, Ultimos4);
        _servicio = new ServicioActivacionPaciente(
            _usuarios, _tokens, _directorio, _consentimientos,
            new PoliticaContrasena(new FiltradasFalsas("passwordpassword")), _hasher, _reloj, _auditoria, _mensajeria);
    }

    private SolicitudActivacion Solicitud(string token, DateOnly? nacimiento = null, string ultimos4 = Ultimos4,
        string? correo = null, string clave = Clave, string terminos = ConsentimientosFalsos.Terminos, string aviso = ConsentimientosFalsos.Aviso) =>
        new(token, nacimiento ?? Nacimiento, ultimos4, correo, clave, terminos, aviso);

    private async Task<string> InvitarAsync()
    {
        var r = await _servicio.InvitarAsync(_pacienteId, _organizacionId, _recepcionId);
        Assert.Equal(ResultadoInvitacion.Enviada, r);
        return _mensajeria.Invitaciones.Last().Token;
    }

    // ---- Invitación ----

    [Fact]
    public async Task RF_IAM_007_recepcion_genera_una_invitacion_por_correo_valida_72_horas()
    {
        await InvitarAsync();

        var envio = Assert.Single(_mensajeria.Invitaciones);
        Assert.Equal(_pacienteId, envio.PacienteId);
        Assert.Equal("Correo:paciente@correo.test", envio.Canal);
        Assert.Equal(_reloj.Ahora.AddHours(72), envio.ExpiraEn);
        Assert.Contains(_auditoria.Eventos, e =>
            e.Tipo == TipoEventoIdentidad.InvitacionPacienteEnviada && e.UsuarioId == _recepcionId && e.OrganizacionId == _organizacionId);
    }

    [Fact]
    public async Task RF_IAM_007_sin_correo_la_invitacion_va_por_sms()
    {
        _directorio.Pacientes[_pacienteId] = _directorio.Pacientes[_pacienteId] with { Correo = null };
        await InvitarAsync();
        Assert.Equal("Sms:8095550100", Assert.Single(_mensajeria.Invitaciones).Canal);
    }

    [Fact]
    public async Task RF_IAM_007_se_respeta_el_canal_pedido_si_hay_dato_de_contacto()
    {
        await _servicio.InvitarAsync(_pacienteId, _organizacionId, _recepcionId, CanalInvitacion.Sms);
        Assert.Equal("Sms:8095550100", Assert.Single(_mensajeria.Invitaciones).Canal);
    }

    [Fact]
    public async Task RF_IAM_007_el_token_se_guarda_solo_como_hash()
    {
        var token = await InvitarAsync();
        var guardado = Assert.Single(_tokens.Todos);
        Assert.NotEqual(token, guardado.HashToken);
        Assert.Equal(PropositoToken.ActivacionCuenta, guardado.Proposito);
        Assert.Equal(_pacienteId, guardado.PacienteId);
        Assert.Null(guardado.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_007_sin_medio_de_contacto_no_se_puede_invitar()
    {
        _directorio.Pacientes[_pacienteId] = _directorio.Pacientes[_pacienteId] with { Correo = null, Telefono = null };
        var r = await _servicio.InvitarAsync(_pacienteId, _organizacionId, _recepcionId);
        Assert.Equal(ResultadoInvitacion.SinContacto, r);
        Assert.Empty(_mensajeria.Invitaciones);
    }

    [Fact]
    public async Task RF_IAM_007_un_paciente_que_ya_tiene_cuenta_no_recibe_invitacion()
    {
        _directorio.Pacientes[_pacienteId].TieneCuenta = true;
        Assert.Equal(ResultadoInvitacion.YaTieneCuenta, await _servicio.InvitarAsync(_pacienteId, _organizacionId, _recepcionId));
        Assert.Empty(_mensajeria.Invitaciones);
    }

    [Fact]
    public async Task RN_015_un_paciente_de_otra_organizacion_se_trata_como_inexistente()
    {
        var r = await _servicio.InvitarAsync(_pacienteId, Guid.NewGuid(), _recepcionId);
        Assert.Equal(ResultadoInvitacion.PacienteNoEncontrado, r);
        Assert.Empty(_mensajeria.Invitaciones);
    }

    [Fact]
    public async Task RF_IAM_007_un_paciente_inexistente_no_se_puede_invitar() =>
        Assert.Equal(ResultadoInvitacion.PacienteNoEncontrado, await _servicio.InvitarAsync(Guid.NewGuid(), _organizacionId, _recepcionId));

    [Fact]
    public async Task RF_IAM_007_una_invitacion_nueva_invalida_la_anterior()
    {
        var primera = await InvitarAsync();
        var segunda = await InvitarAsync();

        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(primera))).Resultado);
        Assert.Equal(ResultadoActivacion.Exito, (await _servicio.ActivarAsync(Solicitud(segunda))).Resultado);
    }

    // ---- Activación ----

    [Fact]
    public async Task RF_IAM_007_activar_crea_la_cuenta_del_paciente_sin_mfa_obligatorio()
    {
        var token = await InvitarAsync();
        var r = await _servicio.ActivarAsync(Solicitud(token));

        Assert.Equal(ResultadoActivacion.Exito, r.Resultado);
        var usuario = (await _usuarios.ObtenerPorCorreoAsync("paciente@correo.test"))!;
        Assert.Equal(r.UsuarioId, usuario.Id);
        Assert.Equal(_organizacionId, usuario.OrganizacionId);
        Assert.False(usuario.RequiereMfa);
        Assert.True(_hasher.Verificar(usuario.HashContrasena, Clave));
        Assert.Equal(usuario.Id, _directorio.Pacientes[_pacienteId].UsuarioId);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.CuentaPacienteActivada && e.UsuarioId == usuario.Id);
    }

    [Fact]
    public async Task RF_IAM_007_el_paciente_puede_iniciar_sesion_con_la_cuenta_recien_creada()
    {
        await _servicio.ActivarAsync(Solicitud(await InvitarAsync()));

        var login = new ServicioInicioSesion(_usuarios, _hasher, _reloj, _auditoria, new AvisosFalsos());
        Assert.Equal(EstadoInicioSesion.Exitoso, (await login.IniciarSesionAsync("paciente@correo.test", Clave)).Estado);
    }

    [Fact]
    public async Task RF_IAM_007_se_registran_los_consentimientos_con_la_version_aceptada()
    {
        await _servicio.ActivarAsync(Solicitud(await InvitarAsync()));

        Assert.Equal(
            [("terminos", ConsentimientosFalsos.Terminos), ("aviso_privacidad", ConsentimientosFalsos.Aviso)],
            _consentimientos.Registrados.Select(c => (c.Tipo, c.Version)));
        Assert.All(_consentimientos.Registrados, c => Assert.Equal(_pacienteId, c.PacienteId));
    }

    [Fact]
    public async Task RF_IAM_007_la_invitacion_es_de_un_solo_uso()
    {
        var token = await InvitarAsync();
        await _servicio.ActivarAsync(Solicitud(token));
        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_007_la_invitacion_caduca_a_las_72_horas()
    {
        var token = await InvitarAsync();
        _reloj.Avanzar(TimeSpan.FromHours(72));
        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("inventado")]
    public async Task RF_IAM_007_un_token_desconocido_es_invalido(string token) =>
        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);

    [Fact]
    public async Task RF_IAM_007_un_token_de_recuperacion_no_sirve_para_activar()
    {
        var plano = "token-de-recuperacion";
        _tokens.Todos.Add(TokenAccion.Crear(PropositoToken.RecuperacionContrasena, Guid.NewGuid(), null, _organizacionId,
            GeneradorTokens.HashDe(plano), _reloj.Ahora, TimeSpan.FromMinutes(30)));
        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(plano))).Resultado);
    }

    [Theory]
    [InlineData(1990, 5, 18, "4321")]   // fecha distinta
    [InlineData(1990, 5, 17, "9999")]   // documento distinto
    public async Task RF_IAM_007_si_la_identidad_no_coincide_no_se_crea_la_cuenta(int a, int m, int d, string ultimos4)
    {
        var token = await InvitarAsync();
        var r = await _servicio.ActivarAsync(Solicitud(token, new DateOnly(a, m, d), ultimos4));

        Assert.Equal(ResultadoActivacion.IdentidadNoCoincide, r.Resultado);
        Assert.Null(await _usuarios.ObtenerPorCorreoAsync("paciente@correo.test"));
    }

    [Fact]
    public async Task RF_IAM_007_cinco_intentos_fallidos_invalidan_la_invitacion_aunque_luego_acierten()
    {
        var token = await InvitarAsync();
        for (var i = 0; i < 5; i++)
            await _servicio.ActivarAsync(Solicitud(token, ultimos4: "0000"));

        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_007_unos_pocos_errores_no_impiden_activar_con_los_datos_correctos()
    {
        var token = await InvitarAsync();
        for (var i = 0; i < 4; i++)
            await _servicio.ActivarAsync(Solicitud(token, ultimos4: "0000"));

        Assert.Equal(ResultadoActivacion.Exito, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_007_sin_aceptar_la_version_vigente_de_los_textos_no_se_activa_ni_se_gasta_la_invitacion()
    {
        var token = await InvitarAsync();

        Assert.Equal(ResultadoActivacion.ConsentimientoRequerido,
            (await _servicio.ActivarAsync(Solicitud(token, terminos: "version-vieja"))).Resultado);
        Assert.Equal(ResultadoActivacion.ConsentimientoRequerido,
            (await _servicio.ActivarAsync(Solicitud(token, aviso: ""))).Resultado);
        Assert.Empty(_consentimientos.Registrados);
        Assert.Equal(ResultadoActivacion.Exito, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_001_una_contrasena_debil_se_rechaza_sin_gastar_la_invitacion()
    {
        var token = await InvitarAsync();

        var corta = await _servicio.ActivarAsync(Solicitud(token, clave: "corta"));
        Assert.Equal(ResultadoActivacion.ContrasenaInvalida, corta.Resultado);
        Assert.Equal(MotivoContrasenaInvalida.Corta, corta.Motivo);
        Assert.Equal(MotivoContrasenaInvalida.Filtrada,
            (await _servicio.ActivarAsync(Solicitud(token, clave: "passwordpassword"))).Motivo);

        Assert.Equal(ResultadoActivacion.Exito, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_007_si_el_paciente_no_tiene_correo_lo_aporta_al_activar()
    {
        _directorio.Pacientes[_pacienteId] = _directorio.Pacientes[_pacienteId] with { Correo = null };
        var token = await InvitarAsync();

        Assert.Equal(ResultadoActivacion.CorreoRequerido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
        var r = await _servicio.ActivarAsync(Solicitud(token, correo: " Nuevo@Correo.test "));

        Assert.Equal(ResultadoActivacion.Exito, r.Resultado);
        Assert.NotNull(await _usuarios.ObtenerPorCorreoAsync("nuevo@correo.test"));
    }

    [Fact]
    public async Task RF_IAM_007_si_el_correo_ya_pertenece_a_otra_cuenta_no_se_activa_ni_se_gasta_la_invitacion()
    {
        _usuarios.Agregar(Usuario.Crear(Guid.NewGuid(), "paciente@correo.test", "h"));
        var token = await InvitarAsync();

        Assert.Equal(ResultadoActivacion.CorreoNoDisponible, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
        Assert.Empty(_consentimientos.Registrados);
    }

    [Fact]
    public async Task RF_IAM_007_si_el_paciente_ya_activo_su_cuenta_la_invitacion_pendiente_no_sirve()
    {
        var token = await InvitarAsync();
        _directorio.Pacientes[_pacienteId].TieneCuenta = true;
        Assert.Equal(ResultadoActivacion.TokenInvalido, (await _servicio.ActivarAsync(Solicitud(token))).Resultado);
    }

    [Fact]
    public async Task RF_IAM_009_la_bitacora_no_contiene_contrasena_documento_ni_token()
    {
        var token = await InvitarAsync();
        await _servicio.ActivarAsync(Solicitud(token));

        Assert.DoesNotContain(_auditoria.Eventos, e =>
            e.ToString()!.Contains(Clave) || e.ToString()!.Contains(Ultimos4) || e.ToString()!.Contains(token));
    }
}
