using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Autorizacion;

public class PoliticaAccesoTests
{
    // Identificadores inventados (datos ficticios).
    private static readonly Guid Org = Guid.Parse("00000000-0000-0000-0000-0000000000a1"), Sede = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid OtraOrganizacion = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid OtraSede = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid UsuarioActor = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid ProfesionalActor = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid OtroProfesional = Guid.Parse("00000000-0000-0000-0000-0000000000d2");
    private static readonly Guid PacienteActor = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly Guid OtroPaciente = Guid.Parse("00000000-0000-0000-0000-0000000000e2");
    private static readonly Guid Episodio = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
    private static readonly Guid Especialidad = Guid.Parse("00000000-0000-0000-0000-000000000051");

    /// <summary>Habilitación vigente en <see cref="Especialidad"/>, sin lista de procedimientos.</summary>
    private static readonly HabilitacionProfesional HabilitacionVigente = new(Especialidad, new HashSet<Guid>(), new DateOnly(2027, 1, 1));

    /// <summary>Hechos del actor. Por defecto: organización <see cref="Org"/>, sede activa <see cref="Sede"/> y hoy 2026-10-09.</summary>
    private static HechosAcceso Hechos(Rol[] roles, Guid? profesionalId = null, Guid? pacienteDelActor = null,
        RelacionClinica? relacion = null, HabilitacionProfesional? hab = null, DateOnly? hoy = null, Guid? sedeActiva = null, Guid? org = null) =>
        new(
            new PerfilActor(UsuarioActor, org ?? Org, new HashSet<Rol>(roles), profesionalId),
            sedeActiva ?? Sede,
            pacienteDelActor,
            relacion,
            hab,
            hoy ?? new DateOnly(2026, 10, 9));

    /// <summary>Contexto del recurso. La organización por defecto es <see cref="Org"/>.</summary>
    private static ContextoRecurso Ctx(Guid? org = null, Guid? paciente = null, Guid? profesional = null, Guid? episodio = null,
        Guid? sede = null, bool? liberado = null, AlcanceReporte? alcance = null, Guid? procedimiento = null) =>
        new(
            org ?? Org,
            PacienteId: paciente,
            ProfesionalId: profesional,
            EpisodioId: episodio,
            SedeId: sede,
            ProcedimientoId: procedimiento,
            LiberadoAlPaciente: liberado,
            AlcanceReporte: alcance);

    /// <summary>Contexto de un recurso de episodio de un paciente que no es del actor.</summary>
    private static ContextoRecurso CtxDeEpisodio => Ctx(paciente: OtroPaciente, episodio: Episodio);

    /// <summary>Relación del actor con el paciente. Por defecto el miembro no tiene permiso sensible y el episodio no es sensible.</summary>
    private static RelacionClinica Rel(TipoRelacion tipo, bool permiteSensible = false, bool episodioSensible = false) =>
        new(tipo, permiteSensible, episodioSensible, Especialidad);

    /// <summary>
    /// Profesional con ficha y habilitación vigente. Estas pruebas verifican la relación y la sensibilidad (R6–R7);
    /// la habilitación (R8) que exigen las escrituras queda cubierta para que no interfiera.
    /// </summary>
    private static HechosAcceso Profesional(RelacionClinica? relacion) =>
        Hechos([Rol.Profesional], profesionalId: ProfesionalActor, relacion: relacion, hab: HabilitacionVigente);

    /// <summary>Asistente clínico con ficha de profesional: sin ella no puede tener relación con el paciente (R6).</summary>
    private static HechosAcceso Asistente(RelacionClinica? relacion) =>
        Hechos([Rol.AsistenteClinico], profesionalId: ProfesionalActor, relacion: relacion);

    private static Decision Prohibido(MotivoDenegacion motivo) => Decision.Denegar(TipoDenegacion.Prohibido, motivo);

