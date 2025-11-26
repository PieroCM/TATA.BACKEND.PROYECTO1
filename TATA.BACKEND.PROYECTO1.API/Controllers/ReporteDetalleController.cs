using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ReporteDetalleController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReporteDetalleController));
        
        private readonly IReporteDetalleService _service;
        private readonly ILogSistemaService _logService;

        public ReporteDetalleController(IReporteDetalleService service, ILogSistemaService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("ReporteDetalleController inicializado.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll ReporteDetalle",
                Detalles = "Obteniendo todos los detalles de reportes",
                IdUsuario = null
            });

            try
            {
                var items = await _service.GetAllAsync();
                var list = new List<ReporteDetalleDTO>();
                foreach (var e in items)
                {
                    list.Add(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
                }

                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll ReporteDetalle",
                    Detalles = $"Total detalles obtenidos: {list.Count}",
                    IdUsuario = null
                });

                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpGet("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> GetByIds(int idReporte, int idSolicitud)
        {
            log.Info($"GetByIds iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetByIds ReporteDetalle",
                Detalles = $"Buscando detalle con idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                IdUsuario = null
            });

            try
            {
                var e = await _service.GetByIdsAsync(idReporte, idSolicitud);
                
                if (e == null)
                {
                    log.Warn($"ReporteDetalle no encontrado para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ReporteDetalle no encontrado",
                        Detalles = $"idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetByIds completado correctamente para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetByIds ReporteDetalle",
                    Detalles = $"Detalle obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetByIds", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReporteDetalleCreateRequest request)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create ReporteDetalle",
                Detalles = $"Creando detalle para idReporte: {request?.IdReporte}, idSolicitud: {request?.IdSolicitud}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn("Create recibió request nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: request nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Create: Validación de ModelState fallida");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
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
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Conflicto: La relación ya existe",
                        Detalles = $"idReporte: {request.IdReporte}, idSolicitud: {request.IdSolicitud}",
                        IdUsuario = null
                    });
                    return Conflict("La relación ya existe.");
                }

                log.Info($"Create completado correctamente para idReporte: {request.IdReporte}, idSolicitud: {request.IdSolicitud}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create ReporteDetalle",
                    Detalles = $"Detalle creado exitosamente",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetByIds),
                    new { idReporte = request.IdReporte, idSolicitud = request.IdSolicitud },
                    new ReporteDetalleDTO { IdReporte = request.IdReporte, IdSolicitud = request.IdSolicitud });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPut("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Update(int idReporte, int idSolicitud, [FromBody] ReporteDetalleUpdateRequest request)
        {
            log.Info($"Update iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Update ReporteDetalle",
                Detalles = $"Actualizando detalle para idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn($"Update recibió request nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: request nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            if (request.IdReporte != idReporte || request.IdSolicitud != idSolicitud)
            {
                log.Warn($"Update: Las claves del body y la ruta no coinciden");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Claves no coinciden",
                    Detalles = "Las claves del body y la ruta no coinciden",
                    IdUsuario = null
                });
                return BadRequest("Las claves del body y la ruta no coinciden.");
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Update: Validación de ModelState fallida");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
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
                    log.Warn($"ReporteDetalle no encontrado para actualizar: idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ReporteDetalle no encontrado para actualizar",
                        Detalles = $"idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Update completado correctamente para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update ReporteDetalle",
                    Detalles = "Detalle actualizado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpDelete("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Delete(int idReporte, int idSolicitud)
        {
            log.Info($"Delete iniciado para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Delete ReporteDetalle",
                Detalles = $"Eliminando detalle para idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                IdUsuario = null
            });

            try
            {
                var ok = await _service.DeleteAsync(idReporte, idSolicitud);
                
                if (!ok)
                {
                    log.Warn($"ReporteDetalle no encontrado para eliminar: idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ReporteDetalle no encontrado para eliminar",
                        Detalles = $"idReporte: {idReporte}, idSolicitud: {idSolicitud}",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para idReporte: {idReporte}, idSolicitud: {idSolicitud}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete ReporteDetalle",
                    Detalles = "Detalle eliminado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
