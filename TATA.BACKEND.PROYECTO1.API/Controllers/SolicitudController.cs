using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudController : ControllerBase
    {
        private readonly ISolicitudService _solicitudService;
        public SolicitudController(ISolicitudService solicitudService)
        {
            _solicitudService = solicitudService;
        }
        // GET: api/solicitud GOD
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _solicitudService.GetAllAsync();
            return Ok(data);
        }

        // GET: api/solicitud/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _solicitudService.GetByIdAsync(id);
            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // POST: api/solicitud
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SolicitudCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _solicitudService.CreateAsync(dto);

            // devuelve 201 + el recurso
            return CreatedAtAction(nameof(GetById), new { id = created.IdSolicitud }, created);
        }

        // PUT: api/solicitud/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SolicitudUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _solicitudService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/solicitud/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _solicitudService.DeleteAsync(id);
            if (!ok)
                return NotFound();

            // 204 sin cuerpo
            return NoContent();
        }
    }
}
