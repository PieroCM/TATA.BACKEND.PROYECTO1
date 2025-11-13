using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPermisoService
    {
        Task<List<PermisoResponseDTO>> GetAll();
        Task<PermisoResponseDTO?> GetById(int id);
        Task<PermisoResponseDTO> Create(PermisoCreateDTO dto);
        Task<bool> Update(int id, PermisoUpdateDTO dto);
        Task<bool> Delete(int id);
    }
}
