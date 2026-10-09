using Continuum.Identidad.Dominio.Autorizacion;

namespace Continuum.Identidad.Tests.Autorizacion;

public class MatrizPermisosTests
{
    private const Condiciones P = Condiciones.Propio;
    private const Condiciones S = Condiciones.MismaSede;
    private const Condiciones T = Condiciones.RelacionClinica;
    private const Condiciones Lib = Condiciones.Liberado;
    private const Condiciones Ninguna = Condiciones.Ninguna;

    /// <summary>Una fila de la spec §7. Acciones: C crear, L leer, A actualizar, X anular.</summary>
    private sealed record Esperado(
        Rol Rol,
        TipoRecurso Recurso,
        string Acciones,
        Condiciones Condiciones,
        NivelAcceso Nivel = NivelAcceso.Completo,
        AlcanceReporte? Alcance = null);

    // Tabla tecleada a mano desde la spec §7 (cuadrícula y tabla de reportes). Nunca se genera desde MatrizPermisos.
    private static readonly Esperado[] Tabla =
    [
        // Paciente (8 celdas)
        new(Rol.Paciente, TipoRecurso.DatosDemograficos, "LA", P),
        new(Rol.Paciente, TipoRecurso.Cita, "L", P),
        new(Rol.Paciente, TipoRecurso.Orden, "L", P | Lib),
        new(Rol.Paciente, TipoRecurso.RespuestaFormulario, "CL", P),
        new(Rol.Paciente, TipoRecurso.Mensaje, "CL", P),

        // Profesional (19 celdas)
        new(Rol.Profesional, TipoRecurso.DatosDemograficos, "L", Ninguna),
        new(Rol.Profesional, TipoRecurso.Cita, "LA", P | S),
        new(Rol.Profesional, TipoRecurso.AgendaProfesional, "CLA", P | S),
        new(Rol.Profesional, TipoRecurso.NotaClinica, "CL", T),
        new(Rol.Profesional, TipoRecurso.ResumenSeguridad, "L", T),
        new(Rol.Profesional, TipoRecurso.SignosVitalesTriaje, "L", T),
        new(Rol.Profesional, TipoRecurso.Orden, "CLX", T),
        new(Rol.Profesional, TipoRecurso.AsignacionFormulario, "C", T),
        new(Rol.Profesional, TipoRecurso.RespuestaFormulario, "L", T),
        new(Rol.Profesional, TipoRecurso.Mensaje, "CL", T),
        new(Rol.Profesional, TipoRecurso.Catalogo, "L", Ninguna),
        new(Rol.Profesional, TipoRecurso.Reporte, "L", P, Alcance: AlcanceReporte.Propio),

        // Asistente clínico (10 celdas)
        new(Rol.AsistenteClinico, TipoRecurso.DatosDemograficos, "L", Ninguna),
        new(Rol.AsistenteClinico, TipoRecurso.Cita, "L", S),
        new(Rol.AsistenteClinico, TipoRecurso.AgendaProfesional, "L", S),
        new(Rol.AsistenteClinico, TipoRecurso.NotaClinica, "L", T, NivelAcceso.PorSeccion),
        new(Rol.AsistenteClinico, TipoRecurso.ResumenSeguridad, "L", T),
        new(Rol.AsistenteClinico, TipoRecurso.SignosVitalesTriaje, "CLA", T),
        new(Rol.AsistenteClinico, TipoRecurso.Orden, "L", T),
        new(Rol.AsistenteClinico, TipoRecurso.Catalogo, "L", Ninguna),

        // Recepción (10 celdas)
        new(Rol.Recepcion, TipoRecurso.DatosDemograficos, "CLA", Ninguna),
        new(Rol.Recepcion, TipoRecurso.Cita, "CLAX", S),
        new(Rol.Recepcion, TipoRecurso.AgendaProfesional, "L", S),
        new(Rol.Recepcion, TipoRecurso.Catalogo, "L", Ninguna),
        new(Rol.Recepcion, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Operativo),

        // Coordinador de sede (14 celdas)
        new(Rol.CoordinadorSede, TipoRecurso.DatosDemograficos, "CLA", Ninguna),
        new(Rol.CoordinadorSede, TipoRecurso.Cita, "CLAX", S),
        new(Rol.CoordinadorSede, TipoRecurso.AgendaProfesional, "CLA", S),
        new(Rol.CoordinadorSede, TipoRecurso.Catalogo, "L", Ninguna),
        new(Rol.CoordinadorSede, TipoRecurso.UsuarioRol, "L", S),
        new(Rol.CoordinadorSede, TipoRecurso.Reporte, "L", S, Alcance: AlcanceReporte.Operativo),
        new(Rol.CoordinadorSede, TipoRecurso.Reporte, "L", S, Alcance: AlcanceReporte.Sede),

        // Director (8 celdas)
        new(Rol.Director, TipoRecurso.Cita, "L", Ninguna, NivelAcceso.Agregado),
        new(Rol.Director, TipoRecurso.AgendaProfesional, "L", Ninguna, NivelAcceso.Agregado),
        new(Rol.Director, TipoRecurso.Catalogo, "L", Ninguna),
        new(Rol.Director, TipoRecurso.UsuarioRol, "L", Ninguna),
        new(Rol.Director, TipoRecurso.Reporte, "L", Ninguna, NivelAcceso.Agregado, AlcanceReporte.Operativo),
        new(Rol.Director, TipoRecurso.Reporte, "L", Ninguna, NivelAcceso.Agregado, AlcanceReporte.Sede),
        new(Rol.Director, TipoRecurso.Reporte, "L", Ninguna, NivelAcceso.Agregado, AlcanceReporte.Global),
        new(Rol.Director, TipoRecurso.Reporte, "L", Ninguna, NivelAcceso.Agregado, AlcanceReporte.Cumplimiento),

        // Administrador funcional (15 celdas)
        new(Rol.AdminFuncional, TipoRecurso.DatosDemograficos, "L", Ninguna),
        new(Rol.AdminFuncional, TipoRecurso.Cita, "L", Ninguna),
        new(Rol.AdminFuncional, TipoRecurso.AgendaProfesional, "L", Ninguna),
        new(Rol.AdminFuncional, TipoRecurso.Catalogo, "CLAX", Ninguna),
        new(Rol.AdminFuncional, TipoRecurso.UsuarioRol, "CLAX", Ninguna),
        new(Rol.AdminFuncional, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Operativo),
        new(Rol.AdminFuncional, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Sede),
        new(Rol.AdminFuncional, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Global),
        new(Rol.AdminFuncional, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Cumplimiento),

        // Auditor (4 celdas)
        new(Rol.Auditor, TipoRecurso.Catalogo, "L", Ninguna),
        new(Rol.Auditor, TipoRecurso.UsuarioRol, "L", Ninguna),
        new(Rol.Auditor, TipoRecurso.Bitacora, "L", Ninguna),
        new(Rol.Auditor, TipoRecurso.Reporte, "L", Ninguna, Alcance: AlcanceReporte.Cumplimiento),

        // Red de apoyo: sin filas (0 celdas)
    ];

