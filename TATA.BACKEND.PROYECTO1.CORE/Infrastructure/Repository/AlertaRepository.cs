using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository
{
    public class AlertaRepository : IAlertaRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public AlertaRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }
        // Get all  Alertas 
        // GET ALL: alerta + solicitud + datos clave de la solicitud
        public async Task<List<Alerta>> GetAlertasAsync()
        {
            return await _context.Alerta
                .AsNoTracking()
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdPersonalNavigation)
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdRolRegistroNavigation)
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdSlaNavigation)
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();
        }

        // GET BY ID
        public async Task<Alerta?> GetAlertaByIdAsync(int id)
        {
            return await _context.Alerta
                .AsNoTracking()
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdPersonalNavigation)
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdRolRegistroNavigation)
                .Include(a => a.IdSolicitudNavigation)
                    .ThenInclude(s => s.IdSlaNavigation)
                .FirstOrDefaultAsync(a => a.IdAlerta == id);
        }

        // POST
        public async Task<Alerta> CreateAlertaAsync(Alerta alerta)
        {
            // validar FK solicitud
            var existsSolicitud = await _context.Solicitud.AnyAsync(s => s.IdSolicitud == alerta.IdSolicitud);
            if (!existsSolicitud)
                throw new ArgumentException($"No existe la solicitud con Id={alerta.IdSolicitud}");

            alerta.FechaCreacion = DateTime.UtcNow;
            alerta.Estado = string.IsNullOrWhiteSpace(alerta.Estado) ? "NUEVA" : alerta.Estado;
            alerta.EnviadoEmail = alerta.EnviadoEmail; // normalmente false

            _context.Alerta.Add(alerta);
            await _context.SaveChangesAsync();
            return alerta;
        }

        // PUT
        public async Task<Alerta?> UpdateAlertaAsync(int id, Alerta alerta)
        {
            var existing = await _context.Alerta.FindAsync(id);
            if (existing == null) return null;

            // si quiere cambiar de solicitud, valida:
            if (existing.IdSolicitud != alerta.IdSolicitud)
            {
                var existsSolicitud = await _context.Solicitud.AnyAsync(s => s.IdSolicitud == alerta.IdSolicitud);
                if (!existsSolicitud)
                    throw new ArgumentException($"No existe la solicitud con Id={alerta.IdSolicitud}");
            }

            _context.Entry(existing).CurrentValues.SetValues(alerta);
            existing.ActualizadoEn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            // Recargar la entidad con sus relaciones para devolverla completa
            await _context.Entry(existing).Reference(a => a.IdSolicitudNavigation).LoadAsync();
            if (existing.IdSolicitudNavigation != null)
            {
                await _context.Entry(existing.IdSolicitudNavigation).Reference(s => s.IdPersonalNavigation).LoadAsync();
                await _context.Entry(existing.IdSolicitudNavigation).Reference(s => s.IdRolRegistroNavigation).LoadAsync();
                await _context.Entry(existing.IdSolicitudNavigation).Reference(s => s.IdSlaNavigation).LoadAsync();
            }
            
            return existing;
        }

        // DELETE lógico (marcar leída o eliminada)
        public async Task<bool> DeleteAlertaAsync(int id)
        {
            var existing = await _context.Alerta.FindAsync(id);
            if (existing == null) return false;

            // puedes marcar como "CERRADA" o "ELIMINADA", según tu flujo
            existing.Estado = "ELIMINADA";
            existing.ActualizadoEn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }


}
