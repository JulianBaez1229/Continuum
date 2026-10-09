---
# PLANTILLA DE FORMULARIO CLÍNICO / PLANTILLA DE NOTA — Continuum
# Sirve para formularios (módulo 14) y plantillas de nota (módulo 12).
codigo: FRM-XXX             # * FRM-<nombre> o PLT-<ESP>-<nnn>
tipo: formulario            # * formulario | plantilla_nota
nombre: ""                  # *
especialidades: []          # *
version: 1.0.0              # *
estado: borrador
fuente: ""                  #   Autor / publicación del instrumento
licencia: ""                # * Libre uso / requiere licencia / propio de la clínica
respondido_por: paciente    #   paciente | profesional | ambos
frecuencia_sugerida: ""     #   p. ej. semanal
---

# <Nombre>

## Propósito

_Qué mide o registra y en qué momento se usa._

## Preguntas / secciones

| ID | Sección | Texto | Tipo | Opciones / unidad / rango | Obligatorio |
|---|---|---|---|---|---|
| q1 | | | escala | 0, 1, 2, 3 | ✔ |
| q2 | | | numero | mmHg, 50–250 | ✔ |
| q3 | | | texto_largo | — | |

Tipos permitidos: `texto_corto`, `texto_largo`, `numero`, `escala`, `seleccion_unica`, `seleccion_multiple`, `fecha`, `si_no`, `diagnostico_cie10`, `componente:<odontograma|mapa_corporal|curva_crecimiento>`, `calculo`.

## Puntuación

- Método: `suma` | `promedio` | `formula` | `ninguno`
- Ítems incluidos:
- Fórmula (si aplica):

## Interpretación y umbrales

| Desde | Hasta | Nivel | ¿Alerta? |
|---|---|---|---|
| | | | |

## Alertas por ítem

| Ítem | Condición | Nivel | Acción |
|---|---|---|---|
| | | crítico | Notificar al tratante + mostrar líneas de emergencia (RN-007, RN-008) |

## Esquema JSON

```json
{
  "codigo": "FRM-XXX",
  "version": "1.0.0",
  "preguntas": [],
  "puntuacion": {},
  "interpretacion": [],
  "alertas_por_item": []
}
```

## Historial

| Versión | Fecha | Autor | Cambio |
|---|---|---|---|
| 1.0.0 | 2026-10-02 | | Alta |
