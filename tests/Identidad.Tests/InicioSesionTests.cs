using Continuum.Identidad.Aplicacion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests;

public class InicioSesionTests
{
    private const string Correo = "psicologa@clinica.test";
    private const string Clave = "una clave larga y segura";
    private const string ClaveSecreta = "secreto-que-no-debe-aparecer";

    private readonly RelojFalso _reloj = new();
    private readonly RepositorioFalso _repo = new();
    private readonly AuditoriaFalsa _auditoria = new();
    private readonly AvisosFalsos _avisos = new();
    private readonly Usuario _usuario;
    private readonly ServicioInicioSesion _servicio;

    public InicioSesionTests()
    {
        var hasher = new HasheadorFalso();
        _usuario = Usuario.Crear(Guid.NewGuid(), Correo, hasher.Hashear(Clave));
        _repo.Agregar(_usuario);
        _servicio = new ServicioInicioSesion(_repo, hasher, _reloj, _auditoria, _avisos);
    }

    private async Task FallarAsync(int veces)
    {
        for (var i = 0; i < veces; i++) await _servicio.IniciarSesionAsync(Correo, "incorrecta");
    }

    [Fact]
    public async Task RF_IAM_001_credenciales_correctas_inician_sesion()
    {
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
        Assert.Equal(_usuario.Id, r.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_001_el_correo_no_distingue_mayusculas_ni_espacios()
    {
        var r = await _servicio.IniciarSesionAsync("  PSICOLOGA@Clinica.test ", Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_001_usuario_inexistente_y_clave_incorrecta_dan_la_misma_respuesta()
    {
        var inexistente = await _servicio.IniciarSesionAsync("nadie@clinica.test", Clave);
        var incorrecta = await _servicio.IniciarSesionAsync(Correo, "incorrecta");
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, inexistente.Estado);
        Assert.Equal(incorrecta, inexistente);
        Assert.Null(incorrecta.UsuarioId);
    }

    [Fact]
    public async Task RF_IAM_001_usuario_inactivo_no_inicia_sesion()
    {
        _usuario.Desactivar();
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.CredencialesInvalidas, r.Estado);
    }

    [Fact]
    public async Task CA_IAM_002_cinco_fallos_bloquean_15_minutos_y_el_sexto_intento_es_rechazado()
    {
        await FallarAsync(5);
        var sexto = await _servicio.IniciarSesionAsync(Correo, Clave); // aun con clave correcta

        Assert.Equal(EstadoInicioSesion.CuentaBloqueada, sexto.Estado);
        Assert.Equal(_reloj.Ahora.AddMinutes(15), sexto.BloqueadoHasta);
    }

    [Fact]
    public async Task CA_IAM_002_el_usuario_recibe_un_aviso_una_sola_vez_por_bloqueo()
    {
        await FallarAsync(5);
        await _servicio.IniciarSesionAsync(Correo, Clave);

        var aviso = Assert.Single(_avisos.Bloqueos);
        Assert.Equal(_usuario.Id, aviso.UsuarioId);
        Assert.Equal(_reloj.Ahora.AddMinutes(15), aviso.Hasta);
    }

    [Fact]
    public async Task RF_IAM_004_cuatro_fallos_no_bloquean()
    {
        await FallarAsync(4);
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_004_los_fallos_fuera_de_la_ventana_de_15_minutos_no_cuentan()
    {
        await FallarAsync(4);
        _reloj.Avanzar(TimeSpan.FromMinutes(16));
        await FallarAsync(1);
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_004_la_cuenta_se_desbloquea_al_vencer_el_bloqueo()
    {
        await FallarAsync(5);
        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_004_el_desbloqueo_es_progresivo_y_cada_bloqueo_duplica_la_duracion()
    {
        await FallarAsync(5);
        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        await FallarAsync(5);

        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(_reloj.Ahora.AddMinutes(30), r.BloqueadoHasta);
    }

    [Fact]
    public async Task RF_IAM_004_el_inicio_exitoso_reinicia_el_contador()
    {
        await FallarAsync(4);
        await _servicio.IniciarSesionAsync(Correo, Clave);
        await FallarAsync(4);
        var r = await _servicio.IniciarSesionAsync(Correo, Clave);
        Assert.Equal(EstadoInicioSesion.Exitoso, r.Estado);
    }

    [Fact]
    public async Task RF_IAM_004_intentos_durante_el_bloqueo_no_lo_extienden()
    {
        await FallarAsync(5);
        var hasta = _reloj.Ahora.AddMinutes(15);
        _reloj.Avanzar(TimeSpan.FromMinutes(5));
        var r = await _servicio.IniciarSesionAsync(Correo, "incorrecta");
        Assert.Equal(hasta, r.BloqueadoHasta);
    }

    [Fact]
    public async Task RF_IAM_009_se_registra_cada_inicio_de_sesion_exitoso_y_fallido()
    {
        await _servicio.IniciarSesionAsync(Correo, "incorrecta");
        await _servicio.IniciarSesionAsync(Correo, Clave);
        await _servicio.IniciarSesionAsync("nadie@clinica.test", Clave);

        Assert.Equal(
            [TipoEventoIdentidad.InicioSesionFallido, TipoEventoIdentidad.InicioSesionExitoso, TipoEventoIdentidad.InicioSesionFallido],
            _auditoria.Eventos.Select(e => e.Tipo));
    }

    [Fact]
    public async Task RF_IAM_009_se_registra_el_bloqueo_de_la_cuenta()
    {
        await FallarAsync(5);
        Assert.Contains(_auditoria.Eventos, e => e.Tipo == TipoEventoIdentidad.CuentaBloqueada && e.UsuarioId == _usuario.Id);
    }

    [Fact]
    public async Task RF_IAM_009_la_bitacora_no_contiene_la_contrasena()
    {
        await _servicio.IniciarSesionAsync(Correo, ClaveSecreta);
        Assert.DoesNotContain(_auditoria.Eventos, e => e.ToString()!.Contains(ClaveSecreta));
    }
}
