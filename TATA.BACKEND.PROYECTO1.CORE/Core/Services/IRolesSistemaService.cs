using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public interface IRolesSistemaService
    {
        Task<List<RolesSistemaResponseDTO>> GetAll();
        Task<RolesSistemaResponseDTO?> GetById(int id);
        Task<RolesSistemaResponseDTO> Create(RolesSistemaCreateDTO dto);
        Task<bool> Update(int id, RolesSistemaUpdateDTO dto);
        Task<bool> Delete(int id);
    }
}
