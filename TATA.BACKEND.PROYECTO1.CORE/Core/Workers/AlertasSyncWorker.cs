using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Workers;

/// <summary>
/// Background Worker que sincroniza alertas automáticamente desde las solicitudes
/// Utiliza PeriodicTimer (.NET 9) y Primary Constructor
/// Configurable mediante appsettings.json (WorkerSettings)
/// </summary>
public class AlertasSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerSettings> workerSettings,
    ILogger<AlertasSyncWorker> logger) : BackgroundService
{
    private readonly WorkerSettings _settings = workerSettings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Verificar si el worker está habilitado en la configuración
        if (!_settings.EnableAlertasSync)
        {
            logger.LogWarning("?? AlertasSyncWorker: Worker DESHABILITADO en appsettings.json (EnableAlertasSync = false)");
            return;
        }

        var intervalHoras = _settings.AlertasSyncIntervalHours;
        logger.LogInformation(
            "?? Worker .NET 9: Sincronización de Alertas INICIADA. Ciclo: cada {Horas} horas (Configurable en appsettings.json)",
            intervalHoras);

        // PeriodicTimer es lo mejor en .NET 9 para tareas repetitivas asíncronas (sin drift de tiempo)
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHoras));

        // Ejecutar inmediatamente al arrancar si está configurado
        if (_settings.RunAlertasSyncOnStartup)
        {
            await DoWorkAsync(stoppingToken);
        }
        else
        {
            logger.LogInformation("? Primera sincronización programada en {Horas} horas (RunAlertasSyncOnStartup = false)", intervalHoras);
        }

        // El bucle espera aquí de forma eficiente (sin gastar CPU)
        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync(stoppingToken);
        }

        logger.LogInformation("?? AlertasSyncWorker: Worker detenido correctamente");
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("?? [Sync] Iniciando actualización automática de tabla Alertas...");

            // IMPORTANTE: Crear un Scope porque BackgroundService es Singleton y DbContext es Scoped
            using var scope = scopeFactory.CreateScope();

            var alertaService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAlertaService>();

            // Ejecución directa a la lógica de negocio (Cero latencia de red, no usa HttpClient)
            await alertaService.SyncAlertasFromSolicitudesAsync();

            logger.LogInformation("? [Sync] Sincronización finalizada correctamente a las {Hora} UTC",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("?? [Sync] Operación cancelada por token de detención");
            throw; // Propagar para detener el worker correctamente
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? [Sync] Error crítico en la sincronización. Se reintentará en el siguiente ciclo ({Horas} horas).", _settings.AlertasSyncIntervalHours);
            // NO propagar la excepción para que el Worker continúe ejecutándose
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogWarning("?? Señal de detención recibida para AlertasSyncWorker");
        return base.StopAsync(stoppingToken);
    }
}
