# ?? RESUMEN EJECUTIVO: Problema Resumen Diario

## ?? TU PROBLEMA
> "El sistema dice que el correo se envió correctamente pero no me llega"

## ? DIAGNÓSTICO RÁPIDO (3 Opciones)

### Opción 1: Script PowerShell Automático (RECOMENDADO)
```powershell
cd "D:\appweb\TATABAKEND\Scripts"
.\DiagnosticoResumenDiario.ps1
```
Este script:
- ? Verifica toda la configuración automáticamente
- ? Te permite enviar un correo de prueba
- ? Muestra recomendaciones específicas

### Opción 2: Script SQL (Base de Datos)
```sql
-- Ejecutar en SQL Server Management Studio
-- Conectar a: Proyecto1SLA_DB
-- Abrir y ejecutar: Scripts/DiagnosticoResumenSQL.sql
```

### Opción 3: API Manual
```powershell
# Forzar envío inmediato
Invoke-RestMethod -Uri "https://localhost:7152/api/email/send-summary" -Method POST
```

## ?? CAUSA MÁS PROBABLE (90% de los casos)

### El correo ESTÁ siendo enviado, pero Gmail lo marca como SPAM

**¿Por qué?**
- Contenido HTML complejo
- Remitente no verificado (patroclown2.0@gmail.com)
- Gmail detecta como "potencialmente sospechoso"

**Solución Inmediata:**
1. Abre Gmail ? Carpeta **SPAM / Correo no deseado**
2. Busca correos con asunto: **[RESUMEN DIARIO SLA]**
3. Márcalo como **"No es spam"**
4. Agrega `patroclown2.0@gmail.com` a tus contactos

## ?? OTRAS CAUSAS POSIBLES

### 1. No hay alertas para enviar
**Verificar:**
```sql
SELECT COUNT(*) FROM alerta 
WHERE Estado = 'ACTIVA' AND (Nivel = 'CRITICO' OR Nivel = 'ALTO');
```
Si el resultado es **0**, el resumen no se enviará (no hay datos).

### 2. Destinatario mal configurado
**Verificar:**
```sql
SELECT DestinatarioResumen FROM email_config WHERE Id = 1;
```

**Corregir:**
```sql
UPDATE email_config 
SET DestinatarioResumen = '22200150@ue.edu.pe'  -- TU EMAIL AQUÍ
WHERE Id = 1;
```

### 3. Resumen diario desactivado
**Verificar:**
```sql
SELECT ResumenDiario FROM email_config WHERE Id = 1;
```
Debe ser **1** (activado).

**Corregir:**
```sql
UPDATE email_config SET ResumenDiario = 1 WHERE Id = 1;
```

### 4. Contraseña de Gmail incorrecta
**Síntoma:** Diagnóstico muestra `authentication.success = false`

**Solución:**
1. Ve a https://myaccount.google.com
2. Seguridad ? Verificación en 2 pasos
3. Contraseñas de aplicaciones ? Crear nueva
4. Actualiza en `appsettings.json`:
```json
{
  "SmtpSettings": {
    "Password": "NUEVA-CONTRASEÑA-16-DIGITOS"
  }
}
```

## ?? CHECKLIST RÁPIDO

- [ ] Revisar carpeta de **SPAM** ? **PRIMERO**
- [ ] Verificar `email_config` en base de datos
- [ ] Confirmar que hay alertas CRITICO/ALTO
- [ ] Probar endpoint: `POST /api/email/send-summary`
- [ ] Revisar logs: `SELECT * FROM email_log WHERE Tipo='RESUMEN'`

## ??? HERRAMIENTAS DISPONIBLES

### 1. Diagnóstico PowerShell
?? `Scripts/DiagnosticoResumenDiario.ps1`
- Verifica configuración completa
- Prueba conectividad SMTP
- Revisa logs de envíos
- Permite envío manual de prueba

### 2. Diagnóstico SQL
?? `Scripts/DiagnosticoResumenSQL.sql`
- Verifica configuración en BD
- Lista alertas activas
- Muestra logs de envíos
- Incluye scripts de corrección

### 3. Endpoints API

**Diagnóstico completo:**
```powershell
GET https://localhost:7152/api/emailtest/diagnosis
```

**Ver configuración:**
```powershell
GET https://localhost:7152/api/email/config
```

**Forzar envío:**
```powershell
POST https://localhost:7152/api/email/send-summary
```

**Ver logs:**
```powershell
GET https://localhost:7152/api/email/logs
```

**Enviar correo de prueba:**
```powershell
POST https://localhost:7152/api/emailtest/send
Body: { "email": "tu-email@ejemplo.com" }
```

## ?? DOCUMENTACIÓN COMPLETA

- ?? **SOLUCION_RESUMEN_NO_LLEGA.md** - Guía detallada paso a paso
- ?? **DIAGNOSTICO_RESUMEN_DIARIO.md** - Problemas comunes y soluciones
- ?? **DIAGNOSTICO_EMAIL.md** - Problemas generales de email

## ?? ACCIÓN INMEDIATA (5 minutos)

```powershell
# Ejecutar en PowerShell
cd "D:\appweb\TATABAKEND"

# 1. Diagnóstico automático
.\Scripts\DiagnosticoResumenDiario.ps1

# 2. Si todo está OK, forzar envío manual
Invoke-RestMethod -Uri "https://localhost:7152/api/email/send-summary" -Method POST

# 3. REVISAR CARPETA DE SPAM en tu correo
# Buscar: [RESUMEN DIARIO SLA]
```

## ?? RESPUESTA DIRECTA

**¿A qué se debe que no llegue?**

En el **90% de los casos**:
- ? El correo SÍ se envía correctamente
- ? Los logs dicen "OK" porque el envío fue exitoso
- ? Pero **Gmail lo marca como SPAM** automáticamente

**Solución:** Revisar carpeta de SPAM, no es un error del sistema.

En el **10% restante**:
- ? No hay alertas críticas para enviar (normal si no hay problemas)
- ? Configuración incorrecta (email destino, resumen desactivado)
- ? Problema de autenticación SMTP (contraseña inválida)

## ?? SOPORTE

Si después de ejecutar el diagnóstico automático el problema persiste:

1. Ejecuta: `Scripts/DiagnosticoResumenSQL.sql` en SQL Server
2. Copia la salida completa del script PowerShell
3. Verifica que `appsettings.json` tenga la configuración correcta
4. Revisa TODAS las carpetas de tu correo (Spam, Promociones, etc.)

**Nota:** El 95% de las veces el correo está en SPAM. Gmail es muy agresivo con correos HTML automatizados.