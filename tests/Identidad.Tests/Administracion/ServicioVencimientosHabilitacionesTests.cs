using Continuum.Identidad.Aplicacion.Administracion;
using Continuum.Identidad.Dominio;

namespace Continuum.Identidad.Tests.Administracion;

/// <summary>RF-ROL-004: aviso al administrador 30 días antes del vencimiento, sin información clínica (RN-016).</summary>
public class ServicioVencimientosHabilitacionesTests
{
    private static readonly Guid Org = EntornoAdministracion.Org, Cardiologia = EntornoAdministracion.Cardiologia;
    private readonly EntornoAdministracion _e = new();
    private DateOnly Hoy => _e.HoyEnSede;

    private async Task<(Guid ProfesionalId, Guid HabilitacionId)> HabilitarAsync(
        string correo, DateOnly vigenteHasta, Guid? especialidad = null)
    {
        var profesionalId = await _e.SembrarProfesionalAsync(correo);
        var hab = await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(
            _e.Actor, profesionalId, especialidad ?? Cardiologia, [], vigenteHasta);
        return (profesionalId, hab.Id!.Value);
    }

    private Task<int> CorrerAsync(Guid? organizacionId = null) =>
        _e.ServicioVencimientos.NotificarAsync(organizacionId ?? Org, ZonaHorariaFalsa.SantoDomingo);

    [Fact]
    public async Task RF_ROL_004_avisa_cuando_faltan_exactamente_30_dias()
    {
        var (profesionalId, habilitacionId) = await HabilitarAsync("a@clinica.test", Hoy.AddDays(30));

        var enviados = await CorrerAsync();

        Assert.Equal(1, enviados);
        var (org, avisos) = Assert.Single(_e.Avisos.Enviados);
        Assert.Equal(Org, org);
        var aviso = Assert.Single(avisos);
        Assert.Equal(new AvisoVencimiento(habilitacionId, profesionalId, Hoy.AddDays(30), 30), aviso);
    }

    [Fact]
    public async Task RF_ROL_004_no_avisa_antes_de_los_30_dias()
    {
        await HabilitarAsync("a@clinica.test", Hoy.AddDays(31));

        Assert.Equal(0, await CorrerAsync());
        Assert.Empty(_e.Avisos.Enviados);
    }

    [Fact]
    public async Task RF_ROL_004_avisa_el_mismo_dia_del_vencimiento_con_cero_dias_restantes()
    {
        await HabilitarAsync("a@clinica.test", Hoy);

        await CorrerAsync();

        Assert.Equal(0, Assert.Single(Assert.Single(_e.Avisos.Enviados).Avisos).DiasRestantes);
    }

    [Fact]
    public async Task RF_ROL_004_no_avisa_de_las_que_ya_vencieron()
    {
        var (_, habilitacionId) = await HabilitarAsync("a@clinica.test", Hoy);
        _e.Reloj.Avanzar(TimeSpan.FromDays(2));

        Assert.Equal(0, await CorrerAsync());
        Assert.NotNull(await _e.Habilitaciones.ObtenerPorIdAsync(habilitacionId));
    }

    [Fact]
    public async Task RF_ROL_004_avisa_una_sola_vez_por_fecha_de_vencimiento()
    {
        await HabilitarAsync("a@clinica.test", Hoy.AddDays(30));
        await CorrerAsync();

        _e.Reloj.Avanzar(TimeSpan.FromDays(1));
        Assert.Equal(0, await CorrerAsync());
        Assert.Single(_e.Avisos.Enviados);
    }

    [Fact]
    public async Task RF_ROL_004_si_se_cambia_la_fecha_de_vencimiento_vuelve_a_avisar()
    {
        var (_, habilitacionId) = await HabilitarAsync("a@clinica.test", Hoy.AddDays(30));
        await CorrerAsync();

        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, habilitacionId, [], Hoy.AddDays(20));

