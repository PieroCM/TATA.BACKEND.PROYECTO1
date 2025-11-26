# ?? Casos de Prueba - Sistema de Recuperación de Contraseña

## ?? Escenarios de Prueba

---

## 1?? SOLICITAR RECUPERACIÓN DE CONTRASEÑA

### ? Caso 1.1: Email válido y registrado
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": "usuario@example.com"
}
```

**Resultado Esperado:**
- Status: `200 OK`
- Response: `{ "message": "Si el correo existe, recibirás un enlace de recuperación." }`
- Email enviado al usuario
- Token guardado en BD con expiración en 1 hora

**Verificación:**
```sql
SELECT token_recuperacion, expiracion_token 
FROM Usuario 
WHERE Correo = 'usuario@example.com'
```

---

### ? Caso 1.2: Email válido pero NO registrado
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": "noexiste@example.com"
}
```

**Resultado Esperado:**
- Status: `200 OK` (por seguridad)
- Response: `{ "message": "Si el correo existe, recibirás un enlace de recuperación." }`
- NO se envía email
- Log de advertencia registrado

**Nota:** El sistema devuelve OK para evitar enumeración de usuarios.

---

### ? Caso 1.3: Email vacío
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": ""
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "El correo electrónico es obligatorio." }`

---

### ? Caso 1.4: Email con formato inválido
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": "emailinvalido"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: Error de validación

---

## 2?? RESTABLECER CONTRASEÑA

### ? Caso 2.1: Token válido y no expirado
**Preparación:**
1. Ejecutar solicitud de recuperación
2. Copiar token del email

**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": "A1B2C3D4E5F6789012345678901234567890ABCDEF1234567890ABCDEF1234",
  "nuevaPassword": "NuevaPassword123!"
}
```

**Resultado Esperado:**
- Status: `200 OK`
- Response: `{ "message": "Contraseña actualizada exitosamente." }`
- Contraseña actualizada en BD (hasheada)
- Token eliminado de BD
- Email de confirmación enviado

**Verificación:**
```sql
SELECT PasswordHash, token_recuperacion, expiracion_token 
FROM Usuario 
WHERE Correo = 'usuario@example.com'
-- token_recuperacion debe ser NULL
-- expiracion_token debe ser NULL
```

---

### ? Caso 2.2: Token inválido (no existe en BD)
**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": "TOKENINVALIDOQUENOEXISTE1234567890ABCDEF1234567890ABCDEF",
  "nuevaPassword": "NuevaPassword123!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Token inválido o expirado. Solicita uno nuevo." }`
- Log de advertencia registrado

---

### ? Caso 2.3: Token expirado (más de 1 hora)
**Preparación:**
1. Ejecutar solicitud de recuperación
2. Esperar 1 hora + 1 minuto
3. O modificar manualmente en BD: `UPDATE Usuario SET expiracion_token = DATEADD(hour, -2, GETUTCDATE())`

**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": "TOKEN_EXPIRADO",
  "nuevaPassword": "NuevaPassword123!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Token inválido o expirado. Solicita uno nuevo." }`

---

### ? Caso 2.4: Email no coincide con el token
**Preparación:**
1. Usuario A solicita recuperación
2. Se intenta usar su token con email de Usuario B

**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "otrousuario@example.com",
  "token": "TOKEN_DE_USUARIO_A",
  "nuevaPassword": "NuevaPassword123!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Token inválido o expirado. Solicita uno nuevo." }`
- Log de advertencia

---

### ? Caso 2.5: Campos faltantes
**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": ""
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Email, token y nueva contraseña son obligatorios." }`

---

### ? Caso 2.6: Reutilizar token ya usado
**Preparación:**
1. Ejecutar recuperación exitosa con un token
2. Intentar usar el mismo token nuevamente

