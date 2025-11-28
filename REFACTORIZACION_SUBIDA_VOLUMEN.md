# Refactorización del Módulo de Carga Masiva de Solicitudes SLA

## Resumen de Cambios

Se ha refactorizado completamente el módulo de carga masiva de solicitudes SLA (`SubidaVolumenServices`) para simplificar su arquitectura y eliminar dependencias innecesarias.

## Cambios Realizados

### 1. Interfaz ISubidaVolumenServices

**Archivo:** `TATA.BACKEND.PROYECTO1.CORE/Core/Interfaces/ISubidaVolumenServices.cs`

**Cambio:**
- Se actualizó la firma del método `ProcesarSolicitudesAsync` para recibir el ID del usuario creador como parámetro:

```csharp
Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
    IEnumerable<SubidaVolumenSolicitudRowDto> filas,
    int idUsuarioCreador);
```

**Antes:**
```csharp
Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
    IEnumerable<SubidaVolumenSolicitudRowDto> filas);
```

### 2. Implementación SubidaVolumenServices

**Archivo:** `TATA.BACKEND.PROYECTO1.CORE/Core/Services/SubidaVolumenServices.cs`

#### 2.1 Dependencias Eliminadas

Se eliminaron las siguientes dependencias del constructor:
- ❌ `IRolesSistemaRepository` 
- ❌ `IUsuarioRepository`

**Dependencias actuales:**
- ✅ `IPersonalRepository`
- ✅ `IConfigSLARepository`
- ✅ `IRolRegistroRepository`
- ✅ `ISolicitudRepository`
- ✅ `ILogService`

#### 2.2 Firma del Método Principal

Se actualizó para recibir el `idUsuarioCreador`:

```csharp
public async Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
    IEnumerable<SubidaVolumenSolicitudRowDto> filas,
    int idUsuarioCreador)
```

#### 2.3 Lógica de Usuario Creador

**ANTES:** El servicio buscaba un "superadmin" o el primer usuario activo en la base de datos:
```csharp
var usuariosList = (await _usuarioRepository.GetAllAsync()).ToList();
var usuarioCreador =
    usuariosList.FirstOrDefault(u => u.Username == "superadmin") ??
    usuariosList.FirstOrDefault(u => u.Estado == "ACTIVO") ??
    usuariosList.FirstOrDefault();
```

**AHORA:** El servicio usa directamente el parámetro recibido:
```csharp
// Se usa directamente: idUsuarioCreador (parámetro del método)
```

✅ **Beneficio:** Elimina una consulta a la base de datos y delega la responsabilidad al llamador.

#### 2.4 Uso del Repositorio de Solicitudes

**ANTES:**
```csharp
await _solicitudRepository.AddAsync(solicitud);
```

**AHORA:**
```csharp
await _solicitudRepository.CreateSolicitudAsync(solicitud);
```

✅ **Beneficio:** Usa el método correcto del repositorio que valida claves foráneas y realiza `SaveChangesAsync` automáticamente.

#### 2.5 Método CrearSolicitud

El método privado ya recibía el parámetro `creadoPor`, ahora se usa correctamente:

```csharp
var solicitud = CrearSolicitud(
    row,
    personal.IdPersonal,
    configSla,
    rolRegistro.IdRolRegistro,
    idUsuarioCreador,  // ✅ Usa el parámetro del método público
    fechaSolicitud,
    fechaIngreso,
    hoyPeru);
```

### 3. Controlador SubidaVolumenController

**Archivo:** `TATA.BACKEND.PROYECTO1.API/Controllers/SubidaVolumenController.cs`

#### 3.1 Endpoint Actualizado

Se agregó el parámetro `idUsuarioCreador` al endpoint:

```csharp
[HttpPost("solicitudes")]
public async Task<ActionResult<BulkUploadResultDto>> CargarSolicitudes(
    [FromBody] IEnumerable<SubidaVolumenSolicitudRowDto>? filas,
    [FromQuery] int idUsuarioCreador = 1)
```

**Cómo usar:**
- Por defecto usa `idUsuarioCreador = 1` (superadmin)
- Se puede pasar como query parameter: `POST /api/SubidaVolumen/solicitudes?idUsuarioCreador=5`
- **Recomendación:** En producción, obtenerlo del contexto de autenticación JWT

