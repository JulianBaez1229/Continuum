# Importación masiva del catálogo

Archivos CSV que el módulo de catálogo importa (RF-CAT-003). Codificación **UTF-8**, separador **coma**, primera fila con encabezados. Los campos con varios valores usan **`|`** como separador interno.

> Los códigos externos (CPT, CDT) de los ejemplos son **referenciales** para ilustrar el formato; el responsable del catálogo debe verificarlos contra el tarifario o la terminología que use la clínica antes de publicarlos.

## `especialidades.csv`

| Columna | Obligatorio | Formato |
|---|---|---|
| `codigo` | ✔ | 3 letras mayúsculas, único |
| `nombre` | ✔ | Texto |
| `descripcion` | | Texto |
| `categoria_sensible` | ✔ | `true` / `false` |
| `tipos_profesional` | ✔ | Lista con `|`: `medico`, `psicologo`, `odontologo`, `fisioterapeuta`, `nutricionista`, `enfermero`… |
| `facultad_prescribir` | ✔ | `true` / `false` (RN-018) |
| `estado` | ✔ | `borrador` / `publicado` / `inactivo` |
| `version` | ✔ | semver |

## `procedimientos.csv`

| Columna | Obligatorio | Formato | Ejemplo |
|---|---|---|---|
| `codigo_interno` | ✔ | `PRC-<ESP>-<nnnn>` | `PRC-CAR-0002` |
| `nombre` | ✔ | Texto (sin comas o entre comillas) | Electrocardiograma de 12 derivaciones con interpretación |
| `nombre_paciente` | | Texto | Electrocardiograma |
| `especialidad` | ✔ | Código existente | `CAR` |
| `tipo` | ✔ | `consulta` · `control` · `sesion` · `procedimiento_menor` · `estudio` · `grupal` | `estudio` |
| `duracion_min` | ✔ | Entero 5–600 | `15` |
| `preparacion_min` | | Entero ≥ 0 | `5` |
| `limpieza_min` | | Entero ≥ 0 | `5` |
| `modalidades` | ✔ | `presencial` · `virtual` · `telefonica` (con `|`) | `presencial|virtual` |
| `capacidad` | | Entero ≥ 1 | `8` |
| `recurso_requerido` | | Código de tipo de recurso | `REC-ELECTROCARDIOGRAFO` |
| `requisitos_previos` | | Lista con `|`; parámetros tras `:` | `REQ-AYUNO:8|REQ-ORDEN` |
| `plantilla_nota` | ✔ | Código de plantilla | `PLT-CAR-001` |
| `formularios_sugeridos` | | Lista con `|` | `FRM-PHQ9|FRM-GAD7` |
| `autoagendable` | | `true` / `false` | `false` |
| `plazo_cancelacion_horas` | | Entero; vacío = valor de la organización | `48` |
| `categoria_sensible` | | `true` / `false`; vacío = hereda | `true` |
| `codigos_externos` | | Lista con `|` de `SISTEMA:codigo` | `CPT:93000` |
| `precio_referencia` | | Decimal (Fase 3) | |
| `version` | ✔ | semver | `1.0.0` |
| `vigente_desde` | ✔ | `AAAA-MM-DD` | `2026-10-02` |
| `vigente_hasta` | | `AAAA-MM-DD` | |
| `estado` | ✔ | `borrador` / `publicado` / `inactivo` | `publicado` |

## Comportamiento de la importación

1. **Validar** (simulación): se revisa cada fila con las reglas del módulo 11, §8, y se descarga un reporte.
2. **Confirmar**: solo se aplican las filas válidas.
3. Si el `codigo_interno` **no existe**, se crea. Si **existe y cambió algo**, se crea una nueva versión (RN-011). Si **no cambió nada**, se ignora.
4. Todo queda en la bitácora con el nombre del archivo y el usuario.

## Tipos de recurso usados en los ejemplos

`REC-CONSULTORIO`, `REC-SALA-GRUPAL`, `REC-SILLON-DENTAL`, `REC-ELECTROCARDIOGRAFO`, `REC-ECOGRAFO`, `REC-GIMNASIO-TERAPIA`

## Formularios referenciados

`FRM-PHQ9`, `FRM-GAD7`, `FRM-PA-CASA` (presión arterial en casa), `FRM-EVA-DOLOR`, `FRM-GLUCEMIA`, `FRM-SATISFACCION`, `FRM-PRECONSULTA`
