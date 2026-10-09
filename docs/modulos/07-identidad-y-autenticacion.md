---
id: MOD-07
codigo: IAM
titulo: Identidad y autenticación
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-03, MOD-24]
trazabilidad: ["RNF01", "RF13"]
---

# 07 — Identidad y autenticación

## 1. Objetivo

Garantizar que solo personas identificadas accedan a Continuum, con un nivel de autenticación proporcional a la sensibilidad de lo que pueden ver (RNF01).

## 2. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-IAM-001 | El sistema deberá autenticar a todos los usuarios con correo (o usuario) y contraseña robusta: mínimo 12 caracteres, verificación contra listas de contraseñas filtradas, sin reglas de composición forzada. | M | RNF01 |
| RF-IAM-002 | El sistema deberá exigir un segundo factor (TOTP con aplicación de autenticación; respaldo por códigos de recuperación) a todos los roles distintos de `PACIENTE` y `RED_APOYO`. | M | RNF01 |
| RF-IAM-003 | El sistema deberá ofrecer el segundo factor de forma opcional a pacientes, con código por correo o SMS. | S | RNF01 |
| RF-IAM-004 | El sistema deberá bloquear temporalmente una cuenta tras 5 intentos fallidos en 15 minutos, con desbloqueo progresivo y aviso al usuario. | M | RNF01 |
| RF-IAM-005 | El sistema deberá permitir recuperar la contraseña mediante un enlace de un solo uso que caduque en 30 minutos. | M | RNF01 |
| RF-IAM-006 | El sistema deberá cerrar la sesión tras un periodo de inactividad configurable (módulo 06) y permitir al usuario cerrar sesión en todos sus dispositivos. | M | RNF01 |
| RF-IAM-007 | El sistema deberá permitir la activación de cuenta de paciente mediante invitación (correo o SMS) generada por recepción, verificando fecha de nacimiento y documento. | M | GEN |
| RF-IAM-008 | El sistema deberá permitir el inicio de sesión único (SSO) con el proveedor de identidad corporativo de la organización mediante OIDC o SAML. | C | EXT |
| RF-IAM-009 | El sistema deberá registrar en la bitácora cada inicio de sesión (exitoso o fallido), cambio de contraseña, alta o baja de MFA y cierre de sesión. | M | RNF09 |
| RF-IAM-010 | El sistema deberá solicitar reautenticación (contraseña + MFA) antes de acciones críticas: firmar una nota, acceso de emergencia, exportar datos clínicos, cambiar roles. | S | GEN |

## 3. Flujos

**Activación de cuenta de paciente**

1. Recepción registra al paciente con correo o teléfono (módulo 08).
2. El sistema envía una invitación con un enlace válido por 72 horas.
3. El paciente confirma fecha de nacimiento y los últimos 4 dígitos de su documento.
4. Crea su contraseña y acepta los términos y el aviso de privacidad (versión registrada en `consentimiento`).

**Inicio de sesión del personal**

1. Usuario y contraseña → 2. Código TOTP → 3. Si tiene roles en varias sedes, selecciona la sede activa → 4. Se emite un token de acceso de corta duración (15 min) y un token de renovación rotativo.

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-IAM-001 | **Dado** un profesional sin MFA configurado, **cuando** inicia sesión por primera vez, **entonces** el sistema lo obliga a configurarlo antes de acceder a cualquier función. |
| CA-IAM-002 | **Dado** 5 intentos fallidos, **cuando** se hace el sexto intento, **entonces** la cuenta queda bloqueada 15 minutos y el usuario recibe un aviso por correo. |
| CA-IAM-003 | **Dado** un usuario de recepción inactivo 15 minutos, **cuando** intenta realizar una acción, **entonces** el sistema le pide autenticarse de nuevo sin perder el formulario en curso. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
