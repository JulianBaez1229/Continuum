---
id: ADR-003
titulo: "Motor de base de datos"
fecha: 2026-10-09
estado: aceptada
decisores: [Dev A]
---

# ADR-003 — Motor de base de datos

## Contexto

Se requiere aislamiento por `organizacion_id` (ADR-004, RLS), JSON versionado para plantillas clínicas (ADR-006) y cifrado de columna.

## Opciones consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| PostgreSQL | RLS nativo, JSONB, extensiones, open source | Operación propia |
| SQL Server | Buen soporte en .NET, Always Encrypted | Licencia |
| MySQL | Muy difundido | RLS y JSON más limitados |

## Decisión

**PostgreSQL 16** (EF Core con Npgsql).

## Consecuencias

- Positivas: RLS y JSONB cubren ADR-004 y ADR-006.
- Negativas / riesgos: cifrado de columna se implementa en la aplicación.
- Módulos de documentación a actualizar: 04, 05, `CLAUDE.md`.
