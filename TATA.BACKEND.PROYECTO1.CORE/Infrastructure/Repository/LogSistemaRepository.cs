using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class LogSistemaRepository : ILogSistemaRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public LogSistemaRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // ✅ Obtener todos los registros
        public async Task<IEnumerable<LogSistema>> GetAllAsync()
        {
            return await _context.LogSistema
                .Include(l => l.IdUsuarioNavigation)
                .OrderByDescending(l => l.FechaHora)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Obtener por Id
        public async Task<LogSistema?> GetByIdAsync(long id)
        {
            return await _context.LogSistema
                .Include(l => l.IdUsuarioNavigation)
                .FirstOrDefaultAsync(l => l.IdLog == id);
        }

        // ✅ Insertar un nuevo log
        public async Task<bool> AddAsync(LogSistema entity)
        {
            await _context.LogSistema.AddAsync(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        // ✅ Eliminar por Id
        public async Task<bool> RemoveAsync(long id)
        {
            var log = await _context.LogSistema.FindAsync(id);
            if (log == null) return false;

            _context.LogSistema.Remove(log);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
