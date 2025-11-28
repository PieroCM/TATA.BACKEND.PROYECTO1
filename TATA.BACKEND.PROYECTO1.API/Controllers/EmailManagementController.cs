using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailManagementController : ControllerBase
    {
        private readonly IEmailAutomationService _emailAutomationService;

        public EmailManagementController(IEmailAutomationService emailAutomationService)
        {
            _emailAutomationService = emailAutomationService;
        }

        /// <summary>
        /// GET: api/emailmanagement/config
        /// Obtiene la configuración actual de emails
        /// </summary>
        [HttpGet("config")]
        public async Task<ActionResult<EmailConfigDto>> GetConfig()
        {
            var config = await _emailAutomationService.GetConfigAsync();
            
            if (config == null)
            {
                // Devolver configuración por defecto si no existe
                return Ok(new EmailConfigDto
                {
                    Id = 0,
                    EnvioInmediato = false,
                    ResumenDiario = false,
                    HoraResumen = new TimeSpan(8, 0, 0), // 08:00 AM por defecto
                    EmailDestinatarioPrueba = null,
                    CreadoEn = DateTime.UtcNow
                });
            }

            return Ok(config);
        }

        /// <summary>
        /// PUT: api/emailmanagement/config
        /// Actualiza la configuración de emails
        /// </summary>
        [HttpPut("config")]
        public async Task<ActionResult<EmailConfigDto>> UpdateConfig([FromBody] EmailConfigCreateUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _emailAutomationService.SaveConfigAsync(dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al guardar configuración", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/emailmanagement/logs
        /// Obtiene el historial de envíos de correos
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<List<EmailLogDto>>> GetLogs([FromQuery] int take = 50)
        {
            try
            {
                var logs = await _emailAutomationService.GetLogsAsync(take);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener logs", detalle = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/emailmanagement/broadcast
        /// Envío manual desde "Sala de Comunicaciones"
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<ActionResult<EmailLogDto>> SendBroadcast([FromBody] BroadcastRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _emailAutomationService.SendBroadcastAsync(dto);
                
                if (result.Estado == "FALLO")
                {
                    return BadRequest(new { mensaje = "Error al enviar correos", detalle = result.DetalleError });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al enviar broadcast", detalle = ex.Message });
            }
        }
    }
}