**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": "TOKEN_YA_USADO",
  "nuevaPassword": "OtraPassword456!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Token inválido o expirado. Solicita uno nuevo." }`

---

## 3?? LOGIN CON NUEVA CONTRASEÑA

### ? Caso 3.1: Login con contraseña restablecida
**Preparación:**
1. Completar flujo de recuperación exitosamente

**Request:**
```json
POST /api/usuario/signin
{
  "correo": "usuario@example.com",
  "password": "NuevaPassword123!"
}
```

**Resultado Esperado:**
- Status: `200 OK`
- Response: `{ "message": "Inicio de sesión exitoso", "token": "JWT_TOKEN" }`
- Token JWT válido retornado

---

### ? Caso 3.2: Login con contraseña antigua (después de restablecer)
**Request:**
```json
POST /api/usuario/signin
{
  "correo": "usuario@example.com",
  "password": "PasswordAntigua123!"
}
```

**Resultado Esperado:**
- Status: `401 Unauthorized`
- Response: `{ "message": "Credenciales inválidas" }`

---

## 4?? SEGURIDAD Y EDGE CASES

### ? Caso 4.1: Múltiples solicitudes de recuperación (mismo usuario)
**Scenario:**
1. Usuario solicita recuperación (Token A)
2. Usuario solicita recuperación nuevamente antes de 1 hora (Token B)

**Resultado Esperado:**
- El Token A se invalida
- Solo el Token B es válido
- Solo el último token funciona

**Verificación:**
```sql
SELECT token_recuperacion, expiracion_token 
FROM Usuario 
WHERE Correo = 'usuario@example.com'
-- Solo debe haber UN token
```

---

### ? Caso 4.2: SQL Injection en email
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": "'; DROP TABLE Usuario; --"
}
```

**Resultado Esperado:**
- Status: `200 OK` (tratado como email inválido)
- Sin errores de BD
- Entity Framework previene SQL Injection

---

### ? Caso 4.3: XSS en campos
**Request:**
```json
POST /api/usuario/solicitar-recuperacion
{
  "email": "<script>alert('XSS')</script>@example.com"
}
```

**Resultado Esperado:**
- Status: `200 OK`
- Script sanitizado/escapado
- Sin ejecución de código malicioso

---

### ? Caso 4.4: Token muy largo
**Request:**
```json
POST /api/usuario/restablecer-password
{
  "email": "usuario@example.com",
  "token": "A" * 10000,
  "nuevaPassword": "Password123!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Sin overflow de BD

---

## 5?? CAMBIAR CONTRASEÑA (Usuario Logueado)

### ? Caso 5.1: Cambio exitoso con token JWT válido
**Request:**
```http
PUT /api/usuario/cambiar-password
Authorization: Bearer {JWT_VALIDO}
Content-Type: application/json

{
  "correo": "usuario@example.com",
  "passwordActual": "Password123!",
  "nuevaPassword": "NuevaPassword456!"
}
```

**Resultado Esperado:**
- Status: `200 OK`
- Response: `{ "message": "Contraseña actualizada correctamente" }`

---

### ? Caso 5.2: Sin token JWT
**Request:**
```json
PUT /api/usuario/cambiar-password
{
  "correo": "usuario@example.com",
  "passwordActual": "Password123!",
  "nuevaPassword": "NuevaPassword456!"
}
```

**Resultado Esperado:**
- Status: `401 Unauthorized`

---

### ? Caso 5.3: Contraseña actual incorrecta
**Request:**
```http
PUT /api/usuario/cambiar-password
Authorization: Bearer {JWT_VALIDO}

