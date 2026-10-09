# Política de autorización (RF-ROL-005) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar, en `Identidad`, la política de autorización del módulo 03 (tramo B): una función pura de dominio que aplica la matriz de permisos y un `Autorizador` que reúne los hechos por puertos y audita toda denegación.

**Architecture:** `PoliticaAcceso` (dominio, sin I/O) evalúa rol → sede → propio → liberado → relación clínica → sensibilidad → habilitación sobre una tabla `MatrizPermisos` en código. `Autorizador` (aplicación) carga solo los hechos necesarios por puertos, llama a la política y registra un evento crítico por cada denegación. Falla cerrada.

**Tech Stack:** .NET 9 (`net9.0`), C# con `Nullable` e `ImplicitUsings`, xUnit 2.9.2, coverlet.collector 6.0.2 (ya referenciados). Sin paquetes nuevos.

**Spec:** `specs/2026-10-09-politica-de-autorizacion-design.md` (los números `§` y `R0`–`R8` de este plan son los de la spec). Léela antes de empezar.

## Global Constraints

- Rama: `claude/exciting-hamilton-ljfxiv` (designada para la sesión). No crear otra rama, no abrir PR. Tras cada commit: `git push -u origin claude/exciting-hamilton-ljfxiv`.
- Solo se tocan `src/Modules/Identidad/`, `tests/Identidad.Tests/` y `specs/`. **No** tocar `docs/`, `src/Shared/`, migraciones ni `src/Continuum.Api/`.
- `Dominio` no referencia `Aplicacion`. Namespaces: `Continuum.Identidad.Dominio.Autorizacion` y `Continuum.Identidad.Aplicacion.Autorizacion`; pruebas en `Continuum.Identidad.Tests.Autorizacion`.
- Estilo del código vecino (`Usuario.cs`, `ServicioInicioSesion.cs`): namespace de archivo, comentarios `///` en español, `CancellationToken ct = default` en métodos asíncronos, constructores primarios.
- Nombres de prueba con el ID del criterio o regla, como `RF_IAM_001_…`: `CA_ROL_001_…`, `RN_001_…`, `RF_ROL_005_…`.
- Solo datos ficticios: GUID generados o constantes inventadas. Nunca contenido clínico en eventos ni logs.
- Códigos de motivo (spec §5), textuales: `OTRA_ORGANIZACION`, `SIN_ROL_EN_SEDE`, `SIN_PERMISO_DE_ROL`, `SEDE_DISTINTA`, `NO_ES_PROPIO`, `NO_LIBERADO`, `SIN_RELACION_CLINICA`, `EPISODIO_SENSIBLE`, `SIN_HABILITACION`, `HABILITACION_VENCIDA`, `DATOS_INSUFICIENTES`.
- Cobertura: reglas de negocio ≥ 90 %, dominio ≥ 70 %.
- Los mensajes de commit terminan con las líneas de atribución que indique la sesión; formato `feat(identidad): … [IDs]` (`CLAUDE.md`).
- **Prerrequisito:** `dotnet --version` debe devolver `9.x`. En el contenedor de planificación no estaba instalado. Si falta: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0` y `export PATH="$HOME/.dotnet:$PATH"`. Si la red lo bloquea, detenerse y avisar. Antes de la Task 1, ejecutar `dotnet build Continuum.sln` y `dotnet test Continuum.sln`: deben pasar. Si no, detenerse y reportar (no se verificó la línea base al escribir el plan).

## Review Focus

Entradas que la spec implica y que pueden romper la política. Cada línea tiene su prueba en la tarea indicada.

1. **Ids nulos a ambos lados de `Propio`.** `null == null` no debe conceder: un profesional sin ficha frente a un contexto sin `ProfesionalId` es `NO_ES_PROPIO` (Task 3).
2. **`Reporte` sin `AlcanceReporte`.** Debe ser `DATOS_INSUFICIENTES`, no concedido ni excepción (Task 3).
3. **Profesional sin ficha en un recurso con relación clínica.** `SIN_RELACION_CLINICA` sin consultar el puerto (Tasks 4 y 6).
4. **Habilitación que no corresponde.** De otra especialidad, con procedimiento fuera de la lista, con lista vacía, o `Anular` una orden con licencia vencida: todo denegado (Task 5).
5. **Valores de enum fuera de rango** (`(Accion)99`, `(Rol)99`): se deniegan sin excepción (Tasks 2 y 3).

---

### Task 1: Tipos de dominio y contrato de decisión

**Files:**
- Create en `src/Modules/Identidad/Dominio/Autorizacion/`: `Rol.cs`, `Accion.cs`, `TipoRecurso.cs`, `AlcanceReporte.cs`, `NivelAcceso.cs`, `MotivoDenegacion.cs`, `Decision.cs`, `Hechos.cs`
- Test: `tests/Identidad.Tests/Autorizacion/DecisionTests.cs`

**Interfaces:**
- Produces (nombres y miembros exactos de la spec §5; el resto del plan los usa tal cual):
  - Enums `Rol`, `Accion`, `TipoRecurso`, `AlcanceReporte`, `NivelAcceso`, `TipoDenegacion`, `MotivoDenegacion`, con los miembros del §5.
  - `static string Codigo(this MotivoDenegacion motivo)` en `MotivoDenegacion.cs`.
  - `sealed record Decision` con `bool EstaPermitido`, `NivelAcceso? Nivel`, `TipoDenegacion? Tipo`, `MotivoDenegacion? Motivo`, constructor privado y fábricas `static Decision Permitir(NivelAcceso)` y `static Decision Denegar(TipoDenegacion, MotivoDenegacion)`. `TipoDenegacion` se declara en `Decision.cs`.
  - En `Hechos.cs`:
    ```csharp
    public sealed record PerfilActor(Guid UsuarioId, Guid OrganizacionId, IReadOnlySet<Rol> Roles, Guid? ProfesionalId);
    public enum TipoRelacion { Ninguna, Equipo, Tratante }
    public sealed record RelacionClinica(TipoRelacion Tipo, bool PermiteSensible, bool EpisodioSensible, Guid EspecialidadId);
    public sealed record HabilitacionProfesional(Guid EspecialidadId, IReadOnlySet<Guid> ProcedimientosPermitidos, DateOnly VigenteHasta);
    public sealed record ContextoRecurso(Guid OrganizacionId, Guid? RecursoId = null, Guid? PacienteId = null,
        Guid? ProfesionalId = null, Guid? EpisodioId = null, Guid? SedeId = null, Guid? ProcedimientoId = null,
        bool? LiberadoAlPaciente = null, AlcanceReporte? AlcanceReporte = null);
    public sealed record HechosAcceso(PerfilActor Actor, Guid SedeActivaId, Guid? PacienteIdDelActor,
        RelacionClinica? Relacion, HabilitacionProfesional? Habilitacion, DateOnly HoyEnSede);
    ```

- [ ] **Step 1: Escribir las pruebas que fallan** en `DecisionTests.cs`:
  - `RF_ROL_005_permitir_expone_el_nivel_y_ningun_motivo`: `Decision.Permitir(NivelAcceso.Resumen)` → `EstaPermitido` true, `Nivel == Resumen`, `Tipo` y `Motivo` nulos.
  - `RF_ROL_005_denegar_expone_tipo_y_motivo_y_ningun_nivel`: `Decision.Denegar(TipoDenegacion.NoEncontrado, MotivoDenegacion.OtraOrganizacion)` → `EstaPermitido` false, `Nivel` nulo.
  - `RF_ROL_005_decisiones_con_los_mismos_valores_son_iguales`: `Assert.Equal` entre dos `Denegar` idénticos y `Assert.NotEqual(Permitir(Completo), Permitir(Agregado))`.
  - `[Theory] RF_ROL_005_codigo_de_auditoria_de_cada_motivo`: un `InlineData` por cada uno de los 11 motivos con el código textual de las restricciones globales; `Assert.Equal(esperado, motivo.Codigo())`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~Autorizacion"`. Expected: error de compilación `CS0246` (tipos inexistentes).
