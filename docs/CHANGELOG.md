# CHANGELOG — Documentación de Continuum

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y [versionado semántico](https://semver.org/lang/es/). Ver la [Guía de actualización](GUIA-DE-ACTUALIZACION.md).

## [Sin publicar]

_Agrega aquí los cambios aprobados que todavía no forman parte de una versión._

### Agregado
- `adr/ADR-002`, `ADR-003`, `ADR-005`: aceptadas (ASP.NET Core, PostgreSQL, identidad propia).
- `CLAUDE.md`: contexto compartido para Claude Code (fuente de verdad, dueños, reglas obligatorias, pruebas, git).
- `adr/ADR-008-paleta-de-colores.md`: decisión de la paleta "Bosque y salvia".

### Cambiado
- Módulo 28 (v1.1.0): paleta definida a partir de la imagen de referencia. Incluye colores medidos, tokens de modo claro y oscuro, escala de ánimo (`--mood-1` a `--mood-4`), reglas de uso, contraste verificado y archivo de tokens CSS.
- `CLAUDE.md`, módulo 29 y `README.md`: referencias a la paleta vigente.
- Prototipo navegable "Continuum MVP" recoloreado con la paleta "Bosque y salvia".
- Módulo 29 (v1.1.0): plan de 8 semanas para 3 desarrolladores con Claude Code y 2 QA, forma de trabajo, entrega a QA y recortes priorizados.

### Retirado
### Catálogo

---

## [1.0.0] — 2026-10-02

### Agregado
- `adr/ADR-002`, `ADR-003`, `ADR-005`: aceptadas (ASP.NET Core, PostgreSQL, identidad propia).
- Especificación modular en 30 documentos a partir del documento "Continuum: plataforma de gestión y seguimiento para centros de salud mental" (Grupo 01, 25-sep-2026).
- Generalización del alcance de **salud mental** a **clínicas multiespecialidad**: catálogo configurable de especialidades, procedimientos, plantillas de historia clínica y formularios.
- Trazabilidad completa de RF01–RF13, RNF01–RNF10 y RN01–RN10 hacia los nuevos IDs por módulo.
- Nuevas reglas de negocio derivadas de la generalización: RN-011 a RN-020 (módulo 27).
- Extensiones empresariales documentadas como Fase 3: facturación y seguros (módulo 20), integraciones HL7 FHIR (módulo 25).
- Plantillas: procedimiento, especialidad, formulario clínico, solicitud de cambio, módulo nuevo y ADR.
- Catálogo inicial de ejemplo: 8 especialidades y 19 procedimientos en CSV de importación masiva, con 4 fichas detalladas.
- Definición del MVP y roadmap por fases (módulo 29).
- Sistema de diseño con tokens neutros; la paleta de colores definitiva queda **pendiente** (módulo 28).
- Prototipo navegable del MVP.
