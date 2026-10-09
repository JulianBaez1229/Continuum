# Usuarios, roles y habilitaciones — notas de diseño (módulo 03, tramo A)

| | |
|---|---|
| **Fecha** | 2026-10-09 |
| **Módulo** | 03 — Actores, roles y permisos (tramo A; el tramo B, la política de acceso, ya está en `main`) |
| **Dueño** | Dev A — `src/Modules/Identidad` |
| **Requisitos** | `RF-ROL-001`, `RF-ROL-002`, `RF-ROL-003`, `RF-ROL-004`, `RF-ROL-009` |
| **Criterios** | `CA-ROL-003` (los demás `CA-ROL-*` los cubre el tramo B) |
| **Reglas** | `RN-006`, `RN-012`, `RN-015`, `RN-016` |

> Estas notas no modifican `docs/`. Lo que dependa de un cambio de documentación está en «Puntos abiertos».

## Alcance

**Dentro:** casos de uso de `ADMIN_FUNCIONAL` para cuentas, roles por sede, ficha de profesional y habilitaciones; los puertos `IRolesPorSede` e `IHabilitaciones` (con implementación); aviso de vencimiento a 30 días; bitácora de cada cambio en la misma transacción; repositorios en memoria.

**Fuera:** endpoints HTTP (esperan los tokens de `RF-IAM-002` y `RF-IAM-006`); tablas y migración de `rol_asignado`, `profesional` y `habilitacion` (PR aparte del Dev B); roles personalizados y acceso de emergencia (`RF-ROL-007` y `008`); equipo de atención (`RF-ROL-006`).

## Cómo encaja

```
ServicioUsuarios / ServicioRoles / ServicioHabilitaciones
   └─ GuardiaAdministracion ── PoliticaAcceso (matriz del módulo 03: UsuarioRol)
   └─ IUnidadDeTrabajo { cambio en repositorio + IAuditoriaAdministracion }   ← una sola transacción
ConsultaRolesPorSede (IRolesPorSede)  ┐ los consume la política de acceso del tramo B
ConsultaHabilitaciones (IHabilitaciones) ┘
ServicioVencimientosHabilitaciones ── IAvisosAdministracion   ← tarea programada, sin actor
```

- **Solo `ADMIN_FUNCIONAL`.** La guardia pide la decisión a `PoliticaAcceso` para `TipoRecurso.UsuarioRol` con la acción que corresponda (`Crear`, `Actualizar`, `Anular`). No hay una segunda lista de permisos: si cambia la matriz, cambia este control.
- **Otra organización → 404** (`RN-015`). El actor nunca declara su organización: sale de su perfil. Cada intento se audita como crítico.
- **Denegaciones auditadas.** Evento `AccesoAdministracionDenegado` (crítico) con el código del motivo. Si la bitácora falla, la excepción se propaga y no se concede nada.
- **Habilitación vencida.** `IHabilitaciones` devuelve la habilitación aunque esté vencida y la política (R8) decide con la fecha local de la sede: la que vence hoy sigue vigente hoy. Una revocada devuelve `null` (`SIN_HABILITACION`).
- **Sin borrado.** Usuarios se desactivan; habilitaciones se revocan; al actualizar una revocada se reactiva. Desactivar un usuario cierra todas sus sesiones.
- **Aviso de 30 días.** Una vez por fecha de vencimiento; cambiar la fecha reabre el aviso. Solo lleva ids, fecha y días restantes (`RN-016`). Si el envío falla no se marca y se reintenta.
- **Bitácora sin datos personales.** Los eventos llevan ids, rol, sede, especialidad y fecha. Nunca correos, nombres ni números de licencia.

## Puertos nuevos que implementan otros módulos

| Puerto | Lo implementa | Para qué |
|---|---|---|
| `IConsultaSedes` | Organización (Dev B) | La sede de una asignación de rol debe ser de la organización del administrador |
| `ICatalogoClinico` | Catálogo (Dev B) | Especialidad y procedimientos de una habilitación deben existir y estar vigentes en el catálogo |
| `IZonaHorariaSede` | Organización (Dev B) | «Hoy» al validar vencimientos (ya estaba en la spec del tramo B, §8) |
| `IAvisosAdministracion` | Comunicación (Dev B) | Entrega del aviso a los administradores |
| `IAuditoriaAdministracion` | Auditoría (Dev A) | Bitácora; se invoca dentro de la unidad de trabajo |
| `IUnidadDeTrabajo` | Persistencia (Dev B) | Transacción de base de datos; hay una versión en memoria para pruebas |

Los repositorios en memoria (`Infraestructura/Memoria`) **no** están registrados en `ModuloIdentidad`: pierden el estado al reiniciar y no son seguros entre hilos.

## Interpretaciones adoptadas

| # | Tema | Decisión |
|---|---|---|
| T1 | Cuentas nuevas | Se crean con una contraseña aleatoria que nadie conoce y con MFA obligatorio. La persona la define con el enlace de recuperación (`RF-IAM-005`), que aún no existe. |
| T2 | Licencia y exequátur | Van en la ficha del profesional, como en el módulo 05, y se exige al menos uno. |
| T3 | Una habilitación por especialidad | `IHabilitaciones.ObtenerAsync(profesionalId, especialidadId)` devuelve una sola, así que se impide duplicar el par. |
| T4 | Vencimiento en el pasado | Registrar o actualizar con un vencimiento anterior a hoy se rechaza (`VENCIMIENTO_PASADO`); para cortar la habilitación se revoca. |
| T5 | Habilitaciones y la matriz | El módulo 03 no tiene una fila propia para habilitaciones; se evalúan con la de `Usuarios y roles`. |

## Puntos abiertos (para decidir, no se asumieron)

1. **Último `ADMIN_FUNCIONAL`.** Decidido por Julian (2026-10-09): no se puede desactivar ni quitar el rol al último administrador activo de la organización (`ULTIMO_ADMINISTRADOR`). El rol de administrador principal con aprobación para desactivar a otros queda en `specs/SC-002-borrador-administrador-principal-y-aprobacion.md`, pendiente de aprobación del equipo.
2. **El MFA de una cuenta depende de sus roles** (`RF-IAM-002`: todos menos `PACIENTE` y `RED_APOYO`), pero `requiere_mfa` se fija al crear. Las cuentas creadas por el administrador son de personal y lo exigen siempre.
3. **Correo único entre organizaciones**: el índice es global, así que un administrador puede saber que un correo existe en otra organización al recibir `CORREO_DUPLICADO`.
4. **Persistencia (Dev B):** tablas `rol_asignado`, `profesional` y `habilitacion` (incluida `avisada_para_vencimiento`) y la implementación de `IUnidadDeTrabajo` con la transacción de EF.
5. **Rollback de `Usuario` en memoria:** la versión en memoria de la transacción no deshace cambios sobre una cuenta ya cargada; con la base de datos real, sí.