#### 3.2 Llamada al Servicio

**ANTES:**
```csharp
var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista);
```

**AHORA:**
```csharp
var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista, idUsuarioCreador);
```

#### 3.3 Logging Mejorado

Todos los logs ahora incluyen el `idUsuarioCreador`:

```csharp
await _logService.AddAsync(new LogSistemaCreateDTO
{
    Nivel = "INFO",
    Mensaje = "Petición CargarSolicitudes recibida",
    Detalles = $"Petición recibida desde FRONTEND. Usuario creador: {idUsuarioCreador}",
    IdUsuario = idUsuarioCreador
});
```

#### 3.4 Mensajes de Advertencia Eliminados

Se eliminaron los comentarios que indicaban que el controlador estaba deshabilitado.

## Lógica que NO se Modificó

✅ **Se mantiene intacta:**
- `ValidarCamposObligatorios`
- `ValidarFechas`
- `AsegurarPersonal` (solo crea Personal, NO Usuario)
- `AsegurarConfigSla`
- `AsegurarRolRegistro`
- `CrearSolicitud` (lógica de cálculo de SLA)
- `RegistrarError`
- Uso de la zona horaria de Perú: `"SA Pacific Standard Time"`
- Logs existentes (`log.Info`, `log.Debug`, `log.Error`)

## DTOs - Sin Cambios

No se modificaron las estructuras de datos:
- ✅ `SubidaVolumenSolicitudRowDto`
- ✅ `BulkUploadResultDto`
- ✅ `BulkUploadErrorDto`

## Repositorios - Sin Cambios

No se modificaron los repositorios:
- ✅ `ISolicitudRepository` - Sigue teniendo el método `CreateSolicitudAsync`
- ✅ `SolicitudRepository` - Implementación sin cambios
- ✅ No se agregaron métodos nuevos tipo `AddAsync`

## Compilación

✅ **Estado:** La solución compila correctamente sin errores.

**Advertencias existentes:** 9 advertencias (pre-existentes, no relacionadas con esta refactorización)

```bash
Compilación correcto con 9 advertencias en 7.6s
```

## Ejemplo de Uso

### Desde Postman/Frontend:

**Endpoint:**
```
POST /api/SubidaVolumen/solicitudes?idUsuarioCreador=5
```

**Body (JSON):**
```json
[
  {
    "personal_nombres": "Juan",
    "personal_apellidos": "Pérez",
    "personal_documento": "12345678",
    "personal_correo": "juan.perez@empresa.com",
    "config_sla_codigo": "SLA001",
    "config_sla_descripcion": "SLA Alta",
    "config_sla_dias_umbral": 5,
    "config_sla_tipo_solicitud": "ALTA",
    "rol_registro_nombre": "DEVELOPER",
    "rol_registro_bloque_tech": "TECH",
    "sol_fecha_solicitud": "2024-01-15",
    "sol_fecha_ingreso": "2024-01-18"
  }
]
```

### Desde código C#:

```csharp
// En un controlador con autenticación JWT
var idUsuarioActual = GetUserIdFromClaims(); // Obtener del token JWT
var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(filas, idUsuarioActual);
```

## Mejoras Futuras Recomendadas

1. **Autenticación:** Integrar con JWT para obtener automáticamente el ID del usuario autenticado
2. **Validación:** Agregar validación de que el `idUsuarioCreador` exista en la base de datos antes de procesar
3. **Autorización:** Verificar que el usuario tenga permisos para realizar carga masiva
4. **Transacciones:** Considerar usar transacciones para rollback completo en caso de errores críticos

## Verificación de Funcionamiento

Para verificar que la refactorización funciona correctamente:

1. ✅ La solución compila sin errores
2. ✅ No se modificaron otras implementaciones (SolicitudController, PersonalController, etc.)
3. ✅ El servicio ya no depende de `IUsuarioRepository`
4. ✅ Se usa `CreateSolicitudAsync` en lugar de `AddAsync`
5. ✅ El `idUsuarioCreador` se pasa correctamente desde el controlador al servicio

---

**Fecha de refactorización:** 2024-11-28  
**Archivos modificados:** 3
- `ISubidaVolumenServices.cs`
- `SubidaVolumenServices.cs`
- `SubidaVolumenController.cs`
