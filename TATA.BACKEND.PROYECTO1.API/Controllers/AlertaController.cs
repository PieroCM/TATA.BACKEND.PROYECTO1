using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertaController : ControllerBase
    {
        private readonly IAlertaService _alertaService;

        public AlertaController(IAlertaService alertaService)
        {
            _alertaService = alertaService;
        }

        // GET: api/alerta
        [HttpGet]
        public async Task<ActionResult<List<AlertaDto>>> GetAll()
        {
            var result = await _alertaService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/alerta/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AlertaDto>> GetById(int id)
        {
            var alerta = await _alertaService.GetByIdAsync(id);
            if (alerta == null)
                return NotFound();

            return Ok(alerta);
        }

        // POST: api/alerta
        [HttpPost]
        public async Task<ActionResult<AlertaDto>> Create([FromBody] AlertaCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creada = await _alertaService.CreateAsync(dto);

            // devuelve 201 con la ruta del GET by id
            return CreatedAtAction(nameof(GetById), new { id = creada.IdAlerta }, creada);
        }

        // PUT: api/alerta/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AlertaDto>> Update(int id, [FromBody] AlertaUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _alertaService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/alerta/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _alertaService.DeleteAsync(id);
            if (!ok)
                return NotFound();

            return NoContent();
        }
    }
}

