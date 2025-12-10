# ?? Guía de Pruebas - EmailAutomationService Refactorizado

## ?? Pre-requisitos para Pruebas

### 1. Verificar datos en la BD

#### Tabla `usuario` (debe tener Admin/Analistas)
```sql
SELECT 
    u.id_usuario,
    u.username,
    u.estado,
    u.id_rol_sistema,
    rs.nombre AS rol_nombre,
    p.correo_corporativo
FROM usuario u
INNER JOIN rol_sistema rs ON u.id_rol_sistema = rs.id_rol_sistema
LEFT JOIN personal p ON u.id_personal = p.id_personal
WHERE u.estado = 'ACTIVO'
  AND u.id_rol_sistema IN (1, 2)  -- 1=Admin, 2=Analista
ORDER BY u.id_rol_sistema, u.username;
```

**Resultado esperado:**
```
| id_usuario | username  | estado | id_rol_sistema | rol_nombre     | correo_corporativo        |
|------------|-----------|--------|----------------|----------------|---------------------------|
| 1          | admin     | ACTIVO | 1              | Administrador  | admin@empresa.com         |
| 2          | admin2    | ACTIVO | 1              | Administrador  | admin2@empresa.com        |
| 5          | analista1 | ACTIVO | 2              | Analista       | analista1@empresa.com     |
| 6          | analista2 | ACTIVO | 2              | Analista       | analista2@empresa.com     |
```

**? Si NO tienes datos, ejecuta:**
```sql
-- Crear usuarios de prueba con correos
UPDATE usuario 
SET id_rol_sistema = 1,  -- Administrador
    estado = 'ACTIVO'
WHERE id_usuario = 1;

UPDATE usuario 
SET id_rol_sistema = 2,  -- Analista
    estado = 'ACTIVO'
WHERE id_usuario = 2;

-- Asegurar que tengan correo en la tabla personal
UPDATE personal 
SET correo_corporativo = 'admin@empresa.com' 
WHERE id_personal = (SELECT id_personal FROM usuario WHERE id_usuario = 1);

UPDATE personal 
SET correo_corporativo = 'analista@empresa.com' 
WHERE id_personal = (SELECT id_personal FROM usuario WHERE id_usuario = 2);
```

---

#### Tabla `email_config` (configuración activada)
```sql
SELECT * FROM email_config WHERE id = 1;
```

**Resultado esperado:**
```
| id | resumen_diario | destinatario_resumen | hora_resumen | envio_inmediato |
|----|----------------|---------------------|--------------|-----------------|
| 1  | 1              | (cualquiera/null)   | 08:00:00     | 1               |
```

**? Si NO existe, crea la configuración:**
```sql
INSERT INTO email_config (resumen_diario, destinatario_resumen, hora_resumen, envio_inmediato)
VALUES (1, NULL, '08:00:00', 1);
```

**?? IMPORTANTE:** `destinatario_resumen` YA NO SE USA, el servicio ahora obtiene los destinatarios dinámicamente.

---

#### Tabla `alerta` (crear alertas de prueba)
```sql
SELECT 
    a.id_alerta,
    a.id_solicitud,
    a.nivel,
    a.mensaje,
    a.estado,
    a.fecha_creacion
FROM alerta a
WHERE a.estado = 'ACTIVA'
  AND a.nivel IN ('CRITICO', 'ALTO')
ORDER BY a.fecha_creacion DESC;
```

**? Si NO hay alertas, crea algunas de prueba:**
```sql
-- Insertar 3 alertas CRÍTICAS de prueba
INSERT INTO alerta (id_solicitud, nivel, mensaje, estado, fecha_creacion)
VALUES 
(1, 'CRITICO', 'Solicitud vence HOY - Requiere acción inmediata', 'ACTIVA', NOW()),
(2, 'CRITICO', 'SLA superado - Escalación necesaria', 'ACTIVA', NOW()),
(3, 'ALTO', 'Faltan 2 días para vencimiento', 'ACTIVA', NOW());
```

---

## ?? Caso de Prueba 1: Envío CON Alertas

### Objetivo
Verificar que el servicio envía el resumen con alertas a todos los Admin/Analistas activos.

