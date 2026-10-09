# Política de autorización — diseño (módulo 03, tramo B)

| | |
|---|---|
| **Fecha** | 2026-10-09 |
| **Módulo** | 03 — Actores, roles y permisos (tramo B; el tramo A, usuarios/roles/habilitaciones, va después) |
| **Dueño** | Dev A — Seguridad y clínico (`src/Modules/Identidad`) |
| **Estado** | Borrador para revisión |
| **Requisitos** | `RF-ROL-005` (núcleo). Parte de política de `RF-ROL-004`: bloquear escrituras con habilitación vencida. |
| **Criterios** | `CA-ROL-001`, `CA-ROL-002`, `CA-ROL-003`, `CA-AUD-003`. Fuera: `CA-ROL-004` (acceso de emergencia, fuera del MVP). |
| **Reglas** | `RN-001`, `RN-002`, `RN-012`, `RN-015`, `RN-016` (acceso a categorías sensibles) |

> Esta spec no modifica `docs/`. Los cambios de documentación que exige (modelo de datos, módulo 12, matriz del módulo 03) van en la solicitud de cambio `SC-002` (borrador en `specs/SC-002-borrador-matriz-y-equipo-de-atencion.md`), que sigue `docs/GUIA-DE-ACTUALIZACION.md`.

## 1. Objetivo

Decidir, en el backend y en cada solicitud, si un usuario puede ejecutar una acción sobre un recurso, combinando **rol, sede, habilitación y relación con el paciente** (`RF-ROL-005`, `RN-001`, `RN-002`). La decisión no es solo sí o no: indica **cuánto** puede ver (nivel de acceso). Toda denegación queda auditada como evento crítico.

## 2. Alcance

**Dentro:**
- La matriz del módulo 03 §4 codificada como tabla en código, con una prueba por celda.
- `PoliticaAcceso`: función pura de dominio que evalúa la solicitud con los hechos ya resueltos.
- `Autorizador` (`IAutorizador`): reúne los hechos por puertos, llama a la política y audita las denegaciones.
- Los puertos como interfaces y sus dobles de prueba.

**Fuera (quedan para otros tramos):**
- Adaptador HTTP (`IAuthorizationHandler` o filtro de endpoint) y su cableado. Espera a que el login emita tokens (`RF-IAM-002` y `RF-IAM-006`).
- Persistencia de `rol_asignado`, `profesional` y `habilitacion`, y gestión de usuarios (`RF-ROL-001` a `004`, tramo A).
- Implementación real de los puertos (la hace cada módulo dueño del dato).
- Equipo de atención (`RF-ROL-006`): necesita `HistoriaClinica`.
- Roles personalizados y acceso de emergencia (`RF-ROL-007` y `008`): fuera del MVP.

## 3. Decisiones tomadas

| # | Decisión | Consecuencia |
|---|---|---|
| D1 | Se hace primero B (política) y después A | La política consulta por puertos hechos que aún no existen y se prueba con dobles. |
| D2 | Enfoque 1: núcleo de dominio puro + adaptadores finos | Sin dependencia de ASP.NET en el núcleo; reutilizable desde workers y reportes. |
| D3 | El permiso explícito del tratante sobre episodios sensibles es un **indicador en `miembro_equipo`** | `SC-002`: campo nuevo en el módulo 05 y migración de Dev B. |
| D4 | Asistente clínico = «solo lo necesario» + nota de la especialidad del episodio sin diagnósticos, **nunca en episodios sensibles** | `SC-002`: indicador de visibilidad por sección en las plantillas (módulo 12, ADR-006). |
| D5 | El contrato público vive en `Continuum.Identidad`, no en `Shared` | `Shared` es contrato protegido; no se toca en este PR. |
| D6 | Supuestos aprobados: (a) el miembro del equipo sin permiso no crea notas en episodios sensibles; (b) `RED_APOYO` denegado en todo en el MVP; (c) la habilitación solo condiciona escrituras; (d) el paciente solo tiene `Leer (P)` en citas | Ver §7 y `SC-002`. |
| D7 | `vigente_hasta` de una habilitación es el **último día válido**, inclusive, evaluado en la zona horaria de la sede | Añade el puerto `IZonaHorariaSede`. |
| D8 | Falla cerrada: nada se concede si falta un hecho; si falla el registro de una denegación, la excepción se propaga | Ver §10. |

