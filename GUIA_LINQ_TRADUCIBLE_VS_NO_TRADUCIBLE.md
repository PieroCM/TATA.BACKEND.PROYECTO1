# ?? Entity Framework Core - Guía de Traducción LINQ a SQL

## ?? Operaciones Traducibles vs. No Traducibles

Esta guía te ayudará a escribir consultas LINQ que EF Core **SÍ pueda traducir a SQL**.

---

## ? OPERACIONES TRADUCIBLES (Puedes usar en el `Select`)

### 1. Concatenación de Strings

```csharp
// ? CORRECTO: Concatenación simple con +
.Select(u => new DTO
{
    NombreCompleto = u.Nombres + " " + u.Apellidos  // ? CONCAT(Nombres, ' ', Apellidos)
})

// ? CORRECTO: Con manejo de NULL
.Select(u => new DTO
{
    NombreCompleto = (u.Nombres ?? "") + " " + (u.Apellidos ?? "")  // ? COALESCE(Nombres, '') + ...
})

// ? INCORRECTO: Con .Trim() inline
.Select(u => new DTO
{
    NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim()  // ? Puede fallar
})
```

**SQL Generado (? Correcto):**
```sql
SELECT 
    COALESCE(u.nombres, '') + ' ' + COALESCE(u.apellidos, '') AS NombreCompleto
FROM usuario u
```

---

### 2. Operador Coalescencia NULL (`??`)

```csharp
// ? CORRECTO: Manejar NULL con ??
.Select(u => new DTO
{
    Email = u.CorreoCorporativo ?? "sin-correo@empresa.com",  // ? COALESCE(...)
    Estado = u.Estado ?? "INACTIVO"
})
```

**SQL Generado:**
```sql
SELECT 
    COALESCE(u.correo_corporativo, 'sin-correo@empresa.com') AS Email,
    COALESCE(u.estado, 'INACTIVO') AS Estado
FROM usuario u
```

---

### 3. Conversión de Mayúsculas/Minúsculas

```csharp
// ? CORRECTO: ToUpper() y ToLower()
.Select(u => new DTO
{
    UsuarioMayuscula = u.Username.ToUpper(),  // ? UPPER(username)
    CorreoMinuscula = u.Email.ToLower()       // ? LOWER(email)
})
```

**SQL Generado:**
```sql
SELECT 
    UPPER(u.username) AS UsuarioMayuscula,
    LOWER(u.email) AS CorreoMinuscula
FROM usuario u
```

---

### 4. Condicionales Ternarios (`? :`)

```csharp
// ? CORRECTO: Operador ternario simple
.Select(u => new DTO
{
    NombreRol = u.IdRolSistemaNavigation != null 
        ? u.IdRolSistemaNavigation.Nombre 
        : "Sin Rol",  // ? CASE WHEN ... THEN ... ELSE ... END
    
    EstadoTexto = u.Estado == "ACTIVO" ? "Activo" : "Inactivo"
})
```

**SQL Generado:**
```sql
SELECT 
    CASE 
        WHEN u.id_rol_sistema_navigation IS NOT NULL 
        THEN rs.nombre 
        ELSE 'Sin Rol' 
    END AS NombreRol,
    CASE 
        WHEN u.estado = 'ACTIVO' 
        THEN 'Activo' 
        ELSE 'Inactivo' 
    END AS EstadoTexto
FROM usuario u
```

---

### 5. Operaciones Matemáticas

```csharp
// ? CORRECTO: Operaciones aritméticas
.Select(s => new DTO
{
    TotalDias = (s.FechaFin - s.FechaInicio).Days,  // ? DATEDIFF(...)
    PorcentajeAvance = (s.TareasCompletadas * 100) / s.TotalTareas,
    CostoConImpuesto = s.Costo * 1.16m
})
```

---

### 6. Comparaciones y Operadores Lógicos

```csharp
// ? CORRECTO: Comparaciones booleanas
.Where(u => u.Estado == "ACTIVO" && 
           (u.IdRolSistema == 1 || u.IdRolSistema == 2) &&
           u.FechaCreacion >= DateTime.Now.AddMonths(-6))
```

**SQL Generado:**
```sql
WHERE 
    u.estado = 'ACTIVO' 
    AND (u.id_rol_sistema = 1 OR u.id_rol_sistema = 2)
    AND u.fecha_creacion >= DATEADD(MONTH, -6, GETDATE())
```

