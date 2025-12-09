using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.API.Services;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrediccionController : ControllerBase
    {
        private readonly PrediccionProxyService _prediccionService;
        private readonly ILogger<PrediccionController> _logger;

        public PrediccionController(PrediccionProxyService prediccionService, ILogger<PrediccionController> logger)
        {
            _prediccionService = prediccionService;
            _logger = logger;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            var result = await _prediccionService.GetHealthAsync();
            if (result == null)
                return StatusCode(503, new { message = "Servicio de predicción no disponible" });
            return Ok(result);
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            var result = await _prediccionService.GetResumenAsync();
            if (result == null)
                return StatusCode(503, new { message = "Error al obtener resumen" });
            return Ok(result);
        }

        [HttpGet("criticas")]
        public async Task<IActionResult> GetCriticas([FromQuery] int limite = 10)
        {
            var result = await _prediccionService.GetPrediccionesCriticasAsync(limite);
            return Ok(result);
        }

        [HttpGet("paginado")]
        public async Task<IActionResult> GetPaginado([FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
        {
            var result = await _prediccionService.GetPrediccionesPaginadasAsync(pagina, tamano);
            if (result == null)
                return StatusCode(503, new { message = "Error al obtener predicciones" });
            return Ok(result);
        }

        [HttpPost("reentrenar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reentrenar()
        {
            var result = await _prediccionService.ReentrenarModeloAsync();
            if (result == null)
                return StatusCode(503, new { message = "Error al reentrenar modelo" });
            return Ok(result);
        }
    }
}
