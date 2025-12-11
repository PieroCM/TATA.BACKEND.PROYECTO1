using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
        private readonly string _frontendBaseUrl;

        public PersonalService(
            IPersonalRepository personalRepository,
            IUsuarioRepository usuarioRepository,
            IEmailService emailService,
            ILogger<PersonalService> logger,
            Proyecto1SlaDbContext context,
            IConfiguration configuration)
        {
            _personalRepository = personalRepository;
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _context = context;
            _frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"] ?? "http://localhost:9000";
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

                // ⚠️ PASO 7: Construir URL absoluta de activación y enviar email (FUERA de la transacción)
                // ✅ CORRECCIÓN: Usar el correo corporativo (email) en lugar de username para consistencia con UsuarioService
                var path = $"/activacion-cuenta?email={Uri.EscapeDataString(personal.CorreoCorporativo!)}&token={token}";
                var activacionUrl = $"{_frontendBaseUrl.TrimEnd('/')}{path}";
                
                _logger.LogInformation("URL de activación generada para {Username}: {Url}", usuario.Username, activacionUrl);

                // ✅ CORRECCIÓN: Usar HTML simple sin estilos en <head> ni emojis (Gmail los filtra)
                var emailBody = $@"
                    <h2>Activacion de Cuenta - Sistema SLA</h2>
                    <p>Hola {personal.Nombres} {personal.Apellidos},</p>
                    <p>Se ha creado una cuenta de usuario para ti en el <strong>Sistema de Gestion SLA</strong>.</p>
                    <p><strong>Tu nombre de usuario es:</strong> {usuario.Username}</p>
                    <p>Para activar tu cuenta y establecer tu contraseña, haz clic en el siguiente enlace:</p>
                    <p><a href='{activacionUrl}'>Activar mi cuenta</a></p>
                    <p>O copia y pega este enlace en tu navegador:</p>
                    <p>{activacionUrl}</p>
                    <p><strong>Este enlace expirara en 24 horas.</strong></p>
                    <br/>
                    <p>Saludos,</p>
                    <p>El Equipo de SLA Manager</p>
                ";

                try
                {
                    await _emailService.SendAsync(
                        personal.CorreoCorporativo,
                        "Activacion de Cuenta - Sistema SLA",
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
                        EstadoPersonal = personal.Estado,

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
        // ✅ NUEVO: DESHABILITACIÓN ADMINISTRATIVA TOTAL CON ELIMINACIÓN CONDICIONAL (TRANSACCIONAL)
        // Desactiva el Personal (estado = INACTIVO) y OPCIONALMENTE:
        // - ELIMINA el Usuario vinculado (si eliminarUsuario = true)
        // - DESHABILITA el Usuario vinculado (si eliminarUsuario = false)
        // ===========================
        public async Task<bool> DeshabilitarPersonalYUsuarioAsync(int idPersonal, bool eliminarUsuario = false)
        {
            _logger.LogInformation("Iniciando deshabilitación administrativa para Personal ID: {IdPersonal}, Eliminar Usuario: {EliminarUsuario}", 
                idPersonal, eliminarUsuario);

            // ⚠️ INICIAR TRANSACCIÓN ATÓMICA
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ⚠️ PASO 1: Verificar que el Personal existe
                var personal = await _personalRepository.GetByIdAsync(idPersonal);
                if (personal == null)
                {
                    _logger.LogWarning("Personal con ID {IdPersonal} no encontrado", idPersonal);
                    return false;
                }

                // ⚠️ PASO 2: Actualizar estado del Personal a INACTIVO (SIEMPRE)
                personal.Estado = "INACTIVO";
                personal.ActualizadoEn = DateTime.UtcNow;
                await _personalRepository.UpdateAsync(personal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Personal ID {IdPersonal} deshabilitado (Estado = INACTIVO)", idPersonal);

                // ⚠️ PASO 3: Buscar si existe Usuario vinculado a este Personal
                var usuario = await _usuarioRepository.GetByPersonalIdAsync(idPersonal);

                if (usuario != null)
                {
                    if (eliminarUsuario)
                    {
                        // ⚠️ OPCIÓN A: ELIMINAR la cuenta de usuario (DELETE)
                        await _usuarioRepository.DeleteAsync(usuario.IdUsuario);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Usuario ID {IdUsuario} (vinculado a Personal {IdPersonal}) ELIMINADO permanentemente", 
                            usuario.IdUsuario, idPersonal);
                    }
                    else
                    {
                        // ⚠️ OPCIÓN B: DESHABILITAR la cuenta de usuario (UPDATE Estado = INACTIVO)
                        usuario.Estado = "INACTIVO";
                        usuario.ActualizadoEn = DateTime.UtcNow;
                        await _usuarioRepository.UpdateAsync(usuario);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Usuario ID {IdUsuario} (vinculado a Personal {IdPersonal}) deshabilitado (Estado = INACTIVO)", 
                            usuario.IdUsuario, idPersonal);
                    }
                }
                else
                {
                    _logger.LogInformation("Personal ID {IdPersonal} no tiene cuenta de usuario vinculada, solo se deshabilitó el Personal", idPersonal);
                }

                // ⚠️ PASO 4: COMMIT de la transacción
                await transaction.CommitAsync();

                _logger.LogInformation("Deshabilitación administrativa completada exitosamente para Personal ID: {IdPersonal}", idPersonal);
                return true;
            }
            catch (Exception ex)
            {
                // ⚠️ ROLLBACK en caso de error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al deshabilitar Personal ID {IdPersonal}. Transacción revertida", idPersonal);
                return false;
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

        // ===========================
        // ✅ NUEVO: HABILITACIÓN/REACTIVACIÓN DE PERSONAL (SIN REACTIVAR USUARIO AUTOMÁTICAMENTE)
        // 🚨 REGLA DE SEGURIDAD CRÍTICA: El Usuario NO se reactiva automáticamente
        // El administrador debe habilitarlo manualmente para garantizar seguridad
        // ===========================
        public async Task<bool> HabilitarPersonalAsync(int idPersonal)
        {
            _logger.LogInformation("Iniciando habilitación/reactivación de Personal ID: {IdPersonal}", idPersonal);

            // ⚠️ INICIAR TRANSACCIÓN ATÓMICA
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ⚠️ PASO 1: Verificar que el Personal existe
                var personal = await _personalRepository.GetByIdAsync(idPersonal);
                if (personal == null)
                {
                    _logger.LogWarning("Personal con ID {IdPersonal} no encontrado", idPersonal);
                    return false;
                }

                // ⚠️ PASO 2: Actualizar estado del Personal a ACTIVO
                personal.Estado = "ACTIVO";
                personal.ActualizadoEn = DateTime.UtcNow;
                await _personalRepository.UpdateAsync(personal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Personal ID {IdPersonal} reactivado (Estado = ACTIVO)", idPersonal);

                // 🚨 PASO 3: VERIFICAR SI EXISTE USUARIO VINCULADO (SOLO PARA LOG)
                var usuario = await _usuarioRepository.GetByPersonalIdAsync(idPersonal);

                if (usuario != null)
                {
                    // ⚠️ IMPORTANTE: NO SE REACTIVA EL USUARIO AUTOMÁTICAMENTE
                    // Esta es una decisión de SEGURIDAD
                    
                    if (usuario.Estado == "INACTIVO")
                    {
                        _logger.LogWarning(
                            "SEGURIDAD: Personal ID {IdPersonal} reactivado, pero Usuario ID {IdUsuario} PERMANECE INACTIVO. " +
                            "El administrador debe habilitarlo manualmente para garantizar seguridad (verificación de rol, restablecimiento de contraseña, etc.)",
                            idPersonal, usuario.IdUsuario
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Personal ID {IdPersonal} reactivado. Usuario ID {IdUsuario} ya está ACTIVO.",
                            idPersonal, usuario.IdUsuario
                        );
                    }
                }
                else
                {
                    _logger.LogInformation("Personal ID {IdPersonal} reactivado. No tiene cuenta de usuario vinculada.", idPersonal);
                }

                // ⚠️ PASO 4: COMMIT de la transacción
                await transaction.CommitAsync();

                _logger.LogInformation("Habilitación de Personal completada exitosamente para ID: {IdPersonal}", idPersonal);
                return true;
            }
            catch (Exception ex)
            {
                // ⚠️ ROLLBACK en caso de error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al habilitar Personal ID {IdPersonal}. Transacción revertida", idPersonal);
                return false;
            }
        }
    }
}
