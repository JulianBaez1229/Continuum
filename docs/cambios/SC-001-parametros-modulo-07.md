---
id: SC-001
titulo: "Parámetros y modelo de datos que el módulo 07 deja sin definir"
solicitante: "Dev A"
fecha: 2026-10-09
tipo: funcional_menor       # editorial | catalogo | funcional_menor | funcional_mayor
estado: abierta             # abierta | en_analisis | aprobada | rechazada | implementada
prioridad: media            # alta | media | baja
---

# SC-001 — Parámetros y modelo de datos que el módulo 07 deja sin definir

## 1. Descripción del cambio

Al implementar el módulo 07 (Identidad y autenticación) hubo que fijar valores que el documento no define y entidades que el modelo de datos (módulo 05) no incluye. Esta solicitud los registra para que el equipo los apruebe o los corrija antes de congelar el contrato de datos. Mientras tanto el código usa los valores propuestos, todos configurables salvo que se indique lo contrario.

## 2. Motivo

CLAUDE.md pide no elegir una interpretación en silencio cuando un requisito es ambiguo. Los cambios de código están en los PR del módulo 07; aquí se deja constancia de qué se asumió y por qué.

## 3. Elementos afectados

| Tipo | ID | Archivo | Acción |
|---|---|---|---|
| Requisito | RF-IAM-004 | modulos/07-identidad-y-autenticacion.md | Precisar el "desbloqueo progresivo" |
| Criterio | CA-IAM-002 | modulos/07-identidad-y-autenticacion.md | Aclarar que el bloqueo se aplica en el 5.º fallo |
| Requisito | RF-IAM-006 | modulos/07-identidad-y-autenticacion.md | Añadir duración máxima de sesión |
| Requisito | RF-IAM-003 | modulos/07-identidad-y-autenticacion.md | Precisar el código por mensaje |
| Requisito | RF-IAM-010 | modulos/07-identidad-y-autenticacion.md | Precisar la vigencia de la reautenticación |
| Modelo | — | modulos/05-modelo-de-datos.md | Añadir `credencial_usuario`, `sesion`, `token_accion` y campos de `usuario` |
| Modelo | — | modulos/05-modelo-de-datos.md | Vínculo paciente ↔ usuario (hoy no existe) |
| Módulo | MOD-29 | modulos/29-mvp-alcance-y-roadmap.md | Confirmar si RF-IAM-003 y RF-IAM-010 (prioridad S) entran en el MVP |

## 4. Texto propuesto

### 4.1 Parámetros (valores implementados)

| # | Requisito | Valor propuesto | Dónde se configura |
|---|---|---|---|
| 1 | RF-IAM-004 | Bloqueo de **15 min** al 5.º fallo en 15 min; cada bloqueo consecutivo **duplica** la duración (15, 30, 60… hasta **24 h**). Un inicio de sesión completo, una recuperación de contraseña o una reautenticación correcta reinician el contador. | Constantes de `Usuario` (no configurable aún) |
| 2 | CA-IAM-002 | El bloqueo se aplica en el **5.º fallo**; el 6.º intento ya se rechaza (aunque la clave sea correcta). | — |
| 3 | RF-IAM-004 | Los fallos del segundo factor, de la reautenticación y de los códigos por mensaje **suman al mismo contador** que los de la contraseña. La contraseña correcta **no** reinicia el contador mientras falte el segundo factor. | — |
| 4 | RF-IAM-006 | Token de acceso **15 min**; token de renovación **rotativo**. Presentar un token ya rotado **revoca toda la sesión**. | `Jwt` |
| 5 | RF-IAM-006 | Inactividad: **15 min** personal / **30 min** paciente (módulo 06, `duracion_sesion_inactiva_min`). Duración máxima de una sesión: **12 h** aunque haya actividad. | `Sesion:*` |
| 6 | RF-IAM-005 | Enlace de recuperación **30 min**, un solo uso, **1 min** de espera entre solicitudes, una solicitud nueva invalida la anterior. Restablecer cierra **todas** las sesiones, levanta el bloqueo y **no desactiva** el MFA. | `ServicioRecuperacionContrasena` |
| 7 | RF-IAM-007 | Invitación **72 h**, un solo uso, **5 intentos** de verificación de identidad antes de anularla; una invitación nueva invalida la anterior. El paciente sin correo en su ficha lo aporta al activar. | `ServicioActivacionPaciente` |
| 8 | RF-IAM-003 | Código de **6 dígitos**, vigencia **10 min**, **5 intentos**, **1 min** de espera entre envíos. Solo para quien no exige TOTP (pacientes). El SMS queda para la Fase 2 (módulo 29); hoy el canal lo decide el módulo 17. | `ServicioMfaPorMensaje` |
| 9 | RF-IAM-002 | **10 códigos de recuperación** de MFA, de un solo uso, entregados una vez. El código TOTP no se puede reutilizar (se rechazan pasos ya usados). | — |
| 10 | RF-IAM-010 | La reautenticación vale **5 min** y exige contraseña + segundo factor. Las acciones críticas la piden con la política `identidad.reautenticacion-reciente`. | `Sesion:ReautenticacionMinutos` |
| 11 | RF-IAM-001 | El **correo es único** globalmente (el MVP tiene una sola organización). | Índice único |

