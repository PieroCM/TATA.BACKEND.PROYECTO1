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
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public UsuarioRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // ⚠️ CAMBIO: GetByCorreoAsync → GetByUsernameAsync
        public async Task<Usuario?> GetByUsernameAsync(string username)
        {
            return await _context.Usuario
                .Include(u => u.IdRolSistemaNavigation)
                .Include(u => u.PersonalNavigation) // ⚠️ Incluir Personal para obtener correo
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        // ⚠️ NUEVO: Buscar usuario por correo (a través de Personal)
        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Usuario
                .Include(u => u.IdRolSistemaNavigation)
                .Include(u => u.PersonalNavigation)
                .FirstOrDefaultAsync(u => u.PersonalNavigation != null && 
                                       u.PersonalNavigation.CorreoCorporativo == email);
        }

        public async Task<Usuario?> GetByUsernameAsync(string username)
        {
            return await _context.Usuario.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            return await _context.Usuario
                .Include(u => u.IdRolSistemaNavigation)
                .Include(u => u.PersonalNavigation) // ⚠️ Incluir Personal
                .FirstOrDefaultAsync(u => u.IdUsuario == id);
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            return await _context.Usuario
                .Include(u => u.IdRolSistemaNavigation)
                .Include(u => u.PersonalNavigation) // ⚠️ Incluir Personal
                .ToListAsync();
        }

        public async Task AddAsync(Usuario usuario)
        {
            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Usuario usuario)
        {
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

        public async Task<Usuario?> GetByRecoveryTokenAsync(string token)
        {
            return await _context.Usuario
                .Include(u => u.PersonalNavigation) // ⚠️ Incluir Personal
                .FirstOrDefaultAsync(u => u.token_recuperacion == token 
                                       && u.expiracion_token > DateTime.UtcNow);
        }
    }
}
