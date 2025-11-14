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
        public async Task<ActionResult<List<AlertaDTO>>> GetAll()
        {
            var result = await _alertaService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/alerta/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AlertaDTO>> GetById(int id)
        {
            var alerta = await _alertaService.GetByIdAsync(id);
            if (alerta == null)
                return NotFound(new { mensaje = "Alerta no encontrada" });

            return Ok(alerta);
        }

        // POST: api/alerta
        [HttpPost]
        public async Task<ActionResult<AlertaDTO>> Create([FromBody] AlertaCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var creada = await _alertaService.CreateAsync(dto);

                // devuelve 201 con la ruta del GET by id
                return CreatedAtAction(nameof(GetById), new { id = creada.IdAlerta }, creada);
            }
            catch (ArgumentException ex)
            {
                // Error de validación (FK solicitud no existe, etc.)
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Error de operación (correo inválido, etc.)
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                // Error inesperado
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // PUT: api/alerta/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AlertaDTO>> Update(int id, [FromBody] AlertaUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _alertaService.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { mensaje = "Alerta no encontrada" });

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                // Error de validación
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Error de correo electrónico
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                // Error inesperado
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/alerta/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var ok = await _alertaService.DeleteAsync(id);
            if (!ok)
                return NotFound(new { mensaje = "Alerta no encontrada" });

            return NoContent();
        }
    }
}

