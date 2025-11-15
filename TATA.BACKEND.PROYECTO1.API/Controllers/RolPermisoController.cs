using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolPermisoController : ControllerBase
    {
        private readonly IRolPermisoService _service;

        public RolPermisoController(IRolPermisoService service)
        {
            _service = service;
        }

        // GET: api/rolpermiso
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET: api/rolpermiso/nombres
        [HttpGet("nombres")]
        public async Task<IActionResult> GetAllWithNames()
        {
            var result = await _service.GetAllWithNamesAsync();
            return Ok(result);
        }

        // GET: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpGet("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> GetByIds(int idRolSistema, int idPermiso)
        {
            var result = await _service.GetByIdsAsync(idRolSistema, idPermiso);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // POST: api/rolpermiso
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolPermisoEntity entity)
        {
            var created = await _service.AddAsync(entity);
            return created ? Ok("Registro creado correctamente") : BadRequest("No se pudo crear el registro");
        }

        [HttpPut("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Update(int idRolSistema, int idPermiso, [FromBody] RolPermisoEntity entity)
        {
            var updated = await _service.UpdateAsync(idRolSistema, idPermiso, entity);

            if (!updated)
            {
                // Verificar si el original existe
                var exists = await _service.GetByIdsAsync(idRolSistema, idPermiso);
                if (exists == null)
                    return NotFound("❌ No se encontró el registro original a actualizar.");

                // Verificar si la nueva combinación ya existe
                var duplicate = await _service.GetByIdsAsync(entity.IdRolSistema, entity.IdPermiso);
                if (duplicate != null)
                    return BadRequest("❌ No se puede actualizar. Ya existe un permiso asignado con esta combinación.");
            }

            return Ok("✔ Registro actualizado correctamente.");
        }

        // DELETE: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpDelete("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Delete(int idRolSistema, int idPermiso)
        {
            var deleted = await _service.RemoveAsync(idRolSistema, idPermiso);
            return deleted ? Ok("Registro eliminado correctamente") : NotFound("No se encontró el registro");
        }
    }
}
