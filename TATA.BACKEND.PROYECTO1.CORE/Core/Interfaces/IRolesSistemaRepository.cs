using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRolesSistemaRepository
    {
        Task<List<RolesSistema>> GetAll();
        Task<RolesSistema?> GetById(int id);
        Task Add(RolesSistema entity);
        Task Update(RolesSistema entity);
        Task Delete(int id);
    }
}