## 4. Arquitectura

Dos capas, como el resto de `Identidad` (`Dominio/` y `Aplicacion/`):

```
src/Modules/Identidad/
├── Dominio/Autorizacion/
│   ├── Rol.cs, Accion.cs, TipoRecurso.cs, AlcanceReporte.cs
│   ├── NivelAcceso.cs, Decision.cs, MotivoDenegacion.cs
│   ├── Hechos.cs               ← PerfilActor, RelacionClinica, HabilitacionProfesional,
│   │                              ContextoRecurso, HechosAcceso
│   ├── MatrizPermisos.cs       ← la tabla del módulo 03 §4
│   └── PoliticaAcceso.cs       ← función pura (sin I/O)
└── Aplicacion/Autorizacion/
    ├── IAutorizador.cs         ← contrato que consumen los demás módulos
    ├── Autorizador.cs          ← carga hechos, aplica la política, audita denegaciones
    └── Puertos.cs              ← IRolesPorSede, IVinculoPaciente, IRelacionClinica,
                                   IHabilitaciones, IZonaHorariaSede, IAuditoriaAutorizacion

tests/Identidad.Tests/Autorizacion/
    MatrizPermisosTests.cs, PoliticaAccesoTests.cs, AutorizadorTests.cs, DoblesAutorizacion.cs
```

Namespaces: `Continuum.Identidad.Dominio.Autorizacion` y `Continuum.Identidad.Aplicacion.Autorizacion`. No se agregan paquetes NuGet.

**Dirección de dependencias.** `Identidad` no referencia a otros módulos. Los módulos consumidores (`HistoriaClinica`, `Agenda`, `Reportes`…) referencian a `Identidad` solo para llamar a `IAutorizador`. `HistoriaClinica` además implementa `IRelacionClinica` (inversión de dependencia).

## 5. Contrato público

```csharp
namespace Continuum.Identidad.Dominio.Autorizacion;

public enum Rol { Paciente, Profesional, AsistenteClinico, Recepcion, CoordinadorSede,
                  Director, AdminFuncional, Auditor, RedApoyo }              // FACTURACION es Fase 3: no se modela

public enum Accion { Crear, Leer, Actualizar, Anular }

public enum TipoRecurso { DatosDemograficos, Cita, AgendaProfesional, NotaClinica, ResumenSeguridad,
                          SignosVitalesTriaje, Orden, AsignacionFormulario, RespuestaFormulario,
                          Mensaje, Reporte, Catalogo, UsuarioRol, Bitacora }

public enum AlcanceReporte { Propio, Operativo, Sede, Global, Cumplimiento } // obligatorio si Recurso = Reporte

public enum NivelAcceso
{
    Completo,    // todo el recurso
    PorSeccion,  // asistente: solo secciones marcadas visibles (HCE filtra con el indicador de plantilla)
    Resumen,     // solo diagnóstico y plan (equipo sin permiso en episodio sensible)
    Agregado     // sin datos identificables (dirección)
}

public enum TipoDenegacion { NoEncontrado /* 404 */, Prohibido /* 403 */ }

public enum MotivoDenegacion   // código de auditoría entre paréntesis
{
    OtraOrganizacion,      // OTRA_ORGANIZACION
    SinRolEnSede,          // SIN_ROL_EN_SEDE
    SinPermisoDeRol,       // SIN_PERMISO_DE_ROL
    SedeDistinta,          // SEDE_DISTINTA
    NoEsPropio,            // NO_ES_PROPIO
    NoLiberado,            // NO_LIBERADO
    SinRelacionClinica,    // SIN_RELACION_CLINICA
    EpisodioSensible,      // EPISODIO_SENSIBLE
    SinHabilitacion,       // SIN_HABILITACION
    HabilitacionVencida,   // HABILITACION_VENCIDA
    DatosInsuficientes     // DATOS_INSUFICIENTES
}

public sealed record Decision
{
    public bool EstaPermitido { get; }
    public NivelAcceso? Nivel { get; }                 // solo si está permitido
    public TipoDenegacion? Tipo { get; }               // solo si está denegado
    public MotivoDenegacion? Motivo { get; }           // solo si está denegado
    public static Decision Permitir(NivelAcceso nivel);
    public static Decision Denegar(TipoDenegacion tipo, MotivoDenegacion motivo);
}
```

