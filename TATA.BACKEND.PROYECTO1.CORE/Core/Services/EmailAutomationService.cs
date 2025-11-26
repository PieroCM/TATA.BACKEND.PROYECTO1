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
    /// </summary>
    public async Task SendBroadcastAsync(BroadcastDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto), "El DTO de broadcast no puede ser nulo");

        if (string.IsNullOrWhiteSpace(dto.MensajeHtml))
            throw new ArgumentException("El mensaje HTML no puede estar vacío", nameof(dto.MensajeHtml));

        _logger.LogInformation("Iniciando envío de broadcast. Filtros: IdRol={IdRol}, IdSla={IdSla}", 
            dto.IdRol, dto.IdSla);

        try
        {
            // Construir query con filtros
            var query = _context.Solicitud
                .AsNoTracking()
                .Include(s => s.IdPersonalNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Include(s => s.IdSlaNavigation)
                .Where(s => s.EstadoSolicitud != "CERRADO" && s.EstadoSolicitud != "ELIMINADO");

            if (dto.IdRol.HasValue)
            {
                query = query.Where(s => s.IdRolRegistro == dto.IdRol.Value);
                _logger.LogDebug("Aplicado filtro por Rol: {IdRol}", dto.IdRol.Value);
            }

            if (dto.IdSla.HasValue)
            {
                query = query.Where(s => s.IdSla == dto.IdSla.Value);
                _logger.LogDebug("Aplicado filtro por SLA: {IdSla}", dto.IdSla.Value);
            }

            // Obtener correos únicos y válidos
            var correos = await query
                .Select(s => s.IdPersonalNavigation.CorreoCorporativo)
                .Where(email => !string.IsNullOrWhiteSpace(email))
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
    /// </summary>
    public async Task SendDailySummaryAsync()
    {
        _logger.LogInformation("Iniciando envío de resumen diario");

        try
        {
            // 1. Obtener configuración
            var config = await _context.EmailConfig.FirstOrDefaultAsync();
            if (config == null || !config.ResumenDiario)
            {
                _logger.LogInformation("Resumen diario deshabilitado o sin configuración");
                return;
            }

            var destinatario = config.DestinatarioResumen;
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                _logger.LogWarning("No hay destinatario configurado para el resumen diario");
                await RegistrarEmailLog("RESUMEN", "", "ERROR",
                    "No hay destinatario configurado para el resumen diario");
                return;
            }

            // 2. Obtener alertas críticas y vencidas
            var hoy = DateTime.UtcNow.Date;
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

            if (!alertasCriticas.Any())
            {
                _logger.LogInformation("No hay alertas críticas para el resumen diario");
                return;
            }

            _logger.LogInformation("Generando resumen con {Count} alertas críticas", alertasCriticas.Count);

            // 3. Generar HTML del resumen
            var htmlBody = GenerarHtmlResumenDiario(alertasCriticas, hoy);

            // 4. Enviar correo
            await _emailService.SendAsync(
                destinatario,
                $"[RESUMEN DIARIO SLA] {hoy:dd/MM/yyyy} - {alertasCriticas.Count} alertas críticas",
                htmlBody);

            await RegistrarEmailLog("RESUMEN", destinatario, "OK",
                $"Enviado resumen con {alertasCriticas.Count} alertas");

            _logger.LogInformation("Resumen diario enviado exitosamente a {Destinatario}", destinatario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar resumen diario");
            await RegistrarEmailLog("RESUMEN", "", "ERROR", $"Error: {ex.Message}");
            throw;
        }
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
