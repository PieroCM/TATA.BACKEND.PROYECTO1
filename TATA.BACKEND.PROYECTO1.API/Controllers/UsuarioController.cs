using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Protege todos los endpoints por defecto
    public class UsuarioController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UsuarioController));
        
        private readonly IUsuarioService _usuarioService;
        private readonly ILogService _logService;

        public UsuarioController(IUsuarioService usuarioService, ILogService logService)
        {
            _usuarioService = usuarioService;
            _logService = logService;
            log.Debug("UsuarioController inicializado.");
        }

        // ===========================
        // ENDPOINTS PÚBLICOS (Sin autenticación) - NO extraen userId
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
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: SignIn", 
                $"Intento de inicio de sesión para email: {dto?.Email}", null);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignIn: Validación fallida - Email y contraseña son obligatorios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Email y contraseña son obligatorios", 
                    "Credenciales incompletas en SignIn", null);
                return BadRequest(new { message = "Email y contraseña son obligatorios" });
            }

            try
            {
                var result = await _usuarioService.SignInAsync(dto);
                
                if (result == null)
                {
                    log.Warn($"SignIn: Credenciales inválidas o usuario inactivo para email: {dto.Email}");
                    await _logService.RegistrarLogAsync("WARN", "SignIn fallido: Credenciales inválidas o usuario inactivo", 
                        $"Email: {dto.Email}", null);
                    return Unauthorized(new { message = "Credenciales inválidas o usuario inactivo" });
                }

                log.Info($"SignIn completado correctamente para email: {dto.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: SignIn", 
                    $"Usuario autenticado exitosamente: {dto.Email}", result.IdUsuario);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                log.Warn($"SignIn: InvalidOperationException - {ex.Message}");
                await _logService.RegistrarLogAsync("WARN", $"SignIn fallido: {ex.Message}", 
                    ex.ToString(), null);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SignIn", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en SignIn", 
                    ex.ToString(), null);
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
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: SignUp", 
                $"Registro de nuevo usuario para email: {dto?.Email}", null);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                log.Warn("SignUp: Validación fallida - Email y contraseña son obligatorios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Email y contraseña son obligatorios", 
                    "Datos incompletos en SignUp", null);
                return BadRequest(new { message = "Email y contraseña son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.SignUpAsync(dto);
                
                if (!success)
                {
                    log.Warn($"SignUp: El email ya está registrado - {dto.Email}");
                    await _logService.RegistrarLogAsync("WARN", "SignUp fallido: El email ya está registrado", 
                        $"Email: {dto.Email}", null);
                    return BadRequest(new { message = "El email ya está registrado" });
                }

                log.Info($"SignUp completado correctamente para email: {dto.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: SignUp", 
                    $"Usuario registrado exitosamente: {dto.Email}", null);

                return Ok(new { message = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SignUp", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en SignUp", 
                    ex.ToString(), null);
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
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Solicitar Recuperación", 
                $"Solicitud de recuperación para email: {request?.Email}", null);

            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                log.Warn("SolicitarRecuperacion: Validación fallida - El email es obligatorio");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: El email es obligatorio", 
                    "Email no proporcionado en solicitud de recuperación", null);
                return BadRequest(new { message = "El email es obligatorio" });
            }

            try
            {
                await _usuarioService.SolicitarRecuperacionPassword(request);
                
                log.Info($"SolicitarRecuperacion completado para email: {request.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Solicitar Recuperación", 
                    $"Solicitud procesada para email: {request.Email}", null);

                return Ok(new { message = "Si el email existe y tiene correo vinculado, recibirás un enlace de recuperación" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante SolicitarRecuperacion", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en SolicitarRecuperacion", 
                    ex.ToString(), null);
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
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Restablecer Password", 
                $"Intento de restablecimiento para email: {request?.Email}", null);

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                log.Warn("RestablecerPassword: Validación fallida - Email, token y nueva contraseña son obligatorios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Email, token y nueva contraseña son obligatorios", 
                    "Datos incompletos en restablecimiento de contraseña", null);
                return BadRequest(new { message = "Email, token y nueva contraseña son obligatorios" });
            }

            try
            {
                var resultado = await _usuarioService.RestablecerPassword(request);

                if (!resultado)
                {
                    log.Warn($"RestablecerPassword: Token inválido o expirado para email: {request.Email}");
                    await _logService.RegistrarLogAsync("WARN", "RestablecerPassword fallido: Token inválido o expirado", 
                        $"Email: {request.Email}", null);
                    return BadRequest(new { message = "Token inválido o expirado. Solicita uno nuevo" });
                }

                log.Info($"RestablecerPassword completado correctamente para email: {request.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Restablecer Password", 
                    $"Contraseña actualizada para email: {request.Email}", null);

                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante RestablecerPassword", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en RestablecerPassword", 
                    ex.ToString(), null);
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
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Activar Cuenta", 
                $"Activación de cuenta para email: {request?.Email}", null);

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                log.Warn("ActivarCuenta: Validación fallida - Email, token y contraseña son obligatorios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Email, token y contraseña son obligatorios", 
                    "Datos incompletos en activación de cuenta", null);
                return BadRequest(new { message = "Email, token y contraseña son obligatorios" });
            }

            try
            {
                var resultado = await _usuarioService.ActivarCuenta(request);

                if (!resultado)
                {
                    log.Warn($"ActivarCuenta: Token inválido, expirado o cuenta ya activada para email: {request.Email}");
                    await _logService.RegistrarLogAsync("WARN", "ActivarCuenta fallido: Token inválido, expirado o cuenta ya activada", 
                        $"Email: {request.Email}", null);
                    return BadRequest(new { message = "Token inválido, expirado o cuenta ya activada" });
                }

                log.Info($"ActivarCuenta completado correctamente para email: {request.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Activar Cuenta", 
                    $"Cuenta activada exitosamente para email: {request.Email}", null);

                return Ok(new { message = "Cuenta activada exitosamente. Ya puedes iniciar sesión." });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante ActivarCuenta", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en ActivarCuenta", 
                    ex.ToString(), null);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ===========================
        // GESTIÓN DE USUARIOS (Requiere autenticación) - SÍ extraen userId
        // ===========================

        /// <summary>
        /// Obtener todos los usuarios
        /// GET /api/usuario
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetAll iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetAll Usuario", 
                "Obteniendo todos los usuarios", userId);

            try
            {
                var usuarios = await _usuarioService.GetAllAsync();
                
                log.Info($"GetAll completado correctamente, {usuarios.Count()} usuarios obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetAll Usuario", 
                    $"Total usuarios obtenidos: {usuarios.Count()}", userId);
                
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetAll Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetById iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetById Usuario", 
                $"Buscando Usuario con id: {id}", userId);

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                
                if (usuario == null)
                {
                    log.Warn($"Usuario con id {id} no encontrado");
                    await _logService.RegistrarLogAsync("WARN", $"Usuario no encontrado: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetById Usuario", 
                    $"Usuario {id} obtenido exitosamente", userId);

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetById Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Create iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Create Usuario", 
                $"Creando Usuario: {dto?.Username}", userId);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                log.Warn("Create: Validación fallida - Username es obligatorio");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Username es obligatorio", 
                    "El cuerpo de la petición no cumple con los requisitos", userId);
                return BadRequest(new { message = "Username es obligatorio" });
            }

            try
            {
                var usuario = await _usuarioService.CreateAsync(dto);
                
                if (usuario == null)
                {
                    log.Warn("Create: No se pudo crear el usuario - Username podría estar en uso");
                    await _logService.RegistrarLogAsync("WARN", "No se pudo crear el usuario", 
                        "El username podría estar en uso", userId);
                    return BadRequest(new { message = "No se pudo crear el usuario. El username podría estar en uso" });
                }

                log.Info($"Create completado correctamente, IdUsuario: {usuario.IdUsuario}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Create Usuario", 
                    $"Usuario creado con id: {usuario.IdUsuario}, Username: {usuario.Username}", userId);

                return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario }, usuario);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Create Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Update iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Update Usuario", 
                $"Actualizando Usuario con id: {id}", userId);

            if (dto == null)
            {
                log.Warn($"Update: Validación fallida - Datos inválidos para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Datos inválidos", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest(new { message = "Datos inválidos" });
            }

            try
            {
                var success = await _usuarioService.UpdateAsync(id, dto);
                
                if (!success)
                {
                    log.Warn($"Update: Usuario no encontrado o correo ya existe para id: {id}");
                    await _logService.RegistrarLogAsync("WARN", $"Usuario no encontrado o correo ya existe: {id}", 
                        "No se pudo actualizar el usuario", userId);
                    return NotFound(new { message = "Usuario no encontrado o correo ya existe" });
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Update Usuario", 
                    $"Usuario {id} actualizado exitosamente", userId);

                return Ok(new { message = "Usuario actualizado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Update Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Delete iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Delete Usuario", 
                $"Eliminando Usuario con id: {id}", userId);

            try
            {
                var success = await _usuarioService.DeleteAsync(id);
                
                if (!success)
                {
                    log.Warn($"Usuario con id {id} no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", $"Usuario no encontrado para eliminar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Delete Usuario", 
                    $"Usuario {id} eliminado exitosamente", userId);

                return Ok(new { message = "Usuario eliminado correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Delete Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"ToggleEstado iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: ToggleEstado Usuario", 
                $"Cambiando estado de Usuario con id: {id} a {dto?.Estado}", userId);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
            {
                log.Warn("ToggleEstado: Validación fallida - Estado es obligatorio");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Estado es obligatorio (ACTIVO o INACTIVO)", 
                    "Estado no proporcionado o inválido", userId);
                return BadRequest(new { message = "Estado es obligatorio (ACTIVO o INACTIVO)" });
            }

            if (dto.Estado != "ACTIVO" && dto.Estado != "INACTIVO")
            {
                log.Warn($"ToggleEstado: Validación fallida - Estado inválido: {dto.Estado}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Estado debe ser ACTIVO o INACTIVO", 
                    $"Estado recibido: {dto.Estado}", userId);
                return BadRequest(new { message = "Estado debe ser ACTIVO o INACTIVO" });
            }

            try
            {
                var success = await _usuarioService.ToggleEstadoAsync(id, dto);
                
                if (!success)
                {
                    log.Warn($"Usuario con id {id} no encontrado para toggle estado");
                    await _logService.RegistrarLogAsync("WARN", $"Usuario no encontrado para toggle estado: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                log.Info($"ToggleEstado completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: ToggleEstado Usuario", 
                    $"Usuario {id} cambiado a {dto.Estado} correctamente", userId);

                return Ok(new { message = $"Usuario {dto.Estado.ToLower()} correctamente" });
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante ToggleEstado para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en ToggleEstado Usuario", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"ChangePassword iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Cambiar Password", 
                $"Cambio de contraseña para email: {dto?.Email}", userId);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.PasswordActual) || string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                log.Warn("ChangePassword: Validación fallida - Todos los campos son obligatorios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Todos los campos son obligatorios", 
                    "Datos incompletos en cambio de contraseña", userId);
                return BadRequest(new { message = "Todos los campos son obligatorios" });
            }

            try
            {
                var success = await _usuarioService.ChangePasswordAsync(dto);
                
                if (!success)
                {
                    log.Warn($"ChangePassword: Contraseña actual incorrecta o usuario no encontrado para email: {dto.Email}");
                    await _logService.RegistrarLogAsync("WARN", "ChangePassword fallido: Contraseña actual incorrecta o usuario no encontrado", 
                        $"Email: {dto.Email}", userId);
                    return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });
                }

                log.Info($"ChangePassword completado correctamente para email: {dto.Email}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Cambiar Password", 
                    $"Contraseña actualizada para email: {dto.Email}", userId);

                return Ok(new { message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante ChangePassword", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en ChangePassword", 
                    ex.ToString(), userId);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"VincularPersonalYActivar iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Vincular Personal y Activar", 
                $"Vinculando IdPersonal: {dto?.IdPersonal}, Username: {dto?.Username}", userId);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El Username es obligatorio");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: El Username es obligatorio", 
                    "Username no proporcionado", userId);
                return BadRequest(new { message = "El Username es obligatorio" });
            }

            if (dto.IdPersonal <= 0)
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El ID del Personal es inválido");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: El ID del Personal es inválido", 
                    $"IdPersonal recibido: {dto.IdPersonal}", userId);
                return BadRequest(new { message = "El ID del Personal es inválido" });
            }

            if (dto.IdRolSistema <= 0)
            {
                log.Warn("VincularPersonalYActivar: Validación fallida - El ID del Rol Sistema es inválido");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: El ID del Rol Sistema es inválido", 
                    $"IdRolSistema recibido: {dto.IdRolSistema}", userId);
                return BadRequest(new { message = "El ID del Rol Sistema es inválido" });
            }

            try
            {
                await _usuarioService.VincularPersonalYActivarAsync(dto);
                
                log.Info($"VincularPersonalYActivar completado correctamente - IdPersonal: {dto.IdPersonal}, Username: {dto.Username}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Vincular Personal y Activar", 
                    $"Cuenta de usuario creada - IdPersonal: {dto.IdPersonal}, Username: {dto.Username}, IdRolSistema: {dto.IdRolSistema}", userId);
                
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
                await _logService.RegistrarLogAsync("WARN", $"VincularPersonalYActivar fallido: {ex.Message}", 
                    ex.ToString(), userId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                log.Error("Error interno del servidor al procesar la vinculación", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error interno del servidor al procesar la vinculación", 
                    ex.ToString(), userId);
                return StatusCode(500, new { message = "Error interno del servidor al procesar la vinculación" });
            }
        }
    }
}