{
  "correo": "usuario@example.com",
  "passwordActual": "PasswordIncorrecto!",
  "nuevaPassword": "NuevaPassword456!"
}
```

**Resultado Esperado:**
- Status: `400 Bad Request`
- Response: `{ "message": "Contraseña actual incorrecta o usuario no encontrado" }`

---

## 6?? EMAILS

### ? Caso 6.1: Email de recuperación recibido
**Verificar:**
- [ ] Email recibido en bandeja de entrada
- [ ] Asunto correcto: "Recuperación de Contraseña - Sistema SLA"
- [ ] Token visible de 64 caracteres
- [ ] Diseño HTML correcto
- [ ] Mensaje de expiración (1 hora)
- [ ] Advertencia de seguridad visible

---

### ? Caso 6.2: Email de confirmación recibido
**Verificar:**
- [ ] Email recibido después de restablecer
- [ ] Asunto correcto: "Contraseña Actualizada - Sistema SLA"
- [ ] Diseño HTML correcto
- [ ] Icono de éxito (?) visible
- [ ] Mensaje de confirmación claro

---

### ? Caso 6.3: Error de SMTP (configuración incorrecta)
**Preparación:**
1. Modificar `SmtpSettings` con credenciales incorrectas

**Resultado Esperado:**
- Status: `200 OK` (para no revelar información)
- Email NO enviado
- Log de error registrado
- Usuario no sabe si el email existe

---

## ?? Matriz de Pruebas

| ID | Caso | Prioridad | Estado |
|----|------|-----------|--------|
| 1.1 | Email válido registrado | Alta | ? |
| 1.2 | Email válido no registrado | Media | ? |
| 1.3 | Email vacío | Media | ? |
| 1.4 | Email formato inválido | Baja | ? |
| 2.1 | Token válido | Alta | ? |
| 2.2 | Token inválido | Alta | ? |
| 2.3 | Token expirado | Alta | ? |
| 2.4 | Email no coincide | Media | ? |
| 2.5 | Campos faltantes | Media | ? |
| 2.6 | Reutilizar token | Alta | ? |
| 3.1 | Login nueva contraseña | Alta | ? |
| 3.2 | Login contraseña antigua | Alta | ? |
| 4.1 | Múltiples solicitudes | Media | ? |
| 4.2 | SQL Injection | Alta | ? |
| 4.3 | XSS | Media | ? |
| 5.1 | Cambio con JWT | Alta | ? |
| 5.2 | Cambio sin JWT | Alta | ? |
| 6.1 | Email recuperación | Alta | ? |
| 6.2 | Email confirmación | Media | ? |

---

## ?? Scripts SQL de Verificación

### Ver tokens activos
```sql
SELECT 
    IdUsuario,
    Username,
    Correo,
    token_recuperacion,
    expiracion_token,
    CASE 
        WHEN expiracion_token > GETUTCDATE() THEN 'VALIDO'
        ELSE 'EXPIRADO'
    END AS Estado
FROM Usuario
WHERE token_recuperacion IS NOT NULL
```

### Limpiar tokens expirados (manual)
```sql
UPDATE Usuario
SET token_recuperacion = NULL,
    expiracion_token = NULL
WHERE expiracion_token < GETUTCDATE()
```

### Ver últimos cambios de contraseña
```sql
SELECT 
    IdUsuario,
    Username,
    Correo,
    ActualizadoEn,
    DATEDIFF(minute, ActualizadoEn, GETUTCDATE()) AS MinutosDesdeActualizacion
FROM Usuario
WHERE ActualizadoEn IS NOT NULL
ORDER BY ActualizadoEn DESC
```

---

## ?? Script de Prueba Automatizada (Postman)

```javascript
// Test: Solicitar Recuperación
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains message", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('message');
});

// Test: Restablecer Password
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Password updated successfully", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.message).to.include("exitosamente");
});
```

---

## ? Checklist de Pruebas

Antes de pasar a producción, verifica:

- [ ] Todos los casos de prueba pasaron
- [ ] Emails se envían correctamente
- [ ] Tokens expiran después de 1 hora
- [ ] Tokens no pueden reutilizarse
- [ ] SQL Injection bloqueado
- [ ] XSS prevenido
- [ ] Logs registrando eventos importantes
- [ ] Errores no revelan información sensible
- [ ] Performance aceptable (< 2s por request)
- [ ] Collection de Postman funciona

---

## ?? Reporte de Bugs

Si encuentras un bug, reporta con:

1. **ID del caso de prueba**
2. **Request completo** (sin datos sensibles)
3. **Response recibida**
4. **Response esperada**
5. **Logs del servidor**
6. **Pasos para reproducir**

---

¡Listo para probar! ??
