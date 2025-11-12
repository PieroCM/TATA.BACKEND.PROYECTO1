using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogSistemaController : ControllerBase
    {
        private readonly ILogSistemaService _service;

        public LogSistemaController(ILogSistemaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _service.GetAllAsync();
            return Ok(logs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var log = await _service.GetByIdAsync(id);
            return log == null ? NotFound() : Ok(log);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LogSistemaCreateDTO dto)
        {
            var result = await _service.AddAsync(dto);
            return result ? Ok("Log registrado correctamente.") : BadRequest("Error al registrar log.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.RemoveAsync(id);
            return result ? Ok("Log eliminado correctamente.") : NotFound("No se encontró el log.");
        }
    }
}
