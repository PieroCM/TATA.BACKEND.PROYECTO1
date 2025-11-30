# ==========================================
# ?? TEST EXHAUSTIVO: ¿Dónde Está Mi Correo?
# ==========================================

param(
    [string]$ApiUrl = "https://localhost:7152",
    [string]$Email = "mellamonose19@gmail.com",
    [string]$SqlServer = ".",
    [string]$Database = "Proyecto1SLA_DB"
)

$ErrorActionPreference = "Continue"
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Host @"
????????????????????????????????????????????????????????????
?  ?? TEST EXHAUSTIVO: ¿Dónde Está Mi Correo?            ?
?  El código funciona, pero el correo no llega...        ?
????????????????????????????????????????????????????????????
"@ -ForegroundColor Red

Write-Host "`nConfiguración:" -ForegroundColor Cyan
Write-Host "  API: $ApiUrl" -ForegroundColor Gray
Write-Host "  Email: $Email" -ForegroundColor Gray
Write-Host "  SQL Server: $SqlServer" -ForegroundColor Gray
Write-Host ""

# ==========================================
# TEST 1: Verificar Logs en Base de Datos
# ==========================================
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?? TEST 1: Verificando logs en Base de Datos" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow

try {
    $query = @"
SELECT TOP 5 
    CONVERT(VARCHAR(19), Fecha, 120) AS Fecha,
    Tipo,
    Destinatarios,
    Estado,
    CASE 
        WHEN ErrorDetalle IS NULL THEN 'Sin error'
        WHEN LEN(ErrorDetalle) > 100 THEN LEFT(ErrorDetalle, 97) + '...'
        ELSE ErrorDetalle
    END AS ErrorDetalle
FROM email_log
WHERE Tipo = 'RESUMEN'
ORDER BY Fecha DESC
"@

    Write-Host "`nÚltimos 5 intentos de envío de RESUMEN:" -ForegroundColor Cyan
    $logs = Invoke-Sqlcmd -ServerInstance $SqlServer -Database $Database -Query $query -ErrorAction SilentlyContinue
    
    if ($logs) {
        $logs | Format-Table -AutoSize
        
        $ultimoLog = $logs[0]
        
        if ($ultimoLog.Estado -eq "OK") {
            Write-Host "? ÚLTIMO LOG: Estado = OK" -ForegroundColor Green
            Write-Host "   Fecha: $($ultimoLog.Fecha)" -ForegroundColor Gray
            Write-Host "   Destinatario: $($ultimoLog.Destinatarios)" -ForegroundColor Gray
            Write-Host "`n?? CONCLUSIÓN:" -ForegroundColor Yellow
            Write-Host "   El correo SÍ SE ENVIÓ correctamente desde el servidor" -ForegroundColor Yellow
            Write-Host "   Pero NO llegó a tu bandeja..." -ForegroundColor Red
        } else {
            Write-Host "? ÚLTIMO LOG: Estado = ERROR" -ForegroundColor Red
            Write-Host "   Error: $($ultimoLog.ErrorDetalle)" -ForegroundColor Red
        }
    } else {
        Write-Host "?? No hay logs de resumen en la base de datos" -ForegroundColor Yellow
        Write-Host "   Esto significa que nunca se ha ejecutado el envío" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "? No se pudo conectar a SQL Server" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
}

# ==========================================
# TEST 2: Forzar Envío Manual
# ==========================================
Write-Host "`n????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?? TEST 2: Forzando envío manual" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow

try {
    Write-Host "`nLlamando a: POST $ApiUrl/api/email/send-summary" -ForegroundColor Gray
    
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/email/send-summary" -Method POST -TimeoutSec 30
    
    if ($response.success) {
        Write-Host "`n??? API RESPUESTA: ÉXITO" -ForegroundColor Green
        Write-Host "   Mensaje: $($response.mensaje)" -ForegroundColor White
        Write-Host "   Fecha: $($response.fecha)" -ForegroundColor Gray
        Write-Host "   Tipo: $($response.tipo)" -ForegroundColor Gray
        
        Write-Host "`n??????????????????????????????????????????????????????" -ForegroundColor Green
        Write-Host "?  ? EL CORREO SE ENVIÓ CORRECTAMENTE              ?" -ForegroundColor Green
        Write-Host "??????????????????????????????????????????????????????" -ForegroundColor Green
        
        Write-Host "`n?? PERO... ¿Dónde está?" -ForegroundColor Yellow
        Write-Host "`n?? CHECKLIST DE VERIFICACIÓN:" -ForegroundColor Cyan
        Write-Host "   ? 1. Revisar carpeta SPAM / Correo no deseado" -ForegroundColor White
        Write-Host "   ? 2. Revisar carpeta Promociones (Gmail)" -ForegroundColor White
        Write-Host "   ? 3. Revisar carpeta Notificaciones (Gmail)" -ForegroundColor White
        Write-Host "   ? 4. Buscar en TODO Gmail: from:mellamonose19@gmail.com" -ForegroundColor White
        Write-Host "   ? 5. Buscar: [RESUMEN DIARIO SLA]" -ForegroundColor White
        Write-Host "   ? 6. Revisar carpeta Eliminados" -ForegroundColor White
        
        Write-Host "`n?? CAUSA MÁS PROBABLE:" -ForegroundColor Yellow
        Write-Host "   Gmail está BLOQUEANDO el correo silenciosamente" -ForegroundColor Red
        Write-Host "   Razones:" -ForegroundColor Yellow
        Write-Host "   - Remitente no verificado (mellamonose19@gmail.com)" -ForegroundColor Gray
        Write-Host "   - Contenido HTML detectado como sospechoso" -ForegroundColor Gray
        Write-Host "   - Falta SPF/DKIM/DMARC en el dominio" -ForegroundColor Gray
        
    } else {
        Write-Host "`n? API RESPUESTA: ERROR" -ForegroundColor Red
        Write-Host "   Mensaje: $($response.mensaje)" -ForegroundColor White
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "`n? ERROR HTTP $statusCode" -ForegroundColor Red
    
    try {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorResponse = $reader.ReadToEnd() | ConvertFrom-Json
        $reader.Close()
        
        Write-Host "   Error: $($errorResponse.error)" -ForegroundColor White
        
        if ($errorResponse.detalleCompleto) {
            Write-Host "`n   Detalle:" -ForegroundColor Gray
            $detalle = $errorResponse.detalleCompleto
            if ($detalle.Length -gt 300) {
                $detalle = $detalle.Substring(0, 300) + "..."
            }
            Write-Host "   $detalle" -ForegroundColor DarkGray
        }
    } catch {
        Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
    }
}

# ==========================================
# TEST 3: Enviar Correo de Prueba Simple
# ==========================================
Write-Host "`n????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?? TEST 3: Enviando correo de prueba simple" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow

try {
    Write-Host "`nEnviando a: $Email" -ForegroundColor Gray
    
    $testBody = @{ email = $Email } | ConvertTo-Json
    $testResponse = Invoke-RestMethod -Uri "$ApiUrl/api/emailtest/send" -Method POST -Body $testBody -ContentType "application/json" -TimeoutSec 30
    
    if ($testResponse.success) {
        Write-Host "`n? Correo de prueba enviado" -ForegroundColor Green
        Write-Host "   Este es un correo MÁS SIMPLE" -ForegroundColor Gray
        Write-Host "   Debería llegar más fácilmente que el resumen" -ForegroundColor Gray
        Write-Host "`n?? Espera 1-2 minutos y revisa:" -ForegroundColor Yellow
        Write-Host "   1. Bandeja de entrada" -ForegroundColor White
        Write-Host "   2. SPAM" -ForegroundColor White
        Write-Host "   3. Busca: 'Correo de Prueba - Sistema TATA'" -ForegroundColor White
        
        Write-Host "`n?? ANÁLISIS:" -ForegroundColor Cyan
        Write-Host "   - Si este correo LLEGA ? El problema es el HTML del resumen" -ForegroundColor Gray
        Write-Host "   - Si tampoco llega ? Gmail está bloqueando TODOS tus correos" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "? Error al enviar correo de prueba" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
}

# ==========================================
# TEST 4: Verificar Configuración SMTP
# ==========================================
Write-Host "`n????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?? TEST 4: Diagnóstico de configuración SMTP" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow

try {
    $diagResponse = Invoke-RestMethod -Uri "$ApiUrl/api/emailtest/diagnosis" -Method GET -TimeoutSec 30
    
    Write-Host "`n?? Configuración SMTP:" -ForegroundColor Cyan
    Write-Host "   Host: $($diagResponse.smtpConfig.host)" -ForegroundColor White
    Write-Host "   Port: $($diagResponse.smtpConfig.port)" -ForegroundColor White
    Write-Host "   SSL: $($diagResponse.smtpConfig.enableSsl)" -ForegroundColor White
    Write-Host "   User: $($diagResponse.smtpConfig.user)" -ForegroundColor White
    Write-Host "   Password configurado: $($diagResponse.smtpConfig.passwordConfigured)" -ForegroundColor White
    
    Write-Host "`n?? Conectividad:" -ForegroundColor Cyan
    $checks = @{
        "Internet" = $diagResponse.connectivity.internetAccess
        "Puerto SMTP" = $diagResponse.connectivity.smtpPortReachable
        "DNS" = $diagResponse.connectivity.dnsResolution
        "Autenticación" = $diagResponse.authentication.success
    }
    
    foreach ($check in $checks.GetEnumerator()) {
        $emoji = if ($check.Value) { "?" } else { "?" }
        $color = if ($check.Value) { "Green" } else { "Red" }
        Write-Host "   $emoji $($check.Key): $($check.Value)" -ForegroundColor $color
    }
    
    if (-not $diagResponse.authentication.success) {
        Write-Host "`n?? PROBLEMA DETECTADO: Autenticación SMTP" -ForegroundColor Red
        Write-Host "   $($diagResponse.authentication.message)" -ForegroundColor Yellow
        Write-Host "   Error: $($diagResponse.authentication.error)" -ForegroundColor Gray
        Write-Host "`n?? SOLUCIÓN:" -ForegroundColor Yellow
        Write-Host "   1. Ve a: https://myaccount.google.com" -ForegroundColor White
        Write-Host "   2. Seguridad ? Verificación en 2 pasos" -ForegroundColor White
        Write-Host "   3. Contraseñas de aplicaciones ? Crear nueva" -ForegroundColor White
        Write-Host "   4. Actualiza Password en appsettings.json" -ForegroundColor White
    }
    
} catch {
    Write-Host "? Error al obtener diagnóstico" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
}

# ==========================================
# TEST 5: Conectividad Directa (Telnet)
# ==========================================
Write-Host "`n????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?? TEST 5: Conectividad directa al servidor Gmail" -ForegroundColor Yellow
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow

try {
    $tcpTest = Test-NetConnection -ComputerName smtp.gmail.com -Port 587 -InformationLevel Quiet
    
    if ($tcpTest) {
        Write-Host "`n? Puerto 587 ABIERTO" -ForegroundColor Green
        Write-Host "   Puedes conectar a smtp.gmail.com:587" -ForegroundColor Gray
    } else {
        Write-Host "`n? Puerto 587 BLOQUEADO" -ForegroundColor Red
        Write-Host "   No puedes conectar a smtp.gmail.com:587" -ForegroundColor Gray
        Write-Host "`n?? POSIBLES CAUSAS:" -ForegroundColor Yellow
        Write-Host "   - Firewall de Windows bloqueando" -ForegroundColor White
        Write-Host "   - Antivirus bloqueando" -ForegroundColor White
        Write-Host "   - Proxy corporativo" -ForegroundColor White
    }
} catch {
    Write-Host "? Error al probar conectividad" -ForegroundColor Red
}

# ==========================================
# CONCLUSIONES FINALES
# ==========================================
Write-Host "`n????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  ?? CONCLUSIONES Y RECOMENDACIONES                      ?" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Write-Host "`n?? ANÁLISIS:" -ForegroundColor Yellow
Write-Host @"

Si viste:
  ? API devuelve success: true
  ? Log en BD con Estado = 'OK'
  ? Autenticación SMTP exitosa
  ? Puerto 587 abierto

PERO el correo NO llega...

?????????????????????????????????????????????????????????????
?  ?? DIAGNÓSTICO: Gmail está BLOQUEANDO el correo         ?
?????????????????????????????????????????????????????????????

¿Por qué?
  - Remitente no verificado (mellamonose19@gmail.com)
  - No hay SPF/DKIM configurado
  - Gmail detecta el HTML como 'sospechoso'
  - El correo va a SPAM silenciosamente

"@ -ForegroundColor White

Write-Host "`n?? SOLUCIONES (en orden de probabilidad):" -ForegroundColor Green

Write-Host "`n1?? REVISAR SPAM EXHAUSTIVAMENTE" -ForegroundColor Yellow
Write-Host "   - Abre Gmail en navegador (no app)" -ForegroundColor White
Write-Host "   - Ve a 'Spam / Correo no deseado'" -ForegroundColor White
Write-Host "   - Busca: from:mellamonose19@gmail.com" -ForegroundColor White
Write-Host "   - O busca: [RESUMEN DIARIO SLA]" -ForegroundColor White

Write-Host "`n2?? AGREGAR REMITENTE A CONTACTOS" -ForegroundColor Yellow
Write-Host "   - Gmail Contacts ? Agregar: mellamonose19@gmail.com" -ForegroundColor White
Write-Host "   - Settings ? Filters ? Crear filtro" -ForegroundColor White
Write-Host "   - From: mellamonose19@gmail.com" -ForegroundColor White
Write-Host "   - Acción: 'Nunca enviar a Spam'" -ForegroundColor White

Write-Host "`n3?? PROBAR CON OTRO CORREO" -ForegroundColor Yellow
Write-Host "   SQL:" -ForegroundColor Gray
Write-Host "   UPDATE email_config SET DestinatarioResumen = 'tu-correo@outlook.com' WHERE Id = 1;" -ForegroundColor DarkGray
Write-Host "   Si llega a Outlook ? El problema es Gmail" -ForegroundColor White

Write-Host "`n4?? USAR SERVICIO PROFESIONAL" -ForegroundColor Yellow
Write-Host "   Gmail NO es confiable para correos automatizados" -ForegroundColor White
Write-Host "   Considera:" -ForegroundColor Gray
Write-Host "   - SendGrid (100 correos/día gratis)" -ForegroundColor DarkGray
Write-Host "   - Mailgun" -ForegroundColor DarkGray
Write-Host "   - Amazon SES" -ForegroundColor DarkGray

Write-Host "`n5?? VERIFICAR CONTRASEÑA DE APP" -ForegroundColor Yellow
Write-Host "   La contraseña 'mpwcevagvfehlfer' podría ser inválida" -ForegroundColor White
Write-Host "   Genera una nueva en:" -ForegroundColor Gray
Write-Host "   https://myaccount.google.com ? Contraseñas de aplicaciones" -ForegroundColor DarkGray

Write-Host "`n????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?  ? TEST COMPLETADO                                     ?" -ForegroundColor Green
Write-Host "????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Próximo paso:" -ForegroundColor Cyan
Write-Host "   Revisa tu carpeta de SPAM en Gmail" -ForegroundColor White
Write-Host "   El correo casi siempre está ahí" -ForegroundColor Yellow
Write-Host ""
