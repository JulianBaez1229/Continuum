---
id: MOD-16
codigo: POR
titulo: Portal del paciente
version: 1.0.0
estado: Borrador
fase: MVP (básico) / Fase 2 (completo)
responsable: Por asignar
dependencias: [MOD-07, MOD-10, MOD-14, MOD-18, MOD-22, MOD-28]
trazabilidad: ["RF02", "RF06", "RF08", "RF09", "RF11", "RNF02", "RNF08"]
---

# 16 — Portal del paciente

## 1. Objetivo

Dar al paciente un espacio digital único, accesible desde cualquier dispositivo, para gestionar sus citas, completar formularios, comunicarse con su profesional y acceder a sus recursos, de forma que el cuidado continúe entre encuentros.

## 2. Funciones por fase

| Función | MVP | Fase 2 | Fase 3 |
|---|---|---|---|
| Activar cuenta e iniciar sesión | ✔ | | |
| Ver próximas citas y anteriores | ✔ | | |
| Confirmar / cancelar cita | ✔ | | |
| Completar formularios asignados | ✔ | | |
| Ver instrucciones de preparación | ✔ | | |
| Solicitar / autoagendar citas | | ✔ | |
| Mensajería segura con el profesional | | ✔ | |
| Ver evolución (si el profesional lo habilita) | | ✔ | |
| Ver órdenes y recetas liberadas | | ✔ | |
| Descargar documentos (certificados, resultados liberados) | | ✔ | |
| Actualizar datos de contacto | | ✔ | |
| Recursos educativos asignados | | | ✔ |
| Gestionar red de apoyo (autorizar / revocar) | | | ✔ |
| Ver quién accedió a su expediente | | | ✔ |
| Pagos en línea | | | ✔ |

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-POR-001 | El sistema deberá mostrar al iniciar sesión un inicio con la próxima cita, formularios pendientes y mensajes nuevos. | M | RNF02 |
| RF-POR-002 | El sistema deberá permitir al paciente consultar su próxima cita o completar un formulario en no más de 3 pasos, sin capacitación previa (RNF02). | M | RNF02 |
| RF-POR-003 | El sistema deberá mostrar en cada cita: fecha, hora, sede con mapa, profesional, procedimiento (nombre para paciente), modalidad e instrucciones de preparación. | M | RF02 |
| RF-POR-004 | El sistema deberá permitir añadir la cita al calendario del dispositivo (archivo .ics). | C | GEN |
| RF-POR-005 | El sistema deberá mostrar de forma permanente un acceso a "¿Necesitas ayuda urgente?" con las líneas de emergencia configuradas (RN-008). | M | RN08 |
| RF-POR-006 | El sistema deberá funcionar como aplicación web adaptable en teléfonos, tabletas y computadoras, e instalable como PWA. | M / C (PWA) | RNF08 |
| RF-POR-007 | El sistema deberá cumplir WCAG 2.2 nivel AA (contraste, navegación por teclado, lector de pantalla, tamaño de texto ajustable). | S | RNF08 |
| RF-POR-008 | El sistema deberá permitir que un tutor gestione el portal de un paciente menor desde su propia cuenta, cambiando de perfil. | S | RN-017 |

## 4. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-POR-001 | **Dado** un paciente que inicia sesión en su teléfono, **cuando** llega al inicio, **entonces** ve su próxima cita sin desplazarse. |
| CA-POR-002 | **Dado** un formulario pendiente, **cuando** el paciente toca "Completar", **entonces** lo envía en 3 pasos o menos (inicio → formulario → enviar). |
| CA-POR-003 | **Dado** cualquier pantalla del portal, **cuando** el paciente busca ayuda urgente, **entonces** encuentra las líneas de emergencia en un toque. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
