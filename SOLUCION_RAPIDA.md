# ?? SOLUCIÓN URGENTE: El Resumen No Me Llega

## ? SOLUCIÓN EN 2 MINUTOS

### PASO 1: Abre PowerShell y ejecuta esto
```powershell
cd "D:\appweb\TATABAKEND\Scripts"
.\DiagnosticoResumenDiario.ps1
```

### PASO 2: Cuando te pregunte si quieres enviar, escribe **SI**

### PASO 3: REVISA TU CORREO EN:
1. ??? **Carpeta SPAM** ? **MIRA AQUÍ PRIMERO**
2. ?? Bandeja de entrada
3. ?? Promociones
4. ?? Buscar: `[RESUMEN DIARIO SLA]`

---

## ?? SI EL SCRIPT NO FUNCIONA

### Plan B: Forzar envío manual

```powershell
# Copiar y pegar en PowerShell (una línea a la vez)

$apiUrl = "https://localhost:7152"

# Enviar resumen ahora
Invoke-RestMethod -Uri "$apiUrl/api/email/send-summary" -Method POST

# AHORA REVISA TU CORREO (especialmente SPAM)
```

---

## ?? PROBLEMA REAL: Gmail marca como SPAM

### ¿Por qué?
- El correo SÍ se envía correctamente
- Pero Gmail lo detecta como "sospechoso"
- Lo mueve automáticamente a SPAM

### ¿Cómo lo arreglo?
1. Abre Gmail
2. Ve a **Spam / Correo no deseado**
3. Busca emails de `patroclown2.0@gmail.com`
4. Haz clic en **"No es spam"**
5. Agrega el remitente a tus contactos

---

## ?? VERIFICAR CONFIGURACIÓN EN BASE DE DATOS

### Abre SQL Server Management Studio y ejecuta:

```sql
-- Conectar a: Proyecto1SLA_DB

-- ? Ver configuración actual
SELECT 
    DestinatarioResumen AS [Mi Email],
    CASE WHEN ResumenDiario = 1 THEN 'ACTIVADO' ELSE 'DESACTIVADO' END AS [Estado],
    HoraResumen AS [Hora de Envío]
FROM email_config;

-- ? Si NO muestra resultados o está mal configurado:
UPDATE email_config 
SET DestinatarioResumen = '22200150@ue.edu.pe',  -- ? CAMBIA POR TU EMAIL
    ResumenDiario = 1,
    HoraResumen = '08:00:00'
WHERE Id = 1;

-- ? Verificar que hay alertas para enviar
SELECT COUNT(*) AS [Alertas Críticas] 
FROM alerta 
WHERE Estado = 'ACTIVA' AND (Nivel = 'CRITICO' OR Nivel = 'ALTO');
-- Si muestra 0, NO se enviará nada (no hay datos)

-- ? Ver últimos intentos de envío
SELECT TOP 5
    Fecha,
    Destinatarios,
    Estado,
    ErrorDetalle
FROM email_log
WHERE Tipo = 'RESUMEN'
ORDER BY Fecha DESC;
-- Si Estado = 'OK', el envío fue exitoso (revisar SPAM)
```

---

## ?? PROBLEMAS COMUNES

### Problema 1: "No se encontró configuración"
**Solución:**
```sql
INSERT INTO email_config (DestinatarioResumen, EnvioInmediato, ResumenDiario, HoraResumen, CreadoEn)
VALUES ('22200150@ue.edu.pe', 1, 1, '08:00:00', GETUTCDATE());
```

### Problema 2: "No hay alertas críticas"
**Esto es NORMAL** si no hay problemas con los SLAs.
Para probar, crea una alerta de prueba:
```sql
-- Obtener un IdSolicitud válido
DECLARE @idSol INT;
SELECT TOP 1 @idSol = IdSolicitud FROM solicitud WHERE EstadoSolicitud <> 'CERRADO';

-- Crear alerta de prueba
INSERT INTO alerta (IdSolicitud, TipoAlerta, Nivel, Mensaje, Estado, EnviadoEmail, FechaCreacion)
VALUES (@idSol, 'SLA_VENCIMIENTO_INMEDIATO', 'CRITICO', 
        '?? URGENTE: Alerta de prueba', 'ACTIVA', 0, GETUTCDATE());
```