### Preparación
```sql
-- Asegurar que hay alertas CRITICO/ALTO
INSERT INTO alerta (id_solicitud, nivel, mensaje, estado, fecha_creacion)
VALUES 
(1, 'CRITICO', 'Prueba: Alerta Crítica 1', 'ACTIVA', NOW()),
(2, 'ALTO', 'Prueba: Alerta Alta 2', 'ACTIVA', NOW());
```

### Ejecución (Swagger/Postman)
```http
POST https://localhost:7xxx/api/email/send-summary
Content-Type: application/json
```

**Cuerpo:** (vacío, el endpoint no requiere parámetros)

### Respuesta Esperada
```json
{
  "exito": true,
  "mensaje": "Resumen diario enviado exitosamente a 4 destinatario(s) con 2 alertas",
  "cantidadAlertas": 2,
  "correoEnviado": true,
  "destinatarios": [
    "admin@empresa.com",
    "admin2@empresa.com",
    "analista1@empresa.com",
    "analista2@empresa.com"
  ],
  "resultadosEnvios": [
    {
      "destinatario": "admin@empresa.com",
      "exitoso": true,
      "mensajeError": null
    },
    {
      "destinatario": "admin2@empresa.com",
      "exitoso": true,
      "mensajeError": null
    },
    // ...
  ]
}
```

### Verificación en BD
```sql
-- Verificar registros en email_log
SELECT 
    tipo,
    destinatarios,
    estado,
    error_detalle,
    fecha
FROM email_log
WHERE tipo = 'RESUMEN_CON_ALERTAS'
ORDER BY fecha DESC
LIMIT 10;
```

**Resultado esperado:**
```
| tipo                  | destinatarios           | estado | error_detalle                        | fecha               |
|-----------------------|-------------------------|--------|--------------------------------------|---------------------|
| RESUMEN_CON_ALERTAS   | admin@empresa.com       | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:30:00 |
| RESUMEN_CON_ALERTAS   | admin2@empresa.com      | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:30:01 |
| RESUMEN_CON_ALERTAS   | analista1@empresa.com   | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:30:02 |
| RESUMEN_CON_ALERTAS   | analista2@empresa.com   | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:30:03 |
```

### Verificación en Correo
**Asunto esperado:**
```
?? [RESUMEN DIARIO SLA] 15/01/2024 - 2 alertas críticas
```

**Contenido esperado:**
- ?? Header morado gradient con título "Resumen Diario de Alertas SLA"
- ?? Estadísticas: Total Alertas (2), Críticas (1), Altas (1)
- ?? Tabla con las 2 alertas detalladas
- ??? Badges de nivel (CRÍTICO rojo, ALTO naranja)

---

## ?? Caso de Prueba 2: Envío SIN Alertas (NUEVO)

### Objetivo
Verificar que el servicio envía un correo positivo "Sin Pendientes" cuando NO hay alertas críticas/altas.

### Preparación
```sql
-- Desactivar TODAS las alertas CRITICO/ALTO
UPDATE alerta 
SET estado = 'RESUELTA' 
WHERE estado = 'ACTIVA' 
  AND nivel IN ('CRITICO', 'ALTO');

-- Verificar que no hay alertas activas
SELECT COUNT(*) FROM alerta 
WHERE estado = 'ACTIVA' AND nivel IN ('CRITICO', 'ALTO');
-- Resultado esperado: 0
```

### Ejecución (Swagger/Postman)
```http
POST https://localhost:7xxx/api/email/send-summary
Content-Type: application/json
```

### Respuesta Esperada
```json
{
  "exito": true,
  "mensaje": "Resumen diario enviado exitosamente a 4 destinatario(s) (Sin alertas pendientes)",
  "cantidadAlertas": 0,
  "correoEnviado": true,
  "destinatarios": [
    "admin@empresa.com",
    "admin2@empresa.com",
    "analista1@empresa.com",
    "analista2@empresa.com"
  ],
  "resultadosEnvios": [
    {
      "destinatario": "admin@empresa.com",
      "exitoso": true,
      "mensajeError": null
    },
    // ...
  ]
}
```

### Verificación en BD
```sql
-- Verificar registros en email_log
SELECT 
    tipo,
    destinatarios,
    estado,
    error_detalle,
    fecha
FROM email_log
WHERE tipo = 'RESUMEN_SIN_PENDIENTES'
ORDER BY fecha DESC
LIMIT 10;
```