`ContextoRecurso` se declara en `Dominio/Autorizacion/Hechos.cs` (namespace `Continuum.Identidad.Dominio.Autorizacion`), porque `PoliticaAcceso` lo consume y `Dominio` no puede referenciar `Aplicacion`. Se muestra aquí junto a la solicitud por claridad.

```csharp
namespace Continuum.Identidad.Aplicacion.Autorizacion;

public sealed record SolicitudAcceso(
    Guid UsuarioId, Guid SedeActivaId, Accion Accion, TipoRecurso Recurso, ContextoRecurso Contexto);

/// <summary>Datos del recurso, leídos por el módulo consumidor de la fila que ya cargó.</summary>
public sealed record ContextoRecurso(
    Guid OrganizacionId,                 // organización dueña del recurso
    Guid? RecursoId = null,
    Guid? PacienteId = null,
    Guid? ProfesionalId = null,          // citas, agenda, reportes propios
    Guid? EpisodioId = null,             // obligatorio en NotaClinica, Orden y SignosVitalesTriaje
    Guid? SedeId = null,                 // solo recursos atados a una sede
    Guid? ProcedimientoId = null,        // opcional, para la habilitación (R8)
    bool? LiberadoAlPaciente = null,     // órdenes
    AlcanceReporte? AlcanceReporte = null);

public interface IAutorizador
{
    Task<Decision> AutorizarAsync(SolicitudAcceso solicitud, CancellationToken ct = default);
}
```

La **organización del actor** no viaja en la solicitud: la devuelve `IRolesPorSede`, para no confiar en el llamador.

## 6. Orden de evaluación (denegar por defecto)

`Autorizador` carga **solo los hechos que la política declara necesarios** para esa combinación de recurso, acción y roles (`PoliticaAcceso.Necesidades(...)`). Un `Leer` de datos demográficos no consulta la relación clínica.

| Paso | Regla | Si falla |
|---|---|---|
| R0 | El perfil del actor debe existir y tener al menos un rol en la sede activa | `Prohibido` · `SIN_ROL_EN_SEDE` |
| R1 | `Contexto.OrganizacionId` debe ser la organización del actor (`RN-015`) | `NoEncontrado` · `OTRA_ORGANIZACION` |
| R2 | Para **cada rol** del actor se busca la celda rol × recurso × acción (× alcance en reportes) | sin celda en ningún rol: `Prohibido` · `SIN_PERMISO_DE_ROL` |
| R3 | `MismaSede`: si la celda lo exige, `Contexto.SedeId` debe ser la sede activa (`RN-002`) | `SEDE_DISTINTA`. Sin `SedeId` en el contexto: `DATOS_INSUFICIENTES` |
| R4 | `Propio`: paciente → `Contexto.PacienteId` = vínculo del actor; profesional → `Contexto.ProfesionalId` = su `ProfesionalId`. `Guid.Empty` se trata como ausente | `NO_ES_PROPIO` |
| R5 | `Liberado`: `Contexto.LiberadoAlPaciente` debe ser `true` | `NO_LIBERADO` |
| R6 | `Relacion` (T): la relación vigente con el episodio o paciente debe ser `Tratante` o `Equipo` (`RN-001`) | `SIN_RELACION_CLINICA` |
| R7 | Sensibilidad (`RN-016`), solo si el episodio es sensible (ver abajo) | `EPISODIO_SENSIBLE` |
| R8 | Habilitación (`RN-012`): toda escritura del rol `Profesional` sobre `NotaClinica` u `Orden` exige habilitación vigente en la especialidad (y procedimiento si se informa) | `SIN_HABILITACION` o `HABILITACION_VENCIDA` |

