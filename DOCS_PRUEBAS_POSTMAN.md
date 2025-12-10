# ?? Guía de Pruebas en Postman - Sala de Comunicaciones

## ? Pre-requisitos

1. **Ejecutar el proyecto:**
   ```bash
   cd D:\appweb\TATABAKEND\TATA.BACKEND.PROYECTO1.API
   dotnet run
   ```

2. **Verificar el puerto:** 
   - La consola mostrará algo como: `Now listening on: https://localhost:7001`
   - Usa ese puerto en las URLs

3. **Verificar Swagger (opcional):**
   ```
   https://localhost:7001/swagger/index.html
   ```

---

## ?? Endpoints disponibles

### **Base URL:** `https://localhost:7001` (reemplazar con tu puerto)

---

## 1?? **GET** Obtener Roles Activos

**URL:**
```
GET https://localhost:7001/api/email/roles
```

**Headers:**
```
Content-Type: application/json
```

**Respuesta esperada (200 OK):**
```json
{
  "total": 3,
  "roles": [
    {
      "id": 1,
      "descripcion": "Desarrollador Backend"
    },
    {
      "id": 2,
      "descripcion": "Analista QA"
    }
  ]
}
```

**Posibles errores:**
- **404 Not Found:** Verifica que el proyecto esté corriendo y la URL sea correcta
- **500 Internal Server Error:** Revisa los logs del servidor

---

## 2?? **GET** Obtener SLAs Activos

**URL:**
```
GET https://localhost:7001/api/email/slas
```

**Headers:**
```
Content-Type: application/json
```

**Respuesta esperada (200 OK):**
```json
{
  "total": 2,
  "slas": [
    {
      "id": 1,
      "descripcion": "Onboarding Estándar"
    },
    {
      "id": 2,
      "descripcion": "Onboarding Express"
    }
  ]
}
```

---

## 3?? **GET** Vista Previa de Destinatarios

**Sin filtros:**
```
GET https://localhost:7001/api/email/preview-destinatarios
```

**Con filtro por Rol:**
```
GET https://localhost:7001/api/email/preview-destinatarios?idRol=1
```

**Con filtro por SLA:**
```
GET https://localhost:7001/api/email/preview-destinatarios?idSla=2
```

**Con ambos filtros:**
```
GET https://localhost:7001/api/email/preview-destinatarios?idRol=1&idSla=2
```

**Respuesta esperada (200 OK):**
```json
{
  "total": 3,
  "destinatarios": [
    {
      "idPersonal": 1,
      "nombreCompleto": "Juan Pérez García",
      "cargo": "Desarrollador Backend",
      "fotoUrl": null,
      "correo": "juan.perez@empresa.com"
    }
  ]
}
```

---

## 4?? **POST** Envío de Broadcast (Modo Prueba)

**URL:**
```
POST https://localhost:7001/api/email/broadcast
```

**Headers:**
```
Content-Type: application/json
```

**Body (JSON):**
```json
{
  "asunto": "Prueba de Envío Masivo",
  "mensajeHtml": "<html><body><h1>Hola</h1><p>Este es un mensaje de prueba.</p></body></html>",
  "idRol": null,
  "idSla": null,
  "esPrueba": true,
  "emailPrueba": "tu-email@gmail.com"
}
```

**Respuesta esperada (200 OK):**
```json
{
  "mensaje": "Correo de prueba enviado exitosamente a tu-email@gmail.com",
  "fecha": "2024-12-08T15:30:00.000Z",
  "modo": "PRUEBA",
  "filtros": {
    "idRol": null,
    "idSla": null
  }
}
```

---

## 5?? **POST** Envío de Broadcast (Producción con filtros)

**URL:**
```
POST https://localhost:7001/api/email/broadcast
```

**Body (JSON):**
```json
{
  "asunto": "Comunicado Importante - Sistema SLA",
  "mensajeHtml": "<html><body><h2>Actualización Importante</h2><p>Estimado equipo, les informamos sobre...</p></body></html>",
  "idRol": 1,
  "idSla": null,
  "esPrueba": false,
  "emailPrueba": null
}
```

**Respuesta esperada (200 OK):**
```json
{
  "mensaje": "Broadcast enviado exitosamente",
  "fecha": "2024-12-08T15:35:00.000Z",
  "modo": "PRODUCCIÓN",
  "filtros": {
    "idRol": 1,
    "idSla": null
  }
}
```

**Posibles errores:**
```json
{
  "mensaje": "No se encontraron destinatarios con los filtros especificados"
}
```

