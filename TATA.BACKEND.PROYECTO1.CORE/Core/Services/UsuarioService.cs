using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{

    public class UsuarioService
    {
        private readonly RepositoryUsuario _repo;

        public UsuarioService(RepositoryUsuario repo)
        {
            _repo = repo;
        }


        // Listar todos los usuarios
        public async Task<List<UsuarioDTO>> GetAllAsync()
        {
            var usuarios = await _repo.GetAllUsuarios();

            // Convertimos a DTO para no exponer contraseñas
            return usuarios.Select(u => new UsuarioDTO
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                Correo = u.Correo,
                IdRolSistema = u.IdRolSistema,
                Estado = u.Estado,
                UltimoLogin = u.UltimoLogin
            }).ToList();
        }

        // Obtener un usuario por ID
        public async Task<UsuarioDTO?> GetByIdAsync(int id)
        {
            var usuario = await _repo.GetUsuarioById(id);
            if (usuario == null)
                return null;

            return new UsuarioDTO
            {
                IdUsuario = usuario.IdUsuario,
                Username = usuario.Username,
                Correo = usuario.Correo,
                IdRolSistema = usuario.IdRolSistema,
                Estado = usuario.Estado,
                UltimoLogin = usuario.UltimoLogin
            };
        }

        // Crear usuario
        public async Task<bool> AddAsync(Usuario usuario)
        {
            await _repo.AddUsuario(usuario);
            return true;
        }

        // Actualizar usuario
        public async Task<bool> UpdateAsync(Usuario usuario)
        {
            await _repo.UpdateUsuario(usuario);
            return true;
        }

        // Eliminar usuario
        public async Task<bool> DeleteAsync(int id)
        {
            await _repo.DeleteUsuario(id);
            return true;
        }

        // =========================================================
        // 🔐 SIGN UP Y SIGN IN (aún sin JWT)
        // =========================================================

        public async Task<bool> SignUp(Usuario newUser)
        {
            return await _repo.SignUp(newUser);
        }

        public async Task<UsuarioDTO?> SignIn(string correo, string password)
        {
            var usuario = await _repo.SignIn(correo, password);
            if (usuario == null)
                return null;

            // Convertir a DTO antes de devolver
            return new UsuarioDTO
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

