---
codigo_interno: PRC-ODO-0003
nombre: Extracción dental simple
nombre_paciente: Extracción de una pieza dental
especialidad: ODO
tipo: procedimiento_menor
duracion_min: 45
preparacion_min: 10
limpieza_min: 15
modalidades: [presencial]
capacidad: 1
recurso_requerido: REC-SILLON-DENTAL
requisitos_previos: [REQ-CONSENT:CONS-ODO-EXTRACCION, REQ-ACOMPANANTE]
plantilla_nota: PLT-ODO-001
formularios_sugeridos: [FRM-EVA-DOLOR]
autoagendable: false
plazo_cancelacion_horas: 48
categoria_sensible: false
codigos_externos:
  - {sistema: CDT, codigo: "D7140"}
precio_referencia:
version: 1.0.0
vigente_desde: 2026-10-02
vigente_hasta:
estado: publicado
revisado_por: "Por asignar (jefe de Odontología)"
---

# Extracción dental simple

## Descripción clínica

Extracción de una pieza dental erupcionada sin necesidad de colgajo ni osteotomía. Requiere evaluación previa (`PRC-ODO-0001`) y consentimiento informado firmado. El plazo de cancelación es de 48 horas por la preparación del sillón y el material.

## Instrucciones para el paciente

- Antes de la cita: informe a su odontólogo los medicamentos que toma (en especial anticoagulantes). Firme el consentimiento informado en el portal o en recepción.
- El día de la cita: coma algo ligero antes de venir y venga acompañado.
- Después de la cita: siga las indicaciones de cuidado que recibirá en el portal. Si el dolor o el sangrado aumentan, comuníquese con la clínica.

## Recursos y personal

| Elemento | Detalle |
|---|---|
| Recurso | Sillón dental |
| Profesionales habilitados | Odontólogo general o cirujano bucal |
| Personal de apoyo | Asistente dental |
| Materiales | Anestesia local, instrumental de extracción, gasas |

## Registro clínico

- Plantilla de nota: `PLT-ODO-001`; marcar la pieza en el odontograma y su nuevo estado ("ausente por extracción").
- El inicio del encuentro se bloquea si el consentimiento `CONS-ODO-EXTRACCION` no está firmado (RN-014).

## Seguimiento

- Formulario de dolor (EVA) a las 24 y 72 horas.
- Control a los 7 días si el profesional lo indica.

## Historial

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Alta (ejemplo) |
