using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers;

/// <summary>
/// Controlador moderno de Alertas con Primary Constructor (.NET 9)
/// Expone endpoints para Dashboard inteligente, sincronización y CRUD básico
/// </summary>
[Route("api/alertas")]
[ApiController]
public class AlertasController(
    IAlertaService alertaService,
    ILogger<AlertasController> logger) : ControllerBase
{
    private readonly IAlertaService _alertaService = alertaService;
    private readonly ILogger<AlertasController> _logger = logger;

    // ========== ENDPOINTS DE NEGOCIO INTELIGENTE ==========

    /// <summary>
    /// Sincroniza alertas desde solicitudes (UPSERT lógico)
    /// POST /api/alertas/sync
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Sync()
    {
        try
        {
            _logger.LogInformation("Solicitud de sincronización de alertas recibida");
            
            await _alertaService.SyncAlertasFromSolicitudesAsync();
            
            return Ok(new
            {
                mensaje = "Sincronización de alertas completada exitosamente",
                fecha = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al sincronizar alertas");
            return StatusCode(500, new
            {
                mensaje = "Error al sincronizar alertas. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene datos enriquecidos y planos para el Dashboard
    /// GET /api/alertas/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(List<AlertaDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AlertaDashboardDto>>> GetDashboard()
    {
        try
        {
            _logger.LogInformation("Solicitud de datos del dashboard recibida");
            
            var result = await _alertaService.GetAllDashboardAsync();
            
            _logger.LogInformation("Dashboard generado con {Count} alertas", result.Count);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datos del dashboard");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener datos del dashboard. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    // ========== ENDPOINTS CRUD BÁSICOS ==========

    /// <summary>
    /// Obtiene todas las alertas (formato completo con navegación)
    /// GET /api/alertas
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AlertaDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AlertaDTO>>> GetAll()
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las alertas");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener alertas. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene una alerta específica por ID
    /// GET /api/alertas/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AlertaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertaDTO>> GetById(int id)
    {
        try
        {
            var alerta = await _alertaService.GetByIdAsync(id);
            
            if (alerta == null)
            {
                _logger.LogWarning("Alerta con ID {IdAlerta} no encontrada", id);
                return NotFound(new { mensaje = $"Alerta con ID {id} no encontrada" });
            }
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener alerta con ID {IdAlerta}", id);
            return StatusCode(500, new
            {
                mensaje = "Error al obtener la alerta. Por favor, contacte al administrador.",
                error = ex.Message
            });
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
    }

    /// <summary>
    /// Crea una nueva alerta manualmente
    /// POST /api/alertas
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlertaDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertaDTO>> Create([FromBody] AlertaCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido al crear alerta: {Errors}", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }
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
            _logger.LogInformation("Creando nueva alerta para solicitud {IdSolicitud}", dto.IdSolicitud);
            
            var creada = await _alertaService.CreateAsync(dto);
            
            _logger.LogInformation("Alerta {IdAlerta} creada exitosamente", creada.IdAlerta);
            
            return CreatedAtAction(
                nameof(GetById),
                new { id = creada.IdAlerta },
                creada);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación al crear alerta");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de operación al crear alerta");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear alerta");
            return StatusCode(500, new
            {
                mensaje = "Error al crear la alerta. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Actualiza una alerta existente
    /// PUT /api/alertas/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AlertaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertaDTO>> Update(int id, [FromBody] AlertaUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido al actualizar alerta {IdAlerta}", id);
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Actualizando alerta {IdAlerta}", id);
            
            var updated = await _alertaService.UpdateAsync(id, dto);
            
            if (updated == null)
            {
                _logger.LogWarning("Alerta {IdAlerta} no encontrada para actualizar", id);
                return NotFound(new { mensaje = $"Alerta con ID {id} no encontrada" });
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

            _logger.LogInformation("Alerta {IdAlerta} actualizada exitosamente", id);
            
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación al actualizar alerta {IdAlerta}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de operación al actualizar alerta {IdAlerta}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar alerta {IdAlerta}", id);
            return StatusCode(500, new
            {
                mensaje = "Error al actualizar la alerta. Por favor, contacte al administrador.",
                error = ex.Message
            });
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
    }

    /// <summary>
    /// Elimina (lógicamente) una alerta
    /// DELETE /api/alertas/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("Eliminando alerta {IdAlerta}", id);
            
            var ok = await _alertaService.DeleteAsync(id);
            
            if (!ok)
            {
                _logger.LogWarning("Alerta {IdAlerta} no encontrada para eliminar", id);
                return NotFound(new { mensaje = $"Alerta con ID {id} no encontrada" });
            }

            _logger.LogInformation("Alerta {IdAlerta} eliminada exitosamente", id);
            
            return NoContent();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar alerta {IdAlerta}", id);
            return StatusCode(500, new
            {
                mensaje = "Error al eliminar la alerta. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }
}

