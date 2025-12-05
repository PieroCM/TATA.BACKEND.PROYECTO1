# ?? ENDPOINTS API - SISTEMA GESTIÓN ALERTAS SLA TATA

## ?? Configuración Base
- **Base URL**: `http://localhost:5260`
- **Content-Type**: `application/json`
- **Authentication**: Bearer Token (JWT) para endpoints protegidos

---

## ?? **ALERTAS - Endpoints Principales**

### 1. **Sincronizar Alertas (UPSERT Inteligente)**
Actualiza/crea alertas automáticamente desde solicitudes existentes

```http
POST http://localhost:5260/api/alertas/sync
```

**Respuesta exitosa (200):**
```json
{
  "mensaje": "Sincronización de alertas completada exitosamente",
  "fecha": "2025-01-25T10:30:00.000Z"
}
```

---

### 2. **Obtener Dashboard Enriquecido (Data Plana para Frontend)**
Retorna datos listos para consumir sin anidación profunda

```http
GET http://localhost:5260/api/alertas/dashboard
```

**Respuesta exitosa (200):**
```json
[
  {
    "idAlerta": 1,
    "tipoAlerta": "SLA_VENCIMIENTO_INMEDIATO",
    "nivel": "CRITICO",
    "mensaje": "?? URGENTE: SOL-000001 vence en 2 día(s). SLA: 10 días",
    "estado": "ACTIVA",
    "enviadoEmail": false,
    "fechaCreacion": "2025-01-25T08:00:00",
    
    "idSolicitud": 1,
    "codigoSolicitud": "SOL-000001",
    "fechaSolicitud": "2025-01-15T00:00:00",
    "estadoSolicitud": "EN_PROCESO",
    
    "idPersonal": 5,
    "nombreResponsable": "Juan Pérez",
    "emailResponsable": "jperez@tata.com",
    "documentoResponsable": "12345678",
    
    "idRolRegistro": 2,
    "nombreRol": "Analista TI",
    "bloqueTech": "Infraestructura",
    
    "idSla": 1,
    "codigoSla": "SLA_ALTA",
    "nombreSla": "SLA para solicitudes de alta prioridad",
    "diasUmbral": 10,
    "tipoSolicitud": "ALTA",
    
    "diasRestantes": 2,
    "porcentajeProgreso": 80,
    "colorEstado": "#FF0000",
    "iconoEstado": "error",
    "estaVencida": false,
    "esCritica": true
  }
]
```

---

### 3. **Obtener Todas las Alertas (Formato Completo)**
```http
GET http://localhost:5260/api/alertas
```

---

### 4. **Obtener Alerta por ID**
```http
GET http://localhost:5260/api/alertas/1
```

---

### 5. **Crear Alerta Manualmente**
```http
POST http://localhost:5260/api/alertas
Content-Type: application/json

{
  "idSolicitud": 1,
  "tipoAlerta": "SLA_PREVENTIVA",
  "nivel": "MEDIO",
  "mensaje": "Alerta preventiva de seguimiento",
  "estado": "NUEVA"
}
```

---

### 6. **Actualizar Alerta**
```http
PUT http://localhost:5260/api/alertas/1
Content-Type: application/json

{
  "nivel": "ALTO",
  "mensaje": "Alerta escalada a nivel ALTO",
  "estado": "LEIDA",
  "enviadoEmail": true
}
```

---

### 7. **Eliminar Alerta (Lógico)**
```http
DELETE http://localhost:5260/api/alertas/1
```

---

## ?? **EMAIL AUTOMATION - Comunicaciones**

### 8. **Enviar Broadcast Masivo**
Envía correos a usuarios filtrados por Rol y/o SLA

```http
POST http://localhost:5260/api/email/broadcast
Content-Type: application/json

{
  "idRol": 2,
  "idSla": null,
  "asunto": "Comunicado Urgente - Mantenimiento Programado",
  "mensajeHtml": "<h1>Estimado equipo</h1><p>Se realizará mantenimiento el día de mañana...</p>"
}
```

**Ejemplo sin filtros (todos los usuarios activos):**
```json
{
  "idRol": null,
  "idSla": null,
  "asunto": "Comunicado General",
  "mensajeHtml": "<h2>Atención a todo el personal</h2><p>Mensaje importante...</p>"
}
```

---

### 9. **Obtener Configuración de Email**
```http
GET http://localhost:5260/api/email/config
```

**Respuesta (200):**
```json
{
  "id": 1,
  "destinatarioResumen": "admin@tata.com",
  "envioInmediato": true,
  "resumenDiario": true,
  "horaResumen": "08:00:00",
  "creadoEn": "2025-01-20T00:00:00",
  "actualizadoEn": "2025-01-25T10:00:00"
}
```

---

### 10. **Actualizar Configuración de Email**
```http
PUT http://localhost:5260/api/email/config/1
Content-Type: application/json

{
  "destinatarioResumen": "admin@tata.com",
  "envioInmediato": true,
  "resumenDiario": true,
  "horaResumen": "09:00:00"
}
```

---

### 11. **Enviar Resumen Diario Manualmente** (Para Pruebas)
```http
POST http://localhost:5260/api/email/send-summary
```

---

