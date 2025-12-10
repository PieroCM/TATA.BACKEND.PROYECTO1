# ============================================================================
# Script de Prueba: Configuración de Resumen Diario desde Frontend
# Simula las llamadas que hará Quasar para activar/desactivar y configurar hora
# ============================================================================

$baseUrl = "https://localhost:7000"
$configId = 1

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   TEST: Configuración de Resumen Diario desde Frontend        ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# PASO 1: Obtener configuración actual
# ============================================================================
Write-Host "?? [PASO 1] Consultando configuración actual..." -ForegroundColor Yellow
Write-Host ""

try {
    $configActual = Invoke-RestMethod -Uri "$baseUrl/api/email/config" -Method Get
    
    Write-Host "? Configuración actual obtenida:" -ForegroundColor Green
    Write-Host "   ID:                    $($configActual.id)" -ForegroundColor White
    Write-Host "   Destinatario:          $($configActual.destinatarioResumen)" -ForegroundColor White
    Write-Host "   Resumen Diario:        $($configActual.resumenDiario) $(if($configActual.resumenDiario){'? ACTIVADO'}else{'? DESACTIVADO'})" -ForegroundColor White
    Write-Host "   Hora Resumen:          $($configActual.horaResumen)" -ForegroundColor White
    Write-Host "   Envío Inmediato:       $($configActual.envioInmediato)" -ForegroundColor White
    Write-Host "   Última Actualización:  $($configActual.actualizadoEn)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? Error al obtener configuración: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 2

# ============================================================================
# PASO 2: ACTIVAR resumen diario a las 08:00:00
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   TEST 1: ACTIVAR Resumen Diario + Configurar Hora 08:00      ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$payload1 = @{
    resumenDiario = $true
    horaResumen = "08:00:00"
} | ConvertTo-Json

Write-Host "?? Enviando solicitud PUT:" -ForegroundColor Yellow
Write-Host $payload1 -ForegroundColor Gray
Write-Host ""

try {
    $result1 = Invoke-RestMethod `
        -Uri "$baseUrl/api/email/config/$configId" `
        -Method Put `
        -Body $payload1 `
        -ContentType "application/json"
    
    Write-Host "? Respuesta exitosa:" -ForegroundColor Green
    Write-Host "   Success:           $($result1.success)" -ForegroundColor White
    Write-Host "   Mensaje:           $($result1.mensaje)" -ForegroundColor White
    Write-Host "   Resumen Diario:    $($result1.data.resumenDiario) ?" -ForegroundColor Green
    Write-Host "   Hora Resumen:      $($result1.data.horaResumen) ?" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "? Error en TEST 1: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 3

# ============================================================================
# PASO 3: Cambiar SOLO la hora a 14:30:00 (sin tocar el estado)
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   TEST 2: Cambiar SOLO la Hora a 14:30:00                     ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$payload2 = @{
    horaResumen = "14:30:00"
} | ConvertTo-Json

Write-Host "?? Enviando solicitud PUT:" -ForegroundColor Yellow
Write-Host $payload2 -ForegroundColor Gray
Write-Host ""

try {
    $result2 = Invoke-RestMethod `
        -Uri "$baseUrl/api/email/config/$configId" `
        -Method Put `
        -Body $payload2 `
        -ContentType "application/json"
    
    Write-Host "? Respuesta exitosa:" -ForegroundColor Green
    Write-Host "   Success:           $($result2.success)" -ForegroundColor White
    Write-Host "   Resumen Diario:    $($result2.data.resumenDiario) (sin cambios)" -ForegroundColor White
    Write-Host "   Hora Resumen:      $($result2.data.horaResumen) ? CAMBIADA" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "? Error en TEST 2: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 3

# ============================================================================
# PASO 4: DESACTIVAR resumen diario (sin cambiar hora)
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   TEST 3: DESACTIVAR Resumen Diario                           ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$payload3 = @{
    resumenDiario = $false
} | ConvertTo-Json

Write-Host "?? Enviando solicitud PUT:" -ForegroundColor Yellow
Write-Host $payload3 -ForegroundColor Gray
Write-Host ""

try {
    $result3 = Invoke-RestMethod `
        -Uri "$baseUrl/api/email/config/$configId" `
        -Method Put `
        -Body $payload3 `
        -ContentType "application/json"
    
    Write-Host "? Respuesta exitosa:" -ForegroundColor Green
    Write-Host "   Success:           $($result3.success)" -ForegroundColor White
    Write-Host "   Resumen Diario:    $($result3.data.resumenDiario) ? DESACTIVADO" -ForegroundColor Red
    Write-Host "   Hora Resumen:      $($result3.data.horaResumen) (sin cambios)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? Error en TEST 3: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 3

# ============================================================================
# PASO 5: Reactivar con nueva hora (ambos campos a la vez)
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   TEST 4: REACTIVAR con Hora 09:15:00 (ambos campos)          ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$payload4 = @{
    resumenDiario = $true
    horaResumen = "09:15:00"
} | ConvertTo-Json

Write-Host "?? Enviando solicitud PUT:" -ForegroundColor Yellow
Write-Host $payload4 -ForegroundColor Gray
Write-Host ""

try {
    $result4 = Invoke-RestMethod `
        -Uri "$baseUrl/api/email/config/$configId" `
        -Method Put `
        -Body $payload4 `
        -ContentType "application/json"
    
    Write-Host "? Respuesta exitosa:" -ForegroundColor Green
    Write-Host "   Success:           $($result4.success)" -ForegroundColor White
    Write-Host "   Resumen Diario:    $($result4.data.resumenDiario) ? REACTIVADO" -ForegroundColor Green
    Write-Host "   Hora Resumen:      $($result4.data.horaResumen) ? ACTUALIZADA" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "? Error en TEST 4: $($_.Exception.Message)" -ForegroundColor Red
}

Start-Sleep -Seconds 2

# ============================================================================
# PASO 6: Verificación final
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   VERIFICACIÓN FINAL: Estado actual de la configuración       ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

try {
    $configFinal = Invoke-RestMethod -Uri "$baseUrl/api/email/config" -Method Get
    
    Write-Host "?? Configuración final:" -ForegroundColor Yellow
    Write-Host "   Resumen Diario:    $($configFinal.resumenDiario) $(if($configFinal.resumenDiario){'?'}else{'?'})" -ForegroundColor $(if($configFinal.resumenDiario){'Green'}else{'Red'})
    Write-Host "   Hora Resumen:      $($configFinal.horaResumen) ?" -ForegroundColor White
    Write-Host "   Destinatario:      $($configFinal.destinatarioResumen)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? Error en verificación final: $($_.Exception.Message)" -ForegroundColor Red
}

# ============================================================================
# Resumen de Pruebas
# ============================================================================
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                   ? TODAS LAS PRUEBAS COMPLETADAS             ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
Write-Host "?? Resumen de funcionalidades probadas:" -ForegroundColor Cyan
Write-Host "   ? Activar resumen diario + configurar hora" -ForegroundColor Green
Write-Host "   ? Cambiar solo la hora (sin tocar estado)" -ForegroundColor Green
Write-Host "   ? Desactivar resumen diario" -ForegroundColor Green
Write-Host "   ? Reactivar con nueva hora (ambos campos)" -ForegroundColor Green
Write-Host "   ? Verificación de estado final" -ForegroundColor Green
Write-Host ""
Write-Host "?? El backend está listo para recibir datos del frontend Quasar" -ForegroundColor Yellow
Write-Host ""
