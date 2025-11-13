using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Esto se pone AQUÍ → a nivel de CLASE
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // ⛔ Estos dos son públicos (no necesitan token)
        [AllowAnonymous] // 🔓 Esto se pone AQUÍ → a nivel de MÉTODO
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequestDTO dto)
        {
            var token = await _usuarioService.SignInAsync(dto);
            if (token == null)
                return Unauthorized(new { message = "Credenciales inválidas" });

            return Ok(new { message = "Inicio de sesión exitoso", token });
        }

        [AllowAnonymous] // 🔓 También aquí
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequestDTO dto)
        {
            var success = await _usuarioService.SignUpAsync(dto);
            if (!success)
                return BadRequest(new { message = "El correo ya está registrado" });

            return Ok(new { message = "Usuario registrado correctamente" });
        }

        // 🔒 Todo lo demás ya está protegido con el [Authorize] de arriba
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });
            return Ok(usuario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            var success = await _usuarioService.UpdateAsync(id, dto);
            if (!success)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(new { message = "Usuario actualizado correctamente" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _usuarioService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Usuario no encontrado" });
            return Ok(new { message = "Usuario eliminado correctamente" });
        }



        //CAMBIAR CONTRASEÑAAA PEROO AHI NOMASS
        [Authorize]
        [HttpPut("cambiar-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UsuarioChangePasswordDTO dto)
        {
            var success = await _usuarioService.ChangePasswordAsync(dto);
            if (!success)
                return BadRequest(new { message = "Contraseña actual incorrecta o usuario no encontrado" });

            return Ok(new { message = "Contraseña actualizada correctamente" });
        }

    }
}
