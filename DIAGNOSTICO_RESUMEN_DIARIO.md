# ?? Script de Diagnóstico - Resumen Diario No Llega

## PROBLEMA IDENTIFICADO

El sistema reporta que el correo se envía correctamente pero no llega al destinatario. Este es un problema común con varias causas posibles.

## ? VERIFICACIONES PASO A PASO

### PASO 1: Verificar Configuración en Base de Datos
```sql
-- Ejecutar en SQL Server Management Studio
SELECT * FROM email_config;

-- Verificar que exista un registro con:
-- ResumenDiario = 1 (true)
-- DestinatarioResumen = tu email correcto
```

### PASO 2: Verificar Logs de Email
```sql
-- Ver últimos intentos de envío de resumen
SELECT TOP 10 * 
FROM email_log 
WHERE Tipo = 'RESUMEN'
ORDER BY Fecha DESC;

-- Buscar errores específicos
SELECT * 
FROM email_log 
WHERE Tipo = 'RESUMEN' AND Estado = 'ERROR'
ORDER BY Fecha DESC;
```

### PASO 3: Probar Endpoint Manual
```powershell
# Ejecutar desde PowerShell
$apiUrl = "https://localhost:7152/api/email/send-summary"

$response = Invoke-WebRequest -Uri $apiUrl -Method POST -UseBasicParsing
$response.Content | ConvertFrom-Json | Format-List
```

### PASO 4: Verificar con EmailTest
```powershell
# Test 1: Diagnóstico completo
Invoke-RestMethod -Uri "https://localhost:7152/api/emailtest/diagnosis" -Method GET

# Test 2: Enviar correo de prueba al mismo destinatario del resumen
$body = @{ email = "22200150@ue.edu.pe" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7152/api/emailtest/send" -Method POST -Body $body -ContentType "application/json"
```

## ?? CAUSAS COMUNES Y SOLUCIONES

### CAUSA 1: El Correo Va a Carpeta de SPAM
**Síntoma**: El sistema reporta éxito pero no ves el correo
**Solución**:
1. Revisar carpeta de Spam/Correo no deseado
2. Agregar remitente a lista de contactos seguros
3. Marcar correo como "No es Spam"

### CAUSA 2: Filtros de Gmail Bloqueando
**Síntoma**: El log dice "OK" pero Gmail silenciosamente descarta
**Solución**:
```json
// En appsettings.json, verificar que From coincida con User
{
  "SmtpSettings": {
    "From": "patroclown2.0@gmail.com",
    "User": "patroclown2.0@gmail.com"
  }
}
```

### CAUSA 3: Destinatario Incorrecto en Base de Datos
**Síntoma**: Se envía pero a la dirección equivocada
**Solución**:
```sql
-- Actualizar destinatario correcto
UPDATE email_config 
SET DestinatarioResumen = 'tu-email-correcto@ejemplo.com'
WHERE Id = 1;
```

### CAUSA 4: No Hay Alertas Críticas
**Síntoma**: El método retorna antes de enviar
**Solución**:
```sql
-- Verificar si hay alertas para enviar
SELECT COUNT(*) AS AlertasCriticas
FROM alerta
WHERE Estado = 'ACTIVA' 
  AND (Nivel = 'CRITICO' OR Nivel = 'ALTO');

-- Si el resultado es 0, no se enviará nada
```

### CAUSA 5: Problema con el Servicio SMTP
**Síntoma**: Exception capturada pero no propagada
**Solución**: Ver logs detallados del sistema

## ??? SOLUCIÓN RÁPIDA

### Opción A: Actualizar Base de Datos
```sql
-- 1. Verificar configuración actual
SELECT * FROM email_config;

-- 2. Si no existe, insertar
INSERT INTO email_config (DestinatarioResumen, EnvioInmediato, ResumenDiario, HoraResumen, CreadoEn)
VALUES ('22200150@ue.edu.pe', 1, 1, '08:00:00', GETUTCDATE());

-- 3. Si existe, actualizar
UPDATE email_config 
SET DestinatarioResumen = '22200150@ue.edu.pe',
    ResumenDiario = 1
WHERE Id = 1;
```

