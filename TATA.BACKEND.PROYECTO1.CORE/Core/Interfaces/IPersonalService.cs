using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPersonalService
    {
        Task<bool> CreateAsync(PersonalCreateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PersonalResponseDTO>> GetAllAsync();
        Task<PersonalResponseDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto);
    }
}