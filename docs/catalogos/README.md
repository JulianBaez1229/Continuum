# Catálogo clínico de Continuum

Fuente documental del catálogo que se carga en el sistema (módulo [11](../modulos/11-catalogo-especialidades-y-procedimientos.md)). Todo lo que hay aquí son **datos de ejemplo** para el MVP; cada clínica los adapta.

## Estructura

```
catalogos/
├── README.md                  ← este archivo
├── CHANGELOG.md               ← historial de cambios del catálogo
├── importacion/
│   ├── README.md              ← especificación de columnas
│   ├── especialidades.csv     ← 8 especialidades de ejemplo
│   └── procedimientos.csv     ← 19 procedimientos de ejemplo
└── procedimientos/
    ├── PSI/PRC-PSI-0002.md    ← fichas detalladas (una por procedimiento)
    ├── CAR/PRC-CAR-0002.md
    ├── ODO/PRC-ODO-0003.md
    └── FIS/PRC-FIS-0002.md
```

## Especialidades incluidas

| Código | Especialidad | Sensible | Prescribe | Procedimientos en CSV |
|---|---|---|---|---|
| PSI | Psicología | Sí | No | 3 |
| PSQ | Psiquiatría | Sí | Sí | 2 |
| MGE | Medicina general | No | Sí | 2 |
| CAR | Cardiología | No | Sí | 3 |
| ODO | Odontología | No | Sí | 3 |
| FIS | Fisioterapia | No | No | 2 |
| NUT | Nutrición | No | No | 2 |
| PED | Pediatría | No | Sí | 2 |

## Cómo agregar o actualizar

1. Procedimiento nuevo → [`plantillas/plantilla-procedimiento.md`](../plantillas/plantilla-procedimiento.md) + fila en `importacion/procedimientos.csv`.
2. Especialidad nueva → [`plantillas/plantilla-especialidad.md`](../plantillas/plantilla-especialidad.md) + fila en `importacion/especialidades.csv`.
3. Cambio en un procedimiento → subir `version` (nunca cambiar `codigo_interno`).
4. Retirar un procedimiento → `estado: inactivo` y `vigente_hasta`; nunca borrar la fila (RN-011).
5. Registrar el cambio en [`CHANGELOG.md`](CHANGELOG.md) y seguir la [Guía de actualización](../GUIA-DE-ACTUALIZACION.md).

La ficha `.md` y la fila del CSV deben coincidir; en caso de diferencia, prevalece la ficha aprobada y se corrige el CSV.
