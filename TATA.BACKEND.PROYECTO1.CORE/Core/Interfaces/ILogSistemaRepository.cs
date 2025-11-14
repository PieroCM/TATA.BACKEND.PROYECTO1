using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface ILogSistemaRepository
    {
        Task<bool> AddAsync(LogSistema entity);
        Task<IEnumerable<LogSistema>> GetAllAsync();
        Task<LogSistema?> GetByIdAsync(long id);
        Task<bool> RemoveAsync(long id);
    }
}