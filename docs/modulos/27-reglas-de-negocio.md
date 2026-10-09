---
id: MOD-27
codigo: RN
titulo: Reglas de negocio
version: 1.0.0
estado: Borrador
fase: MVP
responsable: Por asignar
dependencias: [Todos]
trazabilidad: ["RN01–RN10"]
---

# 27 — Reglas de negocio

Las reglas de negocio son políticas de la organización que determinan cómo deben ejecutarse sus procesos, con independencia de la tecnología (documento original, §3.4). Este es el **catálogo único** de reglas: los módulos funcionales las referencian por ID y no deben redefinirlas. Las reglas marcadas **[P]** tienen parámetros configurables por organización (módulo 06).

## 1. Reglas del documento original (generalizadas)

| ID | Regla | Descripción | Origen | Módulos |
|---|---|---|---|---|
| **RN-001** | Confidencialidad clínica | La información clínica solo puede ser consultada por el profesional tratante y los miembros del equipo de atención del episodio. Ningún otro usuario accede a notas o diagnósticos, salvo acceso de emergencia justificado y auditado. | RN01 | 03, 12, 23 |
| **RN-002** | Acceso según rol | Cada usuario accede únicamente a las funciones de su rol y de la sede asignada. El personal administrativo gestiona pacientes, citas y agendas, pero no visualiza contenido clínico. La dirección ve datos agregados. | RN02 | 03, 21 |
| **RN-003** | Registro previo **[P]** | No se puede programar una cita a un paciente sin sus datos obligatorios: nombre, documento de identidad (o alternativa válida para menores y extranjeros), teléfono y contacto de emergencia. La organización puede agregar campos obligatorios, no quitar estos. | RN03 | 08, 10 |
| **RN-004** | No solapamiento | Un profesional no puede tener dos citas en el mismo horario ni citas fuera de su disponibilidad. Tampoco un recurso (consultorio, equipo) puede estar en dos citas a la vez, ni un paciente en dos citas simultáneas. Excepción: procedimientos grupales hasta su capacidad. | RN04 | 09, 10 |
| **RN-005** | Cancelaciones **[P]** | Las citas deben cancelarse con al menos 24 horas de anticipación (ajustable por organización y por procedimiento). Una cancelación posterior o la ausencia del paciente se registra como inasistencia. Las cancelaciones hechas por la clínica nunca cuentan como inasistencia. | RN05 | 10, 11 |
| **RN-006** | Autoría e inmutabilidad clínica | Solo el profesional tratante (o del equipo con habilitación) puede registrar o modificar tratamientos, terapias, medicamentos y órdenes. Los registros clínicos no se eliminan; las correcciones se agregan como adendas o nuevas entradas. | RN06 | 12, 13 |
| **RN-007** | Formularios periódicos y umbral de riesgo **[P]** | La frecuencia de los formularios la define el profesional. Si un resultado supera el umbral de riesgo definido en el formulario, el sistema notifica de inmediato al profesional tratante. | RN07 | 14, 17 |
| **RN-008** | Comunicación segura, no de emergencia | La comunicación entre paciente y profesional se realiza solo por el canal interno. Este canal no sustituye la atención de emergencia: el sistema muestra las líneas de emergencia disponibles en la mensajería, en el portal y ante alertas críticas. | RN08 | 14, 16, 18 |
| **RN-009** | Acceso de la red de apoyo | Un familiar solo accede a información del paciente con su consentimiento expreso, o el de su tutor legal si es menor de edad. El paciente puede revocar el permiso en cualquier momento. | RN09 | 19 |
| **RN-010** | Restricción de infraestructura | El sistema requiere conexión a internet; su uso está condicionado a la infraestructura del centro y del usuario. | RN10 | 26 |

## 2. Reglas nuevas derivadas de la generalización multiespecialidad