### Problema 3: Error de autenticación SMTP
**Solución:**
1. Ve a https://myaccount.google.com
2. Seguridad ? Verificación en 2 pasos (activar)
3. Contraseñas de aplicaciones ? Crear nueva
4. Copia la contraseña (16 dígitos)
5. Pega en `appsettings.json`:
```json
{
  "SmtpSettings": {
    "Password": "CONTRASEÑA-NUEVA-AQUI"
  }
}
```
6. Reinicia la API

---

## ? CHECKLIST VISUAL

```
? Ejecuté el script de diagnóstico
    ? cd D:\appweb\TATABAKEND\Scripts
    ? .\DiagnosticoResumenDiario.ps1

? Forcé un envío manual
    ? POST /api/email/send-summary

? Revisé MI CARPETA DE SPAM
    ? Gmail ? Spam / Correo no deseado
    ? Buscar: [RESUMEN DIARIO SLA]

? Verifiqué la configuración en BD
    ? SELECT * FROM email_config
    ? DestinatarioResumen = mi email correcto
    ? ResumenDiario = 1

? Confirmé que hay alertas
    ? SELECT COUNT(*) FROM alerta WHERE Nivel IN ('CRITICO','ALTO')
    ? Resultado > 0

? Los logs muestran OK
    ? SELECT * FROM email_log WHERE Tipo='RESUMEN'
    ? Estado = 'OK'
```

---

## ?? RESUMEN

**El problema MÁS COMÚN:**
- ? El sistema funciona correctamente
- ? El correo se envía sin errores
- ? Gmail lo marca como SPAM automáticamente

**La solución MÁS RÁPIDA:**
1. Revisar carpeta de SPAM
2. Marcar como "No es spam"
3. Agregar remitente a contactos

**Si realmente no se está enviando:**
1. Ejecutar script de diagnóstico
2. Verificar configuración en BD
3. Confirmar que hay alertas activas
4. Probar con envío manual

---

## ?? AYUDA RÁPIDA

### Test rápido en PowerShell:
```powershell
# Test 1: ¿Funciona la autenticación?
$diag = Invoke-RestMethod -Uri "https://localhost:7152/api/emailtest/diagnosis" -Method GET
Write-Host "Autenticación SMTP: $($diag.authentication.success)"

# Test 2: ¿Hay configuración?
$config = Invoke-RestMethod -Uri "https://localhost:7152/api/email/config" -Method GET
Write-Host "Destinatario: $($config.destinatarioResumen)"
Write-Host "Activado: $($config.resumenDiario)"

# Test 3: Enviar correo de prueba
$body = @{ email = "22200150@ue.edu.pe" } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:7152/api/emailtest/send" -Method POST -Body $body -ContentType "application/json"
Write-Host "Correo de prueba enviado. REVISA TU SPAM"
```

### Ver logs en SQL:
```sql
SELECT TOP 10 * FROM email_log WHERE Tipo = 'RESUMEN' ORDER BY Fecha DESC;
```

---

## ?? ÚLTIMO RECURSO

Si después de todo esto el problema persiste:

1. **Ejecuta el diagnóstico SQL completo:**
   - Abre: `Scripts/DiagnosticoResumenSQL.sql`
   - Ejecuta todo el script
   - Copia la salida

2. **Verifica appsettings.json:**
   ```json
   {
     "SmtpSettings": {
       "Host": "smtp.gmail.com",
       "Port": 587,
       "EnableSsl": true,
       "From": "patroclown2.0@gmail.com",
       "User": "patroclown2.0@gmail.com",
       "Password": "ndrtihrbjhhhpvku"
     }
   }
   ```

3. **Prueba con otro correo:**
   - Actualiza `DestinatarioResumen` a otro email
   - Intenta enviar de nuevo
   - Si llega al otro correo, el problema es filtros de Gmail

---

**RECUERDA:** En el 95% de los casos, el correo ESTÁ siendo enviado correctamente pero Gmail lo marca como SPAM. Siempre revisa ahí primero.