**Varios roles.** Se evalúa cada rol y se concede el resultado más permisivo, con este orden: `Completo` > `PorSeccion` > `Resumen` > `Agregado`. Si ningún rol concede, el motivo es el del rol cuya evaluación llegó más lejos (orden R3 → R8); si ningún rol tiene celda, `SIN_PERMISO_DE_ROL`.

**R7, sensibilidad.** Aplica a `NotaClinica`, `Orden` y `SignosVitalesTriaje` cuando `RelacionClinica.EpisodioSensible` es verdadero:

| Quién | Lectura | Escritura |
|---|---|---|
| Tratante | `Completo` | permitida |
| Equipo **con** `PermiteSensible` | `Completo` | permitida |
| Equipo **sin** `PermiteSensible` | `Resumen` solo en `NotaClinica`; en lo demás `EPISODIO_SENSIBLE` | `EPISODIO_SENSIBLE` (supuesto a) |
| Asistente clínico | `EPISODIO_SENSIBLE` | `EPISODIO_SENSIBLE` |

**R8, borde de la licencia.** La especialidad evaluada es `RelacionClinica.EspecialidadId` (las escrituras clínicas siempre exigen relación, R6). La habilitación del puerto trae `VigenteHasta` como `DateOnly`. «Hoy» es la fecha de `IReloj.Ahora` en la zona horaria de la sede activa. Hay vencimiento si `hoy > VigenteHasta`. Si la habilitación no es de esa especialidad, o se informa `ProcedimientoId` y no figura en la lista permitida: `SIN_HABILITACION`, que se evalúa **antes** que el vencimiento. Una licencia que venció ayer se deniega; una que vence hoy sigue vigente hoy (`CA-ROL-003`).

**Resultado.** Si pasa R0–R8: `Permitir(nivel)`. El nivel sale de la celda, ajustado por R7: `PorSeccion` para el asistente en `NotaClinica`, `Resumen` para el equipo sin permiso, `Agregado` para la dirección en citas, agenda y reportes, y `Completo` en lo demás.

## 7. Matriz codificada

Es la tabla del módulo 03 §4 más las ampliaciones marcadas con ★. Leyenda: **C** crear · **L** leer · **A** actualizar · **X** anular · `[P]` propio · `[T]` relación clínica · `[S]` misma sede · `[Lib]` liberado · `(nivel)` si no es `Completo`.

| Recurso | Paciente | Profesional | Asist. clínico | Recepción | Coord. sede | Director | Admin func. | Auditor |
|---|---|---|---|---|---|---|---|---|
| `DatosDemograficos` | L/A `[P]` | L | L | C/L/A | C/L/A | — | L | — |
| `Cita` | L `[P]` | L/A `[P,S]` | L `[S]` | C/L/A/X `[S]` | C/L/A/X `[S]` | L (Agregado) | L | — |
| `AgendaProfesional` | — | C/L/A `[P,S]` | L `[S]` | L `[S]` | C/L/A `[S]` | L (Agregado) | L | — |
| `NotaClinica` (incluye diagnósticos) | — | C/L `[T]` | L `[T]` (PorSeccion) | — | — | — | — | — |
| `ResumenSeguridad` ★ | — | L `[T]` | L `[T]` | — | — | — | — | — |
| `SignosVitalesTriaje` ★ | — | L `[T]` | C/L/A `[T]` | — | — | — | — | — |
| `Orden` | L `[P,Lib]` | C/L/X `[T]` | L `[T]` | — | — | — | — | — |
| `AsignacionFormulario` ★ | — | C `[T]` | — | — | — | — | — | — |
| `RespuestaFormulario` | C/L `[P]` | L `[T]` | — | — | — | — | — | — |
| `Mensaje` | C/L `[P]` | C/L `[T]` | — | — | — | — | — | — |
| `Catalogo` | — | L | L | L | L | L | C/L/A/X | L |
| `UsuarioRol` | — | — | — | — | L `[S]` | L | C/L/A/X | L |
| `Bitacora` | — | — | — | — | — | — | — | L |

