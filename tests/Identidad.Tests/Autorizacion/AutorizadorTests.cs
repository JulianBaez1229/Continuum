using System.Reflection;
using Continuum.Identidad.Aplicacion.Autorizacion;
using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Autorizacion;

/// <summary>
/// El <see cref="Autorizador"/> solo carga los hechos que la política declara necesarios (spec §6).
/// Los contadores de llamadas de los dobles son la prueba de que la carga es perezosa. También verifica que cada
/// denegación se audita (spec §9) y que ante cualquier fallo de un puerto el acceso se cierra (spec §10).
/// </summary>
public class AutorizadorTests
{
    // Identificadores inventados (datos ficticios).
    private static readonly Guid Org = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid OtraOrg = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Sede = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid OtraSede = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid Usuario = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid ProfesionalActor = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid PacienteActor = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid OtroPaciente = Guid.Parse("00000000-0000-0000-0000-0000000000e2");
    private static readonly Guid Episodio = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
    private static readonly Guid Especialidad = Guid.Parse("00000000-0000-0000-0000-000000000051");
    private static readonly Guid RecursoNota = Guid.Parse("00000000-0000-0000-0000-0000000000a9");

    private static readonly HabilitacionProfesional HabilitacionVigente = new(Especialidad, new HashSet<Guid>(), new DateOnly(2027, 1, 1));
    private static readonly RelacionClinica Tratante = new(TipoRelacion.Tratante, PermiteSensible: false, EpisodioSensible: false, Especialidad);

    /// <summary>El autorizador con sus seis puertos sustituidos por dobles. Por defecto el actor tiene el perfil de la organización <see cref="Org"/>.</summary>
    private sealed class Escenario
    {
        public RolesPorSedeFalso Roles { get; } = new();
        public VinculoPacienteFalso Vinculo { get; } = new();
        public RelacionClinicaFalsa Relacion { get; } = new();
        public HabilitacionesFalsas Habilitaciones { get; } = new();
        public ZonaHorariaSedeFalsa Zonas { get; } = new();
        public RelojFalso Reloj { get; } = new();
        public AuditoriaAutorizacionFalsa Auditoria { get; } = new();
        public Autorizador Autorizador { get; }

        public Escenario(Rol[] roles, Guid? profesionalId = null)
        {
            Roles.Perfil = new PerfilActor(Usuario, Org, new HashSet<Rol>(roles), profesionalId);
            Autorizador = new Autorizador(Roles, Vinculo, Relacion, Habilitaciones, Zonas, Reloj, Auditoria);
        }

        public Task<Decision> Autorizar(Accion accion, TipoRecurso recurso, ContextoRecurso contexto, CancellationToken ct = default) =>
            Autorizador.AutorizarAsync(new SolicitudAcceso(Usuario, Sede, accion, recurso, contexto), ct);
    }

    /// <summary>Contexto del recurso. La organización por defecto es <see cref="Org"/>.</summary>
    private static ContextoRecurso Ctx(
        Guid? org = null, Guid? paciente = null, Guid? episodio = null, Guid? sede = null, Guid? profesional = null, Guid? recurso = null) =>
        new(org ?? Org, RecursoId: recurso, PacienteId: paciente, EpisodioId: episodio, SedeId: sede, ProfesionalId: profesional);

    private static Decision Prohibido(MotivoDenegacion motivo) => Decision.Denegar(TipoDenegacion.Prohibido, motivo);

