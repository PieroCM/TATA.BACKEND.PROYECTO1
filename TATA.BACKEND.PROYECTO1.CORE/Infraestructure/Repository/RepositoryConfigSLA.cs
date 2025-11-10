using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository
{
    public class RepositoryConfigSLA : IRepositoryConfigSLA
    {
        private readonly Proyecto1SlaDbContext _context;

        public RepositoryConfigSLA(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // ------- READ -------
        public IEnumerable<ConfigSla> GetAll()
            => _context.ConfigSla.AsNoTracking().ToList();

        public async Task<IEnumerable<ConfigSla>> GetAllAsync()
            => await _context.ConfigSla.AsNoTracking().ToListAsync();

        public async Task<ConfigSla?> GetByIdAsync(int id)
            => await _context.ConfigSla
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.IdSla == id);

        // ------- CREATE -------
        public async Task<int> InsertAsync(ConfigSla entity)
        {
            var now = DateTime.UtcNow;
            if (entity.CreadoEn == default) entity.CreadoEn = now;
            entity.ActualizadoEn = entity.CreadoEn;

            await _context.ConfigSla.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.IdSla;
        }

        // ------- UPDATE -------
        public async Task<bool> UpdateAsync(ConfigSla entity)
        {
            var current = await _context.ConfigSla.FindAsync(entity.IdSla);
            if (current is null) return false;

            current.CodigoSla = entity.CodigoSla;
            current.Descripcion = entity.Descripcion;
            current.DiasUmbral = entity.DiasUmbral;
            current.TipoSolicitud = entity.TipoSolicitud;
            current.EsActivo = entity.EsActivo;
            current.ActualizadoEn = entity.ActualizadoEn == default ? DateTime.UtcNow : entity.ActualizadoEn;

            await _context.SaveChangesAsync();
            return true;
        }

        // ------- DELETE físico -------
        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _context.ConfigSla.FindAsync(id);
            if (current is null) return false;

            _context.ConfigSla.Remove(current);
            await _context.SaveChangesAsync();
            return true;
        }

        // ------- DELETE lógico -------
        public async Task<bool> DeleteLogicAsync(int id)
        {
            var current = await _context.ConfigSla.FindAsync(id);
            if (current is null) return false;

            current.EsActivo = false;
            current.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
