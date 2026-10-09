<!--
BORRADOR. Según docs/GUIA-DE-ACTUALIZACION.md §2, la solicitud definitiva se copia a
docs/cambios/SC-001-matriz-y-equipo-de-atencion.md en una rama `docs/SC-001-matriz-y-equipo-de-atencion`,
con su propio PR. Aquí no se edita nada de docs/. El número SC-001 es provisional (no existe aún ninguna solicitud).
Origen: specs/2026-10-09-politica-de-autorizacion-design.md
-->
---
id: SC-001
titulo: "Matriz de permisos explícita, permiso sensible y visibilidad del asistente"
solicitante: "Dev A — Seguridad y clínico (borrador asistido por Claude Code)"
fecha: 2026-10-09
tipo: funcional_menor
estado: abierta
prioridad: alta
---

# SC-001 — Matriz de permisos explícita, permiso sensible y visibilidad del asistente

## 1. Descripción del cambio

Hacer explícito en la documentación lo que la política de autorización (`RF-ROL-005`) necesita para ser implementable sin interpretar en silencio: dónde se guarda el permiso del tratante sobre episodios sensibles, qué ve el asistente clínico, y qué celdas de la matriz del módulo 03 se aclaran o amplían.

## 2. Motivo

Al diseñar la política de autorización se encontraron contradicciones y vacíos entre los módulos 03, 05, 12 y 24 (ver spec `2026-10-09-politica-de-autorizacion-design.md`, §12). `CLAUDE.md` exige no elegir una interpretación en silencio.

## 3. Elementos afectados

| Tipo | ID | Archivo | Acción |
|---|---|---|---|
| Modelo de datos | `miembro_equipo` | `modulos/05-modelo-de-datos.md` §3.4 | Modificar: agregar `permite_sensible` |
| Requisito | `RF-ROL-006` | `modulos/03-actores-roles-y-permisos.md` §5 | Modificar: el tratante marca el permiso al agregar al miembro |
| Requisito | `RF-HCE-017` | `modulos/12-historia-clinica-electronica.md` §4 | Agregar: visibilidad por sección para el asistente |
| Decisión | `ADR-006` | (propuesta) | Modificar: el esquema de plantilla incluye `visible_asistente` por sección |
| Criterios | `CA-ROL-005`, `CA-ROL-006`, `CA-ROL-007` | `modulos/03-actores-roles-y-permisos.md` §6 | Agregar |
| Matriz | §3 y §4 | `modulos/03-actores-roles-y-permisos.md` | Modificar: aclaraciones de la sección 4 de esta solicitud |
| Criterio | `CA-HCE-003` | `modulos/12-historia-clinica-electronica.md` §5 | Modificar: «cualquier profesional con relación clínica con el paciente» |

## 4. Texto propuesto

### 4.1 Módulo 05 — `miembro_equipo`

**Antes:** `miembro_equipo` — episodio_id, profesional_id, desde, hasta

**Después:** `miembro_equipo` — episodio_id, profesional_id, desde, hasta, **permite_sensible** (bool, por defecto `false`; lo fija el tratante, `RN-016`)

### 4.2 Módulo 03 — `RF-ROL-006`

**Antes:** El sistema deberá permitir al profesional tratante agregar o retirar miembros del equipo de atención de un episodio, con registro en la bitácora.

**Después:** El sistema deberá permitir al profesional tratante agregar o retirar miembros del equipo de atención de un episodio y, en episodios sensibles, **otorgar o revocar el permiso para ver las notas completas**, con registro en la bitácora.

### 4.3 Módulo 03 — aclaraciones a la matriz (§4)

