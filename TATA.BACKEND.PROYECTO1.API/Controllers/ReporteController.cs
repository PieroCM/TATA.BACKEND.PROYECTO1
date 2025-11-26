using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // Requiere JWT para todos los endpoints (ajusta si sólo quieres en generar)
    public class ReporteController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReporteController));
        
        private readonly IReporteService _reporteService;
        private readonly ILogSistemaService _logService;

        public ReporteController(IReporteService reporteService, ILogSistemaService logService)
        {
            _reporteService = reporteService;
            _logService = logService;
            log.Debug("ReporteController inicializado.");
        }

        // GET: api/reportes
        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            log.Info("GetReportes iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetReportes",
                Detalles = "Obteniendo todos los reportes",
                IdUsuario = null
            });

            try
            {
                var entities = await _reporteService.GetAllAsync();
                var list = entities.Select(MapToDto).ToList();
                
                log.Info("GetReportes completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetReportes",
                    Detalles = $"Total reportes obtenidos: {list.Count}",
                    IdUsuario = null
                });
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetReportes", ex);
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

        // GET: api/reportes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            log.Info($"GetReporteById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetReporteById {id}",
                Detalles = $"Buscando reporte con id: {id}",
                IdUsuario = null
            });

            try
            {
                var entity = await _reporteService.GetByIdAsync(id);
                
                if (entity == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetReporteById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetReporteById",
                    Detalles = $"Reporte {id} obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetReporteById para id: {id}", ex);
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

        // POST: api/reportes
        [AllowAnonymous] // si aún necesitas crear sin JWT; quítalo cuando todo requiera token
        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReporteCreateRequest request)
        {
            log.Info("CreateReporte iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: CreateReporte",
                Detalles = $"Creando reporte tipo: {request?.TipoReporte}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn("CreateReporte recibió request nulo");
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
                log.Warn("CreateReporte: Validación de ModelState fallida");
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
                var entity = new Reporte
                {
                    TipoReporte = request.TipoReporte,
                    Formato = request.Formato,
                    FiltrosJson = request.FiltrosJson,
                    RutaArchivo = request.RutaArchivo,
                    GeneradoPor = request.GeneradoPor
                };

                await _reporteService.AddAsync(entity);

                log.Info($"CreateReporte completado correctamente, IdReporte: {entity.IdReporte}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: CreateReporte",
                    Detalles = $"Reporte creado con id: {entity.IdReporte}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetReporteById), new { id = entity.IdReporte }, MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante CreateReporte", ex);
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

        // POST: api/reportes/generar
        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarReporteRequest request)
        {
            log.Info("Generar iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Generar Reporte",
                Detalles = $"Generando reporte para {request?.IdsSolicitudes?.Count ?? 0} solicitudes",
                IdUsuario = null
            });

            if (request == null || request.IdsSolicitudes == null || !request.IdsSolicitudes.Any())
            {
                log.Warn("Generar: Solicitudes vacías o nulas");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: sin solicitudes",
                    Detalles = "Debe enviar al menos una solicitud",
                    IdUsuario = null
                });
                return BadRequest("Debes enviar al menos una solicitud.");
            }

            try
            {
                // Obtener id usuario desde el JWT (claim "UserId")
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var idUsuarioActual))
                {
                    log.Warn("Generar: Token sin UserId válido");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Validación fallida: Token sin UserId",
                        Detalles = "El token no contiene claim UserId válido",
                        IdUsuario = null
                    });
                    return Unauthorized("Token sin claim UserId válido.");
                }

                var reporte = await _reporteService.GenerarReporteAsync(request, idUsuarioActual);

                var dto = MapToDto(reporte);
                dto.GeneradoPorNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? "(sin_username)";

                log.Info($"Generar completado correctamente, IdReporte: {reporte.IdReporte}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Generar Reporte",
                    Detalles = $"Reporte generado con id: {reporte.IdReporte}",
                    IdUsuario = idUsuarioActual
                });

                return Ok(dto);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Generar", ex);
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

        // PUT: api/reportes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReporteUpdateRequest request)
        {
            log.Info($"UpdateReporte iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: UpdateReporte {id}",
                Detalles = $"Actualizando reporte con id: {id}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn($"UpdateReporte recibió request nulo para id: {id}");
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
                log.Warn($"UpdateReporte: Validación de ModelState fallida para id: {id}");
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
                var existing = await _reporteService.GetByIdAsync(id);
                if (existing == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                existing.TipoReporte = request.TipoReporte;
                existing.Formato = request.Formato;
                existing.FiltrosJson = request.FiltrosJson;
                existing.RutaArchivo = request.RutaArchivo;
                existing.GeneradoPor = request.GeneradoPor;

                var ok = await _reporteService.UpdateAsync(existing);
                if (!ok)
                {
                    log.Warn($"No se pudo actualizar reporte {id}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"No se pudo actualizar reporte: {id}",
                        Detalles = "El servicio retornó false",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"UpdateReporte completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: UpdateReporte",
                    Detalles = $"Reporte {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante UpdateReporte para id: {id}", ex);
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

        // DELETE: api/reportes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            log.Info($"DeleteReporte iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: DeleteReporte {id}",
                Detalles = $"Eliminando reporte con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _reporteService.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"Reporte con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"DeleteReporte completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: DeleteReporte",
                    Detalles = $"Reporte {id} eliminado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante DeleteReporte para id: {id}", ex);
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

        private static ReporteDTO MapToDto(Reporte r) => new ReporteDTO
        {
            IdReporte = r.IdReporte,
            TipoReporte = r.TipoReporte,
            Formato = r.Formato,
            FiltrosJson = r.FiltrosJson,
            RutaArchivo = r.RutaArchivo,
            GeneradoPor = r.GeneradoPor,
            FechaGeneracion = r.FechaGeneracion,
            TotalSolicitudes = r.Detalles != null ? r.Detalles.Count : 0
        };
    }
}
