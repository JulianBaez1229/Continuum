---
id: MOD-06
codigo: ORG
titulo: Organización, sedes y multi-tenant
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-04, MOD-05]
trazabilidad: ["GEN", "RN10"]
---

# 06 — Organización, sedes y multi-tenant

## 1. Objetivo

Permitir que Continuum sirva a una clínica con una sola sede, a un grupo con varias sedes y, a futuro, a muchas organizaciones independientes desde la misma instalación (modelo SaaS), manteniendo sus datos completamente aislados.

## 2. Jerarquía

```
Organización (tenant)
├── Configuración general (zona horaria, idioma, logotipo, políticas)
├── Especialidades habilitadas  ← subconjunto del catálogo (módulo 11)
└── Sedes
    ├── Horario de atención y feriados
    ├── Recursos (consultorios, equipos, salas)
    └── Personal asignado (roles por sede)
```

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-ORG-001 | El sistema deberá permitir registrar la organización con nombre, RNC, dirección fiscal, logotipo, zona horaria e idioma. | M | GEN |
| RF-ORG-002 | El sistema deberá permitir crear, editar y desactivar sedes con dirección, teléfono, horario de atención y calendario de feriados. | M | GEN |
| RF-ORG-003 | El sistema deberá permitir habilitar o deshabilitar especialidades del catálogo para toda la organización o por sede. | M | GEN |
| RF-ORG-004 | El sistema deberá permitir registrar recursos por sede (tipo, nombre, capacidad, procedimientos compatibles) y marcarlos fuera de servicio. | M | GEN |
| RF-ORG-005 | El sistema deberá permitir configurar políticas parametrizables de la organización: plazo de cancelación (RN-005), límite de inasistencias (RN-020), campos obligatorios del paciente (RN-003), canales de notificación y textos legales. | M | RN03, RN05 |
| RF-ORG-006 | El sistema deberá aplicar el aislamiento por organización en todas las consultas, de modo que ningún usuario vea datos de otra organización (RN-015). | M | GEN |
| RF-ORG-007 | El sistema deberá permitir que un paciente sea atendido en varias sedes de la misma organización con un único expediente. | M | GEN |
| RF-ORG-008 | El sistema deberá permitir personalizar la marca del portal del paciente (logotipo y colores de la organización) mediante los tokens del sistema de diseño (módulo 28). | C | EXT |
| RF-ORG-009 | El sistema deberá permitir el alta de nuevas organizaciones por parte del operador de la plataforma, con un asistente de configuración inicial. | W (Fase 3) | EXT |

## 4. Parámetros configurables por organización

| Parámetro | Valor por defecto | Regla |
|---|---|---|
| `plazo_cancelacion_horas` | 24 | RN-005 (se puede sobrescribir por procedimiento) |
| `limite_inasistencias` | 3 en 90 días | RN-020 |
| `campos_obligatorios_paciente` | nombre, documento, teléfono, contacto de emergencia | RN-003 |
| `ventana_reserva_portal_dias` | 60 | Módulo 16 |
| `recordatorio_cita_horas` | [48, 2] | Módulo 17 |
| `edad_mayoria` | 18 | RN-017 |
| `duracion_sesion_inactiva_min` | 15 (personal) / 30 (paciente) | Módulo 24 |
| `lineas_emergencia` | 911; línea de salud mental vigente del país | RN-008 |

## 5. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-ORG-001 | **Dado** dos organizaciones A y B, **cuando** un usuario de A llama a la API con el ID de un paciente de B, **entonces** recibe "no encontrado" y el intento queda en la bitácora. |
| CA-ORG-002 | **Dado** un consultorio marcado fuera de servicio, **cuando** recepción intenta agendar un procedimiento que lo requiere, **entonces** el sistema no ofrece ese recurso. |
| CA-ORG-003 | **Dado** un feriado configurado en la sede, **cuando** se consulta disponibilidad para ese día, **entonces** no aparecen espacios libres. |

## 6. Pantallas

- Administración › Organización (datos, marca, políticas).
- Administración › Sedes › Detalle (horario, feriados, recursos, personal).

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
