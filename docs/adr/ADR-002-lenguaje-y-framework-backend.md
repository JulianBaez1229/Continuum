---
id: ADR-002
titulo: "Lenguaje y framework del backend"
fecha: 2026-10-09
estado: aceptada
decisores: [Dev A]
---

# ADR-002 — Lenguaje y framework del backend

## Contexto

Plazo de 8 semanas, 3 desarrolladores trabajando con Claude Code y monolito modular (ADR-001). Se necesita autenticación, autorización por políticas, ORM maduro y buen soporte de pruebas.

## Opciones consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| ASP.NET Core (C#) | Identity, políticas de autorización, EF Core, xUnit; tipado fuerte; ya es la pila del módulo 04 | Curva si el equipo no conoce C# |
| Node.js (NestJS) | Un solo lenguaje con el frontend | Menos integrado en seguridad; más piezas a elegir |
| Java (Spring Boot) | Muy maduro | Más ceremonia y tiempo de arranque |

## Decisión

**ASP.NET Core (C#, .NET 9)** como monolito modular, un proyecto por módulo bajo `src/Modules/`.

## Consecuencias

- Positivas: Identity y autorización por políticas incluidas; esqueleto ya creado.
- Negativas / riesgos: dependencia de .NET 9 (soporte estándar).
- Módulos de documentación a actualizar: 04 (estado de ADR-002), `CLAUDE.md` (quitar "propuesta").
