using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class EmailConfigRepository : IEmailConfigRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public EmailConfigRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<EmailConfig?> GetConfigAsync()
        {
            // Siempre habrá solo 1 registro, pero agregamos OrderBy para evitar warnings
            return await _context.EmailConfig
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<EmailConfig> CreateOrUpdateAsync(EmailConfig config)
        {
            var existing = await _context.EmailConfig
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                // Crear nuevo
                config.CreadoEn = System.DateTime.UtcNow;
                config.ActualizadoEn = config.CreadoEn;
                _context.EmailConfig.Add(config);
            }
            else
            {
                // Actualizar existente
                existing.EnvioInmediato = config.EnvioInmediato;
                existing.ResumenDiario = config.ResumenDiario;
                existing.HoraResumen = config.HoraResumen;
                existing.EmailDestinatarioPrueba = config.EmailDestinatarioPrueba;
                existing.ActualizadoEn = System.DateTime.UtcNow;
                
                config = existing;
            }

            await _context.SaveChangesAsync();
            return config;
        }
    }
}