- [ ] **Step 3: Implementar** los tipos en los archivos listados. `Decision` es un `record` con constructor privado; las propiedades se fijan solo desde las fábricas.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: 4 pruebas (14 casos) PASS.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Dominio/Autorizacion tests/Identidad.Tests/Autorizacion
  git commit -m "feat(identidad): tipos y decisión de la política de autorización [RF-ROL-005]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 2: `MatrizPermisos`, la tabla del módulo 03

**Files:**
- Create: `src/Modules/Identidad/Dominio/Autorizacion/MatrizPermisos.cs`
- Test: `tests/Identidad.Tests/Autorizacion/MatrizPermisosTests.cs`

**Interfaces:**
- Consumes: enums de la Task 1.
- Produces:
  ```csharp
  [Flags] public enum Condiciones { Ninguna = 0, Propio = 1, MismaSede = 2, Liberado = 4, RelacionClinica = 8 }
  public sealed record Permiso(Rol Rol, TipoRecurso Recurso, Accion Accion, AlcanceReporte? Alcance,
                               Condiciones Condiciones, NivelAcceso Nivel);
  public static class MatrizPermisos {
      public static IReadOnlyList<Permiso> Todos { get; }
      public static Permiso? Buscar(Rol rol, TipoRecurso recurso, Accion accion, AlcanceReporte? alcance = null);
  }
  ```
  `Buscar` compara `alcance` exactamente (las entradas que no son `Reporte` tienen `Alcance == null`) y devuelve `null` si no hay celda.

