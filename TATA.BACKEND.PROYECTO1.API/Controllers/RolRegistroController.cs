using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolRegistroController : ControllerBase
    {
        private readonly IRolRegistroService _service;

        public RolRegistroController(IRolRegistroService service)
        {
            _service = service;
        }

        // GET: api/rolregistro?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolRegistroDTO>>> Get([FromQuery] bool soloActivos = true)
        {
            var list = await _service.GetAllAsync(soloActivos);
            return Ok(list);
        }

        // GET: api/rolregistro/5
        [HttpGet("{id:int}", Name = "GetRolRegistroById")]
        public async Task<ActionResult<RolRegistroDTO>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // POST: api/rolregistro
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] RolRegistroCreateDTO dto)
        {
            try
            {
                var id = await _service.CreateAsync(null, dto);
                return CreatedAtRoute("GetRolRegistroById", new { id }, id);
            }
            catch (DuplicateNameException)
            {
                return Conflict(new { message = "NombreRol ya existe" });
            }
        }


        // PUT: api/rolregistro/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] RolRegistroUpdateDTO dto)
        {
            if (dto is null) return BadRequest();
            var ok = await _service.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/rolregistro/5 (físico)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}
