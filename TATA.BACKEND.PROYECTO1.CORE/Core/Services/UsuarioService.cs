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
            var usuario = await _usuarioRepository.GetByUsernameAsync(dto.Username); // ⚠️ Usar Username
            if (usuario == null) 
            {
                _logger.LogWarning("Intento de login con username no registrado: {Username}", dto.Username);
                return null;
            }

            // ⚠️ NUEVO: Verificar si la cuenta está pendiente de activación
            if (usuario.PasswordHash == null)
            {
                _logger.LogWarning("Intento de login con cuenta pendiente de activación: {Username}", dto.Username);
                throw new InvalidOperationException("Cuenta pendiente de activación. Revisa tu correo electrónico.");
            }

            // Verificar si el usuario está activo
            if (usuario.Estado != "ACTIVO")
            {
                _logger.LogWarning("Intento de login con usuario inactivo: {Username}", dto.Username);
                return null;
            }

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordOk) 
            {
                _logger.LogWarning("Contraseña incorrecta para: {Username}", dto.Username);
                return null;
            }

            usuario.UltimoLogin = DateTime.Now;
            await _usuarioRepository.UpdateAsync(usuario);

            _logger.LogInformation("Login exitoso: {Username}", dto.Username);
            return _jwtService.GenerateJWToken(usuario);
        }

        public async Task<bool> SignUpAsync(SignUpRequestDTO dto)
        {
            var existing = await _usuarioRepository.GetByUsernameAsync(dto.Username); // ⚠️ Usar Username
            if (existing != null) 
            {
                _logger.LogWarning("Intento de registro con username existente: {Username}", dto.Username);
                return false;
            }

            var usuario = new Usuario
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IdRolSistema = 1, // Rol por defecto
                IdPersonal = dto.IdPersonal, // ⚠️ Vincular con Personal si se proporciona
                Estado = "ACTIVO",
                CreadoEn = DateTime.Now
            };

            await _usuarioRepository.AddAsync(usuario);
            _logger.LogInformation("Usuario registrado: {Username}", dto.Username);
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
                IdRolSistema = u.IdRolSistema,
                NombreRol = u.IdRolSistemaNavigation?.Nombre ?? "Sin Rol",
                Estado = u.Estado ?? "ACTIVO",
                IdPersonal = u.IdPersonal,
                NombresPersonal = u.PersonalNavigation?.Nombres,
                ApellidosPersonal = u.PersonalNavigation?.Apellidos,
                CorreoPersonal = u.PersonalNavigation?.CorreoCorporativo, // ⚠️ Obtener de Personal
                CuentaActivada = u.PasswordHash != null, // ⚠️ Verificar si tiene contraseña
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
                IdRolSistema = u.IdRolSistema,
                NombreRol = u.IdRolSistemaNavigation?.Nombre ?? "Sin Rol",
                Estado = u.Estado ?? "ACTIVO",
                IdPersonal = u.IdPersonal,
                NombresPersonal = u.PersonalNavigation?.Nombres,
                ApellidosPersonal = u.PersonalNavigation?.Apellidos,
                CorreoPersonal = u.PersonalNavigation?.CorreoCorporativo,
                CuentaActivada = u.PasswordHash != null,
                UltimoLogin = u.UltimoLogin,
                CreadoEn = u.CreadoEn,
                ActualizadoEn = u.ActualizadoEn
            };
        }

        public async Task<UsuarioResponseDTO?> CreateAsync(UsuarioCreateDTO dto)
        {
            try
            {
                // Validar que el username no exista
                var existing = await _usuarioRepository.GetByUsernameAsync(dto.Username);
                if (existing != null)
                {
                    _logger.LogWarning("Intento de crear usuario con username existente: {Username}", dto.Username);
                    return null;
                }

                var usuario = new Usuario
                {
                    Username = dto.Username,
                    PasswordHash = string.IsNullOrEmpty(dto.Password) ? null : BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    IdRolSistema = dto.IdRolSistema,
                    IdPersonal = dto.IdPersonal,
                    Estado = dto.Estado,
                    CreadoEn = DateTime.Now
                };

                await _usuarioRepository.AddAsync(usuario);
                
                _logger.LogInformation("Usuario creado: {Username} - Rol: {IdRol}", 
                    dto.Username, dto.IdRolSistema);

                // Retornar el usuario creado
                return await GetByIdAsync(usuario.IdUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario: {Username}", dto.Username);
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

                // Validar username único si se está actualizando
                if (!string.IsNullOrEmpty(dto.Username) && dto.Username != usuario.Username)
                {
                    var existingUsername = await _usuarioRepository.GetByUsernameAsync(dto.Username);
                    if (existingUsername != null)
                    {
                        _logger.LogWarning("Intento de actualizar usuario con username existente: {Username}", dto.Username);
                        return false;
                    }
                    usuario.Username = dto.Username;
                }

                // Actualizar campos opcionales
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
                var usuario = await _usuarioRepository.GetByUsernameAsync(dto.Username); // ⚠️ Usar Username
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña de usuario inexistente: {Username}", dto.Username);
                    return false;
                }

                if (usuario.PasswordHash == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña de cuenta no activada: {Username}", dto.Username);
                    return false;
                }

                bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash);
                if (!passwordOk)
                {
                    _logger.LogWarning("Contraseña actual incorrecta para: {Username}", dto.Username);
                    return false;
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
                usuario.ActualizadoEn = DateTime.Now;

                await _usuarioRepository.UpdateAsync(usuario);
                
                _logger.LogInformation("Contraseña cambiada exitosamente para: {Username}", dto.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña: {Username}", dto.Username);
                return false;
            }
        }

        public async Task<bool> SolicitarRecuperacionPassword(SolicitarRecuperacionDTO request)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByUsernameAsync(request.Username); // ⚠️ Usar Username
                
                if (usuario == null)
                {
                    _logger.LogWarning("Solicitud de recuperación para username no registrado: {Username}", request.Username);
                    // Por seguridad, devuelve true aunque no exista el usuario
                    return true;
                }

                // ⚠️ Validar que el usuario tenga Personal vinculado para obtener el correo
                if (usuario.PersonalNavigation == null || string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    _logger.LogWarning("Usuario sin Personal vinculado o sin correo: {Username}", request.Username);
                    return true; // Por seguridad, devuelve true
                }

                // Generar token seguro de 32 bytes (64 caracteres hexadecimales)
                var token = GenerateSecureToken();
                
                // Configurar token con expiración de 1 hora
                usuario.token_recuperacion = token;
                usuario.expiracion_token = DateTime.UtcNow.AddHours(1);
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Construir URL completa del frontend con username y token
                var recoveryUrl = $"{_frontendSettings.BaseUrl}/forgot-password?username={Uri.EscapeDataString(usuario.Username)}&token={token}";
                
                _logger.LogInformation("URL de recuperación generada para {Username}: {Url}", request.Username, recoveryUrl);

                // Generar email usando el template con la URL completa
                var emailBody = EmailTemplates.BuildRecuperacionPasswordBody(usuario.Username, recoveryUrl);
                
                // Enviar email al correo corporativo del Personal vinculado
                await _emailService.SendAsync(
                    usuario.PersonalNavigation.CorreoCorporativo,
                    "Recuperación de Contraseña - Sistema SLA",
                    emailBody
                );

                _logger.LogInformation("Enlace de recuperación enviado a {Email} para usuario {Username}", 
                    usuario.PersonalNavigation.CorreoCorporativo, request.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de recuperación para {Username}", request.Username);
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

                // Verificar que el username coincida (seguridad adicional)
                if (usuario.Username != request.Username)
                {
                    _logger.LogWarning("Username {Username} no coincide con el token de recuperación", request.Username);
                    return false;
                }

                // Actualizar contraseña (hasheando con BCrypt)
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                
                // IMPORTANTE: Limpiar token de recuperación para que no pueda ser reutilizado (enlace de un solo uso)
                usuario.token_recuperacion = null;
                usuario.expiracion_token = null;
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Enviar email de confirmación si tiene Personal vinculado
                if (usuario.PersonalNavigation != null && !string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    var emailBody = EmailTemplates.BuildPasswordChangedBody(usuario.Username);
                    
                    await _emailService.SendAsync(
                        usuario.PersonalNavigation.CorreoCorporativo,
                        "Contraseña Actualizada - Sistema SLA",
                        emailBody
                    );
                }

                _logger.LogInformation("Contraseña restablecida exitosamente para {Username}. Token eliminado (enlace de un solo uso)", request.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña para {Username}", request.Username);
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

        // ===========================
        // ACTIVACIÓN DE CUENTA
        // ===========================

        public async Task<bool> ActivarCuenta(ActivarCuentaDTO request)
        {
            try
            {
                // Buscar usuario por token válido y no expirado
                var usuario = await _usuarioRepository.GetByRecoveryTokenAsync(request.Token);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de activar cuenta con token inválido o expirado");
                    return false;
                }

                // Verificar que el username coincida
                if (usuario.Username != request.Username)
                {
                    _logger.LogWarning("Username {Username} no coincide con el token de activación", request.Username);
                    return false;
                }

                // Verificar que la cuenta esté pendiente de activación
                if (usuario.PasswordHash != null)
                {
                    _logger.LogWarning("Intento de activar cuenta ya activada: {Username}", request.Username);
                    return false;
                }

                // Establecer contraseña (hashear con BCrypt)
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                
                // Limpiar token de activación (un solo uso)
                usuario.token_recuperacion = null;
                usuario.expiracion_token = null;
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // Enviar email de confirmación si tiene Personal vinculado
                if (usuario.PersonalNavigation != null && !string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    var emailBody = $@"
                        <h2>Cuenta Activada Exitosamente</h2>
                        <p>Hola {usuario.Username},</p>
                        <p>Tu cuenta ha sido activada correctamente.</p>
                        <p>Ya puedes iniciar sesión en el Sistema SLA.</p>
                    ";
                    
                    await _emailService.SendAsync(
                        usuario.PersonalNavigation.CorreoCorporativo,
                        "Cuenta Activada - Sistema SLA",
                        emailBody
                    );
                }

                _logger.LogInformation("Cuenta activada exitosamente para {Username}. Token eliminado (enlace de un solo uso)", request.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar cuenta para {Username}", request.Username);
                return false;
            }
        }
    }
}
