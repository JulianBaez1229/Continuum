---
id: MOD-22
codigo: BIE
titulo: Recursos educativos y de bienestar
version: 1.0.0
estado: Borrador
fase: Fase 3
responsable: Por asignar
dependencias: [MOD-11, MOD-16]
trazabilidad: ["RF11"]
---

# 22 — Recursos educativos y de bienestar

## 1. Objetivo

Generalizar la "biblioteca de hábitos saludables" a una **biblioteca de recursos educativos por especialidad** que el profesional asigna al paciente: guías, ejercicios, videos, planes y material de apoyo (RF11).

## 2. Ejemplos

| Especialidad | Recurso |
|---|---|
| Salud mental | Técnicas de respiración, higiene del sueño, registro de pensamientos |
| Fisioterapia | Rutina de ejercicios en casa con imágenes o video |
| Nutrición | Plan alimentario, lista de intercambios |
| Cardiología | Cómo medir la presión en casa, dieta baja en sodio |
| Odontología | Cuidados después de una extracción |
| Pediatría | Calendario de vacunación, signos de alarma |
| Todas | Preparación para un procedimiento (enlazado con `instrucciones_paciente` del catálogo) |

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-BIE-001 | El sistema deberá ofrecer una biblioteca de recursos (texto enriquecido, PDF, imagen, enlace a video) clasificada por especialidad y etiquetas, que el profesional puede asignar al paciente. | C | RF11 |
| RF-BIE-002 | El sistema deberá permitir al administrador funcional y a los profesionales autorizados crear, revisar y publicar recursos con versión y fecha de revisión. | C | RF11 |
| RF-BIE-003 | El sistema deberá permitir asignar un recurso con instrucciones personalizadas y una frecuencia (p. ej., "Rutina A, 3 veces por semana"). | C | RF11 |
| RF-BIE-004 | El sistema deberá permitir al paciente marcar un recurso como leído o una actividad como realizada, y mostrar esta adherencia al profesional. | C | GEN |
| RF-BIE-005 | El sistema deberá permitir asociar recursos a procedimientos del catálogo para asignarlos automáticamente al agendar o completar. | C | GEN |
| RF-BIE-006 | El sistema deberá mostrar la fuente y la fecha de revisión de cada recurso, y alertar cuando un recurso tenga más de 24 meses sin revisión. | C | GEN |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-BIE-001 | **Dado** un fisioterapeuta que asigna una rutina 3 veces por semana, **cuando** el paciente la marca como realizada, **entonces** el fisioterapeuta ve la adherencia en el seguimiento. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
