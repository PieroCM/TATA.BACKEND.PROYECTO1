using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface ILogSistemaService
    {
        Task<bool> AddAsync(LogSistemaCreateDTO dto);
        Task<IEnumerable<LogSistemaDTO>> GetAllAsync();
        Task<LogSistemaDTO?> GetByIdAsync(long id);
        Task<bool> RemoveAsync(long id);
    }
}