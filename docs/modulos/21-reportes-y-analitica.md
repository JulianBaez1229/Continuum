---
id: MOD-21
codigo: REP
titulo: Reportes y analítica
version: 1.0.0
estado: Borrador
fase: Fase 2 (tablero básico en MVP)
responsable: Por asignar
dependencias: [MOD-10, MOD-12, MOD-14, MOD-03]
trazabilidad: ["RF10"]
---

# 21 — Reportes y analítica

## 1. Objetivo

Dar a la dirección, a los coordinadores y a los profesionales información oportuna y confiable sobre la operación clínica y administrativa, exportable a PDF y Excel, respetando la confidencialidad (RF10).

## 2. Catálogo de reportes

| Código | Reporte | Audiencia | Filtros | Fase |
|---|---|---|---|---|
| `REP-001` | Tablero operativo del día: citas por estado, en sala, tiempos de espera | Recepción, coordinador | Sede | MVP |
| `REP-002` | Asistencia, cancelaciones e inasistencias | Director, coordinador | Periodo, sede, especialidad, profesional, procedimiento | MVP |
| `REP-003` | Carga de trabajo y productividad por profesional | Director | Periodo, sede, especialidad | Fase 2 |
| `REP-004` | Ocupación de agendas y recursos (horas disponibles vs. agendadas) | Director, coordinador | Periodo, sede, recurso | Fase 2 |
| `REP-005` | Pacientes nuevos y recurrentes | Director | Periodo, especialidad | Fase 2 |
| `REP-006` | Lista de espera: tamaño y tiempo medio | Coordinador | Procedimiento | Fase 2 |
| `REP-007` | Diagnósticos más frecuentes (agregado, sin identificar pacientes) | Director clínico | Periodo, especialidad | Fase 2 |
| `REP-008` | Alertas de riesgo: generadas, atendidas, tiempo de respuesta | Director clínico | Periodo | Fase 2 |
| `REP-009` | Adherencia a formularios | Profesional, director clínico | Formulario, profesional | Fase 2 |
| `REP-010` | Mis pacientes y mi agenda | Profesional | Periodo | MVP |
| `REP-011` | Auditoría de accesos a información clínica | Auditor | Usuario, paciente, periodo, acción | MVP |
| `REP-012` | Satisfacción del paciente | Director | Periodo, especialidad, profesional | Fase 3 |
| `REP-013` | Ingresos por especialidad y ARS | Director | Periodo | Fase 3 (módulo 20) |

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-REP-001 | El sistema deberá generar reportes clínicos y administrativos, como asistencia, cancelaciones y carga de trabajo por profesional, exportables a PDF. | S | RF10 |
| RF-REP-002 | El sistema deberá permitir exportar los reportes tabulares a Excel/CSV. | S | GEN |
| RF-REP-003 | El sistema deberá mostrar un tablero con indicadores clave (KPI): tasa de asistencia, tasa de inasistencia, ocupación, tiempo medio de espera, pacientes activos. | S | RF10 |
| RF-REP-004 | El sistema deberá restringir los reportes según el rol: la dirección ve datos agregados sin contenido clínico identificable (RN-002). | M | RN02 |
| RF-REP-005 | El sistema deberá permitir programar el envío periódico de un reporte por correo (sin datos clínicos identificables en el adjunto). | C | GEN |
| RF-REP-006 | El sistema deberá calcular los reportes desde una réplica de lectura o un almacén analítico para no afectar el rendimiento operativo (RNF04). | S | RNF04 |
| RF-REP-007 | El sistema deberá registrar en la bitácora cada exportación de un reporte. | M | RNF09 |

## 4. Definición de indicadores

| Indicador | Fórmula |
|---|---|
| Tasa de inasistencia | citas en estado `inasistencia` / (citas `completada` + `inasistencia`) |
| Tasa de cancelación | citas `cancelada` / citas programadas en el periodo |
| Ocupación de agenda | minutos agendados / minutos disponibles |
| Tiempo medio de espera en sala | promedio (inicio de atención − check-in) |
| Tiempo de respuesta a alertas | promedio (acuse − generación de alerta) |
| Pacientes activos | pacientes con al menos un encuentro en los últimos 90 días o una cita futura |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
