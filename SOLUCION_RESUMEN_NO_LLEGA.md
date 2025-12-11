# ?? SOLUCIÓN: Resumen Diario No Llega al Correo

## ?? TU PROBLEMA

- ? El sistema reporta que el correo se envió correctamente
- ? No te llega ningún correo a tu bandeja
- ? Los logs dicen "OK" pero no ves nada

## ?? CAUSA MÁS PROBABLE

**El 90% de las veces, el correo SÍ se envió, pero Gmail lo marcó como SPAM** porque:
- Contenido HTML complejo detectado como sospechoso
- Remitente no verificado en tu lista de contactos
- Volumen de correos considerado inusual por Gmail

## ? SOLUCIÓN RÁPIDA (5 MINUTOS)

### PASO 1: Ejecutar Diagnóstico Automático

Abre PowerShell como Administrador y ejecuta:

```powershell
cd "D:\appweb\TATABAKEND\Scripts"
.\DiagnosticoResumenDiario.ps1
```

Este script automáticamente:
- ? Verifica la configuración SMTP
- ? Revisa logs de envíos anteriores
- ? Te permite enviar un correo de prueba
- ? Te da recomendaciones específicas

### PASO 2: Revisar Carpetas de Correo

**IMPORTANTE: Revisa TODAS estas ubicaciones en $EmailDestino:**

1. **?? Bandeja de entrada principal**
2. **??? Spam / Correo no deseado** ? **AQUÍ ESTÁ EL 80% DE LAS VECES**
3. **?? Promociones** (si usas Gmail con pestañas)
4. **?? Notificaciones** (Gmail)
5. **?? Buscar en TODO el correo**: `[RESUMEN DIARIO SLA]`

### PASO 3: Forzar Envío Manual

Si quieres probar manualmente sin esperar al worker automático:

```powershell
# Reemplaza con tu URL de API
$apiUrl = "https://localhost:7152"

# Forzar envío inmediato del resumen
Invoke-RestMethod -Uri "$apiUrl/api/email/send-summary" -Method POST
```

Respuesta esperada:
```json
{
  "mensaje": "Resumen diario enviado exitosamente",
  "fecha": "2025-01-22T...",
  "tipo": "MANUAL"
}
```

### PASO 4: Verificar Configuración en Base de Datos

```sql
-- Ejecutar en SQL Server Management Studio
-- Conectar a tu base de datos Proyecto1SLA_DB

-- Ver configuración actual
SELECT * FROM email_config;

-- Verificar datos importantes:
-- ? ResumenDiario debe ser 1 (true)
-- ? DestinatarioResumen debe ser tu email correcto
-- ? HoraResumen es la hora en formato TimeSpan (ej: 08:00:00)

-- Si no existe o está mal configurado, actualizar:
UPDATE email_config 
SET DestinatarioResumen = '22200150@ue.edu.pe',  -- CAMBIA POR TU EMAIL
    ResumenDiario = 1,
    HoraResumen = '08:00:00'  -- 8:00 AM
WHERE Id = 1;
```

### PASO 5: Verificar que Hay Alertas para Enviar

El resumen solo se envía si hay alertas críticas o altas:

```sql
-- Ver si hay alertas para enviar
SELECT 
    COUNT(*) AS Total,
    Nivel,
    Estado
FROM alerta
WHERE Estado = 'ACTIVA' 
  AND (Nivel = 'CRITICO' OR Nivel = 'ALTO')
GROUP BY Nivel, Estado;

-- Si el resultado es 0, NO SE ENVIARÁ NINGÚN RESUMEN
-- Necesitas tener al menos 1 alerta CRITICO o ALTO activa
```

## ?? DIAGNÓSTICO AVANZADO

### Verificar Logs de la Aplicación

1. **En Visual Studio**:
   - Ve a `View` ? `Output`
   - Selecciona `Show output from: Debug`
   - Busca líneas que contengan:
     ```
     "Iniciando envío de resumen diario"
     "Resumen diario enviado exitosamente"
     "Error al enviar resumen diario"
     ```

