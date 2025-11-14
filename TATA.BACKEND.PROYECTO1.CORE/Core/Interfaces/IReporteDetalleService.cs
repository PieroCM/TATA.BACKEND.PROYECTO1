using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IReporteDetalleService
    {
        Task<IEnumerable<ReporteDetalle>> GetAllAsync();
        Task<ReporteDetalle?> GetByIdsAsync(int idReporte, int idSolicitud);
        Task<bool> AddAsync(ReporteDetalle entity);
        Task<bool> UpdateAsync(ReporteDetalle entity);
        Task<bool> DeleteAsync(int idReporte, int idSolicitud);
    }
}