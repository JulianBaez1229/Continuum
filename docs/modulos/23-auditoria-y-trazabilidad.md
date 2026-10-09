---
id: MOD-23
codigo: AUD
titulo: Auditoría y trazabilidad
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-04, MOD-24]
trazabilidad: ["RNF09", "RN06", "RNF07"]
---

# 23 — Auditoría y trazabilidad

## 1. Objetivo

Registrar de forma inmutable **quién consulta o modifica información**, cuándo, desde dónde y con qué resultado, para proteger al paciente, responder a investigaciones y demostrar cumplimiento legal (RNF09).

## 2. Qué se audita

| Categoría | Acciones | Nivel |
|---|---|---|
| Acceso clínico | Ver HCE, ver nota, ver respuesta de formulario, ver mensaje, imprimir, exportar | Informativo |
| Escritura clínica | Crear / firmar nota, adenda, diagnóstico, orden, anulación | Informativo |
| Acceso excepcional | Acceso de emergencia, acceso a categoría sensible fuera del equipo | **Crítico** |
| Identidad | Inicio de sesión (exitoso/fallido), MFA, bloqueo, cambio de contraseña | Informativo / Advertencia |
| Administración | Cambio de rol, habilitación, usuario, configuración, catálogo, importaciones | Advertencia |
| Datos de paciente | Alta, edición de datos, fusión de expedientes, consentimientos | Informativo |
| Red de apoyo | Autorización, revocación, accesos del familiar | Informativo |
| Exportaciones | Reportes, PDF, descargas de documentos | Advertencia |
| Seguridad | Accesos denegados, intento de acceso a otra organización | **Crítico** |

## 3. Estructura del evento

| Campo | Ejemplo |
|---|---|
| `ocurrido_en` | 2026-10-02T14:31:05Z |
| `usuario_id`, `rol`, `sede_id` | u-123, PROFESIONAL, sede-norte |
| `accion` | `HCE.NOTA.VER` |
| `recurso`, `recurso_id` | `nota_clinica`, n-456 |
| `paciente_id` | p-789 |
| `resultado` | `permitido` / `denegado` |
| `motivo` | Justificación (accesos de emergencia) |
| `ip`, `agente` | 10.0.0.5, Chrome 140 / Windows |
| `correlacion_id` | Para seguir una solicitud completa |
| `hash_anterior`, `hash` | Encadenamiento para detectar alteraciones |

**No se guarda** el contenido clínico en la bitácora, solo referencias.

## 4. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-AUD-001 | El sistema deberá registrar en una bitácora quién consulta o modifica información clínica, con fecha y hora de cada acción. | M | RNF09 |
| RF-AUD-002 | El sistema deberá registrar también los eventos de identidad, administración, exportación y seguridad de la sección 2. | M | RNF09 |
| RF-AUD-003 | La bitácora deberá ser de solo anexar: ningún usuario, incluido el administrador, podrá modificar ni eliminar eventos; la integridad se verificará con encadenamiento de hashes. | M | RNF09 |
| RF-AUD-004 | El sistema deberá permitir al auditor consultar la bitácora por usuario, paciente, acción, nivel y periodo, y exportarla. | M | RNF09 |
| RF-AUD-005 | El sistema deberá generar alertas al oficial de privacidad ante patrones anómalos: accesos de emergencia, más de N expedientes consultados por hora por un usuario, accesos fuera de horario. | S | GEN |
| RF-AUD-006 | El sistema deberá permitir al paciente solicitar el registro de quién accedió a su expediente (derecho de acceso, Ley 172-13). | S | RNF07 |
| RF-AUD-007 | El sistema deberá conservar la bitácora 10 años. | M | RNF09 |
| RF-AUD-008 | El registro de auditoría deberá escribirse en la misma transacción que la operación (o con garantía de entrega) para que no haya acciones sin evento. | M | RNF09 |

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-AUD-001 | **Dado** un profesional que abre 3 notas de un paciente, **cuando** el auditor filtra por ese paciente, **entonces** ve 3 eventos `HCE.NOTA.VER` con usuario, fecha y hora. |
| CA-AUD-002 | **Dado** un administrador de base de datos que altera un evento directamente en la tabla, **cuando** se ejecuta la verificación de integridad, **entonces** el sistema detecta la ruptura de la cadena. |
| CA-AUD-003 | **Dado** un intento de un recepcionista de abrir una nota por URL directa, **cuando** el sistema lo deniega, **entonces** queda un evento `denegado` de nivel crítico. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
