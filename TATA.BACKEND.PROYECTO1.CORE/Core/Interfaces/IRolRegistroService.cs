using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRolRegistroService
    {
        Task<IEnumerable<RolRegistroDTO>> GetAllAsync(bool soloActivos = true);
        Task<RolRegistroDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(int? id, RolRegistroCreateDTO dto);
        Task<bool> UpdateAsync(int id, RolRegistroUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> DisableAsync(int id);
        Task<bool> ExistsByNombreAsync(string nombreRol);
    }
}
