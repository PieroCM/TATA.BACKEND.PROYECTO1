# ?? Guía Rápida para Integración Frontend - Recuperación de Contraseña

## ?? Resumen de Endpoints

### Base URL
```
http://localhost:5260
```

---

## ?? 1. FLUJO DE RECUPERACIÓN DE CONTRASEÑA

### Paso 1: Usuario solicita recuperación
**Frontend muestra:** Formulario con campo de email

**Endpoint:** `POST /api/usuario/solicitar-recuperacion`

```typescript
// Ejemplo en TypeScript/JavaScript
async function solicitarRecuperacion(email: string) {
  const response = await fetch('http://localhost:5260/api/usuario/solicitar-recuperacion', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email })
  });
  
  if (response.ok) {
    // Mostrar mensaje: "Revisa tu correo electrónico"
    return await response.json();
  }
  
  throw new Error('Error al solicitar recuperación');
}
```

**Respuesta:**
```json
{
  "message": "Si el correo existe, recibirás un enlace de recuperación."
}
```

---

### Paso 2: Usuario recibe email con token
**Frontend muestra:** Mensaje indicando revisar el correo

**Email contiene:**
- Token de 64 caracteres hexadecimales
- Válido por 1 hora

---

### Paso 3: Usuario ingresa token y nueva contraseña
**Frontend muestra:** Formulario con 3 campos:
1. Email (readonly o pre-llenado)
2. Token (copiado del email)
3. Nueva contraseña

**Endpoint:** `POST /api/usuario/restablecer-password`

```typescript
async function restablecerPassword(email: string, token: string, nuevaPassword: string) {
  const response = await fetch('http://localhost:5260/api/usuario/restablecer-password', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      email,
      token,
      nuevaPassword
    })
  });
  
  if (response.ok) {
    // Redirigir a login
    return await response.json();
  }
  
  const error = await response.json();
  throw new Error(error.message);
}
```

**Respuesta Exitosa:**
```json
{
  "message": "Contraseña actualizada exitosamente."
}
```

**Respuesta Error:**
```json
{
  "message": "Token inválido o expirado. Solicita uno nuevo."
}
```

---

## ?? 2. FLUJO DE LOGIN

**Endpoint:** `POST /api/usuario/signin`

```typescript
async function login(correo: string, password: string) {
  const response = await fetch('http://localhost:5260/api/usuario/signin', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ correo, password })
  });
  
  if (response.ok) {
    const data = await response.json();
    // Guardar token en localStorage o sessionStorage
    localStorage.setItem('token', data.token);
    return data;
  }
  
  throw new Error('Credenciales inválidas');
}
```

**Respuesta:**
```json
{
  "message": "Inicio de sesión exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## ?? 3. FLUJO DE REGISTRO

**Endpoint:** `POST /api/usuario/signup`

```typescript
async function registrar(username: string, correo: string, password: string) {
  const response = await fetch('http://localhost:5260/api/usuario/signup', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      username,
      correo,
      password
    })
  });
  
  if (response.ok) {
    return await response.json();
  }
  
  const error = await response.json();
  throw new Error(error.message);
}
```

---

## ?? 4. CAMBIAR CONTRASEÑA (Usuario ya logueado)

**Endpoint:** `PUT /api/usuario/cambiar-password`

```typescript
async function cambiarPassword(correo: string, passwordActual: string, nuevaPassword: string) {
  const token = localStorage.getItem('token');
  
  const response = await fetch('http://localhost:5260/api/usuario/cambiar-password', {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      correo,
      passwordActual,
      nuevaPassword
    })
  });
  
  if (response.ok) {
    return await response.json();
  }
  
  throw new Error('Error al cambiar contraseña');
}
```

---

## ?? Componentes React Ejemplo

### Componente: SolicitarRecuperacion.tsx
```tsx
import React, { useState } from 'react';

