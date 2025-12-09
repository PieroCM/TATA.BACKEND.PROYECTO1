using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesSistemaController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RolesSistemaController));
        
        private readonly IRolesSistemaService _service;
        private readonly ILogService _logService;

        public RolesSistemaController(IRolesSistemaService service, ILogService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("RolesSistemaController inicializado.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAll iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll RolesSistema", 
                "Obteniendo todos los roles del sistema", userId);

            try
            {
                var list = await _service.GetAll();
                
                log.Info($"GetAll completado correctamente, {list.Count} roles obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll RolesSistema", 
                    $"Total roles obtenidos: {list.Count}", userId);
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll RolesSistema", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetById iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetById RolesSistema", 
                $"Buscando rol con id: {id}", userId);

            try
            {
                var item = await _service.GetById(id);
                
                if (item == null)
                {
                    log.Warn($"RolesSistema con id {id} no encontrado");
                    await _logService.RegistrarLogAsync("WARN", $"RolesSistema no encontrado: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetById RolesSistema", 
                    $"Rol {id} obtenido exitosamente", userId);

                return Ok(item);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetById RolesSistema", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolesSistemaCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Create iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create RolesSistema", 
                $"Creando rol con código: {dto?.Codigo}", userId);

            if (dto == null)
            {
                log.Warn("Create recibió dto nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: dto nulo", 
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
                var created = await _service.Create(dto);
                
                log.Info($"Create completado correctamente, IdRolSistema: {created.IdRolSistema}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create RolesSistema", 
                    $"Rol creado con id: {created.IdRolSistema}", userId);

                return CreatedAtAction(nameof(GetById), new { id = created.IdRolSistema }, created);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create RolesSistema", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RolesSistemaUpdateDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Update iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update RolesSistema", 
                $"Actualizando rol con id: {id}", userId);

            if (dto == null)
            {
                log.Warn($"Update recibió dto nulo para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: dto nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Update: Validación de ModelState fallida para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
                return BadRequest(ModelState);
            }

            try
            {
                var ok = await _service.Update(id, dto);
                
                if (!ok)
                {
                    log.Warn($"RolesSistema con id {id} no encontrado para actualizar");
                    await _logService.RegistrarLogAsync("WARN", $"RolesSistema no encontrado para actualizar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update RolesSistema", 
                    $"Rol {id} actualizado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update RolesSistema", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete RolesSistema", 
                $"Eliminando rol con id: {id}", userId);

            try
            {
                var ok = await _service.Delete(id);
                
                if (!ok)
                {
                    log.Warn($"RolesSistema con id {id} no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", $"RolesSistema no encontrado para eliminar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete RolesSistema", 
                    $"Rol {id} eliminado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete RolesSistema", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