| ID | Regla | Descripción | Origen | Módulos |
|---|---|---|---|---|
| **RN-011** | Versionado de catálogo | Los procedimientos, plantillas y formularios no se eliminan; se inactivan con fecha de vigencia. Cada cita, nota y respuesta conserva la versión del elemento de catálogo que se usó. | GEN | 11, 12, 14 |
| **RN-012** | Habilitación profesional | Un profesional solo puede atender, recibir citas y registrar notas de los procedimientos y especialidades en los que está habilitado y con licencia vigente. | GEN | 03, 09, 10 |
| **RN-013** | Duración por procedimiento | La duración de una cita es la duración total del procedimiento (preparación + atención + limpieza) definida en el catálogo. Solo un coordinador puede ajustarla en una cita concreta, con motivo. | GEN | 09, 10, 11 |
| **RN-014** | Requisitos previos | Si un procedimiento exige requisitos previos (consentimiento informado, orden médica, ayuno, estudios, tutor), el sistema los informa al agendar y advierte al iniciar la atención si no se cumplen. El consentimiento informado es bloqueante. | GEN | 10, 11, 12 |
| **RN-015** | Aislamiento por organización | Ningún dato de una organización puede ser visto, modificado ni inferido por usuarios de otra organización. | GEN | 06, 24 |
| **RN-016** | Categorías sensibles | La información de salud mental, VIH, salud sexual y reproductiva, adicciones y genética se marca como sensible. Dentro del equipo de atención, solo el tratante y quienes él autorice ven el detalle; las notificaciones externas usan textos genéricos. | GEN (extiende RN01) | 12, 17, 24 |
| **RN-017** | Menores de edad **[P]** | Un paciente menor de la edad de mayoría configurada (18 por defecto) debe tener un tutor legal registrado; los consentimientos los firma el tutor. Al alcanzar la mayoría de edad, el acceso del tutor termina. | GEN (extiende RN09) | 08, 16, 19 |
| **RN-018** | Facultad de prescripción | Solo profesionales con facultad legal de prescribir pueden emitir órdenes de medicamentos; los medicamentos controlados requieren además los controles adicionales que exija la normativa. | GEN | 13 |
| **RN-019** | Prioridad en lista de espera | Al liberarse un espacio, se ofrece primero al paciente con mayor prioridad clínica asignada por el profesional y, a igual prioridad, al de mayor antigüedad en la lista. La oferta expira en el plazo configurado (por defecto 2 horas). | GEN | 10 |
| **RN-020** | Inasistencias reiteradas **[P]** | Cuando un paciente acumula el límite de inasistencias (por defecto 3 en 90 días), las nuevas citas requieren confirmación explícita de recepción. La medida nunca bloquea la atención clínica urgente. | GEN | 10 |

## 3. Cómo se aplican

| Regla | Punto de control en el sistema |
|---|---|
| RN-001, RN-002, RN-015, RN-016 | Política de autorización del backend (en cada solicitud) + filtros de consulta |
| RN-003 | Validación al crear cita |
| RN-004, RN-012, RN-013 | Motor de disponibilidad + validación transaccional al guardar cita |
| RN-005, RN-020 | Servicio de cancelación + trabajo programado de cierre del día |
| RN-006, RN-011 | Dominio: entidades inmutables tras firma/publicación |
| RN-007, RN-008 | Evaluación al enviar formulario + componente de UI de emergencia |
| RN-009, RN-017 | Servicio de autorizaciones + trabajo programado diario de mayoría de edad |
| RN-014 | Validación al agendar (informativa) y al iniciar el encuentro (bloqueante para consentimiento) |
| RN-018 | Política de autorización por tipo de orden |
| RN-019 | Servicio de lista de espera, disparado por `CitaCancelada` |

Cada regla debe tener **pruebas automatizadas** dedicadas (RNF-MAN-003: ≥ 90 % de cobertura en reglas).

## Historial de cambios

| Versión | Fecha | Autor | Cambio | Solicitud |
|---|---|---|---|---|
| 1.0.0 | 2026-10-02 | Equipo | Versión inicial | — |
