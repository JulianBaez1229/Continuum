---
id: MOD-24
codigo: SEC
titulo: Seguridad, privacidad y cumplimiento
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-03, MOD-07, MOD-23]
trazabilidad: ["RNF01", "RNF07", "RNF10", "RN01", "RN09"]
---

# 24 — Seguridad, privacidad y cumplimiento

## 1. Objetivo

Proteger la confidencialidad, integridad y disponibilidad de la información de salud, y cumplir la **Ley n.º 172-13** sobre protección de datos personales y las normas de confidencialidad de la información médica de la República Dominicana (RNF07). Se toman como referencia de buenas prácticas OWASP ASVS, ISO/IEC 27001 y, para organizaciones con pacientes de otros países, los principios de HIPAA y GDPR.

> Las obligaciones legales concretas (plazos, retención, registros ante autoridades) deben validarse con asesoría legal antes de producción.

## 2. Clasificación de la información

| Nivel | Ejemplos | Controles |
|---|---|---|
| **Público** | Nombre de la clínica, sedes, especialidades | — |
| **Interno** | Agendas (sin paciente), catálogos, reportes agregados | Autenticación |
| **Confidencial** | Datos demográficos y de contacto del paciente, citas | Autenticación + rol + auditoría |
| **Clínico** | Notas, diagnósticos, órdenes, formularios, mensajes | + relación tratante/equipo, cifrado de columna, auditoría de lectura |
| **Clínico sensible** | Salud mental, VIH, salud sexual y reproductiva, adicciones, genética | + permiso explícito del tratante, textos genéricos en notificaciones (RN-016) |

## 3. Controles técnicos

| Área | Control | Origen |
|---|---|---|
| Transporte | TLS 1.2+ (preferente 1.3), HSTS, sin contenido mixto | RNF01 |
| Reposo | Cifrado del disco/base de datos (AES-256) + cifrado de columna para documento de identidad, notas, mensajes | RNF01 |
| Claves | Gestor de secretos / KMS; rotación anual; nunca en el repositorio | RNF01 |
| Autenticación | Módulo 07 (MFA obligatorio para personal) | RNF01 |
| Autorización | Verificación en el servidor en cada solicitud (RBAC + ABAC); denegar por defecto; políticas de seguridad por fila para el tenant | RN01, RN02 |
| Aplicación | Validación de entradas, consultas parametrizadas, protección CSRF, CSP estricta, cabeceras seguras, límite de tasa | OWASP |
| Archivos | Escaneo antimalware, tipos permitidos, URLs firmadas con expiración | — |
| Sesiones | Tokens cortos, renovación rotativa, cierre por inactividad | RNF01 |
| Registro | Sin datos clínicos ni secretos en logs técnicos | RNF09 |
| Dependencias | Análisis de vulnerabilidades en CI (SCA), actualización mensual | RNF05 |
| Pruebas | SAST en cada PR, DAST antes de cada versión, prueba de penetración anual | — |
| Respaldo | Diario, cifrado, en ubicación separada; restauración probada trimestralmente; RTO < 24 h, RPO < 24 h | RNF10 |
| Entornos | Datos reales solo en producción; pruebas con datos ficticios o anonimizados | — |

## 4. Privacidad (Ley 172-13 y buenas prácticas)

| ID | Requisito | Prioridad | Origen |
|---|---|---|---|
| RF-SEC-001 | El sistema deberá registrar el consentimiento informado del paciente para el tratamiento de sus datos, con versión del texto, fecha y medio. | M | RNF07 |
| RF-SEC-002 | El sistema deberá permitir atender los derechos de acceso, rectificación, cancelación y oposición (ARCO): exportar los datos del paciente en formato legible, registrar solicitudes y su respuesta. | S | RNF07 |
| RF-SEC-003 | El sistema deberá aplicar minimización: cada pantalla y reporte muestra solo los datos necesarios para la función del rol. | M | RN02 |
| RF-SEC-004 | El sistema deberá mostrar el aviso de privacidad en el registro y en el portal. | M | RNF07 |
| RF-SEC-005 | El sistema deberá soportar la anonimización de datos para reportes analíticos y entornos de prueba. | S | GEN |
| RF-SEC-006 | El sistema deberá contar con un procedimiento documentado de respuesta a incidentes de seguridad, incluida la notificación a afectados y autoridades cuando corresponda. | M | RNF07 |
| RF-SEC-007 | El sistema deberá permitir configurar la región de alojamiento de los datos (ADR-007) y documentar a los encargados de tratamiento (proveedores de nube, correo, SMS). | S | RNF07 |
| RF-SEC-008 | El sistema deberá marcar automáticamente como sensibles los episodios y procedimientos de especialidades de categoría sensible (RN-016). | M | RN01 |

## 5. Modelo de amenazas (resumen STRIDE)

| Amenaza | Ejemplo | Mitigación principal |
|---|---|---|
| Suplantación | Robo de contraseña de un profesional | MFA, detección de anomalías |
| Manipulación | Alteración de una nota firmada | Inmutabilidad + hash + auditoría |
| Repudio | "Yo no prescribí eso" | Firma con reautenticación, bitácora encadenada |
| Divulgación | Recepcionista lee diagnósticos | ABAC, auditoría, minimización |
| Denegación de servicio | Saturación de la API | Límite de tasa, WAF, autoescalado |
| Elevación de privilegios | IDOR cambiando un ID en la URL | Autorización por recurso en el servidor, IDs no secuenciales, RLS por tenant |

## 6. Criterios de aceptación

| ID | Dado / Cuando / Entonces |
|---|---|
| CA-SEC-001 | **Dado** un acceso a la base de datos sin la clave de la aplicación, **cuando** se consulta la columna `numero_documento`, **entonces** el valor está cifrado. |
| CA-SEC-002 | **Dado** el escaneo de seguridad de una versión, **cuando** hay una vulnerabilidad de severidad alta o crítica sin mitigar, **entonces** la versión no se publica. |
| CA-SEC-003 | **Dado** una restauración de prueba del respaldo de ayer, **cuando** se ejecuta, **entonces** termina en menos de 24 horas y los datos son consistentes. |

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
