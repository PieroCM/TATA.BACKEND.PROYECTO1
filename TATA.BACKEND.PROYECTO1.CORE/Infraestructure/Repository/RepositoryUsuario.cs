using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository
{
    public class RepositoryUsuario : IRepositoryUsuario
    {
        private readonly Proyecto1SlaDbContext _context;

        public RepositoryUsuario(Proyecto1SlaDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // CRUD COMPLETO DE USUARIO
        // =========================================================

        // Listar todos los usuarios
        public async Task<List<Usuario>> GetAllUsuarios()
        {
            return await _context.Usuario
                .AsNoTracking()
                .OrderBy(u => u.IdUsuario)
                .ToListAsync();
        }

        // Obtener usuario por ID
        public async Task<Usuario?> GetUsuarioById(int id)
        {
            return await _context.Usuario
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == id);
        }

        // Agregar usuario nuevo
        public async Task AddUsuario(Usuario usuario)
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
            usuario.Estado = "ACTIVO";
            usuario.CreadoEn = DateTime.UtcNow;

            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();
        }

        // Actualizar usuario
        public async Task UpdateUsuario(Usuario usuario)
        {
            usuario.ActualizadoEn = DateTime.UtcNow;
            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // Eliminar usuario (marcar como INACTIVO)
        public async Task DeleteUsuario(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);
            if (usuario != null)
            {
                usuario.Estado = "INACTIVO";
                usuario.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // =========================================================
        //  AUTENTICACIÓN: SIGN UP / SIGN IN
        // =========================================================

        // Registro de usuario (SignUp)
        public async Task<bool> SignUp(Usuario newUser)
        {
            bool exists = await _context.Usuario
                .AnyAsync(u => u.Correo == newUser.Correo || u.Username == newUser.Username);

            if (exists)
                return false; // Ya existe usuario con mismo correo o username

            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash);
            newUser.Estado = "ACTIVO";
            newUser.CreadoEn = DateTime.UtcNow;

            _context.Usuario.Add(newUser);
            await _context.SaveChangesAsync();
            return true;
        }

        // Inicio de sesión (SignIn)
        public async Task<Usuario?> SignIn(string correo, string password)
        {
            var usuario = await _context.Usuario
                .FirstOrDefaultAsync(u => u.Correo == correo && u.Estado == "ACTIVO");

            if (usuario == null)
                return null;

            bool valid = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
            if (!valid)
                return null;

            usuario.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Retornar solo información segura
            return new Usuario
            {
                IdUsuario = usuario.IdUsuario,
                Username = usuario.Username,
                Correo = usuario.Correo,
                IdRolSistema = usuario.IdRolSistema,
                Estado = usuario.Estado,
                UltimoLogin = usuario.UltimoLogin
            };
        }
    }
}
