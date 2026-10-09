# CLAUDE.md — Continuum

> Copiar este archivo a la **raíz del repositorio de código**. La documentación de producto vive en `docs/` (el contenido de `continuum-docs/`).

## Qué es este proyecto

Continuum es una plataforma web para gestionar y dar seguimiento a pacientes en **clínicas multiespecialidad**: pacientes, agenda, citas, historia clínica, órdenes, formularios clínicos con alertas, portal del paciente, reportes y auditoría. Las especialidades y los procedimientos se configuran por **catálogo**, no con código.

Es un proyecto académico (UNAPEC, Grupo 01) con estándar empresarial. Plazo: 8 semanas. El alcance está en `docs/modulos/29-mvp-alcance-y-roadmap.md`.

## Fuente de verdad

Antes de implementar, lee el módulo correspondiente. **Si el código y la documentación no coinciden, se detiene y se pregunta**; no se inventa comportamiento.

| Necesito… | Leer |
|---|---|
| El requisito y sus criterios de aceptación | `docs/modulos/<nn>-*.md` (IDs `RF-XXX-nnn`, `CA-XXX-nnn`) |
| Una regla de negocio | `docs/modulos/27-reglas-de-negocio.md` (catálogo único `RN-001` a `RN-020`) |
| Entidades, campos y estados | `docs/modulos/05-modelo-de-datos.md` |
| Quién puede ver o hacer qué | `docs/modulos/03-actores-roles-y-permisos.md` |
| Arquitectura y módulos del backend | `docs/modulos/04-arquitectura-general.md` |
| Seguridad | `docs/modulos/24-seguridad-privacidad-y-cumplimiento.md` |
| Colores, tipografía, componentes | `docs/modulos/28-ux-ui-y-sistema-de-diseno.md` |
| Formato del catálogo e importación CSV | `docs/catalogos/importacion/README.md` |
| Plan semanal y dueños | `docs/modulos/29-mvp-alcance-y-roadmap.md` |

## Dueños de módulos

Cada desarrollador solo modifica sus carpetas. Para tocar código de otro dueño, se abre un PR y lo aprueba ese dueño.

| Dueño | Módulos de docs | Carpetas de código |
|---|---|---|
| **Dev A** — Seguridad y clínico | 03, 07, 12, 13, 23, 24 | `src/Modules/Identidad`, `src/Modules/HistoriaClinica`, `src/Modules/Auditoria`, `src/Continuum.Api/Middleware` |
| **Dev B** — Operación | 06, 08, 09, 10, 11, 17, 21 (backend) | `src/Modules/Organizacion`, `Pacientes`, `Catalogo`, `Agenda`, `Comunicacion`, `Reportes`, migraciones |
| **Dev C** — Experiencia y formularios | 14, 16, 21 (UI), 28 | `src/Modules/Formularios`, `web/` |

**Contratos protegidos** (cambian solo por PR aprobado por su dueño): especificación OpenAPI, migraciones de base de datos, `src/Shared/`, tokens de diseño.

## Cómo implementar un requisito

1. Leer el requisito, sus criterios de aceptación y las reglas `RN-*` que cita.
2. Escribir primero las pruebas que reproducen cada `CA-*`.
3. Implementar lo mínimo para que pasen.
4. Verificar autorización y auditoría del endpoint (ver reglas obligatorias).
5. Ejecutar linter y pruebas.
6. Commit y PR citando los IDs.

Si un requisito es ambiguo o contradice otro, **no se elige una interpretación en silencio**: se deja una nota en el PR y se propone una solicitud de cambio (`docs/plantillas/plantilla-solicitud-de-cambio.md`).

## Reglas obligatorias

Estas reglas se aplican siempre, aunque la tarea no las mencione.

**Seguridad y privacidad**
- La autorización se valida **en el backend en cada solicitud**: rol + sede + habilitación + relación con el paciente (RN-001, RN-002, RN-012). La UI ocultando un botón no cuenta como control.
- Toda consulta filtra por `organizacion_id` (RN-015). Un recurso de otra organización responde 404, no 403.
- Toda lectura o escritura de información clínica genera un evento de auditoría en la misma transacción (módulo 23).
- Nunca registrar contenido clínico, documentos de identidad, tokens ni contraseñas en logs.
- Notificaciones externas (correo, SMS) sin información clínica; texto genérico para categorías sensibles (RN-016).
- Solo datos **ficticios** en código, pruebas, semillas y capturas. Nunca datos reales de pacientes.
- Secretos solo por variables de entorno o gestor de secretos. Nunca en el repositorio.

