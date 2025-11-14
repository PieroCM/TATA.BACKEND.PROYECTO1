using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
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
                                 .Include(r => r.Detalles) // incluye filas de la tabla intermedia
                                 .ToListAsync();
        }

        // GET /api/reportes/{id}
        public async Task<Reporte?> GetByIdAsync(int id)
        {
            return await _context.Reporte
                                 .AsNoTracking()
                                 .Include(r => r.Detalles)
                                 .FirstOrDefaultAsync(r => r.IdReporte == id);
        }

        // GET reportes por idSolicitud usando la tabla intermedia
        public async Task<IEnumerable<Reporte>> GetBySolicitudIdAsync(int idSolicitud)
        {
            // busca reportes cuya tabla intermedia contenga la solicitud
            return await _context.Reporte
                                 .AsNoTracking()
                                 .Where(r => r.Detalles.Any(d => d.IdSolicitud == idSolicitud))
                                 .Include(r => r.Detalles)
                                 .ToListAsync();
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
            // Borra hijos en reporte_detalle por consulta
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
