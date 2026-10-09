---
id: MOD-19
codigo: FAM
titulo: Red de apoyo y acceso delegado
version: 1.0.0
estado: Borrador
fase: Fase 3 (tutores de menores en MVP)
responsable: Por asignar
dependencias: [MOD-08, MOD-16, MOD-24]
trazabilidad: ["RF12", "RN09"]
---

# 19 — Red de apoyo y acceso delegado

## 1. Objetivo

Permitir que familiares, cuidadores o tutores participen en el cuidado del paciente, viendo solo la información que el paciente (o su tutor legal) autoriza y por el tiempo que decida (RF12, RN-009).

## 2. Tipos de acceso

| Tipo | Quién | Origen del permiso | Alcance típico |
|---|---|---|---|
| **Tutor legal** | Padre, madre o tutor de un menor; representante legal de un adulto incapacitado | Registro legal en el expediente (RN-017) | Gestión completa del portal del menor, firma de consentimientos |
| **Red de apoyo** | Familiar, pareja, cuidador | Consentimiento expreso del paciente | Limitado: citas, recursos asignados, instrucciones de preparación |
| **Cuidador ampliado** _(opcional)_ | Cuidador de un paciente adulto mayor o crónico | Consentimiento expreso + aprobación del tratante | Además: formularios a completar en nombre del paciente, órdenes liberadas |

## 3. Alcances configurables

`citas` · `instrucciones` · `recursos` · `formularios_completar` · `ordenes_liberadas` · `mensajes_administrativos`

Nunca se incluyen: notas clínicas, diagnósticos de categorías sensibles, mensajes clínicos.

## 4. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-FAM-001 | El sistema deberá permitir que un familiar autorizado por el paciente consulte información limitada, como citas y recursos asignados. | C | RF12 |
| RF-FAM-002 | El sistema deberá exigir el consentimiento expreso del paciente, o el de su tutor legal si es menor de edad, registrado con fecha, alcance y versión del texto (RN-009). | M | RN09 |
| RF-FAM-003 | El sistema deberá permitir al paciente revocar el permiso en cualquier momento con efecto inmediato y notificar al familiar. | M | RN09 |
| RF-FAM-004 | El sistema deberá permitir definir una fecha de caducidad del permiso. | S | GEN |
| RF-FAM-005 | El sistema deberá permitir que el profesional tratante marque que, a su criterio clínico, la participación de la red de apoyo es recomendable, sin que esto otorgue acceso por sí solo. | C | Tabla 1 |
| RF-FAM-006 | El sistema deberá retirar automáticamente el acceso del tutor cuando el paciente alcance la mayoría de edad y avisar a ambos 30 días antes. | S | RN-017 |
| RF-FAM-007 | El sistema deberá registrar en la bitácora cada acceso de un familiar o tutor. | M | RNF09 |

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-FAM-001 | **Dado** un familiar con alcance `citas`, **cuando** entra al portal, **entonces** ve las citas del paciente sin el nombre de procedimientos de categorías sensibles. |
| CA-FAM-002 | **Dado** que el paciente revoca el permiso, **cuando** el familiar intenta entrar un minuto después, **entonces** ya no ve información del paciente. |
| CA-FAM-003 | **Dado** un paciente que cumple 18 años, **cuando** llega la fecha, **entonces** el tutor pierde el acceso y el paciente recibe una invitación para activar su propia cuenta. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
