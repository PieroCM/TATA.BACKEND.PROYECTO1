using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // api/configsla
    public class ConfigSlaController : ControllerBase
    {
        private readonly IConfigSlaService _service;

        public ConfigSlaController(IConfigSlaService service)
        {
            _service = service;
        }

        // GET: api/configsla?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigSlaDTO>>> Get([FromQuery] bool soloActivos = true)
        {
            var list = await _service.GetAllAsync(soloActivos);
            return Ok(list);
        }

        // GET: api/configsla/5
        [HttpGet("{id:int}", Name = "GetConfigSlaById")]
        public async Task<ActionResult<ConfigSlaDTO>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // POST: api/configsla
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] ConfigSlaCreateDTO dto)
        {
            if (dto is null) return BadRequest();
            var id = await _service.CreateAsync(null, dto);
            return CreatedAtRoute("GetConfigSlaById", new { id }, id);
        }

        // PUT: api/configsla/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] ConfigSlaUpdateDTO dto)
        {
            if (dto is null) return BadRequest();
            var ok = await _service.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE (físico): api/configsla/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

    }
}
