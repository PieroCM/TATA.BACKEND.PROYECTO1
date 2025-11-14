using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPermisoRepository
    {
        Task<List<Permiso>> GetAll();
        Task<Permiso?> GetById(int id);
        Task Add(Permiso entity);
        Task Update(Permiso entity);
        Task Delete(int id);
    }
}
