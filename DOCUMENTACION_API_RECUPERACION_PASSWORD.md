# ?? API de Recuperación de Contraseña - Sistema SLA

## ?? Configuración Previa

Asegúrate de tener configurado SMTP en `appsettings.json`:

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "From": "tu-email@gmail.com",
    "User": "tu-email@gmail.com",
    "Password": "tu-app-password"
  }
}
```

## ?? Endpoints Disponibles

### 1. ?? Iniciar Sesión (SignIn)
**Endpoint:** `POST http://localhost:5260/api/usuario/signin`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "correo": "usuario@example.com",
  "password": "Password123!"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "message": "Inicio de sesión exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Respuesta Error (401 Unauthorized):**
```json
{
  "message": "Credenciales inválidas"
}
```

---

### 2. ?? Registrarse (SignUp)
**Endpoint:** `POST http://localhost:5260/api/usuario/signup`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "username": "juanperez",
  "correo": "juan.perez@example.com",
  "password": "Password123!"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "message": "Usuario registrado correctamente"
}
```

**Respuesta Error (400 Bad Request):**
```json
{
  "message": "El correo ya está registrado"
}
```

---

### 3. ?? Solicitar Recuperación de Contraseña
**Endpoint:** `POST http://localhost:5260/api/usuario/solicitar-recuperacion`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "email": "usuario@example.com"
}
```

**Respuesta (200 OK):**
```json
{
  "message": "Si el correo existe, recibirás un enlace de recuperación."
}
```

**?? Email que recibirá el usuario:**
- Asunto: "Recuperación de Contraseña - Sistema SLA"
- Contiene un **token de 64 caracteres** (ejemplo: `A1B2C3D4E5F6...`)
- El token expira en **1 hora**

**Notas:**
- Por seguridad, siempre devuelve 200 OK aunque el email no exista
- El token se envía por correo electrónico al usuario
- El usuario debe copiar el token del email para el siguiente paso

---

### 4. ?? Restablecer Contraseña con Token
**Endpoint:** `POST http://localhost:5260/api/usuario/restablecer-password`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "email": "usuario@example.com",
  "token": "A1B2C3D4E5F6789012345678901234567890ABCDEF1234567890ABCDEF1234",
  "nuevaPassword": "NuevaPassword123!"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "message": "Contraseña actualizada exitosamente."
}
```

**Respuesta Error (400 Bad Request):**
```json
{
  "message": "Token inválido o expirado. Solicita uno nuevo."
}
```

**?? Email de confirmación:**
- Asunto: "Contraseña Actualizada - Sistema SLA"
- Confirma que la contraseña fue cambiada exitosamente

**Notas:**
- El token solo puede usarse una vez
- Después de usar el token, este se elimina de la base de datos
- Si el token expiró (más de 1 hora), solicita uno nuevo

---

### 5. ?? Cambiar Contraseña (Usuario Logueado)
**Endpoint:** `PUT http://localhost:5260/api/usuario/cambiar-password`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {token-jwt}
```

**Body:**
```json
{
  "correo": "usuario@example.com",
  "passwordActual": "Password123!",
  "nuevaPassword": "NuevaPassword456!"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "message": "Contraseña actualizada correctamente"
}
```

**Respuesta Error (400 Bad Request):**
```json
{
  "message": "Contraseña actual incorrecta o usuario no encontrado"
}
```

---

## ?? Flujo Completo de Recuperación de Contraseña

### Escenario: Usuario olvidó su contraseña

1. **El usuario solicita recuperación:**
   ```
   POST /api/usuario/solicitar-recuperacion
   Body: { "email": "usuario@example.com" }
   ```

2. **Sistema envía email con token:**
   - Email enviado a `usuario@example.com`
   - Token: `A1B2C3D4E5F6...` (64 caracteres)
   - Expira en 1 hora

3. **Usuario copia el token del email**

4. **Usuario restablece su contraseña:**
   ```
   POST /api/usuario/restablecer-password
   Body: {
     "email": "usuario@example.com",
     "token": "A1B2C3D4E5F6...",
     "nuevaPassword": "NuevaPassword123!"
   }
   ```

5. **Sistema confirma cambio:**
   - Email de confirmación enviado
   - Token eliminado de la BD
   - Usuario puede hacer login con la nueva contraseña

6. **Usuario inicia sesión con nueva contraseña:**
   ```
   POST /api/usuario/signin
   Body: {
     "correo": "usuario@example.com",
     "password": "NuevaPassword123!"
   }
   ```

---

## ??? Características de Seguridad

? **Tokens seguros:** Generados con `RandomNumberGenerator` (32 bytes)
? **Expiración:** Los tokens expiran en 1 hora
? **Un solo uso:** El token se elimina después de usarse
? **Validación de email:** Se verifica que el email coincida con el token
? **Hashing BCrypt:** Las contraseñas se almacenan hasheadas
? **Protección contra enumeración:** Siempre devuelve OK en solicitud
? **Emails de confirmación:** El usuario recibe confirmación de cambios

---

## ?? Campos de Base de Datos (Tabla Usuario)

Los siguientes campos fueron agregados a la tabla `Usuario`:

```sql
token_recuperacion NVARCHAR(128) NULL,
expiracion_token DATETIME2 NULL
```

Estos campos se gestionan automáticamente:
- Se llenan al solicitar recuperación
- Se limpian después de usar el token
- Se limpian si el token expira

---

## ?? Troubleshooting

### ? No llega el email
- Verifica la configuración SMTP en `appsettings.json`
- Asegúrate de que `EnableSsl = true` para Gmail
- Si usas Gmail, necesitas una "App Password", no tu contraseña normal

### ? Token inválido o expirado
- El token expira en 1 hora desde que se generó
- Solicita un nuevo token con `/solicitar-recuperacion`
- Cada vez que solicitas un token, el anterior se invalida

### ? Error 401 Unauthorized
- En endpoints protegidos, verifica que incluyas el header `Authorization: Bearer {token}`
- El token JWT se obtiene del login (`/signin`)

---

## ?? Collection de Postman

Importa estos endpoints en Postman:

1. Crear nueva Collection: "Sistema SLA - Autenticación"
2. Agregar los 5 endpoints anteriores
3. Configurar Variables de Environment:
   - `base_url`: `http://localhost:5260`
   - `token`: (se guarda automáticamente del login)

**Script Post-Response para SignIn (guardar token automáticamente):**
```javascript
var response = pm.response.json();
if (response.token) {
    pm.environment.set("token", response.token);
}
```

---

## ?? Soporte

Para más información sobre la API, contacta al equipo de desarrollo.

**Versión:** 1.0  
**Última actualización:** 2024