- [ ] **Step 1: Escribir las pruebas que fallan.** La tabla esperada se **teclea a mano** desde la spec §7 (cuadrícula y tabla de reportes), nunca generada desde `MatrizPermisos`. Codificación:
  ```csharp
  private sealed record Esperado(Rol Rol, TipoRecurso Recurso, string Acciones /* C,L,A,X */,
      Condiciones Condiciones, NivelAcceso Nivel = NivelAcceso.Completo, AlcanceReporte? Alcance = null);
  // ejemplos (transcribir las 88 celdas de §7: Paciente 8, Profesional 19, Asistente 10,
  // Recepción 10, Coordinador 14, Director 8, Admin 15, Auditor 4, RedApoyo 0):
  new(Rol.Recepcion, TipoRecurso.Cita, "CLAX", Condiciones.MismaSede),
  new(Rol.AsistenteClinico, TipoRecurso.NotaClinica, "L", Condiciones.RelacionClinica, NivelAcceso.PorSeccion),
  new(Rol.Director, TipoRecurso.Reporte, "L", Condiciones.Ninguna, NivelAcceso.Agregado, AlcanceReporte.Global),
  ```
  Pruebas:
  - `RF_ROL_005_matriz_coincide_con_el_modulo_03_y_las_ampliaciones`: por cada `Esperado` y cada letra de `Acciones`, `Buscar` devuelve un `Permiso` con las mismas `Condiciones`, `Nivel` y `Alcance`. Además `Assert.Equal(88, MatrizPermisos.Todos.Count)`.
  - `RF_ROL_005_matriz_deniega_por_defecto_toda_celda_no_listada`: recorre `Rol × TipoRecurso × Accion × (null + cada AlcanceReporte)`; toda combinación que no esté en `Esperado` devuelve `null`.
  - `RF_ROL_005_red_de_apoyo_no_tiene_ningun_permiso_en_el_mvp`: ningún `Permiso` con `Rol.RedApoyo`.
  - `RF_ROL_005_valores_de_enum_fuera_de_rango_no_encuentran_celda`: `Buscar((Rol)99, …)`, `Buscar(…, (Accion)99)` y `Buscar(…, (TipoRecurso)99)` devuelven `null` sin excepción.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~MatrizPermisosTests"`. Expected: error de compilación (`MatrizPermisos` no existe).
- [ ] **Step 3: Implementar `MatrizPermisos`** con la lista `Todos` construida en el código (una entrada por rol × recurso × acción × alcance) y `Buscar` con un diccionario indexado por la clave `(Rol, TipoRecurso, Accion, AlcanceReporte?)`. Los valores salen de la spec §7; `[T]` es `Condiciones.RelacionClinica`, `[S]` es `MismaSede`, `[P]` es `Propio`, `[Lib]` es `Liberado`.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS (4 pruebas).
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Dominio/Autorizacion/MatrizPermisos.cs tests/Identidad.Tests/Autorizacion/MatrizPermisosTests.cs
  git commit -m "feat(identidad): matriz de permisos del módulo 03 en código [RF-ROL-005, RN-002]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 3: `PoliticaAcceso`, reglas R0–R5 (organización, rol, sede, propio, liberado)

**Files:**
- Create: `src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs`
- Test: `tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs`

**Interfaces:**
- Consumes: tipos de la Task 1, `MatrizPermisos.Buscar` de la Task 2.
- Produces: `public static class PoliticaAcceso { public static Decision Evaluar(Accion accion, TipoRecurso recurso, ContextoRecurso contexto, HechosAcceso hechos); }`

**Reglas que esta tarea implementa (spec §6):**
- Orden: R0 (`Actor.Roles` vacío → `Denegar(Prohibido, SinRolEnSede)`) → R1 (`contexto.OrganizacionId != Actor.OrganizacionId` → `Denegar(NoEncontrado, OtraOrganizacion)`) → si el recurso es `Reporte` y `contexto.AlcanceReporte` es nulo → `Denegar(Prohibido, DatosInsuficientes)` → evaluación por rol.
- Por cada rol: R2 celda (`Buscar`; sin celda → `SinPermisoDeRol`) → R3 `MismaSede` (`contexto.SedeId` nulo → `DatosInsuficientes`; distinto de `SedeActivaId` → `SedeDistinta`) → R4 `Propio` → R5 `Liberado` (`LiberadoAlPaciente != true` → `NoLiberado`).
- `Propio`: rol `Paciente` compara `contexto.PacienteId` con `hechos.PacienteIdDelActor`; rol `Profesional` compara `contexto.ProfesionalId` con `hechos.Actor.ProfesionalId`. **Ambos lados deben ser no nulos**; si no, `NoEsPropio`.
- Varios roles: gana el resultado más permisivo (`Completo` > `PorSeccion` > `Resumen` > `Agregado`). Si ninguno concede, el motivo es el de la etapa más profunda alcanzada (R2 < R3 < R4 < R5 < R6 < R7 < R8); empate: el rol de menor valor del enum `Rol`.
- **Provisional:** una celda con `Condiciones.RelacionClinica` se deniega con `SinRelacionClinica` hasta que la Task 4 implemente R6.
- Todas las denegaciones por rol o atributo son `TipoDenegacion.Prohibido`.