**Datos**
- No hay borrado físico de entidades clínicas ni de catálogo: se usan `estado` y fechas de vigencia (RN-006, RN-011).
- Una nota firmada es inmutable; las correcciones son adendas.
- Cada cita, nota y respuesta guarda la versión del elemento de catálogo usado.
- Fechas en UTC en la base de datos; zona horaria de la sede en la presentación (`America/Santo_Domingo` por defecto).
- IDs UUID; nada de IDs secuenciales expuestos en la API.

**Frontend**
- Solo tokens semánticos del módulo 28 (`--color-brand`, `--color-danger`…). **Nunca colores fijos**: los valores de la paleta "Bosque y salvia" (ADR-008) viven en un único archivo de tokens (`web/src/styles/tokens.css`), copiado de módulo 28 §2.7.
- El rojo (`--color-danger`) es solo para errores, alertas clínicas, alergias y "Ayuda urgente". Los estados de éxito llevan siempre icono y texto, porque se parecen al verde de marca.
- `--color-accent` (crema) solo sobre fondos oscuros. Las pantallas del personal usan `--color-bg`; la salvia intensa (`--color-surface-sage`) es para el portal.
- Los estados se muestran con texto e icono, no solo con color.
- Componentes accesibles: `<button>`, `<label>` para cada campo, objetivos táctiles ≥ 44 px, WCAG 2.2 AA.
- Todo debe funcionar desde 360 px de ancho.
- El portal del paciente muestra siempre el acceso a "Ayuda urgente" (RN-008).

## Pruebas

| Tipo | Meta |
|---|---|
| Reglas de negocio | Prueba dedicada por regla; cobertura ≥ 90 % |
| Dominio | Cobertura ≥ 70 % |
| Endpoints clínicos | Matriz mínima: tratante (permitido), equipo según sensibilidad, profesional ajeno (denegado), recepción (denegado), otra organización (404), sin sesión (401) |
| Nombres | Incluir el ID del criterio: `CA_CIT_001_espacio_ya_reservado_devuelve_conflicto` |

Los QA verifican cada `CA-*` con un caso `CP-*` en el ambiente de pruebas. Un error S1 o S2 abierto bloquea la entrega.

## Stack y comandos

> Pila **propuesta**, pendiente de confirmar por ADR (módulo 04). Actualizar esta sección cuando se acepten ADR-002, ADR-003 y ADR-005.

- Backend: ASP.NET Core (C#), monolito modular, arquitectura limpia por módulo.
- Frontend: React + TypeScript (Vite).
- Base de datos: PostgreSQL. Caché: Redis. Cola: RabbitMQ. Documentos: MinIO.
- Contenedores: Docker Compose para desarrollo.

```bash
docker compose up -d          # servicios locales (BD, caché, cola, almacenamiento)
dotnet build                  # compilar backend
dotnet test                   # pruebas del backend
cd web && npm install         # dependencias del frontend
npm run dev                   # frontend en desarrollo
npm run lint && npm test      # linter y pruebas del frontend
npx playwright test           # E2E
```

## Git

- Ramas: `feat/RF-CIT-003-solapamiento`, `fix/BUG-042-checkin`, `docs/SC-007-plazo-cancelacion`.
- Commits: `feat(citas): impedir solapamiento de profesional y recurso [RF-CIT-003, RN-004]`.
- PR: lista de IDs implementados, cómo se probó y capturas si hay UI. Revisión cruzada obligatoria (A→B, B→C, C→A); nadie aprueba su propio PR.
- Integrar a `main` a diario; `main` siempre desplegable.

## No hacer

- No cambiar el alcance del MVP, una regla de negocio ni un rol sin una solicitud de cambio aprobada.
- No agregar dependencias nuevas sin mencionarlo en el PR.
- No modificar contratos protegidos ni carpetas de otro dueño en el mismo PR de una funcionalidad.
- No desactivar pruebas, linters ni verificaciones de seguridad para que el pipeline pase.
- No editar `docs/` como efecto secundario: los cambios de documentación siguen `docs/GUIA-DE-ACTUALIZACION.md`.
