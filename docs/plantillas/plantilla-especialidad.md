---
# PLANTILLA DE ESPECIALIDAD — Continuum
# Copiar a: catalogos/especialidades/<CODIGO>.md
codigo: XXX                 # * 3 letras, único
nombre: ""                  # *
descripcion: ""
categoria_sensible: false   # * true para salud mental, VIH, salud sexual/reproductiva, adicciones, genética (RN-016)
tipos_profesional: []       # * p. ej. [medico, psicologo, odontologo, fisioterapeuta, nutricionista, enfermero]
facultad_prescribir: false  # * ¿Sus profesionales pueden prescribir medicamentos? (RN-018)
estado: borrador            # borrador | publicado | inactivo
version: 1.0.0
responsable_clinico: ""     # Jefe de la especialidad que valida el contenido
---

# Especialidad: <Nombre>

## 1. Alcance en la clínica

_Qué atiende esta especialidad en la organización, en qué sedes y con qué profesionales._

## 2. Procedimientos

| Código | Nombre | Tipo | Duración (min) | Recurso | Requisitos | Autoagendable |
|---|---|---|---|---|---|---|
| PRC-XXX-0001 | | consulta | | | | |
| PRC-XXX-0002 | | control | | | | |

> Cada procedimiento se documenta con `plantillas/plantilla-procedimiento.md` y se agrega al CSV de importación.

## 3. Plantillas de nota clínica

| Código | Nombre | Estructura principal | Componentes especiales |
|---|---|---|---|
| PLT-XXX-001 | | | |

## 4. Formularios clínicos

| Código | Nombre | Uso | Umbral de alerta | Licencia verificada |
|---|---|---|---|---|
| FRM- | | | | ☐ |

## 5. Diagnósticos frecuentes (CIE-10)

| Código | Descripción |
|---|---|
| | |

## 6. Recursos físicos necesarios

| Tipo de recurso | Cantidad por sede | Observaciones |
|---|---|---|
| | | |

## 7. Reglas particulares

_Reglas propias de la especialidad que se configuran como parámetros (p. ej., plazo de cancelación de 48 h para procedimientos largos, presencia del tutor obligatoria)._

## 8. Lista de verificación para publicar

- [ ] Especialidad creada en el catálogo
- [ ] Procedimientos importados y validados
- [ ] Plantillas de nota publicadas
- [ ] Formularios publicados (licencias verificadas)
- [ ] Recursos registrados en cada sede
- [ ] Profesionales habilitados
- [ ] Prueba completa en preproducción (agendar → atender → firmar)
- [ ] Entrada en el CHANGELOG

## Historial

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 1.0.0 | 2026-10-02 | | Alta |
