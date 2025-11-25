using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
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
        private readonly FrontendSettings _frontendSettings;

        public UsuarioService(
            IUsuarioRepository usuarioRepository, 
            IJWTService jwtService,
            IEmailService emailService,
            ILogger<UsuarioService> logger,
            IOptions<FrontendSettings> frontendSettings)
        {
            _usuarioRepository = usuarioRepository;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _frontendSettings = frontendSettings.Value;
        }

        // ===========================
        // AUTENTICACIÓN
        // ===========================

        public async Task<string?> SignInAsync(SignInRequestDTO dto)
        {
            var usuario = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (usuario == null) 
            {
                _logger.LogWarning("Intento de login con correo no registrado: {Correo}", dto.Correo);
                return null;
            }

            // Verificar si el usuario está activo
            if (usuario.Estado != "ACTIVO")
            {
                _logger.LogWarning("Intento de login con usuario inactivo: {Correo}", dto.Correo);
                return null;
            }

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordOk) 
            {
                _logger.LogWarning("Contraseña incorrecta para: {Correo}", dto.Correo);
                return null;
            }

            usuario.UltimoLogin = DateTime.Now;
            await _usuarioRepository.UpdateAsync(usuario);

            _logger.LogInformation("Login exitoso: {Correo}", dto.Correo);
            return _jwtService.GenerateJWToken(usuario);
        }

        public async Task<bool> SignUpAsync(SignUpRequestDTO dto)
        {
            var existing = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
            if (existing != null) 
            {
                _logger.LogWarning("Intento de registro con correo existente: {Correo}", dto.Correo);
                return false;
            }

            var usuario = new Usuario
            {
                Username = dto.Username,
                Correo = dto.Correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IdRolSistema = 1, // Rol por defecto
                Estado = "ACTIVO",
                CreadoEn = DateTime.Now
            };

            await _usuarioRepository.AddAsync(usuario);
            _logger.LogInformation("Usuario registrado: {Username} ({Correo})", dto.Username, dto.Correo);
            return true;
        }

        // ===========================
        // CRUD COMPLETO
        // ===========================
        
        public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync()
        {
            var usuarios = await _usuarioRepository.GetAllAsync();
            return usuarios.Select(u => new UsuarioResponseDTO
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                Correo = u.Correo,
                IdRolSistema = u.IdRolSistema,
                NombreRol = u.IdRolSistemaNavigation?.Nombre ?? "Sin Rol",
                Estado = u.Estado ?? "ACTIVO",
                UltimoLogin = u.UltimoLogin,
                CreadoEn = u.CreadoEn,
                ActualizadoEn = u.ActualizadoEn
            });
        }

        public async Task<UsuarioResponseDTO?> GetByIdAsync(int id)
        {
            var u = await _usuarioRepository.GetByIdAsync(id);
            if (u == null) 
            {
                _logger.LogWarning("Usuario no encontrado: ID {Id}", id);
                return null;
            }

            return new UsuarioResponseDTO
            {
                IdUsuario = u.IdUsuario,
                Username = u.Username,
                Correo = u.Correo,
                IdRolSistema = u.IdRolSistema,
                NombreRol = u.IdRolSistemaNavigation?.Nombre ?? "Sin Rol",
                Estado = u.Estado ?? "ACTIVO",
                UltimoLogin = u.UltimoLogin,
                CreadoEn = u.CreadoEn,
                ActualizadoEn = u.ActualizadoEn
            };
        }

        public async Task<UsuarioResponseDTO?> CreateAsync(UsuarioCreateDTO dto)
        {
            try
            {
                // Validar que el correo no exista
                var existing = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
                if (existing != null)
                {
                    _logger.LogWarning("Intento de crear usuario con correo existente: {Correo}", dto.Correo);
                    return null;
                }

                var usuario = new Usuario
                {
                    Username = dto.Username,
                    Correo = dto.Correo,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    IdRolSistema = dto.IdRolSistema,
                    Estado = dto.Estado,
                    CreadoEn = DateTime.Now
                };

                await _usuarioRepository.AddAsync(usuario);
                
                _logger.LogInformation("Usuario creado: {Username} ({Correo}) - Rol: {IdRol}", 
                    dto.Username, dto.Correo, dto.IdRolSistema);

                // Retornar el usuario creado
                return await GetByIdAsync(usuario.IdUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario: {Correo}", dto.Correo);
                return null;
            }
        }

        public async Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de actualizar usuario inexistente: ID {Id}", id);
                    return false;
                }

                // Validar correo único si se está actualizando
                if (!string.IsNullOrEmpty(dto.Correo) && dto.Correo != usuario.Correo)
                {
                    var existingEmail = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
                    if (existingEmail != null)
                    {
                        _logger.LogWarning("Intento de actualizar usuario con correo existente: {Correo}", dto.Correo);
                        return false;
                    }
                    usuario.Correo = dto.Correo;
                }

                // Actualizar campos opcionales
                if (!string.IsNullOrEmpty(dto.Username))
                    usuario.Username = dto.Username;

                if (dto.IdRolSistema.HasValue)
                    usuario.IdRolSistema = dto.IdRolSistema.Value;

                if (!string.IsNullOrEmpty(dto.Estado))
                    usuario.Estado = dto.Estado;

                usuario.ActualizadoEn = DateTime.Now;

                await _usuarioRepository.UpdateAsync(usuario);
                
                _logger.LogInformation("Usuario actualizado: ID {Id} - {Username}", id, usuario.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario: ID {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de eliminar usuario inexistente: ID {Id}", id);
                    return false;
                }

                await _usuarioRepository.DeleteAsync(id);
                
                _logger.LogInformation("Usuario eliminado: ID {Id} - {Username}", id, usuario.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario: ID {Id}", id);
                return false;
            }
        }

        public async Task<bool> ToggleEstadoAsync(int id, UsuarioToggleEstadoDTO dto)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de cambiar estado de usuario inexistente: ID {Id}", id);
                    return false;
                }

                usuario.Estado = dto.Estado;
                usuario.ActualizadoEn = DateTime.Now;

                await _usuarioRepository.UpdateAsync(usuario);
                
                _logger.LogInformation("Estado de usuario actualizado: ID {Id} - {Username} - Estado: {Estado}", 
                    id, usuario.Username, dto.Estado);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de usuario: ID {Id}", id);
                return false;
            }
        }

        // ===========================
        // CONTRASEÑAS
        // ===========================

        public async Task<bool> ChangePasswordAsync(UsuarioChangePasswordDTO dto)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByCorreoAsync(dto.Correo);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña de usuario inexistente: {Correo}", dto.Correo);
                    return false;
                }

                bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash);
                if (!passwordOk)
                {
                    _logger.LogWarning("Contraseña actual incorrecta para: {Correo}", dto.Correo);
                    return false;
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
                usuario.ActualizadoEn = DateTime.Now;

                await _usuarioRepository.UpdateAsync(usuario);
                
                _logger.LogInformation("Contraseña cambiada exitosamente para: {Correo}", dto.Correo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña: {Correo}", dto.Correo);
                return false;
            }
        }

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
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Construir URL completa del frontend con email y token
                var recoveryUrl = $"{_frontendSettings.BaseUrl}/forgot-password?email={Uri.EscapeDataString(usuario.Correo)}&token={token}";
                
                _logger.LogInformation("URL de recuperación generada para {Email}: {Url}", request.Email, recoveryUrl);

                // Generar email usando el template con la URL completa
                var emailBody = EmailTemplates.BuildRecuperacionPasswordBody(usuario.Username, recoveryUrl);
                
                // Enviar email
                await _emailService.SendAsync(
                    usuario.Correo,
                    "Recuperación de Contraseña - Sistema SLA",
                    emailBody
                );

                _logger.LogInformation("Enlace de recuperación enviado a {Email}", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de recuperación para {Email}", request.Email);
                return false;
            }
        }

        public async Task<bool> RestablecerPassword(RestablecerPasswordDTO request)
        {
            try
            {
                // Buscar usuario por token válido y no expirado
                var usuario = await _usuarioRepository.GetByRecoveryTokenAsync(request.Token);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de restablecer con token inválido o expirado");
                    return false;
                }

                // Verificar que el email coincida (seguridad adicional)
                if (usuario.Correo != request.Email)
                {
                    _logger.LogWarning("Email {Email} no coincide con el token de recuperación", request.Email);
                    return false;
                }

                // Actualizar contraseña (hasheando con BCrypt)
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                
                // IMPORTANTE: Limpiar token de recuperación para que no pueda ser reutilizado (enlace de un solo uso)
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

                _logger.LogInformation("Contraseña restablecida exitosamente para {Email}. Token eliminado (enlace de un solo uso)", request.Email);
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
