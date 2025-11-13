using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public interface IRolPermisoService
    {
        Task<IEnumerable<RolPermisoEntity>> GetAllAsync();
        Task<IEnumerable<object>> GetAllWithNamesAsync();
        Task<RolPermisoEntity?> GetByIdsAsync(int idRolSistema, int idPermiso);
        Task<bool> AddAsync(RolPermisoEntity entity);
        Task<bool> UpdateAsync(int idRolSistema, int idPermiso, RolPermisoEntity entity);
        Task<bool> RemoveAsync(int idRolSistema, int idPermiso);
    }
}