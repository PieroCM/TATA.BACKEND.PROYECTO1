using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRolRegistroRepository
    {
        IEnumerable<RolRegistro> GetAll();
        Task<IEnumerable<RolRegistro>> GetAllAsync();
        Task<RolRegistro?> GetByIdAsync(int id);
        Task<int> InsertAsync(RolRegistro entity);
        Task<bool> UpdateAsync(RolRegistro entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteLogicAsync(int id);
        Task<bool> ExistsByNombreAsync(string nombreRol);
    }
}