**Reportes** (siempre `Leer`; el alcance es obligatorio):

| `AlcanceReporte` | Quién |
|---|---|
| `Propio` | Profesional `[P]` |
| `Operativo` | Recepción · Coord. `[S]` · Director (Agregado) · Admin |
| `Sede` | Coord. `[S]` · Director (Agregado) · Admin |
| `Global` | Director (Agregado) · Admin |
| `Cumplimiento` | Auditor · Director (Agregado) · Admin |

**Diferencias deliberadas respecto al módulo 03** (todas en `SC-002`):
- `RED_APOYO`: denegado en todo (módulo 19 es Fase 3; los tutores de menores los define el módulo 08).
- `Paciente × Cita`: solo `L [P]`. «Solicitar» es Fase 2 y confirmar o cancelar desde el portal no está en la matriz.
- `Paciente × Bitacora`: denegado (el módulo 03 lo marca como Fase 2).
- `NotaClinica` no tiene `Actualizar`: el borrador, la firma y las adendas se tratan como `Crear` (`RN-006`).

## 8. Puertos

Cada puerto lo implementa el módulo dueño del dato. En este PR solo existen las interfaces y los dobles.

| Puerto | Firma | Lo implementa |
|---|---|---|
| `IRolesPorSede` | `ObtenerPerfilAsync(usuarioId, sedeId) → PerfilActor?` con `OrganizacionId`, `Roles`, `ProfesionalId?`. `null` si el usuario no existe o está inactivo. | `Identidad` (tramo A) |
| `IVinculoPaciente` | `ObtenerPacienteIdAsync(usuarioId) → Guid?` | `Pacientes` (Dev B) |
| `IRelacionClinica` | `ObtenerAsync(profesionalId, pacienteId, episodioId?, ahora) → RelacionClinica?` con `Tipo` (`Ninguna`/`Equipo`/`Tratante`), `PermiteSensible`, `EpisodioSensible`, `EspecialidadId`. Sin `episodioId` devuelve la mejor relación con cualquier episodio abierto del paciente. Aplica `desde`/`hasta` del miembro a `ahora`. | `HistoriaClinica` (Dev A) |
| `IHabilitaciones` | `ObtenerAsync(profesionalId, especialidadId) → HabilitacionProfesional?` con `ProcedimientosPermitidos` y `VigenteHasta` (`DateOnly`) | `Identidad` (tramo A) |
| `IZonaHorariaSede` | `ObtenerAsync(sedeId) → TimeZoneInfo` (por defecto `America/Santo_Domingo`) | `Organizacion` (Dev B) |
| `IAuditoriaAutorizacion` | `RegistrarDenegacionAsync(EventoAutorizacion)` | `Auditoria` (Dev A) |

Se reutiliza `IReloj`. `IZonaHorariaSede` no estaba en la lista aprobada: se añade porque el borde de la licencia (D7) depende de la fecha local de la sede.

## 9. Auditoría de denegaciones

