using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ReporteDetalleController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReporteDetalleController));
        
        private readonly IReporteDetalleService _service;
        private readonly ILogService _logService;

        public ReporteDetalleController(IReporteDetalleService service, ILogService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("ReporteDetalleController inicializado.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAll iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll ReporteDetalle", 
                "Obteniendo todos los detalles de reportes", userId);

            try
            {
                var items = await _service.GetAllAsync();
                var list = new List<ReporteDetalleDTO>();
                foreach (var e in items)
                {
                    list.Add(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
                }

                log.Info($"GetAll completado correctamente, {list.Count} detalles obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll ReporteDetalle", 
                    $"Total detalles obtenidos: {list.Count}", userId);

                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll ReporteDetalle", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpGet("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> GetByIds(int idReporte, int idSolicitud)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetByIds iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetByIds ReporteDetalle", 
                $"Buscando detalle con idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);

            try
            {
                var e = await _service.GetByIdsAsync(idReporte, idSolicitud);
                
                if (e == null)
                {
                    log.Warn($"ReporteDetalle no encontrado para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                    await _logService.RegistrarLogAsync("WARN", "ReporteDetalle no encontrado", 
                        $"idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);
                    return NotFound();
                }

                log.Info($"GetByIds completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetByIds ReporteDetalle", 
                    "Detalle obtenido exitosamente", userId);

                return Ok(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetByIds", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetByIds ReporteDetalle", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReporteDetalleCreateRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Create iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create ReporteDetalle", 
                $"Creando detalle para idReporte: {request?.IdReporte}, idSolicitud: {request?.IdSolicitud}", userId);

            if (request == null)
            {
                log.Warn("Create recibió request nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: request nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest();
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
                var ok = await _service.AddAsync(new ReporteDetalle
                {
                    IdReporte = request.IdReporte,
                    IdSolicitud = request.IdSolicitud
                });

                if (!ok)
                {
                    log.Warn($"La relación ya existe: idReporte: {request.IdReporte}, idSolicitud: {request.IdSolicitud}");
                    await _logService.RegistrarLogAsync("WARN", "Conflicto: La relación ya existe", 
                        $"idReporte: {request.IdReporte}, idSolicitud: {request.IdSolicitud}", userId);
                    return Conflict("La relación ya existe.");
                }

                log.Info($"Create completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create ReporteDetalle", 
                    "Detalle creado exitosamente", userId);

                return CreatedAtAction(nameof(GetByIds),
                    new { idReporte = request.IdReporte, idSolicitud = request.IdSolicitud },
                    new ReporteDetalleDTO { IdReporte = request.IdReporte, IdSolicitud = request.IdSolicitud });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create ReporteDetalle", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPut("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Update(int idReporte, int idSolicitud, [FromBody] ReporteDetalleUpdateRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Update iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update ReporteDetalle", 
                $"Actualizando detalle para idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);

            if (request == null)
            {
                log.Warn("Update recibió request nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: request nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest();
            }

            if (request.IdReporte != idReporte || request.IdSolicitud != idSolicitud)
            {
                log.Warn("Update: Las claves del body y la ruta no coinciden");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Claves no coinciden", 
                    "Las claves del body y la ruta no coinciden", userId);
                return BadRequest("Las claves del body y la ruta no coinciden.");
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
                var ok = await _service.UpdateAsync(new ReporteDetalle
                {
                    IdReporte = idReporte,
                    IdSolicitud = idSolicitud
                });

                if (!ok)
                {
                    log.Warn($"ReporteDetalle no encontrado para actualizar");
                    await _logService.RegistrarLogAsync("WARN", "ReporteDetalle no encontrado para actualizar", 
                        $"idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);
                    return NotFound();
                }

                log.Info($"Update completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update ReporteDetalle", 
                    "Detalle actualizado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Update", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update ReporteDetalle", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpDelete("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Delete(int idReporte, int idSolicitud)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete ReporteDetalle", 
                $"Eliminando detalle para idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);

            try
            {
                var ok = await _service.DeleteAsync(idReporte, idSolicitud);
                
                if (!ok)
                {
                    log.Warn($"ReporteDetalle no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", "ReporteDetalle no encontrado para eliminar", 
                        $"idReporte: {idReporte}, idSolicitud: {idSolicitud}", userId);
                    return NotFound();
                }

                log.Info($"Delete completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete ReporteDetalle", 
                    "Detalle eliminado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Delete", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete ReporteDetalle", 
                    ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
