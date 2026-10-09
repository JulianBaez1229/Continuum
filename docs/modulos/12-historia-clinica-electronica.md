---
id: MOD-12
codigo: HCE
titulo: Historia clínica electrónica
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-03, MOD-08, MOD-11, MOD-13, MOD-23]
trazabilidad: ["RF04", "RN01", "RN06"]
---

# 12 — Historia clínica electrónica (HCE)

## 1. Objetivo

Registrar y consultar la historia clínica de cada paciente —notas de encuentro, diagnósticos, antecedentes, evolución y documentos— con **plantillas específicas por especialidad**, garantizando autoría, inmutabilidad y confidencialidad (RF04, RN-001, RN-006).

## 2. Estructura de la HCE

```
Paciente
├── Resumen clínico (siempre visible para el equipo autorizado)
│   ├── Alergias  ⚠ (banner permanente)
│   ├── Problemas activos / diagnósticos vigentes
│   ├── Medicamentos activos
│   └── Antecedentes (personales, familiares, quirúrgicos, hábitos)
├── Episodios de atención (por especialidad / problema)
│   └── Encuentros
│       ├── Signos vitales y triaje (asistente clínico)
│       ├── Nota clínica (plantilla de la especialidad)
│       ├── Diagnósticos (CIE-10)
│       ├── Órdenes (módulo 13)
│       └── Adendas
├── Resultados y documentos (laboratorio, imágenes, consentimientos, referencias)
└── Línea de tiempo (vista cronológica unificada filtrable)
```

## 3. Plantillas de nota clínica

Una plantilla es un **esquema JSON versionado** (ADR-006) con secciones y campos tipados. El sistema genera el formulario a partir del esquema; no hay pantallas programadas por especialidad.

| Tipo de campo | Ejemplos |
|---|---|
| Texto corto / largo | Motivo de consulta, evolución |
| Número con unidad y rango | Presión arterial, peso, frecuencia cardíaca |
| Lista / selección múltiple | Estado de ánimo observado, tipo de dolor |
| Escala | EVA 0–10 |
| Fecha | Fecha de última menstruación |
| Diagnóstico CIE-10 | Buscador con lista frecuente de la especialidad |
| Componente especializado | **Odontograma**, mapa corporal, curva de crecimiento (pediatría), genograma (Fase 3) |
| Cálculo | IMC, puntuación de riesgo |

**Plantillas iniciales sugeridas**

| Código | Especialidad | Estructura |
|---|---|---|
| `PLT-MGE-001` | Medicina general | SOAP (Subjetivo, Objetivo, Análisis, Plan) |
| `PLT-PSI-001` | Psicología | Motivo, observación, intervención, plan terapéutico, riesgo |
| `PLT-PSQ-001` | Psiquiatría | Examen mental, diagnóstico, plan farmacológico, riesgo |
| `PLT-CAR-001` | Cardiología | Anamnesis, examen cardiovascular, ECG, plan |
| `PLT-ODO-001` | Odontología | Odontograma, hallazgos por pieza, plan de tratamiento |
| `PLT-FIS-001` | Fisioterapia | Evaluación funcional, rango de movimiento, objetivos, sesión |
| `PLT-PED-001` | Pediatría | Crecimiento y desarrollo, vacunas, examen físico, plan |
| `PLT-NUT-001` | Nutrición | Antropometría, recordatorio 24 h, plan alimentario |

Las plantillas se documentan con [`plantillas/plantilla-formulario-clinico.md`](../plantillas/plantilla-formulario-clinico.md) (sirve para plantillas de nota y formularios).

## 4. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-HCE-001 | El sistema deberá permitir registrar y consultar el historial clínico de cada paciente: notas de encuentro, diagnósticos, antecedentes y evolución. | M | RF04 |
| RF-HCE-002 | El sistema deberá presentar la nota clínica según la plantilla asociada al procedimiento de la cita, y permitir cambiar a otra plantilla de la misma especialidad. | M | GEN |
| RF-HCE-003 | El sistema deberá guardar automáticamente el borrador de una nota cada 30 segundos. | M | GEN |
| RF-HCE-004 | El sistema deberá permitir firmar la nota; una nota firmada es inmutable, guarda la versión de la plantilla usada y un hash de su contenido. | M | RN06 |
| RF-HCE-005 | El sistema deberá permitir corregir una nota firmada solo mediante adendas con motivo, visibles junto al original (RN-006). | M | RN06 |
| RF-HCE-006 | El sistema deberá restringir el acceso a notas y diagnósticos al profesional tratante y al equipo de atención del episodio (RN-001). | M | RN01 |
| RF-HCE-007 | El sistema deberá aplicar restricciones adicionales a episodios de categorías sensibles: otros profesionales del equipo solo verán el resumen (diagnóstico y plan) salvo permiso explícito del tratante (RN-016). | M | RN01 |
| RF-HCE-008 | El sistema deberá mostrar las alergias del paciente como alerta permanente en toda pantalla clínica. | M | GEN |
| RF-HCE-009 | El sistema deberá permitir codificar diagnósticos con CIE-10, marcando principal y secundarios, y mantener una lista de problemas activos. | M | GEN |
| RF-HCE-010 | El sistema deberá permitir al asistente clínico registrar signos vitales y triaje antes del encuentro. | S | GEN |
| RF-HCE-011 | El sistema deberá permitir adjuntar documentos (PDF, imágenes) de hasta 20 MB, con escaneo antimalware. | S | GEN |
| RF-HCE-012 | El sistema deberá ofrecer una línea de tiempo unificada del paciente filtrable por especialidad, tipo de registro y fecha, mostrando solo lo que el usuario tiene permiso de ver. | S | RF04 |
| RF-HCE-013 | El sistema deberá permitir imprimir o exportar a PDF una nota o un resumen clínico, con marca de agua de confidencialidad y registro en la bitácora. | S | RF10 |
| RF-HCE-014 | El sistema deberá permitir al profesional cerrar el episodio con una nota de alta. | S | GEN |
| RF-HCE-015 | El sistema deberá permitir registrar referimientos entre especialidades, agregando al profesional receptor al equipo de atención cuando lo acepte. | S | GEN |
| RF-HCE-016 | El sistema deberá permitir crear y versionar plantillas de nota mediante un editor visual sin código. | C (Fase 2) | GEN |

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-HCE-001 | **Dado** una nota firmada, **cuando** el autor intenta editarla, **entonces** el sistema solo ofrece "Agregar adenda". |
| CA-HCE-002 | **Dado** un episodio de psicología marcado como sensible, **cuando** el médico general del mismo paciente (miembro del equipo) lo consulta, **entonces** ve el diagnóstico y el plan, pero no las notas de sesión. |
| CA-HCE-003 | **Dado** un paciente alérgico a la penicilina, **cuando** cualquier profesional abre su HCE, **entonces** ve el banner de alergia en la parte superior. |
| CA-HCE-004 | **Dado** una cita de odontología, **cuando** el odontólogo inicia el encuentro, **entonces** se abre la plantilla `PLT-ODO-001` con el odontograma. |
| CA-HCE-005 | **Dado** cualquier apertura de una nota, **cuando** se consulta la bitácora, **entonces** figura el usuario, la fecha, la hora y el paciente. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
