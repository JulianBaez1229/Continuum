---
id: MOD-25
codigo: INT
titulo: Integraciones e interoperabilidad
version: 1.0.0
estado: Borrador
fase: Fase 2–3
responsable: Por asignar
dependencias: [MOD-04, MOD-17, MOD-20]
trazabilidad: ["EXT", "RF08"]
---

# 25 — Integraciones e interoperabilidad

## 1. Objetivo

Conectar Continuum con servicios externos (mensajería, laboratorios, aseguradoras, identidad, telemedicina) mediante estándares abiertos, y exponer una API propia segura para terceros autorizados.

## 2. Mapa de integraciones

| Integración | Propósito | Estándar / mecanismo | Fase |
|---|---|---|---|
| Correo transaccional | Notificaciones | SMTP / API del proveedor | MVP |
| SMS / WhatsApp Business | Recordatorios | API del proveedor | Fase 2 |
| Proveedor de identidad corporativo | SSO del personal | OIDC / SAML 2.0 | Fase 2 |
| Telemedicina | Enlace de videoconsulta en citas virtuales | Enlace generado por API (p. ej., Jitsi, Zoom, Teams) | Fase 2 |
| Laboratorios | Enviar órdenes y recibir resultados | HL7 FHIR R4 (`ServiceRequest`, `DiagnosticReport`, `Observation`) o archivo | Fase 3 |
| Imágenes | Recibir informes y enlaces a visores | FHIR `ImagingStudy` / enlace DICOMweb | Fase 3 |
| ARS | Validar afiliación, solicitar autorizaciones | API/portal de cada ARS | Fase 3 |
| Facturación electrónica | Comprobantes e-CF | Servicios de la DGII / proveedor autorizado | Fase 3 |
| Pasarela de pago | Pagos en línea | Pasarela certificada PCI DSS (tokenización) | Fase 3 |
| Exportación del paciente | Portabilidad de datos | FHIR `Bundle` (`Patient`, `Encounter`, `Condition`, `MedicationRequest`…) | Fase 3 |

## 3. Correspondencia con recursos FHIR (referencia)

| Entidad Continuum | Recurso FHIR R4 |
|---|---|
| `paciente` | `Patient` |
| `profesional` / `habilitacion` | `Practitioner` / `PractitionerRole` |
| `organizacion` / `sede` | `Organization` / `Location` |
| `procedimiento` (catálogo) | `ActivityDefinition` / `HealthcareService` |
| `cita` | `Appointment` |
| `disponibilidad` | `Schedule` / `Slot` |
| `encuentro` | `Encounter` |
| `episodio` | `EpisodeOfCare` |
| `diagnostico` | `Condition` |
| `orden` (medicamento) | `MedicationRequest` |
| `orden` (laboratorio/imagen) | `ServiceRequest` |
| `formulario` / `respuesta_formulario` | `Questionnaire` / `QuestionnaireResponse` |
| `consentimiento` | `Consent` |
| `evento_auditoria` | `AuditEvent` |

Diseñar el modelo con esta correspondencia en mente facilita la interoperabilidad futura sin tener que adoptar FHIR como modelo interno.

## 4. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-INT-001 | El sistema deberá exponer una API REST documentada con OpenAPI 3, versionada (`/api/v1`), autenticada con OAuth 2.0 / OIDC. | M | GEN |
| RF-INT-002 | El sistema deberá abstraer los proveedores de mensajería detrás de una interfaz común, para cambiar de proveedor sin modificar los módulos de negocio. | S | GEN |
| RF-INT-003 | El sistema deberá emitir webhooks firmados para eventos seleccionados (cita programada, cancelada, completada) hacia sistemas autorizados de la organización. | C | EXT |
| RF-INT-004 | El sistema deberá permitir generar un enlace de videoconsulta para citas virtuales mediante un proveedor configurable. | C | EXT |
| RF-INT-005 | El sistema deberá recibir resultados de laboratorio en FHIR o CSV y vincularlos con la orden y el paciente; los resultados no conciliados irán a una bandeja de revisión. | W | EXT |
| RF-INT-006 | El sistema deberá registrar cada llamada de integración (sin contenido clínico) con estado y tiempo de respuesta. | S | RNF09 |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