**Resultado esperado:**
```
| tipo                     | destinatarios           | estado | error_detalle                           | fecha               |
|--------------------------|-------------------------|--------|-----------------------------------------|---------------------|
| RESUMEN_SIN_PENDIENTES   | admin@empresa.com       | OK     | Resumen 'Sin Pendientes' enviado        | 2024-01-15 10:35:00 |
| RESUMEN_SIN_PENDIENTES   | admin2@empresa.com      | OK     | Resumen 'Sin Pendientes' enviado        | 2024-01-15 10:35:01 |
| RESUMEN_SIN_PENDIENTES   | analista1@empresa.com   | OK     | Resumen 'Sin Pendientes' enviado        | 2024-01-15 10:35:02 |
| RESUMEN_SIN_PENDIENTES   | analista2@empresa.com   | OK     | Resumen 'Sin Pendientes' enviado        | 2024-01-15 10:35:03 |
```

### Verificación en Correo
**Asunto esperado:**
```
? [RESUMEN DIARIO SLA] 15/01/2024 - Todo en Orden
```

**Contenido esperado:**
- ? Header verde gradient con ícono de éxito (80px)
- ?? Título: "Resumen Diario SLA"
- ?? Mensaje principal: "¡Todo en Orden!"
- ?? Estadísticas: 0 Alertas Críticas, 0 Alertas Altas
- ?? Información sobre el monitoreo activo
- ? Estado: OPERATIVO

