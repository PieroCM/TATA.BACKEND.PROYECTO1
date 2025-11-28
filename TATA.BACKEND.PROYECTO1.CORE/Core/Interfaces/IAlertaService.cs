using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IAlertaService
    {
        Task<AlertaDTO> CreateAsync(AlertaCreateDto dto);
        Task<List<AlertaDTO>> GetAllAsync();
        Task<AlertaDTO?> GetByIdAsync(int id);
        Task<AlertaDTO?> UpdateAsync(int id, AlertaUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}