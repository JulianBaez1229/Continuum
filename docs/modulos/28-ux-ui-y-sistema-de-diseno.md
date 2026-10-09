---
id: MOD-28
codigo: UXD
titulo: UX/UI y sistema de diseño
version: 1.1.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-16, MOD-26]
trazabilidad: ["RNF02", "RNF08"]
---

# 28 — UX/UI y sistema de diseño

## 1. Principios de experiencia

1. **Calma y claridad.** Es software de salud: pocas distracciones, jerarquía clara, lenguaje humano.
2. **Lo urgente se ve, lo sensible se protege.** Alergias y alertas siempre visibles; datos sensibles ocultos por defecto con "mostrar" explícito.
3. **Mínimos pasos.** Paciente: ≤ 3 pasos para sus tareas clave (RNF-USA-001). Recepción: agendar en < 60 s (RNF-USA-002).
4. **Una sola interfaz, muchas especialidades.** Los formularios se generan desde el catálogo; los componentes deben verse coherentes sin importar la especialidad.
5. **Accesible por defecto.** WCAG 2.2 AA (RNF-COM-003).

## 2. Paleta de colores — "Bosque y salvia" (aprobada, v1)

> Paleta tomada de la imagen de referencia del equipo (maqueta de app móvil en verdes bosque, salvia y crema). **Solo se toman los colores**: el nombre, el logotipo y los textos de la imagen no forman parte de Continuum. La decisión queda registrada en [ADR-008](../adr/ADR-008-paleta-de-colores.md).
>
> Todo el código sigue usando **tokens semánticos**; los valores hexadecimales viven en un solo archivo de tokens.

### 2.1 Colores extraídos de la referencia

Valores medidos por muestreo de píxeles sobre la imagen (promedio de zonas planas):

| Color | Dónde aparece en la referencia | Hex medido |
|---|---|---|
| Verde bosque | Barra de navegación inferior, botones | `#263E2E` |
| Bosque profundo | Encabezado, mosaicos oscuros | `#1C2B22` |
| Verde azulado profundo | Mosaicos "Sesiones" y "Profesionales" | `#1B3337` |
| Pizarra azul | Mosaico "Herramientas" | `#213442` |
| Salvia | Fondo de la pantalla | `#C9D7BD` |
| Salvia clara | Tarjeta "¿Cómo te sientes hoy?" | `#D0DEC3` |
| Crema | Iconos y textos sobre fondos oscuros | `#E7D5B1` |
| Verde musgo | Emoji "Feliz" | `#3E5835` |
| Verde agua | Emoji "Calma" | `#2A5357` |
| Ámbar tierra | Emoji "Ansiedad" | `#7A5328` |
| Pizarra | Emoji "Triste" | `#263E4A` |

### 2.2 Tokens semánticos — modo claro

Los valores marcados **(derivado)** no aparecen tal cual en la imagen: se aclararon u oscurecieron a partir de ella para cumplir contraste o para uso en pantallas grandes de escritorio. El rojo de peligro **no** sale de la imagen: se reserva para alertas clínicas (criterio de §2.5).

| Token | Uso | Valor | Origen |
|---|---|---|---|
| `--color-brand` | Botón primario, enlaces, selección, navegación activa | `#263E2E` | Verde bosque |
| `--color-brand-strong` | Hover/pulsado del primario, encabezados oscuros, barra superior | `#1C2B22` | Bosque profundo |
| `--color-brand-contrast` | Texto e iconos sobre `brand` y `brand-strong` | `#FFFFFF` | — |
| `--color-brand-soft` | Elemento seleccionado, fondos suaves de marca | `#D0DEC3` | Salvia clara |
| `--color-accent` | Acento decorativo sobre fondos oscuros (iconos, detalles, logotipo) | `#E7D5B1` | Crema |
| `--color-secondary` | Mosaicos y bloques secundarios oscuros, gráficos | `#1B3337` | Verde azulado profundo |
| `--color-bg` | Fondo general de la aplicación | `#F1F4EC` | Salvia (derivado, aclarado) |
| `--color-surface` | Tarjetas, paneles, modales | `#FFFFFF` | — |
| `--color-surface-alt` | Encabezados de tabla, zonas secundarias, barra lateral | `#DDE6D2` | Salvia (derivado) |
| `--color-surface-sage` | Superficies de bienvenida y portal del paciente | `#C9D7BD` | Salvia |
| `--color-border` | Divisores y bordes decorativos | `#C9D3BE` | Salvia (derivado) |
| `--color-border-strong` | Borde de campos de formulario y controles (≥ 3:1) | `#6F8068` | Salvia (derivado, oscurecido) |
| `--color-text` | Texto principal | `#1A231D` | Bosque (derivado) |
| `--color-text-muted` | Texto secundario | `#4F5E52` | Bosque (derivado) |
| `--color-success` · `--color-success-soft` | Confirmado, completado | `#3E5835` · `#E4EDDC` | Verde musgo |
| `--color-warning` · `--color-warning-soft` | Pendiente, advertencia | `#7A5328` · `#F4E9D6` | Ámbar tierra |
| `--color-danger` · `--color-danger-soft` | Error, alerta crítica, alergia | `#A93226` · `#F8E4E1` | **No viene de la imagen** |
| `--color-info` · `--color-info-soft` | Información | `#2A5357` · `#DDEBEA` | Verde agua |
| `--color-sensitive` · `--color-sensitive-soft` | Indicador de información sensible | `#2C4257` · `#E1E7EE` | Pizarra azul (derivado) |

