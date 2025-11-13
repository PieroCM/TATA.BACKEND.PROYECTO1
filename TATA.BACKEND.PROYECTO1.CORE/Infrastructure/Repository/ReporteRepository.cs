using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repositories
{
    public class ReporteRepository : IReporteRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public ReporteRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // GET /api/reportes
        public async Task<IEnumerable<Reporte>> GetAllAsync()
        {
            return await _context.Reporte
                                 .AsNoTracking()
                                 .Include(r => r.Detalles)
                                 .ToListAsync();
        }

        // GET /api/reportes/{id}
        public async Task<Reporte?> GetByIdAsync(int id)
        {
            return await _context.Reporte
                                 .AsNoTracking()
                                 .Include(r => r.Detalles)
                                 // .Include(r => r.GeneradoPorNavigation) // <- solo si lo necesitas
                                 .FirstOrDefaultAsync(r => r.IdReporte == id);
        }

        // POST /api/reportes
        public async Task AddAsync(Reporte reporte)
        {
            await _context.Reporte.AddAsync(reporte);
            await _context.SaveChangesAsync();
        }

        // PUT /api/reportes/{id}
        public async Task UpdateAsync(Reporte reporte)
        {
            _context.Reporte.Update(reporte);
            await _context.SaveChangesAsync();
        }

        // DELETE /api/reportes/{id}
        public async Task DeleteAsync(Reporte reporte)
        {
            // ⚠️ No confíes en reporte.Detalles (puede no estar cargado).
            // Borra hijos por consulta para evitar violaciones de FK.
            var hijos = await _context.Set<ReporteDetalle>()
                                      .Where(d => d.IdReporte == reporte.IdReporte)
                                      .ToListAsync();

            if (hijos.Count > 0)
            {
                _context.Set<ReporteDetalle>().RemoveRange(hijos);
                await _context.SaveChangesAsync();
            }

            _context.Reporte.Remove(reporte);
            await _context.SaveChangesAsync();
        }



        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Reporte
                                 .AsNoTracking()
                                 .AnyAsync(r => r.IdReporte == id);
        }

    }
}
