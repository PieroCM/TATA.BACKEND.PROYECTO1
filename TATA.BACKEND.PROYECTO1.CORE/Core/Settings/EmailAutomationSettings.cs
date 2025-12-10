namespace TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

/// <summary>
/// Configuración para automatización de envío de correos
/// </summary>
public class EmailAutomationSettings
{
    /// <summary>
    /// Hora del día para enviar el resumen diario (formato HH:mm)
    /// Ejemplo: "08:00" para las 8 AM
    /// </summary>
    public string HoraEnvioResumenDiario { get; set; } = "08:00";

    /// <summary>
    /// Habilita/deshabilita el envío automático del resumen diario
    /// </summary>
    public bool EnviarResumenDiario { get; set; } = true;

    /// <summary>
    /// Email del destinatario del resumen diario
    /// </summary>
    public string DestinatarioResumenDiario { get; set; } = "admin@tata.com";

    /// <summary>
    /// Habilita/deshabilita el envío de notificaciones individuales
    /// </summary>
    public bool EnviarNotificacionesIndividuales { get; set; } = true;

    /// <summary>
    /// Días antes del vencimiento para enviar notificaciones
    /// Ejemplo: [2, 1, 0] = Notificar cuando faltan 2, 1 o 0 días
    /// </summary>
    public List<int> DiasParaNotificar { get; set; } = new() { 2, 1, 0 };

    /// <summary>
    /// Intervalo en minutos para verificar si es hora de enviar correos
    /// Por defecto: 60 minutos (1 hora)
    /// </summary>
    public int IntervaloVerificacionMinutos { get; set; } = 60;

    /// <summary>
    /// Obtiene el TimeSpan de la hora de envío
    /// </summary>
    public TimeSpan GetHoraEnvio()
    {
        if (TimeSpan.TryParse(HoraEnvioResumenDiario, out var hora))
        {
            return hora;
        }
        return new TimeSpan(8, 0, 0); // Default: 8 AM
    }
}
