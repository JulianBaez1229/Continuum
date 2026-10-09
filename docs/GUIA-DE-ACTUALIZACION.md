---
id: DOC-001
titulo: Guía de actualización de la documentación
version: 1.0.0
estado: Aprobado
fecha: 2026-10-02
---

# Guía de actualización de la documentación

Esta guía define **cómo se cambia cualquier archivo** de la documentación de Continuum: un requisito, una regla, un procedimiento médico del catálogo o un módulo completo. El objetivo es que la documentación siga siendo la fuente de verdad del producto y que cada cambio sea trazable.

## 1. Tipos de cambio

| Tipo | Ejemplos | Versión | Aprobación |
|---|---|---|---|
| **Editorial** | Ortografía, redacción, enlaces rotos | `PATCH` (1.0.0 → 1.0.1) | Cualquier integrante |
| **Catálogo** | Agregar o actualizar procedimientos, especialidades, formularios | `PATCH` del catálogo | Responsable del módulo 11 |
| **Funcional menor** | Nuevo criterio de aceptación, ajuste de campo, nueva regla que no rompe otras | `MINOR` (1.0.0 → 1.1.0) | Responsable del módulo + 1 revisor |
| **Funcional mayor** | Cambiar alcance del MVP, eliminar un requisito, cambiar un rol, cambiar arquitectura | `MAJOR` (1.x → 2.0.0) | Todo el equipo (y el maestro si afecta entregables) |

## 2. Flujo de un cambio

```
 Solicitud de cambio  →  Análisis de impacto  →  Edición  →  Revisión  →  Aprobación  →  CHANGELOG
 (plantilla SC)          (qué módulos toca)      (rama)      (PR)         (merge)        (versión)
```

1. **Crear la solicitud.** Copia [`plantillas/plantilla-solicitud-de-cambio.md`](plantillas/plantilla-solicitud-de-cambio.md) en `cambios/SC-<nnn>-<titulo-corto>.md`.
2. **Analizar el impacto.** Busca el ID afectado en todo el repositorio (por ejemplo `RN-004`) y lista cada módulo donde aparece. Un cambio a una regla de negocio casi siempre toca el módulo 27 y al menos un módulo funcional.
3. **Editar en una rama.** Nombre: `docs/SC-<nnn>-<titulo-corto>`. Un cambio = una rama = un Pull Request.
4. **Actualizar el frontmatter** de cada archivo tocado: `version`, `fecha` y, si aplica, `estado: En revisión`.
5. **Agregar una fila en el "Historial de cambios"** al final del módulo.
6. **Abrir el Pull Request** con el enlace a la solicitud. El revisor comprueba la lista de verificación (sección 5).
7. **Al aprobar**, se fusiona y se agrega la entrada en [`CHANGELOG.md`](CHANGELOG.md).

## 3. Reglas para los identificadores

- **Nunca reutilices un ID.** Si un requisito se elimina, se marca como `~~RF-CIT-004~~ (Retirado en v1.3.0, ver SC-012)` y su número queda reservado.
- Los nuevos requisitos toman el siguiente número libre del módulo.
- Todo requisito nuevo debe declarar su **origen** (`RFxx`, `RNxx`, `GEN`, `EXT` o `SC-nnn`).
- Todo requisito nuevo debe tener al menos **un criterio de aceptación** verificable.

## 4. Cargar o actualizar procedimientos médicos

Los procedimientos viven en dos formatos equivalentes:

| Formato | Uso | Ubicación |
|---|---|---|
| Markdown con frontmatter YAML | Documentar, revisar y discutir un procedimiento | `catalogos/procedimientos/<especialidad>/<codigo>.md` |
| CSV de importación | Carga masiva al sistema | `catalogos/importacion/procedimientos.csv` |

**Agregar un procedimiento**

1. Copia [`plantillas/plantilla-procedimiento.md`](plantillas/plantilla-procedimiento.md) a la carpeta de su especialidad.
2. Asigna el código interno `PRC-<ESP>-<nnnn>` (ver módulo 11) y, si existe, el código de una terminología estándar (CPT, SNOMED CT, CIE-10-PCS o el tarifario de la ARS).
3. Completa los campos obligatorios (marcados con `*`).
4. Agrega la misma información como una fila en el CSV de importación.
5. Abre el PR con el tipo de cambio **Catálogo**.

**Actualizar un procedimiento existente**

- Cambios de duración, precio de referencia, preparación o requisitos → incrementa `version` del procedimiento y documenta en su historial.
- Un procedimiento **nunca se borra**: se marca `estado: inactivo` con `vigente_hasta`. Las citas y registros históricos siguen apuntando a él (ver RN-011 en el módulo 27).

## 5. Lista de verificación del revisor

- [ ] El frontmatter tiene `version` y `fecha` actualizadas.
- [ ] Cada requisito nuevo tiene ID único, origen, prioridad y criterio de aceptación.
- [ ] Los enlaces internos funcionan.
- [ ] Si cambió una regla de negocio, el módulo 27 y los módulos funcionales dicen lo mismo.
- [ ] Si cambió el alcance del MVP, el módulo 29 está actualizado.
- [ ] Si cambió el modelo de datos, el módulo 05 está actualizado.
- [ ] La entrada del CHANGELOG está redactada.
- [ ] No se incluyeron datos reales de pacientes (solo datos ficticios).

## 6. Estilo de redacción

- Español neutro, voz activa, frases cortas.
- Los requisitos usan **"El sistema deberá…"**; las reglas de negocio usan **"Un/Una … no puede / debe…"**.
- Evita palabras ambiguas: "rápido", "fácil", "adecuado", "etc." Usa valores medibles.
- Las tablas se prefieren a los párrafos para requisitos y campos.

## 7. Plantilla de historial de cambios (al final de cada módulo)

```markdown
## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
```
