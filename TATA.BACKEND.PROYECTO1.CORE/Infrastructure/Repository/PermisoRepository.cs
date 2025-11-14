using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class PermisoRepository : IPermisoRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public PermisoRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Permiso>> GetAll()
        {
            return await _context.Permiso
                .AsNoTracking()
                .OrderBy(p => p.IdPermiso)
                .ToListAsync();
        }

        public async Task<Permiso?> GetById(int id)
        {
            return await _context.Permiso
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPermiso == id);
        }

        public async Task Add(Permiso entity)
        {
            _context.Permiso.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Permiso entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var entity = await _context.Permiso.FindAsync(id);
            if (entity != null)
            {
                _context.Permiso.Remove(entity); // physical delete
                await _context.SaveChangesAsync();
            }
        }
    }
}
