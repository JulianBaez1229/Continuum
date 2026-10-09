---
id: MOD-17
codigo: NOT
titulo: Notificaciones
version: 1.0.0
estado: Borrador
fase: Fase 2 (correo básico en MVP)
responsable: Por asignar
dependencias: [MOD-10, MOD-14, MOD-25]
trazabilidad: ["RF08"]
---

# 17 — Notificaciones

## 1. Objetivo

Enviar recordatorios y avisos automáticos por los canales adecuados para reducir inasistencias y asegurar que las alertas clínicas lleguen a tiempo (RF08).

## 2. Catálogo de notificaciones

| Código | Evento | Destinatario | Canales | Fase |
|---|---|---|---|---|
| `NOT-CIT-CONF` | Cita programada | Paciente | Correo, plataforma | MVP |
| `NOT-CIT-REC` | Recordatorio de cita (48 h y 2 h, configurable) | Paciente | Correo, SMS/WhatsApp, plataforma | MVP (correo) / Fase 2 |
| `NOT-CIT-CANC` | Cita cancelada o reprogramada | Paciente, profesional | Correo, plataforma | MVP |
| `NOT-CIT-ESPERA` | Espacio disponible de la lista de espera | Paciente | SMS/WhatsApp, correo | Fase 2 |
| `NOT-FRM-PEND` | Formulario pendiente | Paciente | Correo, plataforma | Fase 2 |
| `NOT-FRM-ALERTA` | Umbral de riesgo superado | Profesional tratante | Plataforma, correo (sin datos clínicos en el texto) | MVP |
| `NOT-FRM-ESCALA` | Alerta sin atender | Coordinador clínico | Plataforma, correo | Fase 2 |
| `NOT-MSG-NUEVO` | Mensaje nuevo | Paciente / profesional | Plataforma, correo (sin contenido) | Fase 2 |
| `NOT-HAB-VENCE` | Licencia por vencer | Profesional, administrador | Correo | Fase 2 |
| `NOT-SEG-EMERG` | Acceso de emergencia | Tratante, oficial de privacidad | Plataforma, correo | Fase 2 |
| `NOT-RED-ACCESO` | Familiar autorizado / revocado | Paciente, familiar | Correo | Fase 3 |

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-NOT-001 | El sistema deberá enviar recordatorios automáticos de citas y formularios pendientes por correo electrónico y dentro de la plataforma. | S | RF08 |
| RF-NOT-002 | El sistema deberá permitir configurar por organización los canales activos, los tiempos de recordatorio y el horario permitido de envío (p. ej., 8:00–20:00). | S | RF08 |
| RF-NOT-003 | El sistema deberá permitir enviar recordatorios por SMS o WhatsApp Business mediante un proveedor integrado (módulo 25). | C | EXT |
| RF-NOT-004 | El sistema **no deberá incluir información clínica** (diagnósticos, nombre de procedimientos sensibles, resultados) en el texto de correos, SMS o WhatsApp; para procedimientos de categorías sensibles se usará un texto genérico ("Tiene una cita en Clínica X"). | M | RN01, RN-016 |
| RF-NOT-005 | El sistema deberá permitir al paciente responder "Confirmar" o "Cancelar" desde el enlace del recordatorio, con un token de un solo uso. | S | GEN |
| RF-NOT-006 | El sistema deberá gestionar plantillas de mensaje editables por organización con variables (`{{paciente.nombre}}`, `{{cita.fecha}}`, `{{sede.direccion}}`). | S | GEN |
| RF-NOT-007 | El sistema deberá registrar el estado de cada envío (en cola, enviado, entregado, fallido) y reintentar hasta 3 veces. | S | GEN |
| RF-NOT-008 | El sistema deberá respetar las preferencias de canal del paciente y su baja de comunicaciones no esenciales. | S | RNF07 |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-NOT-001 | **Dado** una cita de psiquiatría, **cuando** se envía el recordatorio por SMS, **entonces** el texto no menciona "psiquiatría". |
| CA-NOT-002 | **Dado** una cita el viernes a las 10:00, **cuando** son las 10:00 del miércoles, **entonces** el paciente recibe el recordatorio de 48 horas. |
| CA-NOT-003 | **Dado** un fallo del proveedor de correo, **cuando** pasan 3 reintentos, **entonces** el envío queda como "fallido" y aparece en el reporte de notificaciones. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
