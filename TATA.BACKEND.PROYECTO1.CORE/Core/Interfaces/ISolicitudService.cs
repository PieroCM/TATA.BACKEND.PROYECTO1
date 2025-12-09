using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface ISolicitudService
    {
        Task<SolicitudDto> CreateAsync(SolicitudCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<SolicitudDto>> GetAllAsync();
        Task<SolicitudDto?> GetByIdAsync(int id);
        Task<SolicitudDto?> UpdateAsync(int id, SolicitudUpdateDto dto);
        
        /// <summary>
        /// Actualiza el SLA diario de todas las solicitudes activas o en proceso.
        /// Se ejecuta automáticamente por el DailySummaryWorker.
        /// </summary>
        /// <param name="fechaReferenciaPeru">Fecha de referencia en zona horaria de Perú para el cálculo de SLA</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de solicitudes actualizadas</returns>
        Task<int> ActualizarSlaDiarioAsync(DateTime fechaReferenciaPeru, CancellationToken cancellationToken = default);
    }
}