- [ ] **Step 1: Escribir las pruebas que fallan** en `PoliticaAccesoTests.cs`. Helpers privados (tanto la Task 4 como la 5 los reutilizan):
  ```csharp
  private static readonly Guid Org = Guid.Parse("00000000-0000-0000-0000-0000000000a1"), Sede = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
  private static HechosAcceso Hechos(Rol[] roles, Guid? profesionalId = null, Guid? pacienteDelActor = null,
      RelacionClinica? relacion = null, HabilitacionProfesional? hab = null, DateOnly? hoy = null, Guid? sedeActiva = null, Guid? org = null);
  private static ContextoRecurso Ctx(Guid? org = null, Guid? paciente = null, Guid? profesional = null, Guid? episodio = null,
      Guid? sede = null, bool? liberado = null, AlcanceReporte? alcance = null, Guid? procedimiento = null);  // org por defecto = Org
  ```
  Pruebas (todas con `PoliticaAcceso.Evaluar`):
  - `CA_ROL_001_recepcion_ve_demograficos_y_citas_pero_no_notas_diagnosticos_ni_ordenes`: `Leer` `DatosDemograficos` → `Permitir(Completo)`; `Leer` `Cita` con `sede: Sede` → `Permitir(Completo)`; `Leer` `NotaClinica` y `Leer` `Orden` → `Denegar(Prohibido, SinPermisoDeRol)`.
  - `RN_015_recurso_de_otra_organizacion_responde_no_encontrado`: contexto con otra `org` → `Denegar(NoEncontrado, OtraOrganizacion)`, también para `DatosDemograficos`.
  - `RN_002_actor_sin_roles_en_la_sede_es_prohibido`: `Roles` vacío → `Denegar(Prohibido, SinRolEnSede)`.
  - `RN_002_director_ve_citas_solo_agregado_y_no_ve_datos_demograficos`: `Cita`/`Leer` → `Permitir(Agregado)`; `DatosDemograficos`/`Leer` → `SinPermisoDeRol`.
  - `RN_002_recepcion_no_actua_sobre_citas_de_otra_sede`: `SedeDistinta`; con `sede` nula en el contexto → `DatosInsuficientes`; `AdminFuncional` lee citas de cualquier sede → `Permitir(Completo)`.
  - `RN_002_paciente_lee_y_actualiza_solo_sus_datos_demograficos`: propio → `Completo`; otro `PacienteId` → `NoEsPropio`.
  - `RN_002_profesional_gestiona_solo_su_propia_agenda`: propio y de la misma sede → `Completo`; otro `ProfesionalId` → `NoEsPropio`.
  - `RF_ROL_005_propio_con_ids_nulos_en_ambos_lados_no_concede` *(Review Focus 1)*: profesional con `ProfesionalId` nulo y contexto sin `ProfesionalId` → `NoEsPropio`; paciente sin vínculo y contexto sin `PacienteId` → `NoEsPropio`.
  - `RF_ROL_005_paciente_lee_solo_ordenes_liberadas`: `liberado: true` → `Completo`; `false` y `null` → `NoLiberado`.
  - `RF_ROL_005_reporte_sin_alcance_es_datos_insuficientes` *(Review Focus 2)*; `profesional_lee_solo_reporte_propio` (alcance `Operativo` → `SinPermisoDeRol`); `director_lee_reportes_agregados`.
  - `RF_ROL_005_varios_roles_gana_el_mas_permisivo`: Director + Profesional sobre su propia `Cita` → `Completo`; sobre una ajena → `Agregado`.
  - `RF_ROL_005_varios_roles_sin_concesion_devuelve_el_motivo_mas_profundo`: Recepción + Paciente leyendo una cita de otra sede de otro paciente → `NoEsPropio` (R4 es más profundo que R3).
  - `RF_ROL_005_valores_de_enum_fuera_de_rango_se_deniegan_sin_excepcion` *(Review Focus 5)*: `(Accion)99` y `(TipoRecurso)99` → `Denegar(Prohibido, SinPermisoDeRol)`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~PoliticaAccesoTests"`. Expected: error de compilación (`PoliticaAcceso` no existe).
- [ ] **Step 3: Implementar `PoliticaAcceso.Evaluar`** con las reglas de arriba. Un método privado evalúa un rol y devuelve la decisión junto con la etapa alcanzada; otro combina los resultados de todos los roles.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs
  git commit -m "feat(identidad): política de acceso por rol, sede, propiedad y liberación [RF-ROL-005, RN-002, RN-015, CA-ROL-001]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 4: Relación clínica y sensibilidad (R6–R7)

**Files:**
- Modify: `src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs`
- Test: `tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs`

**Interfaces:**
- Consumes: `Evaluar` de la Task 3 y sus helpers de prueba.
- Produces: `public static bool EsDeEpisodio(TipoRecurso recurso)` en `PoliticaAcceso`, verdadero para `NotaClinica`, `Orden` y `SignosVitalesTriaje`. Reemplaza el rechazo provisional de la Task 3.

**Reglas (spec §6, R6 y R7):**
- R6, solo para celdas con `Condiciones.RelacionClinica`: `Actor.ProfesionalId` nulo → `SinRelacionClinica`; recurso de episodio con `contexto.EpisodioId` nulo, o `hechos.Relacion` nula → `DatosInsuficientes`; `Relacion.Tipo == Ninguna` → `SinRelacionClinica`.
- R7, solo si `EsDeEpisodio(recurso)` y `Relacion.EpisodioSensible`:

  | Quién | Resultado |
  |---|---|
  | rol `AsistenteClinico` | `EpisodioSensible` (cualquier acción) |
  | `Tratante`, o `Equipo` con `PermiteSensible` | sigue el nivel de la celda |
  | `Equipo` sin `PermiteSensible` | `NotaClinica`+`Leer` → `Resumen`; todo lo demás → `EpisodioSensible` |

  En episodios no sensibles, `PermiteSensible` no cambia nada. `ResumenSeguridad` no se filtra por sensibilidad (spec §12, I8).

