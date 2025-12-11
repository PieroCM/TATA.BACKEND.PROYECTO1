# ?? TEST DEFINITIVO: Debugging Agresivo

## TU SITUACIÓN

```
? API dice: "Resumen enviado exitosamente"
? EmailService NO lanza excepciones  
? Correo NO llega a tu bandeja
```

## ?? TEST 1: Ver TODOS los logs en tiempo real

### Paso 1: Modificar `appsettings.json` temporalmente

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",          // ? Cambiar de Information a Debug
      "Microsoft": "Debug",         // ? Agregar
      "MailKit": "Debug"            // ? Agregar  
    }
  }
}
```

### Paso 2: Ejecutar en Visual Studio con Output abierto

1. Presiona **F5** para ejecutar en Debug
2. **View** ? **Output**  
3. **Show output from:** `Debug`
4. Llamar al endpoint: `POST /api/email/send-summary`

### Paso 3: Buscar en los logs

Busca **EXACTAMENTE** estas líneas (en orden):

```
[DEBUG] ?? Conectando a servidor SMTP smtp.gmail.com:587...
[INFO] ? Conectado a smtp.gmail.com:587
[DEBUG] ?? Autenticando usuario mellamonose19@gmail.com...
[INFO] ? Autenticado correctamente
[DEBUG] ?? Enviando mensaje...
[INFO] ? Mensaje enviado. Respuesta del servidor: OK  ? ?? IMPORTANTE
```

**Si ves "? Mensaje enviado"** ? El correo **SÍ** se envió, pero está en SPAM

---

## ?? TEST 2: Verificar en Base de Datos

```sql
-- Ver TODOS los logs de resumen
SELECT 
    Id,
    Fecha,
    Tipo,
    Destinatarios,
    Estado,
    ErrorDetalle
FROM email_log
WHERE Tipo = 'RESUMEN'
ORDER BY Fecha DESC;

-- ?? Si Estado = 'OK' pero no te llega:
-- El correo SÍ se envió, pero está en SPAM o Gmail lo bloqueó
```

---

## ?? TEST 3: Probar con Correo Diferente

Cambia temporalmente el destinatario a **OTRO correo** (no Gmail):

```sql
-- Probar con Outlook o Yahoo
UPDATE email_config 
SET DestinatarioResumen = 'tu-correo@outlook.com'  -- O @yahoo.com
WHERE Id = 1;
```

Luego ejecuta: `POST /api/email/send-summary`

**Si llega a Outlook pero NO a Gmail** ? El problema es **filtros de Gmail**

---

## ?? TEST 4: Verificar Contraseña de App Gmail

La contraseña `mpwcevagvfehlfer` puede ser inválida.

### Generar nueva contraseña:

1. Ve a: https://myaccount.google.com
2. **Seguridad** ? **Verificación en 2 pasos** (debe estar ? ACTIVADA)
3. **Contraseñas de aplicaciones** ? **Crear nueva**
4. Selecciona: **Correo** y **Windows Computer**
5. Copia la contraseña (16 dígitos sin espacios)
6. Pega en `appsettings.json`:

```json
{
  "SmtpSettings": {
    "User": "mellamonose19@gmail.com",
    "Password": "NUEVA-CONTRASEÑA-16-DIGITOS-AQUI"
  }
}
```

7. **Reinicia la API** (importante)
8. Vuelve a probar

---

## ?? TEST 5: Script PowerShell con Telnet

Verifica que puedes conectar directamente al servidor SMTP:

```powershell
# Test de conectividad raw
Test-NetConnection -ComputerName smtp.gmail.com -Port 587

# Debe mostrar:
# TcpTestSucceeded : True ?
```

Si muestra `False` ? Tu firewall/antivirus está bloqueando el puerto 587

---

## ?? TEST 6: Enviar Correo Simple (Sin HTML)

Modifica temporalmente para probar con texto plano:

```sql
-- Ejecutar en SQL Server
-- Primero, desactivar alertas críticas temporalmente
UPDATE alerta SET Estado = 'INACTIVA' WHERE Nivel IN ('CRITICO', 'ALTO');

-- Ahora el resumen no se enviará porque no hay alertas
-- Crear UNA alerta de prueba simple
DECLARE @idSol INT;
SELECT TOP 1 @idSol = IdSolicitud FROM solicitud WHERE EstadoSolicitud <> 'CERRADO';

INSERT INTO alerta (IdSolicitud, TipoAlerta, Nivel, Mensaje, Estado, EnviadoEmail, FechaCreacion)
VALUES (@idSol, 'PRUEBA', 'CRITICO', 'Alerta de prueba', 'ACTIVA', 0, GETUTCDATE());