### 4.2 Modelo de datos (módulo 05)

La sección 3.2 dice "Credenciales en el servicio de identidad". Como la identidad es propia (ADR-005), se proponen estas tablas (el mapeo está en `IdentidadDbContext`):

| Entidad | Campos clave |
|---|---|
| `usuario` (ampliada) | `organizacion_id`, `requiere_mfa`, `mfa_habilitado`, `mfa_mensaje_habilitado`, `row_version` |
| `credencial_usuario` (misma clave que `usuario`) | `hash_contrasena`, `secreto_totp_protegido`, `secreto_totp_pendiente_protegido`, `ultimo_paso_totp`, `bloqueado_hasta`, `bloqueos_consecutivos`, `fallos_recientes`, `codigos_recuperacion_hash` |
| `sesion` | `usuario_id`, `organizacion_id`, `es_personal`, `hash_renovacion`, `hash_renovacion_anterior`, `creada_en`, `ultima_actividad`, `revocada_en`, `row_version` |
| `token_accion` | `proposito`, `usuario_id`, `paciente_id`, `organizacion_id`, `hash_token`, `creado_en`, `expira_en`, `usado_en`, `intentos`, `row_version` |

**Vacío detectado:** `paciente` no tiene vínculo con `usuario` (el módulo 05 no lo define), pero la activación de la cuenta (RF-IAM-007) y el portal (módulo 16) lo necesitan. Se propone `paciente.usuario_id` (nulo hasta activar). El módulo 08 (Dev B) debe implementar `IDirectorioPacientes` e `IConsentimientos` (contrato en `Aplicacion/Puertos.cs`).

### 4.3 Alcance

RF-IAM-003 y RF-IAM-010 son prioridad **S** y el módulo 29 §3 lista para el MVP solo "contraseña robusta, MFA para el personal, invitación de pacientes, bloqueo, cierre por inactividad". Se implementaron ambos porque el módulo 07 los marca como `fase: MVP`; si el equipo decide dejarlos fuera, basta con no exponer sus endpoints.

## 5. Análisis de impacto

- Módulos afectados: 05 (modelo), 07 (requisitos), 08 (contratos del directorio de pacientes y consentimientos), 17 (mensajería), 23 (bitácora), 29 (alcance).
- ¿Cambia el alcance del MVP? Sí / **No** (a confirmar el punto 4.3).
- ¿Cambia el modelo de datos? **Sí** (4.2).
- ¿Cambia una regla de negocio? No.
- ¿Requiere nuevos criterios de aceptación? Sí: se proponen CA para recuperación de contraseña (un solo uso, 30 min), activación de paciente (72 h, identidad) y sesión (renovación rotativa). Hoy están cubiertos por pruebas con nombre `RF_IAM_*`.
- Esfuerzo estimado: bajo (documentación); la migración recae en el Dev B.

## 6. Decisión

| Fecha | Decisión | Aprobado por | Comentarios |
|---|---|---|---|
| | | | |

## 7. Implementación

- Rama / PR: `docs/SC-001-parametros-modulo-07`
- Versión de la documentación:
- Entrada en CHANGELOG: ☐
