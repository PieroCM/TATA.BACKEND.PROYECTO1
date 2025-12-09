using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolPermisoController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RolPermisoController));
        
        private readonly IRolPermisoService _service;
        private readonly ILogService _logService;

        public RolPermisoController(IRolPermisoService service, ILogService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("RolPermisoController inicializado.");
        }

        // GET: api/rolpermiso
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAll iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll RolPermiso", 
                "Obteniendo todos los RolPermiso", userId);

            try
            {
                var result = await _service.GetAllAsync();
                
                log.Info($"GetAll completado correctamente, {result.Count()} registros obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll RolPermiso", 
                    $"Total RolPermiso obtenidos: {result.Count()}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/rolpermiso/nombres
        [HttpGet("nombres")]
        public async Task<IActionResult> GetAllWithNames()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAllWithNames iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAllWithNames RolPermiso", 
                "Obteniendo todos los RolPermiso con nombres", userId);

            try
            {
                var result = await _service.GetAllWithNamesAsync();
                
                log.Info($"GetAllWithNames completado correctamente, {result.Count()} registros obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAllWithNames RolPermiso", 
                    $"Total RolPermiso obtenidos: {result.Count()}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAllWithNames", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAllWithNames RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpGet("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> GetByIds(int idRolSistema, int idPermiso)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetByIds iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetByIds RolPermiso", 
                $"Buscando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);

            try
            {
                var result = await _service.GetByIdsAsync(idRolSistema, idPermiso);
                
                if (result == null)
                {
                    log.Warn($"RolPermiso no encontrado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}");
                    await _logService.RegistrarLogAsync("WARN", "RolPermiso no encontrado", 
                        $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);
                    return NotFound();
                }

                log.Info("GetByIds completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetByIds RolPermiso", 
                    "RolPermiso obtenido exitosamente", userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetByIds", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetByIds RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/rolpermiso
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolPermisoEntity entity)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Create iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create RolPermiso", 
                $"Creando RolPermiso con idRolSistema: {entity?.IdRolSistema}, idPermiso: {entity?.IdPermiso}", userId);

            if (entity == null)
            {
                log.Warn("Create recibió entity nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: entity nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Create: Validación de ModelState fallida");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _service.AddAsync(entity);
                
                if (!created)
                {
                    log.Warn("No se pudo crear el RolPermiso");
                    await _logService.RegistrarLogAsync("WARN", "No se pudo crear el registro", 
                        "El servicio retornó false", userId);
                    return BadRequest("No se pudo crear el registro");
                }

                log.Info("Create completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create RolPermiso", 
                    "RolPermiso creado exitosamente", userId);

                return Ok("Registro creado correctamente");
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPut("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Update(int idRolSistema, int idPermiso, [FromBody] RolPermisoEntity entity)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Update iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update RolPermiso", 
                $"Actualizando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);

            if (entity == null)
            {
                log.Warn("Update recibió entity nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: entity nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Update: Validación de ModelState fallida");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
                return BadRequest(ModelState);
            }

            try
            {
                var updated = await _service.UpdateAsync(idRolSistema, idPermiso, entity);

                if (!updated)
                {
                    var exists = await _service.GetByIdsAsync(idRolSistema, idPermiso);
                    if (exists == null)
                    {
                        log.Warn("RolPermiso original no encontrado");
                        await _logService.RegistrarLogAsync("WARN", "RolPermiso no encontrado", 
                            $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);
                        return NotFound("❌ No se encontró el registro original a actualizar.");
                    }

                    var duplicate = await _service.GetByIdsAsync(entity.IdRolSistema, entity.IdPermiso);
                    if (duplicate != null)
                    {
                        log.Warn("Combinación duplicada al actualizar RolPermiso");
                        await _logService.RegistrarLogAsync("WARN", "Combinación duplicada", 
                            "Ya existe un permiso con esta combinación", userId);
                        return BadRequest("❌ No se puede actualizar. Ya existe un permiso asignado con esta combinación.");
                    }
                }

                log.Info("Update completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update RolPermiso", 
                    "RolPermiso actualizado exitosamente", userId);

                return Ok("✔ Registro actualizado correctamente.");
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Update", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpDelete("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Delete(int idRolSistema, int idPermiso)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete RolPermiso", 
                $"Eliminando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);

            try
            {
                var deleted = await _service.RemoveAsync(idRolSistema, idPermiso);
                
                if (!deleted)
                {
                    log.Warn("RolPermiso no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", "RolPermiso no encontrado para eliminar", 
                        $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}", userId);
                    return NotFound("No se encontró el registro");
                }

                log.Info("Delete completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete RolPermiso", 
                    "RolPermiso eliminado exitosamente", userId);

                return Ok("Registro eliminado correctamente");
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Delete", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete RolPermiso", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
