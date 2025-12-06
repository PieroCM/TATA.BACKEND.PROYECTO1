using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data; // ⚠️ NUEVO: Para DbContext

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class PersonalService : IPersonalService
    {
        private readonly IPersonalRepository _personalRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PersonalService> _logger;
        private readonly Proyecto1SlaDbContext _context; // ⚠️ NUEVO: Para transacciones

        public PersonalService(
            IPersonalRepository personalRepository,
            IUsuarioRepository usuarioRepository,
            IEmailService emailService,
            ILogger<PersonalService> logger,
            Proyecto1SlaDbContext context) // ⚠️ NUEVO
        {
            _personalRepository = personalRepository;
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _context = context; // ⚠️ NUEVO
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
            // ⚠️ VALIDAR: Si se proporciona documento, verificar que no exista
            if (!string.IsNullOrWhiteSpace(dto.Documento))
            {
                var existe = await _personalRepository.ExisteDocumentoAsync(dto.Documento);
                if (existe)
                {
                    _logger.LogWarning("Intento de crear Personal con documento duplicado: {Documento}", dto.Documento);
                    return false;
                }
            }

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

        // ⚠️ NUEVO: Crear Personal con Cuenta de Usuario Condicional (TRANSACCIONAL)
        public async Task<bool> CreateWithAccountAsync(PersonalCreateWithAccountDTO dto)
        {
            // ⚠️ INICIAR TRANSACCIÓN
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // ⚠️ PASO 0: VALIDAR DOCUMENTO DUPLICADO
                if (!string.IsNullOrWhiteSpace(dto.Documento))
                {
                    var documentoExiste = await _personalRepository.ExisteDocumentoAsync(dto.Documento);
                    if (documentoExiste)
                    {
                        _logger.LogWarning("Intento de crear Personal con documento duplicado: {Documento}", dto.Documento);
                        return false;
                    }
                }

                // ⚠️ PASO 1: Crear el registro de Personal y GUARDAR para obtener IdPersonal
                var personal = new Personal
                {
                    Nombres = dto.Nombres,
                    Apellidos = dto.Apellidos,
                    Documento = dto.Documento,
                    CorreoCorporativo = dto.CorreoCorporativo,
                    Estado = dto.Estado ?? "ACTIVO",
                    CreadoEn = DateTime.UtcNow
                };

                _context.Personal.Add(personal);
                await _context.SaveChangesAsync(); // ⚠️ GUARDAR para obtener IdPersonal
                
                _logger.LogInformation("Personal creado: {Nombres} {Apellidos} (ID: {Id})", 
                    personal.Nombres, personal.Apellidos, personal.IdPersonal);

                // ⚠️ PASO 2: Si NO se debe crear cuenta de usuario, COMMIT y terminar
                if (!dto.CrearCuentaUsuario)
                {
                    await transaction.CommitAsync(); // ⚠️ COMMIT
                    _logger.LogInformation("Personal {Id} creado SIN cuenta de usuario", personal.IdPersonal);
                    return true;
                }

                // ⚠️ PASO 3: Validaciones para crear cuenta de usuario
                if (string.IsNullOrWhiteSpace(dto.Username))
                {
                    _logger.LogWarning("Intento de crear cuenta sin username para Personal {Id}", personal.IdPersonal);
                    await transaction.RollbackAsync(); // ⚠️ ROLLBACK
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dto.CorreoCorporativo))
                {
                    _logger.LogWarning("Intento de crear cuenta sin correo para Personal {Id}", personal.IdPersonal);
                    await transaction.RollbackAsync(); // ⚠️ ROLLBACK
                    return false;
                }

                // Verificar que el username no exista
                var existingUser = await _usuarioRepository.GetByUsernameAsync(dto.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Username {Username} ya existe", dto.Username);
                    await transaction.RollbackAsync(); // ⚠️ ROLLBACK
                    return false;
                }

                // ⚠️ PASO 4: Crear cuenta de usuario vinculada (PasswordHash = NULL)
                var usuario = new Usuario
                {
                    Username = dto.Username,
                    PasswordHash = null, // ⚠️ NULL = pendiente de activación
                    IdRolSistema = dto.IdRolSistema ?? 4,
                    IdPersonal = personal.IdPersonal, // ⚠️ Vinculado al Personal recién creado
                    Estado = "ACTIVO",
                    CreadoEn = DateTime.UtcNow
                };

                // ⚠️ PASO 5: Generar token de activación (24 horas)
                var token = GenerateSecureToken();
                usuario.token_recuperacion = token;
                usuario.expiracion_token = DateTime.UtcNow.AddHours(24);

                _context.Usuario.Add(usuario);
                await _context.SaveChangesAsync(); // ⚠️ GUARDAR Usuario
                
                _logger.LogInformation("Cuenta de usuario creada (pendiente de activación): {Username} para Personal {Id}", 
                    usuario.Username, personal.IdPersonal);

                // ⚠️ PASO 6: COMMIT de la transacción
                await transaction.CommitAsync();
                
                _logger.LogInformation("Transacción completada exitosamente para Personal {Id}", personal.IdPersonal);

                // ⚠️ PASO 7: Construir URL de activación y enviar email (FUERA de la transacción)
                // Using relative path because frontend URL is managed externally (Quasar)
                var activacionUrl = $"/activacion-cuenta?username={Uri.EscapeDataString(usuario.Username)}&token={token}";
                
                _logger.LogInformation("URL de activación generada para {Username}: {Url}", usuario.Username, activacionUrl);

                // Generar y enviar email de activación
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

                try
                {
                    await _emailService.SendAsync(
                        personal.CorreoCorporativo,
                        "Activación de Cuenta - Sistema SLA",
                        emailBody
                    );

                    _logger.LogInformation("Email de activación enviado a {Email} para usuario {Username}", 
                        personal.CorreoCorporativo, usuario.Username);
                }
                catch (Exception emailEx)
                {
                    // ⚠️ Si falla el email, NO hacer rollback (los datos ya están guardados)
                    _logger.LogError(emailEx, "Error al enviar email de activación a {Email}. Usuario creado pero sin email", 
                        personal.CorreoCorporativo);
                }

                return true;
            }
            catch (Exception ex)
            {
                // ⚠️ ROLLBACK en caso de error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear Personal con cuenta de usuario. Transacción revertida");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return false;

            // ⚠️ VALIDAR: Si se actualiza el documento, verificar que no exista en otro registro
            if (!string.IsNullOrWhiteSpace(dto.Documento) && dto.Documento != p.Documento)
            {
                var existe = await _personalRepository.ExisteDocumentoAsync(dto.Documento, id);
                if (existe)
                {
                    _logger.LogWarning("Intento de actualizar Personal {Id} con documento duplicado: {Documento}", id, dto.Documento);
                    return false;
                }
            }

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
        // ✅ NUEVO: LISTADO UNIFICADO CON LEFT JOIN
        // Personal → Usuario → RolesSistema
        // ===========================
        public async Task<IEnumerable<PersonalUsuarioResponseDTO>> GetUnifiedListAsync()
        {
            try
            {
                // Obtener todos los registros de Personal con sus relaciones cargadas
                var personales = await _personalRepository.GetAllAsync();
                
                // Obtener todos los usuarios con sus relaciones de Rol
                var usuarios = await _usuarioRepository.GetAllAsync();

                // Realizar el LEFT JOIN en memoria
                var resultado = personales.Select(personal =>
                {
                    // Buscar el usuario vinculado a este Personal (puede ser null)
                    var usuario = usuarios.FirstOrDefault(u => u.IdPersonal == personal.IdPersonal);

                    return new PersonalUsuarioResponseDTO
                    {
                        // ========== DATOS DE PERSONAL (Siempre presentes) ==========
                        IdPersonal = personal.IdPersonal,
                        Nombres = personal.Nombres,
                        Apellidos = personal.Apellidos,
                        Documento = personal.Documento,
                        CorreoCorporativo = personal.CorreoCorporativo,

                        // ========== DATOS DE USUARIO (Pueden ser NULL) ==========
                        IdUsuario = usuario?.IdUsuario,
                        Username = usuario?.Username,
                        EstadoCuentaAcceso = usuario?.Estado,
                        CuentaActivada = usuario?.PasswordHash != null,

                        // ========== DATOS DE ROL (Pueden ser NULL) ==========
                        NombreRol = usuario?.IdRolSistemaNavigation?.Nombre,

                        // Fecha de creación tomada del registro de Personal
                        CreadoEn = personal.CreadoEn
                    };
                }).ToList();

                _logger.LogInformation("Listado unificado generado: {Count} registros (Personal con/sin Usuario)", resultado.Count);
                
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar listado unificado de Personal y Usuarios");
                throw;
            }
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
