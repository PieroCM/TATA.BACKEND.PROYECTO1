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
    /// Body: { "asunto": "...", "mensajeHtml": "...", "idRol": 1, "idSla": 2, "esPrueba": true, "emailPrueba": "test@correo.com" }
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

        // Validar que si es prueba, debe tener email de prueba
        if (dto.EsPrueba && string.IsNullOrWhiteSpace(dto.EmailPrueba))
        {
            _logger.LogWarning("EmailPrueba vacío cuando EsPrueba es true");
            return BadRequest(new
            {
                mensaje = "El campo 'emailPrueba' es obligatorio cuando 'esPrueba' es true"
            });
        }

        try
        {
            _logger.LogInformation(
                "Solicitud de broadcast recibida. Modo: {Modo}, IdRol={IdRol}, IdSla={IdSla}, Asunto='{Asunto}'",
                dto.EsPrueba ? "PRUEBA" : "PRODUCCIÓN", dto.IdRol, dto.IdSla, dto.Asunto);

            await _emailAutomationService.SendBroadcastAsync(dto);

            _logger.LogInformation("Broadcast enviado exitosamente");

            return Ok(new
            {
                mensaje = dto.EsPrueba 
                    ? $"Correo de prueba enviado exitosamente a {dto.EmailPrueba}" 
                    : "Broadcast enviado exitosamente",
                fecha = DateTime.UtcNow,
                modo = dto.EsPrueba ? "PRUEBA" : "PRODUCCIÓN",
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
            _logger.LogInformation("?? Solicitud de configuración de email desde frontend");

            var config = await _emailConfigService.GetConfigAsync();

            if (config == null)
            {
                _logger.LogWarning("?? No se encontró configuración de email");
                return NotFound(new 
                { 
                    mensaje = "No se encontró configuración de email. Ejecute las migraciones de BD.",
                    success = false 
                });
            }

            _logger.LogInformation("? Configuración obtenida: ResumenDiario={Estado}, HoraResumen={Hora}", 
                config.ResumenDiario ? "ACTIVADO" : "DESACTIVADO", 
                config.HoraResumen.ToString(@"hh\:mm\:ss"));

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al obtener configuración de email");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener configuración. Por favor, contacte al administrador.",
                error = ex.Message,
                success = false
            });
        }
    }

    /// <summary>
    /// Actualizar configuración de email desde el frontend
    /// PUT /api/email/config/{id}
    /// Body ejemplo: { "resumenDiario": true, "horaResumen": "08:00:00" }
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
            _logger.LogWarning("?? Modelo inválido al actualizar configuración de email desde frontend");
            return BadRequest(new 
            { 
                mensaje = "Datos inválidos",
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                success = false
            });
        }

        try
        {
            _logger.LogInformation("?? Actualizando configuración de email {Id} desde frontend", id);
            
            // Log de los campos recibidos
            if (dto.ResumenDiario.HasValue)
            {
                var emoji = dto.ResumenDiario.Value ? "?" : "?";
                _logger.LogInformation("{Emoji} Frontend solicita cambiar ResumenDiario a: {Estado}", 
                    emoji, dto.ResumenDiario.Value);
            }
            
            if (dto.HoraResumen.HasValue)
            {
                _logger.LogInformation("? Frontend solicita cambiar HoraResumen a: {Hora}", 
                    dto.HoraResumen.Value.ToString(@"hh\:mm\:ss"));
            }

            var updated = await _emailConfigService.UpdateConfigAsync(id, dto);

            if (updated == null)
            {
                _logger.LogWarning("?? Configuración de email {Id} no encontrada", id);
                return NotFound(new 
                { 
                    mensaje = $"Configuración con ID {id} no encontrada",
                    success = false
                });
            }

            _logger.LogInformation("? Configuración de email {Id} actualizada exitosamente desde frontend", id);

            return Ok(new
            {
                success = true,
                mensaje = "Configuración actualizada exitosamente",
                data = updated,
                actualizadoEn = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al actualizar configuración de email {Id}", id);
            return StatusCode(500, new
            {
                mensaje = "Error al actualizar configuración. Por favor, contacte al administrador.",
                error = ex.Message,
                success = false
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
    /// Enviar resumen diario manualmente (para pruebas)
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
    /// Enviar notificaciones individuales manualmente (para pruebas)
    /// POST /api/email/send-notifications
    /// </summary>
    [HttpPost("send-notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendNotifications()
    {
        try
        {
            _logger.LogInformation("Solicitud manual de envío de notificaciones individuales");

            await _emailAutomationService.SendIndividualNotificationsAsync();

            _logger.LogInformation("Notificaciones individuales enviadas exitosamente (manual)");

            return Ok(new
            {
                mensaje = "Notificaciones individuales enviadas exitosamente",
                fecha = DateTime.UtcNow,
                tipo = "MANUAL"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones individuales manualmente");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar notificaciones individuales. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtener lista de roles activos para selectores
    /// GET /api/email/roles
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetRoles()
    {
        try
        {
            _logger.LogInformation("Solicitud de roles activos para selector");

            var roles = await _emailAutomationService.GetRolesActivosAsync();

            _logger.LogInformation("Se retornaron {Count} roles activos", roles.Count);

            return Ok(new
            {
                total = roles.Count,
                roles = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener roles");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener roles. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtener lista de SLAs activos para selectores
    /// GET /api/email/slas
    /// </summary>
    [HttpGet("slas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetSlas()
    {
        try
        {
            _logger.LogInformation("Solicitud de SLAs activos para selector");

            var slas = await _emailAutomationService.GetSlasActivosAsync();

            _logger.LogInformation("Se retornaron {Count} SLAs activos", slas.Count);

            return Ok(new
            {
                total = slas.Count,
                slas = slas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener SLAs");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener SLAs. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Vista previa de destinatarios según filtros
    /// GET /api/email/preview-destinatarios?idRol=1&idSla=2
    /// </summary>
    [HttpGet("preview-destinatarios")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPreviewDestinatarios([FromQuery] int? idRol, [FromQuery] int? idSla)
    {
        try
        {
            _logger.LogInformation("Solicitud de preview de destinatarios. IdRol={IdRol}, IdSla={IdSla}", idRol, idSla);

            var destinatarios = await _emailAutomationService.GetDestinatariosPreviewAsync(idRol, idSla);

            _logger.LogInformation("Se retornaron {Count} destinatarios para preview", destinatarios.Count);

            return Ok(new
            {
                total = destinatarios.Count,
                destinatarios = destinatarios
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener preview de destinatarios");
            return StatusCode(500, new
            {
                mensaje = "Error al obtener preview de destinatarios. Por favor, contacte al administrador.",
                error = ex.Message
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
