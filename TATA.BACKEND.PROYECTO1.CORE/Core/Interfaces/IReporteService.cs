using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IReporteService
    {
        Task AddAsync(Reporte entity);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Reporte>> GetAllAsync();
        Task<Reporte?> GetByIdAsync(int id);   // <- nullable para permitir 404
        Task<bool> UpdateAsync(Reporte existing);

        // Nuevo método para generación de reportes a partir de ids de solicitudes
        Task<Reporte> GenerarReporteAsync(GenerarReporteRequest request, int idUsuarioActual, CancellationToken ct = default);
    }
}