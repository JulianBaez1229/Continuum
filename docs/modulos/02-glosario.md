---
id: MOD-02
codigo: GLO
titulo: Glosario
version: 1.0.0
estado: Borrador
fase: —
responsable: Por asignar
dependencias: []
trazabilidad: []
---

# 02 — Glosario

Términos usados en toda la documentación. Si un término aparece con otro significado en un módulo, prevalece este glosario; cualquier cambio se tramita con la [Guía de actualización](../GUIA-DE-ACTUALIZACION.md).

| Término | Definición |
|---|---|
| **Organización (tenant)** | Entidad cliente de Continuum (una clínica o grupo de clínicas). Sus datos están aislados de los de otras organizaciones. |
| **Sede** | Ubicación física de una organización con sus propios horarios, consultorios y recursos. |
| **Especialidad** | Área clínica configurada en el catálogo (p. ej., Psicología, Cardiología, Odontología). Define plantillas, procedimientos y formularios disponibles. |
| **Procedimiento** | Servicio clínico que se puede agendar y registrar: consulta, sesión, control, procedimiento menor o estudio. Tiene duración, recursos, requisitos previos y código. |
| **Recurso** | Elemento agendable distinto del profesional: consultorio, sillón dental, equipo (electrocardiógrafo, ecógrafo), sala de terapia grupal. |
| **Profesional de salud** | Usuario con licencia clínica (médico, psicólogo, odontólogo, terapeuta, nutricionista, enfermero) habilitado en una o varias especialidades. |
| **Habilitación (credencial)** | Autorización registrada de un profesional para ejercer una especialidad y realizar ciertos procedimientos, con número de exequátur o licencia y vigencia. |
| **Profesional tratante** | Profesional responsable de un paciente en un episodio de atención. Tiene acceso clínico completo a ese episodio (RN-001). |
| **Equipo de atención** | Conjunto de profesionales autorizados a ver un episodio además del tratante (p. ej., un psiquiatra y un psicólogo que comparten paciente). |
| **Episodio de atención** | Agrupación de encuentros relacionados con un mismo problema o tratamiento (p. ej., "Terapia de ansiedad 2026", "Ortodoncia"). |
| **Encuentro clínico** | Interacción registrada entre paciente y profesional (presencial, telefónica o virtual). Normalmente se origina en una cita. |
| **Cita** | Reserva de un espacio de tiempo para un procedimiento con un profesional (y opcionalmente un recurso) en una sede. |
| **Inasistencia (no-show)** | Cita a la que el paciente no se presentó o que canceló fuera del plazo permitido (RN-005). |
| **Lista de espera** | Cola de pacientes que desean una cita más pronto de la disponible. |
| **Historia clínica electrónica (HCE)** | Conjunto de registros clínicos del paciente: antecedentes, notas, diagnósticos, órdenes, resultados, documentos. |
| **Nota clínica** | Registro de un encuentro según una plantilla de especialidad (SOAP, evolución, odontograma, etc.). |
| **Adenda** | Corrección o ampliación de una nota firmada. No modifica el original; se agrega como entrada nueva (RN-006). |
| **Orden** | Indicación clínica: tratamiento, medicamento, laboratorio, imagen, referimiento a otra especialidad. |
| **Prescripción** | Orden de medicamento con dosis, vía, frecuencia y duración. |
| **Formulario clínico** | Instrumento configurable con preguntas y puntuación (PHQ-9, GAD-7, escala de dolor EVA, registro de presión arterial). |
| **Umbral de riesgo** | Valor de puntuación de un formulario que dispara una alerta inmediata al profesional tratante (RN-007). |
| **Categoría sensible** | Clasificación de información clínica con acceso restringido adicional: salud mental, VIH, salud sexual y reproductiva, adicciones, genética (RN-016). |
| **Acceso de emergencia ("romper el cristal")** | Acceso excepcional a información restringida con justificación obligatoria y auditoría reforzada. |
| **Red de apoyo** | Familiares, tutores o cuidadores autorizados por el paciente para ver información limitada. |
| **Tutor legal** | Persona responsable de un paciente menor de edad o incapacitado; firma consentimientos en su nombre. |
| **Consentimiento informado** | Documento que el paciente (o su tutor) firma para autorizar un procedimiento, el tratamiento de datos o el acceso de terceros. |
| **ARS** | Administradora de Riesgos de Salud (aseguradora del sistema de seguridad social dominicano). |
| **Autorización de ARS** | Número de aprobación que emite la ARS para cubrir un procedimiento. |
| **CIE-10 / CIE-11** | Clasificación Internacional de Enfermedades de la OMS, usada para codificar diagnósticos. |
| **CPT / SNOMED CT** | Terminologías estándar para codificar procedimientos y conceptos clínicos. |
| **HL7 FHIR** | Estándar de interoperabilidad para intercambiar datos de salud mediante API. |
| **Bitácora de auditoría** | Registro inmutable de quién consultó o modificó qué información, cuándo y desde dónde (RNF09). |
| **MFA** | Autenticación multifactor (segundo factor). |
| **RBAC / ABAC** | Control de acceso basado en roles / en atributos (sede, especialidad, relación con el paciente). |
| **MVP** | Producto mínimo viable: primera versión utilizable de punta a punta (módulo 29). |
| **ADR** | Registro de decisión de arquitectura. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
