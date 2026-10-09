---
id: MOD-03
codigo: ROL
titulo: Actores, roles y permisos
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-06, MOD-07, MOD-27]
trazabilidad: ["Tabla 1", "RF13", "RN01", "RN02", "RN09"]
---

# 03 — Actores, roles y permisos

## 1. Objetivo

Definir quién usa Continuum, qué necesita y qué puede hacer. El modelo combina **roles** (qué funciones ve un usuario) con **atributos** (en qué sede trabaja, qué especialidades tiene habilitadas y qué relación tiene con el paciente). Así se cumple RN-001 (confidencialidad) y RN-002 (acceso según rol) en una clínica con muchas especialidades.

## 2. Actores y necesidades (generalización de la Tabla 1)

| Actor | Necesidades | Módulos que las atienden | Origen |
|---|---|---|---|
| **Paciente** | Consultar sus citas y recibir recordatorios; completar formularios entre encuentros; acceder a recursos; comunicarse de forma segura con su profesional; autorizar a su red de apoyo | 10, 14, 16, 17, 18, 19, 22 | RF02, RF06–RF09, RF11 |
| **Profesional de salud** | Información clínica organizada; registrar notas, diagnósticos, órdenes y prescripciones según su especialidad; gestionar su agenda; dar seguimiento | 09, 12, 13, 14, 15, 18 | RF03–RF07, RF09 |
| **Asistente clínico / enfermería** | Recibir al paciente, registrar signos vitales y triaje, preparar al paciente para el procedimiento | 10, 12 (sección de enfermería) | GEN |
| **Recepción / personal administrativo** | Registrar pacientes; programar, reprogramar y cancelar citas; gestionar lista de espera; check-in | 08, 09, 10 | RF01–RF03 |
| **Coordinador de sede** | Supervisar agendas y recursos de su sede; reasignar citas; ver ocupación | 09, 10, 21 | GEN |
| **Director / propietario** | Supervisar la operación y tomar decisiones con datos confiables | 21 | RF10 |
| **Administrador funcional** | Configurar especialidades, procedimientos, plantillas, formularios, sedes, usuarios y roles | 06, 11, 14, 03 | RF13, GEN |
| **Familiar / tutor** | Participar como red de apoyo con autorización del paciente | 19 | RF12, RN09 |
| **Oficial de privacidad / auditor** | Revisar la bitácora, atender solicitudes de derechos ARCO, investigar accesos | 23, 24 | RNF07, RNF09 |
| **Facturación / caja** _(Fase 3)_ | Registrar cobros, autorizaciones de ARS y cierres de caja | 20 | EXT |
| **Soporte de plataforma** | Operar la infraestructura sin acceso a datos clínicos | 30 | RNF05 |

## 3. Roles del sistema

Un usuario puede tener **varios roles** (p. ej., un médico que también es director). Los roles se asignan **por sede**.

| Rol (código) | Descripción | Requiere MFA |
|---|---|---|
| `PACIENTE` | Acceso al portal del paciente | Opcional (recomendado) |
| `PROFESIONAL` | Acceso clínico; su alcance depende de sus habilitaciones y de su relación con el paciente | **Sí** |
| `ASISTENTE_CLINICO` | Check-in clínico, signos vitales, triaje; sin diagnósticos ni notas de otras especialidades | **Sí** |
| `RECEPCION` | Pacientes (datos demográficos), agendas y citas | Sí |
| `COORDINADOR_SEDE` | Todo lo de recepción + gestión de agendas y recursos de su sede | Sí |
| `DIRECTOR` | Reportes agregados y operativos; sin contenido clínico | Sí |
| `ADMIN_FUNCIONAL` | Catálogos, usuarios, roles, configuración de la organización | **Sí** |
| `AUDITOR` | Lectura de la bitácora y reportes de cumplimiento | **Sí** |
| `RED_APOYO` | Vista limitada del paciente que lo autorizó | Opcional |
| `FACTURACION` _(Fase 3)_ | Cobros, seguros, caja | Sí |

## 4. Matriz de permisos (resumen)

Leyenda: **C** crear · **L** leer · **A** actualizar · **X** anular/desactivar · **—** sin acceso · **P** solo lo propio · **T** solo como tratante o miembro del equipo de atención

