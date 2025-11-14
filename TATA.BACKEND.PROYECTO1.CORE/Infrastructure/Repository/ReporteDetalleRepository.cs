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
            return await _context.ReporteDetalle
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        // GET /api/reportedetalles/{idReporte}/{idSolicitud}
        public async Task<ReporteDetalle?> GetByIdsAsync(int idReporte, int idSolicitud)
        {
            // Con PK compuesta, mejor FirstOrDefault + AsNoTracking
            return await _context.ReporteDetalle
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(d => d.IdReporte == idReporte && d.IdSolicitud == idSolicitud);
        }

        // GET reportedetalles por solicitud
        public async Task<IEnumerable<ReporteDetalle>> GetBySolicitudIdAsync(int idSolicitud)
        {
            return await _context.ReporteDetalle
                                 .AsNoTracking()
                                 .Where(d => d.IdSolicitud == idSolicitud)
                                 .ToListAsync();
        }

        // GET reportedetalles por reporte
        public async Task<IEnumerable<ReporteDetalle>> GetByReporteIdAsync(int idReporte)
        {
            return await _context.ReporteDetalle
                                 .AsNoTracking()
                                 .Where(d => d.IdReporte == idReporte)
                                 .ToListAsync();
        }

        // POST /api/reportedetalles
        public async Task AddAsync(ReporteDetalle entity)
        {
            // Si tu Service ya valida duplicados, puedes guardar directo.
            _context.ReporteDetalle.Add(entity);
            await _context.SaveChangesAsync();
        }

        // PUT /api/reportedetalles/{idReporte}/{idSolicitud}
        public async Task UpdateAsync(ReporteDetalle entity)
        {
            // En una join table pura no hay columnas a actualizar.
            // Implementamos no-op validado para mantener el contrato.
            var exists = await _context.ReporteDetalle
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
            _context.ReporteDetalle.Remove(entity);
            await _context.SaveChangesAsync();
        }


        public async Task<bool> ExistsAsync(int idReporte, int idSolicitud)
        {
            return await _context.ReporteDetalle
                                 .AsNoTracking()
                                 .AnyAsync(d => d.IdReporte == idReporte &&
                                                d.IdSolicitud == idSolicitud);
        }

    }

}
