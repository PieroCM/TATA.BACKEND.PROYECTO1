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
        /// Retorna un objeto con el resultado de la operación y mensaje dinámico
        /// </summary>
        Task<EmailSummaryResponseDto> SendDailySummaryAsync();

        /// <summary>
        /// Envío individual de notificación (para botón del Dashboard)
        /// </summary>
        Task SendIndividualNotificationAsync(string destinatario, string asunto, string cuerpoHtml);

        /// <summary>
        /// Obtener últimos 100 logs de envío
        /// </summary>
        Task<List<EmailLog>> GetLogsAsync();
    }
}
