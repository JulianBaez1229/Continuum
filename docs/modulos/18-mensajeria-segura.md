---
id: MOD-18
codigo: MSG
titulo: Mensajería segura
version: 1.0.0
estado: Borrador
fase: Fase 2
responsable: Por asignar
dependencias: [MOD-03, MOD-16, MOD-17, MOD-24]
trazabilidad: ["RF09", "RN08"]
---

# 18 — Mensajería segura

## 1. Objetivo

Proveer un canal interno y cifrado entre el paciente y su equipo de atención que sustituya el uso de mensajería personal, con reglas claras de que **no es un canal de emergencias** (RF09, RN-008).

## 2. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-MSG-001 | El sistema deberá proveer un canal interno y cifrado de mensajes entre el paciente y su profesional tratante. | S | RF09 |
| RF-MSG-002 | El sistema deberá organizar los mensajes en hilos por episodio de atención, de modo que un paciente con varias especialidades tenga conversaciones separadas. | S | GEN |
| RF-MSG-003 | El sistema deberá mostrar, antes de que el paciente escriba, un aviso visible de que el canal no es para emergencias y las líneas de emergencia (RN-008). | M | RN08 |
| RF-MSG-004 | El sistema deberá permitir al profesional configurar su tiempo habitual de respuesta (p. ej., "Respondo en 2 días hábiles") y su horario de mensajería; fuera de horario se muestra un aviso automático. | S | GEN |
| RF-MSG-005 | El sistema deberá permitir adjuntar imágenes y PDF (máx. 10 MB) con escaneo antimalware. | C | GEN |
| RF-MSG-006 | El sistema deberá permitir al profesional delegar la bandeja en un asistente clínico para mensajes administrativos, sin acceso a hilos de categorías sensibles. | C | GEN |
| RF-MSG-007 | El sistema deberá permitir al profesional incorporar un mensaje relevante a la historia clínica como nota. | S | RF04 |
| RF-MSG-008 | El sistema deberá permitir que el profesional cierre o deshabilite la mensajería con un paciente, con aviso al paciente. | S | GEN |
| RF-MSG-009 | El sistema deberá conservar los mensajes con la misma retención de la historia clínica y registrarlos en la bitácora. Los mensajes no se pueden editar ni eliminar después de enviados. | M | RNF09 |
| RF-MSG-010 | El sistema deberá detectar palabras clave de riesgo configurables en mensajes del paciente y mostrar inmediatamente al paciente las líneas de emergencia, además de marcar el hilo como prioritario para el profesional. | C | RN08 |

## 3. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-MSG-001 | **Dado** un paciente que abre la mensajería, **cuando** va a escribir, **entonces** ve el aviso de "no es un canal de emergencias" con los números de ayuda. |
| CA-MSG-002 | **Dado** un paciente atendido en Cardiología y Psicología, **cuando** abre sus mensajes, **entonces** ve dos hilos separados. |
| CA-MSG-003 | **Dado** un mensaje enviado, **cuando** el remitente intenta editarlo, **entonces** el sistema no lo permite. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
