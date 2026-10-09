---
id: MOD-26
codigo: RNF
titulo: Requisitos no funcionales
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [MOD-04, MOD-24, MOD-30]
trazabilidad: ["RNF01–RNF10"]
---

# 26 — Requisitos no funcionales

Los requisitos no funcionales establecen las restricciones y atributos de calidad bajo los cuales opera el sistema (Pressman, 2010). Se conservan los diez del documento original, elevados a nivel empresarial y con una métrica verificable cada uno.

## 1. Trazabilidad con el documento original

| Original | Atributo | Nuevos IDs |
|---|---|---|
| RNF01 | Seguridad | RNF-SEG-001 a 004 |
| RNF02 | Usabilidad | RNF-USA-001 a 003 |
| RNF03 | Disponibilidad | RNF-DIS-001 a 002 |
| RNF04 | Rendimiento | RNF-REN-001 a 004 |
| RNF05 | Mantenibilidad | RNF-MAN-001 a 004 |
| RNF06 | Documentación y capacitación | RNF-DOC-001 a 003 |
| RNF07 | Cumplimiento legal | RNF-LEG-001 a 002 |
| RNF08 | Compatibilidad y accesibilidad | RNF-COM-001 a 003 |
| RNF09 | Auditoría | RNF-AUD-001 |
| RNF10 | Respaldo | RNF-RES-001 a 002 |
| — | Escalabilidad | RNF-ESC-001 a 002 (nuevo) |
| — | Internacionalización | RNF-I18-001 (nuevo) |
| — | Observabilidad | RNF-OBS-001 (nuevo) |

## 2. Requisitos

