using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertaController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AlertaController));
        
        private readonly IAlertaService _alertaService;
        private readonly ILogSistemaService _logService;

        public AlertaController(IAlertaService alertaService, ILogSistemaService logService)
        {
            _alertaService = alertaService;
            _logService = logService;
            log.Debug("AlertaController inicializado.");
        }

        // GET: api/alerta
        [HttpGet]
        public async Task<ActionResult<List<AlertaDTO>>> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll alertas",
                Detalles = "Obteniendo todas las alertas",
                IdUsuario = null
            });

            try
            {
                var result = await _alertaService.GetAllAsync();
                
                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll",
                    Detalles = $"Total alertas obtenidas: {result.Count}",
                    IdUsuario = null
                });
                
                return Ok(result);
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

        // GET: api/alerta/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AlertaDTO>> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById alerta {id}",
                Detalles = $"Buscando alerta con id: {id}",
                IdUsuario = null
            });

            try
            {
                var alerta = await _alertaService.GetByIdAsync(id);
                if (alerta == null)
                {
                    log.Warn($"Alerta con id {id} no encontrada");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Alerta no encontrada: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { mensaje = "Alerta no encontrada" });
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById",
                    Detalles = $"Alerta {id} obtenida exitosamente",
                    IdUsuario = null
                });

                return Ok(alerta);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
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

        // POST: api/alerta
        [HttpPost]
        public async Task<ActionResult<AlertaDTO>> Create([FromBody] AlertaCreateDto dto)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create alerta",
                Detalles = $"Creando alerta para solicitud: {dto?.IdSolicitud}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn("Create recibió dto nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
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
                var creada = await _alertaService.CreateAsync(dto);

                log.Info($"Create completado correctamente, IdAlerta: {creada.IdAlerta}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create alerta",
                    Detalles = $"Alerta creada con id: {creada.IdAlerta}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetById), new { id = creada.IdAlerta }, creada);
            }
            catch (ArgumentException ex)
            {
                log.Warn($"Error de validación en Create: {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"Error de validación: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                log.Warn($"Error de operación en Create: {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"Error de operación: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = ex.Message });
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

        // PUT: api/alerta/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AlertaDTO>> Update(int id, [FromBody] AlertaUpdateDto dto)
        {
            log.Info($"Update iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update alerta {id}",
                Detalles = $"Actualizando alerta con id: {id}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn($"Update recibió dto nulo para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Update: Validación de ModelState fallida para id: {id}");
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
                var updated = await _alertaService.UpdateAsync(id, dto);
                if (updated == null)
                {
                    log.Warn($"Alerta con id {id} no encontrada para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Alerta no encontrada para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { mensaje = "Alerta no encontrada" });
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update alerta",
                    Detalles = $"Alerta {id} actualizada exitosamente",
                    IdUsuario = null
                });

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                log.Warn($"Error de validación en Update para id {id}: {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"Error de validación: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                log.Warn($"Error de operación en Update para id {id}: {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"Error de operación: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
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

        // DELETE: api/alerta/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete alerta {id}",
                Detalles = $"Eliminando alerta con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _alertaService.DeleteAsync(id);
                if (!ok)
                {
                    log.Warn($"Alerta con id {id} no encontrada para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Alerta no encontrada para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { mensaje = "Alerta no encontrada" });
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete alerta",
                    Detalles = $"Alerta {id} eliminada exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
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