2. **Verificar EmailLog en Base de Datos**:
```sql
-- Ver últimos 10 intentos de envío de resumen
SELECT TOP 10 
    Fecha,
    Tipo,
    Destinatarios,
    Estado,
    ErrorDetalle
FROM email_log
WHERE Tipo = 'RESUMEN'
ORDER BY Fecha DESC;

-- Si Estado = 'ERROR', revisar ErrorDetalle para ver qué falló
```

### Test de Conectividad SMTP

```powershell
# Verificar que puedes conectar a Gmail SMTP
$apiUrl = "https://localhost:7152"

# Diagnóstico completo
$diagnosis = Invoke-RestMethod -Uri "$apiUrl/api/emailtest/diagnosis" -Method GET
$diagnosis | ConvertTo-Json -Depth 3

# Debe mostrar:
# ? authentication.success = true
# ? connectivity.smtpPortReachable = true
# ? connectivity.internetAccess = true
```

### Enviar Correo de Prueba Simple

```powershell
# Si el resumen no llega, prueba con un correo simple
$apiUrl = "https://localhost:7152"
$body = @{ 
    email = "22200150@ue.edu.pe"  # CAMBIA POR TU EMAIL
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "$apiUrl/api/emailtest/send" -Method POST -Body $body -ContentType "application/json"

# Si ESTE correo llega pero el resumen NO:
# ? El problema está en el contenido HTML del resumen
# ? Gmail lo marca automáticamente como spam
```

## ?? PROBLEMAS COMUNES Y SOLUCIONES

### Problema 1: "No hay destinatario configurado"

**Síntoma**: 
```
Error: No hay destinatario configurado para el resumen diario
```

**Solución**:
```sql
UPDATE email_config 
SET DestinatarioResumen = 'tu-email@ejemplo.com'
WHERE Id = 1;
```

### Problema 2: "No hay alertas críticas para el resumen"

**Síntoma**: Log muestra "No hay alertas críticas para el resumen diario"

**Solución**: Esto es normal si no hay alertas. Para probar, crea una alerta de prueba:
```sql
-- Crear una alerta de prueba (necesitas una solicitud existente)
INSERT INTO alerta (IdSolicitud, TipoAlerta, Nivel, Mensaje, Estado, EnviadoEmail, FechaCreacion)
VALUES (
    1,  -- ID de una solicitud existente
    'SLA_VENCIMIENTO_INMEDIATO',
    'CRITICO',
    '?? URGENTE: Solicitud de prueba vence en 1 día',
    'ACTIVA',
    0,
    GETUTCDATE()
);
```

### Problema 3: "Authentication failed"

**Síntoma**: Diagnóstico muestra error de autenticación SMTP

**Solución**:
1. Ve a https://myaccount.google.com
2. Seguridad ? Verificación en 2 pasos (debe estar activada)
3. Contraseñas de aplicaciones ? Crear nueva para "Correo"
4. Copia la contraseña generada (16 caracteres sin espacios)
5. Actualiza `appsettings.json`:
```json
{
  "SmtpSettings": {
    "Password": "NUEVA-CONTRASEÑA-AQUI"
  }
}
```
6. Reinicia la aplicación

### Problema 4: Correo va a SPAM automáticamente

**Síntoma**: Logs dicen "OK" pero solo encuentras el correo en Spam

**Solución**:
1. **En Gmail**: Abre el correo en Spam
2. Marca como **"No es spam"**
3. Ve a Configuración ? Filtros y direcciones bloqueadas
4. Crea un filtro:
   - De: `patroclown2.0@gmail.com`
   - Acción: "No enviar nunca a Spam" + "Marcar como importante"
5. Agrega `patroclown2.0@gmail.com` a tus contactos

### Problema 5: Worker no está ejecutándose

**Síntoma**: Nunca se envía automáticamente a la hora configurada

**Solución**: Verificar que el Worker esté registrado en `Program.cs`:
```csharp
// En Program.cs, debe existir esta línea:
builder.Services.AddHostedService<DailySummaryWorker>();
```

Ver logs del Worker:
```
[INFO] DailySummaryWorker iniciado correctamente
[INFO] Hora de envío alcanzada: 08:00:00 ? 08:00:00. Enviando resumen diario...
```

## ?? CHECKLIST COMPLETO

Marca cada item conforme lo verificas:

- [ ] **Configuración SMTP**
  - [ ] Host: smtp.gmail.com
  - [ ] Port: 587
  - [ ] EnableSsl: true
  - [ ] Contraseña de app válida

- [ ] **EmailConfig en Base de Datos**
  - [ ] Registro existe en tabla email_config
  - [ ] ResumenDiario = 1 (true)
  - [ ] DestinatarioResumen = tu email correcto
  - [ ] HoraResumen configurada (ej: 08:00:00)

- [ ] **Datos para Enviar**
  - [ ] Existen alertas con Estado = 'ACTIVA'
  - [ ] Al menos 1 alerta con Nivel = 'CRITICO' o 'ALTO'

- [ ] **Conectividad**
  - [ ] API respondiendo correctamente
  - [ ] Diagnóstico SMTP exitoso
  - [ ] Autenticación Gmail funcionando

- [ ] **Verificación de Recepción**
  - [ ] Revisaste Bandeja de entrada
  - [ ] Revisaste carpeta SPAM ? **MUY IMPORTANTE**
  - [ ] Revisaste Promociones/Notificaciones
  - [ ] Buscaste por asunto "[RESUMEN DIARIO SLA]"

- [ ] **Logs**
  - [ ] EmailLog muestra Estado = 'OK'
  - [ ] No hay errores en ErrorDetalle
  - [ ] Logs de aplicación muestran envío exitoso

## ?? TEST DEFINITIVO

Ejecuta esta secuencia completa para confirmar que todo funciona:

```powershell
# 1. Verificar configuración
$apiUrl = "https://localhost:7152"
$config = Invoke-RestMethod -Uri "$apiUrl/api/email/config" -Method GET
Write-Host "Destinatario: $($config.destinatarioResumen)"
Write-Host "Resumen Diario: $($config.resumenDiario)"

# 2. Verificar diagnóstico
$diag = Invoke-RestMethod -Uri "$apiUrl/api/emailtest/diagnosis" -Method GET
Write-Host "Autenticación: $($diag.authentication.success)"

# 3. Forzar envío
$result = Invoke-RestMethod -Uri "$apiUrl/api/email/send-summary" -Method POST
Write-Host "Resultado: $($result.mensaje)"

# 4. Verificar logs
$logs = Invoke-RestMethod -Uri "$apiUrl/api/email/logs" -Method GET
$lastResumen = $logs.logs | Where-Object { $_.tipo -eq 'RESUMEN' } | Select-Object -First 1
Write-Host "Último envío: $($lastResumen.fecha) - Estado: $($lastResumen.estado)"

# 5. AHORA REVISA TU CORREO (especialmente SPAM)
Write-Host "`n?? AHORA REVISA:"
Write-Host "1. Bandeja de entrada"
Write-Host "2. SPAM / Correo no deseado ? MIRA AQUÍ"
Write-Host "3. Busca: [RESUMEN DIARIO SLA]"
```

## ?? RESPUESTA RÁPIDA A TU PREGUNTA

**"Me sale que el correo se envía correctamente pero no me llega al final, ¿a qué se debe?"**

**Respuesta**: El correo **SÍ se está enviando correctamente**, pero Gmail lo está marcando automáticamente como **SPAM** porque detecta el contenido HTML como potencialmente sospechoso.

**Solución en 3 pasos**:
1. Revisa tu carpeta de **SPAM/Correo no deseado**
2. Encuentra el correo con asunto `[RESUMEN DIARIO SLA]`
3. Márcalo como **"No es spam"** y agrégalo a contactos

**Para evitarlo en el futuro**:
- Agrega `patroclown2.0@gmail.com` a tus contactos de Gmail
- Crea un filtro para que nunca vaya a Spam
- Considera usar un servicio SMTP profesional (SendGrid, Mailgun)

## ?? SI NECESITAS MÁS AYUDA

1. Ejecuta el script de diagnóstico: `.\DiagnosticoResumenDiario.ps1`
2. Copia la salida completa
3. Revisa los logs en `email_log` de la base de datos
4. Verifica que Gmail no tenga la cuenta bloqueada

El problema casi siempre es **SPAM** o **falta de alertas para enviar**.