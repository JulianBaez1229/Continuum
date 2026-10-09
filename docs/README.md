---
id: DOC-000
titulo: Continuum — Documentación de producto y especificación modular
version: 1.0.0
estado: Borrador para revisión del equipo
fecha: 2026-10-02
fuente: "Grupo 01 — Continuum: Plataforma de gestión y seguimiento para centros de salud mental (UNAPEC, 25-sep-2026)"
---

# Continuum — Plataforma clínica multiespecialidad

> **Continuum** es una plataforma web empresarial para la gestión y el seguimiento de pacientes en **clínicas y centros de salud de cualquier especialidad** (salud mental, medicina general, pediatría, cardiología, odontología, fisioterapia, nutrición, etc.). Conserva los requisitos del documento original (RF01–RF13, RNF01–RNF10, RN01–RN10), pero los generaliza para que la especialidad, los procedimientos, los formularios clínicos y las reglas se **configuren por catálogo** en lugar de estar fijos en el código.

## Cómo usar esta documentación

1. Empieza por [01 — Visión y alcance](modulos/01-vision-y-alcance.md) y [29 — MVP y roadmap](modulos/29-mvp-alcance-y-roadmap.md).
2. Cada integrante toma uno o varios módulos; cada módulo es autocontenido (objetivo, requisitos, reglas, datos, pantallas, criterios de aceptación).
3. Para **cargar procedimientos médicos** o una **nueva especialidad**, usa las plantillas de [`plantillas/`](plantillas/) y el catálogo de [`catalogos/`](catalogos/). El proceso está en [11 — Catálogo de especialidades y procedimientos](modulos/11-catalogo-especialidades-y-procedimientos.md).
4. Para **modificar cualquier documento**, sigue la [Guía de actualización](GUIA-DE-ACTUALIZACION.md) y registra el cambio en el [CHANGELOG](CHANGELOG.md).
5. El prototipo navegable del MVP (lienzo de diseño **"Continuum MVP"**) muestra las pantallas de cada rol y el portal del paciente; las que no son del MVP llevan una etiqueta de fase.

## Índice de módulos

| # | Módulo | Código | Fase | Origen (doc. original) |
|---|--------|--------|------|------------------------|
| 01 | [Visión y alcance](modulos/01-vision-y-alcance.md) | VIS | — | §1, §2, §3 |
| 02 | [Glosario](modulos/02-glosario.md) | GLO | — | — |
| 03 | [Actores, roles y permisos](modulos/03-actores-roles-y-permisos.md) | ROL | MVP | RF13, RN02, Tabla 1 |
| 04 | [Arquitectura general](modulos/04-arquitectura-general.md) | ARQ | MVP | RNF05, RNF08 |
| 05 | [Modelo de datos](modulos/05-modelo-de-datos.md) | DAT | MVP | Todos los RF |
| 06 | [Organización, sedes y multi-tenant](modulos/06-organizacion-sedes-y-multitenant.md) | ORG | MVP | Generalización |
| 07 | [Identidad y autenticación](modulos/07-identidad-y-autenticacion.md) | IAM | MVP | RNF01, RF13 |
| 08 | [Registro maestro de pacientes](modulos/08-registro-de-pacientes.md) | PAC | MVP | RF01, RN03 |
| 09 | [Agenda de profesionales y recursos](modulos/09-agenda-de-profesionales.md) | AGE | MVP | RF03, RN04 |
| 10 | [Gestión de citas](modulos/10-gestion-de-citas.md) | CIT | MVP | RF02, RN04, RN05 |
| 11 | [Catálogo de especialidades y procedimientos](modulos/11-catalogo-especialidades-y-procedimientos.md) | CAT | MVP | Generalización |
| 12 | [Historia clínica electrónica](modulos/12-historia-clinica-electronica.md) | HCE | MVP | RF04, RN01, RN06 |
| 13 | [Órdenes, tratamientos y medicamentos](modulos/13-ordenes-tratamientos-y-medicamentos.md) | TRA | MVP | RF05, RN06 |
| 14 | [Formularios clínicos y evaluaciones](modulos/14-formularios-y-evaluaciones.md) | FRM | MVP | RF06, RN07 |
| 15 | [Seguimiento y evolución del paciente](modulos/15-seguimiento-y-evolucion.md) | SEG | Fase 2 | RF07 |
| 16 | [Portal del paciente](modulos/16-portal-del-paciente.md) | POR | MVP (básico) | RF02, RF06, RNF02 |
| 17 | [Notificaciones](modulos/17-notificaciones.md) | NOT | Fase 2 | RF08 |
| 18 | [Mensajería segura](modulos/18-mensajeria-segura.md) | MSG | Fase 2 | RF09, RN08 |
| 19 | [Red de apoyo y acceso delegado](modulos/19-red-de-apoyo-y-acceso-delegado.md) | FAM | Fase 3 | RF12, RN09 |
| 20 | [Facturación, caja y seguros (extensión)](modulos/20-facturacion-y-seguros.md) | FAC | Fase 3 | Extensión empresarial |
| 21 | [Reportes y analítica](modulos/21-reportes-y-analitica.md) | REP | Fase 2 | RF10 |
| 22 | [Recursos educativos y de bienestar](modulos/22-recursos-educativos-y-bienestar.md) | BIE | Fase 3 | RF11 |
| 23 | [Auditoría y trazabilidad](modulos/23-auditoria-y-trazabilidad.md) | AUD | MVP | RNF09, RN06 |
| 24 | [Seguridad, privacidad y cumplimiento](modulos/24-seguridad-privacidad-y-cumplimiento.md) | SEC | MVP | RNF01, RNF07, RN01 |
| 25 | [Integraciones e interoperabilidad](modulos/25-integraciones-e-interoperabilidad.md) | INT | Fase 2–3 | Extensión |
| 26 | [Requisitos no funcionales](modulos/26-requisitos-no-funcionales.md) | RNF | MVP | RNF01–RNF10 |
| 27 | [Reglas de negocio](modulos/27-reglas-de-negocio.md) | RN | MVP | RN01–RN10 |
| 28 | [UX/UI y sistema de diseño](modulos/28-ux-ui-y-sistema-de-diseno.md) | UXD | MVP | RNF02, RNF08 |
| 29 | [MVP, alcance y roadmap](modulos/29-mvp-alcance-y-roadmap.md) | MVP | — | Prioridades Tabla 2 |
| 30 | [Calidad, pruebas, despliegue y operación](modulos/30-calidad-pruebas-y-despliegue.md) | QA | MVP | RNF03, RNF04, RNF06, RNF10 |

