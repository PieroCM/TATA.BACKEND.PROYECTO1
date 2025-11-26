using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    /// <summary>
    /// Background service que actualiza las alertas de solicitudes existentes diariamente
    /// - Calcula días restantes para cada solicitud activa
    /// - Crea o actualiza alertas según el estado actual
    /// - Envía correos automáticamente para alertas críticas
    /// </summary>
    public class DailyAlertUpdateWorker : BackgroundService
    {
        private readonly ILogger<DailyAlertUpdateWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private DateTime? _lastExecutionDate;

        public DailyAlertUpdateWorker(
            ILogger<DailyAlertUpdateWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("?? DailyAlertUpdateWorker iniciado - Actualización automática de alertas habilitada");

            // Esperar 2 minutos antes de la primera ejecución
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateAlertsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error en DailyAlertUpdateWorker");
                }

                // Ejecutar cada 6 horas (ajustable según necesidad)
                // Para pruebas, puedes cambiar a TimeSpan.FromMinutes(30)
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }

            _logger.LogInformation("?? DailyAlertUpdateWorker detenido");
        }

        private async Task CheckAndUpdateAlertsAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;

            // Verificar si ya se ejecutó hoy
            if (_lastExecutionDate.HasValue && _lastExecutionDate.Value.Date == today)
            {
                _logger.LogInformation("?? Ya se ejecutó la actualización hoy. Próxima ejecución en 6 horas.");
                return;
            }

            _logger.LogInformation($"?? Iniciando actualización de alertas: {now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var solicitudRepo = scope.ServiceProvider.GetRequiredService<ISolicitudRepository>();
                var alertaRepo = scope.ServiceProvider.GetRequiredService<IAlertaRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Obtener todas las solicitudes activas
                var solicitudes = await solicitudRepo.GetSolicitudsAsync();
                var solicitudesActivas = solicitudes.Where(s => s.EstadoSolicitud == "ACTIVO").ToList();

                _logger.LogInformation($"?? Solicitudes activas encontradas: {solicitudesActivas.Count}");

                int alertasCreadas = 0;
                int alertasActualizadas = 0;
                int correosEnviados = 0;
                int errores = 0;

                foreach (var solicitud in solicitudesActivas)
                {
                    try
                    {
                        // Validar que tenga las relaciones necesarias
                        if (solicitud.IdSlaNavigation == null)
                        {
                            _logger.LogWarning($"?? Solicitud #{solicitud.IdSolicitud} sin ConfigSLA asociado");
                            continue;
                        }

                        // Calcular días restantes ACTUALES
                        var configSla = solicitud.IdSlaNavigation;
                        var fechaInicio = solicitud.FechaIngreso ?? solicitud.FechaSolicitud;
                        var diasUmbral = configSla.DiasUmbral;
                        var fechaVencimiento = fechaInicio.AddDays(diasUmbral);
                        var hoy = DateOnly.FromDateTime(today);
                        
                        var diasTranscurridos = hoy.DayNumber - fechaInicio.DayNumber;
                        var diasRestantes = fechaVencimiento.DayNumber - hoy.DayNumber;

                        // Determinar nivel de alerta según días restantes
                        string nivelAlerta;
                        bool esCritico = false;

                        if (diasRestantes < 0)
                        {
                            nivelAlerta = "CRITICO";
                            esCritico = true;
                        }
                        else if (diasRestantes <= 2)
                        {
                            nivelAlerta = "CRITICO";
                            esCritico = true;
                        }
                        else if (diasRestantes <= 5)
                        {
                            nivelAlerta = "ALTO";
                        }
                        else
                        {
                            nivelAlerta = "MEDIO";
                        }

                        // Generar mensaje descriptivo
                        var mensaje = diasRestantes < 0
                            ? $"?? URGENTE: Solicitud #{solicitud.IdSolicitud} VENCIDA. Se excedió el SLA por {Math.Abs(diasRestantes)} día(s)."
                            : diasRestantes == 0
                                ? $"?? ATENCIÓN: Solicitud #{solicitud.IdSolicitud} vence HOY. Requiere acción inmediata."
                                : diasRestantes <= 2
                                    ? $"?? CRÍTICO: Solicitud #{solicitud.IdSolicitud} está cerca de vencer el SLA. Quedan solo {diasRestantes} día(s)."
                                    : $"Solicitud #{solicitud.IdSolicitud} en seguimiento. Vencimiento en {diasRestantes} día(s) (SLA: {diasUmbral} días).";

                        // Buscar alerta existente de tipo ACTUALIZACION_DIARIA para esta solicitud
                        var alertas = await alertaRepo.GetAlertasAsync();
                        var alertaExistente = alertas.FirstOrDefault(a => 
                            a.IdSolicitud == solicitud.IdSolicitud && 
                            a.Estado != "ELIMINADA" &&
                            a.TipoAlerta == "ACTUALIZACION_DIARIA");

                        bool enviarCorreo = false;

                        if (alertaExistente == null)
                        {
                            // Crear nueva alerta
                            var nuevaAlerta = new Alerta
                            {
                                IdSolicitud = solicitud.IdSolicitud,
                                TipoAlerta = "ACTUALIZACION_DIARIA",
                                Nivel = nivelAlerta,
                                Mensaje = mensaje,
                                Estado = "NUEVA",
                                EnviadoEmail = false,
                                FechaCreacion = DateTime.UtcNow
                            };

                            await alertaRepo.CreateAlertaAsync(nuevaAlerta);
                            alertasCreadas++;
                            
                            // Enviar correo si es crítico
                            if (esCritico)
                            {
                                enviarCorreo = true;
                                alertaExistente = nuevaAlerta; // Para usar después
                            }

                            _logger.LogInformation($"? Alerta creada para solicitud #{solicitud.IdSolicitud} - Nivel: {nivelAlerta}, Días restantes: {diasRestantes}");
                        }
                        else
                        {
                            // Actualizar alerta existente solo si cambió el nivel o es crítico sin correo enviado
                            bool nivelCambio = alertaExistente.Nivel != nivelAlerta;
                            bool seVolveCritico = !alertaExistente.EnviadoEmail && esCritico;

                            if (nivelCambio || seVolveCritico)
                            {
                                alertaExistente.Nivel = nivelAlerta;
                                alertaExistente.Mensaje = mensaje;
                                alertaExistente.ActualizadoEn = DateTime.UtcNow;

                                await alertaRepo.UpdateAlertaAsync(alertaExistente.IdAlerta, alertaExistente);
                                alertasActualizadas++;

                                // Enviar correo si se volvió crítico y no se ha enviado
                                if (seVolveCritico)
                                {
                                    enviarCorreo = true;
                                }

                                _logger.LogInformation($"?? Alerta actualizada para solicitud #{solicitud.IdSolicitud} - Nivel: {nivelAlerta}, Días restantes: {diasRestantes}");
                            }
                        }

                        // Enviar correo si es necesario
                        if (enviarCorreo)
                        {
                            // Obtener el correo del personal responsable
                            var destinatario = solicitud.IdPersonalNavigation?.CorreoCorporativo;

                            if (!string.IsNullOrWhiteSpace(destinatario))
                            {
                                try
                                {
                                    var subject = $"?? [ALERTA CRÍTICA SLA] Solicitud #{solicitud.IdSolicitud} requiere atención URGENTE";
                                    
                                    // Obtener la alerta completa con todas las relaciones para el template
                                    var alertaParaEmail = await alertaRepo.GetAlertaByIdAsync(
                                        alertaExistente?.IdAlerta ?? 0
                                    );

                                    if (alertaParaEmail != null)
                                    {
                                        var body = EmailTemplates.BuildAlertaBody(alertaParaEmail);
                                        await emailService.SendAsync(destinatario, subject, body);

                                        // Marcar como enviado
                                        alertaParaEmail.EnviadoEmail = true;
                                        alertaParaEmail.ActualizadoEn = DateTime.UtcNow;
                                        await alertaRepo.UpdateAlertaAsync(alertaParaEmail.IdAlerta, alertaParaEmail);

                                        correosEnviados++;
                                        _logger.LogInformation($"?? Correo crítico enviado a {destinatario} para solicitud #{solicitud.IdSolicitud}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"?? Error al enviar correo a {destinatario} para solicitud #{solicitud.IdSolicitud}: {ex.Message}");
                                    errores++;
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"?? Solicitud #{solicitud.IdSolicitud} no tiene correo de personal asignado");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"? Error al procesar solicitud #{solicitud.IdSolicitud}");
                        errores++;
                    }
                }

                _lastExecutionDate = today;

                _logger.LogInformation($"? Actualización completada - Creadas: {alertasCreadas}, Actualizadas: {alertasActualizadas}, Correos: {correosEnviados}, Errores: {errores}");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
            {
                _logger.LogWarning("?? Las tablas necesarias no existen. Verifique la migración de la base de datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error inesperado al actualizar alertas");
            }
        }
    }
}
