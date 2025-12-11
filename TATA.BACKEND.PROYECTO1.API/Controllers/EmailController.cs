using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;
using System.Diagnostics; // Necesario para Stopwatch

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
    ILogger<EmailController> logger,
    ILogService logService) : ControllerBase
{
    private static readonly ILog log = LogManager.GetLogger(typeof(EmailController));
    
    private readonly IEmailAutomationService _emailAutomationService = emailAutomationService;
    private readonly IEmailConfigService _emailConfigService = emailConfigService;
    private readonly ILogger<EmailController> _logger = logger;
    private readonly ILogService _logService = logService;

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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"SendBroadcast iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: SendBroadcast", 
            $"Usuario {userId} solicita broadcast con IdRol={dto?.IdRol}, IdSla={dto?.IdSla}", userId);

        if (!ModelState.IsValid)
        {
            log.Warn("Modelo inválido en SendBroadcast");
            await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
            
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
            log.Warn("MensajeHtml vacío en broadcast");
            await _logService.RegistrarLogAsync("WARN", "Validación fallida: MensajeHtml vacío", 
                "El campo mensajeHtml es obligatorio", userId);
            
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

            log.Info("Broadcast enviado exitosamente");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: SendBroadcast", 
                $"Broadcast enviado exitosamente por usuario {userId}", userId);
            
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
            log.Warn($"Error de validación en broadcast: {ex.Message}");
            await _logService.RegistrarLogAsync("WARN", "Error de validación en broadcast", ex.ToString(), userId);
            
            _logger.LogWarning(ex, "Error de validación en broadcast");
            return BadRequest(new
            {
                mensaje = "Datos incompletos",
                detalle = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            log.Warn($"Error de argumentos en broadcast: {ex.Message}");
            await _logService.RegistrarLogAsync("WARN", "Error de argumentos en broadcast", ex.ToString(), userId);
            
            _logger.LogWarning(ex, "Error de argumentos en broadcast");
            return BadRequest(new
            {
                mensaje = "Datos inválidos",
                detalle = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            log.Warn($"Error de operación en broadcast: {ex.Message}");
            await _logService.RegistrarLogAsync("WARN", "Error de operación en broadcast", ex.ToString(), userId);
            
            _logger.LogWarning(ex, "Error de operación en broadcast");
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            log.Error("Error inesperado al enviar broadcast", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error inesperado al enviar broadcast", ex.ToString(), userId);
            
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"GetLogs iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetLogs email", 
            $"Usuario {userId} solicita logs de email", userId);

        try
        {
            _logger.LogInformation("Solicitud de logs de email");

            var logs = await _emailAutomationService.GetLogsAsync();

            log.Info($"GetLogs completado correctamente, {logs.Count} logs obtenidos");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetLogs email", 
                $"Se retornaron {logs.Count} logs", userId);
            
            _logger.LogInformation("Se retornaron {Count} logs", logs.Count);

            return Ok(new
            {
                total = logs.Count,
                logs = logs
            });
        }
        catch (Exception ex)
        {
            log.Error("Error al obtener logs", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error al obtener logs de email", ex.ToString(), userId);
            
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"GetConfig iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetConfig email", 
            $"Usuario {userId} solicita configuración de email", userId);

        try
        {
            _logger.LogInformation("🔍 Solicitud de configuración de email desde frontend");

            var config = await _emailConfigService.GetConfigAsync();

            if (config == null)
            {
                // LOGICA SPRINT 3 (Auditoría)
                log.Warn("No se encontró configuración de email");
                await _logService.RegistrarLogAsync("WARN", "Configuración de email no encontrada", 
                    "No existe configuración de email en BD", userId);
                _logger.LogWarning("⚠️ No se encontró configuración de email");

                // RESPUESTA FIX/ALERTAV5 (Estructura correcta)
                return NotFound(new 
                { 
                    mensaje = "No se encontró configuración de email. Ejecute las migraciones de BD.",
                    success = false 
                });
            }

            log.Info("GetConfig completado correctamente");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetConfig email", 
                $"Configuración obtenida exitosamente", userId);

            _logger.LogInformation("✅ Configuración obtenida: ResumenDiario={Estado}, HoraResumen={Hora}", 
                config.ResumenDiario ? "ACTIVADO" : "DESACTIVADO", 
                config.HoraResumen.ToString(@"hh\:mm\:ss"));

            return Ok(config);
        }
        catch (Exception ex)
        {
            // LOGICA SPRINT 3 + FIX (Auditoría + Estructura)
            log.Error("Error al obtener configuración de email", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error al obtener configuración de email", ex.ToString(), userId);
            _logger.LogError(ex, "❌ Error al obtener configuración de email");

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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"UpdateConfig iniciado para id: {id}, usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: UpdateConfig email", 
            $"Usuario {userId} actualizando configuración de email {id}", userId);

        if (!ModelState.IsValid)
        {
            // COMBINACIÓN: Log detallado de Sprint3 + Respuesta detallada de Fix
            log.Warn("Modelo inválido al actualizar configuración de email");
            await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
            
            _logger.LogWarning("⚠️ Modelo inválido al actualizar configuración de email desde frontend");
            
            return BadRequest(new 
            { 
                mensaje = "Datos inválidos",
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                success = false
            });
        }

        try
        {
            _logger.LogInformation("🔄 Actualizando configuración de email {Id} desde frontend", id);
            
            if (dto.ResumenDiario.HasValue)
            {
                var emoji = dto.ResumenDiario.Value ? "🔔" : "🔕";
                _logger.LogInformation("{Emoji} Frontend solicita cambiar ResumenDiario a: {Estado}", 
                    emoji, dto.ResumenDiario.Value);
            }
            
            if (dto.HoraResumen.HasValue)
            {
                _logger.LogInformation("⏰ Frontend solicita cambiar HoraResumen a: {Hora}", 
                    dto.HoraResumen.Value.ToString(@"hh\:mm\:ss"));
            }

            var updated = await _emailConfigService.UpdateConfigAsync(id, dto);

            if (updated == null)
            {
                // LOGICA SPRINT 3
                log.Warn($"Configuración de email {id} no encontrada");
                await _logService.RegistrarLogAsync("WARN", $"Configuración de email no encontrada: {id}", 
                    "Recurso solicitado no existe", userId);
                _logger.LogWarning("⚠️ Configuración de email {Id} no encontrada", id);
                
                // RESPUESTA FIX
                return NotFound(new 
                { 
                    mensaje = $"Configuración con ID {id} no encontrada",
                    success = false
                });
            }

            // LOGICA SPRINT 3
            log.Info($"UpdateConfig completado correctamente para id: {id}");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: UpdateConfig email", 
                $"Configuración {id} actualizada exitosamente", userId);
            _logger.LogInformation("✅ Configuración de email {Id} actualizada exitosamente desde frontend", id);

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
            // LOGICA SPRINT 3
            log.Error($"Error al actualizar configuración de email {id}", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error al actualizar configuración de email", ex.ToString(), userId);
            _logger.LogError(ex, "❌ Error al actualizar configuración de email {Id}", id);

            // RESPUESTA FIX
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"SendNotification iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: SendNotification", 
            $"Usuario {userId} enviando notificación a {dto?.Destinatario}", userId);

        if (!ModelState.IsValid)
        {
            log.Warn("Modelo inválido en SendNotification");
            await _logService.RegistrarLogAsync("WARN", "Validación fallida: ModelState inválido", 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userId);
            
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

            log.Info("Notificación enviada exitosamente");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: SendNotification", 
                $"Notificación enviada a {dto.Destinatario}", userId);
            
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
            log.Warn($"Error de validación en notificación: {ex.Message}");
            await _logService.RegistrarLogAsync("WARN", "Error de validación en notificación", ex.ToString(), userId);
            
            _logger.LogWarning(ex, "Error de validación en notificación");
            return BadRequest(new
            {
                mensaje = "Datos inválidos",
                detalle = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            log.Error("Error al enviar notificación", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error al enviar notificación", ex.ToString(), userId);
            
            _logger.LogError(ex, "Error al enviar notificación");
            return StatusCode(500, new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            log.Error("Error inesperado al enviar notificación", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error inesperado al enviar notificación", ex.ToString(), userId);
            
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"SendSummary iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: SendSummary manual", 
            $"Usuario {userId} solicita envío manual de resumen diario", userId);
        
        _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");
        _logger.LogCritical("🔔  🛑 [API] Solicitud manual de envío de resumen diario 🛑");
        _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Solicitud manual de envío de resumen diario");

            await _emailAutomationService.SendDailySummaryAsync();

            stopwatch.Stop();
            
            log.Info($"SendSummary completado correctamente en {stopwatch.Elapsed.TotalSeconds:F2}s");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: SendSummary", 
                $"Resumen diario enviado exitosamente en {stopwatch.Elapsed.TotalSeconds:F2}s", userId);
            
            _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");
            _logger.LogCritical("🔔  ✅ [API] Resumen diario enviado exitosamente         🔔");
            _logger.LogCritical("🔔  ⏱️  Tiempo total: {Time:F2}s                         🔔", stopwatch.Elapsed.TotalSeconds);
            _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");

            _logger.LogInformation("Resumen diario enviado exitosamente (manual)");

            return Ok(new
            {
                mensaje = "Resumen diario enviado exitosamente",
                fecha = DateTime.UtcNow,
                tipo = "MANUAL",
                duracionSegundos = stopwatch.Elapsed.TotalSeconds,
                success = true
            });
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            
            log.Error($"No se pudo enviar resumen diario después de {stopwatch.Elapsed.TotalSeconds:F2}s", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error de configuración al enviar resumen diario", 
                ex.ToString(), userId);
            
            _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");
            _logger.LogCritical("🔔  ❌ [API] No se pudo enviar resumen diario            🔔");
            _logger.LogCritical("🔔  ⏱️  Falló después de: {Time:F2}s                         🔔", stopwatch.Elapsed.TotalSeconds);
            _logger.LogCritical("🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔🔔");
            _logger.LogError(ex, "Detalles del error:");
            _logger.LogWarning(ex, "No se pudo enviar resumen diario");

            return BadRequest(new
            {
                mensaje = ex.Message,
                success = false
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error al enviar resumen diario manualmente");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar resumen diario. Por favor, contacte al administrador.",
                error = ex.Message,
                success = false
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
                tipo = "MANUAL",
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones individuales manualmente");
            return StatusCode(500, new
            {
                mensaje = "Error al enviar notificaciones individuales. Por favor, contacte al administrador.",
                error = ex.Message,
                success = false
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
    /// Obtener administradores y analistas con sus correos para selección de destinatarios
    /// GET /api/email/administradores-analistas
    /// </summary>
    [HttpGet("administradores-analistas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetAdministradoresYAnalistas()
    {
        try
        {
            _logger.LogInformation("🔍 Solicitud de administradores y analistas para envío de resumen");

            var usuarios = await _emailAutomationService.GetAdministradoresYAnalistasAsync();

            _logger.LogInformation("✅ Se retornaron {Count} administradores y analistas", usuarios.Count);

            return Ok(new
            {
                success = true,
                total = usuarios.Count,
                usuarios = usuarios,
                mensaje = $"Se encontraron {usuarios.Count} administradores y analistas con correo corporativo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener administradores y analistas");
            return StatusCode(500, new
            {
                success = false,
                mensaje = "Error al obtener administradores y analistas. Por favor, contacte al administrador.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Enviar resumen diario a múltiples destinatarios seleccionados
    /// POST /api/email/send-summary-multiple
    /// Body: { "destinatarios": ["admin@correo.com", "analista@correo.com"] }
    /// </summary>
    [HttpPost("send-summary-multiple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendSummaryToMultiple([FromBody] SendSummaryToMultipleDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("⚠️ Modelo inválido en SendSummaryToMultiple");
            return BadRequest(new
            {
                success = false,
                mensaje = "Datos inválidos",
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        if (dto.Destinatarios == null || !dto.Destinatarios.Any())
        {
            _logger.LogWarning("⚠️ No se proporcionaron destinatarios");
            return BadRequest(new
            {
                success = false,
                mensaje = "Debe proporcionar al menos un destinatario"
            });
        }

        try
        {
            _logger.LogInformation("📧📨 Solicitud de envío de resumen a {Count} destinatarios", dto.Destinatarios.Count);
            foreach (var destinatario in dto.Destinatarios)
            {
                _logger.LogInformation("   ➡️ {Email}", destinatario);
            }

            var resultado = await _emailAutomationService.SendDailySummaryToRecipientsAsync(dto.Destinatarios);

            if (resultado.Exito)
            {
                _logger.LogInformation("✅ Resumen enviado exitosamente");
                return Ok(new
                {
                    success = true,
                    mensaje = resultado.Mensaje,
                    data = resultado,
                    fecha = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("⚠️ Resumen con errores: {Mensaje}", resultado.Mensaje);
                return Ok(new
                {
                    success = false,
                    mensaje = resultado.Mensaje,
                    data = resultado,
                    fecha = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar resumen a múltiples destinatarios");
            return StatusCode(500, new
            {
                success = false,
                mensaje = "Error al enviar resumen. Por favor, contacte al administrador.",
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
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        log.Info($"TestComparison iniciado para usuario {userId}");
        await _logService.RegistrarLogAsync("INFO", "Petición recibida: TestComparison email", 
            $"Usuario {userId} ejecuta test de comparación de envíos", userId);
        
        _logger.LogCritical("🧪 INICIANDO TEST DE COMPARACIÓN");

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
            _logger.LogWarning("➡️➡️➡️ TEST 1: Envío Personalizado ⬅️⬅️⬅️");
            var emailConfig = await _emailConfigService.GetConfigAsync();
            
            if (emailConfig == null)
            {
                log.Warn("No hay configuración de EmailConfig para test");
                await _logService.RegistrarLogAsync("WARN", "Test fallido: sin configuración", 
                    "No existe EmailConfig en BD", userId);
                return BadRequest(new { error = "No hay configuración de EmailConfig" });
            }

            test1Destinatario = emailConfig.DestinatarioResumen;
            var htmlTest = "<html><body><h1>TEST PERSONALIZADO</h1><p>Si esto llega, el problema está en el resumen.</p></body></html>";

            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _emailAutomationService.SendIndividualNotificationAsync(
                    test1Destinatario,
                    "🧪 TEST: Envío Personalizado",
                    htmlTest);
                
                sw1.Stop();
                test1Exitoso = true;
                test1Duracion = sw1.Elapsed.TotalSeconds;
                _logger.LogInformation("✅ TEST 1 EXITOSO en {Time:F2}s", test1Duracion);
            }
            catch (Exception ex)
            {
                sw1.Stop();
                test1Exitoso = false;
                test1Duracion = sw1.Elapsed.TotalSeconds;
                test1Error = ex.Message;
                _logger.LogError(ex, "❌ TEST 1 FALLÓ");
            }

            // Esperar 2 segundos
            await Task.Delay(2000);

            // TEST 2: Resumen diario (que NO funciona)
            _logger.LogWarning("➡️➡️➡️ TEST 2: Resumen Diario ⬅️⬅️⬅️");
            
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _emailAutomationService.SendDailySummaryAsync();
                
                sw2.Stop();
                test2Exitoso = true;
                test2Duracion = sw2.Elapsed.TotalSeconds;
                _logger.LogInformation("✅ TEST 2 EXITOSO en {Time:F2}s", test2Duracion);
            }
            catch (Exception ex)
            {
                sw2.Stop();
                test2Exitoso = false;
                test2Duracion = sw2.Elapsed.TotalSeconds;
                test2Error = ex.Message;
                _logger.LogError(ex, "❌ TEST 2 FALLÓ");
            }

            // Análisis
            string analisis;
            if (test1Exitoso && test2Exitoso)
            {
                analisis = "✅ Ambos funcionan. El problema puede ser que Gmail marca el resumen como SPAM.";
            }
            else if (test1Exitoso && !test2Exitoso)
            {
                analisis = "⚠️ Personalizado funciona pero Resumen falla. Problema específico en SendDailySummaryAsync.";
            }
            else if (!test1Exitoso && test2Exitoso)
            {
                analisis = "⚠️ Resumen funciona pero Personalizado falla. Inesperado.";
            }
            else
            {
                analisis = "🛑 Ambos fallan. Problema general de SMTP.";
            }

            log.Info($"TestComparison completado: {analisis}");
            await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: TestComparison", 
                $"Test1: {test1Exitoso}, Test2: {test2Exitoso}. {analisis}", userId);
            
            _logger.LogCritical("🎯 RESULTADO: {Analisis}", analisis);

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
            log.Error("Error en test de comparación", ex);
            await _logService.RegistrarLogAsync("ERROR", "Error en test de comparación", ex.ToString(), userId);
            
            _logger.LogError(ex, "Error en test de comparación");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}