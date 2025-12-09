using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Workers;

/// <summary>
/// Background Worker que envía el resumen diario de alertas automáticamente
/// Utiliza Primary Constructor (.NET 9)
/// Se ejecuta cada 60 segundos y verifica la hora configurada
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
        _logger.LogInformation("?? DailySummaryWorker iniciado correctamente");

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
            // Obtener configuración
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

            var horaActual = DateTime.UtcNow.TimeOfDay;
            var horaResumen = config.HoraResumen;
            var hoy = DateTime.UtcNow.Date;

            // Verificar si estamos en la hora configurada (± 1 minuto de tolerancia)
            var diferencia = Math.Abs((horaActual - horaResumen).TotalMinutes);

            // Verificar si ya se envió hoy
            var yaEnviadoHoy = _lastExecutionDate.Date == hoy;

            if (diferencia <= 1.5 && !yaEnviadoHoy)
            {
                _logger.LogInformation(
                    "? Hora de envío alcanzada: {HoraActual} ? {HoraResumen}. Enviando resumen diario...",
                    horaActual.ToString(@"hh\:mm\:ss"),
                    horaResumen.ToString(@"hh\:mm\:ss"));

                try
                {
                    await emailAutomationService.SendDailySummaryAsync();
                    _lastExecutionDate = DateTime.UtcNow;

                    _logger.LogInformation("? Resumen diario enviado exitosamente a las {Hora}",
                        DateTime.UtcNow.ToString("HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error al enviar resumen diario");
                    // No actualizar _lastExecutionDate para reintentar mañana
                }
            }
            else
            {
                _logger.LogTrace(
                    "Verificación periódica: Hora actual {HoraActual}, Hora configurada {HoraResumen}, Diferencia {Diferencia:F2} min, Ya enviado hoy: {YaEnviado}",
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