## Material de apoyo

| Carpeta / archivo | Contenido |
|---|---|
| [`plantillas/`](plantillas/) | Plantillas para procedimiento, especialidad, formulario clínico, solicitud de cambio, nuevo módulo y ADR |
| [`catalogos/`](catalogos/) | Catálogo cargable: especialidades y procedimientos de ejemplo + CSV de importación masiva |
| [`CLAUDE.md`](CLAUDE.md) | Contexto compartido para Claude Code; se copia a la raíz del repositorio de código |
| [`GUIA-DE-ACTUALIZACION.md`](GUIA-DE-ACTUALIZACION.md) | Cómo proponer, revisar y versionar cambios a cualquier documento |
| [`CHANGELOG.md`](CHANGELOG.md) | Historial de versiones de la documentación |
| [`adr/`](adr/) | Decisiones de arquitectura y diseño aceptadas (ADR-008: paleta de colores) |
| Lienzo de diseño "Continuum MVP" | Prototipo navegable con la paleta "Bosque y salvia": aplicación del personal (5 roles), portal del paciente móvil y hoja de tokens |

## Convenciones

### Identificadores de requisitos

| Tipo | Formato | Ejemplo | Significado |
|---|---|---|---|
| Requisito funcional | `RF-<COD>-<nnn>` | `RF-CIT-004` | Requisito funcional nº 4 del módulo Citas |
| Requisito no funcional | `RNF-<ATR>-<nnn>` | `RNF-SEG-002` | Ver módulo 26 |
| Regla de negocio | `RN-<nnn>` | `RN-004` | Ver módulo 27 (catálogo único) |
| Criterio de aceptación | `CA-<COD>-<nnn>` | `CA-CIT-004` | Prueba verificable asociada |
| Decisión de arquitectura | `ADR-<nnn>` | `ADR-003` | Ver módulo 04 |

Cada requisito indica su **origen** (`RF02`, `RN04`, etc. del documento original, o `GEN` si nace de la generalización multiespecialidad, o `EXT` si es una extensión empresarial nueva). Así se mantiene la trazabilidad exigida por ISO/IEC/IEEE 29148.

### Prioridad (MoSCoW)

- **M (Must)** — imprescindible para operar; corresponde a "Alta" del documento original.
- **S (Should)** — importante, corresponde a "Media".
- **C (Could)** — deseable, corresponde a "Baja".
- **W (Won't, por ahora)** — fuera de alcance de esta versión, documentado para el futuro.

### Fases

- **MVP** — primera versión utilizable de punta a punta (ver módulo 29).
- **Fase 2** — continuidad del tratamiento y operación (seguimiento, notificaciones, mensajería, reportes).
- **Fase 3** — extensiones empresariales (familiares, facturación, recursos, integraciones avanzadas).

### Estado de un documento

`Borrador` → `En revisión` → `Aprobado` → `Obsoleto`. El estado va en el frontmatter de cada archivo.

## Equipo

| Integrante | Matrícula |
|---|---|
| Julian Elias Baez Mena | A00117224 |
| Carlos Alejandro Pérez | A00116038 |
| Elga Romero | A00116800 |
| Bladimir Ventura | A00115757 |
| Sean Alcántara | A00115911 |

Asignatura: Desarrollo con Tecnologías Open Source y Propietaria II — Maestro: Alexis Morillo — UNAPEC, Escuela de Informática.
