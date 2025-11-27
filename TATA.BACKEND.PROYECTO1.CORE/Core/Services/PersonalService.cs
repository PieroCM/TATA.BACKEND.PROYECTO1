using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class PersonalService : IPersonalService
    {
        private readonly IPersonalRepository _personalRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PersonalService> _logger;
        private readonly FrontendSettings _frontendSettings;

        public PersonalService(
            IPersonalRepository personalRepository,
            IUsuarioRepository usuarioRepository,
            IEmailService emailService,
            ILogger<PersonalService> logger,
            IOptions<FrontendSettings> frontendSettings)
        {
            _personalRepository = personalRepository;
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _frontendSettings = frontendSettings.Value;
        }

        public async Task<IEnumerable<PersonalResponseDTO>> GetAllAsync()
        {
            var lista = await _personalRepository.GetAllAsync();
            return lista.Select(p => MapToResponseDTO(p));
        }

        public async Task<PersonalResponseDTO?> GetByIdAsync(int id)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            return p == null ? null : MapToResponseDTO(p);
        }

        public async Task<bool> CreateAsync(PersonalCreateDTO dto)
        {
            var entity = new Personal
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Documento = dto.Documento,
                CorreoCorporativo = dto.CorreoCorporativo,
                Estado = dto.Estado ?? "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };
            await _personalRepository.AddAsync(entity);
            return true;
        }

        // ⚠️ NUEVO: Crear Personal con Cuenta de Usuario Condicional
        public async Task<bool> CreateWithAccountAsync(PersonalCreateWithAccountDTO dto)
        {
            try
            {
                // PASO 1: Crear el registro de Personal
                var personal = new Personal
                {
                    Nombres = dto.Nombres,
                    Apellidos = dto.Apellidos,
                    Documento = dto.Documento,
                    CorreoCorporativo = dto.CorreoCorporativo,
                    Estado = dto.Estado ?? "ACTIVO",
                    CreadoEn = DateTime.UtcNow
                };

                await _personalRepository.AddAsync(personal);
                _logger.LogInformation("Personal creado: {Nombres} {Apellidos} (ID: {Id})", 
                    personal.Nombres, personal.Apellidos, personal.IdPersonal);

                // PASO 2: Si NO se debe crear cuenta de usuario, terminar aquí
                if (!dto.CrearCuentaUsuario)
                {
                    _logger.LogInformation("Personal {Id} creado SIN cuenta de usuario", personal.IdPersonal);
                    return true;
                }

                // PASO 3: Validaciones para crear cuenta de usuario
                if (string.IsNullOrWhiteSpace(dto.Username))
                {
                    _logger.LogWarning("Intento de crear cuenta sin username para Personal {Id}", personal.IdPersonal);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dto.CorreoCorporativo))
                {
                    _logger.LogWarning("Intento de crear cuenta sin correo para Personal {Id}", personal.IdPersonal);
                    return false;
                }

                // Verificar que el username no exista
                var existingUser = await _usuarioRepository.GetByUsernameAsync(dto.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Username {Username} ya existe", dto.Username);
                    return false;
                }

                // PASO 4: Crear cuenta de usuario vinculada
                var usuario = new Usuario
                {
                    Username = dto.Username,
                    PasswordHash = null,
                    IdRolSistema = dto.IdRolSistema ?? 4,
                    IdPersonal = personal.IdPersonal,
                    Estado = "ACTIVO",
                    CreadoEn = DateTime.UtcNow
                };

                // PASO 5: Generar token de activación
                var token = GenerateSecureToken();
                usuario.token_recuperacion = token;
                usuario.expiracion_token = DateTime.UtcNow.AddHours(24);

                await _usuarioRepository.AddAsync(usuario);
                _logger.LogInformation("Cuenta de usuario creada (pendiente de activación): {Username} para Personal {Id}", 
                    usuario.Username, personal.IdPersonal);

                // PASO 6: Construir URL de activación
                var activacionUrl = $"{_frontendSettings.BaseUrl}/activacion-cuenta?username={Uri.EscapeDataString(usuario.Username)}&token={token}";
                
                _logger.LogInformation("URL de activación generada para {Username}: {Url}", usuario.Username, activacionUrl);

                // PASO 7: Generar y enviar email de activación
                var emailBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                            .content {{ background-color: #f8f9fa; padding: 30px; }}
                            .btn {{ display: inline-block; background-color: #28a745; color: white !important; padding: 15px 40px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                            .info-box {{ background-color: #e7f3ff; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>🔑 Activación de Cuenta</h1>
                            </div>
                            <div class='content'>
                                <h2>Hola {personal.Nombres} {personal.Apellidos},</h2>
                                <p>Se ha creado una cuenta de usuario para ti en el <strong>Sistema de Gestión SLA</strong>.</p>
                                <p><strong>Tu nombre de usuario es:</strong> <code>{usuario.Username}</code></p>
                                
                                <div class='info-box'>
                                    <p>Para activar tu cuenta y establecer tu contraseña, haz clic en el siguiente botón:</p>
                                    <div style='text-align: center;'>
                                        <a href='{activacionUrl}' class='btn'>Activar Cuenta</a>
                                    </div>
                                </div>
                                
                                <p><strong>⏰ Este enlace expirará en 24 horas.</strong></p>
                                <p>Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:</p>
                                <p style='word-break: break-all; color: #007bff; font-size: 11px;'>{activacionUrl}</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                await _emailService.SendAsync(
                    personal.CorreoCorporativo,
                    "Activación de Cuenta - Sistema SLA",
                    emailBody
                );

                _logger.LogInformation("Email de activación enviado a {Email} para usuario {Username}", 
                    personal.CorreoCorporativo, usuario.Username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear Personal con cuenta de usuario");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return false;

            p.Nombres = dto.Nombres ?? p.Nombres;
            p.Apellidos = dto.Apellidos ?? p.Apellidos;
            p.Documento = dto.Documento ?? p.Documento;
            p.CorreoCorporativo = dto.CorreoCorporativo ?? p.CorreoCorporativo;
            p.Estado = dto.Estado ?? p.Estado;
            p.ActualizadoEn = DateTime.Now;

            await _personalRepository.UpdateAsync(p);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return false;

            await _personalRepository.DeleteAsync(p);
            return true;
        }

        // ===========================
        // MÉTODOS AUXILIARES
        // ===========================

        private PersonalResponseDTO MapToResponseDTO(Personal p)
        {
            return new PersonalResponseDTO
            {
                IdPersonal = p.IdPersonal,
                Nombres = p.Nombres,
                Apellidos = p.Apellidos,
                Documento = p.Documento,
                CorreoCorporativo = p.CorreoCorporativo,
                Estado = p.Estado,
                IdUsuario = p.UsuarioNavigation?.IdUsuario,
                Username = p.UsuarioNavigation?.Username,
                TieneCuentaUsuario = p.UsuarioNavigation != null,
                CuentaActivada = p.UsuarioNavigation?.PasswordHash != null
            };
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToHexString(randomBytes);
        }
    }
}
