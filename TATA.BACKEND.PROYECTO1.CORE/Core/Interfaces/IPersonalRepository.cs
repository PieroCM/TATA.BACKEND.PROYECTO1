using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPersonalRepository
    {
        Task AddAsync(Personal personal);
        Task DeleteAsync(Personal personal);
        Task<IEnumerable<Personal>> GetAllAsync();
        Task<Personal?> GetByIdAsync(int id);
        Task UpdateAsync(Personal personal);
    }
}