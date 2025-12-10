# ?? Documentación: Envío de Resumen Diario a Múltiples Destinatarios

## ?? Objetivo
Permitir desde el frontend seleccionar administradores y analistas para enviarles el resumen diario de alertas SLA.

---

## ?? Nuevas Funcionalidades Implementadas

### 1. **Obtener Administradores y Analistas**
**Endpoint:** `GET /api/email/administradores-analistas`

**Descripción:** Obtiene la lista de usuarios activos con rol de Administrador (ID 1) o Analista (ID 2) que tengan correo corporativo.

**Response:**
```json
{
  "success": true,
  "total": 5,
  "usuarios": [
    {
      "idUsuario": 1,
      "username": "admin01",
      "correoCorporativo": "admin@empresa.com",
      "idRolSistema": 1,
      "nombreRol": "Administrador",
      "nombreCompleto": "Juan Pérez López",
      "tieneCorreo": true
    },
    {
      "idUsuario": 2,
      "username": "analista01",
      "correoCorporativo": "analista@empresa.com",
      "idRolSistema": 2,
      "nombreRol": "Analista",
      "nombreCompleto": "María García Torres",
      "tieneCorreo": true
    }
  ],
  "mensaje": "Se encontraron 5 administradores y analistas con correo corporativo"
}
```

---

### 2. **Enviar Resumen a Múltiples Destinatarios**
**Endpoint:** `POST /api/email/send-summary-multiple`

**Descripción:** Envía el resumen diario de alertas críticas/altas a los correos seleccionados.

**Request Body:**
```json
{
  "destinatarios": [
    "admin@empresa.com",
    "analista@empresa.com",
    "supervisor@empresa.com"
  ]
}
```

**Response Exitoso (todos los envíos OK):**
```json
{
  "success": true,
  "mensaje": "Resumen enviado exitosamente a 3 destinatario(s) con 15 alertas",
  "data": {
    "exito": true,
    "mensaje": "Resumen enviado exitosamente a 3 destinatario(s) con 15 alertas",
    "cantidadAlertas": 15,
    "correoEnviado": true,
    "destinatarios": [
      "admin@empresa.com",
      "analista@empresa.com",
      "supervisor@empresa.com"
    ],
    "resultadosEnvios": [
      {
        "destinatario": "admin@empresa.com",
        "exitoso": true,
        "mensajeError": null
      },
      {
        "destinatario": "analista@empresa.com",
        "exitoso": true,
        "mensajeError": null
      },
      {
        "destinatario": "supervisor@empresa.com",
        "exitoso": true,
        "mensajeError": null
      }
    ],
    "fecha": "2024-01-15T10:30:00Z"
  },
  "fecha": "2024-01-15T10:30:00Z"
}
```

**Response Parcial (algunos fallos):**
```json
{
  "success": true,
  "mensaje": "Resumen enviado parcialmente: 2 exitosos, 1 fallidos de 3 destinatarios",
  "data": {
    "exito": true,
    "mensaje": "Resumen enviado parcialmente: 2 exitosos, 1 fallidos de 3 destinatarios",
    "cantidadAlertas": 15,
    "correoEnviado": true,
    "destinatarios": [
      "admin@empresa.com",
      "invalido@noexiste.com",
      "analista@empresa.com"
    ],
    "resultadosEnvios": [
      {
        "destinatario": "admin@empresa.com",
        "exitoso": true,
        "mensajeError": null
      },
      {
        "destinatario": "invalido@noexiste.com",
        "exitoso": false,
        "mensajeError": "SmtpCommandException: No such user"
      },
      {
        "destinatario": "analista@empresa.com",
        "exitoso": true,
        "mensajeError": null
      }
    ],
    "fecha": "2024-01-15T10:30:00Z"
  }
}
```

**Response Sin Alertas:**
```json
{
  "success": true,
  "mensaje": "No se encontraron alertas para enviar",
  "data": {
    "exito": true,
    "mensaje": "No se encontraron alertas para enviar",
    "cantidadAlertas": 0,
    "correoEnviado": false,
    "destinatarios": [
      "admin@empresa.com",
      "analista@empresa.com"
    ],
    "resultadosEnvios": null,
    "fecha": "2024-01-15T10:30:00Z"
  }
}
```

---

## ??? Implementación en el Frontend

### Ejemplo con JavaScript/Fetch

#### 1. Obtener lista de Administradores y Analistas:
```javascript
async function obtenerAdministradoresAnalistas() {
  try {
    const response = await fetch('https://tu-api.com/api/email/administradores-analistas', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer TU_TOKEN_JWT'
      }
    });

    const data = await response.json();
    
    if (data.success) {
      console.log(`Se encontraron ${data.total} usuarios`);
      return data.usuarios;
    }
  } catch (error) {
    console.error('Error al obtener usuarios:', error);
  }
}
```

