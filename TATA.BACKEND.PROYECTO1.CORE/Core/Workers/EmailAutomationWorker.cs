using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Workers;

/// <summary>
/// Worker en segundo plano para automatización de correos
/// - Resumen diario a hora predefinida
/// - Notificaciones individuales cada hora
/// VERSIÓN CON LOGS DE DIAGNÓSTICO VERBOSE
/// </summary>
public class EmailAutomationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailAutomationWorker> _logger;
    private readonly EmailAutomationSettings _settings;
    private DateTime _ultimoResumenEnviado = DateTime.MinValue;
    private int _numeroIteracion = 0;

    public EmailAutomationWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailAutomationWorker> logger,
        IOptions<EmailAutomationSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;

        // ?? LOG VERBOSE: Configuración cargada en constructor
        _logger.LogWarning("?? [DIAGNÓSTICO] EmailAutomationWorker CONSTRUCTOR iniciado");
        _logger.LogWarning("?? [DIAGNÓSTICO] Settings inyectados: {@Settings}", new
        {
            _settings.HoraEnvioResumenDiario,
            _settings.EnviarResumenDiario,
            _settings.DestinatarioResumenDiario,
            _settings.EnviarNotificacionesIndividuales,
            DiasParaNotificar = string.Join(", ", _settings.DiasParaNotificar),
            _settings.IntervaloVerificacionMinutos
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogCritical("?? ========== [DIAGNÓSTICO] EmailAutomationWorker INICIANDO ExecuteAsync ==========");
        _logger.LogInformation("? EmailAutomationWorker iniciado correctamente");
        _logger.LogInformation("? Resumen diario configurado para las {Hora}", _settings.HoraEnvioResumenDiario);
        _logger.LogInformation("?? Notificaciones individuales habilitadas: {Habilitado}", 
            _settings.EnviarNotificacionesIndividuales);

        // ?? LOG VERBOSE: Estado inicial
        _logger.LogWarning("?? [DIAGNÓSTICO] Estado inicial del Worker:");
        _logger.LogWarning("??   - Hora actual (Local): {HoraLocal}", DateTime.Now);
        _logger.LogWarning("??   - Hora actual (UTC): {HoraUtc}", DateTime.UtcNow);
        _logger.LogWarning("??   - Zona horaria del servidor: {TimeZone}", TimeZoneInfo.Local.DisplayName);
        _logger.LogWarning("??   - Último resumen enviado: {UltimoResumen}", _ultimoResumenEnviado);

        // Esperar 30 segundos antes de iniciar
        _logger.LogWarning("?? [DIAGNÓSTICO] Esperando 30 segundos antes de iniciar ciclo...");
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        _logger.LogWarning("?? [DIAGNÓSTICO] Espera completada. Iniciando ciclo principal.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _numeroIteracion++;
            _logger.LogCritical("?? ========== [DIAGNÓSTICO] ITERACIÓN #{Num} INICIADA ==========", _numeroIteracion);

            try
            {
                var ahora = DateTime.Now;
                var ahoraUtc = DateTime.UtcNow;

                _logger.LogWarning("?? [DIAGNÓSTICO] Hora actual en iteración #{Num}:", _numeroIteracion);
                _logger.LogWarning("??   - DateTime.Now (Local): {HoraLocal}", ahora);
                _logger.LogWarning("??   - DateTime.UtcNow: {HoraUtc}", ahoraUtc);
                _logger.LogWarning("??   - TimeSpan actual (Local): {TimeSpan}", ahora.TimeOfDay);

                // ====== RESUMEN DIARIO ======
                _logger.LogWarning("?? [DIAGNÓSTICO] Evaluando envío de RESUMEN DIARIO...");
                _logger.LogWarning("??   - _settings.EnviarResumenDiario: {Estado}", _settings.EnviarResumenDiario);

                if (_settings.EnviarResumenDiario)
                {
                    _logger.LogWarning("?? [DIAGNÓSTICO] Resumen diario HABILITADO. Verificando hora...");

                    var horaConfiguracion = _settings.GetHoraEnvio();
                    var horaResumen = new TimeSpan(horaConfiguracion.Hours, horaConfiguracion.Minutes, 0);

                    _logger.LogWarning("?? [DIAGNÓSTICO] Hora configurada para resumen:");
                    _logger.LogWarning("??   - HoraEnvioResumenDiario (string): {HoraString}", _settings.HoraEnvioResumenDiario);
                    _logger.LogWarning("??   - TimeSpan parseado: {TimeSpan}", horaResumen);
                    _logger.LogWarning("??   - TimeSpan actual (Local): {TimeSpanActual}", ahora.TimeOfDay);

                    var esHora = EsHoraDeEnvio(ahora, horaResumen);
                    var seEnvioHoy = SeEnvioResumenHoy();

                    _logger.LogWarning("?? [DIAGNÓSTICO] Verificación de condiciones:");
                    _logger.LogWarning("??   - EsHoraDeEnvio(): {EsHora}", esHora);
                    _logger.LogWarning("??   - SeEnvioResumenHoy(): {SeEnvioHoy}", seEnvioHoy);
                    _logger.LogWarning("??   - Último resumen enviado: {UltimoResumen}", _ultimoResumenEnviado);
                    _logger.LogWarning("??   - Fecha del último resumen: {FechaUltimo}", _ultimoResumenEnviado.Date);
                    _logger.LogWarning("??   - Fecha actual: {FechaActual}", DateTime.Now.Date);

                    // Verificar si es la hora de enviar el resumen
                    if (esHora && !seEnvioHoy)
                    {
                        _logger.LogCritical("?? ========== [DIAGNÓSTICO] ¡CONDICIONES CUMPLIDAS! Iniciando envío de resumen ==========");
                        _logger.LogInformation("?? Es hora de enviar el resumen diario ({Hora})", 
                            _settings.HoraEnvioResumenDiario);
                        
                        await EnviarResumenDiarioAsync();
                        _ultimoResumenEnviado = ahora;

                        _logger.LogCritical("?? [DIAGNÓSTICO] Resumen marcado como enviado. _ultimoResumenEnviado actualizado a: {Fecha}", 
                            _ultimoResumenEnviado);
                    }
                    else
                    {
                        _logger.LogWarning("?? [DIAGNÓSTICO] Condiciones NO cumplidas para envío:");
                        if (!esHora)
                        {
                            var diferencia = Math.Abs((ahora.TimeOfDay - horaResumen).TotalMinutes);
                            _logger.LogWarning("??   ? NO es la hora (diferencia: {Diferencia:F2} minutos, margen permitido: ±5 minutos)", 
                                diferencia);
                        }
                        if (seEnvioHoy)
                        {
                            _logger.LogWarning("??   ? Ya se envió resumen HOY (último envío: {Fecha})", 
                                _ultimoResumenEnviado);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("?? [DIAGNÓSTICO] ?? Resumen diario DESHABILITADO en configuración");
                }

                // ====== NOTIFICACIONES INDIVIDUALES ======
                _logger.LogWarning("?? [DIAGNÓSTICO] Evaluando NOTIFICACIONES INDIVIDUALES...");
                _logger.LogWarning("??   - _settings.EnviarNotificacionesIndividuales: {Estado}", 
                    _settings.EnviarNotificacionesIndividuales);

                if (_settings.EnviarNotificacionesIndividuales)
                {
                    _logger.LogInformation("?? Verificando notificaciones individuales...");
                    _logger.LogWarning("?? [DIAGNÓSTICO] Notificaciones individuales HABILITADAS. Iniciando envío...");
                    
                    await EnviarNotificacionesIndividualesAsync();
                }
                else
                {
                    _logger.LogWarning("?? [DIAGNÓSTICO] ?? Notificaciones individuales DESHABILITADAS en configuración");
                }

                // Esperar el intervalo configurado
                var intervalo = TimeSpan.FromMinutes(_settings.IntervaloVerificacionMinutos);
                _logger.LogDebug("? Próxima verificación en {Minutos} minutos", 
                    _settings.IntervaloVerificacionMinutos);
                _logger.LogWarning("?? [DIAGNÓSTICO] Iteración #{Num} completada. Esperando {Minutos} minuto(s) para próxima verificación...", 
                    _numeroIteracion, _settings.IntervaloVerificacionMinutos);
                
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error en EmailAutomationWorker");
                _logger.LogCritical("?? [DIAGNÓSTICO] ? EXCEPCIÓN CAPTURADA en iteración #{Num}", _numeroIteracion);
                _logger.LogCritical("?? [DIAGNÓSTICO] Tipo de excepción: {Tipo}", ex.GetType().FullName);
                _logger.LogCritical("?? [DIAGNÓSTICO] Mensaje: {Mensaje}", ex.Message);
                _logger.LogCritical("?? [DIAGNÓSTICO] StackTrace: {StackTrace}", ex.StackTrace);
                
                // Esperar 5 minutos antes de reintentar
                _logger.LogWarning("?? [DIAGNÓSTICO] Esperando 5 minutos antes de reintentar...");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("?? EmailAutomationWorker detenido");
        _logger.LogCritical("?? [DIAGNÓSTICO] Worker detenido. Total de iteraciones ejecutadas: {Total}", _numeroIteracion);
    }

    /// <summary>
    /// Verifica si la hora actual coincide con la hora configurada (con margen de 5 minutos)
    /// </summary>
    private bool EsHoraDeEnvio(DateTime ahora, TimeSpan horaConfiguracion)
    {
        var horaActual = ahora.TimeOfDay;
        var diferencia = Math.Abs((horaActual - horaConfiguracion).TotalMinutes);
        
        _logger.LogWarning("?? [DIAGNÓSTICO] EsHoraDeEnvio() ejecutado:");
        _logger.LogWarning("??   - Hora actual: {HoraActual}", horaActual);
        _logger.LogWarning("??   - Hora configurada: {HoraConfig}", horaConfiguracion);
        _logger.LogWarning("??   - Diferencia (minutos): {Diferencia:F2}", diferencia);
        _logger.LogWarning("??   - Margen permitido: ±5 minutos");
        _logger.LogWarning("??   - Resultado: {Resultado}", diferencia <= 5);

        // Margen de ±5 minutos
        return diferencia <= 5;
    }

    /// <summary>
    /// Verifica si ya se envió el resumen hoy
    /// </summary>
    private bool SeEnvioResumenHoy()
    {
        var resultado = _ultimoResumenEnviado.Date == DateTime.UtcNow.Date;
        
        _logger.LogWarning("?? [DIAGNÓSTICO] SeEnvioResumenHoy() ejecutado:");
        _logger.LogWarning("??   - Último resumen enviado: {UltimoResumen}", _ultimoResumenEnviado);
        _logger.LogWarning("??   - Fecha del último resumen: {FechaUltimo}", _ultimoResumenEnviado.Date);
        _logger.LogWarning("??   - Fecha actual (UTC): {FechaActual}", DateTime.UtcNow.Date);
        _logger.LogWarning("??   - ¿Son iguales?: {Resultado}", resultado);

        return resultado;
    }

    /// <summary>
    /// Envía el resumen diario usando un scope de servicios
    /// </summary>
    private async Task EnviarResumenDiarioAsync()
    {
        _logger.LogCritical("?? ========== [DIAGNÓSTICO] EnviarResumenDiarioAsync() INICIADO ==========");

        try
        {
            _logger.LogWarning("?? [DIAGNÓSTICO] Creando nuevo Scope para servicios...");
            using var scope = _serviceProvider.CreateScope();
            _logger.LogWarning("?? [DIAGNÓSTICO] Scope creado exitosamente");

            _logger.LogWarning("?? [DIAGNÓSTICO] Obteniendo IEmailAutomationService del Scope...");
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailAutomationService>();
            _logger.LogWarning("?? [DIAGNÓSTICO] IEmailAutomationService obtenido: {Tipo}", emailService.GetType().FullName);

            // ?? LOG VERBOSE: Obtener IEmailConfigService para verificar configuración de BD
            _logger.LogWarning("?? [DIAGNÓSTICO] Obteniendo IEmailConfigService para verificar configuración de BD...");
            var emailConfigService = scope.ServiceProvider.GetRequiredService<IEmailConfigService>();
            var configBD = await emailConfigService.GetConfigAsync();

            if (configBD != null)
            {
                _logger.LogWarning("?? [DIAGNÓSTICO] Configuración de BD cargada: {@ConfigBD}", new
                {
                    configBD.Id,
                    configBD.ResumenDiario,
                    configBD.HoraResumen,
                    configBD.EnvioInmediato,
                    configBD.DestinatarioResumen
                });

                // Validación de destinatario
                if (string.IsNullOrWhiteSpace(configBD.DestinatarioResumen))
                {
                    _logger.LogCritical("?? [DIAGNÓSTICO] ? PROBLEMA DETECTADO: DestinatarioResumen en BD está VACÍO o NULL");
                }
                else
                {
                    _logger.LogWarning("?? [DIAGNÓSTICO] ? DestinatarioResumen en BD: {Destinatario}", 
                        configBD.DestinatarioResumen);
                }
            }
            else
            {
                _logger.LogCritical("?? [DIAGNÓSTICO] ? PROBLEMA DETECTADO: NO se encontró configuración en email_config (tabla BD)");
            }

            _logger.LogInformation("?? Enviando resumen diario...");
            _logger.LogWarning("?? [DIAGNÓSTICO] Llamando a emailService.SendDailySummaryAsync()...");
            
            await emailService.SendDailySummaryAsync();
            
            _logger.LogWarning("?? [DIAGNÓSTICO] SendDailySummaryAsync() completado SIN excepciones");
            _logger.LogInformation("? Resumen diario enviado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al enviar resumen diario");
            _logger.LogCritical("?? [DIAGNÓSTICO] ? EXCEPCIÓN en EnviarResumenDiarioAsync()");
            _logger.LogCritical("?? [DIAGNÓSTICO] Tipo: {Tipo}", ex.GetType().FullName);
            _logger.LogCritical("?? [DIAGNÓSTICO] Mensaje: {Mensaje}", ex.Message);
            _logger.LogCritical("?? [DIAGNÓSTICO] StackTrace: {StackTrace}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                _logger.LogCritical("?? [DIAGNÓSTICO] InnerException: {Inner}", ex.InnerException.Message);
            }
        }

        _logger.LogCritical("?? ========== [DIAGNÓSTICO] EnviarResumenDiarioAsync() FINALIZADO ==========");
    }

    /// <summary>
    /// Envía notificaciones individuales usando un scope de servicios
    /// </summary>
    private async Task EnviarNotificacionesIndividualesAsync()
    {
        _logger.LogCritical("?? ========== [DIAGNÓSTICO] EnviarNotificacionesIndividualesAsync() INICIADO ==========");

        try
        {
            _logger.LogWarning("?? [DIAGNÓSTICO] Creando nuevo Scope para notificaciones individuales...");
            using var scope = _serviceProvider.CreateScope();
            _logger.LogWarning("?? [DIAGNÓSTICO] Scope creado exitosamente");

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailAutomationService>();
            _logger.LogWarning("?? [DIAGNÓSTICO] IEmailAutomationService obtenido para notificaciones");

            _logger.LogWarning("?? [DIAGNÓSTICO] Llamando a SendIndividualNotificationsAsync()...");
            await emailService.SendIndividualNotificationsAsync();
            _logger.LogWarning("?? [DIAGNÓSTICO] SendIndividualNotificationsAsync() completado");

            _logger.LogInformation("? Notificaciones individuales procesadas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al enviar notificaciones individuales");
            _logger.LogCritical("?? [DIAGNÓSTICO] ? EXCEPCIÓN en EnviarNotificacionesIndividualesAsync()");
            _logger.LogCritical("?? [DIAGNÓSTICO] Tipo: {Tipo}", ex.GetType().FullName);
            _logger.LogCritical("?? [DIAGNÓSTICO] Mensaje: {Mensaje}", ex.Message);
        }

        _logger.LogCritical("?? ========== [DIAGNÓSTICO] EnviarNotificacionesIndividualesAsync() FINALIZADO ==========");
    }
}
