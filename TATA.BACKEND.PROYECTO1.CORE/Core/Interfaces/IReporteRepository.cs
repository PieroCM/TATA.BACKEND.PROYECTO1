using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IReporteRepository
    {
        Task<IEnumerable<Reporte>> GetAllAsync();
        Task<Reporte?> GetByIdAsync(int id);
        Task AddAsync(Reporte reporte);
        Task UpdateAsync(Reporte reporte);
        Task DeleteAsync(Reporte reporte);

        // Validaciones en los services/controllers (devolver 404/409 más fácil), no son obligatorios
        Task<bool> ExistsAsync(int id);
    }
}