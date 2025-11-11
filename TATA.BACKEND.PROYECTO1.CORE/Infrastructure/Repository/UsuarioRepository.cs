using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public UsuarioRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByCorreoAsync(string correo)
        {
            return await _context.Usuario.FirstOrDefaultAsync(u => u.Correo == correo);
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            return await _context.Usuario.FindAsync(id);
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            return await _context.Usuario.ToListAsync();
        }

        public async Task AddAsync(Usuario usuario)
        {
            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            usuario.ActualizadoEn = DateTime.Now;
            _context.Usuario.Update(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuario.Remove(usuario);
                await _context.SaveChangesAsync();
            }
        }
    }
}