---

### 7. Funciones de Fecha

```csharp
// ? CORRECTO: Operaciones de fecha traducibles
.Select(s => new DTO
{
    Anio = s.FechaCreacion.Year,        // ? YEAR(fecha_creacion)
    Mes = s.FechaCreacion.Month,        // ? MONTH(fecha_creacion)
    Dia = s.FechaCreacion.Day,          // ? DAY(fecha_creacion)
    FechaCorta = s.FechaCreacion.Date   // ? CAST(fecha_creacion AS DATE)
})
```

---

### 8. Substring (Recortar Strings)

```csharp
// ? CORRECTO: Substring básico
.Select(u => new DTO
{
    InicialNombre = u.Nombres.Substring(0, 1),  // ? SUBSTRING(nombres, 0, 1)
    Codigo = u.Documento.Substring(0, 3)
})

// ?? CUIDADO: Verificar longitud primero para evitar errores
.Select(u => new DTO
{
    // Verificar que tenga al menos 1 carácter
    InicialNombre = u.Nombres.Length > 0 ? u.Nombres.Substring(0, 1) : ""
})
```

---

## ? OPERACIONES NO TRADUCIBLES (NO usar en `Select`)

### 1. `.Trim()`, `.TrimStart()`, `.TrimEnd()`

```csharp
// ? INCORRECTO: .Trim() dentro del Select
.Select(u => new DTO
{
    NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim()  // ? Error
})

// ? CORRECTO: .Trim() DESPUÉS del ToListAsync()
var usuarios = await _context.Usuario
    .Select(u => new DTO
    {
        NombreCompleto = u.Nombres + " " + u.Apellidos  // Sin .Trim()
    })
    .ToListAsync();

// Ahora SÍ puedes usar .Trim()
foreach (var usuario in usuarios)
{
    usuario.NombreCompleto = usuario.NombreCompleto?.Trim();
}
```

**¿Por qué falla?**
- `TRIM()` no existe en SQL Server < 2017
- EF Core puede no traducir `.Trim()` según el provider (SQL Server, PostgreSQL, MySQL)

---

### 2. `string.Format()`

```csharp
// ? INCORRECTO: string.Format() en Select
.Select(u => new DTO
{
    NombreCompleto = string.Format("{0} {1}", u.Nombres, u.Apellidos)  // ? Error
})

// ? CORRECTO: Concatenación simple
.Select(u => new DTO
{
    NombreCompleto = u.Nombres + " " + u.Apellidos  // ? OK
})
```

**¿Por qué falla?**
- `string.Format()` no tiene equivalente directo en SQL

---

### 3. Interpolación de Strings con Lógica Compleja

```csharp
// ? INCORRECTO: Interpolación con operaciones
.Select(u => new DTO
{
    Info = $"{u.Nombres.ToUpper()} - {u.Estado.ToLower()} ({u.IdUsuario})"  // ? Demasiado complejo
})

// ? CORRECTO: Traer datos primero, formatear después
var usuarios = await _context.Usuario
    .Select(u => new { u.Nombres, u.Estado, u.IdUsuario })
    .ToListAsync();

var dtos = usuarios.Select(u => new DTO
{
    Info = $"{u.Nombres.ToUpper()} - {u.Estado.ToLower()} ({u.IdUsuario})"  // ? En memoria
}).ToList();
```

---

### 4. Expresiones Regulares (Regex)

```csharp
// ? INCORRECTO: Regex en Select
.Select(u => new DTO
{
    EsEmailValido = System.Text.RegularExpressions.Regex.IsMatch(u.Email, @"^\S+@\S+$")  // ? Error
})

// ? CORRECTO: Validar DESPUÉS de traer datos
var usuarios = await _context.Usuario.ToListAsync();

foreach (var usuario in usuarios)
{
    usuario.EsEmailValido = System.Text.RegularExpressions.Regex.IsMatch(usuario.Email, @"^\S+@\S+$");
}
```

---

### 5. Métodos Personalizados (Custom)

```csharp
// ? INCORRECTO: Llamar a métodos propios en Select
.Select(u => new DTO
{
    NombreFormateado = FormatearNombre(u.Nombres, u.Apellidos)  // ? Error
})

// ? CORRECTO: Aplicar método DESPUÉS
var usuarios = await _context.Usuario
    .Select(u => new { u.Nombres, u.Apellidos })
    .ToListAsync();

var dtos = usuarios.Select(u => new DTO
{
    NombreFormateado = FormatearNombre(u.Nombres, u.Apellidos)  // ? En memoria
}).ToList();
```

