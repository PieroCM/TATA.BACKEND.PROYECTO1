using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IReporteDetalleRepository
    {
        Task<IEnumerable<ReporteDetalle>> GetAllAsync();
        Task<ReporteDetalle?> GetByIdsAsync(int idReporte, int idSolicitud);
        Task AddAsync(ReporteDetalle entity);
        Task UpdateAsync(ReporteDetalle entity);
        Task DeleteAsync(ReporteDetalle entity);

        // Validaciones en los services/controllers (devolver 404/409 más fácil), no son obligatorios
        Task<bool> ExistsAsync(int idReporte, int idSolicitud);
    }
}