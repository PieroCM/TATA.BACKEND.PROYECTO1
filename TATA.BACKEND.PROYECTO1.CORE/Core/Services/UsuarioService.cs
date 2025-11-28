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
        private readonly IPersonalRepository _personalRepository; // ⚠️ NUEVO
        private readonly IJWTService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UsuarioService> _logger;
        private readonly FrontendSettings _frontendSettings;

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IPersonalRepository personalRepository, // ⚠️ NUEVO
            IJWTService jwtService,
            IEmailService emailService,
            ILogger<UsuarioService> logger,
            IOptions<FrontendSettings> frontendSettings)
        {
            _usuarioRepository = usuarioRepository;
            _personalRepository = personalRepository; // ⚠️ NUEVO
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
            // ⚠️ CAMBIO: Buscar por email en lugar de username
            var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email);
            if (usuario == null) 
            {
                _logger.LogWarning("Intento de login con email no registrado: {Email}", dto.Email);
                return null;
            }

            // ⚠️ Verificar si la cuenta está pendiente de activación
            if (usuario.PasswordHash == null)
            {
                _logger.LogWarning("Intento de login con cuenta pendiente de activación: {Email}", dto.Email);
                throw new InvalidOperationException("Cuenta pendiente de activación. Revisa tu correo electrónico.");
            }

            // Verificar si el usuario está activo
            if (usuario.Estado != "ACTIVO")
            {
                _logger.LogWarning("Intento de login con usuario inactivo: {Email}", dto.Email);
                return null;
            }

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordOk) 
            {
                _logger.LogWarning("Contraseña incorrecta para: {Email}", dto.Email);
                return null;
            }

            usuario.UltimoLogin = DateTime.UtcNow;
            await _usuarioRepository.UpdateAsync(usuario);

            _logger.LogInformation("Login exitoso: {Email} - Username: {Username}", dto.Email, usuario.Username);
            return _jwtService.GenerateJWToken(usuario);
        }

        public async Task<bool> SignUpAsync(SignUpRequestDTO dto)
        {
            // ⚠️ CAMBIO: Buscar por email en lugar de username
            var existing = await _usuarioRepository.GetByEmailAsync(dto.Email);
            if (existing != null) 
            {
                _logger.LogWarning("Intento de registro con email existente: {Email}", dto.Email);
                return false;
            }

            // ⚠️ Validar que si proporciona IdPersonal, exista y tenga ese correo
            Personal? personal = null;
            if (dto.IdPersonal.HasValue)
            {
                personal = await _personalRepository.GetByIdAsync(dto.IdPersonal.Value);
                if (personal == null || personal.CorreoCorporativo != dto.Email)
                {
                    _logger.LogWarning("IdPersonal {IdPersonal} no existe o email no coincide", dto.IdPersonal);
                    return false;
                }
            }

            var usuario = new Usuario
            {
                Username = dto.Email, // ⚠️ Username = Email
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IdRolSistema = 1,
                IdPersonal = dto.IdPersonal,
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            await _usuarioRepository.AddAsync(usuario);
            _logger.LogInformation("Usuario registrado: {Email}", dto.Email);
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
                // ⚠️ CAMBIO: Buscar por email en lugar de username
                var usuario = await _usuarioRepository.GetByEmailAsync(dto.Email);
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña de usuario inexistente: {Email}", dto.Email);
                    return false;
                }

                if (usuario.PasswordHash == null)
                {
                    _logger.LogWarning("Intento de cambiar contraseña de cuenta no activada: {Email}", dto.Email);
                    return false;
                }

                bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash);
                if (!passwordOk)
                {
                    _logger.LogWarning("Contraseña actual incorrecta para: {Email}", dto.Email);
                    return false;
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
                usuario.ActualizadoEn = DateTime.UtcNow;

                await _usuarioRepository.UpdateAsync(usuario);
                
                _logger.LogInformation("Contraseña cambiada exitosamente para: {Email}", dto.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña: {Email}", dto.Email);
                return false;
            }
        }

        public async Task<bool> SolicitarRecuperacionPassword(SolicitarRecuperacionDTO request)
        {
            try
            {
                // ⚠️ CAMBIO: Buscar por email en lugar de username
                var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Solicitud de recuperación para email no registrado: {Email}", request.Email);
                    return true; // Por seguridad, devuelve true aunque no exista
                }

                // Validar que tenga Personal vinculado para correo
                if (usuario.PersonalNavigation == null || string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    _logger.LogWarning("Usuario sin Personal vinculado o sin correo: {Email}", request.Email);
                    return true;
                }

                // Generar token seguro
                var token = GenerateSecureToken();
                
                usuario.token_recuperacion = token;
                usuario.expiracion_token = DateTime.UtcNow.AddHours(1);
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                // ⚠️ CAMBIO: URL usa email en lugar de username
                var recoveryUrl = $"{_frontendSettings.BaseUrl}/forgot-password?email={Uri.EscapeDataString(request.Email)}&token={token}";
                
                _logger.LogInformation("URL de recuperación generada para {Email}: {Url}", request.Email, recoveryUrl);

                var emailBody = EmailTemplates.BuildRecuperacionPasswordBody(request.Email, recoveryUrl);
                
                await _emailService.SendAsync(
                    usuario.PersonalNavigation.CorreoCorporativo,
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
                var usuario = await _usuarioRepository.GetByRecoveryTokenAsync(request.Token);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de restablecer con token inválido o expirado");
                    return false;
                }

                // ⚠️ CAMBIO: Verificar email en lugar de username
                if (usuario.PersonalNavigation?.CorreoCorporativo != request.Email)
                {
                    _logger.LogWarning("Email {Email} no coincide con el token de recuperación", request.Email);
                    return false;
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                usuario.token_recuperacion = null;
                usuario.expiracion_token = null;
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                if (usuario.PersonalNavigation != null && !string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    var emailBody = EmailTemplates.BuildPasswordChangedBody(usuario.PersonalNavigation.CorreoCorporativo);
                    
                    await _emailService.SendAsync(
                        usuario.PersonalNavigation.CorreoCorporativo,
                        "Contraseña Actualizada - Sistema SLA",
                        emailBody
                    );
                }

                _logger.LogInformation("Contraseña restablecida exitosamente para {Email}. Token eliminado", request.Email);
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

        // ===========================
        // ACTIVACIÓN DE CUENTA
        // ===========================

        public async Task<bool> ActivarCuenta(ActivarCuentaDTO request)
        {
            try
            {
                var usuario = await _usuarioRepository.GetByRecoveryTokenAsync(request.Token);
                
                if (usuario == null)
                {
                    _logger.LogWarning("Intento de activar cuenta con token inválido o expirado");
                    return false;
                }

                // ⚠️ CAMBIO: Verificar email en lugar de username
                if (usuario.PersonalNavigation?.CorreoCorporativo != request.Email)
                {
                    _logger.LogWarning("Email {Email} no coincide con el token de activación", request.Email);
                    return false;
                }

                // Verificar que la cuenta esté pendiente de activación
                if (usuario.PasswordHash != null)
                {
                    _logger.LogWarning("Intento de activar cuenta ya activada: {Email}", request.Email);
                    return false;
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);
                usuario.token_recuperacion = null;
                usuario.expiracion_token = null;
                usuario.ActualizadoEn = DateTime.UtcNow;
                
                await _usuarioRepository.UpdateAsync(usuario);

                if (usuario.PersonalNavigation != null && !string.IsNullOrEmpty(usuario.PersonalNavigation.CorreoCorporativo))
                {
                    var emailBody = $@"
                        <h2>Cuenta Activada Exitosamente</h2>
                        <p>Hola,</p>
                        <p>Tu cuenta ha sido activada correctamente.</p>
                        <p>Ya puedes iniciar sesión en el Sistema SLA con tu correo electrónico.</p>
                    ";
                    
                    await _emailService.SendAsync(
                        usuario.PersonalNavigation.CorreoCorporativo,
                        "Cuenta Activada - Sistema SLA",
                        emailBody
                    );
                }

                _logger.LogInformation("Cuenta activada exitosamente para {Email}. Token eliminado", request.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar cuenta para {Email}", request.Email);
                return false;
            }
        }

        // ===========================
        // VINCULAR PERSONAL → USUARIO (ADMIN)
        // ===========================

        public async Task VincularPersonalYActivarAsync(VincularPersonalDTO dto)
        {
            try
            {
                // PASO 1: Verificar que el Personal existe
                var personal = await _personalRepository.GetByIdAsync(dto.IdPersonal);
                if (personal == null)
                {
                    _logger.LogWarning("Intento de vincular Personal inexistente: ID {IdPersonal}", dto.IdPersonal);
                    throw new InvalidOperationException($"El Personal con ID {dto.IdPersonal} no existe.");
                }

                // PASO 2: Verificar que el Personal tiene correo corporativo
                if (string.IsNullOrWhiteSpace(personal.CorreoCorporativo))
                {
                    _logger.LogWarning("Personal {IdPersonal} no tiene correo corporativo registrado", dto.IdPersonal);
                    throw new InvalidOperationException($"El Personal '{personal.Nombres} {personal.Apellidos}' no tiene correo corporativo registrado. No se puede crear la cuenta de usuario.");
                }

                // PASO 3: Verificar que el Personal NO tiene ya un usuario vinculado
                var usuariosExistentes = await _usuarioRepository.GetAllAsync();
                var personalYaTieneUsuario = usuariosExistentes.Any(u => u.IdPersonal == dto.IdPersonal);
                
                if (personalYaTieneUsuario)
                {
                    _logger.LogWarning("Personal {IdPersonal} ya tiene una cuenta de usuario vinculada", dto.IdPersonal);
                    throw new InvalidOperationException($"El Personal '{personal.Nombres} {personal.Apellidos}' ya tiene una cuenta de usuario vinculada.");
                }

                // PASO 4: Verificar que el Username no existe
                var usernameExiste = await _usuarioRepository.GetByUsernameAsync(dto.Username);
                if (usernameExiste != null)
                {
                    _logger.LogWarning("Intento de crear usuario con username existente: {Username}", dto.Username);
                    throw new InvalidOperationException($"El username '{dto.Username}' ya está en uso.");
                }

                // PASO 5: Crear el Usuario vinculado al Personal (sin contraseña = pendiente de activación)
                var nuevoUsuario = new Usuario
                {
                    Username = dto.Username,
                    PasswordHash = null, // ⚠️ NULL = pendiente de activación
                    IdRolSistema = dto.IdRolSistema,
                    IdPersonal = dto.IdPersonal,
                    Estado = "ACTIVO",
                    CreadoEn = DateTime.UtcNow
                };

                // PASO 6: Generar token de activación
                var token = GenerateSecureToken();
                nuevoUsuario.token_recuperacion = token;
                nuevoUsuario.expiracion_token = DateTime.UtcNow.AddHours(24); // 24 horas para activar

                await _usuarioRepository.AddAsync(nuevoUsuario);

                _logger.LogInformation("Usuario creado y vinculado a Personal {IdPersonal}: Username={Username}, Rol={IdRol}", 
                    dto.IdPersonal, dto.Username, dto.IdRolSistema);

                // PASO 7: Construir URL de activación
                var activacionUrl = $"{_frontendSettings.BaseUrl}/activacion-cuenta?email={Uri.EscapeDataString(personal.CorreoCorporativo)}&token={token}";
                
                _logger.LogInformation("URL de activación generada para {Username}: {Url}", dto.Username, activacionUrl);

                // PASO 8: Enviar correo de bienvenida con enlace de activación
                var emailBody = BuildActivacionBienvenidaBody(
                    personal.Nombres,
                    personal.Apellidos,
                    dto.Username,
                    activacionUrl
                );

                await _emailService.SendAsync(
                    personal.CorreoCorporativo,
                    "¡Bienvenido a SLA Manager! Activa tu Cuenta",
                    emailBody
                );

                _logger.LogInformation("Correo de activación enviado a {Email} para usuario {Username}", 
                    personal.CorreoCorporativo, dto.Username);
            }
            catch (InvalidOperationException)
            {
                // Re-lanzar las excepciones de validación para que el controlador las maneje
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al vincular Personal {IdPersonal} con cuenta de usuario", dto.IdPersonal);
                throw new Exception("Error interno al procesar la vinculación. Por favor, contacta al administrador del sistema.", ex);
            }
        }

        // ===========================
        // TEMPLATE DE EMAIL DE ACTIVACIÓN/BIENVENIDA
        // ===========================

        private static string BuildActivacionBienvenidaBody(string nombres, string apellidos, string username, string activacionUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Activación de Cuenta</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .btn {{ display: inline-block; background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ border-top: 1px solid #eee; padding-top: 20px; margin-top: 30px; font-size: 12px; color: #999; }}
        .info-box {{ background-color: #f8f9fa; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>¡Bienvenido a SLA Manager!</h1>
        </div>
        <div class='content'>
            <h2>¡Hola, {nombres} {apellidos}!</h2>
            <p>Tu cuenta para el sistema <strong>SLA Manager</strong> ha sido creada por un Administrador.</p>
            
            <div class='info-box'>
                <p><strong>Tu nombre de usuario es:</strong> <code>{username}</code></p>
            </div>
            
            <p>Para activar tu cuenta y establecer tu contraseña por primera vez, haz clic en el siguiente botón. <strong>Este enlace caducará en 24 horas.</strong></p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{activacionUrl}' class='btn'>Activar Mi Cuenta</a>
            </div>

            <p>Si no puedes hacer clic en el botón, copia y pega el siguiente enlace en tu navegador:</p>
            <p style='word-break: break-all; font-size: 11px; color: #007bff;'>{activacionUrl}</p>

            <p>Saludos cordiales,</p>
            <p><strong>El Equipo de SLA Manager</strong></p>
        </div>
        <div class='footer'>
            <p>Si no solicitaste esta activación, puedes ignorar este correo.</p>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}
