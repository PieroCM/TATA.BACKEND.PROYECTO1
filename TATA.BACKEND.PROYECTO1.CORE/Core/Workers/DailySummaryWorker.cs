using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Shared;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Workers;

/// <summary>
/// Background Worker dedicado EXCLUSIVAMENTE al envío del resumen diario de alertas por correo.
/// Se ejecuta cada 60 segundos y verifica la hora configurada en EmailConfig.HoraResumen.
/// 
/// RESPONSABILIDAD ÚNICA: Enviar resumen de alertas por email.
/// NO hace recálculo de SLA (eso lo hace SlaDailyWorker).
/// 
/// Usa la zona horaria de Perú (UTC-5) para todas las operaciones de tiempo.
/// </summary>
public class DailySummaryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DailySummaryWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<DailySummaryWorker> _logger = logger;
    private DateTime _lastExecutionDate = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "?? DailySummaryWorker iniciado correctamente. Zona horaria: {TimeZone}. " +
            "NOTA: Este worker solo envía resúmenes de alertas por email. El recálculo de SLA lo hace SlaDailyWorker.",
            PeruTimeProvider.TimeZoneName);

        // Esperar 10 segundos antes de comenzar para asegurar que todos los servicios estén listos
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarYEnviarResumenAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DailySummaryWorker: Operación cancelada por token de detención");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error crítico en DailySummaryWorker");
            }

            // Esperar 60 segundos antes de la siguiente verificación
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DailySummaryWorker: Detención solicitada durante el delay");
                break;
            }
        }

        _logger.LogInformation("?? DailySummaryWorker detenido");
    }

    private async Task VerificarYEnviarResumenAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Proyecto1SlaDbContext>();
        var emailAutomationService = scope.ServiceProvider.GetRequiredService<IEmailAutomationService>();

        try
        {
            // Obtener configuración de email
            var config = await context.EmailConfig
                .AsNoTracking()
                .FirstOrDefaultAsync(stoppingToken);

            if (config == null)
            {
                _logger.LogDebug("No hay configuración de email disponible");
                return;
            }

            if (!config.ResumenDiario)
            {
                _logger.LogDebug("Resumen diario deshabilitado en la configuración");
                return;
            }

            // Usar hora de Perú para la comparación
            var ahoraPeru = PeruTimeProvider.NowPeru;
            var horaActual = ahoraPeru.TimeOfDay;
            var horaResumen = config.HoraResumen;
            var hoy = ahoraPeru.Date;

            // Verificar si estamos en la hora configurada (± 1.5 minutos de tolerancia)
            var diferencia = Math.Abs((horaActual - horaResumen).TotalMinutes);

            // Verificar si ya se envió hoy (usando fecha de Perú)
            var yaEnviadoHoy = _lastExecutionDate.Date == hoy;

            if (diferencia <= 1.5 && !yaEnviadoHoy)
            {
                _logger.LogInformation(
                    "? Hora de envío de resumen alcanzada en Perú: {HoraActual} ? {HoraResumen}. Enviando resumen de alertas...",
                    horaActual.ToString(@"hh\:mm\:ss"),
                    horaResumen.ToString(@"hh\:mm\:ss"));

                try
                {
                    // ??????????????????????????????????????????????????????????????
                    // RESPONSABILIDAD ÚNICA: Solo enviar el resumen de alertas
                    // El recálculo de SLA lo hace SlaDailyWorker a medianoche
                    // ??????????????????????????????????????????????????????????????
                    await emailAutomationService.SendDailySummaryAsync();
                    _lastExecutionDate = ahoraPeru; // Guardar fecha de Perú

                    _logger.LogInformation(
                        "?? Resumen diario de alertas enviado exitosamente a las {Hora} (Hora Perú)",
                        ahoraPeru.ToString("HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error al enviar resumen diario de alertas");
                    // No actualizar _lastExecutionDate para reintentar mañana
                }
            }
            else
            {
                _logger.LogTrace(
                    "Verificación periódica (Hora Perú): Actual {HoraActual}, Configurada {HoraResumen}, Diferencia {Diferencia:F2} min, Ya enviado hoy: {YaEnviado}",
                    horaActual.ToString(@"hh\:mm\:ss"),
                    horaResumen.ToString(@"hh\:mm\:ss"),
                    diferencia,
                    yaEnviadoHoy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la verificación del resumen diario");
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("?? Señal de detención recibida para DailySummaryWorker");
        return base.StopAsync(stoppingToken);
    }
}
