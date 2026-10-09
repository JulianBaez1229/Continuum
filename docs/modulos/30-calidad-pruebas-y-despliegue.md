---
id: MOD-30
codigo: QA
titulo: Calidad, pruebas, despliegue y operación
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-04, MOD-24, MOD-26]
trazabilidad: ["RNF03", "RNF04", "RNF05", "RNF06", "RNF10"]
---

# 30 — Calidad, pruebas, despliegue y operación

## 1. Estrategia de pruebas

| Nivel | Qué se prueba | Herramienta sugerida | Meta |
|---|---|---|---|
| Unitarias | Dominio y reglas de negocio (RN-001 a RN-020) | xUnit / Jest | ≥ 70 % dominio, ≥ 90 % reglas |
| Integración | API + base de datos (contenedor real), autorización por endpoint | Testcontainers | Todos los endpoints |
| Contrato | API vs. especificación OpenAPI | Schemathesis / Pact | Sin diferencias |
| Extremo a extremo | Flujo de demostración del MVP (módulo 29) | Playwright | 12/12 pasos |
| Accesibilidad | WCAG 2.2 AA | axe-core en E2E + revisión manual | 0 errores críticos |
| Rendimiento | RNF-REN-001 a 004 | k6 | p95 ≤ 3 s con 100 usuarios |
| Seguridad | SAST, SCA, DAST, secretos | CodeQL / Semgrep, Dependabot, OWASP ZAP, gitleaks | 0 altas/críticas |
| Usabilidad | RNF-USA-001 a 003 | Pruebas moderadas con 5 usuarios por rol | SUS ≥ 70 (MVP) |
| Restauración | RNF-RES-002 | Simulacro | < 24 h |

### Matriz de pruebas de autorización (obligatoria)

Para cada endpoint clínico se prueba, como mínimo: tratante (permitido), miembro del equipo (según sensibilidad), profesional ajeno (denegado), recepción (denegado), usuario de otra organización (no encontrado), sin sesión (401).

## 2. Ambientes

| Ambiente | Propósito | Datos |
|---|---|---|
| Local | Desarrollo | Semilla ficticia |
| Pruebas (QA) | Integración continua y pruebas E2E | Semilla ficticia |
| Preproducción | Validación de versión, demo | Ficticios o anonimizados |
| Producción | Operación | Reales |

## 3. Integración y despliegue continuos

```
PR → linter + compilación + pruebas unitarias + SAST + SCA + secretos
   → revisión de código (1 aprobación mínima; 2 si toca seguridad o reglas)
   → merge a main → pruebas de integración + E2E en QA
   → etiqueta de versión → despliegue a preproducción → DAST + humo
   → aprobación manual → producción (despliegue sin caída) → humo + monitoreo
```

- Migraciones de base de datos versionadas, compatibles hacia atrás (expandir → migrar → contraer).
- Versionado semántico del software; notas de versión generadas desde los PR.
- Reversión documentada para cada versión.

## 4. Operación

| Área | Práctica |
|---|---|
| Monitoreo | Disponibilidad externa cada minuto; alertas de error y latencia (RNF-OBS-001) |
| Respaldo | Diario cifrado + registros de transacciones; copia en otra región (RNF-RES-001) |
| Restauración | Simulacro trimestral documentado (RNF-RES-002) |
| Incidentes | Severidades S1–S4, guardia, comunicación a clínicas, análisis posterior sin culpables |
| Mantenimiento | Ventanas fuera de horario, aviso 72 h (RNF-DIS-002) |
| Parches | Dependencias críticas en ≤ 7 días; resto mensual |

## 5. Documentación y capacitación (RNF06)

| Entregable | Audiencia | Fase |
|---|---|---|
| Documentación técnica (arquitectura, ADR, API, despliegue) | Equipo técnico | MVP |
| Manual de usuario por rol (recepción, profesional, director, administrador, paciente) | Usuarios | MVP |
| Guía de configuración del catálogo (especialidades, procedimientos, formularios) | Admin funcional | MVP |
| Guías rápidas de una página por rol | Usuarios | MVP |
| Videos cortos de capacitación | Usuarios | Fase 2 |
| Plan de capacitación por centro (sesión de 2 h por rol + acompañamiento la primera semana) | Clínica | Fase 2 |

## 6. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-QA-001 | **Dado** un PR que reduce la cobertura de reglas de negocio por debajo del 90 %, **cuando** corre el pipeline, **entonces** falla. |
| CA-QA-002 | **Dado** una versión candidata, **cuando** se ejecuta la prueba de carga con 100 usuarios, **entonces** el p95 es ≤ 3 s o la versión no se publica. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
