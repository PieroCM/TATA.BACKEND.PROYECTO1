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
                Detalles = $"Intento de inicio de sesión para: {dto?.Correo}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignIn: Correo y/o contraseña faltantes");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Credenciales incompletas",
                    Detalles = "Correo y contraseña son obligatorios",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Correo y contraseña son obligatorios" });
            }

            try
            {
                var token = await _usuarioService.SignInAsync(dto);
                
                if (token == null)
                {
                    log.Warn($"SignIn fallido para: {dto.Correo}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Inicio de sesión fallido",
                        Detalles = $"Credenciales inválidas o usuario inactivo para: {dto.Correo}",
                        IdUsuario = null
                    });
                    return Unauthorized(new { message = "Credenciales inválidas o usuario inactivo" });
                }

                log.Info($"SignIn exitoso para: {dto.Correo}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: SignIn",
                    Detalles = $"Inicio de sesión exitoso para: {dto.Correo}",
                    IdUsuario = null
                });

                return Ok(new { message = "Inicio de sesión exitoso", token });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante SignIn para: {dto?.Correo}", ex);
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
                Detalles = $"Registro de nuevo usuario: {dto?.Correo}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || 
                string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignUp: Campos obligatorios faltantes");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Campos incompletos",
                    Detalles = "Todos los campos son obligatorios",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Todos los campos son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.SignUpAsync(dto);
                
                if (!success)
                {
                    log.Warn($"SignUp: Correo ya registrado: {dto.Correo}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Registro fallido: Correo duplicado",
                        Detalles = $"El correo ya está registrado: {dto.Correo}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "El correo ya está registrado" });
                }

                log.Info($"SignUp exitoso para: {dto.Correo}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: SignUp",
                    Detalles = $"Usuario registrado correctamente: {dto.Correo}",
                    IdUsuario = null
                });

                return Ok(new { message = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante SignUp para: {dto?.Correo}", ex);
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
                Mensaje = "Petición recibida: Solicitar recuperación password",
                Detalles = $"Solicitud de recuperación para: {request?.Email}",
                IdUsuario = null
            });

            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                log.Warn("SolicitarRecuperacion: Email faltante");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Email vacío",
                    Detalles = "El correo electrónico es obligatorio",
                    IdUsuario = null
                });
                return BadRequest(new { message = "El correo electrónico es obligatorio" });
            }

            try
            {
                await _usuarioService.SolicitarRecuperacionPassword(request);
                
                log.Info($"SolicitarRecuperacion completado para: {request.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Solicitar recuperación",
                    Detalles = $"Solicitud procesada para: {request.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Si el correo existe, recibirás un enlace de recuperación" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante SolicitarRecuperacion para: {request?.Email}", ex);
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
                Mensaje = "Petición recibida: Restablecer password",
                Detalles = $"Intento de restablecer password para: {request?.Email}",
                IdUsuario = null
            });

            if (request == null || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                log.Warn("RestablecerPassword: Campos obligatorios faltantes");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Campos incompletos",
                    Detalles = "Email, token y nueva contraseña son obligatorios",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Email, token y nueva contraseña son obligatorios" });
            }

            try
            {
                var resultado = await _usuarioService.RestablecerPassword(request);
                
                if (!resultado)
                {
                    log.Warn($"RestablecerPassword: Token inválido para: {request.Email}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Restablecer password fallido: Token inválido",
                        Detalles = $"Token inválido o expirado para: {request.Email}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Token inválido o expirado. Solicita uno nuevo" });
                }

                log.Info($"RestablecerPassword exitoso para: {request.Email}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Restablecer password",
                    Detalles = $"Contraseña actualizada para: {request.Email}",
                    IdUsuario = null
                });

                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante RestablecerPassword para: {request?.Email}", ex);
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
                Mensaje = "Petición recibida: GetAll Usuarios",
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
                    Mensaje = "Operación completada correctamente: GetAll Usuarios",
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
                Detalles = $"Buscando usuario con id: {id}",
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
                Detalles = $"Creando usuario: {dto?.Username}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || 
                string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("Create: Campos obligatorios faltantes");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Campos incompletos",
                    Detalles = "Username, correo y contraseña son obligatorios",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Username, correo y contraseña son obligatorios" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Create: Validación de ModelState fallida");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
                return BadRequest(ModelState);
            }

            try
            {
                var usuario = await _usuarioService.CreateAsync(dto);
                
                if (usuario == null)
                {
                    log.Warn($"No se pudo crear usuario: {dto.Correo}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Create fallido: Correo en uso",
                        Detalles = $"El correo podría estar en uso: {dto.Correo}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "No se pudo crear el usuario. El correo podría estar en uso" });
                }

                log.Info($"Create completado correctamente, IdUsuario: {usuario.IdUsuario}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create Usuario",
                    Detalles = $"Usuario creado con id: {usuario.IdUsuario}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario }, usuario);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Create para: {dto?.Correo}", ex);
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
                Detalles = $"Actualizando usuario con id: {id}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn($"Update recibió dto nulo para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "Datos inválidos",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Datos inválidos" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Update: Validación de ModelState fallida para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _usuarioService.UpdateAsync(id, dto);
                
                if (!success)
                {
                    log.Warn($"Usuario con id {id} no encontrado o correo duplicado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Update fallido para usuario: {id}",
                        Detalles = "Usuario no encontrado o correo ya existe",
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
                Detalles = $"Eliminando usuario con id: {id}",
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
                Detalles = $"Cambiando estado de usuario con id: {id} a: {dto?.Estado}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
            {
                log.Warn($"ToggleEstado: Estado faltante para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Estado vacío",
                    Detalles = "Estado es obligatorio (ACTIVO o INACTIVO)",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Estado es obligatorio (ACTIVO o INACTIVO)" });
            }

            if (dto.Estado != "ACTIVO" && dto.Estado != "INACTIVO")
            {
                log.Warn($"ToggleEstado: Estado inválido para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Estado inválido",
                    Detalles = $"Estado debe ser ACTIVO o INACTIVO, recibido: {dto.Estado}",
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
                    Detalles = $"Usuario {id} cambiado a {dto.Estado}",
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
                Mensaje = "Petición recibida: ChangePassword",
                Detalles = $"Cambio de contraseña para: {dto?.Correo}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Correo) || 
                string.IsNullOrWhiteSpace(dto.PasswordActual) || string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                log.Warn("ChangePassword: Campos obligatorios faltantes");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Campos incompletos",
                    Detalles = "Todos los campos son obligatorios",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Todos los campos son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.ChangePasswordAsync(dto);
                
                if (!success)
                {
                    log.Warn($"ChangePassword fallido para: {dto.Correo}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "ChangePassword fallido",
                        Detalles = $"Contraseña actual incorrecta o usuario no encontrado: {dto.Correo}",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });
                }

                log.Info($"ChangePassword completado correctamente para: {dto.Correo}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: ChangePassword",
                    Detalles = $"Contraseña actualizada para: {dto.Correo}",
                    IdUsuario = null
                });

                return Ok(new { message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante ChangePassword para: {dto?.Correo}", ex);
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
    }
}