| Recurso | Paciente | Profesional | Asist. clínico | Recepción | Coord. sede | Director | Admin func. | Auditor | Red apoyo |
|---|---|---|---|---|---|---|---|---|---|
| Datos demográficos del paciente | L/A (P) | L | L | C/L/A | C/L/A | — | L | — | L (limitado) |
| Citas | L (P), solicitar | L/A (P) | L | C/L/A/X | C/L/A/X | L (agregado) | L | — | L (si autorizado) |
| Agenda del profesional | — | C/L/A (P) | L | L | C/L/A | L (agregado) | L | — | — |
| Notas clínicas y diagnósticos | — | C/L (T) | L parcial (T) | — | — | — | — | — | — |
| Categorías sensibles | — | L (T + permiso específico) | — | — | — | — | — | — | — |
| Órdenes y prescripciones | L (P, las liberadas) | C/L/X (T) | L (T) | — | — | — | — | — | — |
| Formularios clínicos (respuestas) | C/L (P) | L (T), asignar | — | — | — | — | — | — | — |
| Mensajes | C/L (P) | C/L (T) | — | — | — | — | — | — | — |
| Reportes | — | L (propios) | — | L (operativos) | L (sede) | L (todos) | L | L (cumplimiento) | — |
| Catálogos (especialidades, procedimientos, formularios) | — | L | L | L | L | L | C/L/A/X | L | — |
| Usuarios y roles | — | — | — | — | L (sede) | L | C/L/A/X | L | — |
| Bitácora de auditoría | Ver sus accesos (Fase 2) | — | — | — | — | — | — | L | — |

## 5. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-ROL-001 | El sistema deberá permitir al administrador funcional crear, editar y desactivar cuentas de usuario. Las cuentas no se eliminan. | M | RF13 |
| RF-ROL-002 | El sistema deberá permitir asignar uno o varios roles a un usuario, por sede. | M | RF13 |
| RF-ROL-003 | El sistema deberá permitir registrar las habilitaciones de un profesional: especialidades, procedimientos autorizados, número de licencia o exequátur y fecha de vencimiento. | M | GEN |
| RF-ROL-004 | El sistema deberá impedir que un profesional con la habilitación vencida registre notas u órdenes y deberá notificar al administrador 30 días antes del vencimiento. | S | GEN |
| RF-ROL-005 | El sistema deberá evaluar cada acceso a información clínica combinando rol, sede, habilitación y relación con el paciente (tratante o equipo de atención). | M | RN01, RN02 |
| RF-ROL-006 | El sistema deberá permitir al profesional tratante agregar o retirar miembros del equipo de atención de un episodio, con registro en la bitácora. | M | GEN |
| RF-ROL-007 | El sistema deberá ofrecer un acceso de emergencia ("romper el cristal") que exija una justificación escrita, notifique al tratante y al oficial de privacidad y caduque en 24 horas. | S | GEN |
| RF-ROL-008 | El sistema deberá permitir crear roles personalizados a partir de los permisos base, sin otorgar más permisos que los del rol `ADMIN_FUNCIONAL`. | C | EXT |
| RF-ROL-009 | El sistema deberá registrar en la bitácora toda creación, desactivación y cambio de rol o habilitación. | M | RNF09 |

## 6. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-ROL-001 | **Dado** un usuario de recepción, **cuando** abre la ficha de un paciente, **entonces** ve datos demográficos y citas, pero no ve notas, diagnósticos ni órdenes. |
| CA-ROL-002 | **Dado** un cardiólogo que no pertenece al equipo de atención de un paciente de psicología, **cuando** busca a ese paciente, **entonces** puede ver datos demográficos para agendar, pero no las notas del episodio de psicología. |
| CA-ROL-003 | **Dado** un profesional cuya licencia venció ayer, **cuando** intenta firmar una nota, **entonces** el sistema lo impide y muestra el motivo. |
| CA-ROL-004 | **Dado** un acceso de emergencia, **cuando** el profesional lo confirma con justificación, **entonces** el tratante y el oficial de privacidad reciben una notificación y la bitácora registra el evento con nivel "crítico". |

## 7. Pantallas

- Administración › Usuarios (lista, filtros por rol, sede, estado).
- Administración › Usuario › Roles y sedes.
- Administración › Usuario › Habilitaciones (especialidades, procedimientos, licencia, vencimiento).
- Episodio › Equipo de atención.

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