| ID | Atributo | Requisito | Métrica / verificación | Origen |
|---|---|---|---|---|
| RNF-SEG-001 | Seguridad | Todos los usuarios se autentican con usuario y contraseña robusta; el personal, además, con un segundo factor. | Prueba de acceso sin MFA = denegado (módulo 07) | RNF01 |
| RNF-SEG-002 | Seguridad | La información se cifra en tránsito (TLS 1.2+) y en reposo (AES-256). | Escaneo TLS calificación A; verificación de cifrado de BD | RNF01 |
| RNF-SEG-003 | Seguridad | Cero vulnerabilidades altas o críticas abiertas al publicar una versión. | Reporte SAST/SCA/DAST | GEN |
| RNF-SEG-004 | Seguridad | La aplicación cumple OWASP ASVS nivel 2. | Lista de verificación ASVS firmada | GEN |
| RNF-USA-001 | Usabilidad | Un paciente puede consultar su próxima cita o completar un formulario en no más de 3 pasos, sin capacitación previa. | Prueba de usabilidad con 5 usuarios: ≥ 4 lo logran sin ayuda | RNF02 |
| RNF-USA-002 | Usabilidad | Recepción agenda una cita de un paciente existente en menos de 60 segundos. | Medición en prueba con usuarios | GEN |
| RNF-USA-003 | Usabilidad | Puntuación SUS (System Usability Scale) ≥ 75 para personal y pacientes. | Encuesta SUS en piloto | GEN |
| RNF-DIS-001 | Disponibilidad | Disponibilidad ≥ **99,5 %** mensual en producción (el original pedía 95 %; se eleva al nivel empresarial, con 95 % como mínimo contractual del MVP académico). | Monitoreo externo de disponibilidad | RNF03 |
| RNF-DIS-002 | Disponibilidad | Las ventanas de mantenimiento planificado se anuncian con 72 h y ocurren fuera del horario de atención. | Registro de mantenimientos | GEN |
| RNF-REN-001 | Rendimiento | Respuesta ≤ **3 s** (percentil 95) con hasta 100 usuarios concurrentes por organización. | Prueba de carga | RNF04 |
| RNF-REN-002 | Rendimiento | Operaciones frecuentes (búsqueda de paciente, carga de agenda, abrir HCE) ≤ 1 s (p95). | Prueba de carga | GEN |
| RNF-REN-003 | Rendimiento | La plataforma soporta 1 000 usuarios concurrentes en total con escalado horizontal. | Prueba de carga | GEN |
| RNF-REN-004 | Rendimiento | Las alertas de riesgo se entregan al profesional en ≤ 60 s. | Prueba funcional cronometrada | RN07 |
| RNF-MAN-001 | Mantenibilidad | El código sigue un estándar de codificación definido y verificado por linter en CI. | Pipeline sin errores de linter | RNF05 |
| RNF-MAN-002 | Mantenibilidad | El código está organizado en módulos independientes (módulo 04) sin dependencias cíclicas. | Prueba de arquitectura automatizada | RNF05 |
| RNF-MAN-003 | Mantenibilidad | Cobertura de pruebas unitarias ≥ 70 % en el dominio y ≥ 90 % en reglas de negocio. | Reporte de cobertura | GEN |
| RNF-MAN-004 | Mantenibilidad | Una especialidad nueva se configura sin cambios de código (catálogo). | CA-CAT-004 | GEN |
| RNF-DOC-001 | Documentación | Existe documentación técnica (arquitectura, API OpenAPI, despliegue) actualizada en cada versión. | Revisión en la lista de publicación | RNF06 |
| RNF-DOC-002 | Documentación | Existen manual de usuario por rol y guías de capacitación. | Entregables del módulo 30 | RNF06 |
| RNF-DOC-003 | Documentación | Ayuda contextual en las pantallas principales. | Revisión de UI | GEN |
| RNF-LEG-001 | Legal | Cumplimiento de la Ley n.º 172-13 y normas de confidencialidad médica. | Lista de verificación legal (módulo 24) | RNF07 |
| RNF-LEG-002 | Legal | Registro de consentimientos y atención de derechos ARCO. | RF-SEC-001, RF-SEC-002 | RNF07 |
| RNF-COM-001 | Compatibilidad | Funciona en las dos últimas versiones de Chrome, Edge, Firefox y Safari. | Matriz de pruebas | RNF08 |
| RNF-COM-002 | Compatibilidad | Diseño adaptable desde 360 px de ancho (teléfono) hasta escritorio. | Pruebas en dispositivos | RNF08 |
| RNF-COM-003 | Accesibilidad | Cumple WCAG 2.2 AA. | Auditoría automatizada (axe) + revisión manual | RNF08 |
| RNF-AUD-001 | Auditoría | Toda consulta o modificación de información clínica queda en la bitácora con usuario, fecha y hora (módulo 23). | CA-AUD-001 | RNF09 |
| RNF-RES-001 | Respaldo | Copias de seguridad diarias cifradas, retención 30 días diarios + 12 mensuales. | Registro de respaldos | RNF10 |
| RNF-RES-002 | Respaldo | Restauración completa en menos de 24 h (RTO) con pérdida máxima de 24 h (RPO); objetivo empresarial RPO ≤ 1 h con respaldo continuo. | Simulacro trimestral | RNF10 |
| RNF-ESC-001 | Escalabilidad | La arquitectura admite agregar organizaciones y sedes sin cambios de código. | Prueba con 2 organizaciones | GEN |
| RNF-ESC-002 | Escalabilidad | El almacenamiento de documentos escala independiente de la base de datos. | Arquitectura (módulo 04) | GEN |
| RNF-I18-001 | Internacionalización | Textos de interfaz externalizados; español por defecto, preparado para inglés. Fechas, horas y moneda según la configuración regional de la organización. | Revisión de código | GEN |
| RNF-OBS-001 | Observabilidad | Métricas, trazas y registros centralizados con alertas de errores y latencia. | Tablero de observabilidad | GEN |

## 3. Restricción de infraestructura

El sistema requiere conexión a internet para funcionar; su uso está condicionado a la disponibilidad de la infraestructura tecnológica del centro y del usuario (RN-010). Como mitigación, el borrador de notas se guarda localmente si se pierde la conexión y se sincroniza al recuperarla (Fase 2).

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
