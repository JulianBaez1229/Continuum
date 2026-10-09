---
id: ADR-005
titulo: "Proveedor de identidad"
fecha: 2026-10-09
estado: aceptada
decisores: [Dev A]
---

# ADR-005 — Proveedor de identidad

## Contexto

RF-IAM-001..006 y 009 exigen contraseña robusta, TOTP, bloqueo, recuperación, sesiones y bitácora. El módulo 05 indica que las credenciales viven en el servicio de identidad. SSO (RF-IAM-008) es Fase 2.

## Opciones consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| ASP.NET Core Identity propio + JWT corto con refresh rotativo | Sin servicio extra; control total de bloqueo, auditoría y MFA; encaja en 8 semanas | Hay que implementar y probar con cuidado el flujo |
| Keycloak | MFA y SSO listos | Servicio adicional que operar; integración más lenta |
| Servicio gestionado (Entra ID / Auth0) | Menos código | Costo, dependencia externa y residencia de datos (ADR-007 pendiente) |

## Decisión

**Identidad propia en el módulo `Identidad`**, con hash de contraseñas de ASP.NET Core Identity, TOTP, token de acceso de 15 min y token de renovación rotativo. La lógica de dominio (política de contraseña, bloqueo) queda detrás de puertos para poder cambiar de proveedor. SSO por OIDC/SAML se añade en Fase 2.

## Consecuencias

- Positivas: control del bloqueo (RF-IAM-004), auditoría (RF-IAM-009) y MFA por rol.
- Negativas / riesgos: la seguridad es responsabilidad del equipo; exige pruebas dedicadas y revisión cruzada.
- Módulos de documentación a actualizar: 04, 07, 24, `CLAUDE.md`.
