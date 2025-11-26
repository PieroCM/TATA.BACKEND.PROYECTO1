using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    /// <summary>
    /// Controlador para el frontend Vue.js - Gestión de Alertas Dashboard
    /// Ruta: /api/alertas (con 's' plural)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AlertasController : ControllerBase
    {
        private readonly IEmailAutomationService _emailAutomationService;
        private readonly IAlertaService _alertaService;
        private readonly Proyecto1SlaDbContext _context;

        public AlertasController(
            IEmailAutomationService emailAutomationService,
            IAlertaService alertaService,
            Proyecto1SlaDbContext context)
        {
            _emailAutomationService = emailAutomationService;
            _alertaService = alertaService;
            _context = context;
        }

        /// <summary>
        /// GET: api/alertas/dashboard
        /// Obtiene todas las alertas con información completa para el dashboard del frontend
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<List<AlertaDashboardFrontendDto>>> GetAlertasDashboard()
        {
            try
            {
                var alertas = await _emailAutomationService.GetDashboardAlertsFrontendAsync();
                return Ok(alertas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al obtener alertas del dashboard", 
                    detalle = ex.Message 
                });
            }
        }

        /// <summary>
        /// GET: api/alertas/{id}
        /// Obtiene una alerta específica por ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AlertaDTO>> GetById(int id)
        {
            try
            {
                var alerta = await _alertaService.GetByIdAsync(id);
                if (alerta == null)
                    return NotFound(new { mensaje = "Alerta no encontrada" });

                return Ok(alerta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al obtener la alerta", 
                    detalle = ex.Message 
                });
            }
        }

        /// <summary>
        /// DELETE: api/alertas/{id}
        /// Elimina una alerta por ID
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var ok = await _alertaService.DeleteAsync(id);
                if (!ok)
                    return NotFound(new { mensaje = "Alerta no encontrada" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al eliminar la alerta", 
                    detalle = ex.Message 
                });
            }
        }

        /// <summary>
        /// PUT: api/alertas/{id}/marcar-leida
        /// Marca una alerta como leída
        /// </summary>
        [HttpPut("{id:int}/marcar-leida")]
        public async Task<ActionResult<AlertaDTO>> MarcarLeida(int id)
        {
            try
            {
                var dto = new AlertaUpdateDto
                {
                    Estado = "LEIDA"
                };

                var updated = await _alertaService.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { mensaje = "Alerta no encontrada" });

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al marcar la alerta como leída", 
                    detalle = ex.Message 
                });
            }
        }

        /// <summary>
        /// GET: api/alertas/estadisticas
        /// Obtiene estadísticas generales de alertas
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticas()
        {
            try
            {
                var alertas = await _emailAutomationService.GetDashboardAlertsFrontendAsync();

                var estadisticas = new
                {
                    total = alertas.Count,
                    nuevas = alertas.Count(a => a.Estado == "NUEVA"),
                    leidas = alertas.Count(a => a.Estado == "LEIDA"),
                    criticas = alertas.Count(a => a.Nivel == "CRITICO"),
                    altas = alertas.Count(a => a.Nivel == "ALTO"),
                    medias = alertas.Count(a => a.Nivel == "MEDIO"),
                    vencidas = alertas.Count(a => a.DiasRestantes < 0),
                    porVencer = alertas.Count(a => a.DiasRestantes >= 0 && a.DiasRestantes <= 2)
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al obtener estadísticas", 
                    detalle = ex.Message 
                });
            }
        }
    }
}
