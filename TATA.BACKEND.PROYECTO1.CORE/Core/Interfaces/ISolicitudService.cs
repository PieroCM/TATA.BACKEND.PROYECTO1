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
    }
}