**Diseño esperado:**
- Colores verdes (#4caf50, #45a049)
- Mensaje positivo y tranquilizador
- Sin tabla de alertas (porque no hay)
- Información de que el sistema está activo

---

## ?? Caso de Prueba 3: Sin Administradores/Analistas Activos

### Objetivo
Verificar que el servicio maneja correctamente el caso de no tener destinatarios.

### Preparación
```sql
-- Desactivar TODOS los Admin/Analistas (temporalmente)
UPDATE usuario 
SET estado = 'INACTIVO' 
WHERE id_rol_sistema IN (1, 2);

-- Verificar
SELECT COUNT(*) FROM usuario 
WHERE estado = 'ACTIVO' AND id_rol_sistema IN (1, 2);
-- Resultado esperado: 0
```

### Ejecución (Swagger/Postman)
```http
POST https://localhost:7xxx/api/email/send-summary
Content-Type: application/json
```

### Respuesta Esperada
```json
{
  "exito": false,
  "mensaje": "? No se encontraron Administradores ni Analistas ACTIVOS con correo corporativo",
  "cantidadAlertas": 0,
  "correoEnviado": false,
  "destinatarios": [],
  "resultadosEnvios": []
}
```

### Verificación en BD
```sql
-- Verificar registro de error en email_log
SELECT 
    tipo,
    destinatarios,
    estado,
    error_detalle,
    fecha
FROM email_log
WHERE tipo = 'RESUMEN_DIARIO'
  AND estado = 'ERROR'
ORDER BY fecha DESC
LIMIT 1;
```

**Resultado esperado:**
```
| tipo           | destinatarios | estado | error_detalle                                                                      | fecha               |
|----------------|---------------|--------|------------------------------------------------------------------------------------|---------------------|
| RESUMEN_DIARIO | (vacío)       | ERROR  | ? No se encontraron Administradores ni Analistas ACTIVOS con correo corporativo   | 2024-01-15 10:40:00 |
```

### Restauración
```sql
-- Reactivar usuarios para siguientes pruebas
UPDATE usuario 
SET estado = 'ACTIVO' 
WHERE id_rol_sistema IN (1, 2);
```

---

## ?? Caso de Prueba 4: Resumen Diario Desactivado

### Objetivo
Verificar que el servicio respeta la configuración de `ResumenDiario = false`.

### Preparación
```sql
-- Desactivar el resumen diario
UPDATE email_config 
SET resumen_diario = 0 
WHERE id = 1;

-- Verificar
SELECT resumen_diario FROM email_config WHERE id = 1;
-- Resultado esperado: 0
```

### Ejecución (Swagger/Postman)
```http
POST https://localhost:7xxx/api/email/send-summary
Content-Type: application/json
```

### Respuesta Esperada
```json
{
  "exito": true,
  "mensaje": "Resumen diario desactivado en la configuración",
  "cantidadAlertas": 0,
  "correoEnviado": false,
  "destinatarios": [],
  "resultadosEnvios": []
}
```

### Verificación en BD
```sql
-- Verificar que NO se registró en email_log
SELECT COUNT(*) 
FROM email_log 
WHERE tipo LIKE 'RESUMEN%'
  AND fecha > DATE_SUB(NOW(), INTERVAL 5 MINUTE);
-- Resultado esperado: 0
```

### Restauración
```sql
-- Reactivar para siguientes pruebas
UPDATE email_config 
SET resumen_diario = 1 
WHERE id = 1;
```

---

## ?? Caso de Prueba 5: Fallo de SMTP (Error de Red)

### Objetivo
Verificar que el servicio maneja correctamente errores de envío y no detiene los demás envíos.

### Preparación
```sql
-- Asegurar que hay múltiples destinatarios
-- Crear un correo inválido para forzar error
UPDATE personal 
SET correo_corporativo = 'invalido@dominio-que-no-existe-xyz123.com' 
WHERE id_personal = (SELECT id_personal FROM usuario WHERE id_usuario = 2 LIMIT 1);
```

### Ejecución (Swagger/Postman)
```http
POST https://localhost:7xxx/api/email/send-summary
Content-Type: application/json
```

### Respuesta Esperada
```json
{
  "exito": true,
  "mensaje": "Resumen enviado parcialmente: 3 exitosos, 1 fallidos de 4 destinatarios con 2 alertas",
  "cantidadAlertas": 2,
  "correoEnviado": true,
  "destinatarios": [
    "admin@empresa.com",
    "invalido@dominio-que-no-existe-xyz123.com",
    "analista1@empresa.com",
    "analista2@empresa.com"
  ],
  "resultadosEnvios": [
    {
      "destinatario": "admin@empresa.com",
      "exitoso": true,
      "mensajeError": null
    },
    {
      "destinatario": "invalido@dominio-que-no-existe-xyz123.com",
      "exitoso": false,
      "mensajeError": "SmtpException: Mailbox unavailable"
    },
    {
      "destinatario": "analista1@empresa.com",
      "exitoso": true,
      "mensajeError": null
    },
    {
      "destinatario": "analista2@empresa.com",
      "exitoso": true,
      "mensajeError": null
    }
  ]
}
```

### Verificación en BD
```sql
-- Verificar que se registraron TODOS los intentos (exitosos y fallidos)
SELECT 
    tipo,
    destinatarios,
    estado,
    error_detalle,
    fecha
FROM email_log
WHERE tipo LIKE 'RESUMEN%'
ORDER BY fecha DESC
LIMIT 10;
```

**Resultado esperado:**
```
| tipo                  | destinatarios                                   | estado | error_detalle                        | fecha               |
|-----------------------|-------------------------------------------------|--------|--------------------------------------|---------------------|
| RESUMEN_CON_ALERTAS   | admin@empresa.com                               | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:45:00 |
| RESUMEN_CON_ALERTAS   | invalido@dominio-que-no-existe-xyz123.com       | ERROR  | SmtpException: Mailbox unavailable   | 2024-01-15 10:45:01 |
| RESUMEN_CON_ALERTAS   | analista1@empresa.com                           | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:45:02 |
| RESUMEN_CON_ALERTAS   | analista2@empresa.com                           | OK     | Resumen enviado con 2 alertas        | 2024-01-15 10:45:03 |
```

### Restauración
```sql
-- Restaurar correo válido
UPDATE personal 
SET correo_corporativo = 'admin2@empresa.com' 
WHERE id_personal = (SELECT id_personal FROM usuario WHERE id_usuario = 2 LIMIT 1);
```

---

## ?? Verificación de Logs en Consola

Al ejecutar el endpoint, deberías ver logs similares a:

### Caso CON Alertas:
```
=================================================================
?? [RESUMEN DIARIO] INICIANDO proceso automático
=================================================================

?? [PASO 1/6] Consultando EmailConfig...
? Configuración validada: ResumenDiario = ACTIVADO

=================================================================
?? [PASO 2/6] Obteniendo destinatarios desde BD (Admin & Analistas)
=================================================================
?? Consultando destinatarios (Admin & Analistas) en BD...
? Consulta completada: 4 correos únicos encontrados
? Se encontraron 4 destinatarios:
   ?? admin@empresa.com (Administrador)
   ?? admin2@empresa.com (Administrador)
   ?? analista1@empresa.com (Analista)
   ?? analista2@empresa.com (Analista)

=================================================================
?? [PASO 3/6] Consultando alertas CRITICO/ALTO en BD
=================================================================
?? Consulta completada: 2 alertas encontradas

=================================================================
?? [PASO 4/6] Generando contenido HTML del correo
=================================================================
?? Se encontraron 2 alertas. Generando HTML de reporte...
?? Primeras 3 alertas a incluir:
   ?? [1] CRITICO: Prueba: Alerta Crítica 1
   ?? [2] ALTO: Prueba: Alerta Alta 2
? HTML con alertas generado: 5432 caracteres

=================================================================
?? [PASO 5/6] Enviando correos a 4 destinatarios
=================================================================
?? Enviando a: admin@empresa.com
? Enviado exitosamente a admin@empresa.com
?? Enviando a: admin2@empresa.com
? Enviado exitosamente a admin2@empresa.com
?? Enviando a: analista1@empresa.com
? Enviado exitosamente a analista1@empresa.com
?? Enviando a: analista2@empresa.com
? Enviado exitosamente a analista2@empresa.com

=================================================================
? [PASO 6/6] Proceso completado
=================================================================
?? Estadísticas:
   ? Exitosos: 4
   ? Fallidos: 0
   ?? Alertas incluidas: 2
   ?? Destinatarios: 4
   ?? Tipo: RESUMEN_CON_ALERTAS
```

### Caso SIN Alertas:
```
=================================================================
?? [RESUMEN DIARIO] INICIANDO proceso automático
=================================================================

?? [PASO 1/6] Consultando EmailConfig...
? Configuración validada: ResumenDiario = ACTIVADO

=================================================================
?? [PASO 2/6] Obteniendo destinatarios desde BD (Admin & Analistas)
=================================================================
? Se encontraron 4 destinatarios

=================================================================
?? [PASO 3/6] Consultando alertas CRITICO/ALTO en BD
=================================================================
?? Consulta completada: 0 alertas encontradas

=================================================================
?? [PASO 4/6] Generando contenido HTML del correo
=================================================================
? No hay alertas críticas/altas. Generando HTML 'Sin Pendientes'...
? HTML 'Sin Pendientes' generado: 4821 caracteres

=================================================================
?? [PASO 5/6] Enviando correos a 4 destinatarios
=================================================================
?? Enviando a: admin@empresa.com
? Enviado exitosamente a admin@empresa.com
[... 3 más ...]

=================================================================
? [PASO 6/6] Proceso completado
=================================================================
?? Estadísticas:
   ? Exitosos: 4
   ? Fallidos: 0
   ?? Alertas incluidas: 0
   ?? Destinatarios: 4
   ?? Tipo: RESUMEN_SIN_PENDIENTES
```

---

## ? Checklist de Verificación

Después de ejecutar las pruebas, verifica:

- [ ] Se envían correos a TODOS los Admin/Analistas activos (no solo 1)
- [ ] Cuando NO hay alertas, se envía correo con mensaje "Todo en Orden"
- [ ] Cuando SÍ hay alertas, se envía correo con tabla de alertas
- [ ] Cada envío se registra individualmente en `email_log`
- [ ] Los errores de un destinatario NO detienen los demás envíos
- [ ] Los logs en consola son claros y detallados
- [ ] La configuración `ResumenDiario = false` detiene el envío
- [ ] El HTML generado es profesional y responsive

---

## ?? Próximos Pasos

Una vez verificadas las pruebas:

1. **Configurar el Worker automático** para envíos diarios
2. **Ajustar la hora de envío** en `email_config.hora_resumen`
3. **Monitorear `email_log`** regularmente
4. **Crear alertas de BD** para detectar fallos de envío

---

**? PRUEBAS COMPLETADAS Y DOCUMENTADAS**