- [ ] **Step 1: Escribir las pruebas que fallan** (nuevos hechos con `RelacionClinica(TipoRelacion.X, permite, sensible, especialidad)`):
  - `RN_001_tratante_lee_la_nota_de_su_episodio_completa`; `RN_001_miembro_del_equipo_lee_nota_no_sensible_completa`; `RN_001_profesional_sin_relacion_no_lee_la_nota` (`Ninguna` → `Denegar(Prohibido, SinRelacionClinica)`).
  - `CA_ROL_002_cardiologo_ajeno_ve_demograficos_pero_no_notas_de_psicologia`: mismo actor, `DatosDemograficos`/`Leer` → `Completo`; `NotaClinica`/`Leer` con `Relacion` `Ninguna` → `SinRelacionClinica`.
  - `RN_016_equipo_sin_permiso_en_episodio_sensible_ve_el_resumen_de_la_nota` → `Permitir(Resumen)`.
  - `RN_016_equipo_sin_permiso_no_crea_notas_ni_lee_ordenes_en_episodio_sensible` → `Denegar(Prohibido, EpisodioSensible)` en `NotaClinica`/`Crear`, `Orden`/`Leer` y `SignosVitalesTriaje`/`Leer`.
  - `RN_016_tratante_y_equipo_con_permiso_ven_completo_en_episodio_sensible`.
  - `RN_016_el_permiso_sensible_no_cambia_nada_en_episodio_no_sensible`.
  - `RN_016_asistente_lee_la_nota_por_seccion_en_episodio_no_sensible` → `Permitir(PorSeccion)`; `asistente_registra_signos_vitales_de_su_episodio` → `Completo`; `asistente_no_accede_a_episodios_sensibles` → `EpisodioSensible` en `NotaClinica`/`Leer` y `SignosVitalesTriaje`/`Crear`.
  - `RF_ROL_005_profesional_sin_ficha_en_recurso_con_relacion_es_sin_relacion_clinica` *(Review Focus 3)*: `ProfesionalId` nulo y `Relacion` `Tratante` → `SinRelacionClinica`.
  - `RF_ROL_005_recurso_de_episodio_sin_episodio_o_con_relacion_nula_es_datos_insuficientes`.
  - `RN_016_resumen_de_seguridad_no_se_filtra_por_sensibilidad`: `Relacion` `Equipo` sin permiso, `EpisodioSensible: true`, `ResumenSeguridad`/`Leer` → `Permitir(Completo)`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~PoliticaAccesoTests"`. Expected: FAIL (las celdas con relación siguen denegadas por el rechazo provisional).
- [ ] **Step 3: Implementar R6 y R7** en `PoliticaAcceso.cs`, y `EsDeEpisodio`. Quitar el rechazo provisional.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS, incluidas las de la Task 3.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs
  git commit -m "feat(identidad): relación clínica y episodios sensibles en la política de acceso [RF-ROL-005, RN-001, RN-016, CA-ROL-002]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 5: Habilitación profesional (R8)

**Files:**
- Modify: `src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs`
- Test: `tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs`

**Interfaces:**
- Consumes: `Evaluar` (Tasks 3–4); `HechosAcceso.Habilitacion`, `HechosAcceso.HoyEnSede`.
- Produces: R8 integrada en `Evaluar`; sin firmas nuevas.

**Regla (spec §6, R8):** aplica solo al rol `Profesional` cuando la acción es `Crear`, `Actualizar` o `Anular` sobre `NotaClinica` u `Orden`, y después de pasar R6–R7. Orden de comprobación: `Habilitacion` nula → `SinHabilitacion`; `Habilitacion.EspecialidadId != Relacion.EspecialidadId` → `SinHabilitacion`; `contexto.ProcedimientoId` informado y fuera de `ProcedimientosPermitidos` → `SinHabilitacion`; `HoyEnSede > VigenteHasta` → `HabilitacionVencida`. Las lecturas y los demás roles no se ven afectados. Todos son `Prohibido`.

- [ ] **Step 1: Escribir las pruebas que fallan** (profesional `Tratante`, `hoy = new DateOnly(2026, 10, 9)`):
  - `CA_ROL_003_licencia_vencida_ayer_impide_firmar_nota_con_motivo`: `NotaClinica`/`Crear` (firmar es `Crear`, spec §12 I3) con `VigenteHasta = 2026-10-08` → `Denegar(Prohibido, HabilitacionVencida)`.
  - `CA_ROL_003_licencia_que_vence_hoy_sigue_vigente`: `VigenteHasta = 2026-10-09` → `Permitir(Completo)`.
  - `RN_012_sin_habilitacion_en_la_especialidad`: `Habilitacion` nula → `SinHabilitacion`.
  - `RN_012_habilitacion_de_otra_especialidad_no_sirve` *(Review Focus 4)*, `RN_012_procedimiento_fuera_de_la_lista_se_deniega`, `RN_012_lista_vacia_con_procedimiento_informado_se_deniega`, `RN_012_sin_procedimiento_en_el_contexto_solo_valida_especialidad_y_vigencia` → `Permitir(Completo)`.
  - `RN_012_sin_habilitacion_se_evalua_antes_que_el_vencimiento`: habilitación vencida y procedimiento fuera de lista → `SinHabilitacion`.
  - `RN_012_anular_una_orden_con_licencia_vencida_se_deniega` *(Review Focus 4)*; `RN_012_crear_orden_con_licencia_vencida_se_deniega`.
  - `RN_012_la_lectura_no_exige_habilitacion`: `NotaClinica`/`Leer` con habilitación vencida → `Completo`.
  - `RN_012_el_asistente_no_necesita_habilitacion`: `SignosVitalesTriaje`/`Crear` con `Habilitacion` nula → `Completo`.
  - `RN_012_sin_relacion_clinica_se_deniega_antes_que_por_habilitacion`: `Relacion` `Ninguna` y habilitación vencida → `SinRelacionClinica`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~PoliticaAccesoTests"`. Expected: FAIL (las escrituras se conceden sin R8).