---

## 6?? **GET** Obtener Logs de Envío

**URL:**
```
GET https://localhost:7001/api/email/logs
```

**Respuesta esperada (200 OK):**
```json
{
  "total": 2,
  "logs": [
    {
      "id": 1,
      "fecha": "2024-12-08T15:30:00.000Z",
      "tipo": "BROADCAST_PRUEBA",
      "destinatarios": "test@email.com",
      "estado": "OK",
      "errorDetalle": "Envío de prueba exitoso"
    },
    {
      "id": 2,
      "fecha": "2024-12-08T15:35:00.000Z",
      "tipo": "BROADCAST",
      "destinatarios": "juan@empresa.com, maria@empresa.com",
      "estado": "OK",
      "errorDetalle": "Enviados exitosamente a 2 destinatarios"
    }
  ]
}
```

---

## ?? Solución de Problemas

### **Error 404 Not Found**

1. **Verificar que el proyecto esté corriendo:**
   ```bash
   cd TATA.BACKEND.PROYECTO1.API
   dotnet run
   ```

2. **Verificar el puerto correcto:**
   - Busca en la consola: `Now listening on: https://localhost:XXXX`
   - Actualiza la URL en Postman

3. **Verificar la ruta completa:**
   ```
   https://localhost:7001/api/email/roles   ? Correcto
   http://localhost:7001/api/email/roles    ? Debe ser HTTPS
   https://localhost:7001/email/roles       ? Falta /api
   ```

4. **Desactivar verificación SSL en Postman:**
   - Settings ? General ? SSL certificate verification ? OFF

### **Error 500 Internal Server Error**

1. **Revisar logs en la consola del servidor**
2. **Verificar que la base de datos esté corriendo**
3. **Verificar que existan datos:**
   ```sql
   SELECT * FROM rol_registro WHERE es_activo = 1;
   SELECT * FROM config_sla WHERE es_activo = 1;
   ```

### **No hay datos en la respuesta**

Si los endpoints funcionan pero retornan arrays vacíos:

```json
{
  "total": 0,
  "roles": []
}
```

**Solución:** Insertar datos de prueba:

```sql
-- Insertar roles
INSERT INTO rol_registro (nombre_rol, es_activo) 
VALUES ('Desarrollador Backend', 1);

-- Insertar SLAs
INSERT INTO config_sla (codigo_sla, tipo_solicitud, dias_umbral, es_activo) 
VALUES ('SLA-001', 'Onboarding Estándar', 30, 1);
```

---

## ?? Colección de Postman

Importa esta colección JSON en Postman:

```json
{
  "info": {
    "name": "Sala de Comunicaciones - TATA",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:7001",
      "type": "string"
    }
  ],
  "item": [
    {
      "name": "1. Obtener Roles",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/email/roles",
          "host": ["{{baseUrl}}"],
          "path": ["api", "email", "roles"]
        }
      }
    },
    {
      "name": "2. Obtener SLAs",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/email/slas",
          "host": ["{{baseUrl}}"],
          "path": ["api", "email", "slas"]
        }
      }
    },
    {
      "name": "3. Preview Destinatarios (Sin filtros)",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/email/preview-destinatarios",
          "host": ["{{baseUrl}}"],
          "path": ["api", "email", "preview-destinatarios"]
        }
      }
    },
    {
      "name": "4. Broadcast Modo Prueba",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"asunto\": \"Prueba de Broadcast\",\n  \"mensajeHtml\": \"<html><body><h1>Prueba</h1></body></html>\",\n  \"esPrueba\": true,\n  \"emailPrueba\": \"tu-email@gmail.com\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/email/broadcast",
          "host": ["{{baseUrl}}"],
          "path": ["api", "email", "broadcast"]
        }
      }
    },
    {
      "name": "5. Obtener Logs",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/email/logs",
          "host": ["{{baseUrl}}"],
          "path": ["api", "email", "logs"]
        }
      }
    }
  ]
}
```

---

## ? Checklist de Verificación

- [ ] Proyecto ejecutándose (`dotnet run`)
- [ ] Puerto correcto en las URLs
- [ ] HTTPS activado
- [ ] Base de datos con datos de prueba
- [ ] SSL verification desactivado en Postman
- [ ] Headers correctos (`Content-Type: application/json`)
- [ ] Endpoint correcto: `/api/email/roles` (no `/email/roles`)

---

¿Necesitas ayuda específica con algún error? ??
