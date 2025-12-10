# ?? Actualización del Endpoint `/api/email/administradores-analistas`

## ?? Cambios Implementados

Se ha agregado **1 campo adicional** al endpoint para incluir información del **Personal** asociado al usuario:

### ? Nuevo Campo en `UsuarioEmailDto`

```csharp
public class UsuarioEmailDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; }
    public string? CorreoCorporativo { get; set; }
    public int IdRolSistema { get; set; }
    public string NombreRol { get; set; }
    public string? NombreCompleto { get; set; }  // Nombre del personal (Nombres + Apellidos)
    public bool TieneCorreo { get; set; }
    
    // ? NUEVO: ID del Personal asociado
    public int? IdPersonal { get; set; }
}
```

---

## ?? Respuesta del Endpoint

### Endpoint
```
GET /api/email/administradores-analistas
```

### Respuesta Anterior (ANTES)
```json
{
  "success": true,
  "total": 3,
  "usuarios": [
    {
      "idUsuario": 1,
      "username": "admin",
      "correoCorporativo": "admin@empresa.com",
      "idRolSistema": 1,
      "nombreRol": "Administrador",
      "nombreCompleto": "Juan Pérez García",
      "tieneCorreo": true
    }
  ],
  "mensaje": "Se encontraron 3 administradores y analistas con correo corporativo"
}
```

### Respuesta Actualizada (AHORA)
```json
{
  "success": true,
  "total": 1,
  "usuarios": [
    {
      "idUsuario": 1,
      "username": "superadmin@sla.local",
      "correoCorporativo": "22200150@ue.edu.pe",
      "idRolSistema": 1,
      "nombreRol": "Super Administrador",
      "nombreCompleto": "Jose Cabrera",
      "tieneCorreo": true,
      "idPersonal": 1
    }
  ],
  "mensaje": "Se encontraron 1 administradores y analistas con correo corporativo"
}
```

---

## ?? Detalle de Campos

| Campo | Tipo | Descripción | Ejemplo |
|-------|------|-------------|---------|
| `idUsuario` | `int` | ID del usuario en la tabla `usuario` | `1` |
| `username` | `string` | Nombre de usuario para login | `"admin"` |
| `correoCorporativo` | `string?` | Email corporativo desde `personal.correo_corporativo` | `"admin@empresa.com"` |
| `idRolSistema` | `int` | ID del rol (1=Admin, 2=Analista) | `1` |
| `nombreRol` | `string` | Nombre del rol desde `rol_sistema.nombre` | `"Administrador"` |
| `nombreCompleto` | `string?` | Nombre completo del personal (Nombres + Apellidos) | `"Juan Pérez García"` |
| `tieneCorreo` | `bool` | Indica si tiene correo corporativo | `true` |
| **`idPersonal`** ? | `int?` | **NUEVO:** ID del registro en la tabla `personal` | `5` |

---

## ?? Uso en el Frontend

### Ejemplo: Mostrar información detallada del usuario

```typescript
interface UsuarioEmailDto {
  idUsuario: number;
  username: string;
  correoCorporativo: string;
  idRolSistema: number;
  nombreRol: string;
  nombreCompleto: string;
  tieneCorreo: boolean;
  idPersonal?: number;  // ? NUEVO
}

// Obtener administradores y analistas
const response = await fetch('/api/email/administradores-analistas');
const data = await response.json();

// Mostrar en tabla o lista
data.usuarios.forEach((usuario: UsuarioEmailDto) => {
  console.log(`ID Personal: ${usuario.idPersonal}`);
  console.log(`Nombre: ${usuario.nombreCompleto}`);
  console.log(`Usuario: ${usuario.username} (${usuario.nombreRol})`);
  console.log(`Email: ${usuario.correoCorporativo}`);
  console.log('---');
});
```

### Ejemplo: Crear tarjetas de usuarios con avatar

```typescript
function UserCard({ usuario }: { usuario: UsuarioEmailDto }) {
  return (
    <div className="user-card">
      <div className="avatar">
        {/* Usar idPersonal para buscar foto de perfil */}
        <img src={`/api/personal/${usuario.idPersonal}/foto`} 
             alt={usuario.nombreCompleto} />
      </div>
      <div className="info">
        <h3>{usuario.nombreCompleto}</h3>
        <p className="role">{usuario.nombreRol}</p>
        <p className="email">{usuario.correoCorporativo}</p>
        <p className="username">@{usuario.username}</p>
      </div>
    </div>
  );
}
```

### Ejemplo: Filtrar por ID de Personal

```typescript
// Buscar usuario por IdPersonal
const usuarioPorPersonal = data.usuarios.find(
  (u: UsuarioEmailDto) => u.idPersonal === 1
);

if (usuarioPorPersonal) {
  console.log(`Usuario encontrado: ${usuarioPorPersonal.nombreCompleto}`);
}
```

