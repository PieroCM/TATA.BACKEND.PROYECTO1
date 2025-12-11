# ?? Ejemplos de Testing con cURL

## Comandos rápidos para probar los endpoints desde terminal

---

## ?? Obtener Configuración Actual

```bash
curl -X GET "https://localhost:7000/api/email/config" ^
  -H "Accept: application/json" ^
  -k
```

**PowerShell:**
```powershell
Invoke-RestMethod -Uri "https://localhost:7000/api/email/config" -Method Get
```

---

## ? Activar Resumen Diario + Configurar Hora

### **cURL (Windows CMD):**
```bash
curl -X PUT "https://localhost:7000/api/email/config/1" ^
  -H "Content-Type: application/json" ^
  -d "{\"resumenDiario\": true, \"horaResumen\": \"08:00:00\"}" ^
  -k
```

### **PowerShell:**
```powershell
$body = @{
    resumenDiario = $true
    horaResumen = "08:00:00"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/config/1" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
```

### **cURL (Linux/Mac):**
```bash
curl -X PUT "https://localhost:7000/api/email/config/1" \
  -H "Content-Type: application/json" \
  -d '{"resumenDiario": true, "horaResumen": "08:00:00"}' \
  -k
```

---

## ? Desactivar Resumen Diario

### **cURL (Windows):**
```bash
curl -X PUT "https://localhost:7000/api/email/config/1" ^
  -H "Content-Type: application/json" ^
  -d "{\"resumenDiario\": false}" ^
  -k
```

### **PowerShell:**
```powershell
$body = @{ resumenDiario = $false } | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/config/1" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
```

---

## ? Cambiar Solo la Hora

### **cURL (Windows):**
```bash
curl -X PUT "https://localhost:7000/api/email/config/1" ^
  -H "Content-Type: application/json" ^
  -d "{\"horaResumen\": \"14:30:00\"}" ^
  -k
```

### **PowerShell:**
```powershell
$body = @{ horaResumen = "14:30:00" } | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/config/1" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
```

---

## ?? Actualización Completa

### **PowerShell:**
```powershell
$body = @{
    destinatarioResumen = "nuevo@empresa.com"
    resumenDiario = $true
    horaResumen = "09:15:00"
    envioInmediato = $true
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/config/1" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
```

---

## ?? Probar Envío Manual de Resumen

### **cURL (Windows):**
```bash
curl -X POST "https://localhost:7000/api/email/send-summary" ^
  -H "Content-Type: application/json" ^
  -k
```

### **PowerShell:**
```powershell
Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/send-summary" `
    -Method Post
```

---

## ?? Script de Prueba Rápida (PowerShell)

```powershell
# Obtener configuración actual
Write-Host "?? Configuración actual:" -ForegroundColor Cyan
$config = Invoke-RestMethod -Uri "https://localhost:7000/api/email/config" -Method Get
$config | Format-List

# Activar resumen a las 08:00
Write-Host "`n? Activando resumen a las 08:00..." -ForegroundColor Green
$body = @{
    resumenDiario = $true
    horaResumen = "08:00:00"
} | ConvertTo-Json

$result = Invoke-RestMethod `
    -Uri "https://localhost:7000/api/email/config/1" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"

Write-Host "Success: $($result.success)" -ForegroundColor $(if($result.success){'Green'}else{'Red'})
Write-Host "Mensaje: $($result.mensaje)"

# Verificar cambio
Write-Host "`n?? Configuración actualizada:" -ForegroundColor Cyan
$configNueva = Invoke-RestMethod -Uri "https://localhost:7000/api/email/config" -Method Get
Write-Host "  Resumen Diario: $($configNueva.resumenDiario)"
Write-Host "  Hora Resumen: $($configNueva.horaResumen)"
```

---

## ?? Verificación en Base de Datos

```sql
-- Ver configuración actual
SELECT 
    id,
    destinatario_resumen,
    resumen_diario,
    CONVERT(VARCHAR(8), hora_resumen, 108) AS hora_resumen,
    envio_inmediato,
    actualizado_en
FROM email_config;

-- Historial de cambios (si existe tabla de log)
SELECT TOP 10 * 
FROM email_log 
WHERE tipo = 'RESUMEN'
ORDER BY fecha DESC;
```

---

## ?? Notas

- El flag `-k` en cURL ignora errores de certificado SSL (solo desarrollo)
- Para producción, usar certificados válidos
- Los comandos PowerShell son más legibles y recomendados en Windows
- Para testing automatizado, usar el script `Test-ConfiguracionResumenDiario.ps1`

---

## ?? Formato de Hora

Formatos válidos para `horaResumen`:
- ? `"08:00:00"` (formato completo)
- ? `"14:30:00"`
- ? `"23:59:59"`
- ? `"8:0:0"` (sin ceros al inicio)
- ? `"25:00:00"` (hora inválida)

---

## ?? Respuesta Exitosa

```json
{
  "success": true,
  "mensaje": "Configuración actualizada exitosamente",
  "data": {
    "id": 1,
    "destinatarioResumen": "mellamonose19@gmail.com",
    "envioInmediato": true,
    "resumenDiario": true,
    "horaResumen": "08:00:00",
    "creadoEn": "2024-01-01T10:00:00Z",
    "actualizadoEn": "2024-01-15T15:00:00Z"
  },
  "actualizadoEn": "2024-01-15T15:00:00Z"
}
```

---

## ? Respuesta de Error

```json
{
  "success": false,
  "mensaje": "Datos inválidos",
  "errores": [
    "El formato del email es inválido"
  ]
}
```
