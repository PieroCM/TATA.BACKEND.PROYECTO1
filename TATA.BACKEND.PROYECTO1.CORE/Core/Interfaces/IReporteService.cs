using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IReporteService
    {
        Task AddAsync(Reporte entity);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Reporte>> GetAllAsync();
        Task<Reporte?> GetByIdAsync(int id);   // <- nullable para permitir 404
        Task<bool> UpdateAsync(Reporte existing);
    }
}