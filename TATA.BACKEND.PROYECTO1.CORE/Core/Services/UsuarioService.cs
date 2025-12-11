using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IPersonalRepository _personalRepository; // ⚠️ NUEVO
        private readonly IJWTService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UsuarioService> _logger;
        private readonly string _frontendBaseUrl;

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IPersonalRepository personalRepository, // ⚠️ NUEVO
            IJWTService jwtService,
            IEmailService emailService,
            ILogger<UsuarioService> logger,
            IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _personalRepository = personalRepository; // ⚠️ NUEVO
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:9000";
        }

        // ===========================
        // AUTENTICACIÓN
        // ===========================

        public async Task<SignInResponseDTO?> SignInAsync(SignInRequestDTO dto)
        {
            // ⚠️ CAMBIO: Buscar por email en lugar de username (incluye rol y permisos)
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

            // Generar token JWT
            var token = _jwtService.GenerateJWToken(usuario);

            // Construir respuesta completa con datos del usuario, rol y permisos
            var response = new SignInResponseDTO
            {
                Token = token,
                IdUsuario = usuario.IdUsuario,
                Username = usuario.Username,
                Email = usuario.PersonalNavigation?.CorreoCorporativo ?? string.Empty,
                IdPersonal = usuario.IdPersonal,
                Nombres = usuario.PersonalNavigation?.Nombres,
                Apellidos = usuario.PersonalNavigation?.Apellidos,
                IdRolSistema = usuario.IdRolSistema,
                RolCodigo = usuario.IdRolSistemaNavigation?.Codigo ?? string.Empty,
                RolNombre = usuario.IdRolSistemaNavigation?.Nombre ?? string.Empty,
                Permisos = usuario.IdRolSistemaNavigation?.IdPermiso
                    .Select(p => p.Codigo)
                    .Distinct()
                    .ToList() ?? new List<string>()
            };

            _logger.LogInformation("Login exitoso: {Email} - Username: {Username} - Rol: {Rol} - Permisos: {Permisos}",
                dto.Email, usuario.Username, response.RolCodigo, string.Join(", ", response.Permisos));

            return response;
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

                // ⚠️ CAMBIO: Construir URL absoluta para el frontend
                var path = $"/forgot-password?email={Uri.EscapeDataString(request.Email)}&token={token}";
                var recoveryUrl = $"{_frontendBaseUrl.TrimEnd('/')}{path}";

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

                // ⚠️ MEJORA: Detectar si request.Email es realmente un EMAIL o un USERNAME
                string emailParaValidar = request.Email;

                // Si NO contiene '@', asumir que es un username
                if (!request.Email.Contains("@"))
                {
                    _logger.LogInformation("Detectado username en lugar de email: {Username}. Buscando correo corporativo...", request.Email);

                    // Buscar usuario por username
                    var usuarioPorUsername = await _usuarioRepository.GetByUsernameAsync(request.Email);

                    if (usuarioPorUsername == null)
                    {
                        _logger.LogWarning("Username {Username} no existe en el sistema", request.Email);
                        return false;
                    }

                    // Verificar que el usuario encontrado por username coincida con el del token
                    if (usuarioPorUsername.IdUsuario != usuario.IdUsuario)
                    {
                        _logger.LogWarning("El username {Username} no coincide con el usuario del token", request.Email);
                        return false;
                    }

                    // Verificar que tenga Personal vinculado con correo
                    if (usuarioPorUsername.PersonalNavigation == null ||
                        string.IsNullOrWhiteSpace(usuarioPorUsername.PersonalNavigation.CorreoCorporativo))
                    {
                        _logger.LogWarning("El usuario {Username} no tiene Personal vinculado o no tiene correo corporativo", request.Email);
                        return false;
                    }

                    // Extraer el correo corporativo real
                    emailParaValidar = usuarioPorUsername.PersonalNavigation.CorreoCorporativo;

                    _logger.LogInformation("Username {Username} mapeado a correo corporativo: {Email}", request.Email, emailParaValidar);
                }

                // ⚠️ Validar contra el correo corporativo (ya sea recibido directamente o extraído del username)
                if (usuario.PersonalNavigation?.CorreoCorporativo != emailParaValidar)
                {
                    _logger.LogWarning("Email {Email} no coincide con el correo corporativo del token de activación", emailParaValidar);
                    return false;
                }

                // Verificar que la cuenta esté pendiente de activación
                if (usuario.PasswordHash != null)
                {
                    _logger.LogWarning("Intento de activar cuenta ya activada: {Email}", emailParaValidar);
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

                _logger.LogInformation("Cuenta activada exitosamente para {Email}. Token eliminado", emailParaValidar);
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

                // PASO 7: Construir URL absoluta de activación
                var path = $"/activacion-cuenta?email={Uri.EscapeDataString(personal.CorreoCorporativo)}&token={token}";
                var activacionUrl = $"{_frontendBaseUrl.TrimEnd('/')}{path}";

                _logger.LogInformation("URL de activación generada para {Username}: {Url}", dto.Username, activacionUrl);

                // PASO 8: Enviar correo de bienvenida con enlace de activación
                var emailBody = EmailTemplates.BuildActivacionBienvenidaBody(
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