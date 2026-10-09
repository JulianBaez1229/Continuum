---
id: ADR-008
titulo: Paleta de colores "Bosque y salvia"
fecha: 2026-10-09
estado: aceptada
decisores: [Equipo Grupo 01]
---

# ADR-008 — Paleta de colores "Bosque y salvia"

## Contexto

El módulo 28 dejó la paleta pendiente y el prototipo usaba grises neutros provisionales. El equipo eligió como referencia visual una maqueta de app móvil de bienestar con verdes bosque, salvia y acentos crema. El producto debe transmitir calma (muchos pacientes llegan en momentos difíciles), pero también servir para jornadas largas de trabajo del personal clínico y cumplir WCAG 2.2 AA.

## Opciones consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| A. Mantener grises neutros | Sin riesgo de contraste; fácil de personalizar por organización | Frío, sin identidad, poco asociado a bienestar |
| B. Azul clínico tradicional | Asociado a salud; muy usado | Genérico; se confunde con el azul de "información" |
| C. Bosque y salvia (de la referencia) | Calma, natural, distinto; buen contraste del verde bosque con blanco (11.6:1) | Marca y éxito son ambos verdes; la salvia intensa cansa en pantallas densas |

## Decisión

Se adopta la **opción C**. Se toman de la imagen únicamente los colores, no su nombre, logotipo ni textos. Tokens, valores claros y oscuros, contraste y reglas de uso en el [módulo 28, §2](../modulos/28-ux-ui-y-sistema-de-diseno.md).

Ajustes respecto a la imagen:

- El fondo de las pantallas del personal se aclara (`#F1F4EC`); la salvia intensa (`#C9D7BD`) queda para el portal y las superficies de bienvenida.
- El rojo de peligro (`#A93226`) no viene de la imagen; se agrega para alertas clínicas.
- Los textos se oscurecen a partir del verde bosque para cumplir 4.5:1.

## Consecuencias

- Positivas: identidad visual definida; el trabajo de diseño del frontend (Dev C) deja de depender de la paleta; los colores de ánimo de la referencia sirven para formularios y gráficos de seguimiento.
- Negativas / riesgos: marca y éxito son poco distinguibles, así que los estados de éxito siempre llevan icono y texto. Falta la prueba con simuladores de daltonismo (QA 1).
- Documentos actualizados: módulo 28 (v1.1.0), módulo 29, `CLAUDE.md`, `README.md`, `CHANGELOG.md`.
- Prototipo navegable actualizado con la paleta el 2026-10-09. El encabezado oscuro y la barra inferior del portal siguen el estilo de la referencia.
