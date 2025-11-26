using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Protege todos los endpoints por defecto
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
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
            if (dto == null || string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Correo y contraseña son obligatorios" });

            var token = await _usuarioService.SignInAsync(dto);
            if (token == null)
                return Unauthorized(new { message = "Credenciales inválidas o usuario inactivo" });

            return Ok(new { message = "Inicio de sesión exitoso", token });
        }

        /// <summary>
        /// Registrar nuevo usuario (auto-registro público)
        /// POST /api/usuario/signup
        /// </summary>
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequestDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || 
                string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Todos los campos son obligatorios" });

            var success = await _usuarioService.SignUpAsync(dto);
            if (!success)
                return BadRequest(new { message = "El correo ya está registrado" });

            return Ok(new { message = "Usuario registrado correctamente" });
        }

        /// <summary>
        /// Solicitar recuperación de contraseña
        /// POST /api/usuario/solicitar-recuperacion
        /// </summary>
        [AllowAnonymous]
        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "El correo electrónico es obligatorio" });

            await _usuarioService.SolicitarRecuperacionPassword(request);
            
            // Por seguridad, siempre devuelve OK aunque el email no exista
            return Ok(new { message = "Si el correo existe, recibirás un enlace de recuperación" });
        }

        /// <summary>
        /// Restablecer contraseña con token
        /// POST /api/usuario/restablecer-password
        /// </summary>
        [AllowAnonymous]
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDTO request)
        {
            if (request == null || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NuevaPassword))
            {
                return BadRequest(new { message = "Email, token y nueva contraseña son obligatorios" });
            }

            var resultado = await _usuarioService.RestablecerPassword(request);
            
            if (!resultado)
                return BadRequest(new { message = "Token inválido o expirado. Solicita uno nuevo" });

            return Ok(new { message = "Contraseña actualizada exitosamente" });
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
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        /// <summary>
        /// Obtener usuario por ID
        /// GET /api/usuario/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });
            
            return Ok(usuario);
        }

        /// <summary>
        /// Crear nuevo usuario (Administrador)
        /// POST /api/usuario
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UsuarioCreateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || 
                string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Username, correo y contraseña son obligatorios" });

            var usuario = await _usuarioService.CreateAsync(dto);
            if (usuario == null)
                return BadRequest(new { message = "No se pudo crear el usuario. El correo podría estar en uso" });

            return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario }, usuario);
        }

        /// <summary>
        /// Actualizar usuario
        /// PUT /api/usuario/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Datos inválidos" });

            var success = await _usuarioService.UpdateAsync(id, dto);
            if (!success)
                return NotFound(new { message = "Usuario no encontrado o correo ya existe" });

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        /// <summary>
        /// Eliminar usuario
        /// DELETE /api/usuario/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _usuarioService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Usuario no encontrado" });
            
            return Ok(new { message = "Usuario eliminado correctamente" });
        }

        /// <summary>
        /// Habilitar/Deshabilitar usuario
        /// PATCH /api/usuario/{id}/toggle-estado
        /// </summary>
        [HttpPatch("{id}/toggle-estado")]
        public async Task<IActionResult> ToggleEstado(int id, [FromBody] UsuarioToggleEstadoDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
                return BadRequest(new { message = "Estado es obligatorio (ACTIVO o INACTIVO)" });

            if (dto.Estado != "ACTIVO" && dto.Estado != "INACTIVO")
                return BadRequest(new { message = "Estado debe ser ACTIVO o INACTIVO" });

            var success = await _usuarioService.ToggleEstadoAsync(id, dto);
            if (!success)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(new { message = $"Usuario {dto.Estado.ToLower()} correctamente" });
        }

        /// <summary>
        /// Cambiar contraseña (usuario autenticado)
        /// PUT /api/usuario/cambiar-password
        /// </summary>
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UsuarioChangePasswordDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Correo) || 
                string.IsNullOrWhiteSpace(dto.PasswordActual) || string.IsNullOrWhiteSpace(dto.NuevaPassword))
                return BadRequest(new { message = "Todos los campos son obligatorios" });

            var success = await _usuarioService.ChangePasswordAsync(dto);
            if (!success)
                return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });

            return Ok(new { message = "Contraseña actualizada correctamente" });
        }
    }
}
