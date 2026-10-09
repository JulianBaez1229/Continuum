---
id: MOD-13
codigo: TRA
titulo: Órdenes, tratamientos y medicamentos
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-12, MOD-11, MOD-03]
trazabilidad: ["RF05", "RN06"]
---

# 13 — Órdenes, tratamientos y medicamentos

## 1. Objetivo

Registrar todas las indicaciones del profesional —tratamientos, terapias, medicamentos, estudios y referimientos— con su detalle estructurado, de modo que sean consultables, imprimibles y trazables (generalización de RF05).

## 2. Tipos de orden

| Tipo | Detalle estructurado | Ejemplo |
|---|---|---|
| `medicamento` | Principio activo, presentación, dosis, vía, frecuencia, duración, cantidad, indicaciones | Sertralina 50 mg, 1 tableta VO cada 24 h por 30 días |
| `tratamiento` / `terapia` | Tipo, objetivo, número de sesiones, frecuencia, procedimiento del catálogo | Psicoterapia cognitivo-conductual, 12 sesiones semanales |
| `laboratorio` | Pruebas (catálogo), indicación clínica, urgencia | Hemograma, perfil lipídico |
| `imagen` | Estudio, región, indicación | Radiografía de tórax PA |
| `referimiento` | Especialidad destino, motivo, urgencia | Referir a Psiquiatría por evaluación farmacológica |
| `indicacion_general` | Texto estructurado | Dieta hiposódica, reposo relativo 3 días |
| `certificado` | Tipo (médico, incapacidad, asistencia), periodo | Certificado de asistencia |

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-TRA-001 | El sistema deberá permitir a los profesionales registrar tratamientos, terapias y medicamentos indicados, con dosis, frecuencia y duración. | M | RF05 |
| RF-TRA-002 | El sistema deberá permitir solo al profesional tratante (o del equipo de atención con habilitación) emitir, modificar o suspender órdenes (RN-006). | M | RN06 |
| RF-TRA-003 | El sistema deberá impedir que profesionales sin facultad de prescribir (p. ej., psicólogos, nutricionistas, fisioterapeutas) emitan órdenes de medicamentos (RN-018). | M | GEN |
| RF-TRA-004 | El sistema deberá alertar si el medicamento coincide con una alergia registrada del paciente. | M | GEN |
| RF-TRA-005 | El sistema deberá alertar sobre duplicidad terapéutica (mismo principio activo ya activo). | S | GEN |
| RF-TRA-006 | El sistema deberá mantener la lista de medicamentos activos del paciente y permitir suspender uno con motivo. Las órdenes no se eliminan; se anulan con motivo. | M | RN06 |
| RF-TRA-007 | El sistema deberá generar la receta o la orden en PDF con datos del profesional (nombre, exequátur, especialidad), firma digital o espacio para firma, y código de verificación. | S | GEN |
| RF-TRA-008 | El sistema deberá permitir crear una cita o una serie de citas directamente desde una orden de tratamiento (p. ej., 12 sesiones). | S | GEN |
| RF-TRA-009 | El sistema deberá permitir registrar el resultado de un estudio y vincularlo a su orden. | S | GEN |
| RF-TRA-010 | El sistema deberá permitir al paciente ver en el portal las órdenes que el profesional marque como "liberadas al paciente". | S | RF02 |
| RF-TRA-011 | El sistema deberá exigir motivo y registrar doble autoría para medicamentos controlados (psicotrópicos y estupefacientes) según la normativa vigente. | S | GEN |
| RF-TRA-012 | El sistema deberá integrar un vademécum o catálogo de medicamentos cargable (CSV) con principio activo, presentaciones y concentraciones. | S | GEN |
| RF-TRA-013 | El sistema deberá ofrecer verificación de interacciones medicamentosas mediante una base de datos externa. | W (Fase 3) | EXT |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-TRA-001 | **Dado** un psicólogo, **cuando** abre el panel de órdenes, **entonces** no aparece el tipo "medicamento". |
| CA-TRA-002 | **Dado** un paciente alérgico a la amoxicilina, **cuando** el médico prescribe amoxicilina, **entonces** aparece una alerta que exige justificación para continuar. |
| CA-TRA-003 | **Dado** una orden de 10 sesiones de fisioterapia, **cuando** se usa "Agendar serie", **entonces** recepción ve un asistente con las 10 fechas propuestas. |
| CA-TRA-004 | **Dado** una receta impresa, **cuando** se escanea su código de verificación, **entonces** se puede comprobar que existe y no ha sido anulada (sin mostrar datos clínicos). |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
