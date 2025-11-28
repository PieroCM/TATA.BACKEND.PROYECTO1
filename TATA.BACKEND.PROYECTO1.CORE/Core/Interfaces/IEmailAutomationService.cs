using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IEmailAutomationService
    {
        // Dashboard
        Task<List<AlertaDashboardDto>> GetDashboardAlertsAsync();
        Task<List<AlertaDashboardFrontendDto>> GetDashboardAlertsFrontendAsync();
        
        // Configuración
        Task<EmailConfigDto?> GetConfigAsync();
        Task<EmailConfigDto> SaveConfigAsync(EmailConfigCreateUpdateDto dto);
        
        // Logs
        Task<List<EmailLogDto>> GetLogsAsync(int take = 50);
        
        // Envío manual (Broadcast)
        Task<EmailLogDto> SendBroadcastAsync(BroadcastRequestDto dto);
        
        // Resumen diario (usado por el background worker)
        Task SendDailyResumeAsync();
    }
}
