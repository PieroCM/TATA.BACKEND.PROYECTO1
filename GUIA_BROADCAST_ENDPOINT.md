# ?? GUÍA COMPLETA: ENDPOINT DE BROADCAST

## ? **IMPLEMENTACIÓN COMPLETADA**

Todos los cambios han sido implementados exitosamente para que el endpoint de broadcast funcione de la mejor manera.

---

## ?? **ENDPOINT DE BROADCAST**

### **URL**
```
POST http://localhost:5260/api/email/broadcast
```

### **Headers**
```
Content-Type: application/json
```

---

## ?? **FORMATO DEL REQUEST**

### **Body (JSON)**
```json
{
  "idSlaFilter": 1,
  "idRolFilter": null,
  "asunto": "Aviso de Vencimiento",
  "mensajeHtml": "<h1>Estimados</h1><p>Favor revisar sus pendientes.</p>"
}
```

### **Parámetros**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `idSlaFilter` | int? | No | Filtrar por tipo de SLA (ej: 1, 2, 3). `null` = todos |
| `idRolFilter` | int? | No | Filtrar por rol (ej: 1, 2, 3). `null` = todos |
| `asunto` | string | Sí | Asunto del correo electrónico |
| `mensajeHtml` | string | Sí | Cuerpo del mensaje en HTML |

---

## ?? **EJEMPLOS DE USO**

### **1. Enviar a TODOS los usuarios activos**
```json
{
  "idSlaFilter": null,
  "idRolFilter": null,
  "asunto": "Comunicado General",
  "mensajeHtml": "<h2>Atención</h2><p>Mensaje para todos.</p>"
}
```

### **2. Enviar solo a SLA específico**
```json
{
  "idSlaFilter": 1,
  "idRolFilter": null,
  "asunto": "Aviso SLA Alta Prioridad",
  "mensajeHtml": "<h2>Importante</h2><p>Solicitudes de alta prioridad.</p>"
}
```

### **3. Enviar solo a Rol específico**
```json
{
  "idSlaFilter": null,
  "idRolFilter": 7,
  "asunto": "Mensaje para Project Managers",
  "mensajeHtml": "<h2>Equipo PMO</h2><p>Mensaje exclusivo para PMs.</p>"
}
```

### **4. Combinar filtros (SLA + Rol)**
```json
{
  "idSlaFilter": 1,
  "idRolFilter": 7,
  "asunto": "Mensaje Específico",
  "mensajeHtml": "<h2>Equipo PMO - Alta Prioridad</h2><p>Solo para PMs con solicitudes de alta.</p>"
}
```

---

## ? **RESPUESTAS DEL SERVIDOR**

### **Success (200 OK)**
```json
{
  "mensaje": "Broadcast enviado exitosamente",
  "fecha": "2025-01-26T07:46:47.777Z",
  "filtros": {
    "idRol": null,
    "idSla": 1
  }
}
```

### **Error - Datos Inválidos (400 Bad Request)**
```json
{
  "mensaje": "El campo 'mensajeHtml' es obligatorio y no puede estar vacío"
}
```

### **Error - Sin Destinatarios (400 Bad Request)**
```json
{
  "mensaje": "No se encontraron destinatarios con los filtros especificados"
}
```

### **Error del Servidor (500 Internal Server Error)**
```json
{
  "mensaje": "Error al enviar broadcast. Por favor, contacte al administrador.",
  "error": "Detalle del error técnico"
}
```

---

## ?? **CÓMO FUNCIONA INTERNAMENTE**

### **Paso 1: Filtrado de Destinatarios**
```sql
SELECT DISTINCT p.correo_corporativo
FROM solicitud s
INNER JOIN personal p ON s.id_personal = p.id_personal
WHERE s.estado_solicitud NOT IN ('CERRADO', 'ELIMINADO')
  AND s.id_sla = @IdSlaFilter              -- Si se especifica
  AND s.id_rol_registro = @IdRolFilter     -- Si se especifica
  AND p.correo_corporativo IS NOT NULL
```

### **Paso 2: Envío de Correos**
- Se envía un correo individual a cada destinatario único
- Si un correo falla, se registra pero continúa con los demás
- Se loguea cada envío exitoso/fallido

### **Paso 3: Registro de Auditoría**
```sql
INSERT INTO email_log (fecha, tipo, destinatarios, estado, error_detalle)
VALUES (GETUTCDATE(), 'BROADCAST', '...', 'OK', 'Exitosos: X, Fallidos: Y')
```

---

## ?? **EJEMPLO DE CORREO ENVIADO**

### **Asunto:**
```
Aviso de Vencimiento
```

### **Cuerpo (HTML renderizado):**
```html
<h1>Estimados</h1>
<p>Favor revisar sus pendientes.</p>
```

---

## ?? **SEGURIDAD Y VALIDACIONES**

### **Validaciones Implementadas:**
? `mensajeHtml` no puede estar vacío  
? `asunto` no puede estar vacío  
? Debe haber al menos un destinatario válido  
? Solo se envía a usuarios con correo corporativo válido  
? Solo se envía a solicitudes activas (no cerradas/eliminadas)  

### **Logging Implementado:**
? Inicio del proceso con filtros aplicados  
? Cantidad de destinatarios encontrados  
? Cada envío exitoso/fallido  
? Resumen final (exitosos vs fallidos)  
? Registro en base de datos (`email_log`)  

---

## ?? **VERIFICAR ENVÍOS**

