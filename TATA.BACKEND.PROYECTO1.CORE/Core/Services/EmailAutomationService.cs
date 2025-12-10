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
    /// VERSIÓN REFACTORIZADA: 
    /// - Destinatarios dinámicos (Administradores y Analistas desde BD)
    /// - Notificación "Sin Pendientes" cuando no hay alertas
    /// - HTML generado una sola vez antes de enviar
    /// </summary>
    public async Task<EmailSummaryResponseDto> SendDailySummaryAsync()
    {
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [RESUMEN DIARIO] INICIANDO proceso automático");
        _logger.LogCritical("=================================================================");

        try
        {
            // PASO 1: Verificar configuración
            _logger.LogInformation("?? [PASO 1/6] Consultando EmailConfig...");
            var config = await _context.EmailConfig.FirstOrDefaultAsync();

            if (config == null)
            {
                var error = "? [FATAL] No existe registro en tabla email_config";
                _logger.LogCritical(error);
                await RegistrarEmailLog("RESUMEN_DIARIO", "", "ERROR", error);
                throw new InvalidOperationException(error);
            }

            if (!config.ResumenDiario)
            {
                _logger.LogWarning("?? ResumenDiario está DESACTIVADO. Abortando envío.");
                return new EmailSummaryResponseDto
                {
                    Exito = true,
                    Mensaje = "Resumen diario desactivado en la configuración",
                    CantidadAlertas = 0,
                    CorreoEnviado = false
                };
            }

            _logger.LogInformation("? Configuración validada: ResumenDiario = ACTIVADO");

            // PASO 2: Obtener destinatarios dinámicos (Administradores y Analistas)
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 2/6] Obteniendo destinatarios desde BD (Admin & Analistas)");
            _logger.LogCritical("=================================================================");

            var destinatarios = await ObtenerDestinatariosAdminYAnalistasAsync();

            if (!destinatarios.Any())
            {
                var error = "? No se encontraron Administradores ni Analistas ACTIVOS con correo corporativo";
                _logger.LogWarning(error);
                await RegistrarEmailLog("RESUMEN_DIARIO", "", "ERROR", error);
                return new EmailSummaryResponseDto
                {
                    Exito = false,
                    Mensaje = error,
                    CantidadAlertas = 0,
                    CorreoEnviado = false
                };
            }

            _logger.LogInformation("? Se encontraron {Count} destinatarios:", destinatarios.Count);
            foreach (var dest in destinatarios)
            {
                _logger.LogInformation("   ?? {Email} ({Rol})", dest, 
                    destinatarios.IndexOf(dest) < destinatarios.Count / 2 ? "Administrador" : "Analista");
            }

            // PASO 3: Consultar alertas CRITICO/ALTO
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 3/6] Consultando alertas CRITICO/ALTO en BD");
            _logger.LogCritical("=================================================================");

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

            _logger.LogInformation("?? Consulta completada: {Count} alertas encontradas", alertasCriticas.Count);

            // PASO 4: Generar HTML según si hay o no alertas
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 4/6] Generando contenido HTML del correo");
            _logger.LogCritical("=================================================================");

            string htmlBody;
            string asunto;
            string tipoCorreo;

            if (!alertasCriticas.Any())
            {
                // ? CONDICIÓN B: No hay alertas ? Generar HTML "Sin Pendientes"
                _logger.LogInformation("? No hay alertas críticas/altas. Generando HTML 'Sin Pendientes'...");

                htmlBody = GenerarHtmlSinPendientes(hoy);
                asunto = $"? [RESUMEN DIARIO SLA] {hoy:dd/MM/yyyy} - Todo en Orden";
                tipoCorreo = "RESUMEN_SIN_PENDIENTES";

                _logger.LogInformation("? HTML 'Sin Pendientes' generado: {Length} caracteres", htmlBody.Length);
            }
            else
            {
                // ? CONDICIÓN A: Hay alertas ? Generar HTML con reporte
                _logger.LogInformation("?? Se encontraron {Count} alertas. Generando HTML de reporte...", alertasCriticas.Count);

                // Mostrar primeras 3 alertas
                _logger.LogInformation("?? Primeras 3 alertas a incluir:");
                foreach (var alerta in alertasCriticas.Take(3))
                {
                    _logger.LogInformation("   ?? [{Id}] {Nivel}: {Mensaje}",
                        alerta.IdAlerta,
                        alerta.Nivel,
                        alerta.Mensaje.Length > 50 ? alerta.Mensaje.Substring(0, 50) + "..." : alerta.Mensaje);
                }

                htmlBody = GenerarHtmlResumenDiario(alertasCriticas, hoy);
                asunto = $"?? [RESUMEN DIARIO SLA] {hoy:dd/MM/yyyy} - {alertasCriticas.Count} alertas críticas";
                tipoCorreo = "RESUMEN_CON_ALERTAS";

                _logger.LogInformation("? HTML con alertas generado: {Length} caracteres", htmlBody.Length);
            }

            // Validar que el HTML no esté vacío
            if (string.IsNullOrWhiteSpace(htmlBody))
            {
                throw new InvalidOperationException("? HTML generado está vacío");
            }

            // PASO 5: Enviar a cada destinatario
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 5/6] Enviando correos a {Count} destinatarios", destinatarios.Count);
            _logger.LogCritical("=================================================================");

            var resultados = new List<EnvioResultadoDto>();
            var exitosos = 0;
            var fallidos = 0;

            foreach (var destinatario in destinatarios)
            {
                _logger.LogInformation("?? Enviando a: {Email}", destinatario);

                try
                {
                    await _emailService.SendAsync(destinatario, asunto, htmlBody);

                    exitosos++;
                    resultados.Add(new EnvioResultadoDto
                    {
                        Destinatario = destinatario,
                        Exitoso = true
                    });

                    _logger.LogInformation("? Enviado exitosamente a {Email}", destinatario);

                    // Registrar en email_log individualmente
                    await RegistrarEmailLog(tipoCorreo, destinatario, "OK",
                        alertasCriticas.Any()
                            ? $"Resumen enviado con {alertasCriticas.Count} alertas"
                            : "Resumen 'Sin Pendientes' enviado");
                }
                catch (Exception ex)
                {
                    fallidos++;
                    var errorMsg = $"{ex.GetType().Name}: {ex.Message}";

                    resultados.Add(new EnvioResultadoDto
                    {
                        Destinatario = destinatario,
                        Exitoso = false,
                        MensajeError = errorMsg
                    });

                    _logger.LogError(ex, "? Error al enviar a {Email}", destinatario);

                    // Registrar error en email_log
                    await RegistrarEmailLog(tipoCorreo, destinatario, "ERROR", errorMsg);
                }
            }

            // PASO 6: Resumen final
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("? [PASO 6/6] Proceso completado");
            _logger.LogCritical("=================================================================");
            _logger.LogInformation("?? Estadísticas:");
            _logger.LogInformation("   ? Exitosos: {Exitosos}", exitosos);
            _logger.LogInformation("   ? Fallidos: {Fallidos}", fallidos);
            _logger.LogInformation("   ?? Alertas incluidas: {Total}", alertasCriticas.Count);
            _logger.LogInformation("   ?? Destinatarios: {Count}", destinatarios.Count);
            _logger.LogInformation("   ?? Tipo: {Tipo}", tipoCorreo);

            var mensajeFinal = exitosos == destinatarios.Count
                ? $"Resumen diario enviado exitosamente a {exitosos} destinatario(s)"
                : $"Resumen enviado parcialmente: {exitosos} exitosos, {fallidos} fallidos de {destinatarios.Count} destinatarios";

            if (alertasCriticas.Any())
            {
                mensajeFinal += $" con {alertasCriticas.Count} alertas";
            }
            else
            {
                mensajeFinal += " (Sin alertas pendientes)";
            }

            return new EmailSummaryResponseDto
            {
                Exito = exitosos > 0,
                Mensaje = mensajeFinal,
                CantidadAlertas = alertasCriticas.Count,
                CorreoEnviado = exitosos > 0,
                Destinatarios = destinatarios,
                ResultadosEnvios = resultados
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "?? Error crítico en SendDailySummaryAsync");
            _logger.LogCritical("Tipo: {Type}", ex.GetType().FullName);
            _logger.LogCritical("Mensaje: {Message}", ex.Message);

            await RegistrarEmailLog("RESUMEN_DIARIO", "", "ERROR", $"Error crítico: {ex.Message}");

            throw;
        }
    }

    /// <summary>
    /// Obtiene lista de correos corporativos de usuarios ACTIVOS con rol Administrador (1) o Analista (2)
    /// Optimizado: Una sola consulta a BD con proyección directa a List de strings
    /// </summary>
    private async Task<List<string>> ObtenerDestinatariosAdminYAnalistasAsync()
    {
        _logger.LogDebug("?? Consultando destinatarios (Admin & Analistas) en BD...");

        try
        {
            var correos = await _context.Usuario
                .AsNoTracking()
                .Where(u => u.Estado == "ACTIVO" &&
                           (u.IdRolSistema == 1 || u.IdRolSistema == 2) && // Admin o Analista
                           u.PersonalNavigation != null &&
                           !string.IsNullOrWhiteSpace(u.PersonalNavigation.CorreoCorporativo))
                .Select(u => u.PersonalNavigation!.CorreoCorporativo!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            _logger.LogDebug("? Consulta completada: {Count} correos únicos encontrados", correos.Count);

            return correos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al consultar destinatarios");
            throw;
        }
    }

    /// <summary>
    /// Genera HTML "Sin Pendientes" cuando no hay alertas críticas/altas
    /// Mensaje positivo: "Todo en orden, no hay alertas próximas a vencer hoy"
    /// </summary>
    private string GenerarHtmlSinPendientes(DateTime fecha)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine(".container { max-width: 800px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #4caf50 0%, #45a049 100%); color: white; padding: 40px 30px; text-align: center; }");
        sb.AppendLine(".header h1 { margin: 0; font-size: 32px; }");
        sb.AppendLine(".header p { margin: 15px 0 0 0; opacity: 0.9; font-size: 16px; }");
        sb.AppendLine(".icon-success { font-size: 80px; margin: 20px 0; }");
        sb.AppendLine(".content { padding: 40px 30px; text-align: center; }");
        sb.AppendLine(".message-box { background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%); border-left: 4px solid #4caf50; padding: 30px; border-radius: 8px; margin: 20px 0; }");
        sb.AppendLine(".message-box h2 { margin: 0 0 15px 0; color: #2e7d32; font-size: 24px; }");
        sb.AppendLine(".message-box p { margin: 10px 0; color: #1b5e20; font-size: 16px; line-height: 1.6; }");
        sb.AppendLine(".stats-box { display: flex; justify-content: center; gap: 30px; margin: 30px 0; flex-wrap: wrap; }");
        sb.AppendLine(".stat-item { background: #f8f9fa; padding: 20px 30px; border-radius: 8px; text-align: center; min-width: 150px; }");
        sb.AppendLine(".stat-number { font-size: 48px; font-weight: bold; color: #4caf50; margin: 0; }");
        sb.AppendLine(".stat-label { font-size: 14px; color: #666; margin: 10px 0 0 0; text-transform: uppercase; letter-spacing: 1px; }");
        sb.AppendLine(".footer { padding: 30px; background-color: #f8f9fa; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e0e0e0; }");
        sb.AppendLine(".footer strong { color: #4caf50; }");
        sb.AppendLine(".check-icon { color: #4caf50; font-size: 24px; margin-right: 10px; }");
        sb.AppendLine("</style></head><body><div class='container'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<div class='icon-success'>?</div>");
        sb.AppendLine("<h1>Resumen Diario SLA</h1>");
        sb.AppendLine($"<p>{fecha:dddd, dd 'de' MMMM 'de' yyyy}</p>");
        sb.AppendLine("</div>");

        // Content
        sb.AppendLine("<div class='content'>");

        // Mensaje principal
        sb.AppendLine("<div class='message-box'>");
        sb.AppendLine("<h2><span class='check-icon'>?</span> ¡Todo en Orden!</h2>");
        sb.AppendLine("<p><strong>No hay alertas críticas ni de alta prioridad pendientes en este momento.</strong></p>");
        sb.AppendLine("<p>Todas las solicitudes SLA se encuentran dentro de los plazos establecidos.</p>");
        sb.AppendLine("<p>El sistema continúa monitoreando activamente todas las solicitudes.</p>");
        sb.AppendLine("</div>");

        // Estadísticas
        sb.AppendLine("<div class='stats-box'>");
        sb.AppendLine("<div class='stat-item'>");
        sb.AppendLine("<p class='stat-number'>0</p>");
        sb.AppendLine("<p class='stat-label'>Alertas Críticas</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='stat-item'>");
        sb.AppendLine("<p class='stat-number'>0</p>");
        sb.AppendLine("<p class='stat-label'>Alertas Altas</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class='stat-item'>");
        sb.AppendLine("<p class='stat-number'>?</p>");
        sb.AppendLine("<p class='stat-label'>Estado del Sistema</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Información adicional
        sb.AppendLine("<div style='margin-top: 30px; padding: 20px; background: #e3f2fd; border-radius: 8px; text-align: left;'>");
        sb.AppendLine("<h3 style='margin: 0 0 15px 0; color: #1565c0;'>?? Información</h3>");
        sb.AppendLine("<ul style='margin: 0; padding-left: 20px; color: #0d47a1;'>");
        sb.AppendLine("<li style='margin: 10px 0;'>Este resumen se genera automáticamente todos los días</li>");
        sb.AppendLine("<li style='margin: 10px 0;'>Se incluyen alertas de nivel CRÍTICO y ALTO activas</li>");
        sb.AppendLine("<li style='margin: 10px 0;'>Recibirás notificación inmediata si surge alguna alerta</li>");
        sb.AppendLine("<li style='margin: 10px 0;'>El sistema monitorea solicitudes próximas a vencer (0, 1, 2 días)</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p><strong>? Estado: OPERATIVO</strong></p>");
        sb.AppendLine("<p>Sistema de Gestión de Alertas SLA TATA</p>");
        sb.AppendLine($"<p>Generado automáticamente el {DateTime.UtcNow:dd/MM/yyyy 'a las' HH:mm:ss} UTC</p>");
        sb.AppendLine("<p style='margin-top: 15px; font-size: 12px; color: #999;'>Este es un correo automático. Por favor, no respondas a este mensaje.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Envío de resumen diario a múltiples destinatarios seleccionados
    /// </summary>
    public async Task<EmailSummaryResponseDto> SendDailySummaryToRecipientsAsync(List<string> destinatarios)
    {
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [ENVÍO MÚLTIPLE] INICIANDO resumen diario a {Count} destinatarios", destinatarios.Count);
        _logger.LogCritical("=================================================================");

        if (destinatarios == null || !destinatarios.Any())
        {
            var error = "? No se proporcionaron destinatarios";
            _logger.LogWarning(error);
            return new EmailSummaryResponseDto
            {
                Exito = false,
                Mensaje = error,
                CantidadAlertas = 0,
                CorreoEnviado = false
            };
        }

        // Filtrar destinatarios válidos
        var destinatariosValidos = destinatarios
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!destinatariosValidos.Any())
        {
            var error = "? No se proporcionaron destinatarios válidos";
            _logger.LogWarning(error);
            return new EmailSummaryResponseDto
            {
                Exito = false,
                Mensaje = error,
                CantidadAlertas = 0,
                CorreoEnviado = false
            };
        }

        _logger.LogInformation("?? Destinatarios válidos: {Count}", destinatariosValidos.Count);
        foreach (var destinatario in destinatariosValidos)
        {
            _logger.LogInformation("   ?? {Email}", destinatario);
        }

        // PASO 1: Obtener alertas
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [PASO 1/3] Consultando alertas CRITICO/ALTO en BD");
        _logger.LogCritical("=================================================================");

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

        _logger.LogInformation("?? Consulta completada: {Count} alertas encontradas", alertasCriticas.Count);

        // GUARD CLAUSE: Validar si hay alertas antes de continuar
        if (!alertasCriticas.Any())
        {
            _logger.LogWarning("?? No se encontraron alertas para enviar");

            return new EmailSummaryResponseDto
            {
                Exito = true,
                Mensaje = "No se encontraron alertas para enviar",
                CantidadAlertas = 0,
                CorreoEnviado = false,
                Destinatarios = destinatariosValidos
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

        // PASO 2: Generar HTML
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [PASO 2/3] Generando HTML del resumen");
        _logger.LogCritical("=================================================================");

        string htmlBody;
        try
        {
            htmlBody = GenerarHtmlResumenDiario(alertasCriticas, hoy);
            _logger.LogInformation("? HTML generado: {Length} caracteres, {Bytes} bytes",
                htmlBody.Length,
                System.Text.Encoding.UTF8.GetByteCount(htmlBody));
        }
        catch (Exception ex)
        {
            var error = $"? Error al generar HTML: {ex.Message}";
            _logger.LogCritical(ex, error);
            return new EmailSummaryResponseDto
            {
                Exito = false,
                Mensaje = error,
                CantidadAlertas = alertasCriticas.Count,
                CorreoEnviado = false,
                Destinatarios = destinatariosValidos
            };
        }

        // PASO 3: Enviar a cada destinatario
        var asunto = $"[RESUMEN DIARIO SLA] {hoy:dd/MM/yyyy} - {alertasCriticas.Count} alertas críticas";

        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [PASO 3/3] Enviando correos a {Count} destinatarios", destinatariosValidos.Count);
        _logger.LogCritical("=================================================================");

        var resultados = new List<EnvioResultadoDto>();
        var exitosos = 0;
        var fallidos = 0;

        foreach (var destinatario in destinatariosValidos)
        {
            _logger.LogInformation("?? Enviando a: {Email}", destinatario);

            try
            {
                await _emailService.SendAsync(destinatario, asunto, htmlBody);

                exitosos++;
                resultados.Add(new EnvioResultadoDto
                {
                    Destinatario = destinatario,
                    Exitoso = true
                });

                _logger.LogInformation("? Enviado exitosamente a {Email}", destinatario);

                // Registrar en email_log individualmente
                await RegistrarEmailLog("RESUMEN_MULTIPLE", destinatario, "OK",
                    $"Resumen diario enviado exitosamente con {alertasCriticas.Count} alertas");
            }
            catch (Exception ex)
            {
                fallidos++;
                var errorMsg = $"{ex.GetType().Name}: {ex.Message}";

                resultados.Add(new EnvioResultadoDto
                {
                    Destinatario = destinatario,
                    Exitoso = false,
                    MensajeError = errorMsg
                });

                _logger.LogError(ex, "? Error al enviar a {Email}", destinatario);

                // Registrar error en email_log
                await RegistrarEmailLog("RESUMEN_MULTIPLE", destinatario, "ERROR", errorMsg);
            }
        }

        // Resumen final
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("? [COMPLETADO] Envío de resumen diario múltiple");
        _logger.LogCritical("=================================================================");
        _logger.LogInformation("?? Resumen:");
        _logger.LogInformation("   ? Exitosos: {Exitosos}", exitosos);
        _logger.LogInformation("   ? Fallidos: {Fallidos}", fallidos);
        _logger.LogInformation("   ?? Alertas incluidas: {Total}", alertasCriticas.Count);
        _logger.LogInformation("   ?? Destinatarios: {Count}", destinatariosValidos.Count);

        var mensajeFinal = exitosos == destinatariosValidos.Count
            ? $"Resumen enviado exitosamente a {exitosos} destinatario(s) con {alertasCriticas.Count} alertas"
            : $"Resumen enviado parcialmente: {exitosos} exitosos, {fallidos} fallidos de {destinatariosValidos.Count} destinatarios";

        return new EmailSummaryResponseDto
        {
            Exito = exitosos > 0,
            Mensaje = mensajeFinal,
            CantidadAlertas = alertasCriticas.Count,
            CorreoEnviado = exitosos > 0,
            Destinatarios = destinatariosValidos,
            ResultadosEnvios = resultados
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
    /// Envía correos a responsables cuando sus solicitudes están próximas a vencer (2, 1, 0 días)
    /// VERSIÓN COMPLETA con logs detallados y validaciones
    /// </summary>
    public async Task SendIndividualNotificationsAsync()
    {
        _logger.LogCritical("=================================================================");
        _logger.LogCritical("?? [NOTIFICACIONES INDIVIDUALES] INICIANDO proceso automático");
        _logger.LogCritical("=================================================================");

        try
        {
            // PASO 1: Obtener configuración desde EmailConfig (BD)
            _logger.LogInformation("?? [PASO 1/5] Consultando configuración de notificaciones...");
            var config = await _context.EmailConfig.FirstOrDefaultAsync();

            if (config == null)
            {
                _logger.LogWarning("?? No existe configuración en email_config. Abortando notificaciones individuales.");
                return;
            }

            if (!config.EnvioInmediato)
            {
                _logger.LogInformation("?? EnvioInmediato está DESACTIVADO en BD. No se enviarán notificaciones.");
                return;
            }

            _logger.LogInformation("? Configuración validada: EnvioInmediato = {Estado}", config.EnvioInmediato);

            // PASO 2: Obtener días para notificar desde appsettings (por defecto: 2, 1, 0)
            var diasParaNotificar = new List<int> { 2, 1, 0 };

            _logger.LogInformation("?? [PASO 2/5] Días para notificar: {Dias}", string.Join(", ", diasParaNotificar));

            // PASO 3: Buscar solicitudes ACTIVAS próximas a vencer
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 3/5] Buscando solicitudes próximas a vencer en BD...");
            _logger.LogCritical("=================================================================");

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var solicitudesProximasAVencer = await _context.Solicitud
                .AsNoTracking()
                .Include(s => s.IdPersonalNavigation)
                .Include(s => s.IdSlaNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Where(s => s.EstadoSolicitud == "ACTIVO" && 
                           s.FechaIngreso.HasValue &&
                           !string.IsNullOrWhiteSpace(s.IdPersonalNavigation!.CorreoCorporativo))
                .ToListAsync();

            _logger.LogInformation("?? Solicitudes ACTIVAS encontradas: {Total}", solicitudesProximasAVencer.Count);

            // Filtrar por días restantes
            var solicitudesParaNotificar = solicitudesProximasAVencer
                .Where(s =>
                {
                    if (!s.FechaIngreso.HasValue || s.IdSlaNavigation == null)
                        return false;

                    var fechaVencimiento = s.FechaIngreso.Value.AddDays(s.IdSlaNavigation.DiasUmbral);
                    var diasRestantes = (fechaVencimiento.ToDateTime(TimeOnly.MinValue) - hoy.ToDateTime(TimeOnly.MinValue)).Days;

                    return diasParaNotificar.Contains(diasRestantes);
                })
                .ToList();

            _logger.LogInformation("?? Solicitudes que requieren notificación: {Count}", solicitudesParaNotificar.Count);

            if (!solicitudesParaNotificar.Any())
            {
                _logger.LogInformation("? No hay solicitudes próximas a vencer (2, 1, 0 días). Proceso finalizado.");
                return;
            }

            // PASO 4: Agrupar por destinatario
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 4/5] Agrupando solicitudes por responsable...");
            _logger.LogCritical("=================================================================");

            var notificacionesPorResponsable = solicitudesParaNotificar
                .GroupBy(s => new
                {
                    Email = s.IdPersonalNavigation!.CorreoCorporativo,
                    NombreCompleto = $"{s.IdPersonalNavigation.Nombres} {s.IdPersonalNavigation.Apellidos}"
                })
                .ToList();

            _logger.LogInformation("?? Destinatarios únicos: {Count}", notificacionesPorResponsable.Count);

            // PASO 5: Enviar correos individuales
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("?? [PASO 5/5] Enviando notificaciones personalizadas...");
            _logger.LogCritical("=================================================================");

            var exitosos = 0;
            var fallidos = 0;
            var solicitudesNotificadas = 0;

            foreach (var grupo in notificacionesPorResponsable)
            {
                var destinatario = grupo.Key.Email!;
                var nombreResponsable = grupo.Key.NombreCompleto;
                var solicitudesDelResponsable = grupo.ToList();

                _logger.LogInformation("?? Procesando: {Nombre} ({Email}) - {Count} solicitud(es)", 
                    nombreResponsable, destinatario, solicitudesDelResponsable.Count);

                try
                {
                    // Generar HTML personalizado
                    var htmlBody = GenerarHtmlNotificacionIndividual(nombreResponsable, solicitudesDelResponsable, hoy);
                    
                    var cantidadSolicitudes = solicitudesDelResponsable.Count;
                    var asunto = cantidadSolicitudes == 1
                        ? $"?? [ALERTA SLA] Tienes 1 solicitud próxima a vencer"
                        : $"?? [ALERTA SLA] Tienes {cantidadSolicitudes} solicitudes próximas a vencer";

                    // Enviar correo
                    await _emailService.SendAsync(destinatario, asunto, htmlBody);

                    exitosos++;
                    solicitudesNotificadas += cantidadSolicitudes;

                    _logger.LogInformation("? Enviado exitosamente a {Email}", destinatario);

                    // Registrar en email_log
                    await RegistrarEmailLog("NOTIFICACION_INDIVIDUAL", destinatario, "OK",
                        $"Notificadas {cantidadSolicitudes} solicitud(es) próximas a vencer");
                }
                catch (Exception ex)
                {
                    fallidos++;
                    _logger.LogError(ex, "? Error al enviar a {Email}", destinatario);

                    await RegistrarEmailLog("NOTIFICACION_INDIVIDUAL", destinatario, "ERROR",
                        $"Error: {ex.Message}");
                }
            }

            // RESUMEN FINAL
            _logger.LogCritical("=================================================================");
            _logger.LogCritical("? [COMPLETADO] Notificaciones individuales finalizadas");
            _logger.LogCritical("=================================================================");
            _logger.LogInformation("?? Resumen:");
            _logger.LogInformation("   ? Correos exitosos: {Exitosos}", exitosos);
            _logger.LogInformation("   ? Correos fallidos: {Fallidos}", fallidos);
            _logger.LogInformation("   ?? Solicitudes notificadas: {Total}", solicitudesNotificadas);
            _logger.LogInformation("   ?? Destinatarios únicos: {Count}", notificacionesPorResponsable.Count);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "?? Error crítico en SendIndividualNotificationsAsync");
            _logger.LogCritical("Tipo: {Type}", ex.GetType().FullName);
            _logger.LogCritical("Mensaje: {Message}", ex.Message);
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
                    FotoUrl = null,
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
    /// Obtener usuarios administradores y analistas con sus correos
    /// IdRolSistema 1 = Administrador, 2 = Analista
    /// VERSIÓN CORREGIDA: Sin string.Format para evitar errores de traducción LINQ
    /// </summary>
    public async Task<List<UsuarioEmailDto>> GetAdministradoresYAnalistasAsync()
    {
        _logger.LogDebug("Obteniendo usuarios administradores y analistas");

        try
        {
            var usuarios = await _context.Usuario
                .AsNoTracking()
                .Include(u => u.IdRolSistemaNavigation)
                .Include(u => u.PersonalNavigation)
                .Where(u => u.Estado == "ACTIVO" && 
                           (u.IdRolSistema == 1 || u.IdRolSistema == 2) && // Administrador o Analista
                           u.PersonalNavigation != null &&
                           !string.IsNullOrWhiteSpace(u.PersonalNavigation.CorreoCorporativo))
                .Select(u => new UsuarioEmailDto
                {
                    IdUsuario = u.IdUsuario,
                    Username = u.Username,
                    CorreoCorporativo = u.PersonalNavigation.CorreoCorporativo,
                    IdRolSistema = u.IdRolSistema,
                    NombreRol = u.IdRolSistemaNavigation != null ? u.IdRolSistemaNavigation.Nombre : "Sin Rol",
                    // ? CORRECCIÓN: Usar concatenación simple en lugar de string.Format
                    NombreCompleto = u.PersonalNavigation != null 
                        ? (u.PersonalNavigation.Nombres ?? "") + " " + (u.PersonalNavigation.Apellidos ?? "")
                        : u.Username,
                    TieneCorreo = !string.IsNullOrWhiteSpace(u.PersonalNavigation.CorreoCorporativo)
                })
                .ToListAsync();

            // ? CORRECCIÓN: Limpiar espacios en blanco extra DESPUÉS de traer los datos de la BD (en memoria)
            foreach (var usuario in usuarios)
            {
                usuario.NombreCompleto = usuario.NombreCompleto?.Trim() ?? usuario.Username;
            }

            // ? CORRECCIÓN: Ordenar EN MEMORIA después de obtener los datos
            var usuariosOrdenados = usuarios
                .OrderBy(u => u.NombreRol)
                .ThenBy(u => u.NombreCompleto)
                .ToList();

            _logger.LogInformation("Se obtuvieron {Count} administradores y analistas activos", usuariosOrdenados.Count);

            return usuariosOrdenados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener administradores y analistas");
            throw;
        }
    }

    /// <summary>
    /// Genera HTML personalizado para notificación individual de solicitudes próximas a vencer
    /// </summary>
    private string GenerarHtmlNotificacionIndividual(string nombreResponsable, List<Solicitud> solicitudes, DateOnly hoy)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'><head><meta charset='UTF-8'><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine(".container { max-width: 800px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine(".header { background: linear-gradient(135deg, #f57c00 0%, #ff9800 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }");
        sb.AppendLine(".header h1 { margin: 0; font-size: 24px; }");
        sb.AppendLine(".header p { margin: 10px 0 0 0; opacity: 0.9; font-size: 14px; }");
        sb.AppendLine(".content { padding: 30px; }");
        sb.AppendLine(".greeting { font-size: 16px; margin-bottom: 20px; color: #333; }");
        sb.AppendLine(".card { border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; margin-bottom: 15px; background-color: #fafafa; }");
        sb.AppendLine(".card.urgent { border-left: 4px solid #d32f2f; background-color: #ffebee; }");
        sb.AppendLine(".card.warning { border-left: 4px solid #f57c00; background-color: #fff3e0; }");
        sb.AppendLine(".card.info { border-left: 4px solid #1976d2; background-color: #e3f2fd; }");
        sb.AppendLine(".card-header { font-size: 18px; font-weight: bold; margin-bottom: 10px; color: #333; }");
        sb.AppendLine(".card-detail { font-size: 14px; margin: 5px 0; color: #666; }");
        sb.AppendLine(".badge { display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 11px; font-weight: 600; margin-left: 10px; }");
        sb.AppendLine(".badge-urgent { background-color: #d32f2f; color: white; }");
        sb.AppendLine(".badge-warning { background-color: #f57c00; color: white; }");
        sb.AppendLine(".badge-info { background-color: #1976d2; color: white; }");
        sb.AppendLine(".footer { padding: 20px 30px; background-color: #f8f9fa; border-radius: 0 0 8px 8px; text-align: center; color: #666; font-size: 12px; }");
        sb.AppendLine(".footer strong { color: #d32f2f; }");
        sb.AppendLine("</style></head><body><div class='container'>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>?? Alerta de Vencimiento de Solicitudes SLA</h1>");
        sb.AppendLine($"<p>{DateTime.UtcNow:dddd, dd 'de' MMMM 'de' yyyy}</p>");
        sb.AppendLine("</div>");

        // Content
        sb.AppendLine("<div class='content'>");
        sb.AppendLine($"<p class='greeting'>Hola <strong>{nombreResponsable}</strong>,</p>");
        sb.AppendLine($"<p class='greeting'>Tienes <strong>{solicitudes.Count}</strong> solicitud(es) que están próximas a vencer:</p>");

        // Cards de solicitudes
        foreach (var solicitud in solicitudes.OrderBy(s => s.FechaIngreso!.Value.AddDays(s.IdSlaNavigation!.DiasUmbral)))
        {
            var fechaVencimiento = solicitud.FechaIngreso!.Value.AddDays(solicitud.IdSlaNavigation!.DiasUmbral);
            var diasRestantes = (fechaVencimiento.ToDateTime(TimeOnly.MinValue) - hoy.ToDateTime(TimeOnly.MinValue)).Days;

            var cardClass = diasRestantes == 0 ? "urgent" : (diasRestantes == 1 ? "warning" : "info");
            var badgeClass = diasRestantes == 0 ? "badge-urgent" : (diasRestantes == 1 ? "badge-warning" : "badge-info");
            var badgeText = diasRestantes == 0 ? "VENCE HOY" : (diasRestantes == 1 ? "Vence mañana" : $"Vence en {diasRestantes} días");

            sb.AppendLine($"<div class='card {cardClass}'>");
            sb.AppendLine($"<div class='card-header'>Solicitud #{solicitud.IdSolicitud} <span class='badge {badgeClass}'>{badgeText}</span></div>");
            sb.AppendLine($"<div class='card-detail'><strong>Tipo SLA:</strong> {solicitud.IdSlaNavigation.TipoSolicitud}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Código SLA:</strong> {solicitud.IdSlaNavigation.CodigoSla}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Rol:</strong> {solicitud.IdRolRegistroNavigation?.NombreRol ?? "N/A"}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Fecha de Solicitud:</strong> {solicitud.FechaSolicitud:dd/MM/yyyy}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Fecha de Ingreso:</strong> {solicitud.FechaIngreso:dd/MM/yyyy}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Fecha de Vencimiento:</strong> {fechaVencimiento:dd/MM/yyyy}</div>");
            sb.AppendLine($"<div class='card-detail'><strong>Días de umbral SLA:</strong> {solicitud.IdSlaNavigation.DiasUmbral}</div>");
            if (!string.IsNullOrWhiteSpace(solicitud.ResumenSla))
            {
                sb.AppendLine($"<div class='card-detail'><strong>Resumen:</strong> {solicitud.ResumenSla}</div>");
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p><strong>?? ACCIÓN REQUERIDA:</strong> Por favor, revisa estas solicitudes y toma las acciones necesarias antes del vencimiento.</p>");
        sb.AppendLine("<p>Este es un correo automático del Sistema de Gestión de Alertas SLA TATA.</p>");
        sb.AppendLine($"<p>Generado el {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div></body></html>");

        return sb.ToString();
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
