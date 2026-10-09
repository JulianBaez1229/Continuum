---
codigo_interno: PRC-CAR-0002
nombre: Electrocardiograma de 12 derivaciones con interpretación
nombre_paciente: Electrocardiograma
especialidad: CAR
tipo: estudio
duracion_min: 15
preparacion_min: 5
limpieza_min: 5
modalidades: [presencial]
capacidad: 1
recurso_requerido: REC-ELECTROCARDIOGRAFO
requisitos_previos: [REQ-ORDEN]
plantilla_nota: PLT-CAR-001
formularios_sugeridos: []
autoagendable: false
plazo_cancelacion_horas:
categoria_sensible: false
codigos_externos:
  - {sistema: CPT, codigo: "93000"}
precio_referencia:
version: 1.0.0
vigente_desde: 2026-10-02
vigente_hasta:
estado: publicado
revisado_por: "Por asignar (jefe de Cardiología)"
---

# Electrocardiograma de 12 derivaciones con interpretación

## Descripción clínica

Registro de la actividad eléctrica del corazón en reposo con 12 derivaciones, seguido de la interpretación por el cardiólogo. Puede realizarse como estudio independiente (con orden) o dentro de una consulta de cardiología.

## Instrucciones para el paciente

- Antes de la cita: traiga la orden médica. Evite aplicar cremas o aceites en el pecho, brazos y piernas.
- El día de la cita: use ropa cómoda que permita descubrir el pecho fácilmente.
- Después de la cita: puede retomar sus actividades normales. El resultado estará disponible cuando el cardiólogo lo libere en el portal.

## Recursos y personal

| Elemento | Detalle |
|---|---|
| Recurso | Electrocardiógrafo de 12 derivaciones |
| Profesionales habilitados | Cardiólogo (interpretación) |
| Personal de apoyo | Asistente clínico o enfermería (toma del trazado) |
| Materiales | Electrodos desechables, papel o salida digital |

## Registro clínico

- Plantilla de nota: `PLT-CAR-001`, sección ECG (ritmo, frecuencia, eje, intervalos, hallazgos, conclusión).
- Adjuntar el trazado en PDF como documento del encuentro.

## Seguimiento

- Si hay hallazgos relevantes, el cardiólogo genera un referimiento o agenda una consulta (`PRC-CAR-0001`).

## Historial

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Alta (ejemplo) |
