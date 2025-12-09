using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolRegistroController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RolRegistroController));
        
        private readonly IRolRegistroService _service;
        private readonly ILogService _logService;

        public RolRegistroController(IRolRegistroService service, ILogService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("RolRegistroController inicializado.");
        }

        // GET: api/rolregistro?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolRegistroDTO>>> Get([FromQuery] bool soloActivos = true)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Get iniciado con soloActivos: {soloActivos}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll RolRegistro", 
                $"Obteniendo RolRegistro con soloActivos: {soloActivos}", userId);

            try
            {
                var list = await _service.GetAllAsync(soloActivos);
                
                log.Info($"Get completado correctamente, {list.Count()} registros obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll RolRegistro", 
                    $"Total RolRegistro obtenidos: {list.Count()}", userId);
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Get", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll RolRegistro", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/rolregistro/5
        [HttpGet("{id:int}", Name = "GetRolRegistroById")]
        public async Task<ActionResult<RolRegistroDTO>> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetById iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetById RolRegistro", 
                $"Buscando RolRegistro con id: {id}", userId);

            try
            {
                var item = await _service.GetByIdAsync(id);
                
                if (item is null)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado");
                    await _logService.RegistrarLogAsync("WARN", $"RolRegistro no encontrado: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetById RolRegistro", 
                    $"RolRegistro {id} obtenido exitosamente", userId);

                return Ok(item);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetById RolRegistro", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/rolregistro
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] RolRegistroCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Post iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create RolRegistro", 
                $"Creando RolRegistro: {dto?.NombreRol}", userId);

            if (dto == null)
            {
                log.Warn("Post recibió dto nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: dto nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Post: Validación de ModelState fallida");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
                return BadRequest(ModelState);
            }

            try
            {
                var id = await _service.CreateAsync(null, dto);
                
                log.Info($"Post completado correctamente, IdRolRegistro: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create RolRegistro", 
                    $"RolRegistro creado con id: {id}", userId);

                return CreatedAtRoute("GetRolRegistroById", new { id }, id);
            }
            catch (DuplicateNameException ex)
            {
                log.Warn($"NombreRol duplicado: {ex.Message}");
                await _logService.RegistrarLogAsync("WARN", "Conflicto: NombreRol ya existe", 
                    ex.ToString(), userId);
                return Conflict(new { message = "NombreRol ya existe" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Post", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create RolRegistro", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // PUT: api/rolregistro/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] RolRegistroUpdateDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Put iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update RolRegistro", 
                $"Actualizando RolRegistro con id: {id}", userId);

            if (dto is null)
            {
                log.Warn($"Put recibió dto nulo para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: dto nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Put: Validación de ModelState fallida para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
                return BadRequest(ModelState);
            }

            try
            {
                var ok = await _service.UpdateAsync(id, dto);
                
                if (!ok)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado para actualizar");
                    await _logService.RegistrarLogAsync("WARN", $"RolRegistro no encontrado para actualizar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Put completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update RolRegistro", 
                    $"RolRegistro {id} actualizado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Put para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update RolRegistro", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/rolregistro/5 (físico)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete RolRegistro", 
                $"Eliminando RolRegistro con id: {id}", userId);

            try
            {
                var ok = await _service.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", $"RolRegistro no encontrado para eliminar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete RolRegistro", 
                    $"RolRegistro {id} eliminado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete RolRegistro", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
