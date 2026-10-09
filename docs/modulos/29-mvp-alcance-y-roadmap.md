---
id: MOD-29
codigo: MVP
titulo: MVP, alcance y roadmap
version: 1.1.1
estado: Borrador
fase: —
responsable: Por asignar
dependencias: [Todos]
trazabilidad: ["Tabla 2 (prioridades)"]
---

# 29 — MVP, alcance y roadmap

## 1. Objetivo del MVP

Demostrar de punta a punta que una clínica **con al menos dos especialidades distintas** puede operar en Continuum: registrar pacientes, configurar procedimientos por catálogo, agendar sin conflictos, atender con una nota clínica de su especialidad, indicar tratamientos, asignar un formulario con alerta de riesgo y auditar todos los accesos.

> **Hipótesis a validar:** la configuración por catálogo permite atender especialidades muy distintas (p. ej., Psicología y Odontología) con la misma plataforma, sin programar pantallas específicas.

## 2. Flujo de demostración del MVP

```
1. Admin funcional importa el catálogo (Psicología + Medicina general + Odontología) desde CSV
2. Admin crea usuarios: recepcionista, psicóloga, odontólogo, director, auditor; habilita especialidades
3. Profesionales definen su disponibilidad
4. Recepción registra a un paciente nuevo (valida campos obligatorios y duplicados)
5. Recepción agenda una sesión de psicología y una limpieza dental (sin solapamientos, con requisitos)
6. El paciente activa su cuenta, ve sus citas y la instrucción de preparación
7. Check-in → la psicóloga atiende con la plantilla PLT-PSI-001, codifica CIE-10, firma la nota
8. La psicóloga indica un plan de 8 sesiones y asigna el PHQ-9 semanal
9. El paciente completa el PHQ-9 desde el portal → supera el umbral → alerta inmediata a la psicóloga
   y el paciente ve las líneas de emergencia
10. El odontólogo atiende con el odontograma; no puede ver las notas de psicología (categoría sensible)
11. El director ve el tablero de asistencia sin contenido clínico
12. El auditor ve en la bitácora cada acceso realizado en la demostración
```

## 3. Alcance del MVP

### Incluido

| Módulo | Qué entra en el MVP |
|---|---|
| 03 Roles | Roles base, habilitaciones, equipo de atención (sin roles personalizados ni acceso de emergencia) |
| 06 Organización | Una organización, varias sedes, recursos, políticas básicas |
| 07 Identidad | Contraseña robusta, MFA para el personal, invitación de pacientes, bloqueo, cierre por inactividad |
| 08 Pacientes | Registro, validaciones RN-003, duplicados, tutor para menores, consentimiento de datos |
| 09 Agenda | Disponibilidad recurrente, bloqueos, recursos, vistas día/semana |
| 10 Citas | Programar, reprogramar, cancelar, check-in, estados, inasistencias, requisitos previos |
| 11 Catálogo | Especialidades, procedimientos, importación CSV con validación, versionado, CIE-10 |
| 12 HCE | Plantillas JSON (3 especialidades), firma, adendas, alergias, diagnósticos, categorías sensibles |
| 13 Órdenes | Medicamentos (con RN-018 y alerta de alergia), tratamientos, indicaciones; PDF básico |
| 14 Formularios | Asignación, respuesta desde el portal, puntuación, alerta por umbral (cargados por JSON) |
| 16 Portal | Inicio, mis citas (ver/confirmar/cancelar), formularios, botón de ayuda urgente |
| 17 Notificaciones | Correo: confirmación, recordatorio 48 h, cancelación; alerta de riesgo en plataforma + correo |
| 21 Reportes | REP-001 (tablero del día), REP-002 (asistencia), REP-010 (mi agenda), REP-011 (auditoría) |
| 23 Auditoría | Bitácora completa de solo anexar con consulta para el auditor |
| 24 Seguridad | Controles técnicos de la sección 3, consentimiento, minimización |
| 28 Diseño | Tokens semánticos con la paleta "Bosque y salvia" (ADR-008), modo claro y oscuro, componentes base |
| 30 Calidad | CI con pruebas, respaldos diarios, despliegue en contenedores |

### Excluido del MVP (con fase destino)

