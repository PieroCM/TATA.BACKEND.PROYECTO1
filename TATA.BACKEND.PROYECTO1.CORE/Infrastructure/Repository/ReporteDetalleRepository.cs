using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repositories
{

    /// Join table N:N (reporte <-> solicitud).
    /// Nota: no tiene campos mutables; el PUT es un no-op validado.
    
    public class ReporteDetalleRepository : IReporteDetalleRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public ReporteDetalleRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // GET /api/reportedetalles
        public async Task<IEnumerable<ReporteDetalle>> GetAllAsync()
        {
            // Usa AsNoTracking para lecturas
            return await _context.Set<ReporteDetalle>()
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        // GET /api/reportedetalles/{idReporte}/{idSolicitud}
        public async Task<ReporteDetalle?> GetByIdsAsync(int idReporte, int idSolicitud)
        {
            // Con PK compuesta, mejor FirstOrDefault + AsNoTracking
            return await _context.Set<ReporteDetalle>()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(d => d.IdReporte == idReporte && d.IdSolicitud == idSolicitud);
        }

        // POST /api/reportedetalles
        public async Task AddAsync(ReporteDetalle entity)
        {
            // Si tu Service ya valida duplicados, puedes guardar directo.
            _context.Set<ReporteDetalle>().Add(entity);
            await _context.SaveChangesAsync();
        }

        // PUT /api/reportedetalles/{idReporte}/{idSolicitud}
        public async Task UpdateAsync(ReporteDetalle entity)
        {
            // En una join table pura no hay columnas a actualizar.
            // Implementamos no-op validado para mantener el contrato.
            var exists = await _context.Set<ReporteDetalle>()
                                       .AnyAsync(d => d.IdReporte == entity.IdReporte &&
                                                      d.IdSolicitud == entity.IdSolicitud);
            if (!exists)
            {
                // No existe → el Service debe devolver 404.
                return;
            }

            // No hay nada que actualizar; dejamos la operación como no-op.
            await Task.CompletedTask;
        }

        // DELETE /api/reportedetalles/{idReporte}/{idSolicitud}
        public async Task DeleteAsync(ReporteDetalle entity)
        {
            // Remove adjunta si no está trackeado
            _context.Set<ReporteDetalle>().Remove(entity);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> ExistsAsync(int idReporte, int idSolicitud)
        {
            return await _context.Set<ReporteDetalle>()
                                 .AsNoTracking()
                                 .AnyAsync(d => d.IdReporte == idReporte &&
                                                d.IdSolicitud == idSolicitud);
        }

    }
}
