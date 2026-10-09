---
id: MOD-14
codigo: FRM
titulo: Formularios clínicos y evaluaciones
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-11, MOD-15, MOD-16, MOD-17]
trazabilidad: ["RF06", "RN07"]
---

# 14 — Formularios clínicos y evaluaciones

## 1. Objetivo

Generalizar los "cuestionarios emocionales" del documento original a **formularios clínicos configurables** para cualquier especialidad: escalas validadas, registros domiciliarios y encuestas, con puntuación automática y alertas por umbral de riesgo (RF06, RN-007).

## 2. Ejemplos por especialidad

| Especialidad | Formulario | Uso | Umbral de alerta (ejemplo) |
|---|---|---|---|
| Salud mental | PHQ-9 (depresión) | Seguimiento semanal/quincenal | Puntuación ≥ 20 **o** respuesta ≠ 0 en el ítem 9 → alerta inmediata |
| Salud mental | GAD-7 (ansiedad) | Seguimiento | ≥ 15 |
| Medicina general / cardiología | Registro de presión arterial en casa | Diario | Sistólica ≥ 180 o diastólica ≥ 120 |
| Endocrinología / nutrición | Glucemia capilar | Diario | < 70 mg/dL o > 300 mg/dL |
| Fisioterapia / dolor | Escala visual analógica (EVA) 0–10 | Por sesión | ≥ 8 |
| Pediatría | Cuestionario de desarrollo | Por control | Según instrumento |
| Todas | Satisfacción tras la cita | Una vez | — |
| Todas | Preconsulta (motivo, síntomas, medicamentos) | Antes de la cita | — |

> **Nota:** las escalas validadas pueden estar sujetas a derechos de autor o licencias de uso. Antes de cargarlas, verifiquen la licencia de cada instrumento y registren la fuente en la plantilla.

## 3. Estructura de un formulario

```json
{
  "codigo": "FRM-PHQ9",
  "nombre": "Cuestionario de salud del paciente (PHQ-9)",
  "version": "1.0.0",
  "especialidades": ["PSI", "PSQ", "MGE"],
  "preguntas": [
    {"id": "q1", "texto": "…", "tipo": "escala", "opciones": [0, 1, 2, 3]}
  ],
  "puntuacion": {"metodo": "suma", "items": ["q1", "q2", "…", "q9"]},
  "interpretacion": [
    {"min": 0, "max": 4, "nivel": "minimo"},
    {"min": 5, "max": 9, "nivel": "leve"},
    {"min": 10, "max": 14, "nivel": "moderado"},
    {"min": 15, "max": 19, "nivel": "moderadamente_severo"},
    {"min": 20, "max": 27, "nivel": "severo", "alerta": true}
  ],
  "alertas_por_item": [{"item": "q9", "condicion": "> 0", "nivel": "critico"}]
}
```

## 4. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-FRM-001 | El sistema deberá permitir al profesional asignar formularios digitales periódicos a un paciente, definiendo frecuencia, fecha de inicio y fin (RN-007). | M | RF06 |
| RF-FRM-002 | El sistema deberá permitir al paciente completar los formularios asignados desde el portal en no más de 3 pasos desde el inicio de sesión (RNF02). | M | RF06 |
| RF-FRM-003 | El sistema deberá calcular la puntuación y el nivel de interpretación automáticamente al enviar. | M | RF06 |
| RF-FRM-004 | El sistema deberá notificar de inmediato al profesional tratante cuando un resultado supere el umbral de riesgo o active una alerta por ítem (RN-007), por notificación en la plataforma y correo. | M | RN07 |
| RF-FRM-005 | El sistema deberá mostrar al paciente, al enviar un formulario con alerta crítica, las líneas de emergencia y la recomendación de buscar ayuda inmediata (RN-008). | M | RN08 |
| RF-FRM-006 | El sistema deberá permitir al profesional aplicar un formulario durante el encuentro (modo asistido). | S | GEN |
| RF-FRM-007 | El sistema deberá permitir al administrador funcional crear y versionar formularios desde un esquema JSON o un editor visual, y vincularlos a especialidades y procedimientos. | M (JSON) / S (editor) | GEN |
| RF-FRM-008 | El sistema deberá conservar la versión del formulario con la que se respondió cada envío. | M | RN-011 |
| RF-FRM-009 | El sistema deberá permitir asignar formularios automáticamente al completar un procedimiento (`formularios_sugeridos` del catálogo), sujeto a confirmación del profesional. | S | GEN |
| RF-FRM-010 | El sistema deberá registrar el acuse de recibo de cada alerta por parte del profesional, y escalar al coordinador clínico si no se atiende en el plazo configurado (por defecto 4 horas laborables). | S | RN07 |

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-FRM-001 | **Dado** un PHQ-9 con el ítem 9 respondido con valor 1, **cuando** el paciente lo envía, **entonces** el tratante recibe una alerta crítica en menos de 1 minuto y el paciente ve las líneas de emergencia. |
| CA-FRM-002 | **Dado** un formulario semanal asignado, **cuando** pasa la fecha sin respuesta, **entonces** el paciente recibe un recordatorio (módulo 17) y el profesional lo ve como "pendiente". |
| CA-FRM-003 | **Dado** una nueva versión de un formulario, **cuando** se consultan respuestas antiguas, **entonces** se muestran con las preguntas de la versión original. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