| Funcionalidad | Fase |
|---|---|
| Gráficos de evolución y panel de seguimiento (15) | 2 |
| Autoagendamiento del paciente, SMS/WhatsApp, lista de espera, citas recurrentes | 2 |
| Mensajería segura (18) | 2 |
| Reportes de productividad, ocupación y alertas (21) | 2 |
| Editor visual de plantillas y formularios | 2 |
| SSO, telemedicina | 2 |
| Red de apoyo para adultos (19) — los tutores de menores sí entran | 3 |
| Recursos educativos (22) | 3 |
| Facturación y seguros (20) | 3 |
| Laboratorios, imágenes, FHIR, ARS (25) | 3 |
| Alta de múltiples organizaciones (SaaS) | 3 |

## 4. Equipo y responsabilidades

Equipo de 5 personas durante **8 semanas**: 3 desarrolladores que trabajan con Claude Code y 2 QA. Cada desarrollador es **dueño de sus módulos** (solo él modifica esas carpetas) para evitar conflictos; cada QA acompaña a líneas concretas.

| Rol | Módulos / alcance | Dueño de | Acompaña a |
|---|---|---|---|
| **Dev A — Seguridad y clínico** | 03, 07, 23, 24, 12, 13 | Autenticación y MFA, autorización por rol y por relación con el paciente, bitácora, historia clínica, órdenes | — |
| **Dev B — Operación** | 06, 11, 08, 09, 10, 17, 21 (backend) | Contrato de datos de operación, catálogo e importación, pacientes, agenda, citas, correos, reportes | — |
| **Dev C — Experiencia y formularios** | 28, 16, 14, 21 (UI), shell de la app | Tokens de diseño, motor de formularios dinámicos (JSON Schema), portal, pantallas | — |
| **QA 1 — Funcional y negocio** | Criterios de aceptación, reglas de negocio, flujo de demostración, usabilidad, manual de usuario | Plan de pruebas, casos `CP-*`, regresión, contenido de prueba del catálogo | Dev B y Dev C |
| **QA 2 — Seguridad, datos y automatización** | Matriz de autorización, auditoría, E2E automatizado, carga, accesibilidad, datos semilla | Datos ficticios, pruebas E2E (Playwright), k6, escaneos | Dev A (y E2E de todos) |

Los documentos 01, 02, 26, 27 y 29 son responsabilidad compartida y se revisan cada viernes.

## 5. Plan de 8 semanas

| Sem. | Dev A | Dev B | Dev C | QA 1 | QA 2 | Hito |
|---|---|---|---|---|---|---|
| **1** | Repositorio, CI, auth + MFA, middleware de tenant y auditoría | Migraciones del modelo de datos (módulo 05) | Front base, tokens, shell por rol, cliente API generado | Plan de pruebas; casos `CP-*` de módulos 11 y 08 | Datos semilla ficticios; plantilla de matriz de autorización; flujo de errores y severidades | **Día 1–2: OpenAPI y modelo de datos congelados entre los 3 devs** |
| **2** | Usuarios, roles, habilitaciones | Catálogo + importación CSV con validación | Motor de formularios + pantallas de catálogo y usuarios | CSV de prueba válidos e inválidos; pruebas de importación | Pruebas de login, MFA, bloqueo y cierre por inactividad | El CSV de ejemplo se importa |
| **3** | Autorización por relación, equipo de atención, categorías sensibles | Pacientes (RN-003, duplicados) + disponibilidad y espacios | Pantallas de pacientes y agenda | Casos de pacientes y agenda (RN-003, RN-004, RN-013) | Intentos de acceso indebido (IDOR, otra organización, recepción → HCE) | |
| **4** | Notas: plantilla, firma, adendas | Citas: solapamiento transaccional, cancelación, check-in | Asistente de agendar, tablero "Hoy" | Concurrencia al agendar; cancelación tardía (RN-005) | Inmutabilidad de notas; eventos en bitácora | **Demo interna: pasos 1–7 del flujo** |
| **5** | Órdenes, RN-018, alerta de alergia | Correos de confirmación y recordatorio, inasistencias | Formularios: asignar, responder, puntuar, alerta RN-007 | Portal y formularios en teléfonos reales | Órdenes (RN-018, alergias); inicio de E2E automatizado | |
| **6** | Consulta de bitácora para el auditor | Reportes REP-001/002/010 | Portal completo, "Ayuda urgente", tablero de dirección | Verificar cálculos de reportes contra datos semilla; prueba de usabilidad (SUS) con 3–5 personas | Accesibilidad (axe); E2E del flujo completo | |
| **7** | Corrección de errores | Corrección de errores | Corrección de errores | Regresión completa; guion de aceptación | Prueba de carga (k6, 100 usuarios); escaneo de seguridad; matriz de autorización completa | **Congelamiento de funcionalidades el viernes** |
| **8** | Estabilización | Estabilización | Estabilización | Manual de usuario; ensayo de la demo | Verificación de correcciones; lista de verificación de la DoD | **Entrega** |