- [ ] **Step 3: Implementar R8** en `PoliticaAcceso.cs` con el orden de comprobación descrito.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs tests/Identidad.Tests/Autorizacion/PoliticaAccesoTests.cs
  git commit -m "feat(identidad): habilitación vigente para escrituras clínicas [RF-ROL-005, RN-012, CA-ROL-003]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 6: Puertos, contrato y `Autorizador` con carga perezosa

**Files:**
- Create en `src/Modules/Identidad/Aplicacion/Autorizacion/`: `Puertos.cs`, `IAutorizador.cs`, `Autorizador.cs`
- Modify: `src/Modules/Identidad/Dominio/Autorizacion/PoliticaAcceso.cs` (añadir `Necesidades`)
- Test: `tests/Identidad.Tests/Autorizacion/AutorizadorTests.cs`, `tests/Identidad.Tests/Autorizacion/DoblesAutorizacion.cs`, más pruebas de `Necesidades` en `PoliticaAccesoTests.cs`

**Interfaces:**
- Consumes: `PoliticaAcceso.Evaluar`/`EsDeEpisodio`; `IReloj` de `Continuum.Identidad.Aplicacion` (`DateTimeOffset Ahora`); `RelojFalso` de `tests/Identidad.Tests/Dobles.cs` (`Ahora` por defecto 2026-10-09 12:00 UTC).
- Produces:
  ```csharp
  // Dominio — PoliticaAcceso.cs
  public sealed record NecesidadesHechos(bool VinculoPaciente, bool Relacion, bool Habilitacion);
  public static NecesidadesHechos Necesidades(Accion accion, TipoRecurso recurso, IReadOnlySet<Rol> roles, AlcanceReporte? alcance = null);

  // Aplicación — Puertos.cs (firmas de la spec §8)
  public interface IRolesPorSede { Task<PerfilActor?> ObtenerPerfilAsync(Guid usuarioId, Guid sedeId, CancellationToken ct = default); }
  public interface IVinculoPaciente { Task<Guid?> ObtenerPacienteIdAsync(Guid usuarioId, CancellationToken ct = default); }
  public interface IRelacionClinica { Task<RelacionClinica?> ObtenerAsync(Guid profesionalId, Guid pacienteId, Guid? episodioId, DateTimeOffset ahora, CancellationToken ct = default); }
  public interface IHabilitaciones { Task<HabilitacionProfesional?> ObtenerAsync(Guid profesionalId, Guid especialidadId, CancellationToken ct = default); }
  public interface IZonaHorariaSede { Task<TimeZoneInfo> ObtenerAsync(Guid sedeId, CancellationToken ct = default); }
  public interface IAuditoriaAutorizacion { Task RegistrarDenegacionAsync(EventoAutorizacion evento, CancellationToken ct = default); }
  public sealed record EventoAutorizacion(DateTimeOffset OcurridoEn, Guid UsuarioId, Guid? OrganizacionId, Guid SedeId,
      IReadOnlyCollection<Rol> Roles, Accion Accion, TipoRecurso Recurso, Guid? RecursoId, Guid? PacienteId,
      TipoDenegacion Tipo, MotivoDenegacion Motivo);

  // Aplicación — IAutorizador.cs / Autorizador.cs
  public sealed record SolicitudAcceso(Guid UsuarioId, Guid SedeActivaId, Accion Accion, TipoRecurso Recurso, ContextoRecurso Contexto);
  public interface IAutorizador { Task<Decision> AutorizarAsync(SolicitudAcceso solicitud, CancellationToken ct = default); }
  public sealed class Autorizador(IRolesPorSede roles, IVinculoPaciente vinculo, IRelacionClinica relacion,
      IHabilitaciones habilitaciones, IZonaHorariaSede zonas, IReloj reloj, IAuditoriaAutorizacion auditoria) : IAutorizador;
  ```
- Dobles en `DoblesAutorizacion.cs` (internos, uno por puerto, estilo de `Dobles.cs`): `RolesPorSedeFalso`, `VinculoPacienteFalso`, `RelacionClinicaFalsa`, `HabilitacionesFalsas`, `ZonaHorariaSedeFalsa`, `AuditoriaAutorizacionFalsa`. Cada uno expone el valor que devuelve, un contador `Llamadas`, el último `CancellationToken` y un `Exception? Fallo` que se lanza si está asignado. `RelacionClinicaFalsa` guarda además los argumentos de la última llamada; `AuditoriaAutorizacionFalsa` guarda `List<EventoAutorizacion> Eventos`.

