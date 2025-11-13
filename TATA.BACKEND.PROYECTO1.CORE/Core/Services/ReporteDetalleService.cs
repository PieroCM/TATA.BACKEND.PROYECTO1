using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{

    /// Servicio para la tabla N:N reporte_detalle (PK compuesta).
    /// PUT es idempotente (no-op) porque no hay columnas editables.
 
    public class ReporteDetalleService : IReporteDetalleService
    {
        private readonly IReporteDetalleRepository _repo;

        public ReporteDetalleService(IReporteDetalleRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<ReporteDetalle>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<ReporteDetalle?> GetByIdsAsync(int idReporte, int idSolicitud)
            => _repo.GetByIdsAsync(idReporte, idSolicitud);

        public async Task<bool> AddAsync(ReporteDetalle entity)
        {
            // Evita duplicar la relación (POST idempotente a nivel lógico)
            var exists = await _repo.GetByIdsAsync(entity.IdReporte, entity.IdSolicitud);
            if (exists != null) return false; // el controller puede devolver 409

            await _repo.AddAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(ReporteDetalle entity)
        {
            // No hay campos que actualizar en una join table pura.
            // Si existe -> true (204 NoContent); si no -> false (404).
            var exists = await _repo.GetByIdsAsync(entity.IdReporte, entity.IdSolicitud);
            if (exists == null) return false;

            // No llamamos al repo.UpdateAsync() para dejar claro que es no-op.
            return true;
        }

        public async Task<bool> DeleteAsync(int idReporte, int idSolicitud)
        {
            var entity = await _repo.GetByIdsAsync(idReporte, idSolicitud);
            if (entity == null) return false;

            await _repo.DeleteAsync(entity);
            return true;
        }
    }
}
