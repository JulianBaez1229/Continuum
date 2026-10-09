---
id: MOD-11
codigo: CAT
titulo: Catálogo de especialidades y procedimientos
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-05, MOD-06, MOD-12, MOD-14]
trazabilidad: ["GEN", "RN-011", "RN-012", "RN-013", "RN-014"]
---

# 11 — Catálogo de especialidades y procedimientos

## 1. Objetivo

Este módulo es el que convierte a Continuum en una plataforma **multiespecialidad**. Define cómo se **cargan, versionan y publican** especialidades, procedimientos médicos, recursos requeridos, plantillas de nota y formularios, para que una clínica agregue una especialidad nueva **sin cambiar código**.

## 2. Qué contiene el catálogo

```
Especialidad (p. ej., Cardiología)
├── Procedimientos           ← consulta, control, ECG, ecocardiograma…
│   ├── Duración, preparación, limpieza
│   ├── Modalidades permitidas (presencial / virtual / telefónica)
│   ├── Recurso requerido (tipo)
│   ├── Requisitos previos (ayuno, orden, consentimiento, estudios)
│   ├── Plantilla de nota clínica asociada
│   ├── Formularios sugeridos
│   ├── Códigos externos (CPT, SNOMED CT, tarifario ARS)
│   └── Precio de referencia (Fase 3)
├── Plantillas de nota clínica (módulo 12)
├── Formularios clínicos (módulo 14)
├── Diagnósticos frecuentes (subconjunto CIE-10)
└── Recursos educativos (módulo 22)
```

## 3. Codificación interna

| Elemento | Formato | Ejemplo |
|---|---|---|
| Especialidad | 3 letras | `PSI`, `PSQ`, `MGE`, `CAR`, `ODO`, `PED`, `FIS`, `NUT`, `GIN`, `DER` |
| Procedimiento | `PRC-<ESP>-<nnnn>` | `PRC-CAR-0002` |
| Plantilla de nota | `PLT-<ESP>-<nnn>` | `PLT-PSI-001` |
| Formulario | `FRM-<nombre corto>` | `FRM-PHQ9`, `FRM-EVA-DOLOR` |
| Tipo de recurso | `REC-<nombre>` | `REC-CONSULTORIO`, `REC-SILLON-DENTAL`, `REC-ECOGRAFO` |

El código interno nunca cambia ni se reutiliza. Los códigos externos son atributos y pueden haber varios por procedimiento.

## 4. Campos de un procedimiento

| Campo | Obligatorio | Tipo | Descripción |
|---|---|---|---|
| `codigo_interno` | ✔ | texto | `PRC-<ESP>-<nnnn>` |
| `nombre` | ✔ | texto | Nombre visible para el personal |
| `nombre_paciente` | | texto | Nombre en lenguaje sencillo para el portal |
| `especialidad` | ✔ | código | Especialidad dueña |
| `tipo` | ✔ | lista | `consulta`, `control`, `sesion`, `procedimiento_menor`, `estudio`, `grupal` |
| `duracion_min` | ✔ | entero | Tiempo de atención |
| `preparacion_min` | | entero | Antes de la atención (por defecto 0) |
| `limpieza_min` | | entero | Después de la atención (por defecto 0) |
| `modalidades` | ✔ | lista | `presencial`, `virtual`, `telefonica` |
| `capacidad` | | entero | Para grupales (por defecto 1) |
| `recurso_requerido` | | código | Tipo de recurso |
| `requisitos_previos` | | lista | Ver tabla 5 |
| `plantilla_nota` | ✔ | código | Plantilla de nota clínica del encuentro |
| `formularios_sugeridos` | | lista | Formularios a asignar al finalizar |
| `autoagendable` | | booleano | Si el paciente puede reservarlo desde el portal |
| `plazo_cancelacion_horas` | | entero | Sobrescribe el de la organización (RN-005) |
| `categoria_sensible` | | booleano | Hereda de la especialidad si no se indica (RN-016) |
| `codigos_externos` | | lista | `{sistema: CPT | SNOMED | CIE10PCS | ARS-<nombre>, codigo}` |
| `precio_referencia` | | decimal | Fase 3 (módulo 20) |
| `instrucciones_paciente` | | texto largo | Preparación que se envía al confirmar |
| `version` | ✔ | semver | Versión del procedimiento |
| `vigente_desde` / `vigente_hasta` | ✔ / | fecha | Vigencia |
| `estado` | ✔ | lista | `borrador`, `publicado`, `inactivo` |

## 5. Requisitos previos estandarizados

| Código | Descripción | Comportamiento |
|---|---|---|
| `REQ-AYUNO` | Ayuno de N horas | Instrucción en la confirmación y recordatorio |
| `REQ-ORDEN` | Orden médica vigente | Recepción debe adjuntar o vincular una orden antes del check-in |
| `REQ-CONSENT` | Consentimiento informado específico | Debe estar firmado antes de iniciar la atención (RN-014) |
| `REQ-ESTUDIO` | Estudio previo (p. ej., analítica) | Alerta al profesional si no hay resultado vinculado |
| `REQ-AUTORIZ-ARS` | Autorización de la ARS | Fase 3 |
| `REQ-ACOMPANANTE` | Debe venir acompañado | Instrucción al paciente |
| `REQ-TUTOR` | Presencia del tutor (menores) | Se exige si el paciente es menor (RN-017) |

