# =========================================
# ?? Script de Diagnóstico Completo
# Resumen Diario - Sistema TATA SLA
# =========================================

param(
    [string]$ApiBaseUrl = "https://localhost:7152",
    [string]$EmailDestino = "22200150@ue.edu.pe"
)

# Configuración
$ErrorActionPreference = "Continue"
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

# Colores
function Write-Success { param($msg) Write-Host "? $msg" -ForegroundColor Green }
function Write-Error-Custom { param($msg) Write-Host "? $msg" -ForegroundColor Red }
function Write-Warning-Custom { param($msg) Write-Host "??  $msg" -ForegroundColor Yellow }
function Write-Info { param($msg) Write-Host "??  $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Magenta }

Write-Host @"
??????????????????????????????????????????????????????
?  ?? DIAGNÓSTICO: Resumen Diario No Llega          ?
?  Sistema TATA SLA - Email Automation              ?
??????????????????????????????????????????????????????
"@ -ForegroundColor Cyan

Write-Info "API Base URL: $ApiBaseUrl"
Write-Info "Email Destino Esperado: $EmailDestino"
Write-Host ""

# =========================================
# PASO 1: Verificar Conectividad API
# =========================================
Write-Step "PASO 1: Verificando Conectividad con la API"
try {
    $healthCheck = Invoke-RestMethod -Uri "$ApiBaseUrl/api/emailtest/connectivity" -Method GET -TimeoutSec 10
    if ($healthCheck.success) {
        Write-Success "API respondiendo correctamente"
        Write-Info "  Host SMTP: $($healthCheck.host):$($healthCheck.port)"
    } else {
        Write-Error-Custom "API no responde correctamente"
    }
} catch {
    Write-Error-Custom "No se puede conectar a la API"
    Write-Info "  Error: $($_.Exception.Message)"
    Write-Warning-Custom "Verifica que la API esté ejecutándose en $ApiBaseUrl"
    exit 1
}

# =========================================
# PASO 2: Diagnóstico Completo SMTP
# =========================================
Write-Step "PASO 2: Diagnóstico Completo de Configuración SMTP"
try {
    $diagnosis = Invoke-RestMethod -Uri "$ApiBaseUrl/api/emailtest/diagnosis" -Method GET
    
    Write-Info "Configuración SMTP:"
    Write-Host "  ?? Host: $($diagnosis.smtpConfig.host)" -ForegroundColor White
    Write-Host "  ?? Puerto: $($diagnosis.smtpConfig.port)" -ForegroundColor White
    Write-Host "  ?? SSL: $($diagnosis.smtpConfig.enableSsl)" -ForegroundColor White
    Write-Host "  ?? De (From): $($diagnosis.smtpConfig.from)" -ForegroundColor White
    Write-Host "  ?? Usuario: $($diagnosis.smtpConfig.user)" -ForegroundColor White
    Write-Host "  ?? Contraseña Configurada: $($diagnosis.smtpConfig.passwordConfigured)" -ForegroundColor White
    
    Write-Host "`n  Conectividad:" -ForegroundColor Yellow
    if ($diagnosis.connectivity.internetAccess) {
        Write-Success "  ? Acceso a Internet"
    } else {
        Write-Error-Custom "  ? Sin acceso a Internet"
    }
    
    if ($diagnosis.connectivity.smtpPortReachable) {
        Write-Success "  ? Puerto SMTP accesible"
    } else {
        Write-Error-Custom "  ? Puerto SMTP no accesible"
    }
    
    if ($diagnosis.connectivity.dnsResolution) {
        Write-Success "  ? Resolución DNS correcta"
    } else {
        Write-Error-Custom "  ? Problemas de resolución DNS"
    }
    
    Write-Host "`n  Autenticación:" -ForegroundColor Yellow
    if ($diagnosis.authentication.success) {
        Write-Success "  ? Autenticación SMTP exitosa"
    } else {
        Write-Error-Custom "  ? Falló autenticación SMTP"
        Write-Info "  Error: $($diagnosis.authentication.error)"
        Write-Warning-Custom "  Verifica la contraseña de app de Gmail"
    }
    
} catch {
    Write-Error-Custom "Error al obtener diagnóstico"
    Write-Info "  $($_.Exception.Message)"
}

# =========================================
# PASO 3: Verificar Configuración EmailConfig
# =========================================
Write-Step "PASO 3: Verificando Configuración de Resumen Diario"
try {
    $config = Invoke-RestMethod -Uri "$ApiBaseUrl/api/email/config" -Method GET
    
    if ($config) {
        Write-Info "Configuración encontrada:"
        Write-Host "  ?? Destinatario: $($config.destinatarioResumen)" -ForegroundColor White
        Write-Host "  ??  Envío Inmediato: $($config.envioInmediato)" -ForegroundColor White
        Write-Host "  ?? Resumen Diario: $($config.resumenDiario)" -ForegroundColor White
        Write-Host "  ?? Hora Resumen: $($config.horaResumen)" -ForegroundColor White
        
        # Validaciones
        if ($config.resumenDiario) {
            Write-Success "Resumen diario ACTIVADO"
        } else {
            Write-Error-Custom "Resumen diario DESACTIVADO"
            Write-Warning-Custom "Necesitas activarlo para recibir correos automáticos"
        }
        
        if ($config.destinatarioResumen -eq $EmailDestino) {
            Write-Success "Destinatario correcto: $EmailDestino"
        } else {
            Write-Warning-Custom "Destinatario configurado: $($config.destinatarioResumen)"
            Write-Warning-Custom "Esperado: $EmailDestino"
        }
    } else {
        Write-Error-Custom "No se encontró configuración de EmailConfig"
        Write-Warning-Custom "Ejecuta las migraciones de base de datos"
    }
} catch {
    Write-Warning-Custom "No se pudo obtener configuración (endpoint puede no existir)"
    Write-Info "  $($_.Exception.Message)"
}

# =========================================
# PASO 4: Verificar Logs de Envío
# =========================================
Write-Step "PASO 4: Revisando Logs de Envíos Recientes"
try {
    $logs = Invoke-RestMethod -Uri "$ApiBaseUrl/api/email/logs" -Method GET
    
    if ($logs -and $logs.Count -gt 0) {
        $resumenLogs = $logs | Where-Object { $_.tipo -eq 'RESUMEN' } | Select-Object -First 5
        
        if ($resumenLogs) {
            Write-Info "Últimos 5 intentos de envío de resumen:"
            
            foreach ($log in $resumenLogs) {
                $emoji = switch ($log.estado) {
                    'OK' { '?' }
                    'ERROR' { '?' }
                    'PARCIAL' { '??' }
                    default { '?' }
                }
                
                Write-Host "`n  $emoji Estado: $($log.estado)" -ForegroundColor $(
                    switch ($log.estado) {
                        'OK' { 'Green' }
                        'ERROR' { 'Red' }
                        default { 'Yellow' }
                    }
                )
                Write-Host "     ?? Fecha: $($log.fecha)"
                Write-Host "     ?? Destinatario: $($log.destinatarios)"
                
                if ($log.errorDetalle) {
                    Write-Host "     ?? Detalle: $($log.errorDetalle)" -ForegroundColor Gray
                }
            }
            
            # Análisis
            $exitosos = ($resumenLogs | Where-Object { $_.estado -eq 'OK' }).Count
            $fallidos = ($resumenLogs | Where-Object { $_.estado -eq 'ERROR' }).Count
            
            Write-Host "`n  ?? Resumen de logs:" -ForegroundColor Cyan
            Write-Host "     Exitosos: $exitosos" -ForegroundColor Green
            Write-Host "     Fallidos: $fallidos" -ForegroundColor Red
            
        } else {
            Write-Warning-Custom "No hay logs de resumen diario"
            Write-Info "Esto es normal si nunca se ha ejecutado"
        }
    } else {
        Write-Warning-Custom "No hay logs disponibles"
    }
} catch {
    Write-Warning-Custom "No se pudieron obtener logs"
    Write-Info "  $($_.Exception.Message)"
}

# =========================================
# PASO 5: Prueba de Envío Manual
# =========================================
Write-Step "PASO 5: Prueba de Envío Manual de Resumen"
Write-Warning-Custom "¿Deseas forzar el envío manual del resumen? (Se enviará ahora)"
$respuesta = Read-Host "Escribe 'SI' para continuar, cualquier otra cosa para omitir"

if ($respuesta -eq 'SI') {
    try {
        Write-Info "Enviando resumen diario..."
        $result = Invoke-RestMethod -Uri "$ApiBaseUrl/api/email/send-summary" -Method POST
        
        Write-Success "Resumen enviado correctamente"
        Write-Info "  Mensaje: $($result.mensaje)"
        Write-Info "  Fecha: $($result.fecha)"
        Write-Host "`n" -NoNewline
        Write-Warning-Custom "AHORA REVISA TU CORREO:"
        Write-Host "  1. Bandeja de entrada" -ForegroundColor Yellow
        Write-Host "  2. Carpeta de SPAM/Correo no deseado" -ForegroundColor Yellow
        Write-Host "  3. Carpeta de Promociones (Gmail)" -ForegroundColor Yellow
        Write-Host "  4. Busca por asunto: '[RESUMEN DIARIO SLA]'" -ForegroundColor Yellow
        
    } catch {
        Write-Error-Custom "Error al enviar resumen"
        $errorDetail = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($errorDetail) {
            Write-Info "  Mensaje: $($errorDetail.mensaje)"
            Write-Info "  Error: $($errorDetail.error)"
        } else {
            Write-Info "  $($_.Exception.Message)"
        }
    }
} else {
    Write-Info "Omitiendo envío manual"
}

# =========================================
# PASO 6: Prueba de Correo Simple
# =========================================
Write-Step "PASO 6: Prueba de Correo Simple al Destinatario"
Write-Info "¿Deseas enviar un correo de prueba a $EmailDestino?"
$respuesta = Read-Host "Escribe 'SI' para continuar"

if ($respuesta -eq 'SI') {
    try {
        $body = @{ email = $EmailDestino } | ConvertTo-Json
        $result = Invoke-RestMethod -Uri "$ApiBaseUrl/api/emailtest/send" -Method POST -Body $body -ContentType "application/json"
        
        Write-Success "Correo de prueba enviado"
        Write-Info "  Revisa tu bandeja en los próximos minutos"
        Write-Warning-Custom "  Si este correo llega pero el resumen no, el problema está en el contenido HTML del resumen"
        
    } catch {
        Write-Error-Custom "Error al enviar correo de prueba"
        Write-Info "  $($_.Exception.Message)"
    }
} else {
    Write-Info "Omitiendo correo de prueba"
}

# =========================================
# RESUMEN Y RECOMENDACIONES
# =========================================
Write-Step "RESUMEN Y RECOMENDACIONES"

Write-Host @"

?? CHECKLIST DE VERIFICACIÓN:
???????????????????????????????????????????????

"@ -ForegroundColor Cyan

Write-Host "? EmailConfig.ResumenDiario = true" -ForegroundColor White
Write-Host "? EmailConfig.DestinatarioResumen = $EmailDestino" -ForegroundColor White
Write-Host "? Autenticación SMTP exitosa" -ForegroundColor White
Write-Host "? Existen alertas CRITICO/ALTO en la BD" -ForegroundColor White
Write-Host "? Logs muestran estado 'OK'" -ForegroundColor White
Write-Host "? Revisaste carpeta de SPAM" -ForegroundColor White
Write-Host "? Agregaste remitente a contactos seguros" -ForegroundColor White

Write-Host @"

?? PRINCIPALES CAUSAS DEL PROBLEMA:
???????????????????????????????????????????????

"@ -ForegroundColor Yellow

Write-Host "1. ?? SPAM: El correo llega pero Gmail lo marca como spam" -ForegroundColor White
Write-Host "   Solución: Revisa carpeta Spam, marca como 'No es spam'" -ForegroundColor Gray

Write-Host "`n2. ? NO HAY ALERTAS: No hay datos para enviar en el resumen" -ForegroundColor White
Write-Host "   Solución: Verifica que existan alertas CRITICO/ALTO en la BD" -ForegroundColor Gray

Write-Host "`n3. ?? EMAIL INCORRECTO: Destinatario mal configurado" -ForegroundColor White
Write-Host "   Solución: Actualiza EmailConfig.DestinatarioResumen" -ForegroundColor Gray

Write-Host "`n4. ?? AUTENTICACIÓN: Contraseña de app incorrecta" -ForegroundColor White
Write-Host "   Solución: Genera nueva contraseña de app en Gmail" -ForegroundColor Gray

Write-Host "`n5. ?? GMAIL LIMITA: Límite de envío diario superado" -ForegroundColor White
Write-Host "   Solución: Espera 24 horas o usa cuenta Google Workspace" -ForegroundColor Gray

Write-Host @"

?? PRÓXIMOS PASOS RECOMENDADOS:
???????????????????????????????????????????????

"@ -ForegroundColor Green

Write-Host "1. Revisa tu carpeta de SPAM en $EmailDestino" -ForegroundColor White
Write-Host "2. Busca correos con asunto '[RESUMEN DIARIO SLA]'" -ForegroundColor White
Write-Host "3. Si el diagnóstico muestra errores de autenticación:" -ForegroundColor White
Write-Host "   ? Ve a https://myaccount.google.com" -ForegroundColor Gray
Write-Host "   ? Seguridad ? Verificación en 2 pasos" -ForegroundColor Gray
Write-Host "   ? Contraseñas de aplicaciones ? Generar nueva" -ForegroundColor Gray
Write-Host "4. Ejecuta query SQL para verificar alertas:" -ForegroundColor White
Write-Host "   SELECT COUNT(*) FROM alerta WHERE Estado='ACTIVA' AND Nivel IN ('CRITICO','ALTO')" -ForegroundColor Gray

Write-Host "`n??????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  ? Diagnóstico Completado                        ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
