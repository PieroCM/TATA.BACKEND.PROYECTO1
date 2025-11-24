using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // Requiere JWT para todos los endpoints (ajusta si sólo quieres en generar)
    public class ReporteController : ControllerBase
    {
        private readonly IReporteService _reporteService;

        public ReporteController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        // GET: api/reportes
        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            var entities = await _reporteService.GetAllAsync();
            var list = entities.Select(MapToDto).ToList();
            return Ok(list);
        }

        // GET: api/reportes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            var entity = await _reporteService.GetByIdAsync(id);
            if (entity == null) return NotFound();

            return Ok(MapToDto(entity));
        }

        // POST: api/reportes
        [AllowAnonymous] // si aún necesitas crear sin JWT; quítalo cuando todo requiera token
        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReporteCreateRequest request)
        {
            if (request == null) return BadRequest();

            var entity = new Reporte
            {
                TipoReporte = request.TipoReporte,
                Formato = request.Formato,
                FiltrosJson = request.FiltrosJson,
                RutaArchivo = request.RutaArchivo,
                GeneradoPor = request.GeneradoPor
                // FechaGeneracion: la setea SQL con DEFAULT (SYSUTCDATETIME)
            };

            await _reporteService.AddAsync(entity);

            // CreatedAtAction arma la URL: GET api/reportes/{id}
            return CreatedAtAction(nameof(GetReporteById), new { id = entity.IdReporte }, MapToDto(entity));
        }

        // POST: api/reportes/generar
        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarReporteRequest request)
        {
            if (request == null || request.IdsSolicitudes == null || !request.IdsSolicitudes.Any())
                return BadRequest("Debes enviar al menos una solicitud.");

            // Obtener id usuario desde el JWT (claim "UserId")
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var idUsuarioActual))
            {
                return Unauthorized("Token sin claim UserId válido.");
            }

            var reporte = await _reporteService.GenerarReporteAsync(request, idUsuarioActual);

            var dto = MapToDto(reporte);
            // set nombre desde claim Name si existe
            dto.GeneradoPorNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? "(sin_username)";
            return Ok(dto);
        }

        // PUT: api/reportes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReporteUpdateRequest request)
        {
            if (request == null) return BadRequest();

            var existing = await _reporteService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.TipoReporte = request.TipoReporte;
            existing.Formato = request.Formato;
            existing.FiltrosJson = request.FiltrosJson;
            existing.RutaArchivo = request.RutaArchivo;
            existing.GeneradoPor = request.GeneradoPor;

            var ok = await _reporteService.UpdateAsync(existing);
            if (!ok) return NotFound(); // por si se borró entre GET y PUT

            return NoContent();
        }

        // DELETE: api/reportes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            var ok = await _reporteService.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        private static ReporteDTO MapToDto(Reporte r) => new ReporteDTO
        {
            IdReporte = r.IdReporte,
            TipoReporte = r.TipoReporte,
            Formato = r.Formato,
            FiltrosJson = r.FiltrosJson,
            RutaArchivo = r.RutaArchivo,
            GeneradoPor = r.GeneradoPor,
            // GeneradoPorNombre se completa en el endpoint generar usando claims
            FechaGeneracion = r.FechaGeneracion,
            TotalSolicitudes = r.Detalles != null ? r.Detalles.Count : 0
        };
    }

}