    /// <summary>La solicitud se resolvió solo con el perfil: ningún otro puerto fue consultado.</summary>
    private static void AssertSinCargarHechos(Escenario e)
    {
        Assert.Equal(0, e.Vinculo.Llamadas);
        Assert.Equal(0, e.Relacion.Llamadas);
        Assert.Equal(0, e.Habilitaciones.Llamadas);
        Assert.Equal(0, e.Zonas.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_leer_datos_demograficos_no_consulta_vinculo_relacion_ni_habilitacion()
    {
        var recepcion = new Escenario([Rol.Recepcion]);
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            await recepcion.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        Assert.Equal(1, recepcion.Roles.Llamadas);
        AssertSinCargarHechos(recepcion);

        // Un profesional con ficha y un paciente en el contexto tampoco dispara la carga: la celda no lo exige.
        var profesional = new Escenario([Rol.Profesional], ProfesionalActor);
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            await profesional.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        AssertSinCargarHechos(profesional);
    }

    [Fact]
    public async Task RF_ROL_005_recurso_de_otra_organizacion_no_carga_mas_hechos_que_el_perfil()
    {
        var noEncontrado = Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion);

        // Con estos roles, cada acción pediría vínculo, relación y/o habilitación si el recurso fuera de la organización.
        foreach (var accion in new[] { Accion.Leer, Accion.Crear })
        {
            var e = new Escenario([Rol.Paciente, Rol.Profesional], ProfesionalActor);

            var decision = await e.Autorizar(accion, TipoRecurso.Orden, Ctx(org: OtraOrg, paciente: OtroPaciente, episodio: Episodio));

            Assert.Equal(noEncontrado, decision);
            Assert.Equal(1, e.Roles.Llamadas);
            AssertSinCargarHechos(e);
        }
    }

    [Fact]
    public async Task RF_ROL_005_perfil_nulo_o_sin_roles_es_sin_rol_en_sede()
    {
        var sinRolEnSede = Prohibido(MotivoDenegacion.SinRolEnSede);

        var sinPerfil = new Escenario([Rol.Recepcion]);
        sinPerfil.Roles.Perfil = null;
        Assert.Equal(
            sinRolEnSede,
            await sinPerfil.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        Assert.Equal(1, sinPerfil.Roles.Llamadas);
        AssertSinCargarHechos(sinPerfil);

        var sinRoles = new Escenario([]);
        Assert.Equal(
            sinRolEnSede,
            await sinRoles.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        // R0 se evalúa antes que R1: sin roles, un recurso de otra organización tampoco es «no encontrado».
        Assert.Equal(
            sinRolEnSede,
            await sinRoles.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(org: OtraOrg, paciente: OtroPaciente)));
        AssertSinCargarHechos(sinRoles);
    }

    [Fact]
    public async Task RF_ROL_005_consulta_el_perfil_con_usuario_sede_y_token_de_la_solicitud()
    {
        using var cts = new CancellationTokenSource();
        var e = new Escenario([Rol.Recepcion]);

        await e.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), cts.Token);

        Assert.Equal(1, e.Roles.Llamadas);
        Assert.Equal<(Guid, Guid)?>((Usuario, Sede), e.Roles.UltimosArgumentos);
        Assert.Equal(cts.Token, e.Roles.UltimoToken);
    }