    [Fact]
    public void CA_ROL_001_recepcion_ve_demograficos_y_citas_pero_no_notas_diagnosticos_ni_ordenes()
    {
        var recepcion = Hechos([Rol.Recepcion]);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), recepcion));
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(paciente: OtroPaciente, sede: Sede), recepcion));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente, episodio: Episodio), recepcion));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, Ctx(paciente: OtroPaciente, episodio: Episodio), recepcion));
    }

    [Fact]
    public void RN_015_recurso_de_otra_organizacion_responde_no_encontrado()
    {
        var recepcion = Hechos([Rol.Recepcion]);
        var noEncontrado = Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion);

        Assert.Equal(
            noEncontrado,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(org: OtraOrganizacion, sede: Sede), recepcion));
        Assert.Equal(
            noEncontrado,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(org: OtraOrganizacion, paciente: OtroPaciente), recepcion));
        // Aunque el rol no tenga celda: un 403 revelaría que el recurso existe en otra organización.
        Assert.Equal(
            noEncontrado,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(org: OtraOrganizacion, paciente: OtroPaciente, episodio: Episodio), recepcion));
    }

    [Fact]
    public void RN_002_actor_sin_roles_en_la_sede_es_prohibido()
    {
        var sinRoles = Hechos([]);
        var sinRolEnSede = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinRolEnSede);

        Assert.Equal(sinRolEnSede, PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), sinRoles));
        // R0 se evalúa antes que R1.
        Assert.Equal(sinRolEnSede, PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(org: OtraOrganizacion), sinRoles));
    }

    [Fact]
    public void RN_002_director_ve_citas_solo_agregado_y_no_ve_datos_demograficos()
    {
        var director = Hechos([Rol.Director]);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Agregado),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(), director));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), director));
    }

    [Fact]
    public void RN_002_recepcion_no_actua_sobre_citas_de_otra_sede()
    {
        var recepcion = Hechos([Rol.Recepcion]);

        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SedeDistinta),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(sede: OtraSede), recepcion));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SedeDistinta),
            PoliticaAcceso.Evaluar(Accion.Anular, TipoRecurso.Cita, Ctx(sede: OtraSede), recepcion));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.DatosInsuficientes),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(sede: null), recepcion));
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(sede: OtraSede), Hechos([Rol.AdminFuncional])));
    }

    [Theory]
    [InlineData(Accion.Leer)]
    [InlineData(Accion.Actualizar)]
    public void RN_002_paciente_lee_y_actualiza_solo_sus_datos_demograficos(Accion accion)
    {
        var paciente = Hechos([Rol.Paciente], pacienteDelActor: PacienteActor);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(accion, TipoRecurso.DatosDemograficos, Ctx(paciente: PacienteActor), paciente));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoEsPropio),
            PoliticaAcceso.Evaluar(accion, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), paciente));
    }

    [Theory]
    [InlineData(Accion.Crear)]
    [InlineData(Accion.Leer)]
    [InlineData(Accion.Actualizar)]
    public void RN_002_profesional_gestiona_solo_su_propia_agenda(Accion accion)
    {
        var profesional = Hechos([Rol.Profesional], profesionalId: ProfesionalActor);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(accion, TipoRecurso.AgendaProfesional, Ctx(profesional: ProfesionalActor, sede: Sede), profesional));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoEsPropio),
            PoliticaAcceso.Evaluar(accion, TipoRecurso.AgendaProfesional, Ctx(profesional: OtroProfesional, sede: Sede), profesional));
    }

    [Fact]
    public void RF_ROL_005_propio_con_ids_nulos_en_ambos_lados_no_concede()
    {
        var noEsPropio = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoEsPropio);

        // Profesional sin ficha (ProfesionalId nulo) y agenda sin ProfesionalId.
        Assert.Equal(
            noEsPropio,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.AgendaProfesional, Ctx(sede: Sede), Hechos([Rol.Profesional])));
        // Usuario paciente sin vínculo y recurso sin PacienteId.
        Assert.Equal(
            noEsPropio,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(), Hechos([Rol.Paciente])));
    }

    [Fact]
    public void RF_ROL_005_paciente_lee_solo_ordenes_liberadas()
    {
        var paciente = Hechos([Rol.Paciente], pacienteDelActor: PacienteActor);
        var noLiberado = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoLiberado);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, Ctx(paciente: PacienteActor, episodio: Episodio, liberado: true), paciente));
        Assert.Equal(
            noLiberado,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, Ctx(paciente: PacienteActor, episodio: Episodio, liberado: false), paciente));
        Assert.Equal(
            noLiberado,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, Ctx(paciente: PacienteActor, episodio: Episodio, liberado: null), paciente));
    }

    [Fact]
    public void RF_ROL_005_reporte_sin_alcance_es_datos_insuficientes()
    {
        var datosInsuficientes = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.DatosInsuficientes);

        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(alcance: null), Hechos([Rol.Director])));
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(profesional: ProfesionalActor, alcance: null),
                Hechos([Rol.Profesional], profesionalId: ProfesionalActor)));
    }

    [Fact]
    public void RF_ROL_005_profesional_lee_solo_reporte_propio()
    {
        var profesional = Hechos([Rol.Profesional], profesionalId: ProfesionalActor);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(profesional: ProfesionalActor, alcance: AlcanceReporte.Propio), profesional));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoEsPropio),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(profesional: OtroProfesional, alcance: AlcanceReporte.Propio), profesional));
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(profesional: ProfesionalActor, alcance: AlcanceReporte.Operativo), profesional));
    }

    [Theory]
    [InlineData(AlcanceReporte.Operativo)]
    [InlineData(AlcanceReporte.Sede)]
    [InlineData(AlcanceReporte.Global)]
    [InlineData(AlcanceReporte.Cumplimiento)]
    public void RF_ROL_005_director_lee_reportes_agregados(AlcanceReporte alcance)
    {
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Agregado),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(alcance: alcance), Hechos([Rol.Director])));
    }

    [Fact]
    public void RF_ROL_005_varios_roles_gana_el_mas_permisivo()
    {
        var directorYProfesional = Hechos([Rol.Director, Rol.Profesional], profesionalId: ProfesionalActor);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(profesional: ProfesionalActor, sede: Sede), directorYProfesional));
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Agregado),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(profesional: OtroProfesional, sede: Sede), directorYProfesional));
    }

    [Fact]
    public void RF_ROL_005_varios_roles_sin_concesion_devuelve_el_motivo_mas_profundo()
    {
        var noEsPropio = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.NoEsPropio);

        // Recepción se detiene en R3 (SedeDistinta); Paciente llega a R4 (NoEsPropio).
        Assert.Equal(
            noEsPropio,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Cita, Ctx(paciente: OtroPaciente, sede: OtraSede),
                Hechos([Rol.Recepcion, Rol.Paciente], pacienteDelActor: PacienteActor)));
        // Paciente se detiene en R2 (sin celda) aunque su valor en Rol es menor; Profesional llega a R4.
        Assert.Equal(
            noEsPropio,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.AgendaProfesional, Ctx(profesional: OtroProfesional, sede: Sede),
                Hechos([Rol.Paciente, Rol.Profesional], profesionalId: ProfesionalActor, pacienteDelActor: PacienteActor)));
    }

    [Fact]
    public void RF_ROL_005_valores_de_enum_fuera_de_rango_se_deniegan_sin_excepcion()
    {
        var sinPermiso = Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol);
        var recepcion = Hechos([Rol.Recepcion]);

        Assert.Equal(sinPermiso, PoliticaAcceso.Evaluar((Accion)99, TipoRecurso.Cita, Ctx(sede: Sede), recepcion));
        Assert.Equal(sinPermiso, PoliticaAcceso.Evaluar(Accion.Leer, (TipoRecurso)99, Ctx(sede: Sede), recepcion));
        Assert.Equal(sinPermiso, PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(), Hechos([(Rol)99])));
        Assert.Equal(
            sinPermiso,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Reporte, Ctx(alcance: (AlcanceReporte)99), Hechos([Rol.Director])));
    }

    // ---- R6: relación clínica (RN-001) ----

    [Fact]
    public void RN_001_tratante_lee_la_nota_de_su_episodio_completa()
    {
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Profesional(Rel(TipoRelacion.Tratante))));
    }

    [Fact]
    public void RN_001_miembro_del_equipo_lee_nota_no_sensible_completa()
    {
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Profesional(Rel(TipoRelacion.Equipo))));
    }

    [Fact]
    public void RN_001_profesional_sin_relacion_no_lee_la_nota()
    {
        var sinRelacion = Prohibido(MotivoDenegacion.SinRelacionClinica);

        Assert.Equal(
            sinRelacion,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Profesional(Rel(TipoRelacion.Ninguna))));
        // R6 va antes que R7: el permiso sensible no sustituye a la relación.
        Assert.Equal(
            sinRelacion,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Profesional(Rel(TipoRelacion.Ninguna, permiteSensible: true, episodioSensible: true))));
    }

    [Fact]
    public void CA_ROL_002_cardiologo_ajeno_ve_demograficos_pero_no_notas_de_psicologia()
    {
        var cardiologoAjeno = Profesional(Rel(TipoRelacion.Ninguna));

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.DatosDemograficos, Ctx(paciente: OtroPaciente), cardiologoAjeno));
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, cardiologoAjeno));
    }

    [Fact]
    public void RF_ROL_005_profesional_sin_ficha_en_recurso_con_relacion_es_sin_relacion_clinica()
    {
        var sinFicha = Hechos([Rol.Profesional], relacion: Rel(TipoRelacion.Tratante));

        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, sinFicha));
        // Sin consultar la relación: aunque falte, el motivo sigue siendo la ausencia de ficha.
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Hechos([Rol.Profesional], relacion: null)));
        // También en recursos de paciente, que no llevan episodio.
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.ResumenSeguridad, Ctx(paciente: OtroPaciente), sinFicha));
    }

    [Fact]
    public void RF_ROL_005_recurso_de_episodio_sin_episodio_o_con_relacion_nula_es_datos_insuficientes()
    {
        var datosInsuficientes = Prohibido(MotivoDenegacion.DatosInsuficientes);

        // Sin EpisodioId en un recurso de episodio.
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente),
                Profesional(Rel(TipoRelacion.Tratante))));
        // Relación nula: el puerto no la encontró.
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Profesional(null)));
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.ResumenSeguridad, Ctx(paciente: OtroPaciente), Profesional(null)));
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.SignosVitalesTriaje, CtxDeEpisodio, Asistente(null)));
    }

    [Fact]
    public void RF_ROL_005_recurso_de_episodio_sin_episodio_es_datos_insuficientes_para_cualquier_rol()
    {
        var datosInsuficientes = Prohibido(MotivoDenegacion.DatosInsuficientes);

        // Paciente: la celda no exige relación, pero la orden pertenece a un episodio.
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, Ctx(paciente: PacienteActor, liberado: true),
                Hechos([Rol.Paciente], pacienteDelActor: PacienteActor)));
        // Asistente con relación presente pero sin episodio.
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Crear, TipoRecurso.SignosVitalesTriaje, Ctx(paciente: OtroPaciente),
                Asistente(Rel(TipoRelacion.Equipo))));
        // Sin celda no hay nada que completar: sigue siendo SIN_PERMISO_DE_ROL.
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinPermisoDeRol),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente), Hechos([Rol.Recepcion])));
        // Con varios roles, faltar el episodio (rango de R3) llega más lejos que no tener celda (R2),
        // aunque el rol sin celda (Paciente) tenga el menor valor de Rol.
        Assert.Equal(
            datosInsuficientes,
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, Ctx(paciente: OtroPaciente),
                Hechos([Rol.Paciente, Rol.Profesional], profesionalId: ProfesionalActor, relacion: Rel(TipoRelacion.Tratante))));
    }

    [Fact]
    public void RF_ROL_005_tipo_de_relacion_fuera_de_rango_se_deniega_sin_excepcion()
    {
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Profesional(Rel((TipoRelacion)99, permiteSensible: true))));
    }

    [Fact]
    public void RN_016_solo_nota_orden_y_signos_vitales_son_recursos_de_episodio()
    {
        Assert.Equal(
            [TipoRecurso.NotaClinica, TipoRecurso.SignosVitalesTriaje, TipoRecurso.Orden],
            Enum.GetValues<TipoRecurso>().Where(PoliticaAcceso.EsDeEpisodio));
        Assert.False(PoliticaAcceso.EsDeEpisodio((TipoRecurso)99));
    }

    // ---- R7: sensibilidad (RN-016) ----

    /// <summary>Celdas del profesional sobre recursos de episodio; se usan con el permiso o sin él.</summary>
    public static TheoryData<Accion, TipoRecurso> CeldasDeEpisodioDelProfesional => new()
    {
        { Accion.Leer, TipoRecurso.NotaClinica },
        { Accion.Crear, TipoRecurso.NotaClinica },
        { Accion.Leer, TipoRecurso.Orden },
        { Accion.Crear, TipoRecurso.Orden },
        { Accion.Anular, TipoRecurso.Orden },
        { Accion.Leer, TipoRecurso.SignosVitalesTriaje }
    };

    [Fact]
    public void RN_016_equipo_sin_permiso_en_episodio_sensible_ve_el_resumen_de_la_nota()
    {
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Resumen),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Profesional(Rel(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true))));
    }

    [Theory]
    [InlineData(Accion.Crear, TipoRecurso.NotaClinica)]
    [InlineData(Accion.Leer, TipoRecurso.Orden)]
    [InlineData(Accion.Crear, TipoRecurso.Orden)]
    [InlineData(Accion.Anular, TipoRecurso.Orden)]
    [InlineData(Accion.Leer, TipoRecurso.SignosVitalesTriaje)]
    public void RN_016_equipo_sin_permiso_no_crea_notas_ni_lee_ordenes_en_episodio_sensible(Accion accion, TipoRecurso recurso)
    {
        Assert.Equal(
            Prohibido(MotivoDenegacion.EpisodioSensible),
            PoliticaAcceso.Evaluar(accion, recurso, CtxDeEpisodio,
                Profesional(Rel(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true))));
    }

    [Theory]
    [MemberData(nameof(CeldasDeEpisodioDelProfesional))]
    public void RN_016_tratante_y_equipo_con_permiso_ven_completo_en_episodio_sensible(Accion accion, TipoRecurso recurso)
    {
        RelacionClinica[] conAcceso =
        [
            Rel(TipoRelacion.Tratante, permiteSensible: false, episodioSensible: true),
            Rel(TipoRelacion.Tratante, permiteSensible: true, episodioSensible: true),
            Rel(TipoRelacion.Equipo, permiteSensible: true, episodioSensible: true)
        ];

        foreach (var relacion in conAcceso)
        {
            Assert.Equal(
                Decision.Permitir(NivelAcceso.Completo),
                PoliticaAcceso.Evaluar(accion, recurso, CtxDeEpisodio, Profesional(relacion)));
        }
    }

    [Theory]
    [MemberData(nameof(CeldasDeEpisodioDelProfesional))]
    public void RN_016_el_permiso_sensible_no_cambia_nada_en_episodio_no_sensible(Accion accion, TipoRecurso recurso)
    {
        foreach (var tipo in new[] { TipoRelacion.Tratante, TipoRelacion.Equipo })
        {
            foreach (var permiteSensible in new[] { false, true })
            {
                Assert.Equal(
                    Decision.Permitir(NivelAcceso.Completo),
                    PoliticaAcceso.Evaluar(accion, recurso, CtxDeEpisodio,
                        Profesional(Rel(tipo, permiteSensible, episodioSensible: false))));
            }
        }
    }

    [Fact]
    public void RN_016_asistente_lee_la_nota_por_seccion_en_episodio_no_sensible()
    {
        Assert.Equal(
            Decision.Permitir(NivelAcceso.PorSeccion),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio, Asistente(Rel(TipoRelacion.Equipo))));
        // El permiso sensible no cambia el nivel en un episodio no sensible.
        Assert.Equal(
            Decision.Permitir(NivelAcceso.PorSeccion),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Asistente(Rel(TipoRelacion.Equipo, permiteSensible: true))));
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.Orden, CtxDeEpisodio, Asistente(Rel(TipoRelacion.Equipo))));
    }

    [Fact]
    public void RN_016_asistente_registra_signos_vitales_de_su_episodio()
    {
        var asistente = Asistente(Rel(TipoRelacion.Equipo));

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Crear, TipoRecurso.SignosVitalesTriaje, CtxDeEpisodio, asistente));
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Actualizar, TipoRecurso.SignosVitalesTriaje, CtxDeEpisodio, asistente));
    }

    [Theory]
    [InlineData(Accion.Leer, TipoRecurso.NotaClinica)]
    [InlineData(Accion.Leer, TipoRecurso.Orden)]
    [InlineData(Accion.Leer, TipoRecurso.SignosVitalesTriaje)]
    [InlineData(Accion.Crear, TipoRecurso.SignosVitalesTriaje)]
    [InlineData(Accion.Actualizar, TipoRecurso.SignosVitalesTriaje)]
    public void RN_016_asistente_no_accede_a_episodios_sensibles(Accion accion, TipoRecurso recurso)
    {
        RelacionClinica[] relaciones =
        [
            Rel(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true),
            // El rol manda: ni el permiso sensible ni ser tratante lo abren al asistente.
            Rel(TipoRelacion.Equipo, permiteSensible: true, episodioSensible: true),
            Rel(TipoRelacion.Tratante, permiteSensible: true, episodioSensible: true)
        ];

        foreach (var relacion in relaciones)
        {
            Assert.Equal(
                Prohibido(MotivoDenegacion.EpisodioSensible),
                PoliticaAcceso.Evaluar(accion, recurso, CtxDeEpisodio, Asistente(relacion)));
        }
    }

    [Fact]
    public void RN_016_resumen_de_seguridad_no_se_filtra_por_sensibilidad()
    {
        var episodioSensible = Rel(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true);
        var resumenDelPaciente = Ctx(paciente: OtroPaciente);

        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.ResumenSeguridad, resumenDelPaciente, Profesional(episodioSensible)));
        // Tampoco para el asistente, que sí queda fuera de los recursos de episodio sensibles.
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.ResumenSeguridad, resumenDelPaciente, Asistente(episodioSensible)));
    }

    [Fact]
    public void RN_016_varios_roles_con_relacion_conceden_lo_mas_amplio_y_explican_el_motivo_mas_profundo()
    {
        HechosAcceso profesionalYAsistente(RelacionClinica relacion) =>
            Hechos([Rol.Profesional, Rol.AsistenteClinico], profesionalId: ProfesionalActor, relacion: relacion, hab: HabilitacionVigente);

        // Profesional: Completo; Asistente: PorSeccion.
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Completo),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                profesionalYAsistente(Rel(TipoRelacion.Equipo))));
        // Episodio sensible, equipo sin permiso: Profesional ve el resumen y el asistente queda fuera.
        Assert.Equal(
            Decision.Permitir(NivelAcceso.Resumen),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                profesionalYAsistente(Rel(TipoRelacion.Equipo, permiteSensible: false, episodioSensible: true))));
        // Ningún rol concede: Paciente no tiene celda (R2) y el asistente llegó a R7.
        Assert.Equal(
            Prohibido(MotivoDenegacion.EpisodioSensible),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Hechos([Rol.Paciente, Rol.AsistenteClinico], profesionalId: ProfesionalActor,
                    relacion: Rel(TipoRelacion.Equipo, permiteSensible: true, episodioSensible: true))));
        // Sin relación en ninguno: gana la etapa más profunda (R6) sobre la falta de celda (R2).
        Assert.Equal(
            Prohibido(MotivoDenegacion.SinRelacionClinica),
            PoliticaAcceso.Evaluar(Accion.Leer, TipoRecurso.NotaClinica, CtxDeEpisodio,
                Hechos([Rol.Paciente, Rol.Profesional], profesionalId: ProfesionalActor, relacion: Rel(TipoRelacion.Ninguna))));
    }
}
