using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
 
    /// Servicio de negocio para Reporte (cabecera).
    /// Depende de IReporteRepository con métodos básicos: GetAll/GetById/Add/Update/Delete.
 
    public class ReporteService : IReporteService
    {
        private readonly IReporteRepository _repo;

        public ReporteService(IReporteRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<Reporte>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<Reporte?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task AddAsync(Reporte reporte)
            => _repo.AddAsync(reporte);

        public async Task<bool> UpdateAsync(Reporte reporte)
        {
            var current = await _repo.GetByIdAsync(reporte.IdReporte);
            if (current is null) return false;

            await _repo.UpdateAsync(reporte);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            await _repo.DeleteAsync(current);
            return true;
        }
    }
}
