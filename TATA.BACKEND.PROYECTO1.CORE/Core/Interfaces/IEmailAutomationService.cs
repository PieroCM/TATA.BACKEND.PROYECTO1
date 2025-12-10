using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IEmailAutomationService
    {
        /// <summary>
        /// Envío masivo de correos según filtros
        /// </summary>
        Task SendBroadcastAsync(BroadcastDto dto);

        /// <summary>
        /// Envío automático del resumen diario
        /// Retorna información sobre el resultado del envío
        /// </summary>
        Task<EmailSummaryResponseDto> SendDailySummaryAsync();

        /// <summary>
        /// Envío automático de notificaciones individuales personalizadas
        /// (Método utilizado por EmailAutomationWorker)
        /// </summary>
        Task SendIndividualNotificationsAsync();

        /// <summary>
        /// Envío individual de notificación (para botón del Dashboard)
        /// </summary>
        Task SendIndividualNotificationAsync(string destinatario, string asunto, string cuerpoHtml);

        /// <summary>
        /// Obtener últimos 100 logs de envío
        /// </summary>
        Task<List<EmailLog>> GetLogsAsync();

        /// <summary>
        /// Obtener lista de destinatarios según filtros (Preview)
        /// </summary>
        Task<List<DestinatarioPreviewDto>> GetDestinatariosPreviewAsync(int? idRol, int? idSla);

        /// <summary>
        /// Obtener roles activos para selectores
        /// </summary>
        Task<List<RolSelectorDto>> GetRolesActivosAsync();

        /// <summary>
        /// Obtener SLAs activos para selectores
        /// </summary>
        Task<List<SlaSelectorDto>> GetSlasActivosAsync();
    }
}
