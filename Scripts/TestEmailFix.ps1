# ==========================================
# ?? Test Rápido del Fix de Email
# ==========================================

param(
    [string]$ApiUrl = "https://localhost:7152"
)

Write-Host @"
??????????????????????????????????????????????????????
?  ?? TEST: Fix de Excepciones Ocultas en Email    ?
?  Ahora verás el error REAL si falla               ?
??????????????????????????????????????????????????????
"@ -ForegroundColor Cyan

[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Host "`n?? Forzando envío de resumen diario..." -ForegroundColor Yellow
Write-Host "   Endpoint: POST $ApiUrl/api/email/send-summary" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/api/email/send-summary" -Method POST -ErrorAction Stop
    
    Write-Host "`n? RESPUESTA DEL SERVIDOR:" -ForegroundColor Green
    Write-Host "????????????????????????????????????????" -ForegroundColor Green
    
    if ($response.success) {
        Write-Host "? SUCCESS: TRUE" -ForegroundColor Green
        Write-Host "?? Mensaje: $($response.mensaje)" -ForegroundColor White
        Write-Host "?? Fecha: $($response.fecha)" -ForegroundColor White
        Write-Host "?? Tipo: $($response.tipo)" -ForegroundColor White
        
        Write-Host "`n??????????????????????????????????????????" -ForegroundColor Green
        Write-Host "?  ? ÉXITO: Correo enviado             ?" -ForegroundColor Green
        Write-Host "??????????????????????????????????????????" -ForegroundColor Green
        Write-Host "`n??  AHORA VERIFICA:" -ForegroundColor Yellow
        Write-Host "   1. Bandeja de entrada" -ForegroundColor White
        Write-Host "   2. Carpeta de SPAM ? MUY IMPORTANTE" -ForegroundColor White
        Write-Host "   3. Busca: [RESUMEN DIARIO SLA]" -ForegroundColor White
    } else {
        Write-Host "? SUCCESS: FALSE" -ForegroundColor Red
        Write-Host "??  Mensaje: $($response.mensaje)" -ForegroundColor Yellow
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorResponse = $null
    
    try {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorResponse = $reader.ReadToEnd() | ConvertFrom-Json
        $reader.Close()
    } catch {
        # Si no se puede parsear JSON, usar el mensaje original
    }
    
    Write-Host "`n? ERROR HTTP $statusCode" -ForegroundColor Red
    Write-Host "????????????????????????????????????????" -ForegroundColor Red
    
    if ($errorResponse) {
        Write-Host "`n?? SUCCESS: $($errorResponse.success)" -ForegroundColor Red
        Write-Host "?? MENSAJE: $($errorResponse.mensaje)" -ForegroundColor Yellow
        Write-Host "`n?? ERROR DETALLADO:" -ForegroundColor Red
        Write-Host "   $($errorResponse.error)" -ForegroundColor White
        
        if ($errorResponse.tipo) {
            Write-Host "`n???  TIPO: $($errorResponse.tipo)" -ForegroundColor Cyan
        }
        
        if ($errorResponse.tipoExcepcion) {
            Write-Host "?? TIPO EXCEPCIÓN: $($errorResponse.tipoExcepcion)" -ForegroundColor Cyan
        }
        
        if ($errorResponse.innerException) {
            Write-Host "`n??  INNER EXCEPTION:" -ForegroundColor Yellow
            Write-Host "   $($errorResponse.innerException)" -ForegroundColor Gray
        }
        
        if ($errorResponse.stackTrace) {
            Write-Host "`n?? STACK TRACE (primeras líneas):" -ForegroundColor Magenta
            foreach ($line in $errorResponse.stackTrace) {
                Write-Host "   $line" -ForegroundColor Gray
            }
        }
        
        if ($errorResponse.detalleCompleto) {
            Write-Host "`n?? DETALLE COMPLETO (primeras 500 chars):" -ForegroundColor Magenta
            $detalle = $errorResponse.detalleCompleto
            if ($detalle.Length -gt 500) {
                $detalle = $detalle.Substring(0, 500) + "..."
            }
            Write-Host $detalle -ForegroundColor Gray
        }
        
        Write-Host "`n??????????????????????????????????????????????????????" -ForegroundColor Red
        Write-Host "?  ? ERROR IDENTIFICADO                            ?" -ForegroundColor Red
        Write-Host "??????????????????????????????????????????????????????" -ForegroundColor Red
        
        # Análisis del error y sugerencias
        Write-Host "`n?? POSIBLES SOLUCIONES:" -ForegroundColor Yellow
        
        $error = $errorResponse.error.ToLower()
        
        if ($error -like "*autenticaci*" -or $error -like "*authentication*") {
            Write-Host "   ?? ERROR DE AUTENTICACIÓN SMTP" -ForegroundColor Red
            Write-Host "   1. Ve a: https://myaccount.google.com" -ForegroundColor White
            Write-Host "   2. Seguridad ? Verificación en 2 pasos" -ForegroundColor White
            Write-Host "   3. Contraseñas de aplicaciones ? Crear nueva" -ForegroundColor White
            Write-Host "   4. Actualiza Password en appsettings.json" -ForegroundColor White
            Write-Host "   5. Reinicia la API" -ForegroundColor White
        }
        elseif ($error -like "*connect*" -or $error -like "*socket*") {
            Write-Host "   ?? ERROR DE CONEXIÓN" -ForegroundColor Red
            Write-Host "   1. Verifica que el puerto 587 esté abierto" -ForegroundColor White
            Write-Host "   2. Desactiva temporalmente el firewall" -ForegroundColor White
            Write-Host "   3. Verifica proxy corporativo" -ForegroundColor White
            Write-Host "   4. Prueba desde otra red" -ForegroundColor White
        }
        elseif ($error -like "*ssl*" -or $error -like "*tls*") {
            Write-Host "   ?? ERROR SSL/TLS" -ForegroundColor Red
            Write-Host "   1. Verifica EnableSsl: true en appsettings.json" -ForegroundColor White
            Write-Host "   2. Verifica Port: 587" -ForegroundColor White
            Write-Host "   3. Actualiza certificados del sistema" -ForegroundColor White
        }
        elseif ($error -like "*destinatario*" -or $error -like "*configuraci*") {
            Write-Host "   ?? ERROR DE CONFIGURACIÓN" -ForegroundColor Red
            Write-Host "   Ejecuta en SQL Server:" -ForegroundColor White
            Write-Host "   UPDATE email_config SET DestinatarioResumen = 'tu@email.com' WHERE Id = 1;" -ForegroundColor Gray
        }
        else {
            Write-Host "   ??  ERROR GENERAL" -ForegroundColor Red
            Write-Host "   1. Revisa los logs en Visual Studio Output" -ForegroundColor White
            Write-Host "   2. Verifica appsettings.json > SmtpSettings" -ForegroundColor White
            Write-Host "   3. Consulta: PROBLEMA_RESUELTO_EMAIL.md" -ForegroundColor White
        }
        
    } else {
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor White
        Write-Host "   Detalles: $($_.ErrorDetails.Message)" -ForegroundColor Gray
    }
}

Write-Host "`n??????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? Siguiente paso:" -ForegroundColor Yellow
Write-Host "   Revisa los logs en Visual Studio Output window" -ForegroundColor White
Write-Host "   Busca líneas con emojis (?? ? ? ??)" -ForegroundColor White
Write-Host "??????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