## ?? **SOLICITUDES**

### 12. **Crear Solicitud (Con Alerta Automática)**
```http
POST http://localhost:5260/api/solicitud
Content-Type: application/json

{
  "idPersonal": 5,
  "idSla": 1,
  "idRolRegistro": 2,
  "creadoPor": 1,
  "fechaSolicitud": "2025-01-25T00:00:00",
  "fechaIngreso": null,
  "resumenSla": "",
  "origenDato": "MANUAL",
  "estadoSolicitud": "EN_PROCESO"
}
```

**Nota**: Al crear una solicitud, automáticamente se crea una alerta inicial.

---

### 13. **Actualizar Solicitud**
```http
PUT http://localhost:5260/api/solicitud/1
Content-Type: application/json

{
  "idPersonal": 5,
  "idSla": 1,
  "idRolRegistro": 2,
  "creadoPor": 1,
  "fechaSolicitud": "2025-01-25T00:00:00",
  "fechaIngreso": "2025-01-28T00:00:00",
  "resumenSla": "Solicitud completada dentro del SLA",
  "origenDato": "MANUAL",
  "estadoSolicitud": "CERRADO"
}
```

---

### 14. **Obtener Todas las Solicitudes**
```http
GET http://localhost:5260/api/solicitud
```

---

### 15. **Obtener Solicitud por ID**
```http
GET http://localhost:5260/api/solicitud/1
```

---

## ?? **AUTENTICACIÓN**

### 16. **Login**
```http
POST http://localhost:5260/api/usuario/signin
Content-Type: application/json

{
  "correo": "admin@tata.com",
  "password": "Admin123"
}
```

**Respuesta (200):**
```json
{
  "message": "Inicio de sesión exitoso",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

---

### 17. **Registro de Usuario**
```http
POST http://localhost:5260/api/usuario/signup
Content-Type: application/json

{
  "username": "nuevoUsuario",
  "correo": "nuevo@tata.com",
  "password": "Password123",
  "idRolSistema": 2
}
```

---

## ?? **FLUJO RECOMENDADO DE PRUEBAS**

### **Paso 1: Autenticación**
```http
POST /api/usuario/signin
```

### **Paso 2: Crear Solicitudes**
```http
POST /api/solicitud
```

### **Paso 3: Sincronizar Alertas**
```http
POST /api/alertas/sync
```

### **Paso 4: Ver Dashboard**
```http
GET /api/alertas/dashboard
```

### **Paso 5: Configurar Email**
```http
PUT /api/email/config/1
```

### **Paso 6: Probar Broadcast**
```http
POST /api/email/broadcast
```

### **Paso 7: Probar Resumen Manual**
```http
POST /api/email/send-summary
```

---

## ?? **AUTOMATIZACIÓN EN BACKGROUND**

El sistema incluye un **DailySummaryWorker** que:

- ? Se ejecuta automáticamente cada 60 segundos
- ? Verifica la hora configurada en `EmailConfig.HoraResumen`
- ? Envía el resumen diario automáticamente cuando coincide la hora
- ? Solo envía una vez por día

**No requiere intervención manual para el envío diario.**

---

## ?? **CÓDIGOS DE RESPUESTA**

| Código | Significado | Uso |
|--------|-------------|-----|
| 200 | OK | Operación exitosa |
| 201 | Created | Recurso creado exitosamente |
| 204 | No Content | Eliminación exitosa |
| 400 | Bad Request | Datos inválidos o faltantes |
| 404 | Not Found | Recurso no encontrado |
| 500 | Internal Server Error | Error del servidor |

---

## ??? **SEGURIDAD**

- Todos los errores internos se logean en el servidor
- Al cliente solo se envían mensajes genéricos
- Los tokens JWT tienen duración de 120 minutos (configurable)
- Las contraseñas se hashean con BCrypt

---

## ?? **COLECCIÓN POSTMAN**

Importa este JSON en Postman para probar todos los endpoints rápidamente.

**Variables de entorno recomendadas:**
- `base_url`: `http://localhost:5260`
- `token`: `{{token}}` (se guarda automáticamente al hacer login)

---

## ?? **CARACTERÍSTICAS AVANZADAS**

### **1. Dashboard Inteligente**
- ? Datos planos sin anidación (fácil de consumir)
- ? Cálculos matemáticos precalculados
- ? Colores e iconos sugeridos para UI
- ? Email del responsable listo para envío

### **2. Sincronización UPSERT**
- ? Crea alertas nuevas automáticamente
- ? Actualiza alertas existentes si cambia el nivel
- ? Procesa todas las solicitudes activas
- ? Logging detallado de operaciones

### **3. Sistema de Broadcast**
- ? Filtrado por rol y/o tipo de SLA
- ? Envío masivo con HTML personalizado
- ? Registro de logs de cada envío
- ? Manejo robusto de errores

### **4. Resumen Diario Automático**
- ? Background Worker autónomo
- ? Configuración horaria flexible
- ? HTML responsive con estilos modernos
- ? Solo alertas críticas y altas

---

## ?? **SOPORTE**

Para dudas o reportar issues, contactar al equipo de desarrollo.

**Fecha de última actualización**: 25/01/2025
**Versión API**: 1.0.0
