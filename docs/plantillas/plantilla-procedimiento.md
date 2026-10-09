---
# ─────────────────────────────────────────────────────────────
# PLANTILLA DE PROCEDIMIENTO — Continuum
# Copiar a: catalogos/procedimientos/<ESP>/<codigo_interno>.md
# Campos con * son obligatorios. Ver módulo 11, sección 4.
# Cada campo de este bloque corresponde a una columna de
# catalogos/importacion/procedimientos.csv
# ─────────────────────────────────────────────────────────────
codigo_interno: PRC-XXX-0000        # * PRC-<ESP>-<nnnn>, nunca se reutiliza
nombre: ""                          # * Nombre para el personal
nombre_paciente: ""                 #   Nombre sencillo para el portal
especialidad: XXX                   # * Código de 3 letras
tipo: consulta                      # * consulta | control | sesion | procedimiento_menor | estudio | grupal
duracion_min: 30                    # * 5–600
preparacion_min: 0
limpieza_min: 0
modalidades: [presencial]           # * presencial | virtual | telefonica
capacidad: 1                        #   >1 solo para grupales
recurso_requerido: REC-CONSULTORIO  #   Tipo de recurso o vacío
requisitos_previos: []              #   REQ-AYUNO:8 | REQ-ORDEN | REQ-CONSENT:<codigo> | REQ-ESTUDIO:<desc> | REQ-ACOMPANANTE | REQ-TUTOR | REQ-AUTORIZ-ARS
plantilla_nota: PLT-XXX-001         # * Plantilla de nota clínica
formularios_sugeridos: []           #   FRM-...
autoagendable: false
plazo_cancelacion_horas:            #   Vacío = valor de la organización (RN-005)
categoria_sensible:                 #   Vacío = hereda de la especialidad (RN-016)
codigos_externos: []                #   - {sistema: CPT, codigo: "99213"}
precio_referencia:                  #   Fase 3
version: 1.0.0                      # * semver
vigente_desde: 2026-10-02           # *
vigente_hasta:
estado: borrador                    # * borrador | publicado | inactivo
revisado_por: ""                    #   Jefe de especialidad / profesional que valida
---

# <Nombre del procedimiento>

## Descripción clínica

_Qué es el procedimiento, para qué se indica y en qué casos no aplica. Lenguaje técnico para el personal._

## Instrucciones para el paciente

_Texto que el paciente recibirá en la confirmación de la cita y en el portal. Lenguaje sencillo, en segunda persona._

- Antes de la cita:
- El día de la cita:
- Después de la cita:

## Recursos y personal

| Elemento | Detalle |
|---|---|
| Recurso | |
| Profesionales habilitados (tipo) | |
| Personal de apoyo | |
| Materiales | |

## Registro clínico

- Plantilla de nota: `PLT-XXX-001`
- Campos que el procedimiento exige completar en la nota:
- Diagnósticos frecuentes (CIE-10):

## Seguimiento

- Formularios sugeridos al finalizar:
- Procedimiento de control sugerido y plazo:

## Referencias

- _Guía clínica, protocolo interno o fuente del código externo._

## Historial

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 1.0.0 | 2026-10-02 | | Alta |
