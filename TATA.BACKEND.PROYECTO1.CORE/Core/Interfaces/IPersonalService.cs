using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPersonalService
    {
        Task<bool> CreateAsync(PersonalCreateDTO dto);
        
        // ⚠️ NUEVO: Crear Personal con cuenta de usuario condicional
        Task<bool> CreateWithAccountAsync(PersonalCreateWithAccountDTO dto);
        
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PersonalResponseDTO>> GetAllAsync();
        Task<PersonalResponseDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto);
    }
}