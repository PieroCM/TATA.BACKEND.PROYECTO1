using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    /// <summary>
    /// Background service que ejecuta el resumen diario de alertas
    /// Verifica cada minuto si debe enviar el resumen según la hora configurada
    /// </summary>
    public class DailyEmailResumeWorker : BackgroundService
    {
        private readonly ILogger<DailyEmailResumeWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private DateTime? _lastExecutionDate;

        public DailyEmailResumeWorker(
            ILogger<DailyEmailResumeWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyEmailResumeWorker iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendDailyResumeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en DailyEmailResumeWorker");
                }

                // Esperar 1 minuto antes de la siguiente verificación
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("DailyEmailResumeWorker detenido");
        }

        private async Task CheckAndSendDailyResumeAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<IEmailConfigRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailAutomationService>();

                var config = await configRepo.GetConfigAsync();
                
                // Verificar si está habilitado el resumen diario
                if (config == null || !config.ResumenDiario)
                {
                    return;
                }

                var now = DateTime.Now;
                var today = now.Date;
                var currentTime = now.TimeOfDay;
                var targetTime = config.HoraResumen;

                // Verificar si ya se ejecutó hoy
                if (_lastExecutionDate.HasValue && _lastExecutionDate.Value.Date == today)
                {
                    return;
                }

                // Verificar si la hora actual coincide con la hora configurada (±1 minuto de tolerancia)
                var timeDifference = Math.Abs((currentTime - targetTime).TotalMinutes);
                
                if (timeDifference <= 1)
                {
                    _logger.LogInformation($"Enviando resumen diario a las {now:HH:mm:ss}");
                    
                    try
                    {
                        await emailService.SendDailyResumeAsync();
                        _lastExecutionDate = today;
                        _logger.LogInformation("Resumen diario enviado exitosamente");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar resumen diario");
                    }
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                // Error 208 = Invalid object name (tabla no existe)
                _logger.LogWarning("Las tablas de email automation no existen. Por favor ejecute el script de migración AddEmailAutomation.sql");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al verificar resumen diario");
            }
        }
    }
}