**Flujo de `AutorizarAsync`:**
1. `perfil = await roles.ObtenerPerfilAsync(...)`. Si es `null` → `Denegar(Prohibido, SinRolEnSede)` y se audita con `OrganizacionId` nula y `Roles` vacío.
2. Si `Contexto.OrganizacionId != perfil.OrganizacionId`, no se cargan más hechos y se evalúa solo con el perfil (R1).
3. En otro caso: `Necesidades(...)`; `IVinculoPaciente` solo si `VinculoPaciente`; `IRelacionClinica` solo si `Relacion` y `perfil.ProfesionalId` y `Contexto.PacienteId` no son nulos y, para recursos de episodio, `Contexto.EpisodioId` no es nulo (pasa `reloj.Ahora`); `IHabilitaciones` y `IZonaHorariaSede(SedeActivaId)` solo si `Habilitacion` y la relación se cargó. `HoyEnSede` = fecha local de `reloj.Ahora` en esa zona; sin habilitación necesaria se usa `DateOnly.FromDateTime(reloj.Ahora.UtcDateTime)`.
4. `PoliticaAcceso.Evaluar`. Si es `Denegado`, auditar (la auditoría se detalla en la Task 7).

- [ ] **Step 1: Escribir las pruebas que fallan.**
  `PoliticaAccesoTests` (`Necesidades`): `Recepcion`+`DatosDemograficos`/`Leer` → las tres en `false`; `Paciente`+`DatosDemograficos`/`Leer` → solo `VinculoPaciente`; `Profesional`+`NotaClinica`/`Leer` → solo `Relacion`; `Profesional`+`NotaClinica`/`Crear` → `Relacion` y `Habilitacion`.
  `AutorizadorTests`:
  - `RF_ROL_005_leer_datos_demograficos_no_consulta_vinculo_relacion_ni_habilitacion`: `Llamadas == 0` en los tres dobles.
  - `RF_ROL_005_recurso_de_otra_organizacion_no_carga_mas_hechos_que_el_perfil`: `Denegar(NoEncontrado, OtraOrganizacion)` y cero llamadas a vínculo, relación y habilitaciones.
  - `RF_ROL_005_perfil_nulo_o_sin_roles_es_sin_rol_en_sede`.
  - `RF_ROL_005_pasa_ahora_del_reloj_y_los_ids_a_la_relacion_clinica`: la relación recibe `reloj.Ahora`, el `ProfesionalId` del perfil, `PacienteId` y `EpisodioId` del contexto, y el `CancellationToken` de la llamada.
  - `RF_ROL_005_profesional_sin_ficha_no_consulta_la_relacion` *(Review Focus 3)*: `SinRelacionClinica` y `Llamadas == 0`.
  - `RF_ROL_005_recurso_de_episodio_sin_episodio_no_consulta_la_relacion_y_es_datos_insuficientes`.
  - `RF_ROL_005_relacion_nula_es_datos_insuficientes`; `RF_ROL_005_habilitacion_nula_es_sin_habilitacion`; `RF_ROL_005_vinculo_nulo_es_no_es_propio`.
  - `RN_012_hoy_se_calcula_en_la_zona_horaria_de_la_sede`: con `Ahora = 2026-10-09T02:00:00Z` y `VigenteHasta = 2026-10-08`, una zona fija `TimeZoneInfo.CreateCustomTimeZone("sede", TimeSpan.FromHours(-4), "sede", "sede")` → `Permitir(Completo)`; con `TimeZoneInfo.Utc` → `HabilitacionVencida`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~Autorizacion"`. Expected: error de compilación (puertos y `Autorizador` no existen).
- [ ] **Step 3: Implementar** `Necesidades`, los puertos, el contrato y `Autorizador` siguiendo el flujo de arriba. Por ahora `Autorizador` aún no audita (lo hace la Task 7); la rama `Denegado` solo devuelve la decisión.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad tests/Identidad.Tests/Autorizacion
  git commit -m "feat(identidad): autorizador con puertos y carga perezosa de hechos [RF-ROL-005, RN-012]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 7: Auditoría de denegaciones y falla cerrada

**Files:**
- Modify: `src/Modules/Identidad/Aplicacion/Autorizacion/Autorizador.cs`
- Test: `tests/Identidad.Tests/Autorizacion/AutorizadorTests.cs`

**Interfaces:**
- Consumes: `IAuditoriaAutorizacion`, `EventoAutorizacion`, `AuditoriaAutorizacionFalsa` y los demás dobles de la Task 6.
- Produces: `Autorizador` registra exactamente un `EventoAutorizacion` por cada `Decision` denegada, antes de devolverla; ninguno si está permitida.

**Reglas (spec §9–§10):** `OcurridoEn = reloj.Ahora`; `UsuarioId`, `SedeId` y `Roles` salen de la solicitud y del perfil; `OrganizacionId` es la del perfil (nula si no hay perfil); `RecursoId` y `PacienteId` salen del contexto; `Tipo` y `Motivo` de la decisión. Una excepción de cualquier puerto, o de `RegistrarDenegacionAsync`, se propaga sin capturarla.

