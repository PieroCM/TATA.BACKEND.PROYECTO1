using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRepositoryConfigSLA
    {
        IEnumerable<ConfigSla> GetAll();                // sync
        Task<IEnumerable<ConfigSla>> GetAllAsync();     // async
        Task<ConfigSla?> GetByIdAsync(int id);

        Task<int> InsertAsync(ConfigSla entity);
        Task<bool> UpdateAsync(ConfigSla entity);
        Task<bool> DeleteAsync(int id);                 // borrado físico
        Task<bool> DeleteLogicAsync(int id);            // es_activo = 0
    }
}
