using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Requiere token JWT válido
    public class PersonalController : ControllerBase
    {
        private readonly IPersonalService _personalService;

        public PersonalController(IPersonalService personalService)
        {
            _personalService = personalService;
        }

        // ✅ OBTENER TODOS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var personales = await _personalService.GetAllAsync();
            return Ok(personales);
        }

        // ✅ OBTENER UNO POR ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var personal = await _personalService.GetByIdAsync(id);
            if (personal == null)
                return NotFound(new { message = "Personal no encontrado" });

            return Ok(personal);
        }

        // ✅ CREAR NUEVO
        [HttpPost]
        [Authorize(Roles = "1")] // opcional: solo admin (idRolSistema = 1)
        public async Task<IActionResult> Create([FromBody] PersonalCreateDTO dto)
        {
            var ok = await _personalService.CreateAsync(dto);
            if (!ok)
                return BadRequest(new { message = "Error al registrar personal" });

            return Ok(new { message = "Personal registrado correctamente" });
        }

        // ✅ ACTUALIZAR
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonalUpdateDTO dto)
        {
            var ok = await _personalService.UpdateAsync(id, dto);
            if (!ok)
                return NotFound(new { message = "Personal no encontrado" });

            return Ok(new { message = "Personal actualizado correctamente" });
        }

        // ✅ ELIMINAR
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _personalService.DeleteAsync(id);
            if (!ok)
                return NotFound(new { message = "Personal no encontrado" });

            return Ok(new { message = "Personal eliminado correctamente" });
        }
    }
}