- [ ] **Step 1: Escribir las pruebas que fallan:**
  - `CA_AUD_003_recepcion_abre_nota_por_url_directa_genera_evento_denegado_critico`: Recepción, `NotaClinica`/`Leer` → `Denegar(Prohibido, SinPermisoDeRol)`; exactamente 1 evento con `UsuarioId`, `SedeId`, `Roles == [Recepcion]`, `Accion.Leer`, `TipoRecurso.NotaClinica`, `RecursoId`, `PacienteId`, `Tipo`, `Motivo` y `OcurridoEn == reloj.Ahora`.
  - `RF_ROL_005_acceso_permitido_no_genera_evento`.
  - `RN_015_otra_organizacion_genera_evento_con_motivo_otra_organizacion` y `OrganizacionId` igual a la del actor.
  - `RF_ROL_005_perfil_nulo_genera_evento_con_organizacion_nula`.
  - `RF_ROL_005_el_evento_de_autorizacion_no_tiene_texto_libre`: por reflexión, ninguna propiedad pública de `EventoAutorizacion` es de tipo `string`.
  - `RF_ROL_005_si_un_puerto_lanza_excepcion_se_propaga_y_no_se_concede_nada`: `RelacionClinicaFalsa.Fallo = new InvalidOperationException()` → `Assert.ThrowsAsync<InvalidOperationException>`; la auditoría queda sin eventos.
  - `RF_ROL_005_si_falla_el_registro_de_la_denegacion_la_excepcion_se_propaga`: `AuditoriaAutorizacionFalsa.Fallo` asignado y solicitud denegada → `ThrowsAsync`.
- [ ] **Step 2: Verificar que falla.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~AutorizadorTests"`. Expected: FAIL (no se registra ningún evento).
- [ ] **Step 3: Implementar el registro** en la rama `Denegado` de `AutorizarAsync`, incluida la del perfil nulo.
- [ ] **Step 4: Verificar que pasa.** Mismo comando. Expected: PASS.
- [ ] **Step 5: Commit.**
  ```bash
  git add src/Modules/Identidad/Aplicacion/Autorizacion/Autorizador.cs tests/Identidad.Tests/Autorizacion/AutorizadorTests.cs
  git commit -m "feat(identidad): auditoría de denegaciones y falla cerrada [RF-ROL-005, CA-AUD-003]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

### Task 8: Matriz mínima de endpoints clínicos y verificación final

**Files:**
- Test: `tests/Identidad.Tests/Autorizacion/AutorizadorTests.cs`

**Interfaces:**
- Consumes: `Autorizador` completo y sus dobles (Tasks 6–7).
- Produces: nada nuevo; cierra la cobertura de la spec §11.

- [ ] **Step 1: Escribir las pruebas** (`NotaClinica`/`Leer` a través de `Autorizador`; las seis filas de `CLAUDE.md` salvo el 401, que es del adaptador):
  - `RN_001_tratante_lee_la_nota_completa` → `Permitir(Completo)`.
  - `RN_001_equipo_en_episodio_no_sensible_lee_la_nota_completa` → `Permitir(Completo)`.
  - `RN_016_equipo_sin_permiso_en_episodio_sensible_lee_el_resumen` → `Permitir(Resumen)`.
  - `RN_001_profesional_ajeno_se_deniega_con_sin_relacion_clinica`.
  - `RN_002_recepcion_se_deniega_con_sin_permiso_de_rol`.
  - `RN_015_otra_organizacion_responde_no_encontrado`.
- [ ] **Step 2: Ejecutar.** Run: `dotnet test tests/Identidad.Tests --filter "FullyQualifiedName~AutorizadorTests"`. Expected: PASS de inmediato (la implementación ya existe). Si alguna falla, es un defecto de las Tasks 3–7: corregirlo allí, con su propia prueba, no aquí.
- [ ] **Step 3: Verificación de toda la solución.** Run: `dotnet build Continuum.sln` → Expected: 0 errores. Run: `dotnet test Continuum.sln` → Expected: todas PASS, incluidas las de inicio de sesión y contraseña previas (no regresiones).
- [ ] **Step 4: Cobertura.** Run: `dotnet test tests/Identidad.Tests --collect:"XPlat Code Coverage" --results-directory <carpeta temporal fuera del repo>`; luego `grep -o 'class name="Continuum.Identidad[^"]*Autorizacion[^"]*" filename="[^"]*" line-rate="[0-9.]*"' <carpeta>/*/coverage.cobertura.xml`. Expected: `PoliticaAcceso`, `MatrizPermisos` y `Autorizador` con `line-rate` ≥ 0.90; el resto del dominio de autorización ≥ 0.70. Si no llegan, añadir pruebas de las ramas no cubiertas, nombradas con la regla que ejercitan.
- [ ] **Step 5: Comprobar el alcance.** Run: `git diff --stat origin/main...HEAD -- . ':!specs'`. Expected: solo rutas bajo `src/Modules/Identidad/` y `tests/Identidad.Tests/`; ninguna bajo `docs/`, `src/Shared/` ni `src/Continuum.Api/`.
- [ ] **Step 6: Commit.**
  ```bash
  git add tests/Identidad.Tests/Autorizacion/AutorizadorTests.cs
  git commit -m "test(identidad): matriz mínima de endpoints clínicos del autorizador [RF-ROL-005, RN-001, RN-002, RN-015, RN-016]"
  git push -u origin claude/exciting-hamilton-ljfxiv
  ```

---

## Fuera de este plan

Adaptador HTTP, persistencia de roles y habilitaciones, implementaciones reales de los puertos, `RF-ROL-001` a `004` y `006`, y la solicitud de cambio `SC-001` (su borrador está en `specs/` y sigue `docs/GUIA-DE-ACTUALIZACION.md`). La migración del indicador `permite_sensible` es de Dev B y depende de que `SC-001` se apruebe.
