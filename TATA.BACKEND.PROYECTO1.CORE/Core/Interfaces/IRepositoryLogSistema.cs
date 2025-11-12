using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRepositoryLogSistema
    {
        Task<bool> AddAsync(LogSistema entity);
        Task<IEnumerable<LogSistema>> GetAllAsync();
        Task<LogSistema?> GetByIdAsync(long id);
        Task<bool> RemoveAsync(long id);
    }
}