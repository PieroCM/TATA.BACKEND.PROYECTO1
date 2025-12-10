using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services;

/// <summary>
/// Servicio de automatización de correos con Primary Constructor (.NET 9)
/// Maneja Broadcast masivo y resúmenes diarios automatizados
/// </summary>
public class EmailAutomationService(
    Proyecto1SlaDbContext context,
    IEmailService emailService,
    ILogger<EmailAutomationService> logger) : IEmailAutomationService
{
    private readonly Proyecto1SlaDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<EmailAutomationService> _logger = logger;

    /// <summary>
    /// Envío masivo (Broadcast) según filtros de Rol y/o SLA
    /// VERSIÓN ACTUALIZADA: Soporta modo de prueba
    /// </summary>
    public async Task SendBroadcastAsync(BroadcastDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto), "El DTO de broadcast no puede ser nulo");

        if (string.IsNullOrWhiteSpace(dto.MensajeHtml))
            throw new ArgumentException("El mensaje HTML no puede estar vacío", nameof(dto.MensajeHtml));

        _logger.LogInformation("Iniciando envío de broadcast. Modo: {Modo}, Filtros: IdRol={IdRol}, IdSla={IdSla}", 
            dto.EsPrueba ? "PRUEBA" : "PRODUCCIÓN", dto.IdRol, dto.IdSla);

        try
        {
            // CASO PRUEBA: Envío simple a un solo correo
            if (dto.EsPrueba)
            {
                if (string.IsNullOrWhiteSpace(dto.EmailPrueba))
                {
                    throw new ArgumentException("Debe especificar un email de prueba cuando EsPrueba es true", nameof(dto.EmailPrueba));
                }

                _logger.LogInformation("Modo PRUEBA activado. Enviando a: {Email}", dto.EmailPrueba);

                await _emailService.SendAsync(dto.EmailPrueba, dto.Asunto, dto.MensajeHtml);

                await RegistrarEmailLog("BROADCAST_PRUEBA", dto.EmailPrueba, "OK", 
                    "Envío de prueba exitoso");

                _logger.LogInformation("Broadcast de prueba enviado exitosamente a {Email}", dto.EmailPrueba);
                return;
            }

            // CASO PRODUCCIÓN: Envío masivo según filtros
            var query = _context.Personal
                .AsNoTracking()
                .Where(p => p.Estado == "ACTIVO" && !string.IsNullOrWhiteSpace(p.CorreoCorporativo));

            // Aplicar filtros dinámicos
            if (dto.IdRol.HasValue)
            {
                // Filtrar por personas que tienen solicitudes con ese rol
                query = query.Where(p => p.Solicitud.Any(s => s.IdRolRegistro == dto.IdRol.Value));
                _logger.LogDebug("Aplicado filtro por Rol: {IdRol}", dto.IdRol.Value);
            }

            if (dto.IdSla.HasValue)
            {
                // Filtrar por personas que tienen solicitudes con ese SLA
                query = query.Where(p => p.Solicitud.Any(s => s.IdSla == dto.IdSla.Value));
                _logger.LogDebug("Aplicado filtro por SLA: {IdSla}", dto.IdSla.Value);
            }

            // Obtener correos únicos
            var correos = await query
                .Select(p => p.CorreoCorporativo!)
                .Distinct()
                .ToListAsync();

            if (!correos.Any())
            {
                _logger.LogWarning("No se encontraron destinatarios con los filtros especificados");
                await RegistrarEmailLog("BROADCAST", "", "ERROR", 
                    "No se encontraron destinatarios con los filtros especificados");
                throw new InvalidOperationException("No se encontraron destinatarios con los filtros especificados");
            }

            _logger.LogInformation("Se enviarán correos a {Count} destinatarios únicos", correos.Count);

            // Enviar correos individualmente
            var exitosos = 0;
            var fallidos = 0;
            var errores = new List<string>();

            foreach (var correo in correos)
            {
                try
                {
                    await _emailService.SendAsync(correo, dto.Asunto, dto.MensajeHtml);
                    exitosos++;
                    _logger.LogDebug("Correo enviado exitosamente a {Correo}", correo);
                }
                catch (Exception ex)
                {
                    fallidos++;
                    var error = $"{correo}: {ex.Message}";
                    errores.Add(error);
                    _logger.LogError(ex, "Error al enviar correo a {Correo}", correo);
                }
            }

            // Registrar resultado en el log
            var destinatariosStr = string.Join(", ", correos);
            var estado = fallidos == 0 ? "OK" : (exitosos == 0 ? "ERROR" : "PARCIAL");
            var errorDetalle = errores.Any()
                ? $"Exitosos: {exitosos}, Fallidos: {fallidos}. Errores: {string.Join("; ", errores.Take(5))}"
                : $"Enviados exitosamente a {exitosos} destinatarios";

            await RegistrarEmailLog("BROADCAST", destinatariosStr, estado, errorDetalle);

            _logger.LogInformation(
                "Broadcast completado. Exitosos: {Exitosos}, Fallidos: {Fallidos}",
                exitosos, fallidos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico durante el envío de broadcast");
            throw;
        }
    }

    /// <summary>
    /// Envío automático del resumen diario de alertas críticas/vencidas
    /// VERSIÓN REFORZADA: Con validaciones, guard clause, logs extremadamente detallados y respuesta dinámica
    /// </summary>
    public async Task<EmailSummaryResponseDto> SendDailySummaryAsync()
    {
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogCritical("?? [PASO 1/7] INICIANDO envío de resumen diario");
        _logger.LogCritical("?????????????????????????????????????????");

        // PASO 1: Obtener configuración
        _logger.LogInformation("?? [PASO 2/7] Consultando EmailConfig en base de datos...");
        var config = await _context.EmailConfig.FirstOrDefaultAsync();
        
        if (config == null)
        {
            var error = "? [FATAL] No existe registro en tabla email_config";
            _logger.LogCritical(error);
            await RegistrarEmailLog("RESUMEN", "", "ERROR", error);
            throw new InvalidOperationException(error);
        }

        _logger.LogInformation("? EmailConfig encontrado: Id={Id}", config.Id);
        _logger.LogInformation("   ?? ResumenDiario: {ResumenDiario}", config.ResumenDiario);
        _logger.LogInformation("   ?? DestinatarioResumen: '{Destinatario}'", config.DestinatarioResumen ?? "NULL");
        _logger.LogInformation("   ? HoraResumen: {Hora}", config.HoraResumen);

        if (!config.ResumenDiario)
        {
            _logger.LogWarning("?? ResumenDiario está DESACTIVADO. Abortando envío.");
            
            // ?? NO registrar en email_log cuando está desactivado (no se envía correo)
            return new EmailSummaryResponseDto
            {
                Exito = true,
                Mensaje = "Resumen diario desactivado en la configuración",
                CantidadAlertas = 0,
                CorreoEnviado = false
            };
        }

        var destinatario = config.DestinatarioResumen?.Trim();
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            var error = "? [FATAL] DestinatarioResumen está vacío o NULL";
            _logger.LogCritical(error);
            _logger.LogCritical("   ?? Valor actual: '{Valor}'", config.DestinatarioResumen ?? "NULL");
            await RegistrarEmailLog("RESUMEN", "", "ERROR", error);
            throw new InvalidOperationException(error);
        }

        _logger.LogInformation("? Destinatario validado: '{Destinatario}'", destinatario);

        // PASO 2: Obtener alertas
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogCritical("?? [PASO 3/7] Consultando alertas CRITICO/ALTO en BD");
        _logger.LogCritical("?????????????????????????????????????????");
        
        var hoy = DateTime.UtcNow.Date;
        
        // Query con logging detallado
        _logger.LogDebug("   Buscando: Estado = 'ACTIVA' AND (Nivel = 'CRITICO' OR Nivel = 'ALTO')");
        
        var alertasCriticas = await _context.Alerta
            .AsNoTracking()
            .Include(a => a.IdSolicitudNavigation)
                .ThenInclude(s => s!.IdPersonalNavigation)
            .Include(a => a.IdSolicitudNavigation)
                .ThenInclude(s => s!.IdSlaNavigation)
            .Include(a => a.IdSolicitudNavigation)
                .ThenInclude(s => s!.IdRolRegistroNavigation)
            .Where(a => a.Estado == "ACTIVA" &&
                       (a.Nivel == "CRITICO" || a.Nivel == "ALTO"))
            .OrderByDescending(a => a.FechaCreacion)
            .ToListAsync();

        _logger.LogInformation("?? Consulta completada: {Count} alertas encontradas", alertasCriticas.Count);

        // ???????????????????????????????????????????????????????????????
        // ?? GUARD CLAUSE: Validar si hay alertas antes de continuar
        // ???????????????????????????????????????????????????????????????
        if (!alertasCriticas.Any())
        {
            _logger.LogWarning("?????????????????????????????????????????");
            _logger.LogWarning("?? No se encontraron alertas para enviar");
            _logger.LogWarning("?????????????????????????????????????????");
            _logger.LogInformation("   ?? No hay alertas con Estado='ACTIVA' y Nivel IN ('CRITICO','ALTO')");
            _logger.LogInformation("   ? Proceso finalizado correctamente sin envío de correo");
            _logger.LogInformation("   ?? Sugerencia: Verifica la consulta con:");
            _logger.LogInformation("      SELECT * FROM alerta WHERE Estado='ACTIVA' AND Nivel IN ('CRITICO','ALTO')");
            
            // ?? NO registrar en email_log cuando no se envía correo
            // await RegistrarEmailLog("RESUMEN", destinatario, "OK", "No se encontraron alertas para enviar"); ? ELIMINADO
            
            // ? EARLY RETURN con mensaje dinámico (sin registro en BD)
            return new EmailSummaryResponseDto
            {
                Exito = true,
                Mensaje = "No se encontraron alertas para enviar",
                CantidadAlertas = 0,
                CorreoEnviado = false,
                Destinatario = destinatario
            };
        }

        // Mostrar primeras 3 alertas para confirmar
        _logger.LogInformation("?? Primeras 3 alertas a incluir:");
        foreach (var alerta in alertasCriticas.Take(3))
        {
            _logger.LogInformation("   ?? [{Id}] {Nivel}: {Mensaje}", 
                alerta.IdAlerta, 
                alerta.Nivel, 
                alerta.Mensaje.Length > 50 ? alerta.Mensaje.Substring(0, 50) + "..." : alerta.Mensaje);
        }

        // PASO 3: Generar HTML
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogCritical("?? [PASO 4/7] Generando HTML del resumen");
        _logger.LogCritical("?????????????????????????????????????????");

        string htmlBody;
        try
        {
            htmlBody = GenerarHtmlResumenDiario(alertasCriticas, hoy);
            _logger.LogInformation("? HTML generado: {Length} caracteres, {Bytes} bytes", 
                htmlBody.Length, 
                System.Text.Encoding.UTF8.GetByteCount(htmlBody));
            
            // Validar que el HTML no esté vacío
            if (string.IsNullOrWhiteSpace(htmlBody))
            {
                throw new InvalidOperationException("HTML generado está vacío");
            }
            
            // Log de preview del HTML
            var preview = htmlBody.Length > 200 ? htmlBody.Substring(0, 200) + "..." : htmlBody;
            _logger.LogDebug("   Preview HTML: {Preview}", preview);
        }
        catch (Exception ex)
        {
            var error = $"? Error al generar HTML: {ex.Message}";
            _logger.LogCritical(ex, error);
            await RegistrarEmailLog("RESUMEN", destinatario, "ERROR", error);
            throw new InvalidOperationException(error, ex);
        }

        // PASO 4: Preparar asunto
        var asunto = $"[RESUMEN DIARIO SLA] {hoy:dd/MM/yyyy} - {alertasCriticas.Count} alertas críticas";
        
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogCritical("?? [PASO 5/7] Preparando envío de correo");
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogInformation("   ?? Destinatario: {To}", destinatario);
        _logger.LogInformation("   ?? Asunto: {Subject}", asunto);
        _logger.LogInformation("   ?? Tamaño HTML: {Size} caracteres", htmlBody.Length);

        // PASO 5: ENVIAR - AQUÍ ES CRÍTICO
        _logger.LogCritical("?????????????????????????????????????????");
        _logger.LogCritical("?? [PASO 6/7] LLAMANDO A EmailService.SendAsync");
        _logger.LogCritical("?????????????????????????????????????????");
        
        var envioComienzo = DateTime.UtcNow;
        var envioExitoso = false;

        try
        {
            _logger.LogWarning("?? Llamando a _emailService.SendAsync...");
            _logger.LogWarning("   Si no ves logs después de esto, el error está en EmailService");
            
            await _emailService.SendAsync(destinatario, asunto, htmlBody);

            envioExitoso = true;
            var duracion = (DateTime.UtcNow - envioComienzo).TotalSeconds;
            
            _logger.LogCritical("?? [ÉXITO] EmailService.SendAsync completado en {Duracion:F2}s", duracion);
            _logger.LogCritical("?????????????????????????????????????????");
        }
        catch (Exception ex)
        {
            var duracion = (DateTime.UtcNow - envioComienzo).TotalSeconds;
            
            _logger.LogCritical("?????????????????????????????????????????");
            _logger.LogCritical("?? [ERROR CAPTURADO] Falló después de {Duracion:F2}s", duracion);
            _logger.LogCritical("?????????????????????????????????????????");
            _logger.LogCritical("Tipo: {Type}", ex.GetType().FullName);
            _logger.LogCritical("Mensaje: {Message}", ex.Message);
            _logger.LogCritical("InnerException: {Inner}", ex.InnerException?.Message ?? "NULL");
            _logger.LogCritical("StackTrace:");
            _logger.LogCritical("{Stack}", ex.StackTrace);

            await RegistrarEmailLog("RESUMEN", destinatario, "ERROR", 
                $"[{ex.GetType().Name}] {ex.Message}");

            throw new InvalidOperationException(
                $"? FALLO SMTP al enviar resumen a {destinatario}. " +
                $"Tipo: {ex.GetType().Name}. Error: {ex.Message}", 
                ex);
        }

        // PASO 6: Registrar éxito y retornar respuesta
        if (envioExitoso)
        {
            _logger.LogCritical("?????????????????????????????????????????");
            _logger.LogCritical("?? [PASO 7/7] Registrando en email_log");
            _logger.LogCritical("?????????????????????????????????????????");

            var mensajeExito = $"Resumen diario enviado exitosamente con {alertasCriticas.Count} alertas";

            await RegistrarEmailLog("RESUMEN", destinatario, "OK", mensajeExito);

            _logger.LogCritical("?? [COMPLETADO] {Mensaje}", mensajeExito);
            _logger.LogCritical("   ?? Destinatario: {To}", destinatario);
            _logger.LogCritical("   ?? Alertas incluidas: {Count}", alertasCriticas.Count);
            _logger.LogCritical("   ? Timestamp: {Time}", DateTime.UtcNow);
            _logger.LogCritical("?????????????????????????????????????????");
            
            // VERIFICACIÓN FINAL
            _logger.LogWarning("?? VERIFICACIÓN POST-ENVÍO:");
            _logger.LogWarning("   Si no ves el correo en tu bandeja:");
            _logger.LogWarning("   1. Revisa SPAM / Correo no deseado");
            _logger.LogWarning("   2. Busca: from:mellamonose19@gmail.com");
            _logger.LogWarning("   3. Busca: subject:[RESUMEN DIARIO SLA]");
            _logger.LogWarning("   4. El correo SÍ se envió, puede estar bloqueado por Gmail");

            // Retornar respuesta con mensaje dinámico
            return new EmailSummaryResponseDto
            {
                Exito = true,
                Mensaje = mensajeExito,
                CantidadAlertas = alertasCriticas.Count,
                CorreoEnviado = true,
                Destinatario = destinatario
            };
        }

        // Este código nunca debería ejecutarse, pero por seguridad:
        return new EmailSummaryResponseDto
        {
            Exito = false,
            Mensaje = "Error desconocido en el envío",
            CantidadAlertas = 0,
            CorreoEnviado = false,
            Destinatario = destinatario
        };
    }

    /// <summary>
    /// Envío individual de notificación (para botón del Dashboard)
    /// </summary>
    public async Task SendIndividualNotificationAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("El destinatario no puede estar vacío", nameof(destinatario));

        if (string.IsNullOrWhiteSpace(asunto))
            throw new ArgumentException("El asunto no puede estar vacío", nameof(asunto));

        if (string.IsNullOrWhiteSpace(cuerpoHtml))
            throw new ArgumentException("El cuerpo del correo no puede estar vacío", nameof(cuerpoHtml));

        _logger.LogInformation("Enviando notificación individual a {Destinatario}", destinatario);

        try
        {
            await _emailService.SendAsync(destinatario, asunto, cuerpoHtml);

            await RegistrarEmailLog("INDIVIDUAL", destinatario, "OK", 
                $"Notificación enviada exitosamente");

            _logger.LogInformation("Notificación enviada exitosamente a {Destinatario}", destinatario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación a {Destinatario}", destinatario);
            
            await RegistrarEmailLog("INDIVIDUAL", destinatario, "ERROR", 
                $"Error: {ex.Message}");

            throw new InvalidOperationException($"No se pudo enviar la notificación a {destinatario}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Envío automático de notificaciones individuales personalizadas
    /// (Método utilizado por EmailAutomationWorker)
    /// TODO: Implementar lógica de envío de notificaciones individuales
    /// </summary>
    public async Task SendIndividualNotificationsAsync()
    {
        _logger.LogInformation("SendIndividualNotificationsAsync llamado - Implementación pendiente");
        
        // TODO: Implementar la lógica de envío de notificaciones individuales
        // Por ejemplo, enviar correos a responsables de alertas próximas a vencer
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Obtener últimos 100 logs de envío ordenados por fecha descendente
    /// </summary>
    public async Task<List<EmailLog>> GetLogsAsync()
    {
        _logger.LogDebug("Obteniendo últimos 100 logs de email");

        try
        {
            var logs = await _context.EmailLog
                .OrderByDescending(x => x.Fecha)
                .Take(100)
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} logs de email", logs.Count);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener logs de email");
            throw;
        }
    }

    /// <summary>
    /// Obtener lista de destinatarios según filtros (Preview antes del envío)
    /// </summary>
    public async Task<List<DestinatarioPreviewDto>> GetDestinatariosPreviewAsync(int? idRol, int? idSla)
    {
        _logger.LogInformation("Obteniendo preview de destinatarios. IdRol={IdRol}, IdSla={IdSla}", idRol, idSla);

        try
        {
            var query = _context.Personal
                .AsNoTracking()
                .Include(p => p.Solicitud)
                    .ThenInclude(s => s.IdRolRegistroNavigation)
                .Where(p => p.Estado == "ACTIVO" && !string.IsNullOrWhiteSpace(p.CorreoCorporativo));

            // Aplicar filtros dinámicos
            if (idRol.HasValue)
            {
                query = query.Where(p => p.Solicitud.Any(s => s.IdRolRegistro == idRol.Value));
                _logger.LogDebug("Aplicado filtro por Rol: {IdRol}", idRol.Value);
            }

            if (idSla.HasValue)
            {
                query = query.Where(p => p.Solicitud.Any(s => s.IdSla == idSla.Value));
                _logger.LogDebug("Aplicado filtro por SLA: {IdSla}", idSla.Value);
            }

            var personal = await query.ToListAsync();

            var destinatarios = personal
                .Select(p => new DestinatarioPreviewDto
                {
                    IdPersonal = p.IdPersonal,
                    NombreCompleto = $"{p.Nombres} {p.Apellidos}",
                    Cargo = p.Solicitud.FirstOrDefault()?.IdRolRegistroNavigation?.NombreRol,
                    FotoUrl = null, // Agregar campo FotoUrl en Personal si se requiere
                    Correo = p.CorreoCorporativo!
                })
                .GroupBy(d => d.Correo)
                .Select(g => g.First())
                .OrderBy(d => d.NombreCompleto)
                .ToList();

            _logger.LogInformation("Se obtuvieron {Count} destinatarios para preview", destinatarios.Count);

            return destinatarios;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener preview de destinatarios");
            throw;
        }
    }

    /// <summary>
    /// Obtener roles activos para selectores
    /// </summary>
    public async Task<List<RolSelectorDto>> GetRolesActivosAsync()
    {
        _logger.LogDebug("Obteniendo roles activos para selector");

        try
        {
            var roles = await _context.RolRegistro
                .AsNoTracking()
                .Where(r => r.EsActivo)
                .OrderBy(r => r.NombreRol)
                .Select(r => new RolSelectorDto
                {
                    Id = r.IdRolRegistro,
                    Descripcion = r.NombreRol
                })
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} roles activos", roles.Count);

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener roles activos");
            throw;
        }
    }

    /// <summary>
    /// Obtener SLAs activos para selectores
    /// </summary>
    public async Task<List<SlaSelectorDto>> GetSlasActivosAsync()
    {
        _logger.LogDebug("Obteniendo SLAs activos para selector");

        try
        {
            var slas = await _context.ConfigSla
                .AsNoTracking()
                .Where(s => s.EsActivo)
                .OrderBy(s => s.CodigoSla)
                .Select(s => new SlaSelectorDto
                {
                    Id = s.IdSla,
                    Descripcion = s.CodigoSla
                })
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} SLAs activos", slas.Count);

            return slas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener SLAs activos");
            throw;
        }
    }

    /// <summary>
    /// Genera el HTML para el resumen diario con estilos modernos
    /// </summary>
    private string GenerarHtmlResumenDiario(List<Alerta> alertas, DateTime fecha)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine(".container { max-width: 1200px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }");
        sb.AppendLine(".header h1 { margin: 0; font-size: 28px; }");
        sb.AppendLine(".header p { margin: 10px 0 0 0; opacity: 0.9; }");
        sb.AppendLine(".content { padding: 30px; }");
        sb.AppendLine(".summary { background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 30px; }");
        sb.AppendLine(".summary-item { display: inline-block; margin-right: 30px; }");
        sb.AppendLine(".summary-number { font-size: 36px; font-weight: bold; color: #667eea; }");
        sb.AppendLine(".summary-label { font-size: 14px; color: #666; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        sb.AppendLine("th, td { padding: 15px; text-align: left; border-bottom: 1px solid #e0e0e0; }");
        sb.AppendLine("th { background-color: #667eea; color: white; font-weight: 600; text-transform: uppercase; font-size: 12px; }");
        sb.AppendLine("tr:hover { background-color: #f8f9fa; }");
        sb.AppendLine(".critico { background-color: #ffebee !important; }");
        sb.AppendLine(".alto { background-color: #fff3e0 !important; }");
        sb.AppendLine(".badge { display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 11px; font-weight: 600; }");
        sb.AppendLine(".badge-critico { background-color: #d32f2f; color: white; }");
        sb.AppendLine(".badge-alto { background-color: #f57c00; color: white; }");
        sb.AppendLine(".footer { padding: 20px 30px; background-color: #f8f9fa; border-radius: 0 0 8px 8px; text-align: center; color: #666; font-size: 12px; }");
        sb.AppendLine("</style></head><body><div class='container'>");

        // Header
        sb.AppendLine($"<div class='header'>");
        sb.AppendLine($"<h1>?? Resumen Diario de Alertas SLA</h1>");
        sb.AppendLine($"<p>{fecha:dddd, dd 'de' MMMM 'de' yyyy}</p>");
        sb.AppendLine("</div>");

        // Content
        sb.AppendLine("<div class='content'>");

        // Summary
        var criticas = alertas.Count(a => a.Nivel == "CRITICO");
        var altas = alertas.Count(a => a.Nivel == "ALTO");

        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<div class='summary-item'>");
        sb.AppendLine($"<div class='summary-number'>{alertas.Count}</div>");
        sb.AppendLine("<div class='summary-label'>Total Alertas</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='summary-item'>");
        sb.AppendLine($"<div class='summary-number' style='color:#d32f2f;'>{criticas}</div>");
        sb.AppendLine("<div class='summary-label'>Críticas</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='summary-item'>");
        sb.AppendLine($"<div class='summary-number' style='color:#f57c00;'>{altas}</div>");
        sb.AppendLine("<div class='summary-label'>Altas</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Table
        sb.AppendLine("<table><thead><tr>");
        sb.AppendLine("<th>ID</th><th>Nivel</th><th>Mensaje</th><th>Responsable</th>");
        sb.AppendLine("<th>Correo</th><th>Rol</th><th>Tipo SLA</th><th>Fecha</th>");
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var alerta in alertas)
        {
            var solicitud = alerta.IdSolicitudNavigation;
            var personal = solicitud?.IdPersonalNavigation;
            var rol = solicitud?.IdRolRegistroNavigation;
            var sla = solicitud?.IdSlaNavigation;

            var nivelClass = alerta.Nivel == "CRITICO" ? "critico" : "alto";
            var badgeClass = alerta.Nivel == "CRITICO" ? "badge-critico" : "badge-alto";
            var nombreCompleto = personal != null
                ? $"{personal.Nombres} {personal.Apellidos}"
                : "N/A";

            sb.AppendLine($"<tr class='{nivelClass}'>");
            sb.AppendLine($"<td><strong>#{alerta.IdAlerta}</strong></td>");
            sb.AppendLine($"<td><span class='badge {badgeClass}'>{alerta.Nivel}</span></td>");
            sb.AppendLine($"<td>{alerta.Mensaje}</td>");
            sb.AppendLine($"<td>{nombreCompleto}</td>");
            sb.AppendLine($"<td>{personal?.CorreoCorporativo ?? "N/A"}</td>");
            sb.AppendLine($"<td>{rol?.NombreRol ?? "N/A"}</td>");
            sb.AppendLine($"<td>{sla?.TipoSolicitud ?? "N/A"}</td>");
            sb.AppendLine($"<td>{alerta.FechaCreacion:dd/MM HH:mm}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></div>");

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>Este es un resumen automático del Sistema de Gestión de Alertas SLA TATA.</p>");
        sb.AppendLine($"<p>Generado el {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Registra un log de envío de correo en la BD
    /// </summary>
    private async Task RegistrarEmailLog(string tipo, string destinatarios, string estado, string? errorDetalle = null)
    {
        try
        {
            var log = new EmailLog
            {
                Fecha = DateTime.UtcNow,
                Tipo = tipo,
                Destinatarios = destinatarios.Length > 2000 ? destinatarios[..1997] + "..." : destinatarios,
                Estado = estado,
                ErrorDetalle = errorDetalle
            };

            _context.EmailLog.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogDebug("EmailLog registrado: Tipo={Tipo}, Estado={Estado}", tipo, estado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar EmailLog");
            // No propagar el error para no afectar el flujo principal
        }
    }
}