---

### 6. `.ToString()` con Formato

```csharp
// ? INCORRECTO: .ToString() con formato personalizado
.Select(u => new DTO
{
    FechaFormateada = u.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")  // ? Error
})

// ? CORRECTO: Formatear DESPUÉS
var usuarios = await _context.Usuario
    .Select(u => new { u.FechaCreacion })
    .ToListAsync();

foreach (var usuario in usuarios)
{
    usuario.FechaFormateada = usuario.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss");
}
```

---

## ?? Patrón Recomendado: 3 Pasos

### Template Universal

```csharp
public async Task<List<MiDTO>> GetDataAsync()
{
    // =============================================
    // PASO 1: CONSULTA SQL (Solo operaciones básicas)
    // =============================================
    var datosRaw = await _context.MiEntidad
        .AsNoTracking()
        .Include(e => e.Relacion1)
        .Include(e => e.Relacion2)
        .Where(e => e.Estado == "ACTIVO")  // ? Filtros simples
        .Select(e => new MiDTO
        {
            // ? Solo concatenación básica con +
            Campo1 = (e.Parte1 ?? "") + " " + (e.Parte2 ?? ""),
            
            // ? Operador ternario simple
            Campo2 = e.Relacion1 != null ? e.Relacion1.Nombre : "N/A",
            
            // ? Operaciones matemáticas
            Campo3 = e.Valor * 1.16m,
            
            // ? Fechas
            Anio = e.FechaCreacion.Year,
            
            // ? Comparaciones booleanas
            EsActivo = e.Estado == "ACTIVO"
        })
        .ToListAsync();  // ??? EJECUTAR AQUÍ

    // =============================================
    // PASO 2: PROCESAMIENTO EN MEMORIA
    // =============================================
    foreach (var item in datosRaw)
    {
        // ? Ahora SÍ puedes usar TODO:
        
        // .Trim()
        item.Campo1 = item.Campo1?.Trim() ?? "Default";
        
        // string.Format()
        item.Campo4 = string.Format("#{0:D5} - {1}", item.Id, item.Campo1);
        
        // Interpolación compleja
        item.Campo5 = $"{item.Campo1.ToUpper()} ({item.Anio})";
        
        // .ToString() con formato
        item.FechaFormateada = item.FechaCreacion.ToString("dd/MM/yyyy");
        
        // Regex
        item.EsEmailValido = Regex.IsMatch(item.Email, @"^\S+@\S+$");
        
        // Métodos personalizados
        item.Campo6 = MiMetodoPersonalizado(item.Campo1, item.Campo2);
    }

    // =============================================
    // PASO 3: ORDENAR EN MEMORIA (Si es necesario)
    // =============================================
    var datosOrdenados = datosRaw
        .OrderBy(d => d.Campo1)
        .ThenByDescending(d => d.Anio)
        .ToList();

    return datosOrdenados;
}
```

---

## ?? Casos Especiales

### Caso 1: Ordenar por Campo Calculado

```csharp
// ? INCORRECTO: OrderBy en campo con lógica compleja
var usuarios = await _context.Usuario
    .Select(u => new DTO
    {
        NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim()  // ? Trim() falla
    })
    .OrderBy(u => u.NombreCompleto)  // ? No puede ordenar por campo con Trim()
    .ToListAsync();

// ? CORRECTO: Ordenar DESPUÉS
var usuarios = await _context.Usuario
    .Select(u => new DTO
    {
        NombreCompleto = u.Nombres + " " + u.Apellidos  // Sin Trim()
    })
    .ToListAsync();

// Limpiar y ordenar en memoria
foreach (var usuario in usuarios)
{
    usuario.NombreCompleto = usuario.NombreCompleto?.Trim();
}

var usuariosOrdenados = usuarios.OrderBy(u => u.NombreCompleto).ToList();
```

---

### Caso 2: Agrupar con Operaciones Complejas

