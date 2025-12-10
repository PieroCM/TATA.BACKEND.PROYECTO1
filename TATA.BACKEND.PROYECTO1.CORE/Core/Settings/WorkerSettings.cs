namespace TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

/// <summary>
/// Configuración para los Background Workers del sistema
/// Se vincula con appsettings.json sección "WorkerSettings"
/// </summary>
public class WorkerSettings
{
    /// <summary>
    /// Intervalo en horas para la sincronización automática de alertas
    /// Por defecto: 6 horas
    /// </summary>
    public int AlertasSyncIntervalHours { get; set; } = 6;

    /// <summary>
    /// Habilitar o deshabilitar el worker de sincronización de alertas
    /// Por defecto: true
    /// </summary>
    public bool EnableAlertasSync { get; set; } = true;

    /// <summary>
    /// Ejecutar sincronización inmediata al iniciar la aplicación
    /// Por defecto: true
    /// </summary>
    public bool RunAlertasSyncOnStartup { get; set; } = true;
}
