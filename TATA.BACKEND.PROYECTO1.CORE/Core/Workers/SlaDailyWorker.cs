using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Shared;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Workers;

/// <summary>
/// Background Worker dedicado EXCLUSIVAMENTE al recálculo diario de SLA.
/// Se ejecuta cada 60 segundos y verifica si debe recalcular el SLA.
/// 
/// COMPORTAMIENTO:
/// 1. Ejecución normal: a medianoche (00:00) hora Perú con tolerancia de ±1.5 min
/// 2. Catch-up: Si el backend estuvo apagado a medianoche, ejecuta al arrancar
/// 
/// GARANTÍA: Se ejecuta exactamente 1 vez por día calendario de Perú.
/// 
/// RESPONSABILIDAD ÚNICA: Recalcular SLA de solicitudes activas.
/// NO usa EmailConfig ni configuración de correos.
/// 
/// Separado de DailySummaryWorker que maneja alertas y correos.
/// </summary>
public class SlaDailyWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SlaDailyWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<SlaDailyWorker> _logger = logger;

    /// <summary>
    /// Fecha de última ejecución exitosa (en hora Perú).
    /// NOTA: Este valor está en memoria. Si el backend se reinicia, se pierde.
    /// Sin embargo, la lógica de catch-up asegura que se ejecute al menos 1 vez por día.
    /// </summary>
    private DateTime _lastExecutionDate = DateTime.MinValue;

    /// <summary>
    /// Hora fija de ejecución diaria: medianoche (00:00:00) hora Perú
    /// </summary>
    private static readonly TimeSpan SlaExecutionTime = new TimeSpan(0, 0, 0);

    /// <summary>
    /// Tolerancia en minutos alrededor de la hora objetivo
    /// </summary>
    private const double ToleranciaMinutos = 1.5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "?? SlaDailyWorker iniciado. Hora objetivo (Perú): {HoraObjetivo}. Zona horaria: {TimeZone}. " +
            "Modo catch-up habilitado: si el backend estuvo apagado a medianoche, ejecutará al arrancar.",
            SlaExecutionTime.ToString(@"hh\:mm\:ss"),
            PeruTimeProvider.TimeZoneName);

        // Pequeña espera inicial para dar tiempo a que arranquen otros servicios
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarYActualizarSlaAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SlaDailyWorker: Operación cancelada por token de detención");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error crítico en SlaDailyWorker");
            }

            // Verificar cada 60 segundos
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SlaDailyWorker: Detención solicitada durante el delay");
                break;
            }
        }

        _logger.LogInformation("?? SlaDailyWorker detenido");
    }

    /// <summary>
    /// Verifica si debe ejecutar el recálculo de SLA:
    /// 1. Ejecución normal: Si estamos en la ventana de medianoche (±1.5 min)
    /// 2. Catch-up: Si ya pasó la hora objetivo y aún no se ejecutó hoy
    /// </summary>
    private async Task VerificarYActualizarSlaAsync(CancellationToken stoppingToken)
    {
        var ahoraPeru = PeruTimeProvider.NowPeru;
        var hoyPeru = ahoraPeru.Date;
        var horaActual = ahoraPeru.TimeOfDay;

        // Verificar si ya se ejecutó hoy
        var yaEjecutadoHoy = _lastExecutionDate.Date == hoyPeru;

        // Hora objetivo fija: medianoche Perú
        var horaObjetivo = SlaExecutionTime;

        // Diferencia en minutos entre la hora actual y la hora objetivo
        var diferenciaMin = Math.Abs((horaActual - horaObjetivo).TotalMinutes);

        // ¿Estamos dentro de la ventana normal de ejecución? (medianoche ± tolerancia)
        var dentroDeVentana = diferenciaMin <= ToleranciaMinutos;

        // ???????????????????????????????????????????????????????????????????
        // LÓGICA DE CATCH-UP:
        // Si el backend estuvo apagado a medianoche, detectar que:
        // - Aún NO se ejecutó hoy (_lastExecutionDate.Date != hoyPeru)
        // - La hora actual ya pasó la ventana de medianoche
        // En ese caso, ejecutar inmediatamente como "catch-up"
        // ???????????????????????????????????????????????????????????????????
        var limiteCatchUp = horaObjetivo.Add(TimeSpan.FromMinutes(ToleranciaMinutos));
        var necesitaCatchUp = !yaEjecutadoHoy && horaActual > limiteCatchUp;

        // Log de verificación periódica (nivel Trace para no saturar)
        _logger.LogTrace(
            "SlaDailyWorker check: AhoraPeru={Ahora:yyyy-MM-dd HH:mm:ss}, HoraObjetivo={Objetivo}, " +
            "Diferencia={Dif:F2} min, DentroDeVentana={Dentro}, NecesitaCatchUp={CatchUp}, YaEjecutadoHoy={YaEjecutado}",
            ahoraPeru,
            horaObjetivo.ToString(@"hh\:mm\:ss"),
            diferenciaMin,
            dentroDeVentana,
            necesitaCatchUp,
            yaEjecutadoHoy);

        // Si no estamos en la ventana normal Y no necesitamos catch-up, salir
        if (!dentroDeVentana && !necesitaCatchUp)
        {
            return;
        }

        // ? Es momento de ejecutar el recálculo de SLA
        var motivoEjecucion = dentroDeVentana ? "ventana normal (medianoche)" : "CATCH-UP (backend estuvo apagado)";
        
        _logger.LogInformation(
            "? Ejecutando recálculo SLA por {Motivo}. Fecha objetivo: {Fecha:yyyy-MM-dd}. Hora actual Perú: {Hora:HH:mm:ss}",
            motivoEjecucion,
            hoyPeru,
            ahoraPeru);

        using var scope = _scopeFactory.CreateScope();
        var solicitudService = scope.ServiceProvider.GetRequiredService<ISolicitudService>();

        try
        {
            var totalActualizadas = await solicitudService.ActualizarSlaDiarioAsync(
                ahoraPeru,
                stoppingToken);

            // Marcar como ejecutado para evitar duplicados hoy
            _lastExecutionDate = ahoraPeru;

            _logger.LogInformation(
                "? Recálculo diario de SLA completado ({Motivo}). Total solicitudes actualizadas: {Total}. Fecha/Hora Perú: {FechaHora:yyyy-MM-dd HH:mm:ss}",
                motivoEjecucion,
                totalActualizadas,
                ahoraPeru);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error al ejecutar ActualizarSlaDiarioAsync en SlaDailyWorker ({Motivo})", motivoEjecucion);
            // NO actualizar _lastExecutionDate para reintentar en el siguiente ciclo
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("?? Señal de detención recibida para SlaDailyWorker");
        return base.StopAsync(stoppingToken);
    }
}
