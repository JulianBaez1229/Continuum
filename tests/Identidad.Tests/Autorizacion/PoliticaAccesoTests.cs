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
}
