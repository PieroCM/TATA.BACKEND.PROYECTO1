using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequestDTO dto)
        {
            var token = await _usuarioService.SignInAsync(dto);
            if (token == null)
                return Unauthorized(new { message = "Credenciales inválidas" });

            return Ok(new { message = "Inicio de sesión exitoso", token });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequestDTO dto)
        {
            var success = await _usuarioService.SignUpAsync(dto);
            if (!success)
                return BadRequest(new { message = "El correo ya está registrado" });

            return Ok(new { message = "Usuario registrado correctamente" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _usuarioService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            var success = await _usuarioService.UpdateAsync(id, dto);
            if (!success) return NotFound();
            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _usuarioService.DeleteAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Usuario eliminado correctamente" });
        }
    }
}