### Opción B: Forzar Envío Manual para Diagnóstico
```powershell
# Script completo de diagnóstico
$baseUrl = "https://localhost:7152/api"

Write-Host "=== DIAGNÓSTICO RESUMEN DIARIO ===" -ForegroundColor Cyan

# 1. Verificar conectividad SMTP
Write-Host "`n1. Verificando conectividad SMTP..." -ForegroundColor Yellow
try {
    $diagnosis = Invoke-RestMethod -Uri "$baseUrl/emailtest/diagnosis" -Method GET
    Write-Host "   ? Configuración SMTP:" -ForegroundColor Green
    Write-Host "      Host: $($diagnosis.smtpConfig.host):$($diagnosis.smtpConfig.port)"
    Write-Host "      Usuario: $($diagnosis.smtpConfig.user)"
    Write-Host "      Autenticación: $($diagnosis.authentication.success)"
} catch {
    Write-Host "   ? Error: $_" -ForegroundColor Red
}

# 2. Intentar enviar resumen
Write-Host "`n2. Enviando resumen diario..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/email/send-summary" -Method POST
    Write-Host "   ? Respuesta: $($result.mensaje)" -ForegroundColor Green
    Write-Host "   ?? Fecha: $($result.fecha)"
} catch {
    Write-Host "   ? Error: $_" -ForegroundColor Red
    Write-Host "   Detalle: $($_.Exception.Response.StatusCode)"
}

# 3. Verificar logs
Write-Host "`n3. Verificando logs de email..." -ForegroundColor Yellow
try {
    $logs = Invoke-RestMethod -Uri "$baseUrl/email/logs" -Method GET
    $resumenLogs = $logs | Where-Object { $_.tipo -eq 'RESUMEN' } | Select-Object -First 5
    
    if ($resumenLogs) {
        Write-Host "   Últimos envíos de resumen:" -ForegroundColor Cyan
        foreach ($log in $resumenLogs) {
            $emoji = if ($log.estado -eq 'OK') { '?' } else { '?' }
            Write-Host "   $emoji $($log.fecha) - Estado: $($log.estado) - Destinatario: $($log.destinatarios)"
            if ($log.errorDetalle) {
                Write-Host "      Error: $($log.errorDetalle)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ?? No hay logs de resumen diario" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ? Error al obtener logs: $_" -ForegroundColor Red
}

Write-Host "`n=== FIN DEL DIAGNÓSTICO ===" -ForegroundColor Cyan
```

## ?? DEBUGGING AVANZADO

### Ver Logs de la Aplicación
```powershell
# Si usas Visual Studio, ver Output Window
# Buscar líneas que contengan:
# - "Iniciando envío de resumen diario"
# - "Resumen diario enviado exitosamente"
# - "Error al enviar resumen diario"
```

### Verificar con Telnet (Opcional)
```powershell
# Verificar conectividad directa a Gmail SMTP
Test-NetConnection -ComputerName smtp.gmail.com -Port 587
```

## ? SOLUCIÓN INMEDIATA

**Si necesitas que funcione YA:**

1. **Verificar que el correo existe en EmailConfig**:
```sql
SELECT DestinatarioResumen FROM email_config WHERE Id = 1;
```

2. **Forzar envío manual**:
```powershell
Invoke-RestMethod -Uri "https://localhost:7152/api/email/send-summary" -Method POST
```

3. **Revisar TODAS estas ubicaciones**:
   - ?? Bandeja de entrada
   - ??? Spam / Correo no deseado
   - ?? Promociones (Gmail)
   - ?? Buscar por asunto: "[RESUMEN DIARIO SLA]"

4. **Si aún no llega, enviar correo de prueba**:
```powershell
$body = @{ email = "22200150@ue.edu.pe" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7152/api/emailtest/send" -Method POST -Body $body -ContentType "application/json"
```

## ?? CHECKLIST FINAL

- [ ] EmailConfig.ResumenDiario = true
- [ ] EmailConfig.DestinatarioResumen tiene tu email correcto
- [ ] Hay alertas CRITICO o ALTO en la base de datos
- [ ] SmtpSettings configurado correctamente
- [ ] Contraseña de app de Gmail válida
- [ ] Revisaste carpeta de Spam
- [ ] Logs muestran estado "OK"
- [ ] EmailTest funciona correctamente

## ?? SI TODO FALLA

El problema más probable es que **el correo está yendo a SPAM** porque:
- Gmail detecta el contenido HTML como sospechoso
- El remitente no está verificado
- Volumen de correos considerado inusual

**Solución definitiva**: 
- Agregar `patroclown2.0@gmail.com` a contactos seguros
- Configurar SPF/DKIM (requiere dominio propio)
- Usar servicio profesional como SendGrid/Mailgun