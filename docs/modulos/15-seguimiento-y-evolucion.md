---
id: MOD-15
codigo: SEG
titulo: Seguimiento y evolución del paciente
version: 1.0.0
estado: Borrador
fase: Fase 2
responsable: Por asignar
dependencias: [MOD-12, MOD-14]
trazabilidad: ["RF07"]
---

# 15 — Seguimiento y evolución del paciente

## 1. Objetivo

Mostrar al profesional la **evolución de los indicadores clínicos** de cada paciente —puntuaciones de formularios, signos vitales, medidas antropométricas, adherencia— en gráficos y alertas, para que llegue a cada encuentro con información completa (generalización de RF07).

## 2. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-SEG-001 | El sistema deberá mostrar la evolución del paciente mediante gráficos de línea generados a partir de sus formularios y de los valores numéricos de sus notas clínicas (p. ej., presión arterial, peso, EVA). | S | RF07 |
| RF-SEG-002 | El sistema deberá superponer en el gráfico las bandas de interpretación del instrumento (mínimo, leve, moderado, severo) y marcar los encuentros y cambios de tratamiento. | S | RF07 |
| RF-SEG-003 | El sistema deberá ofrecer al profesional un **panel de seguimiento** con sus pacientes activos ordenados por prioridad: alertas sin atender, formularios vencidos, empeoramiento sostenido, sin cita futura. | S | GEN |
| RF-SEG-004 | El sistema deberá detectar empeoramiento sostenido (tres mediciones consecutivas con tendencia negativa, configurable por formulario) y marcarlo en el panel. | C | GEN |
| RF-SEG-005 | El sistema deberá calcular la adherencia a formularios (respondidos / asignados) y a citas (asistidas / programadas) por paciente. | S | GEN |
| RF-SEG-006 | El sistema deberá permitir definir **objetivos terapéuticos** medibles por episodio (p. ej., PHQ-9 < 10, presión < 130/80) y mostrar el avance. | C | GEN |
| RF-SEG-007 | El sistema deberá permitir al paciente ver en el portal una versión simplificada de su evolución, si el profesional lo habilita para ese indicador. | C | RF07 |
| RF-SEG-008 | El sistema deberá permitir exportar el gráfico de evolución a PDF para incluirlo en un informe. | C | RF10 |

## 3. Panel de seguimiento (definición)

| Columna | Fuente |
|---|---|
| Paciente | Módulo 08 |
| Prioridad (crítica, alta, media) | Alertas RN-007 / empeoramiento / vencidos |
| Último indicador y tendencia (↑ ↓ →) | Módulo 14 |
| Formularios pendientes | Módulo 14 |
| Próxima cita | Módulo 10 |
| Adherencia | RF-SEG-005 |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-SEG-001 | **Dado** un paciente con 6 PHQ-9 respondidos, **cuando** el psicólogo abre su seguimiento, **entonces** ve un gráfico con 6 puntos, las bandas de severidad y las fechas de sesión. |
| CA-SEG-002 | **Dado** un paciente de cardiología con presión registrada en casa durante 30 días, **cuando** el cardiólogo abre el encuentro, **entonces** ve el gráfico de sistólica y diastólica con el objetivo marcado. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
