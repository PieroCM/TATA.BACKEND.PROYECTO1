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
    /// MODIFICADO: Ahora devuelve error 400 con detalles completos si falla
    /// </summary>
    [HttpPost("send-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendSummary()
    {
        _logger.LogCritical("??????????????????????????????????????????????????????????");
        _logger.LogCritical("?  ?? [API] Solicitud manual de envío de resumen diario ?");
        _logger.LogCritical("??????????????????????????????????????????????????????????");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _emailAutomationService.SendDailySummaryAsync();

            stopwatch.Stop();
            _logger.LogCritical("??????????????????????????????????????????????????????????");
            _logger.LogCritical("?  ? [API] Resumen diario enviado exitosamente         ?");
            _logger.LogCritical("?  ??  Tiempo total: {Time:F2}s                            ?", stopwatch.Elapsed.TotalSeconds);
            _logger.LogCritical("??????????????????????????????????????????????????????????");

            return Ok(new
            {
                success = true,
                mensaje = "? Resumen diario enviado exitosamente",
                fecha = DateTime.UtcNow,
                tipo = "MANUAL",
                duracionSegundos = stopwatch.Elapsed.TotalSeconds
            });
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogCritical("??????????????????????????????????????????????????????????");
            _logger.LogCritical("?  ? [API] No se pudo enviar resumen diario            ?");
            _logger.LogCritical("?  ??  Falló después de: {Time:F2}s                        ?", stopwatch.Elapsed.TotalSeconds);
            _logger.LogCritical("??????????????????????????????????????????????????????????");
            _logger.LogError(ex, "Detalles del error:");
            
            return BadRequest(new
            {
                success = false,
                mensaje = "? No se pudo enviar el resumen diario",
                error = ex.Message,
                detalleCompleto = ex.ToString(),
                tipo = "CONFIGURATION_ERROR",
                fecha = DateTime.UtcNow,
                duracionSegundos = stopwatch.Elapsed.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogCritical("??????????????????????????????????????????????????????????");
            _logger.LogCritical("?  ?? [API] Error crítico al enviar resumen             ?");
            _logger.LogCritical("?  ??  Falló después de: {Time:F2}s                        ?", stopwatch.Elapsed.TotalSeconds);
            _logger.LogCritical("??????????????????????????????????????????????????????????");
            _logger.LogError(ex, "Error inesperado:");
            
            return StatusCode(500, new
            {
                success = false,
                mensaje = "? Error crítico al enviar resumen diario",
                error = ex.Message,
                tipoExcepcion = ex.GetType().Name,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray(),
                fecha = DateTime.UtcNow,
                duracionSegundos = stopwatch.Elapsed.TotalSeconds
            });
        }
    }

    /// <summary>
    /// TEST: Comparar envío personalizado vs resumen (para debugging)
    /// POST /api/email/test-comparison
    /// </summary>
    [HttpPost("test-comparison")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> TestComparison()
    {
        _logger.LogCritical("?? INICIANDO TEST DE COMPARACIÓN");

        var test1Exitoso = false;
        var test1Duracion = 0.0;
        var test1Error = "";
        var test1Destinatario = "";

        var test2Exitoso = false;
        var test2Duracion = 0.0;
        var test2Error = "";

        try
        {
            // TEST 1: Envío personalizado (que funciona)
            _logger.LogWarning("??? TEST 1: Envío Personalizado ???");
            var emailConfig = await _emailConfigService.GetConfigAsync();
            
            if (emailConfig == null)
            {
                return BadRequest(new { error = "No hay configuración de EmailConfig" });
            }

            test1Destinatario = emailConfig.DestinatarioResumen;
            var htmlTest = "<html><body><h1>TEST PERSONALIZADO</h1><p>Si esto llega, el problema está en el resumen.</p></body></html>";

            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _emailAutomationService.SendIndividualNotificationAsync(
                    test1Destinatario,
                    "?? TEST: Envío Personalizado",
                    htmlTest);
                
                sw1.Stop();
                test1Exitoso = true;
                test1Duracion = sw1.Elapsed.TotalSeconds;
                _logger.LogInformation("? TEST 1 EXITOSO en {Time:F2}s", test1Duracion);
            }
            catch (Exception ex)
            {
                sw1.Stop();
                test1Exitoso = false;
                test1Duracion = sw1.Elapsed.TotalSeconds;
                test1Error = ex.Message;
                _logger.LogError(ex, "? TEST 1 FALLÓ");
            }

            // Esperar 2 segundos
            await Task.Delay(2000);

            // TEST 2: Resumen diario (que NO funciona)
            _logger.LogWarning("??? TEST 2: Resumen Diario ???");
            
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _emailAutomationService.SendDailySummaryAsync();
                
                sw2.Stop();
                test2Exitoso = true;
                test2Duracion = sw2.Elapsed.TotalSeconds;
                _logger.LogInformation("? TEST 2 EXITOSO en {Time:F2}s", test2Duracion);
            }
            catch (Exception ex)
            {
                sw2.Stop();
                test2Exitoso = false;
                test2Duracion = sw2.Elapsed.TotalSeconds;
                test2Error = ex.Message;
                _logger.LogError(ex, "? TEST 2 FALLÓ");
            }

            // Análisis
            string analisis;
            if (test1Exitoso && test2Exitoso)
            {
                analisis = "? Ambos funcionan. El problema puede ser que Gmail marca el resumen como SPAM.";
            }
            else if (test1Exitoso && !test2Exitoso)
            {
                analisis = "?? Personalizado funciona pero Resumen falla. Problema específico en SendDailySummaryAsync.";
            }
            else if (!test1Exitoso && test2Exitoso)
            {
                analisis = "? Resumen funciona pero Personalizado falla. Inesperado.";
            }
            else
            {
                analisis = "? Ambos fallan. Problema general de SMTP.";
            }

            _logger.LogCritical("?? RESULTADO: {Analisis}", analisis);

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                test1_envioPersonalizado = new
                {
                    exitoso = test1Exitoso,
                    duracionSegundos = test1Duracion,
                    destinatario = test1Destinatario,
                    error = test1Exitoso ? null : test1Error
                },
                test2_resumenDiario = new
                {
                    exitoso = test2Exitoso,
                    duracionSegundos = test2Duracion,
                    error = test2Exitoso ? null : test2Error
                },
                analisis = analisis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en test de comparación");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