        Assert.Equal(1, await CorrerAsync());
        Assert.Equal(2, _e.Avisos.Enviados.Count);
    }

    [Fact]
    public async Task RF_ROL_004_actualizar_sin_cambiar_la_fecha_no_repite_el_aviso()
    {
        var (_, habilitacionId) = await HabilitarAsync("a@clinica.test", Hoy.AddDays(30));
        await CorrerAsync();

        await _e.ServicioHabilitaciones.ActualizarHabilitacionAsync(_e.Actor, habilitacionId, [EntornoAdministracion.Electrocardiograma], Hoy.AddDays(30));

        Assert.Equal(0, await CorrerAsync());
    }

    [Fact]
    public async Task RF_ROL_004_no_avisa_de_habilitaciones_revocadas()
    {
        var (_, habilitacionId) = await HabilitarAsync("a@clinica.test", Hoy.AddDays(5));
        await _e.ServicioHabilitaciones.RevocarHabilitacionAsync(_e.Actor, habilitacionId);

        Assert.Equal(0, await CorrerAsync());
    }

    [Fact]
    public async Task RN_015_solo_avisa_de_las_habilitaciones_de_la_organizacion_indicada()
    {
        await HabilitarAsync("a@clinica.test", Hoy.AddDays(5));
        var ajeno = _e.SembrarUsuario("ajeno@otra.test", EntornoAdministracion.OtraOrg);
        var ficha = Profesional.Crear(ajeno.Id, EntornoAdministracion.OtraOrg, "Luis", "Gómez", "Médico", "LIC-9", null);
        _e.Profesionales.Sembrar(ficha);
        _e.Habilitaciones.Sembrar(Habilitacion.Registrar(ficha.Id, EntornoAdministracion.OtraOrg, Cardiologia, [], Hoy.AddDays(5)));

        await CorrerAsync();

        var (org, avisos) = Assert.Single(_e.Avisos.Enviados);
        Assert.Equal(Org, org);
        Assert.Single(avisos);
    }

    [Fact]
    public async Task RF_ROL_004_agrupa_en_un_solo_envio_todas_las_que_estan_por_vencer()
    {
        await HabilitarAsync("a@clinica.test", Hoy.AddDays(3));
        await HabilitarAsync("b@clinica.test", Hoy.AddDays(25));

        Assert.Equal(2, await CorrerAsync());
        Assert.Equal(2, Assert.Single(_e.Avisos.Enviados).Avisos.Count);
    }

    [Fact]
    public async Task RF_ROL_004_la_fecha_de_hoy_se_toma_en_la_zona_horaria_indicada()
    {
        // 02:00 UTC del día siguiente: en Santo Domingo todavía es «ayer», así que faltan 30 días, no 29.
        _e.Reloj.Ahora = new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);
        var profesionalId = await _e.SembrarProfesionalAsync();
        await _e.ServicioHabilitaciones.RegistrarHabilitacionAsync(_e.Actor, profesionalId, Cardiologia, [], new DateOnly(2026, 11, 8));

        await CorrerAsync();

        Assert.Equal(30, Assert.Single(Assert.Single(_e.Avisos.Enviados).Avisos).DiasRestantes);
    }

    [Fact]
    public async Task RF_ROL_004_si_falla_el_envio_no_se_marca_como_avisada_y_se_reintenta()
    {
        await HabilitarAsync("a@clinica.test", Hoy.AddDays(10));
        _e.Avisos.Falla = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CorrerAsync());

        _e.Avisos.Falla = false;
        Assert.Equal(1, await CorrerAsync());
    }

    [Fact]
    public void RN_016_el_aviso_solo_contiene_identificadores_fechas_y_conteos()
    {
        var tipos = typeof(AvisoVencimiento).GetProperties().Select(p => p.PropertyType);

        Assert.All(tipos, t => Assert.Contains(t, new[] { typeof(Guid), typeof(DateOnly), typeof(int) }));
    }
}
