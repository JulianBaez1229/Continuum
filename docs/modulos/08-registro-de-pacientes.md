---
id: MOD-08
codigo: PAC
titulo: Registro maestro de pacientes
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-05, MOD-06, MOD-27]
trazabilidad: ["RF01", "RN03", "RN09"]
---

# 08 — Registro maestro de pacientes

## 1. Objetivo

Mantener **un único expediente por paciente** dentro de la organización, con datos demográficos, de contacto, de seguro y antecedentes básicos, reutilizable por todas las especialidades y sedes (generalización de RF01).

## 2. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-PAC-001 | El sistema deberá permitir a recepción registrar un paciente con datos personales, de contacto, contacto de emergencia y antecedentes clínicos básicos. | M | RF01 |
| RF-PAC-002 | El sistema deberá asignar automáticamente un número de expediente único por organización. | M | GEN |
| RF-PAC-003 | El sistema deberá aceptar como documento de identidad la cédula dominicana, pasaporte u otro documento extranjero, y para menores sin documento, el acta de nacimiento o el documento del tutor. | M | GEN |
| RF-PAC-004 | El sistema deberá validar el formato de la cédula (11 dígitos con dígito verificador) y advertir si el documento ya existe. | M | GEN |
| RF-PAC-005 | El sistema deberá detectar posibles duplicados (mismo documento, o mismo nombre y fecha de nacimiento) antes de guardar y ofrecer abrir el expediente existente. | M | GEN |
| RF-PAC-006 | El sistema deberá permitir fusionar dos expedientes duplicados, conservando todo el historial y dejando constancia en la bitácora; solo el rol `ADMIN_FUNCIONAL` puede hacerlo. | S | GEN |
| RF-PAC-007 | El sistema deberá registrar el seguro médico del paciente: ARS, plan, número de afiliado y vigencia; un paciente puede tener varios seguros con orden de prioridad. | S | GEN |
| RF-PAC-008 | El sistema deberá exigir un tutor legal registrado cuando el paciente sea menor de edad (RN-017). | M | RN09 |
| RF-PAC-009 | El sistema deberá permitir buscar pacientes por nombre, documento, número de expediente o teléfono, con resultados en menos de 1 segundo para 100 000 expedientes. | M | RNF04 |
| RF-PAC-010 | El sistema deberá marcar al paciente como "registro incompleto" mientras falten los campos obligatorios de RN-003 e impedir agendarle citas. | M | RN03 |
| RF-PAC-011 | El sistema deberá permitir al paciente actualizar desde el portal su teléfono, correo y dirección; los cambios de nombre o documento requieren validación de recepción. | S | GEN |
| RF-PAC-012 | El sistema deberá registrar el consentimiento del paciente para el tratamiento de sus datos personales, con versión del texto y fecha. | M | RNF07 |
| RF-PAC-013 | El sistema deberá permitir registrar preferencias de comunicación (canal, idioma, horario) y necesidades de accesibilidad. | C | GEN |

## 3. Campos del paciente

| Grupo | Campo | Obligatorio | Notas |
|---|---|---|---|
| Identificación | Nombres, apellidos | ✔ | |
| | Tipo y número de documento | ✔ | Cifrado (módulo 24) |
| | Fecha de nacimiento | ✔ | Calcula edad y si requiere tutor |
| | Sexo registrado | ✔ | Catálogo configurable |
| | Nombre preferido | | Opcional |
| Contacto | Teléfono móvil | ✔ | |
| | Correo electrónico | | Requerido para el portal |
| | Dirección (provincia, municipio, sector, calle) | | |
| Emergencia | Nombre, parentesco, teléfono | ✔ | RN-003 |
| Tutor (si es menor) | Nombre, documento, parentesco, teléfono | ✔ si menor | RN-017 |
| Seguro | ARS, plan, número de afiliado, vigencia | | |
| Clínico básico | Alergias, antecedentes personales y familiares relevantes, medicamentos actuales | | Visible solo para personal clínico |
| Administrativo | Sede preferida, ocupación, estado civil, nacionalidad | | |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-PAC-001 | **Dado** un paciente nuevo sin contacto de emergencia, **cuando** recepción intenta agendarle una cita, **entonces** el sistema la bloquea e indica el campo faltante. |
| CA-PAC-002 | **Dado** un paciente ya registrado con cédula 001-1234567-8, **cuando** recepción registra otro con la misma cédula, **entonces** el sistema muestra el expediente existente y no crea uno nuevo. |
| CA-PAC-003 | **Dado** un paciente de 15 años, **cuando** se registra sin tutor, **entonces** el sistema no permite guardar el registro como completo. |
| CA-PAC-004 | **Dado** un usuario de recepción, **cuando** consulta la ficha, **entonces** no ve alergias ni antecedentes clínicos. |

## 5. Pantallas

- Pacientes › Búsqueda.
- Pacientes › Nuevo / Editar (asistente por pasos: identificación → contacto → emergencia/tutor → seguro → consentimiento).
- Pacientes › Ficha (resumen, citas, seguros, contactos, documentos administrativos).

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
