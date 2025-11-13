using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    //[Route("api/reportedetalles")]  
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ReporteDetalleController : ControllerBase
    {
        private readonly IReporteDetalleService _service;

        public ReporteDetalleController(IReporteDetalleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            var list = new List<ReporteDetalleDTO>();
            foreach (var e in items)
            {
                list.Add(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
            }
            return Ok(list);
        }

        [HttpGet("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> GetByIds(int idReporte, int idSolicitud)
        {
            var e = await _service.GetByIdsAsync(idReporte, idSolicitud);
            if (e == null) return NotFound();
            return Ok(new ReporteDetalleDTO { IdReporte = e.IdReporte, IdSolicitud = e.IdSolicitud });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReporteDetalleCreateRequest request)
        {
            if (request == null) return BadRequest();

            var ok = await _service.AddAsync(new ReporteDetalle
            {
                IdReporte = request.IdReporte,
                IdSolicitud = request.IdSolicitud
            });

            if (!ok) return Conflict("La relación ya existe.");

            return CreatedAtAction(nameof(GetByIds),
                new { idReporte = request.IdReporte, idSolicitud = request.IdSolicitud },
                new ReporteDetalleDTO { IdReporte = request.IdReporte, IdSolicitud = request.IdSolicitud });
        }

        [HttpPut("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Update(int idReporte, int idSolicitud, [FromBody] ReporteDetalleUpdateRequest request)
        {
            if (request == null) return BadRequest();
            if (request.IdReporte != idReporte || request.IdSolicitud != idSolicitud)
                return BadRequest("Las claves del body y la ruta no coinciden.");

            var ok = await _service.UpdateAsync(new ReporteDetalle
            {
                IdReporte = idReporte,
                IdSolicitud = idSolicitud
            });

            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{idReporte:int}/{idSolicitud:int}")]
        public async Task<IActionResult> Delete(int idReporte, int idSolicitud)
        {
            var ok = await _service.DeleteAsync(idReporte, idSolicitud);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
