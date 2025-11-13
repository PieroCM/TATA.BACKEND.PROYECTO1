using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IJWTService _jwtService;

        public UsuarioService(IUsuarioRepository usuarioRepository, IJWTService jwtService)
        {
            _usuarioRepository = usuarioRepository;
            _jwtService = jwtService;
        }

        // LOGIN
        public async Task<string?> SignInAsync(SignInRequestDTO dto)
        {
            var usuario = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (usuario == null) return null;

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordOk) return null;

            usuario.UltimoLogin = DateTime.Now;
            await _usuarioRepository.UpdateAsync(usuario);

            return _jwtService.GenerateJWToken(usuario);
        }

        // REGISTRO
        public async Task<bool> SignUpAsync(SignUpRequestDTO dto)
        {
            var existing = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (existing != null) return false;

            var usuario = new Usuario
            {
                Username = dto.Username,
                Correo = dto.Correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IdRolSistema = dto.IdRolSistema,
                Estado = "ACTIVO",
                CreadoEn = DateTime.Now
            };

            await _usuarioRepository.AddAsync(usuario);
            return true;
        }

        // CRUD
        public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync()
        {
            var usuarios = await _usuarioRepository.GetAllAsync();
            return usuarios.Select(u => new UsuarioResponseDTO
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                Correo = u.Correo,
                IdRolSistema = u.IdRolSistema,
                Estado = u.Estado,
                UltimoLogin = u.UltimoLogin,
                CreadoEn = u.CreadoEn,
                ActualizadoEn = u.ActualizadoEn
            });
        }

        public async Task<UsuarioResponseDTO?> GetByIdAsync(int id)
        {
            var u = await _usuarioRepository.GetByIdAsync(id);
            if (u == null) return null;

            return new UsuarioResponseDTO
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                Correo = u.Correo,
                IdRolSistema = u.IdRolSistema,
                Estado = u.Estado,
                UltimoLogin = u.UltimoLogin,
                CreadoEn = u.CreadoEn,
                ActualizadoEn = u.ActualizadoEn
            };
        }

        public async Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
                return false;

            usuario.Estado = dto.Estado ?? usuario.Estado;
            usuario.ActualizadoEn = DateTime.Now;

            await _usuarioRepository.UpdateAsync(usuario);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return false;

            await _usuarioRepository.DeleteAsync(id);
            return true;
        }


        //LO DE RECUPERAR CONTRASEÑA
        public async Task<bool> ChangePasswordAsync(UsuarioChangePasswordDTO dto)
        {
            // Buscar usuario por correo
            var usuario = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (usuario == null)
                return false;

            // Verificar contraseña actual
            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash);
            if (!passwordOk)
                return false;

            // Hashear la nueva contraseña
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.ActualizadoEn = DateTime.Now;

            // Actualizar en base de datos
            await _usuarioRepository.UpdateAsync(usuario);
            return true;
        }

    }
}