```csharp
public sealed record EventoAutorizacion(
    DateTimeOffset OcurridoEn, Guid UsuarioId, Guid? OrganizacionId, Guid SedeId,
    IReadOnlyCollection<Rol> Roles, Accion Accion, TipoRecurso Recurso,
    Guid? RecursoId, Guid? PacienteId, TipoDenegacion Tipo, MotivoDenegacion Motivo);
```

- `Autorizador` registra **un evento por cada `Denegado`**. El puerto lo trata siempre como nivel **crítico**, resultado `denegado` (módulo 23 §2, «Seguridad», y `CA-AUD-003`).
- El evento lleva solo identificadores y códigos. **Nunca** contenido clínico, nombres ni documentos de identidad. Los roles del evento son una copia del perfil: no cambian si el perfil se modifica después.
- La IP, el agente y el `correlacion_id` los añade el adaptador de `Auditoria` desde la solicitud HTTP; la política no los ve.
- El módulo `Auditoria` traduce `Recurso` + `Accion` al código del módulo 23 (por ejemplo `HCE.NOTA.VER`).
- Los accesos **permitidos** los audita el módulo consumidor dentro de su propia transacción (`RF-AUD-008`). La política no controla esa transacción.

## 10. Manejo de fallos

- Si un puerto lanza una excepción, esta se propaga: la solicitud falla y no se concede acceso.
- Un `null` de un puerto significa algo distinto según el puerto: `IRolesPorSede` → `SIN_ROL_EN_SEDE` (R0); `IHabilitaciones` → `SIN_HABILITACION` (R8: el profesional no tiene habilitación en esa especialidad); `IVinculoPaciente` → `NO_ES_PROPIO` (R4: el usuario no es paciente); `IRelacionClinica` → `DATOS_INSUFICIENTES`. Lo mismo ocurre si el contexto no trae un dato exigido (`EpisodioId` en recursos de episodio, `SedeId` con `[S]`, `AlcanceReporte` en reportes). `DATOS_INSUFICIENTES` es `Denegado(Prohibido, ...)` y se audita. Un profesional sin ficha (`ProfesionalId` nulo) en un recurso con `[T]` es `SIN_RELACION_CLINICA`, sin consultar el puerto.
- Si falla `IAuditoriaAutorizacion.RegistrarDenegacionAsync`, la excepción se propaga. Se prefiere fallar a perder en silencio un evento crítico. Nunca se devuelve `Permitido` en estos casos.
- Los logs técnicos no incluyen contenido clínico, identificadores de documento ni tokens.

## 11. Pruebas

En `tests/Identidad.Tests/Autorizacion/`, con xUnit y dobles manuales como los de `Dobles.cs`. Metas: reglas de negocio ≥ 90 %, dominio ≥ 70 %.

| Clase | Qué cubre |
|---|---|
| `MatrizPermisosTests` | Recorre rol × recurso × acción (× alcance) contra una **copia literal** de la tabla del §7. Si el código y la tabla divergen, falla. Segunda aserción: toda celda que no está en la tabla devuelve `SIN_PERMISO_DE_ROL`. |
| `PoliticaAccesoTests` | Una prueba dedicada por regla `RN-001`, `RN-002`, `RN-012`, `RN-015` y el acceso sensible de `RN-016`. Los criterios por ID, ver abajo. Varios roles, borde de licencia, motivo más profundo. |
| `AutorizadorTests` | Carga perezosa de hechos, auditoría de denegaciones, ausencia de evento en accesos permitidos, falla cerrada, `ahora` de `IReloj` hacia los puertos, y las seis filas de la matriz mínima de endpoints clínicos. |

**Trazabilidad de criterios** (nombre de la prueba lleva el ID):

