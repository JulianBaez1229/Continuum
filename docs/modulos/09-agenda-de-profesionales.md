---
id: MOD-09
codigo: AGE
titulo: Agenda de profesionales y recursos
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-06, MOD-11, MOD-10]
trazabilidad: ["RF03", "RN04"]
---

# 09 — Agenda de profesionales y recursos

## 1. Objetivo

Que cada profesional defina cuándo, dónde y qué procedimientos atiende, y que la clínica controle también la disponibilidad de consultorios y equipos, de modo que nunca se produzcan solapamientos (RF03, RN-004).

## 2. Conceptos

- **Plantilla de disponibilidad:** bloques recurrentes semanales (p. ej., lunes 8:00–12:00 en Sede Norte, solo Consulta de cardiología y Electrocardiograma).
- **Excepción:** cambio puntual de una fecha (horario extendido, día libre).
- **Bloqueo:** periodo no disponible (vacaciones, congreso, reunión, mantenimiento de equipo).
- **Espacio (slot):** intervalo concreto agendable que resulta de disponibilidad − bloqueos − citas existentes, calculado según la duración del procedimiento.

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-AGE-001 | El sistema deberá permitir a cada profesional (o al coordinador de sede) definir su disponibilidad recurrente por día, horario, sede y procedimientos permitidos, con fechas de vigencia. | M | RF03 |
| RF-AGE-002 | El sistema deberá permitir registrar excepciones y bloqueos con motivo; si un bloqueo afecta citas existentes, deberá listarlas para reprogramarlas antes de confirmar. | M | RF03 |
| RF-AGE-003 | El sistema deberá mostrar la agenda del profesional en vista diaria, semanal y de lista, con el estado de cada cita (código de color y texto). | M | RF03 |
| RF-AGE-004 | El sistema deberá calcular los espacios disponibles a partir de la duración total del procedimiento (preparación + atención + limpieza) definida en el catálogo (RN-013). | M | GEN |
| RF-AGE-005 | El sistema deberá gestionar la disponibilidad de recursos (consultorios, equipos, salas) y solo ofrecer espacios donde el profesional **y** el recurso requerido estén libres. | M | GEN |
| RF-AGE-006 | El sistema deberá permitir procedimientos grupales con capacidad máxima (p. ej., terapia grupal de 8 personas, taller de nutrición). | S | GEN |
| RF-AGE-007 | El sistema deberá permitir a recepción ver en una sola vista la agenda de varios profesionales de una especialidad o sede. | M | GEN |
| RF-AGE-008 | El sistema deberá permitir definir reglas de sobrecupo por profesional (número máximo de sobrecupos por día), deshabilitado por defecto. | C | GEN |
| RF-AGE-009 | El sistema deberá permitir sincronizar la agenda del profesional con su calendario personal (exportación iCal de solo lectura, sin datos clínicos: solo "Ocupado"). | C | EXT |

## 4. Algoritmo de disponibilidad (referencia)

```
espacios(profesional, procedimiento, sede, rango):
  duracion = proc.preparacion + proc.duracion + proc.limpieza
  base = bloques de disponibilidad vigentes del profesional en la sede
         que permitan el procedimiento, dentro del rango
  base = base − feriados de la sede − bloqueos del profesional
  base = base − citas activas del profesional (no canceladas)
  si proc.requiere_recurso:
       recursos = recursos de la sede compatibles y en servicio
       base = intersección(base, unión de huecos libres de cada recurso)
  devolver particionar(base, duracion, paso = granularidad de la sede [5/10/15 min])
```

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-AGE-001 | **Dado** un psicólogo con disponibilidad lunes 8:00–12:00 y sesiones de 50 min + 10 min de cierre, **cuando** se consulta su disponibilidad, **entonces** se muestran 4 espacios (8:00, 9:00, 10:00, 11:00). |
| CA-AGE-002 | **Dado** un ecocardiograma que requiere el ecógrafo, **cuando** el único ecógrafo está ocupado a las 10:00, **entonces** el cardiólogo no aparece disponible a las 10:00 para ese procedimiento aunque tenga su agenda libre. |
| CA-AGE-003 | **Dado** un profesional que registra vacaciones con 6 citas en ese periodo, **cuando** guarda el bloqueo, **entonces** el sistema le muestra las 6 citas y no confirma el bloqueo hasta que se reprogramen o cancelen (con notificación al paciente). |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
