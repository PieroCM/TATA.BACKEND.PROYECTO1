using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Protege todos los endpoints por defecto
    public class UsuarioController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UsuarioController));
        
        private readonly IUsuarioService _usuarioService;
        private readonly ILogSistemaService _logService;

        public UsuarioController(IUsuarioService usuarioService, ILogSistemaService logService)
        {
            _usuarioService = usuarioService;
            _logService = logService;
            log.Debug("UsuarioController inicializado.");
        }

        // ===========================
        // ENDPOINTS PÚBLICOS (Sin autenticación)
        // ===========================

        /// <summary>
        /// Iniciar sesión
        /// POST /api/usuario/signin
        /// </summary>
        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequestDTO dto)
        {
            log.Info("SignIn iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: SignIn",
                Detalles = $"Intento de inicio de sesión para email: {dto?.Email}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignIn: Validación fallida - Email y contraseña son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Email y contraseña son obligatorios",
                    Detalles = "Credenciales incompletas en SignIn",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Email y contraseña son obligatorios" });
            }

            try
            {
                var result = await _usuarioService.SignInAsync(dto);
                
                if (result == null)
                {
                    log.Warn($"SignIn: Credenciales inválidas o usuario inactivo para email: {dto.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "SignIn fallido: Credenciales inválidas o usuario inactivo",
                        Detalles = $"Email: {dto.Email}",
                        IdUsuario = null
                    });
                    return Unauthorized(new { message = "Credenciales inválidas o usuario inactivo" });
                }

                log.Info($"SignIn completado correctamente para email: {dto.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: SignIn",
                    Detalles = $"Usuario autenticado exitosamente: {dto.Email}",
                    IdUsuario = null
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                log.Warn($"SignIn: InvalidOperationException - {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"SignIn fallido: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SignIn", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Registrar nuevo usuario (auto-registro público)
        /// POST /api/usuario/signup
        /// </summary>
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequestDTO dto)
        {
            log.Info("SignUp iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: SignUp",
                Detalles = $"Registro de nuevo usuario para email: {dto?.Email}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignUp: Validación fallida - Email y contraseña son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Email y contraseña son obligatorios",
                    Detalles = "Datos incompletos en SignUp",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Email y contraseña son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.SignUpAsync(dto);
                
                if (!success)
                {
                    log.Warn($"SignUp: El email ya está registrado - {dto.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "SignUp fallido: El email ya está registrado",
                        Detalles = $"Email: {dto.Email}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "El email ya está registrado" });
                }

                log.Info($"SignUp completado correctamente para email: {dto.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: SignUp",
                    Detalles = $"Usuario registrado exitosamente: {dto.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SignUp", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// POST /api/usuario/solicitar-recuperacion
        /// </summary>
        [AllowAnonymous]
        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionDTO request)
        {
            log.Info("SolicitarRecuperacion iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Solicitar Recuperación",
                Detalles = $"Solicitud de recuperación para email: {request?.Email}",
                IdUsuario = null
            });

            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                log.Warn("SolicitarRecuperacion: Validación fallida - El email es obligatorio");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: El email es obligatorio",
                    Detalles = "Email no proporcionado en solicitud de recuperación",
                    IdUsuario = null
                });
                return BadRequest(new { message = "El email es obligatorio" });
            }

            try
            {
                await _usuarioService.SolicitarRecuperacionPassword(request);
                
                log.Info($"SolicitarRecuperacion completado para email: {request.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Solicitar Recuperación",
                    Detalles = $"Solicitud procesada para email: {request.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Si el email existe y tiene correo vinculado, recibirás un enlace de recuperación" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SolicitarRecuperacion", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Restablecer contraseña con token
        /// POST /api/usuario/restablecer-password
        /// </summary>
        [AllowAnonymous]
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDTO request)
        {
            log.Info("RestablecerPassword iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Restablecer Password",
                Detalles = $"Intento de restablecimiento para email: {request?.Email}",
                IdUsuario = null
            });

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                log.Warn("RestablecerPassword: Validación fallida - Email, token y nueva contraseña son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Email, token y nueva contraseña son obligatorios",
                    Detalles = "Datos incompletos en restablecimiento de contraseña",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Email, token y nueva contraseña son obligatorios" });
            }

            try
            {
                var resultado = await _usuarioService.RestablecerPassword(request);

                if (!resultado)
                {
                    log.Warn($"RestablecerPassword: Token inválido o expirado para email: {request.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "RestablecerPassword fallido: Token inválido o expirado",
                        Detalles = $"Email: {request.Email}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Token inválido o expirado. Solicita uno nuevo" });
                }

                log.Info($"RestablecerPassword completado correctamente para email: {request.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Restablecer Password",
                    Detalles = $"Contraseña actualizada para email: {request.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante RestablecerPassword", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Activar cuenta con token (para cuentas recién creadas)
        /// POST /api/usuario/activar-cuenta
        /// </summary>
        [AllowAnonymous]
        [HttpPost("activar-cuenta")]
        public async Task<IActionResult> ActivarCuenta([FromBody] ActivarCuentaDTO request)
        {
            log.Info("ActivarCuenta iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Activar Cuenta",
                Detalles = $"Activación de cuenta para email: {request?.Email}",
                IdUsuario = null
            });

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                log.Warn("ActivarCuenta: Validación fallida - Email, token y contraseña son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Email, token y contraseña son obligatorios",
                    Detalles = "Datos incompletos en activación de cuenta",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Email, token y contraseña son obligatorios" });
            }

            try
            {
                var resultado = await _usuarioService.ActivarCuenta(request);

                if (!resultado)
                {
                    log.Warn($"ActivarCuenta: Token inválido, expirado o cuenta ya activada para email: {request.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ActivarCuenta fallido: Token inválido, expirado o cuenta ya activada",
                        Detalles = $"Email: {request.Email}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Token inválido, expirado o cuenta ya activada" });
                }

                log.Info($"ActivarCuenta completado correctamente para email: {request.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Activar Cuenta",
                    Detalles = $"Cuenta activada exitosamente para email: {request.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Cuenta activada exitosamente. Ya puedes iniciar sesión." });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante ActivarCuenta", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ===========================
        // GESTIÓN DE USUARIOS (Requiere autenticación)
        // ===========================

        /// <summary>
        /// Obtener todos los usuarios
        /// GET /api/usuario
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll Usuario",
                Detalles = "Obteniendo todos los usuarios",
                IdUsuario = null
            });

            try
            {
                var usuarios = await _usuarioService.GetAllAsync();
                
                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll Usuario",
                    Detalles = $"Total usuarios obtenidos: {usuarios.Count()}",
                    IdUsuario = null
                });
                
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtener usuario por ID
        /// GET /api/usuario/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById Usuario {id}",
                Detalles = $"Buscando Usuario con id: {id}",
                IdUsuario = null
            });

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                
                if (usuario == null)
                {
                    log.Warn($"Usuario con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Usuario no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById Usuario",
                    Detalles = $"Usuario {id} obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crear nuevo usuario (Administrador)
        /// POST /api/usuario
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UsuarioCreateDTO dto)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create Usuario",
                Detalles = $"Creando Usuario: {dto?.Username}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                log.Warn("Create: Validación fallida - Username es obligatorio");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Username es obligatorio",
                    Detalles = "El cuerpo de la petición no cumple con los requisitos",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Username es obligatorio" });
            }

            try
            {
                var usuario = await _usuarioService.CreateAsync(dto);
                
                if (usuario == null)
                {
                    log.Warn("Create: No se pudo crear el usuario - Username podría estar en uso");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "No se pudo crear el usuario",
                        Detalles = "El username podría estar en uso",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "No se pudo crear el usuario. El username podría estar en uso" });
                }

                log.Info($"Create completado correctamente, IdUsuario: {usuario.IdUsuario}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create Usuario",
                    Detalles = $"Usuario creado con id: {usuario.IdUsuario}, Username: {usuario.Username}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario }, usuario);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar usuario
        /// PUT /api/usuario/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            log.Info($"Update iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update Usuario {id}",
                Detalles = $"Actualizando Usuario con id: {id}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn($"Update: Validación fallida - Datos inválidos para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Datos inválidos",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Datos inválidos" });
            }

            try
            {
                var success = await _usuarioService.UpdateAsync(id, dto);
                
                if (!success)
                {
                    log.Warn($"Update: Usuario no encontrado o correo ya existe para id: {id}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Usuario no encontrado o correo ya existe: {id}",
                        Detalles = "No se pudo actualizar el usuario",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Usuario no encontrado o correo ya existe" });
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update Usuario",
                    Detalles = $"Usuario {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return Ok(new { message = "Usuario actualizado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar usuario
        /// DELETE /api/usuario/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete Usuario {id}",
                Detalles = $"Eliminando Usuario con id: {id}",
                IdUsuario = null
            });

            try
            {
                var success = await _usuarioService.DeleteAsync(id);
                
                if (!success)
                {
                    log.Warn($"Usuario con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Usuario no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete Usuario",
                    Detalles = $"Usuario {id} eliminado exitosamente",
                    IdUsuario = null
                });

                return Ok(new { message = "Usuario eliminado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Habilitar/Deshabilitar usuario
        /// PATCH /api/usuario/{id}/toggle-estado
        /// </summary>
        [HttpPatch("{id}/toggle-estado")]
        public async Task<IActionResult> ToggleEstado(int id, [FromBody] UsuarioToggleEstadoDTO dto)
        {
            log.Info($"ToggleEstado iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: ToggleEstado Usuario {id}",
                Detalles = $"Cambiando estado de Usuario con id: {id} a {dto?.Estado}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
            {
                log.Warn("ToggleEstado: Validación fallida - Estado es obligatorio");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Estado es obligatorio (ACTIVO o INACTIVO)",
                    Detalles = "Estado no proporcionado o inválido",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Estado es obligatorio (ACTIVO o INACTIVO)" });
            }

            if (dto.Estado != "ACTIVO" && dto.Estado != "INACTIVO")
            {
                log.Warn($"ToggleEstado: Validación fallida - Estado inválido: {dto.Estado}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Estado debe ser ACTIVO o INACTIVO",
                    Detalles = $"Estado recibido: {dto.Estado}",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Estado debe ser ACTIVO o INACTIVO" });
            }

            try
            {
                var success = await _usuarioService.ToggleEstadoAsync(id, dto);
                
                if (!success)
                {
                    log.Warn($"Usuario con id {id} no encontrado para toggle estado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Usuario no encontrado para toggle estado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"ToggleEstado completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: ToggleEstado Usuario",
                    Detalles = $"Usuario {id} cambiado a {dto.Estado} correctamente",
                    IdUsuario = null
                });

                return Ok(new { message = $"Usuario {dto.Estado.ToLower()} correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante ToggleEstado para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Cambiar contraseña (usuario autenticado)
        /// PUT /api/usuario/cambiar-password
        /// </summary>
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UsuarioChangePasswordDTO dto)
        {
            log.Info("ChangePassword iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Cambiar Password",
                Detalles = $"Cambio de contraseña para email: {dto?.Email}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.PasswordActual) || string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                log.Warn("ChangePassword: Validación fallida - Todos los campos son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Todos los campos son obligatorios",
                    Detalles = "Datos incompletos en cambio de contraseña",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Todos los campos son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.ChangePasswordAsync(dto);
                
                if (!success)
                {
                    log.Warn($"ChangePassword: Contraseña actual incorrecta o usuario no encontrado para email: {dto.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ChangePassword fallido: Contraseña actual incorrecta o usuario no encontrado",
                        Detalles = $"Email: {dto.Email}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });
                }

                log.Info($"ChangePassword completado correctamente para email: {dto.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Cambiar Password",
                    Detalles = $"Contraseña actualizada para email: {dto.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante ChangePassword", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ===========================
        // VINCULAR PERSONAL → USUARIO (SOLO ADMIN)
        // ===========================

        /// <summary>
        /// Vincular un Personal existente con una nueva cuenta de Usuario (SOLO ADMIN)
        /// POST /api/usuario/vincular-personal
        /// </summary>
        [Authorize(Roles = "1")]
        [HttpPost("vincular-personal")]
        public async Task<IActionResult> VincularPersonalYActivar([FromBody] VincularPersonalDTO dto)
        {
            log.Info("VincularPersonalYActivar iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Vincular Personal y Activar",
                Detalles = $"Vinculando IdPersonal: {dto?.IdPersonal}, Username: {dto?.Username}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El Username es obligatorio");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: El Username es obligatorio",
                    Detalles = "Username no proporcionado",
                    IdUsuario = null
                });
                return BadRequest(new { message = "El Username es obligatorio" });
            }

            if (dto.IdPersonal <= 0)
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El ID del Personal es inválido");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: El ID del Personal es inválido",
                    Detalles = $"IdPersonal recibido: {dto.IdPersonal}",
                    IdUsuario = null
                });
                return BadRequest(new { message = "El ID del Personal es inválido" });
            }

            if (dto.IdRolSistema <= 0)
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El ID del Rol Sistema es inválido");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: El ID del Rol Sistema es inválido",
                    Detalles = $"IdRolSistema recibido: {dto.IdRolSistema}",
                    IdUsuario = null
                });
                return BadRequest(new { message = "El ID del Rol Sistema es inválido" });
            }

            try
            {
                await _usuarioService.VincularPersonalYActivarAsync(dto);
                
                log.Info($"VincularPersonalYActivar completado correctamente - IdPersonal: {dto.IdPersonal}, Username: {dto.Username}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Vincular Personal y Activar",
                    Detalles = $"Cuenta de usuario creada - IdPersonal: {dto.IdPersonal}, Username: {dto.Username}, IdRolSistema: {dto.IdRolSistema}",
                    IdUsuario = null
                });
                
                return Ok(new 
                { 
                    message = "Cuenta de usuario creada y correo de activación enviado correctamente",
                    detalles = new
                    {
                        idPersonal = dto.IdPersonal,
                        username = dto.Username,
                        idRolSistema = dto.IdRolSistema,
                        instrucciones = "El usuario recibirá un correo con el enlace de activación. Tiene 24 horas para activar su cuenta."
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                // Errores de validación de negocio (Personal no existe, ya tiene cuenta, username duplicado, etc.)
                log.Warn($"VincularPersonalYActivar: InvalidOperationException - {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = $"VincularPersonalYActivar fallido: {ex.Message}",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                log.Error("Error interno del servidor al procesar la vinculación", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { message = "Error interno del servidor al procesar la vinculación" });
            }
        }
    }
}
