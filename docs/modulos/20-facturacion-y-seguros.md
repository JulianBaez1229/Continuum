---
id: MOD-20
codigo: FAC
titulo: Facturación, caja y seguros (extensión)
version: 1.0.0
estado: Borrador
fase: Fase 3
responsable: Por asignar
dependencias: [MOD-10, MOD-11, MOD-08, MOD-25]
trazabilidad: ["EXT"]
---

# 20 — Facturación, caja y seguros (extensión empresarial)

> **Este módulo no forma parte del documento original.** Se incluye porque una clínica multiespecialidad a nivel empresarial casi siempre lo necesita. El equipo puede decidir mantenerlo fuera de alcance; en ese caso se marca `estado: Obsoleto` sin afectar al resto.

## 1. Objetivo

Registrar los cobros de los servicios prestados, gestionar la cobertura de las ARS y cuadrar la caja diaria, a partir de las citas completadas y los procedimientos del catálogo.

## 2. Flujo general

```
Cita completada → Cargo generado (procedimiento + tarifa vigente)
                → ¿Tiene seguro? → Autorización ARS → Copago del paciente + Monto a reclamar a la ARS
                → Cobro en caja (efectivo, tarjeta, transferencia) → Comprobante fiscal
                → Cierre de caja diario → Reclamación mensual a ARS
```

## 3. Requisitos funcionales

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-FAC-001 | El sistema deberá mantener tarifarios por procedimiento: tarifa privada y tarifa por ARS/plan, con vigencia. | C | EXT |
| RF-FAC-002 | El sistema deberá generar automáticamente el cargo al completar una cita, aplicando la tarifa vigente en la fecha del servicio. | C | EXT |
| RF-FAC-003 | El sistema deberá registrar el número de autorización de la ARS, el monto cubierto y el copago. | C | EXT |
| RF-FAC-004 | El sistema deberá registrar cobros con varios medios de pago y emitir el comprobante correspondiente, preparado para integrarse con la facturación electrónica de la DGII (e-CF). | C | EXT |
| RF-FAC-005 | El sistema deberá permitir el cierre de caja por usuario y sede con arqueo y diferencias. | C | EXT |
| RF-FAC-006 | El sistema deberá generar el reporte de reclamación por ARS en el formato de cada aseguradora. | W | EXT |
| RF-FAC-007 | El sistema deberá permitir pagos en línea desde el portal mediante una pasarela de pago certificada; Continuum no almacenará datos de tarjetas. | W | EXT |

## 4. Consideraciones

- Los requisitos fiscales (comprobantes, e-CF) deben validarse con un contador y la normativa vigente de la DGII antes de diseñar.
- El rol `FACTURACION` no tiene acceso a información clínica; ve solo procedimiento, fecha, profesional y montos.

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
