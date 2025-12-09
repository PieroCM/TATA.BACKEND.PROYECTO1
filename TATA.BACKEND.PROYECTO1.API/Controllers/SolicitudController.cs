using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SolicitudController));
        
        private readonly ISolicitudService _solicitudService;
        private readonly ILogService _logService;

        public SolicitudController(ISolicitudService solicitudService, ILogService logService)
        {
            _solicitudService = solicitudService;
            _logService = logService;
            log.Debug("SolicitudController inicializado.");
        }

        // GET: api/solicitud GOD
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAll iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll Solicitudes", 
                "Obteniendo todas las solicitudes", userId);

            try
            {
                var data = await _solicitudService.GetAllAsync();
                
                log.Info($"GetAll completado correctamente, {data.Count} solicitudes obtenidas");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll Solicitudes", 
                    $"Total solicitudes obtenidas: {data.Count}", userId);
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll Solicitudes", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/solicitud/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetById iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetById Solicitud", 
                $"Buscando solicitud con id: {id}", userId);

            try
            {
                var data = await _solicitudService.GetByIdAsync(id);
                
                if (data == null)
                {
                    log.Warn($"Solicitud con id {id} no encontrada");
                    await _logService.RegistrarLogAsync("WARN", $"Solicitud no encontrada: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetById Solicitud", 
                    $"Solicitud {id} obtenida exitosamente", userId);

                return Ok(data);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetById Solicitud", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/solicitud
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SolicitudCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Create iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create Solicitud", 
                $"Creando solicitud para personal: {dto?.IdPersonal}", userId);

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
                var created = await _solicitudService.CreateAsync(dto);

                log.Info($"Create completado correctamente, IdSolicitud: {created.IdSolicitud}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create Solicitud", 
                    $"Solicitud creada con id: {created.IdSolicitud}", userId);

                return CreatedAtAction(nameof(GetById), new { id = created.IdSolicitud }, created);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create Solicitud", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // PUT: api/solicitud/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SolicitudUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Update iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update Solicitud", 
                $"Actualizando solicitud con id: {id}", userId);

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
                var updated = await _solicitudService.UpdateAsync(id, dto);
                
                if (updated == null)
                {
                    log.Warn($"Solicitud con id {id} no encontrada para actualizar");
                    await _logService.RegistrarLogAsync("WARN", $"Solicitud no encontrada para actualizar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update Solicitud", 
                    $"Solicitud {id} actualizada exitosamente", userId);

                return Ok(updated);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update Solicitud", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/solicitud/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete Solicitud", 
                $"Eliminando solicitud con id: {id}", userId);

            try
            {
                var ok = await _solicitudService.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"Solicitud con id {id} no encontrada para eliminar");
                    await _logService.RegistrarLogAsync("WARN", $"Solicitud no encontrada para eliminar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete Solicitud", 
                    $"Solicitud {id} eliminada exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete Solicitud", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