### 2.3 Tokens semánticos — modo oscuro

| Token | Valor |
|---|---|
| `--color-bg` | `#141C17` |
| `--color-surface` | `#1E2A23` |
| `--color-surface-alt` | `#26352C` |
| `--color-border` | `#33443A` |
| `--color-border-strong` | `#7C8E75` |
| `--color-text` | `#E9E4D2` |
| `--color-text-muted` | `#A9B5A3` |
| `--color-brand` | `#9FBF93` |
| `--color-brand-contrast` | `#141C17` |
| `--color-brand-soft` | `#2C3F32` |
| `--color-accent` | `#E7D5B1` |
| `--color-success` | `#9FCB8E` |
| `--color-warning` | `#E0B77A` |
| `--color-danger` | `#F09A8E` |
| `--color-info` | `#8EC3C4` |
| `--color-sensitive` | `#A9BDD6` |

Los `*-soft` del modo oscuro son el color de estado al 18 % de opacidad sobre `--color-surface`.

### 2.4 Escala de estado de ánimo (formularios y seguimiento)

La referencia usa cuatro colores para el diario de ánimo. Se incorporan como tokens para formularios clínicos con escalas de ánimo (módulo 14), gráficos de seguimiento (módulo 15) y el portal (módulo 16). Siempre van con **etiqueta de texto e icono**, nunca solo con color.

| Token | Etiqueta | Valor |
|---|---|---|
| `--mood-1` | Feliz / bien | `#3E5835` |
| `--mood-2` | Calma / estable | `#2A5357` |
| `--mood-3` | Ansiedad / inquietud | `#7A5328` |
| `--mood-4` | Tristeza / bajo ánimo | `#263E4A` |

### 2.5 Reglas de uso

- El rojo (`--color-danger`) es **exclusivo** de errores, alertas clínicas, alergias y "Ayuda urgente". Nunca se usa como decoración.
- `--color-brand` y `--color-success` son ambos verdes y se distinguen poco entre sí (contraste 1.5:1). Por eso un estado de éxito **siempre** lleva icono de verificación, texto y fondo `success-soft`; nunca es un relleno sólido parecido a un botón primario.
- `--color-accent` (crema) solo va sobre fondos oscuros (`brand`, `brand-strong`, `secondary`). Sobre fondos claros no tiene contraste suficiente.
- La salvia intensa (`--color-surface-sage`) se usa en el portal del paciente y en superficies de bienvenida. Las pantallas de trabajo del personal (tablas, agenda, notas) usan `--color-bg` y `--color-surface`, más claros, para largas jornadas de lectura.
- Cada organización puede sustituir `brand`, `brand-strong`, `brand-soft` y `accent` (RF-ORG-008). Los colores de estado y de peligro **no** son personalizables.

### 2.6 Contraste verificado (WCAG 2.2)

| Combinación | Ratio | Mínimo |
|---|---|---|
| `brand-contrast` sobre `brand` | 11.6:1 | 4.5:1 |
| `text` sobre `bg` | 14.5:1 | 4.5:1 |
| `text-muted` sobre `bg` | 6.2:1 | 4.5:1 |
| `text-muted` sobre `surface-alt` | 5.3:1 | 4.5:1 |
| `text` sobre `surface-sage` | 10.7:1 | 4.5:1 |
| `brand` sobre `brand-soft` | 8.2:1 | 4.5:1 |
| `accent` sobre `brand` | 8.0:1 | 4.5:1 |
| `border-strong` sobre `bg` | 3.8:1 | 3:1 (componentes) |
| `success` / `warning` / `danger` / `info` / `sensitive` sobre su `*-soft` | 6.6 / 5.6 / 5.4 / 6.9 / 8.3:1 | 4.5:1 |
| Modo oscuro: `text` sobre `bg` | 13.7:1 | 4.5:1 |
| Modo oscuro: `text-muted` sobre `bg` | 8.1:1 | 4.5:1 |
| Modo oscuro: `brand` y colores de estado sobre `bg` | ≥ 8.0:1 | 4.5:1 |

Pendiente antes de producción: prueba con simuladores de daltonismo (deuteranopía, protanopía, tritanopía) y revisión con usuarios reales (QA 1).