| Criterio | Prueba |
|---|---|
| `CA-ROL-001` | `CA_ROL_001_recepcion_ve_demograficos_y_citas_pero_no_notas_diagnosticos_ni_ordenes` |
| `CA-ROL-002` | `CA_ROL_002_cardiologo_ajeno_ve_demograficos_pero_no_notas_de_psicologia` |
| `CA-ROL-003` | `CA_ROL_003_licencia_vencida_ayer_impide_firmar_nota_con_motivo`, más `..._licencia_que_vence_hoy_sigue_vigente` |
| `CA-AUD-003` | `CA_AUD_003_recepcion_abre_nota_por_url_directa_genera_evento_denegado_critico` |

**Matriz mínima de endpoints** (`CLAUDE.md`), a nivel de `Autorizador`: tratante → permitido; equipo según sensibilidad → `Completo` o `Resumen`; profesional ajeno → `SIN_RELACION_CLINICA`; recepción → `SIN_PERMISO_DE_ROL`; otra organización → `NoEncontrado`. La fila «sin sesión → 401» corresponde al adaptador HTTP y queda fuera de B.

La vigencia `desde`/`hasta` de los miembros del equipo es responsabilidad de `IRelacionClinica` (`HistoriaClinica`) y se prueba allí. En B solo se verifica que `Autorizador` le pasa `ahora`.

## 12. Interpretaciones adoptadas y puntos abiertos

Cosas que la documentación no define y que esta spec resuelve de una forma concreta. Todas pasan a `SC-002` para que la documentación quede explícita.

| # | Tema | Interpretación adoptada |
|---|---|---|
| I1 | Sede del recurso | `RN-002` y `RF-ROL-005` nombran la sede pero no dicen cómo. Se exige `[S]` solo en recursos atados a una sede (citas, agenda, usuarios, reportes de sede) y solo a los roles operativos. Los datos clínicos y demográficos son de nivel organización. |
| I2 | Recursos añadidos | `ResumenSeguridad`, `SignosVitalesTriaje` y `AsignacionFormulario` no son filas del módulo 03 §4; salen de los módulos 12 y 14. Los reportes se dividen por alcance porque la matriz usa «propios, operativos, sede, todos, cumplimiento». |
| I3 | `NotaClinica` sin `Actualizar` | Se respeta la matriz literal: guardar el borrador, firmar y agregar adendas son `Crear`. |
| I4 | `CA-HCE-003` dice que **cualquier** profesional ve el banner de alergias; `RN-001` limita la información clínica al tratante y al equipo | Prevalece `RN-001`: `ResumenSeguridad` exige relación clínica con el paciente. **Para revisar.** |
| I5 | Asistente en episodios sensibles | Consecuencia de D4: tampoco registra signos vitales ni triaje en ellos. **Para revisar**, porque el triaje previo a una consulta de salud mental queda a cargo del profesional. |
| I6 | `Resumen` solo para `NotaClinica` | `Orden` y `SignosVitalesTriaje` se niegan por completo al equipo sin permiso. El «plan» forma parte del contenido de la nota. |
| I7 | Reportes: Director × Cumplimiento y Admin × todos | Se interpreta «todos» y «L» como todos los alcances salvo `Propio`. **Para revisar con Dev B** (módulo 21, `RF-REP-004`). |
| I8 | `ResumenSeguridad` no se filtra por sensibilidad | Se lee a nivel paciente; los medicamentos activos de un episodio sensible podrían revelar el diagnóstico. Es una decisión de composición del resumen en el módulo 12. **Para revisar.** |

## 13. Riesgos

- **Los módulos consumidores proveen `OrganizacionId`, `SedeId` y demás datos del recurso.** La política confía en que sean los de la fila cargada. Mitigación: filtro por `organizacion_id` en todas las consultas (`RN-015`) y políticas por fila (ADR-004); la política es una defensa adicional.
- **Hasta el tramo A y `HistoriaClinica`, los puertos solo existen como interfaces.** B se valida con dobles; la integración real llega con esos módulos.
- **Migración y plantillas.** D3 y D4 dependen de `SC-002` aprobada y de una migración de Dev B antes de que `HistoriaClinica` persista el permiso sensible y el indicador de sección.
