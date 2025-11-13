using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repositories
{
    public class PersonalRepository : IPersonalRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public PersonalRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Personal>> GetAllAsync()
        {
            return await _context.Personal.Include(p => p.IdUsuarioNavigation).ToListAsync();
        }

        public async Task<Personal?> GetByIdAsync(int id)
        {
            return await _context.Personal.Include(p => p.IdUsuarioNavigation)
                                          .FirstOrDefaultAsync(p => p.IdPersonal == id);
        }

        public async Task AddAsync(Personal personal)
        {
            _context.Personal.Add(personal);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Personal personal)
        {
            _context.Personal.Update(personal);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Personal personal)
        {
            _context.Personal.Remove(personal);
            await _context.SaveChangesAsync();
        }
    }
}
