using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using System.Security.Cryptography;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IJWTService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(
            IUsuarioRepository usuarioRepository, 
            IJWTService jwtService,
            IEmailService emailService,
            ILogger<UsuarioService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
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
                IdRolSistema = 1,
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

        //LO DE CAMBIAR CONTRASEÑA (usuario ya logueado)
        public async Task<bool> ChangePasswordAsync(UsuarioChangePasswordDTO dto)
        {
            var usuario = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (usuario == null)
                return false;

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash);
            if (!passwordOk)
                return false;

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.ActualizadoEn = DateTime.Now;

            await _usuarioRepository.UpdateAsync(usuario);
            return true;
        }

        // ===========================
        // SOLICITAR RECUPERACIÓN DE CONTRASEÑA
        // ===========================
        public async Task<bool> SolicitarRecuperacionPassword(SolicitarRecuperacionDTO request)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByCorreoAsync(request.Email);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Solicitud de recuperación para email no registrado: {Email}", request.Email);
                    // Por seguridad, devuelve true aunque no exista el usuario
                    return true;
                }

                // Generar token seguro de 32 bytes (64 caracteres hexadecimales)
                var token = GenerateSecureToken();
                
                // Configurar token con expiración de 1 hora
                usuario.token_recuperacion = token;
                usuario.expiracion_token = DateTime.UtcNow.AddHours(1);
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Generar email usando el template
                var emailBody = EmailTemplates.BuildRecuperacionPasswordBody(usuario.Username, token);
                
                // Enviar email
                await _emailService.SendAsync(
                    usuario.Correo,
                    "Recuperación de Contraseña - Sistema SLA",
                    emailBody
                );

                _logger.LogInformation("Token de recuperación enviado a {Email}", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de recuperación para {Email}", request.Email);
                return false;
            }
        }

        // ===========================
        // RESTABLECER CONTRASEÑA CON TOKEN
        // ===========================
        public async Task<bool> RestablecerPassword(RestablecerPasswordDTO request)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByRecoveryTokenAsync(request.Token);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de restablecer con token inválido o expirado");
                    return false;
                }

                // Verificar que el email coincida (seguridad adicional)
                if (usuario.Correo != request.Email)
                {
                    _logger.LogWarning("Email no coincide con el token de recuperación");
                    return false;
                }

                // Actualizar contraseña (hasheando con BCrypt)
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                
                // Limpiar token de recuperación
                usuario.token_recuperacion = null;
                usuario.expiracion_token = null;
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Generar email de confirmación usando el template
                var emailBody = EmailTemplates.BuildPasswordChangedBody(usuario.Username);
                
                // Enviar email de confirmación
                await _emailService.SendAsync(
                    usuario.Correo,
                    "Contraseña Actualizada - Sistema SLA",
                    emailBody
                );

                _logger.LogInformation("Contraseña restablecida exitosamente para {Email}", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña para {Email}", request.Email);
                return false;
            }
        }

        // ===========================
        // MÉTODO AUXILIAR PRIVADO
        // ===========================
        private static string GenerateSecureToken()
        {
            // Genera un token seguro de 32 bytes (64 caracteres hexadecimales)
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToHexString(randomBytes);
        }
    }
}
