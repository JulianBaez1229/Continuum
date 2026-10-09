---
id: MOD-01
codigo: VIS
titulo: Visión y alcance
version: 1.0.0
estado: Borrador
fase: —
responsable: Por asignar
dependencias: []
trazabilidad: ["§1 Introducción", "§2 Tema y problemática", "§3 Justificación"]
---

# 01 — Visión y alcance

## 1. Declaración de visión

> Para **clínicas, policlínicas y centros de salud de cualquier especialidad** que hoy gestionan citas, expedientes y seguimiento con papel, hojas de cálculo y mensajería personal, **Continuum** es una plataforma web que centraliza la operación clínica y administrativa y da **continuidad al cuidado del paciente entre una consulta y otra**. A diferencia de los sistemas de agenda aislados o los expedientes monoespecialidad, Continuum se adapta a cada especialidad mediante **catálogos configurables** de procedimientos, plantillas clínicas y formularios, sin cambiar el código.

## 2. De salud mental a multiespecialidad

El documento original se centró en centros de salud mental. Esta especificación conserva todos sus requisitos y los generaliza así:

| Concepto original (salud mental) | Concepto generalizado | Dónde se define |
|---|---|---|
| Psicólogos y psiquiatras | **Profesionales de salud** con una o varias especialidades habilitadas | [03](03-actores-roles-y-permisos.md), [11](11-catalogo-especialidades-y-procedimientos.md) |
| Sesión terapéutica | **Encuentro clínico** de un **procedimiento** del catálogo (consulta, sesión, control, procedimiento menor) | [11](11-catalogo-especialidades-y-procedimientos.md), [12](12-historia-clinica-electronica.md) |
| Notas de sesión | **Plantillas de nota clínica** por especialidad (SOAP, evolución, odontograma, etc.) | [12](12-historia-clinica-electronica.md) |
| Terapias y medicamentos | **Órdenes**: tratamientos, medicamentos, laboratorios, imágenes, referimientos | [13](13-ordenes-tratamientos-y-medicamentos.md) |
| Cuestionarios emocionales | **Formularios clínicos** configurables (PHQ-9, escala de dolor, signos vitales en casa, control glucémico) | [14](14-formularios-y-evaluaciones.md) |
| Evolución del estado emocional | **Seguimiento de indicadores** clínicos por paciente | [15](15-seguimiento-y-evolucion.md) |
| Hábitos saludables | **Recursos educativos** por especialidad | [22](22-recursos-educativos-y-bienestar.md) |
| Un centro | **Organización** con una o varias **sedes** | [06](06-organizacion-sedes-y-multitenant.md) |
| Confidencialidad de la salud mental | **Categorías clínicas sensibles** con acceso restringido adicional (salud mental, VIH, salud sexual y reproductiva, adicciones) | [24](24-seguridad-privacidad-y-cumplimiento.md), RN-016 |

## 3. Problemática (generalizada)

1. **Información dispersa.** Agendas en libretas o Excel, expedientes en papel y coordinación por WhatsApp personal provocan pérdida de información, citas duplicadas u olvidadas y errores de transcripción.
2. **Riesgo sobre datos sensibles.** No existe control formal sobre quién consulta o modifica la información clínica, lo que incumple la Ley n.º 172-13.
3. **Cuidado interrumpido.** Entre consultas, el paciente no puede reportar su evolución, recibir recordatorios ni comunicarse de forma segura; el profesional llega a cada encuentro con información incompleta.
4. **Cada especialidad trabaja distinto.** Una clínica multiespecialidad necesita formatos clínicos, duraciones, recursos y requisitos previos diferentes por especialidad; los sistemas genéricos obligan a improvisar.
5. **Dirección sin datos.** No hay cifras consolidadas de asistencia, cancelaciones, productividad ni ocupación de recursos.

## 4. Objetivos del producto

| ID | Objetivo | Indicador | Meta (12 meses tras el lanzamiento) |
|---|---|---|---|
| OBJ-01 | Reducir inasistencias | Tasa de inasistencia mensual | −30 % frente a la línea base del centro |
| OBJ-02 | Eliminar conflictos de agenda | Citas solapadas registradas | 0 |
| OBJ-03 | Digitalizar el expediente | % de encuentros con nota clínica digital | ≥ 95 % |
| OBJ-04 | Dar continuidad al cuidado | % de pacientes activos con al menos un formulario completado entre encuentros | ≥ 40 % |
| OBJ-05 | Proteger la información | Accesos a información clínica con registro de auditoría | 100 % |
| OBJ-06 | Configurar una nueva especialidad sin programar | Tiempo de alta de especialidad + procedimientos | ≤ 1 día hábil |

## 5. Alcance

### Dentro del alcance (todas las fases)

- Registro maestro de pacientes, agendas, citas y lista de espera.
- Catálogo configurable de especialidades, procedimientos, recursos, plantillas y formularios.
- Historia clínica electrónica con plantillas por especialidad, órdenes y prescripciones.
- Portal del paciente (web adaptable a móvil), notificaciones, mensajería segura.
- Red de apoyo (familiares y tutores) con consentimiento.
- Reportes clínicos y administrativos; auditoría completa.
- Organización con múltiples sedes; arquitectura preparada para multi-tenant.

### Fuera del alcance (esta versión)

- Telemedicina por video integrada (se documenta como integración futura en [25](25-integraciones-e-interoperabilidad.md)).
- Sistema de laboratorio (LIS) o de imágenes (PACS/RIS) propio; solo integración.
- Gestión hospitalaria de internamiento (camas, quirófanos, farmacia hospitalaria).
- Aplicación móvil nativa (el portal es web adaptable; RNF08).
- Contabilidad general y nómina.

## 6. Supuestos y restricciones

| Tipo | Descripción |
|---|---|
| Restricción | El sistema requiere conexión a internet (RN-010). |
| Restricción | Debe cumplir la Ley n.º 172-13 y las normas de confidencialidad de la información médica (RNF07). |
| Supuesto | Cada organización designa un **administrador funcional** que mantiene catálogos y usuarios. |
| Supuesto | Los profesionales cuentan con correo electrónico y teléfono móvil para el segundo factor de autenticación. |
| Supuesto | Los datos de prueba y demostración son siempre ficticios. |

## 7. Partes interesadas

| Parte interesada | Interés principal |
|---|---|
| Pacientes | Acceso a sus citas, continuidad del cuidado, confidencialidad |
| Profesionales de salud | Información clínica organizada, agenda clara, menos tareas operativas |
| Personal administrativo / recepción | Registro y agendamiento ágil, sin conflictos |
| Dirección / propietarios | Supervisión, indicadores, cumplimiento legal |
| Familiares / tutores | Participar como red de apoyo con autorización |
| Administrador funcional | Configurar especialidades, procedimientos, usuarios |
| Equipo de TI / proveedor | Operación, seguridad, respaldos |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