---

## ?? Notas Importantes

### 1. ¿Por qué solo IdPersonal?

El campo `nombreCompleto` ya contiene el nombre del personal asociado (Nombres + Apellidos), por lo que no es necesario un campo adicional `nombrePersonal` que sería redundante.

### 2. ¿Por qué `idPersonal` puede ser `null`?

Aunque en la consulta se filtra por `u.PersonalNavigation != null`, el campo se marca como **nullable (`int?`)** por seguridad y para evitar errores si en el futuro hay usuarios sin personal asociado.

### 3. ¿Cómo se relacionan las tablas?

```
????????????         ????????????
? usuario  ?         ? personal ?
????????????         ????????????
? id (PK)  ?         ? id (PK)  ?
? username ?         ? nombres  ?
? id_personal ????????? apellidos?
? id_rol   ?         ? correo   ?
? estado   ?         ? estado   ?
????????????         ????????????
       ?
       ?
       ?
????????????????
? rol_sistema  ?
????????????????
? id (PK)      ?
? nombre       ?
????????????????
```

**Relación:**
- `usuario.id_personal` ? `personal.id` (FK)
- `usuario.id_rol_sistema` ? `rol_sistema.id` (FK)

---

## ?? Pruebas

### 1. Verificar que el endpoint retorna el nuevo campo

```bash
# Usando curl
curl -X GET "https://localhost:7xxx/api/email/administradores-analistas" \
  -H "accept: application/json"
```

**Verificar en la respuesta:**
- ? Campo `idPersonal` presente
- ? Valor no vacío ni `null` (si hay datos)
- ? Sin duplicación de nombre

### 2. Verificar en Swagger

1. Ir a `/swagger`
2. Buscar el endpoint `GET /api/email/administradores-analistas`
3. Hacer clic en "Try it out" ? "Execute"
4. Verificar que la respuesta incluya el campo `idPersonal`

### 3. Verificar en SQL

```sql
-- Ver usuarios con su personal asociado
SELECT 
    u.id_usuario,
    u.username,
    u.id_personal,
    p.nombres,
    p.apellidos,
    CONCAT(p.nombres, ' ', p.apellidos) AS nombre_completo,
    p.correo_corporativo,
    rs.nombre AS nombre_rol
FROM usuario u
INNER JOIN personal p ON u.id_personal = p.id_personal
INNER JOIN rol_sistema rs ON u.id_rol_sistema = rs.id_rol_sistema
WHERE u.estado = 'ACTIVO'
  AND u.id_rol_sistema IN (1, 2)  -- Admin o Analista
  AND p.correo_corporativo IS NOT NULL
ORDER BY rs.nombre, p.nombres;
```

---

## ?? Ejemplo de Respuesta Real

```json
{
  "success": true,
  "total": 1,
  "usuarios": [
    {
      "idUsuario": 1,
      "username": "superadmin@sla.local",
      "correoCorporativo": "22200150@ue.edu.pe",
      "idRolSistema": 1,
      "nombreRol": "Super Administrador",
      "nombreCompleto": "Jose Cabrera",
      "tieneCorreo": true,
      "idPersonal": 1
    }
  ],
  "mensaje": "Se encontraron 1 administradores y analistas con correo corporativo"
}
```

---

## ?? Casos de Uso

### 1. Mostrar Avatar del Personal

```typescript
// Obtener URL de la foto del personal
const avatarUrl = `/api/personal/${usuario.idPersonal}/foto`;
```

### 2. Enlace al Perfil del Personal

```typescript
// Crear enlace al perfil completo
const perfilUrl = `/personal/${usuario.idPersonal}`;
```

### 3. Validar Relación Usuario-Personal

```typescript
// Verificar que el usuario tiene personal asociado
if (usuario.idPersonal) {
  console.log(`Usuario vinculado al personal ID: ${usuario.idPersonal}`);
} else {
  console.warn(`Usuario ${usuario.username} no tiene personal asociado`);
}
```

### 4. Búsqueda Cruzada

```typescript
// Buscar por username y obtener idPersonal
const idPersonal = data.usuarios
  .find((u: UsuarioEmailDto) => u.username === "admin")
  ?.idPersonal;

console.log(`ID Personal del admin: ${idPersonal}`);
```

---

## ? Resumen de Cambios

| Archivo Modificado | Cambio |
|--------------------|--------|
| `EmailConfigDTO.cs` | ? Agregado `IdPersonal` al DTO (sin duplicación) |
| `EmailAutomationService.cs` | ? Actualizada consulta SQL para incluir `IdPersonal` |

---

## ?? Estado

- ? **Compilación exitosa**
- ? **Campo agregado sin redundancia**
- ? **Servicio optimizado**
- ? **Sin errores de traducción LINQ**
- ? **Listo para uso en producción**

---

**? OPTIMIZACIÓN COMPLETADA**
