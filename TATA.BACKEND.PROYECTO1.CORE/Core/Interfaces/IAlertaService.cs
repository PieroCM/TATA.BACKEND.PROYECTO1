using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IAlertaService
    {
        // ========== MÉTODOS DE NEGOCIO INTELIGENTE ==========
        
        /// <summary>
        /// Sincroniza y repara alertas desde las solicitudes existentes (UPSERT lógico)
        /// - Crea alertas nuevas para solicitudes sin alerta
        /// - Actualiza nivel/mensaje de alertas existentes si han cambiado
        /// </summary>
        Task SyncAlertasFromSolicitudesAsync();

        /// <summary>
        /// Obtiene datos enriquecidos y planos para el Dashboard del Frontend
        /// Incluye cálculos matemáticos, colores y estado procesado
        /// </summary>
        Task<List<AlertaDashboardDto>> GetAllDashboardAsync();

        // ========== MÉTODOS CRUD BÁSICOS (Mantenidos) ==========
        Task<AlertaDTO> CreateAsync(AlertaCreateDto dto);
        Task<List<AlertaDTO>> GetAllAsync();
        Task<AlertaDTO?> GetByIdAsync(int id);
        Task<AlertaDTO?> UpdateAsync(int id, AlertaUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}