using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IAlertaService
    {
        Task<AlertaDto> CreateAsync(AlertaCreateDto dto);
        Task<List<AlertaDto>> GetAllAsync();
        Task<AlertaDto?> GetByIdAsync(int id);
        Task<AlertaDto?> UpdateAsync(int id, AlertaUpdateDto dto);
        Task<bool> DeleteAsync(int id);

    }
}