### **1. Consultar Email Log**
```sql
SELECT 
    id,
    fecha,
    tipo,
    destinatarios,
    estado,
    error_detalle
FROM email_log
WHERE tipo = 'BROADCAST'
ORDER BY fecha DESC
```

### **2. Ver Logs de Consola**
```
[11:46:47 INF] Solicitud de broadcast recibida. IdRol=, IdSla=1, Asunto='Aviso de Vencimiento'
[11:46:47 INF] Se enviarán correos a 3 destinatarios únicos
[11:46:48 INF] Correo enviado exitosamente a 2220144@ue.edu.pe
[11:46:48 INF] Correo enviado exitosamente a usuario2@tata.com
[11:46:48 INF] Correo enviado exitosamente a usuario3@tata.com
[11:46:48 INF] Broadcast completado. Exitosos: 3, Fallidos: 0
```

### **3. Verificar Correos Recibidos**
- Los destinatarios recibirán el correo en sus bandejas de entrada
- Pueden verlo en su cliente de email (Gmail, Outlook, etc.)

---

## ?? **MEJORES PRÁCTICAS PARA HTML**

### **HTML Básico**
```html
<h1>Título Principal</h1>
<p>Párrafo de texto.</p>
<ul>
  <li>Punto 1</li>
  <li>Punto 2</li>
</ul>
```

### **HTML con Estilos**
```html
<div style="background-color:#f5f5f5; padding:20px; border-radius:8px;">
  <h2 style="color:#667eea;">Título con Color</h2>
  <p style="font-size:16px; line-height:1.6;">
    Texto con mejor legibilidad.
  </p>
</div>
```

### **HTML con Botón**
```html
<div style="text-align:center; margin:30px 0;">
  <a href="https://tata.com/portal" 
     style="background-color:#667eea; color:white; padding:12px 24px; 
            text-decoration:none; border-radius:4px; display:inline-block;">
    Ir al Portal
  </a>
</div>
```

---

## ?? **TESTING EN POSTMAN**

### **1. Crear Request**
1. Método: `POST`
2. URL: `http://localhost:5260/api/email/broadcast`
3. Headers: `Content-Type: application/json`
4. Body (raw - JSON):
```json
{
  "idSlaFilter": 1,
  "idRolFilter": null,
  "asunto": "Test desde Postman",
  "mensajeHtml": "<h1>Prueba</h1><p>Este es un correo de prueba.</p>"
}
```

### **2. Guardar en Colección**
- Nombre: "Envío Masivo (Broadcast)"
- Carpeta: "Email Automation"

### **3. Variables de Entorno**
```json
{
  "base_url": "http://localhost:5260",
  "id_sla_test": 1,
  "id_rol_test": 7
}
```

---

## ?? **MÉTRICAS Y ESTADÍSTICAS**

### **Próximamente disponible:**
```
GET http://localhost:5260/api/email/stats
```

Retornará:
- Total de broadcasts enviados
- Tasa de éxito/fallo
- Destinatarios más frecuentes
- Horarios de mayor actividad

---

## ??? **TROUBLESHOOTING**

### **Problema: No se encuentran destinatarios**
**Causa:** No hay solicitudes activas con los filtros especificados

**Solución:**
```sql
-- Verificar solicitudes activas con el SLA
SELECT COUNT(*) 
FROM solicitud s
WHERE s.id_sla = 1 
  AND s.estado_solicitud NOT IN ('CERRADO', 'ELIMINADO')
```

### **Problema: Algunos correos no se envían**
**Causa:** Correos inválidos o problemas SMTP

**Solución:**
1. Verificar configuración SMTP en `appsettings.json`
2. Revisar logs de error en `email_log`
3. Validar formato de correos en tabla `personal`

### **Problema: Error 500**
**Causa:** Error interno del servidor

**Solución:**
1. Revisar logs de consola
2. Verificar conexión a BD
3. Validar configuración SMTP
4. Revisar tabla `email_log` para detalles

---

## ?? **CASOS DE USO REALES**

### **1. Comunicado de Mantenimiento**
```json
{
  "idSlaFilter": null,
  "idRolFilter": null,
  "asunto": "Mantenimiento Programado - Sistema SLA",
  "mensajeHtml": "<h2>Estimado equipo</h2><p>El día de mañana habrá mantenimiento del sistema de 2am a 4am.</p><p>Gracias por su comprensión.</p>"
}
```

### **2. Recordatorio de Solicitudes Pendientes**
```json
{
  "idSlaFilter": 1,
  "idRolFilter": null,
  "asunto": "Recordatorio: Solicitudes de Alta Prioridad Pendientes",
  "mensajeHtml": "<h2>?? Atención</h2><p>Tienes solicitudes de alta prioridad pendientes de revisión.</p><p><a href='https://tata.com/dashboard'>Ir al Dashboard</a></p>"
}
```

### **3. Felicitación por Cumplimiento de SLA**
```json
{
  "idSlaFilter": null,
  "idRolFilter": 7,
  "asunto": "¡Felicitaciones Equipo PMO!",
  "mensajeHtml": "<h2>?? Excelente Trabajo</h2><p>El equipo PMO ha cumplido el 98% de los SLAs este mes.</p><p>¡Sigan así!</p>"
}
```

---

## ?? **SOPORTE**

**Documentación técnica:** Ver `DOCUMENTACION_ENDPOINTS_POSTMAN.md`  
**Logs del servidor:** Consola de la aplicación  
**Auditoría de envíos:** Tabla `email_log` en BD  

**Fecha de implementación:** 26/01/2025  
**Versión:** 1.0.0  
**Estado:** ? **OPERATIVO Y PROBADO**
