using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IConfigSlaService
    {
        Task<int> CreateAsync(int? id, ConfigSlaCreateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> DisableAsync(int id);
        Task<IEnumerable<ConfigSlaDTO>> GetAllAsync(bool soloActivos = true);
        Task<ConfigSlaDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, ConfigSlaUpdateDTO dto);

    }
}