1. **Asistente clínico, «L parcial (T)»:** lee y registra signos vitales y triaje; lee el resumen de seguridad (alergias y medicamentos activos) y las órdenes; lee las secciones de la nota de la especialidad del episodio que la plantilla marque como visibles al asistente; **no lee diagnósticos**; **no accede a episodios sensibles**.
2. **Categorías sensibles:** el miembro del equipo sin permiso explícito ve el resumen (diagnóstico y plan) y no puede crear notas ni leer órdenes del episodio. El tratante y el miembro con permiso ven todo.
3. **Recursos adicionales** (provienen de los módulos 12 y 14): resumen de seguridad, signos vitales y triaje, asignación de formularios.
4. **Reportes por alcance:** propios (profesional), operativos (recepción), sede (coordinador), todos (dirección), cumplimiento (auditor); la dirección y el administrador los ven todos salvo los propios.
5. **`RED_APOYO`:** sin acceso en el MVP hasta que el módulo 19 (Fase 3) y el módulo 08 (tutores de menores) definan su alcance.
6. **Paciente y citas:** «solicitar» es Fase 2 (autoagendamiento); confirmar y cancelar desde el portal (módulo 16, MVP) requieren definir si son `Actualizar (P)` acotado.
7. **`NotaClinica`:** borrador, firma y adendas son `Crear` (`RN-006`).
8. **Sede:** los roles operativos solo actúan sobre recursos de su sede activa (`RN-002`); los datos clínicos y demográficos son de nivel organización.

### 4.4 Módulo 12 — nuevo requisito y criterio

> **RF-HCE-017** (S, origen SC-001): El sistema deberá permitir marcar en la plantilla de nota qué secciones puede ver el asistente clínico. El asistente solo ve esas secciones y nunca en episodios sensibles.

> **CA-HCE-006:** **Dado** una plantilla con la sección «Evaluación funcional» marcada como visible al asistente, **cuando** el asistente clínico del equipo abre la nota, **entonces** ve esa sección, no los diagnósticos ni las demás secciones.

### 4.5 Módulo 12 — `CA-HCE-003`

**Antes:** **Dado** un paciente alérgico a la penicilina, **cuando** cualquier profesional abre su HCE, **entonces** ve el banner de alergia en la parte superior.

**Después:** **Dado** un paciente alérgico a la penicilina, **cuando** un profesional con relación clínica con el paciente (tratante o equipo) abre su HCE, **entonces** ve el banner de alergia en la parte superior.

### 4.6 Módulo 03 — criterios nuevos

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-ROL-005 | **Dado** un episodio sensible y un miembro del equipo sin permiso explícito, **cuando** intenta leer las notas, **entonces** ve solo diagnóstico y plan, y no puede crear notas. |
| CA-ROL-006 | **Dado** un asistente clínico del equipo de un episodio no sensible, **cuando** abre la nota, **entonces** ve las secciones visibles para el asistente y no los diagnósticos. |
| CA-ROL-007 | **Dado** un usuario de otra organización, **cuando** pide un recurso por su ID, **entonces** recibe «no encontrado» y queda un evento crítico en la bitácora. |

## 5. Análisis de impacto

- Módulos afectados: 03, 05, 12, 21 (alcance de reportes), 08 y 16 (tutores y portal), ADR-006.
- ¿Cambia el alcance del MVP? **No.**
- ¿Cambia el modelo de datos? **Sí** (`miembro_equipo.permite_sensible`; migración de Dev B, contrato protegido).
- ¿Cambia una regla de negocio? **No** (concreta `RN-001`, `RN-002` y `RN-016`; no las altera).
- ¿Requiere nuevos criterios de aceptación? **Sí** (`CA-ROL-005` a `007`, `CA-HCE-006`).
- Esfuerzo estimado: 0,5 día de documentación + 0,25 día de migración.
- Puntos que decide el equipo: I4 (alergias visibles solo con relación clínica), I5 (asistente sin triaje en episodios sensibles), I7 (alcance de reportes de la dirección, con Dev B) e I8 (resumen de seguridad y episodios sensibles).

## 6. Decisión

| Fecha | Decisión | Aprobado por | Comentarios |
|---|---|---|---|
| | | | |

## 7. Implementación

- Rama / PR: `docs/SC-001-matriz-y-equipo-de-atencion` (pendiente)
- Versión de la documentación: módulo 03 → 1.1.0, módulo 05 → 1.1.0, módulo 12 → 1.1.0
- Entrada en CHANGELOG: ☐