    [Fact]
    public async Task RF_ROL_005_pasa_ahora_del_reloj_y_los_ids_a_la_relacion_clinica()
    {
        using var cts = new CancellationTokenSource();
        var ahora = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Reloj.Ahora = ahora;
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio), cts.Token);

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
        Assert.Equal<(Guid, Guid, Guid?, DateTimeOffset)?>((ProfesionalActor, OtroPaciente, Episodio, ahora), e.Relacion.UltimosArgumentos);
        Assert.Equal(cts.Token, e.Relacion.UltimoToken);
    }

    [Fact]
    public async Task RF_ROL_005_recurso_que_no_es_de_episodio_consulta_la_relacion_sin_episodio()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.ResumenSeguridad, Ctx(paciente: OtroPaciente));

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
        Assert.Equal<(Guid, Guid, Guid?, DateTimeOffset)?>((ProfesionalActor, OtroPaciente, null, e.Reloj.Ahora), e.Relacion.UltimosArgumentos);
    }

    [Fact]
    public async Task RF_ROL_005_profesional_sin_ficha_no_consulta_la_relacion()
    {
        // Review Focus 3: sin ficha de profesional no puede haber relación; el puerto ni se consulta.
        var e = new Escenario([Rol.Profesional], profesionalId: null);
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.SinRelacionClinica), decision);
        Assert.Equal(0, e.Relacion.Llamadas);
        Assert.Equal(0, e.Habilitaciones.Llamadas);
        Assert.Equal(0, e.Zonas.Llamadas);
    }

    [Theory]
    [InlineData(TipoRecurso.NotaClinica)]
    [InlineData(TipoRecurso.Orden)]
    [InlineData(TipoRecurso.SignosVitalesTriaje)]
    public async Task RF_ROL_005_recurso_de_episodio_sin_episodio_no_consulta_la_relacion_y_es_datos_insuficientes(TipoRecurso recurso)
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, recurso, Ctx(paciente: OtroPaciente, episodio: null));

        Assert.Equal(Prohibido(MotivoDenegacion.DatosInsuficientes), decision);
        Assert.Equal(0, e.Relacion.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_sin_paciente_en_el_contexto_no_consulta_la_relacion_y_es_datos_insuficientes()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.ResumenSeguridad, Ctx(paciente: null));

        Assert.Equal(Prohibido(MotivoDenegacion.DatosInsuficientes), decision);
        Assert.Equal(0, e.Relacion.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_relacion_nula_es_datos_insuficientes()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = null;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.DatosInsuficientes), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_habilitacion_nula_es_sin_habilitacion()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;
        e.Habilitaciones.Habilitacion = null;

        var decision = await e.Autorizar(Accion.Crear, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.SinHabilitacion), decision);
        Assert.Equal(1, e.Habilitaciones.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_vinculo_nulo_es_no_es_propio()
    {
        var e = new Escenario([Rol.Paciente]);
        e.Vinculo.PacienteId = null;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: PacienteActor));

        Assert.Equal(Prohibido(MotivoDenegacion.NoEsPropio), decision);
        Assert.Equal(1, e.Vinculo.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_paciente_consulta_su_vinculo_con_el_usuario_y_el_token_y_nada_mas()
    {
        using var cts = new CancellationTokenSource();
        var e = new Escenario([Rol.Paciente]);
        e.Vinculo.PacienteId = PacienteActor;

        var propio = await e.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: PacienteActor), cts.Token);
        var ajeno = await e.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), cts.Token);

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), propio);
        Assert.Equal(Prohibido(MotivoDenegacion.NoEsPropio), ajeno);
        Assert.Equal(2, e.Vinculo.Llamadas);
        Assert.Equal(Usuario, e.Vinculo.UltimoUsuarioId);
        Assert.Equal(cts.Token, e.Vinculo.UltimoToken);
        Assert.Equal(0, e.Relacion.Llamadas);
        Assert.Equal(0, e.Habilitaciones.Llamadas);
        Assert.Equal(0, e.Zonas.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_profesional_con_cita_propia_no_consulta_el_vinculo_del_paciente()
    {
        // «Propio» del profesional sale de su ficha; solo el rol paciente necesita el vínculo.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.Cita, Ctx(paciente: OtroPaciente, sede: Sede, profesional: ProfesionalActor));

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        AssertSinCargarHechos(e);
    }

    [Fact]
    public async Task RF_ROL_005_accion_sin_celda_no_consulta_ningun_hecho()
    {
        // Profesional x Actualizar x NotaClinica no tiene celda: no se pide relación ni habilitación.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;
        e.Habilitaciones.Habilitacion = HabilitacionVigente;

        var decision = await e.Autorizar(Accion.Actualizar, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.SinPermisoDeRol), decision);
        AssertSinCargarHechos(e);
    }

    [Fact]
    public async Task RF_ROL_005_lectura_clinica_no_consulta_habilitaciones_ni_zona_horaria()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
        Assert.Equal(0, e.Habilitaciones.Llamadas);
        Assert.Equal(0, e.Zonas.Llamadas);
    }

    [Fact]
    public async Task RF_ROL_005_escritura_clinica_carga_relacion_habilitacion_y_zona_una_vez_cada_una()
    {
        using var cts = new CancellationTokenSource();
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = Tratante;
        e.Habilitaciones.Habilitacion = HabilitacionVigente;

        // El recurso es de otra sede que la activa: la zona horaria debe ser la de la sede ACTIVA, no la del recurso.
        var decision = await e.Autorizar(
            Accion.Crear, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio, sede: OtraSede), cts.Token);

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Equal(0, e.Vinculo.Llamadas);
        Assert.Equal(1, e.Relacion.Llamadas);
        Assert.Equal(1, e.Habilitaciones.Llamadas);
        Assert.Equal(1, e.Zonas.Llamadas);
        // La habilitación se pide en la especialidad de la relación; la zona, de la sede activa.
        Assert.Equal<(Guid, Guid)?>((ProfesionalActor, Especialidad), e.Habilitaciones.UltimosArgumentos);
        Assert.Equal(Sede, e.Zonas.UltimaSedeId);
        Assert.Equal(cts.Token, e.Habilitaciones.UltimoToken);
        Assert.Equal(cts.Token, e.Zonas.UltimoToken);
    }

    [Fact]
    public async Task RF_ROL_005_sin_relacion_cargada_no_consulta_habilitaciones_ni_zona_horaria()
    {
        // Sin relación no hay especialidad en la que pedir la habilitación.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = null;
        e.Habilitaciones.Habilitacion = HabilitacionVigente;

        var decision = await e.Autorizar(Accion.Crear, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.DatosInsuficientes), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
        Assert.Equal(0, e.Habilitaciones.Llamadas);
        Assert.Equal(0, e.Zonas.Llamadas);
    }

    [Fact]
    public async Task RN_012_hoy_se_calcula_en_la_zona_horaria_de_la_sede()
    {
        // 02:00 UTC del 9 de octubre son las 22:00 del 8 en la sede (UTC-4): la licencia que vence el 8 sigue vigente.
        var ahora = new DateTimeOffset(2026, 10, 9, 2, 0, 0, TimeSpan.Zero);
        var venceAyerUtc = new HabilitacionProfesional(Especialidad, new HashSet<Guid>(), new DateOnly(2026, 10, 8));
        var utcMenos4 = TimeZoneInfo.CreateCustomTimeZone("sede", TimeSpan.FromHours(-4), "sede", "sede");

        async Task<Decision> Escribir(TimeZoneInfo zona)
        {
            var e = new Escenario([Rol.Profesional], ProfesionalActor);
            e.Reloj.Ahora = ahora;
            e.Relacion.Relacion = Tratante;
            e.Habilitaciones.Habilitacion = venceAyerUtc;
            e.Zonas.Zona = zona;
            // El recurso es de otra sede que la activa: «hoy» se mide en la zona de la sede ACTIVA, no en la del recurso.
            var decision = await e.Autorizar(
                Accion.Crear, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio, sede: OtraSede));
            Assert.Equal(Sede, e.Zonas.UltimaSedeId);
            return decision;
        }

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), await Escribir(utcMenos4));
        Assert.Equal(Prohibido(MotivoDenegacion.HabilitacionVencida), await Escribir(TimeZoneInfo.Utc));
    }

    // ---- Auditoría de denegaciones (spec §9) y falla cerrada (spec §10) ----

    [Fact]
    public async Task CA_AUD_003_recepcion_abre_nota_por_url_directa_genera_evento_denegado_critico()
    {
        // Recepción abre una nota clínica por URL directa: Prohibido y un único evento con todos sus campos.
        // El nivel «crítico» lo fija el puerto de Auditoría (siempre registra denegaciones como críticas).
        using var cts = new CancellationTokenSource();
        var ahora = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var e = new Escenario([Rol.Recepcion]);
        e.Reloj.Ahora = ahora;

        var decision = await e.Autorizar(
            Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio, recurso: RecursoNota), cts.Token);

        Assert.Equal(Prohibido(MotivoDenegacion.SinPermisoDeRol), decision);
        var evento = Assert.Single(e.Auditoria.Eventos);
        Assert.Equal(1, e.Auditoria.Llamadas);
        Assert.Equal(cts.Token, e.Auditoria.UltimoToken);
        Assert.Equal(ahora, evento.OcurridoEn);
        Assert.Equal(Usuario, evento.UsuarioId);
        Assert.Equal(Org, evento.OrganizacionId);
        Assert.Equal(Sede, evento.SedeId);
        Assert.Equal([Rol.Recepcion], evento.Roles);
        Assert.Equal(Accion.Leer, evento.Accion);
        Assert.Equal(TipoRecurso.NotaClinica, evento.Recurso);
        Assert.Equal(RecursoNota, evento.RecursoId);
        Assert.Equal(OtroPaciente, evento.PacienteId);
        Assert.Equal(TipoDenegacion.Prohibido, evento.Tipo);
        Assert.Equal(MotivoDenegacion.SinPermisoDeRol, evento.Motivo);
    }

    [Fact]
    public async Task RF_ROL_005_acceso_permitido_no_genera_evento()
    {
        // Los accesos permitidos los audita el módulo consumidor (spec §9): el autorizador no escribe nada.
        var recepcion = new Escenario([Rol.Recepcion]);
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            await recepcion.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        Assert.Equal(0, recepcion.Auditoria.Llamadas);
        Assert.Empty(recepcion.Auditoria.Eventos);

        var tratante = new Escenario([Rol.Profesional], ProfesionalActor);
        tratante.Relacion.Relacion = Tratante;
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            await tratante.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio, recurso: RecursoNota)));
        Assert.Equal(0, tratante.Auditoria.Llamadas);
        Assert.Empty(tratante.Auditoria.Eventos);
    }

    [Fact]
    public async Task RN_015_otra_organizacion_genera_evento_con_motivo_otra_organizacion()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);

        var decision = await e.Autorizar(
            Accion.Leer, TipoRecurso.NotaClinica, Ctx(org: OtraOrg, paciente: OtroPaciente, episodio: Episodio, recurso: RecursoNota));

        Assert.Equal(Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion), decision);
        var evento = Assert.Single(e.Auditoria.Eventos);
        Assert.Equal(TipoDenegacion.NoEncontrado, evento.Tipo);
        Assert.Equal(MotivoDenegacion.OtraOrganizacion, evento.Motivo);
        // La organización del evento es la del actor, no la del recurso ajeno.
        Assert.Equal(Org, evento.OrganizacionId);
        Assert.Equal(Usuario, evento.UsuarioId);
        Assert.Equal(Sede, evento.SedeId);
        Assert.Equal([Rol.Profesional], evento.Roles);
        Assert.Equal(Accion.Leer, evento.Accion);
        Assert.Equal(TipoRecurso.NotaClinica, evento.Recurso);
        Assert.Equal(RecursoNota, evento.RecursoId);
        Assert.Equal(OtroPaciente, evento.PacienteId);
    }

    [Fact]
    public async Task RF_ROL_005_perfil_nulo_genera_evento_con_organizacion_nula()
    {
        var ahora = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var e = new Escenario([Rol.Recepcion]);
        e.Roles.Perfil = null;
        e.Reloj.Ahora = ahora;

        var decision = await e.Autorizar(
            Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente, recurso: RecursoNota));

        Assert.Equal(Prohibido(MotivoDenegacion.SinRolEnSede), decision);
        var evento = Assert.Single(e.Auditoria.Eventos);
        Assert.Equal(ahora, evento.OcurridoEn);
        Assert.Equal(Usuario, evento.UsuarioId);
        Assert.Null(evento.OrganizacionId);
        Assert.Equal(Sede, evento.SedeId);
        Assert.Empty(evento.Roles);
        Assert.Equal(Accion.Leer, evento.Accion);
        Assert.Equal(TipoRecurso.DatosDemograficos, evento.Recurso);
        Assert.Equal(RecursoNota, evento.RecursoId);
        Assert.Equal(OtroPaciente, evento.PacienteId);
        Assert.Equal(TipoDenegacion.Prohibido, evento.Tipo);
        Assert.Equal(MotivoDenegacion.SinRolEnSede, evento.Motivo);
    }

    [Fact]
    public async Task RF_ROL_005_el_evento_conserva_los_roles_aunque_el_perfil_cambie_despues()
    {
        // Los roles del evento son una copia: el perfil lo entrega el puerto y podría reutilizarse o mutarse después,
        // y el evento que ya se entregó a Auditoría no debe cambiar con él.
        var rolesDelPerfil = new HashSet<Rol> { Rol.Recepcion };
        var e = new Escenario([Rol.Recepcion]);
        e.Roles.Perfil = new PerfilActor(Usuario, Org, rolesDelPerfil, null);

        var decision = await e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio));

        Assert.Equal(Prohibido(MotivoDenegacion.SinPermisoDeRol), decision);
        var evento = Assert.Single(e.Auditoria.Eventos);
        Assert.Equal([Rol.Recepcion], evento.Roles);

        rolesDelPerfil.Remove(Rol.Recepcion);
        rolesDelPerfil.Add(Rol.Director);

        Assert.Equal([Rol.Recepcion], evento.Roles);
        Assert.Equal([Rol.Recepcion], e.Auditoria.Eventos[0].Roles);
    }

    [Fact]
    public void RF_ROL_005_el_evento_de_autorizacion_no_tiene_texto_libre()
    {
        // Solo identificadores y códigos: sin propiedades de texto no puede viajar contenido clínico (spec §9).
        var propiedades = typeof(EventoAutorizacion).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(propiedades);
        Assert.DoesNotContain(propiedades, p => p.PropertyType == typeof(string));
    }

    /// <summary>
    /// Escenario en el que la solicitud llega hasta <paramref name="puerto"/> y este lanza <paramref name="fallo"/>.
    /// Devuelve también cuántas veces se consultó ese puerto, para probar que el fallo se ejerció de verdad.
    /// </summary>
    private static (Escenario Escenario, Accion Accion, TipoRecurso Recurso, ContextoRecurso Contexto, Func<int> LlamadasDelPuerto)
        EscenarioConPuertoCaido(string puerto, Exception fallo)
    {
        var contextoClinico = Ctx(paciente: OtroPaciente, episodio: Episodio, recurso: RecursoNota);
        switch (puerto)
        {
            case "roles":
            {
                var e = new Escenario([Rol.Recepcion]);
                e.Roles.Fallo = fallo;
                return (e, Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), () => e.Roles.Llamadas);
            }
            case "vinculo":
            {
                var e = new Escenario([Rol.Paciente]);
                e.Vinculo.Fallo = fallo;
                return (e, Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: PacienteActor), () => e.Vinculo.Llamadas);
            }
            case "relacion":
            {
                var e = new Escenario([Rol.Profesional], ProfesionalActor);
                e.Relacion.Fallo = fallo;
                return (e, Accion.Leer, TipoRecurso.NotaClinica, contextoClinico, () => e.Relacion.Llamadas);
            }
            case "habilitaciones":
            {
                var e = new Escenario([Rol.Profesional], ProfesionalActor);
                e.Relacion.Relacion = Tratante;
                e.Habilitaciones.Fallo = fallo;
                return (e, Accion.Crear, TipoRecurso.NotaClinica, contextoClinico, () => e.Habilitaciones.Llamadas);
            }
            case "zona":
            {
                var e = new Escenario([Rol.Profesional], ProfesionalActor);
                e.Relacion.Relacion = Tratante;
                e.Habilitaciones.Habilitacion = HabilitacionVigente;
                e.Zonas.Fallo = fallo;
                return (e, Accion.Crear, TipoRecurso.NotaClinica, contextoClinico, () => e.Zonas.Llamadas);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(puerto), puerto, null);
        }
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("vinculo")]
    [InlineData("relacion")]
    [InlineData("habilitaciones")]
    [InlineData("zona")]
    public async Task RF_ROL_005_si_un_puerto_lanza_excepcion_se_propaga_y_no_se_concede_nada(string puerto)
    {
        // Falla cerrada (spec §10): la excepción llega tal cual al llamador, no se devuelve decisión alguna
        // (ni Permitido ni una denegación auditada) y la auditoría queda sin tocar.
        var fallo = new InvalidOperationException("puerto caído");
        var (e, accion, recurso, contexto, llamadasDelPuerto) = EscenarioConPuertoCaido(puerto, fallo);

        var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Autorizar(accion, recurso, contexto));

        Assert.Same(fallo, lanzada);
        Assert.Equal(1, llamadasDelPuerto());
        Assert.Equal(0, e.Auditoria.Llamadas);
        Assert.Empty(e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RF_ROL_005_si_falla_el_registro_de_la_denegacion_la_excepcion_se_propaga()
    {
        // Se prefiere fallar a perder en silencio un evento crítico (spec §10): el llamador nunca recibe la denegación sin su evento.
        var fallo = new InvalidOperationException("auditoría caída");
        var e = new Escenario([Rol.Recepcion]);
        e.Auditoria.Fallo = fallo;

        var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
            () => e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio)));

        Assert.Same(fallo, lanzada);
        Assert.Equal(1, e.Auditoria.Llamadas);

        // Con la auditoría caída, un acceso permitido sigue su curso: solo las denegaciones la usan.
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            await e.Autorizar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente)));
        Assert.Equal(1, e.Auditoria.Llamadas);
    }

    #region Matriz mínima de endpoints clínicos

    // Las seis filas de la matriz mínima de CLAUDE.md para un endpoint clínico (aquí, NotaClinica x Leer) a través del
    // Autorizador completo (spec §11). La séptima, «sin sesión → 401», es del adaptador HTTP y queda fuera de este tramo.
    // Cada prueba compara la Decision entera y, si deniega, comprueba además el evento de auditoría (CA-AUD-003).

    private static RelacionClinica RelacionDe(TipoRelacion tipo, bool permiteSensible = false, bool episodioSensible = false) =>
        new(tipo, permiteSensible, episodioSensible, Especialidad);

    /// <summary>Lectura de una nota del <see cref="OtroPaciente"/> en el <see cref="Episodio"/> (de la organización <see cref="Org"/> salvo que se indique).</summary>
    private static Task<Decision> LeerNota(Escenario e, Guid? org = null) =>
        e.Autorizar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(org: org, paciente: OtroPaciente, episodio: Episodio, recurso: RecursoNota));

    /// <summary>Una denegación deja exactamente un evento, con el tipo y el motivo de la decisión.</summary>
    private static void AssertUnEventoDeDenegacion(Escenario e, TipoDenegacion tipo, MotivoDenegacion motivo)
    {
        var evento = Assert.Single(e.Auditoria.Eventos);
        Assert.Equal(tipo, evento.Tipo);
        Assert.Equal(motivo, evento.Motivo);
    }

    [Fact]
    public async Task RN_001_tratante_lee_la_nota_completa()
    {
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = RelacionDe(TipoRelacion.Tratante);

        var decision = await LeerNota(e);

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Empty(e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RN_001_equipo_en_episodio_no_sensible_lee_la_nota_completa()
    {
        // El permiso sensible del miembro del equipo no interviene si el episodio no es sensible.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = RelacionDe(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: false);

        var decision = await LeerNota(e);

        Assert.Equal(Decision.Permitir(NivelAcceso.Completo), decision);
        Assert.Empty(e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RN_016_equipo_sin_permiso_en_episodio_sensible_lee_el_resumen()
    {
        // Es una concesión reducida, no una denegación: el acceso es Resumen y no se audita como denegado.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = RelacionDe(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true);

        var decision = await LeerNota(e);

        Assert.Equal(Decision.Permitir(NivelAcceso.Resumen), decision);
        Assert.Empty(e.Auditoria.Eventos);
    }

    [Fact]
    public async Task RN_001_profesional_ajeno_se_deniega_con_sin_relacion_clinica()
    {
        // Un profesional de la organización, con ficha, pero sin relación con el paciente.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = RelacionDe(TipoRelacion.Ninguna);

        var decision = await LeerNota(e);

        Assert.Equal(Prohibido(MotivoDenegacion.SinRelacionClinica), decision);
        Assert.Equal(1, e.Relacion.Llamadas);
        AssertUnEventoDeDenegacion(e, TipoDenegacion.Prohibido, MotivoDenegacion.SinRelacionClinica);
    }

    [Fact]
    public async Task RN_002_recepcion_se_deniega_con_sin_permiso_de_rol()
    {
        var e = new Escenario([Rol.Recepcion]);

        var decision = await LeerNota(e);

        Assert.Equal(Prohibido(MotivoDenegacion.SinPermisoDeRol), decision);
        AssertSinCargarHechos(e);
        AssertUnEventoDeDenegacion(e, TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol);
    }

    [Fact]
    public async Task RN_015_otra_organizacion_responde_no_encontrado()
    {
        // Con una relación de tratante cargable, solo la organización distinta impide el acceso: un 404, no un 403.
        var e = new Escenario([Rol.Profesional], ProfesionalActor);
        e.Relacion.Relacion = RelacionDe(TipoRelacion.Tratante);

        var decision = await LeerNota(e, org: OtraOrg);

        Assert.Equal(Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion), decision);
        AssertSinCargarHechos(e);
        AssertUnEventoDeDenegacion(e, TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion);
    }

    #endregion
}