```csharp
// ? INCORRECTO: GroupBy con Select complejo
var grupos = await _context.Solicitud
    .GroupBy(s => s.IdPersonal)
    .Select(g => new
    {
        IdPersonal = g.Key,
        Total = g.Count(),
        NombresFormateados = string.Join(", ", g.Select(s => s.Nombre.Trim()))  // ? Error
    })
    .ToListAsync();

// ? CORRECTO: GroupBy simple, procesar después
var grupos = await _context.Solicitud
    .GroupBy(s => s.IdPersonal)
    .Select(g => new
    {
        IdPersonal = g.Key,
        Total = g.Count(),
        Nombres = g.Select(s => s.Nombre).ToList()  // ? Sin Trim(), sin Join()
    })
    .ToListAsync();

// Procesar en memoria
var gruposFormateados = grupos.Select(g => new
{
    g.IdPersonal,
    g.Total,
    NombresFormateados = string.Join(", ", g.Nombres.Select(n => n?.Trim()))  // ? Ahora sí
}).ToList();
```

---

## ?? Tabla Resumen: Traducibilidad

| Operación | Traducible | SQL Generado | Notas |
|-----------|-----------|--------------|-------|
| `a + b` | ? Sí | `CONCAT(a, b)` o `a || b` | |
| `a ?? b` | ? Sí | `COALESCE(a, b)` | |
| `.ToUpper()` | ? Sí | `UPPER(...)` | |
| `.ToLower()` | ? Sí | `LOWER(...)` | |
| `.Substring(x, y)` | ? Sí | `SUBSTRING(..., x, y)` | |
| `.Length` | ? Sí | `LEN(...)` o `LENGTH(...)` | |
| `.Year`, `.Month`, `.Day` | ? Sí | `YEAR(...)`, `MONTH(...)`, `DAY(...)` | |
| `? :` (ternario) | ? Sí | `CASE WHEN ... THEN ... END` | |
| `.Trim()` | ?? Parcial | `TRIM(...)` (SQL Server 2017+) | Falla en versiones antiguas |
| `string.Format()` | ? No | N/A | Formatear en memoria |
| `$"..."` (interpolación compleja) | ? No | N/A | Usar concatenación simple |
| `.ToString("formato")` | ? No | N/A | Formatear en memoria |
| `Regex.IsMatch()` | ? No | N/A | Validar en memoria |
| Métodos personalizados | ? No | N/A | Aplicar en memoria |
| `.Contains()` | ? Sí | `... LIKE '%...%'` | |
| `.StartsWith()` | ? Sí | `... LIKE '...%'` | |
| `.EndsWith()` | ? Sí | `... LIKE '%...'` | |

---

## ?? Debugging: Ver el SQL Generado

### Método 1: Logs en Console

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Console Output:**
```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (12ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT COALESCE(u.nombres, '') + ' ' + COALESCE(u.apellidos, '') AS NombreCompleto
      FROM usuario u
      WHERE u.estado = 'ACTIVO'
```

---

### Método 2: `.ToQueryString()` (EF Core 5+)

```csharp
var query = _context.Usuario
    .Where(u => u.Estado == "ACTIVO")
    .Select(u => new { ... });

// Ver el SQL antes de ejecutar
var sql = query.ToQueryString();
Console.WriteLine(sql);

// También puedes loguearlo
_logger.LogDebug("SQL: {Sql}", sql);
```

---

## ? Checklist: ¿Es Traducible?

Antes de escribir tu consulta LINQ, pregúntate:

- [ ] ¿Estoy usando solo operaciones básicas en el `Select`? (`+`, `??`, `? :`)
- [ ] ¿Evito `.Trim()` dentro del `Select`?
- [ ] ¿Evito `string.Format()` dentro del `Select`?
- [ ] ¿Evito interpolación compleja `$"...{complejo}..."`?
- [ ] ¿Evito llamar a métodos personalizados dentro del `Select`?
- [ ] ¿Traigo los datos con `.ToListAsync()` antes de aplicar operaciones complejas?
- [ ] ¿Ordeno DESPUÉS si necesito ordenar por campos calculados complejos?

---

## ?? Conclusión

**Regla de Oro:**

> **Si no estás seguro de que EF Core pueda traducirlo a SQL, tráelo primero con `.ToListAsync()` y procésalo en memoria.**

**Flujo correcto:**

```
SQL (Simple) ? .ToListAsync() ? Memoria (Complejo) ? Ordenar/Filtrar ? Return
```

**¡Recuerda!**

- ? SQL es rápido pero limitado
- ? Memoria es lento pero flexible
- ? Usa SQL para **filtrar y obtener solo lo necesario**
- ? Usa memoria para **formatear y calcular campos complejos**

---

**?? FIN DE LA GUÍA**
