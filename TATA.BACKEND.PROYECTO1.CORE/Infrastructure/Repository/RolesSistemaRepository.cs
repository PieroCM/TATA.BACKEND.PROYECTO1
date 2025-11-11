using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class RolesSistemaRepository : IRolesSistemaRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public RolesSistemaRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<List<RolesSistema>> GetAll()
        {
            return await _context.RolesSistema
                .AsNoTracking()
                .OrderBy(r => r.IdRolSistema)
                .ToListAsync();
        }

        public async Task<RolesSistema?> GetById(int id)
        {
            return await _context.RolesSistema
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdRolSistema == id);
        }

        public async Task Add(RolesSistema entity)
        {
            entity.EsActivo = true;
            _context.RolesSistema.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Update(RolesSistema entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var entity = await _context.RolesSistema.FindAsync(id);
            if (entity != null)
            {
                entity.EsActivo = false; // logical delete
                await _context.SaveChangesAsync();
            }
        }
    }
}