A partir de la semana 4, cada desarrollador reserva alrededor del **20 % de su tiempo** para corregir errores reportados por QA.

## 6. Forma de trabajo

| Práctica | Detalle |
|---|---|
| Contexto para Claude Code | `CLAUDE.md` en el repositorio que apunta a `docs/modulos/` y `27-reglas-de-negocio.md`. Las tareas se piden por ID: "Implementa RF-CIT-003 según el módulo 10, con pruebas para CA-CIT-001". |
| Pruebas desde los criterios | Cada `CA-XXX-nnn` tiene una prueba automatizada del dev y un caso `CP-XXX-nnn` de QA que lo verifica en el ambiente de pruebas. |
| Contratos protegidos | OpenAPI y migraciones solo cambian por PR aprobado por su dueño (B: datos de operación; A: seguridad y clínico). |
| Revisión de código | Cruzada y obligatoria: A revisa a B, B a C, C a A. Nadie aprueba su propio código. |
| Entrega a QA | Lo fusionado a `main` se despliega a QA a diario; QA verifica en menos de 24 h. |
| Errores | Severidad S1 (bloquea la demo) a S4 (cosmético). S1 y S2 bloquean la entrega; triaje diario de 15 minutos. |
| Ritmo | Planificación el lunes (30 min), integración diaria, demo corta el viernes. |

### Recortes si el plan se atrasa (en este orden)

1. PDF de recetas → impresión desde el navegador.
2. Verificación por hash de la bitácora → bitácora solo anexar.
3. Varias sedes → una sola sede.
4. Recursos agendables → solo profesionales.
5. Recordatorio de 48 h → solo correo de confirmación.

No se recortan: autorización, auditoría, no solapamiento ni importación del catálogo.

## 7. Definición de terminado (DoD)

- [ ] Requisito implementado según su ID y criterios de aceptación en verde.
- [ ] Pruebas unitarias y de integración; reglas de negocio con pruebas dedicadas.
- [ ] Autorización y auditoría verificadas para el endpoint.
- [ ] Sin vulnerabilidades altas o críticas en el escaneo.
- [ ] Accesible (axe sin errores críticos) y adaptable a móvil.
- [ ] Documentación del módulo actualizada si cambió algo (Guía de actualización).
- [ ] Demostrado en el ambiente de pruebas con datos ficticios.

## 8. Roadmap

| Fase | Tema | Contenido principal |
|---|---|---|
| **MVP** | Operar de punta a punta | Sección 3 |
| **Fase 2** | Continuidad del cuidado | Seguimiento y gráficos, mensajería, SMS/WhatsApp, autoagendamiento, lista de espera, citas recurrentes, reportes ampliados, editor visual, SSO, telemedicina |
| **Fase 3** | Plataforma empresarial | Red de apoyo, recursos educativos, facturación y ARS, integraciones FHIR con laboratorios, multi-organización SaaS, PWA, app móvil |

## 9. Métricas de éxito del MVP

| Métrica | Meta |
|---|---|
| Flujo de demostración completo sin errores | 12/12 pasos |
| Especialidades operando solo con configuración | ≥ 3 |
| Criterios de aceptación de módulos MVP en verde | 100 % |
| SUS en prueba con usuarios | ≥ 70 |
| Respuesta p95 con 100 usuarios simulados | ≤ 3 s |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
| 1.1.0 | 2026-10-02 | Equipo | Plan de 8 semanas para 3 desarrolladores con Claude Code y 2 QA; forma de trabajo y recortes | — |
| 1.1.1 | 2026-10-09 | Equipo | Alcance de diseño del MVP con la paleta definida | ADR-008 |
