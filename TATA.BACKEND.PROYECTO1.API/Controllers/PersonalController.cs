using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonalController : ControllerBase
    {
        private readonly IPersonalService _personalService;

        public PersonalController(IPersonalService personalService)
        {
            _personalService = personalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _personalService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var personal = await _personalService.GetByIdAsync(id);
            if (personal == null) return NotFound();
            return Ok(personal);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonalCreateDTO dto)
        {
            var ok = await _personalService.CreateAsync(dto);
            return ok ? Ok(new { message = "Personal registrado correctamente" })
                      : BadRequest(new { message = "Error al registrar personal" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonalUpdateDTO dto)
        {
            var ok = await _personalService.UpdateAsync(id, dto);
            return ok ? Ok(new { message = "Personal actualizado correctamente" })
                      : NotFound(new { message = "Personal no encontrado" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _personalService.DeleteAsync(id);
            return ok ? Ok(new { message = "Personal eliminado correctamente" })
                      : NotFound(new { message = "Personal no encontrado" });
        }
    }
}
