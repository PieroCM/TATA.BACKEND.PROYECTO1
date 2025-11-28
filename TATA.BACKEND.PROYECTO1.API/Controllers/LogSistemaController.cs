using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogSistemaController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LogSistemaController));
        
        private readonly ILogSistemaService _service;

        public LogSistemaController(ILogSistemaService service)
        {
            _service = service;
            log.Debug("LogSistemaController inicializado.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            // No registramos en BD aquí para evitar recursión infinita con logs de logs
            
            try
            {
                var logs = await _service.GetAllAsync();
                
                log.Info("GetAll completado correctamente");
                return Ok(logs);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            log.Info($"GetById iniciado para id: {id}");
            
            try
            {
                var logItem = await _service.GetByIdAsync(id);
                
                if (logItem == null)
                {
                    log.Warn($"Log con id {id} no encontrado");
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                return Ok(logItem);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LogSistemaCreateDTO dto)
        {
            log.Info("Create iniciado");
            
            if (dto == null)
            {
                log.Warn("Create recibió dto nulo");
                return BadRequest("El cuerpo de la petición no puede ser nulo");
            }

            try
            {
                var result = await _service.AddAsync(dto);
                
                if (!result)
                {
                    log.Warn("No se pudo registrar el log en BD");
                    return BadRequest("Error al registrar log.");
                }

                log.Info("Create completado correctamente");
                return Ok("Log registrado correctamente.");
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            log.Info($"Delete iniciado para id: {id}");
            
            try
            {
                var result = await _service.RemoveAsync(id);
                
                if (!result)
                {
                    log.Warn($"Log con id {id} no encontrado para eliminar");
                    return NotFound("No se encontró el log.");
                }

                log.Info($"Delete completado correctamente para id: {id}");
                return Ok("Log eliminado correctamente.");
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