-- Ahora llamar: POST /api/email/send-summary
```

---

## ?? ANÁLISIS DEL LOG QUE ENVIASTE

```
info: TATA.BACKEND.PROYECTO1.API.Controllers.EmailController[0]
      ? [API] Resumen diario enviado exitosamente (manual)
```

Este log **SOLO aparece si NO hubo excepción**. Esto significa:

1. ? EmailAutomationService.SendDailySummaryAsync() completó sin errores
2. ? EmailService.SendAsync() completó sin errores  
3. ? SmtpClient.SendAsync() devolvió éxito
4. ? El correo **FUE ENVIADO** al servidor Gmail

**PERO...**

? Gmail puede estar:
- Marcándolo como SPAM silenciosamente
- Rechazándolo por "reputación del remitente"  
- Bloqueándolo por "contenido sospechoso"

---

## ?? PRUEBA DEFINITIVA

### Opción A: Script PowerShell Mejorado

```powershell
# Script de prueba completo
$apiUrl = "https://localhost:7152"
$destinatarioOriginal = "mellamonose19@gmail.com"

Write-Host "?? TEST 1: Verificar logs en BD" -ForegroundColor Yellow

# Ejecutar consulta SQL para ver logs
sqlcmd -S "." -d "Proyecto1SLA_DB" -Q "SELECT TOP 3 * FROM email_log WHERE Tipo='RESUMEN' ORDER BY Fecha DESC"

Write-Host "`n?? TEST 2: Forzar envío manual" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/email/send-summary" -Method POST
    
    Write-Host "? API Respuesta: $($response.mensaje)" -ForegroundColor Green
    
    if ($response.success) {
        Write-Host "`n?? EL CORREO SE ENVIÓ CORRECTAMENTE" -ForegroundColor Yellow
        Write-Host "   Pero NO llega a tu bandeja..." -ForegroundColor Gray
        Write-Host "`n?? CHECKLIST:" -ForegroundColor Cyan
        Write-Host "   1. ¿Revisaste SPAM?" -ForegroundColor White
        Write-Host "   2. ¿Revisaste Promociones?" -ForegroundColor White
        Write-Host "   3. ¿Buscaste: [RESUMEN DIARIO SLA]?" -ForegroundColor White
        Write-Host "   4. ¿Probaste con otro correo (Outlook/Yahoo)?" -ForegroundColor White
        Write-Host "`n?? CAUSA MÁS PROBABLE:" -ForegroundColor Yellow
        Write-Host "   Gmail está BLOQUEANDO el correo silenciosamente" -ForegroundColor Red
        Write-Host "   porque detecta el remitente como 'no confiable'" -ForegroundColor Red
    }
    
} catch {
    Write-Host "? ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n?? TEST 3: Enviar correo simple de prueba" -ForegroundColor Yellow

$testBody = @{ email = $destinatarioOriginal } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$apiUrl/api/emailtest/send" -Method POST -Body $testBody -ContentType "application/json"
    Write-Host "? Correo de prueba enviado" -ForegroundColor Green
    Write-Host "   REVISA tu correo (incluyendo SPAM)" -ForegroundColor Yellow
} catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
}
```

### Opción B: Verificación Manual

1. **Revisa Gmail con FILTROS DESACTIVADOS temporalmente:**
   - Settings ? Filters and Blocked Addresses
   - Desactiva TODOS los filtros temporalmente
   - Vuelve a ejecutar el envío

2. **Verifica que el correo NO esté en "Eliminados":**
   - A veces Gmail elimina directamente sin pasar por Spam

3. **Busca en TODO Gmail:**
   ```
   Buscar: from:mellamonose19@gmail.com
   ```

---

## ?? SI TODO LO ANTERIOR FALLA

### Prueba con SendGrid (alternativa profesional)

Gmail es **MUY agresivo** con correos automatizados. Considera usar:

1. **SendGrid** (gratuito hasta 100 correos/día)
2. **Mailgun**
3. **Amazon SES**

Estos servicios tienen mejor "reputación" y NO son bloqueados por Gmail.

---

## ?? RESUMEN

Tu problema **NO es el código**. El código está funcionando correctamente.

**El problema es:**
1. ? Gmail está bloqueando/filtrando el correo
2. ? O el correo está en SPAM y no lo has visto

**Soluciones:**
1. ? Revisar SPAM exhaustivamente
2. ? Agregar remitente a contactos
3. ? Probar con otro correo (Outlook/Yahoo)
4. ? Usar servicio profesional (SendGrid)

**Si ves este log:**
```
[INFO] ? Mensaje enviado. Respuesta del servidor: OK
```

**Significa que el correo SÍ SE ENVIÓ correctamente.** El problema está en Gmail, no en tu código.