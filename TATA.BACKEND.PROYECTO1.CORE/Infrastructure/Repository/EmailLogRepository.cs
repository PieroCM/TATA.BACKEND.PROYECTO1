using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class EmailLogRepository : IEmailLogRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public EmailLogRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmailLog>> GetLogsAsync(int take = 50)
        {
            return await _context.EmailLog
                .AsNoTracking()
                .OrderByDescending(l => l.FechaEjecucion)
                .Take(take)
                .ToListAsync();
        }

        public async Task<EmailLog> CreateLogAsync(EmailLog log)
        {
            log.FechaEjecucion = DateTime.UtcNow;
            _context.EmailLog.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }
    }
}