#### 2. Enviar resumen a destinatarios seleccionados:
```javascript
async function enviarResumenMultiple(correosSeleccionados) {
  try {
    const response = await fetch('https://tu-api.com/api/email/send-summary-multiple', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer TU_TOKEN_JWT'
      },
      body: JSON.stringify({
        destinatarios: correosSeleccionados
      })
    });

    const data = await response.json();
    
    if (data.success) {
      console.log('Resumen enviado:', data.mensaje);
      
      // Mostrar resultados individuales
      data.data.resultadosEnvios.forEach(resultado => {
        if (resultado.exitoso) {
          console.log(`? Enviado a ${resultado.destinatario}`);
        } else {
          console.log(`? Error al enviar a ${resultado.destinatario}: ${resultado.mensajeError}`);
        }
      });
    }
  } catch (error) {
    console.error('Error al enviar resumen:', error);
  }
}

// Uso:
const correosSeleccionados = [
  'admin@empresa.com',
  'analista@empresa.com'
];

enviarResumenMultiple(correosSeleccionados);
```

### Ejemplo con React:
```jsx
import React, { useState, useEffect } from 'react';

function ResumenEmailSelector() {
  const [usuarios, setUsuarios] = useState([]);
  const [seleccionados, setSeleccionados] = useState([]);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    cargarUsuarios();
  }, []);

  const cargarUsuarios = async () => {
    const response = await fetch('/api/email/administradores-analistas');
    const data = await response.json();
    if (data.success) {
      setUsuarios(data.usuarios);
    }
  };

  const toggleSeleccion = (correo) => {
    if (seleccionados.includes(correo)) {
      setSeleccionados(seleccionados.filter(c => c !== correo));
    } else {
      setSeleccionados([...seleccionados, correo]);
    }
  };

  const enviarResumen = async () => {
    if (seleccionados.length === 0) {
      alert('Selecciona al menos un destinatario');
      return;
    }

    setEnviando(true);
    try {
      const response = await fetch('/api/email/send-summary-multiple', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ destinatarios: seleccionados })
      });

      const data = await response.json();
      
      if (data.success) {
        alert(`? ${data.mensaje}`);
      } else {
        alert(`?? ${data.mensaje}`);
      }
    } catch (error) {
      alert('? Error al enviar resumen');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div>
      <h2>Seleccionar Destinatarios del Resumen Diario</h2>
      
      <div className="usuarios-lista">
        {usuarios.map(usuario => (
          <div key={usuario.idUsuario} className="usuario-item">
            <input
              type="checkbox"
              checked={seleccionados.includes(usuario.correoCorporativo)}
              onChange={() => toggleSeleccion(usuario.correoCorporativo)}
            />
            <span>{usuario.nombreCompleto}</span>
            <span className="rol">{usuario.nombreRol}</span>
            <span className="correo">{usuario.correoCorporativo}</span>
          </div>
        ))}
      </div>

      <button 
        onClick={enviarResumen} 
        disabled={enviando || seleccionados.length === 0}
      >
        {enviando ? 'Enviando...' : `Enviar Resumen a ${seleccionados.length} destinatario(s)`}
      </button>
    </div>
  );
}
```

---

## ?? Logs y Registro

Cada envío se registra en la tabla `email_log` con:
- **Tipo:** `RESUMEN_MULTIPLE`
- **Destinatarios:** Cada correo se registra individualmente
- **Estado:** `OK` o `ERROR`
- **ErrorDetalle:** Mensaje de error si falla

---

## ?? Características Implementadas

? **Filtrado automático de usuarios activos** (solo Administradores y Analistas)
? **Validación de correos corporativos** (solo usuarios con correo registrado)
? **Envío individual con control de errores** (un error no detiene los demás envíos)
? **Resultado detallado por destinatario** (éxito/error para cada correo)
? **Deduplicación de correos** (elimina duplicados automáticamente)
? **Registro en BD** (cada envío se registra en `email_log`)
? **Logs detallados** (seguimiento completo en logs del servidor)

---

## ?? Testing

### Prueba desde Swagger/Postman:

#### 1. GET Administradores y Analistas:
```http
GET /api/email/administradores-analistas
Authorization: Bearer TU_TOKEN
```

#### 2. POST Envío Múltiple:
```http
POST /api/email/send-summary-multiple
Content-Type: application/json
Authorization: Bearer TU_TOKEN

{
  "destinatarios": [
    "admin@empresa.com",
    "analista@empresa.com"
  ]
}
```

---

## ?? Notas Importantes

1. **Solo se envía si hay alertas:** Si no hay alertas críticas/altas activas, no se envía el correo (se retorna mensaje informativo).

2. **Roles válidos:** Solo usuarios con `IdRolSistema = 1` (Administrador) o `IdRolSistema = 2` (Analista).

3. **Estado activo requerido:** Solo usuarios con `Estado = "ACTIVO"`.

4. **Correo corporativo obligatorio:** Solo usuarios con `CorreoCorporativo` no vacío.

5. **Envíos independientes:** Si un correo falla, los demás se envían normalmente.

6. **HTML enriquecido:** El correo incluye tabla con todas las alertas, estadísticas y estilos profesionales.

---

## ?? Endpoint Original Mantenido

El endpoint original `/api/email/send-summary` (POST) sigue funcionando igual, enviando al destinatario configurado en `email_config.destinatario_resumen`.

---

## ?? Referencias

- **Interface:** `IEmailAutomationService.cs`
- **Servicio:** `EmailAutomationService.cs`
- **Controlador:** `EmailController.cs`
- **DTOs:** `EmailConfigDTO.cs`

---

**Desarrollado por:** GitHub Copilot
**Fecha:** 2024
**Versión:** .NET 9
