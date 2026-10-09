<!--
BORRADOR. Según docs/GUIA-DE-ACTUALIZACION.md §2, la solicitud definitiva se copia a
docs/cambios/SC-<nnn>-administrador-principal-y-aprobacion.md en una rama `docs/SC-<nnn>-...`, con su propio PR.
Aquí no se edita nada de docs/. El número es provisional (se salta SC-002 para no chocar con otros borradores de la revisión del módulo 03): docs/cambios ya tiene SC-001 (parámetros del módulo 07) y
specs/SC-001-borrador-matriz-y-equipo-de-atencion.md también reclama SC-001.
Origen: petición de Julian en el hilo del tramo A (2026-10-09) tras el punto abierto «quitar o desactivar al último administrador».
-->
---
id: SC-003
titulo: "Administrador principal (SYSTEM_ADMIN) y aprobación para desactivar administradores"
solicitante: "Julian (Dev A) — borrador asistido por Claude Code"
fecha: 2026-10-09
tipo: funcional_mayor            # cambia un rol (GUIA-DE-ACTUALIZACION §1)
estado: abierta
prioridad: media
---

# SC-003 — Administrador principal y aprobación para desactivar administradores

## 1. Descripción del cambio

Crear un rol `SYSTEM_ADMIN` (administrador principal) por organización. Los demás `ADMIN_FUNCIONAL` pueden ser desactivados, o perder su rol, solo con la aprobación del administrador principal, mediante un flujo de solicitud y aprobación.

## 2. Motivo

La documentación no define ninguna protección contra que una organización se quede sin administradores. Mientras esta solicitud se decide, el tramo A aplica una regla mínima (decidida por Julian el 2026-10-09): no se puede desactivar ni quitar el rol al **último** `ADMIN_FUNCIONAL` activo de la organización (`ULTIMO_ADMINISTRADOR`). Un administrador sí puede desactivar a otro mientras quede alguno activo, y la regla mínima no impide que dos administradores se desactiven entre sí en secuencia hasta quedar uno solo. Esta solicitud sustituye esa regla por un administrador principal con aprobación (ver «Puntos abiertos» de `specs/2026-10-09-usuarios-roles-habilitaciones-tramo-a.md`).

## 3. Elementos afectados

| Tipo | ID | Archivo | Acción |
|---|---|---|---|
| Rol | `SYSTEM_ADMIN` | `modulos/03-actores-roles-y-permisos.md` §3 | Agregar (MFA obligatorio) |
| Matriz | «Usuarios y roles» | `modulos/03-actores-roles-y-permisos.md` §4 | Modificar: columna nueva |
| Requisito | `RF-ROL-010` | `modulos/03-actores-roles-y-permisos.md` §5 | Agregar: solicitud y aprobación de desactivación de un administrador |
| Requisito | `RF-ROL-011` | `modulos/03-actores-roles-y-permisos.md` §5 | Agregar: el administrador principal no se desactiva por este flujo; se traspasa |
| Criterios | `CA-ROL-005…` | `modulos/03-actores-roles-y-permisos.md` §6 | Agregar (numeración a coordinar con SC-001) |
| Regla | `RN-021` | `modulos/27-reglas-de-negocio.md` | Agregar: siempre hay al menos un `SYSTEM_ADMIN` activo por organización |
| Modelo de datos | `rol_asignado`, `solicitud_aprobacion` | `modulos/05-modelo-de-datos.md` | Modificar / agregar entidad |
| Auditoría | eventos de solicitud, aprobación, rechazo | `modulos/23-auditoria-y-trazabilidad.md` §2 | Agregar |
| Identidad | MFA y reautenticación | `modulos/07-identidad-y-autenticacion.md` | Revisar `RF-IAM-002` y `RF-IAM-010` |

## 4. Texto propuesto (a discutir)

> **RF-ROL-010.** El sistema deberá exigir la aprobación de un `SYSTEM_ADMIN` para desactivar a un usuario con rol `ADMIN_FUNCIONAL` o para retirarle ese rol. Un `ADMIN_FUNCIONAL` crea la solicitud; el `SYSTEM_ADMIN` la aprueba o la rechaza con un motivo; el solicitante no puede aprobar su propia solicitud. Hasta la aprobación no cambia nada.
>
> **RF-ROL-011.** El sistema deberá impedir que el último `SYSTEM_ADMIN` activo de una organización sea desactivado o pierda el rol. El cargo se traspasa asignando `SYSTEM_ADMIN` a otro usuario antes.
>
> **CA (borrador).** *Dado* un `ADMIN_FUNCIONAL` que solicita desactivar a otro administrador, *cuando* el `SYSTEM_ADMIN` aprueba, *entonces* la cuenta queda inactiva, sus sesiones se cierran y la bitácora registra solicitud, aprobación y desactivación.

## 5. Análisis de impacto

- Módulos afectados: 03, 05, 07, 23, 27 (y 17 si la aprobación se notifica).
- ¿Cambia el alcance del MVP? **Sí**, añade un flujo de aprobación. Hay que decidir si entra en el MVP o en la Fase 2.
- ¿Cambia el modelo de datos? Sí.
- ¿Cambia una regla de negocio? Sí (`RN-021` nueva).
- ¿Requiere nuevos criterios de aceptación? Sí.
- Implementación: nueva política de la matriz, una entidad de solicitud con estados, y cambios en `ServicioUsuarios` y `ServicioRoles`.

## Preguntas para la decisión

1. ¿Uno o varios `SYSTEM_ADMIN` por organización? Con uno solo, perderlo bloquea todo; con varios, ¿basta con uno que apruebe?
2. ¿Quién crea el primer `SYSTEM_ADMIN`? Probablemente el alta de la organización (módulo 06).
3. ¿La aprobación aplica también a **quitar el rol** y a **desactivarse a uno mismo**, o solo a desactivar a otros?
4. ¿La solicitud caduca? (propuesta: sí, a los 7 días)
5. ¿Exige reautenticación con MFA al aprobar (`RF-IAM-010`)?
6. ¿Entra en el MVP o en la Fase 2?

## 6. Decisión

| Fecha | Decisión | Aprobado por | Comentarios |
|---|---|---|---|
| | | | |

## 7. Implementación

- Rama / PR:
- Versión de la documentación:
- Entrada en CHANGELOG: ☐