### 2.7 Archivo de tokens (referencia de implementación)

```css
:root {
  --color-brand: #263E2E;        --color-brand-strong: #1C2B22;
  --color-brand-contrast: #FFFFFF; --color-brand-soft: #D0DEC3;
  --color-accent: #E7D5B1;       --color-secondary: #1B3337;
  --color-bg: #F1F4EC;           --color-surface: #FFFFFF;
  --color-surface-alt: #DDE6D2;  --color-surface-sage: #C9D7BD;
  --color-border: #C9D3BE;       --color-border-strong: #6F8068;
  --color-text: #1A231D;         --color-text-muted: #4F5E52;
  --color-success: #3E5835;      --color-success-soft: #E4EDDC;
  --color-warning: #7A5328;      --color-warning-soft: #F4E9D6;
  --color-danger: #A93226;       --color-danger-soft: #F8E4E1;
  --color-info: #2A5357;         --color-info-soft: #DDEBEA;
  --color-sensitive: #2C4257;    --color-sensitive-soft: #E1E7EE;
  --mood-1: #3E5835; --mood-2: #2A5357; --mood-3: #7A5328; --mood-4: #263E4A;
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --color-bg: #141C17;           --color-surface: #1E2A23;
    --color-surface-alt: #26352C;  --color-border: #33443A;
    --color-border-strong: #7C8E75;
    --color-text: #E9E4D2;         --color-text-muted: #A9B5A3;
    --color-brand: #9FBF93;        --color-brand-contrast: #141C17;
    --color-brand-soft: #2C3F32;   --color-accent: #E7D5B1;
    --color-success: #9FCB8E;      --color-warning: #E0B77A;
    --color-danger: #F09A8E;       --color-info: #8EC3C4;
    --color-sensitive: #A9BDD6;
  }
}
:root[data-theme="dark"] { /* mismos valores que el bloque anterior */ }
```

## 3. Tipografía y espaciado (tipografía pendiente)

| Token | Valor |
|---|---|
| Familia | Sans-serif del sistema o una libre de Google Fonts (p. ej., Inter, Source Sans 3) — pendiente; la referencia de la paleta usa una sans humanista para el texto y una serif clásica en mayúsculas para la marca |
| Escala | 12 · 14 · 16 (base) · 18 · 20 · 24 · 30 px |
| Interlineado | 1.5 para texto, 1.2 para títulos |
| Espaciado | Múltiplos de 4 px (4, 8, 12, 16, 24, 32, 48) |
| Radio | 6 px componentes, 10 px tarjetas |
| Objetivo táctil | ≥ 44 × 44 px |

## 4. Componentes base

| Componente | Notas |
|---|---|
| Barra lateral de navegación por rol | Menú distinto según rol; colapsable |
| Encabezado de paciente (banner) | Nombre, edad, expediente, alergias (siempre), indicadores de sensibilidad |
| Tarjeta de cita | Hora, paciente, procedimiento, estado (chip con texto) |
| Calendario (día / semana / lista) | Arrastrar para reprogramar con confirmación |
| Formulario dinámico | Renderiza plantillas y formularios desde JSON Schema |
| Chip de estado | Texto + icono + color semántico |
| Alerta clínica | Banner no descartable para riesgo crítico |
| Botón "Ayuda urgente" | Persistente en el portal (RN-008) |
| Tabla de datos | Ordenar, filtrar, paginar, exportar |
| Gráfico de evolución | Línea con bandas de interpretación (módulo 15) |
| Estado vacío | Mensaje útil + acción siguiente |

## 5. Mapa de navegación

| Rol | Menú principal |
|---|---|
| Recepción | Hoy · Agenda · Citas · Pacientes · Lista de espera |
| Profesional | Hoy · Mi agenda · Mis pacientes · Seguimiento · Mensajes · Reportes |
| Coordinador | Todo lo de recepción + Recursos · Reportes de sede |
| Director | Tablero · Reportes |
| Admin funcional | Organización · Sedes · Catálogo · Formularios · Usuarios |
| Auditor | Bitácora · Reportes de cumplimiento |
| Paciente (portal) | Inicio · Citas · Formularios · Mensajes · Recursos · Mi perfil |

## 6. Lenguaje

- Tuteo o usted: **a decidir** (recomendación: usted en el portal del paciente, tono directo en la aplicación del personal).
- Nombres de procedimientos en lenguaje sencillo en el portal (`nombre_paciente`).
- Errores que expliquen qué pasó y qué hacer ("Ese horario ya fue reservado. Le mostramos los más cercanos.").

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial con paleta neutra provisional | — |
| 1.1.0 | 2026-10-09 | Equipo | Paleta "Bosque y salvia" extraída de la imagen de referencia; tokens claros y oscuros, escala de ánimo, reglas de uso y contraste verificado | ADR-008 |