## 6. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-CAT-001 | El sistema deberá permitir al administrador funcional crear, editar, publicar e inactivar especialidades. | M | GEN |
| RF-CAT-002 | El sistema deberá permitir crear, editar, publicar e inactivar procedimientos con todos los campos de la sección 4. | M | GEN |
| RF-CAT-003 | El sistema deberá permitir la **importación masiva** de procedimientos desde CSV o JSON con el esquema de [`catalogos/importacion/`](../catalogos/importacion/), en dos pasos: validación (simulación con reporte de errores por fila) y confirmación. | M | GEN |
| RF-CAT-004 | El sistema deberá tratar la importación como inserción o actualización por `codigo_interno`: si el código existe y hay cambios, crea una nueva versión del procedimiento; nunca sobrescribe la anterior. | M | RN-011 |
| RF-CAT-005 | El sistema deberá permitir exportar el catálogo completo o filtrado a CSV y JSON con el mismo esquema de importación. | S | GEN |
| RF-CAT-006 | El sistema deberá conservar el historial de versiones de cada procedimiento y mostrar qué versión aplicaba a cada cita histórica. | M | RN-011 |
| RF-CAT-007 | El sistema deberá impedir eliminar procedimientos; solo permitirá inactivarlos con fecha `vigente_hasta`. Las citas futuras de un procedimiento inactivado se listarán para su gestión. | M | RN-011 |
| RF-CAT-008 | El sistema deberá permitir programar una nueva versión con `vigente_desde` futura (p. ej., cambio de duración a partir del 1 de enero). | S | GEN |
| RF-CAT-009 | El sistema deberá validar al publicar que el procedimiento tenga una plantilla de nota publicada y, si requiere recurso, que exista al menos un recurso compatible en alguna sede. | M | GEN |
| RF-CAT-010 | El sistema deberá incluir un **catálogo base** precargado (especialidades comunes y procedimientos genéricos) que cada organización puede activar, copiar y adaptar. | S | GEN |
| RF-CAT-011 | El sistema deberá permitir buscar procedimientos por nombre, código interno o código externo. | M | GEN |
| RF-CAT-012 | El sistema deberá permitir cargar la tabla CIE-10 (y en el futuro CIE-11) y definir listas de diagnósticos frecuentes por especialidad. | M | GEN |
| RF-CAT-013 | El sistema deberá registrar en la bitácora toda creación, importación, publicación e inactivación en el catálogo. | M | RNF09 |

## 7. Flujo de publicación

```
Borrador  →  Validación automática  →  Revisión (otro administrador o jefe de especialidad)  →  Publicado
                                                                                              ↓
                                                                          Nueva versión (borrador) … → Publicado (reemplaza desde vigente_desde)
                                                                                              ↓
                                                                                          Inactivo
```

## 8. Importación masiva: reglas de validación

| Validación | Resultado si falla |
|---|---|
| Columnas obligatorias presentes | Rechazo del archivo completo |
| `codigo_interno` con formato `PRC-XXX-0000` | Error en la fila |
| Especialidad existe y está activa | Error en la fila |
| `duracion_min` entre 5 y 600 | Error en la fila |
| `modalidades` en la lista permitida | Error en la fila |
| `plantilla_nota` existe | Advertencia (queda en borrador) |
| `recurso_requerido` existe | Advertencia (queda en borrador) |
| Códigos duplicados dentro del mismo archivo | Error en ambas filas |
| Tamaño máximo | 5 000 filas o 5 MB por archivo |

El reporte de validación se descarga como CSV con las columnas originales + `resultado` + `mensaje`.

## 9. Agregar una especialidad nueva (procedimiento operativo)

1. Copiar [`plantillas/plantilla-especialidad.md`](../plantillas/plantilla-especialidad.md) y completarla con el jefe de la especialidad.
2. Definir o reutilizar las plantillas de nota (módulo 12) y formularios (módulo 14).
3. Documentar cada procedimiento con [`plantillas/plantilla-procedimiento.md`](../plantillas/plantilla-procedimiento.md) o directamente en el CSV.
4. Importar en el ambiente de pruebas, revisar el reporte y corregir.
5. Registrar habilitaciones de los profesionales (módulo 03) y recursos (módulo 06).
6. Publicar en producción y registrar el cambio en el CHANGELOG del catálogo.

## 10. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-CAT-001 | **Dado** un CSV con 200 procedimientos y 3 filas con duración inválida, **cuando** se valida, **entonces** el reporte muestra 197 filas válidas y 3 errores, y no se importa nada hasta confirmar. |
| CA-CAT-002 | **Dado** el procedimiento `PRC-FIS-0001` v1.0.0 de 45 min, **cuando** se importa con 60 min, **entonces** se crea la v1.1.0 y las citas ya completadas siguen mostrando 45 min. |
| CA-CAT-003 | **Dado** un procedimiento inactivado, **cuando** recepción agenda, **entonces** no aparece en la lista, pero sigue visible en citas históricas. |
| CA-CAT-004 | **Dado** una clínica que agrega Dermatología, **cuando** importa su catálogo y publica, **entonces** puede agendar procedimientos de dermatología sin ningún despliegue de código. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