export function SolicitarRecuperacion() {
  const [email, setEmail] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    
    try {
      const response = await fetch('http://localhost:5260/api/usuario/solicitar-recuperacion', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email })
      });
      
      const data = await response.json();
      setMensaje(data.message);
    } catch (error) {
      setMensaje('Error al enviar solicitud');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="recuperacion-form">
      <h2>Recuperar Contraseña</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Ingresa tu correo"
          required
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Enviando...' : 'Enviar'}
        </button>
      </form>
      {mensaje && <p className="mensaje">{mensaje}</p>}
    </div>
  );
}
```

### Componente: RestablecerPassword.tsx
```tsx
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export function RestablecerPassword() {
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [nuevaPassword, setNuevaPassword] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    
    try {
      const response = await fetch('http://localhost:5260/api/usuario/restablecer-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, token, nuevaPassword })
      });
      
      const data = await response.json();
      
      if (response.ok) {
        setMensaje(data.message);
        // Redirigir al login después de 2 segundos
        setTimeout(() => navigate('/login'), 2000);
      } else {
        setMensaje(data.message);
      }
    } catch (error) {
      setMensaje('Error al restablecer contraseña');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="restablecer-form">
      <h2>Restablecer Contraseña</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Tu correo"
          required
        />
        <input
          type="text"
          value={token}
          onChange={(e) => setToken(e.target.value)}
          placeholder="Token del email"
          required
        />
        <input
          type="password"
          value={nuevaPassword}
          onChange={(e) => setNuevaPassword(e.target.value)}
          placeholder="Nueva contraseña"
          required
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Actualizando...' : 'Restablecer'}
        </button>
      </form>
      {mensaje && <p className={mensaje.includes('exitosa') ? 'success' : 'error'}>{mensaje}</p>}
    </div>
  );
}
```

---

## ?? Componentes Angular Ejemplo

### Servicio: auth.service.ts
```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'http://localhost:5260/api/usuario';

  constructor(private http: HttpClient) {}

  solicitarRecuperacion(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/solicitar-recuperacion`, { email });
  }

  restablecerPassword(email: string, token: string, nuevaPassword: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/restablecer-password`, {
      email,
      token,
      nuevaPassword
    });
  }

  login(correo: string, password: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/signin`, { correo, password });
  }
}
```

---

## ? Validaciones Recomendadas en Frontend

### Para Email:
```typescript
function validarEmail(email: string): boolean {
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return regex.test(email);
}
```

### Para Contraseña:
```typescript
function validarPassword(password: string): boolean {
  // Mínimo 8 caracteres, al menos una letra mayúscula, una minúscula y un número
  const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;
  return regex.test(password);
}
```

### Para Token:
```typescript
function validarToken(token: string): boolean {
  // Token debe ser exactamente 64 caracteres hexadecimales
  const regex = /^[A-F0-9]{64}$/i;
  return regex.test(token);
}
```

---

## ?? Estados de UI Recomendados

### 1. Solicitar Recuperación
- ? Formulario inicial
- ? Enviando solicitud (loading)
- ? Email enviado (success)
- ? Error al enviar (error)

### 2. Restablecer Contraseña
- ? Formulario inicial
- ? Validando token (loading)
- ? Contraseña actualizada (success ? redirect)
- ? Token inválido/expirado (error)

### 3. Login
- ? Formulario inicial
- ? Iniciando sesión (loading)
- ? Login exitoso (success ? redirect)
- ? Credenciales incorrectas (error)

---

## ?? Mensajes de Usuario

### Español
```typescript
const MENSAJES = {
  solicitarRecuperacion: {
    success: '?? Revisa tu correo electrónico para continuar',
    error: '? Error al enviar la solicitud. Intenta nuevamente.'
  },
  restablecerPassword: {
    success: '? Contraseña actualizada. Redirigiendo al login...',
    tokenInvalido: '? Token inválido o expirado. Solicita uno nuevo.',
    error: '? Error al restablecer contraseña.'
  },
  login: {
    success: '? Bienvenido al sistema',
    error: '? Credenciales incorrectas'
  }
};
```

---

## ?? Variables de Entorno (Frontend)

### .env
```bash
VITE_API_BASE_URL=http://localhost:5260
# o
REACT_APP_API_BASE_URL=http://localhost:5260
# o
NEXT_PUBLIC_API_BASE_URL=http://localhost:5260
```

### Uso en código:
```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL; // Vite
// o
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL; // Create React App
// o
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL; // Next.js
```

---

## ?? Checklist de Integración

- [ ] Implementar formulario de solicitud de recuperación
- [ ] Implementar formulario de restablecimiento con token
- [ ] Implementar formulario de login
- [ ] Implementar formulario de registro
- [ ] Guardar token JWT en localStorage/sessionStorage
- [ ] Agregar validaciones de email y contraseña
- [ ] Implementar estados de loading
- [ ] Implementar mensajes de éxito/error
- [ ] Redirigir después de acciones exitosas
- [ ] Implementar logout (limpiar token)
- [ ] Agregar interceptor para adjuntar token en requests
- [ ] Manejar errores 401 (token expirado)

---

## ?? Listo para Usar

Todos los endpoints están funcionando y probados. El backend está completo y listo para integrarse con tu frontend.

**Puntos clave:**
- ? CORS configurado para permitir requests del frontend
- ? JWT implementado para autenticación
- ? Recuperación de contraseña con tokens seguros
- ? Emails HTML profesionales
- ? Validaciones de seguridad
- ? Endpoints públicos y protegidos correctamente

¡Empieza a integrar! ??
