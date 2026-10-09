using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Autorizacion;

public class DecisionTests
{
    [Fact]
    public void RF_ROL_005_permitir_expone_el_nivel_y_ningun_motivo()
    {
        var decision = Decision.Permitir(NivelAcceso.Resumen);

        Assert.True(decision.EstaPermitido);
        Assert.Equal(NivelAcceso.Resumen, decision.Nivel);
        Assert.Null(decision.Tipo);
        Assert.Null(decision.Motivo);
    }

    [Fact]
    public void RF_ROL_005_denegar_expone_tipo_y_motivo_y_ningun_nivel()
    {
        var decision = Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion);

        Assert.False(decision.EstaPermitido);
        Assert.Null(decision.Nivel);
        Assert.Equal(TipoDenegacion.NoEncontrado, decision.Tipo);
        Assert.Equal(MotivoDenegacion.OtraOrganizacion, decision.Motivo);
    }

    [Fact]
    public void RF_ROL_005_decisiones_con_los_mismos_valores_son_iguales()
    {
        Assert.Equal(
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol),
            Decision.Denegar(TipoDenegacion.Prohibido, MotivoDenegacion.SinPermisoDeRol));
        Assert.NotEqual(Decision.Permitir(NivelAcceso.Completo), Decision.Permitir(NivelAcceso.Agregado));
    }

    [Theory]
    [InlineData(MotivoDenegacion.OtraOrganizacion, "OTRA_ORGANIZACION")]
    [InlineData(MotivoDenegacion.SinRolEnSede, "SIN_ROL_EN_SEDE")]
    [InlineData(MotivoDenegacion.SinPermisoDeRol, "SIN_PERMISO_DE_ROL")]
    [InlineData(MotivoDenegacion.SedeDistinta, "SEDE_DISTINTA")]
    [InlineData(MotivoDenegacion.NoEsPropio, "NO_ES_PROPIO")]
    [InlineData(MotivoDenegacion.NoLiberado, "NO_LIBERADO")]
    [InlineData(MotivoDenegacion.SinRelacionClinica, "SIN_RELACION_CLINICA")]
    [InlineData(MotivoDenegacion.EpisodioSensible, "EPISODIO_SENSIBLE")]
    [InlineData(MotivoDenegacion.SinHabilitacion, "SIN_HABILITACION")]
    [InlineData(MotivoDenegacion.HabilitacionVencida, "HABILITACION_VENCIDA")]
    [InlineData(MotivoDenegacion.DatosInsuficientes, "DATOS_INSUFICIENTES")]
    public void RF_ROL_005_codigo_de_auditoria_de_cada_motivo(MotivoDenegacion motivo, string esperado) =>
        Assert.Equal(esperado, motivo.Codigo());
}