    private static Accion LetraAAccion(char letra) => letra switch
    {
        'C' => Accion.Crear,
        'L' => Accion.Leer,
        'A' => Accion.Actualizar,
        'X' => Accion.Anular,
        _ => throw new ArgumentOutOfRangeException(nameof(letra), letra, "Letra de acción desconocida en la tabla esperada.")
    };

    private static IEnumerable<(Esperado Fila, Accion Accion)> Celdas() =>
        Tabla.SelectMany(fila => fila.Acciones.Select(letra => (fila, LetraAAccion(letra))));

    [Fact]
    public void RF_ROL_005_matriz_coincide_con_el_modulo_03_y_las_ampliaciones()
    {
        foreach (var (fila, accion) in Celdas())
        {
            var permiso = MatrizPermisos.Buscar(fila.Rol, fila.Recurso, accion, fila.Alcance);

            Assert.True(permiso is not null, $"Falta la celda {fila.Rol} x {fila.Recurso} x {accion} x {fila.Alcance}.");
            Assert.Equal(fila.Rol, permiso.Rol);
            Assert.Equal(fila.Recurso, permiso.Recurso);
            Assert.Equal(accion, permiso.Accion);
            Assert.Equal(fila.Alcance, permiso.Alcance);
            Assert.Equal(fila.Condiciones, permiso.Condiciones);
            Assert.Equal(fila.Nivel, permiso.Nivel);
        }

        Assert.Equal(88, MatrizPermisos.Todos.Count);
    }

    [Fact]
    public void RF_ROL_005_matriz_deniega_por_defecto_toda_celda_no_listada()
    {
        var listadas = Celdas()
            .Select(c => (c.Fila.Rol, c.Fila.Recurso, c.Accion, c.Fila.Alcance))
            .ToHashSet();
        AlcanceReporte?[] alcances = [null, .. Enum.GetValues<AlcanceReporte>().Cast<AlcanceReporte?>()];

        foreach (var rol in Enum.GetValues<Rol>())
        foreach (var recurso in Enum.GetValues<TipoRecurso>())
        foreach (var accion in Enum.GetValues<Accion>())
        foreach (var alcance in alcances)
        {
            if (listadas.Contains((rol, recurso, accion, alcance)))
                continue;

            Assert.True(
                MatrizPermisos.Buscar(rol, recurso, accion, alcance) is null,
                $"La celda {rol} x {recurso} x {accion} x {alcance} no está en la spec §7 y debe estar denegada.");
        }
    }

    [Fact]
    public void RF_ROL_005_red_de_apoyo_no_tiene_ningun_permiso_en_el_mvp()
    {
        Assert.DoesNotContain(MatrizPermisos.Todos, permiso => permiso.Rol == Rol.RedApoyo);
    }

    [Fact]
    public void RF_ROL_005_valores_de_enum_fuera_de_rango_no_encuentran_celda()
    {
        Assert.Null(MatrizPermisos.Buscar((Rol)99, TipoRecurso.Cita, Accion.Leer));
        Assert.Null(MatrizPermisos.Buscar(Rol.Recepcion, TipoRecurso.Cita, (Accion)99));
        Assert.Null(MatrizPermisos.Buscar(Rol.Recepcion, (TipoRecurso)99, Accion.Leer));
        Assert.Null(MatrizPermisos.Buscar(Rol.Director, TipoRecurso.Reporte, Accion.Leer, (AlcanceReporte)99));
    }
}
