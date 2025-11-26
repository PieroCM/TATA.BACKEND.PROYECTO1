using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers;

/// <summary>
/// Controlador de automatización de emails con Primary Constructor (.NET 9)
/// Maneja Broadcast masivo, configuración y envío manual de resúmenes
/// </summary>
[Route("api/email")]
[ApiController]
public class EmailController(
    IEmailAutomationService emailAutomationService,
    IEmailConfigService emailConfigService,
    ILogger<EmailController> logger) : ControllerBase
{
    private readonly IEmailAutomationService _emailAutomationService = emailAutomationService;
    private readonly IEmailConfigService _emailConfigService = emailConfigService;
    private readonly ILogger<EmailController> _logger = logger;

    /// <summary>
    /// Envío masivo de correos (Broadcast) según filtros
    /// POST /api/email/broadcast
    /// Body: { "idRol": 1, "idSla": 2, "asunto": "...", "mensajeHtml": "..." }
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendBroadcast([FromBody] BroadcastDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido en SendBroadcast: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(new
            {
                mensaje = "Datos inválidos",
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        // Validación adicional
        if (string.IsNullOrWhiteSpace(dto.MensajeHtml))
        {
            _logger.LogWarning("MensajeHtml vacío en broadcast");
            return BadRequest(new
            {
                mensaje = "El campo 'mensajeHtml' es obligatorio y no puede estar vacío"
            });
        }

        try
        {
            _logger.LogInformation(
                "Solicitud de broadcast recibida. IdRol={IdRol}, IdSla={IdSla}, Asunto='{Asunto}'",
                dto.IdRol, dto.IdSla, dto.Asunto);

            await _emailAutomationService.SendBroadcastAsync(dto);

            _logger.LogInformation("Broadcast enviado exitosamente");

            return Ok(new
            {
                mensaje = "Broadcast enviado exitosamente",
                fecha = DateTime.UtcNow,
                filtros = new
                {
                    idRol = dto.IdRol,
                    idSla = dto.IdSla
                }
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Error de validación en broadcast");
            return BadRequest(new
            {
                mensaje = "Datos incompletos",
                detalle = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de argumentos en broadcast");
            return BadRequest(new
            {
                mensaje = "Datos inválidos",
                detalle = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de operación en broadcast");
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar broadcast");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar broadcast. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtener últimos 100 logs de envío (para tabla de configuración)
    /// GET /api/email/logs
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetLogs()
    {
        try
        {
            _logger.LogInformation("Solicitud de logs de email");

            var logs = await _emailAutomationService.GetLogsAsync();

            _logger.LogInformation("Se retornaron {Count} logs", logs.Count);

            return Ok(new
            {
                total = logs.Count,
                logs = logs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener logs");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener logs. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtener configuración de email
    /// GET /api/email/config
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(EmailConfigDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmailConfigDTO>> GetConfig()
    {
        try
        {
            _logger.LogInformation("Solicitud de configuración de email");

            var config = await _emailConfigService.GetConfigAsync();

            if (config == null)
            {
                _logger.LogWarning("No se encontró configuración de email");
                return NotFound(new { mensaje = "No se encontró configuración de email. Ejecute las migraciones de BD." });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de email");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener configuración. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Actualizar configuración de email
    /// PUT /api/email/config/{id}
    /// </summary>
    [HttpPut("config/{id:int}")]
    [ProducesResponseType(typeof(EmailConfigDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmailConfigDTO>> UpdateConfig(int id, [FromBody] EmailConfigUpdateDTO dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido al actualizar configuración de email");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Actualizando configuración de email {Id}", id);

            var updated = await _emailConfigService.UpdateConfigAsync(id, dto);

            if (updated == null)
            {
                _logger.LogWarning("Configuración de email {Id} no encontrada", id);
                return NotFound(new { mensaje = $"Configuración con ID {id} no encontrada" });
            }

            _logger.LogInformation("Configuración de email {Id} actualizada exitosamente", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar configuración de email {Id}", id);
            return StatusCode(500, new
            {
                mensaje = "Error al actualizar configuración. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Envío individual de notificación (para botón del Dashboard)
    /// POST /api/email/notify
    /// Body: { "destinatario": "usuario@correo.com", "asunto": "...", "cuerpoHtml": "..." }
    /// </summary>
    [HttpPost("notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendNotification([FromBody] NotificationDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido en SendNotification");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Solicitud de notificación individual a {Destinatario}", dto.Destinatario);

            await _emailAutomationService.SendIndividualNotificationAsync(
                dto.Destinatario, 
                dto.Asunto, 
                dto.CuerpoHtml);

            _logger.LogInformation("Notificación enviada exitosamente");

            return Ok(new
            {
                mensaje = "Notificación enviada exitosamente",
                destinatario = dto.Destinatario,
                fecha = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación en notificación");
            return BadRequest(new
            {
                mensaje = "Datos inválidos",
                detalle = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al enviar notificación");
            return StatusCode(500, new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar notificación");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar notificación. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Enviar resumen diario manualmente (para pruebas o botón administrativo)
    /// POST /api/email/send-summary
    /// </summary>
    [HttpPost("send-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendSummary()
    {
        try
        {
            _logger.LogInformation("Solicitud manual de envío de resumen diario");

            await _emailAutomationService.SendDailySummaryAsync();

            _logger.LogInformation("Resumen diario enviado exitosamente (manual)");

            return Ok(new
            {
                mensaje = "Resumen diario enviado exitosamente",
                fecha = DateTime.UtcNow,
                tipo = "MANUAL"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar resumen diario");
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar resumen diario manualmente");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar resumen diario. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtener estadísticas de envíos de correos
    /// GET /api/email/stats
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetStats()
    {
        try
        {
            _logger.LogInformation("Solicitud de estadísticas de email");

            // Aquí podrías implementar lógica para obtener estadísticas desde EmailLog
            // Por ahora retornamos un placeholder

            return Ok(new
            {
                mensaje = "Estadísticas disponibles próximamente",
                fecha = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener estadísticas",
                error = ex.Message
            });
        }
    }
}
