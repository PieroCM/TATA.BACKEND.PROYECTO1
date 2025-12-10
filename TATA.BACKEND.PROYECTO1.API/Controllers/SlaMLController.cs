using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.API.DTOs.SlaML;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    /// <summary>
    /// Controlador para el módulo de Machine Learning de predicción SLA
    /// Integra con un microservicio Python/FastAPI para entrenamiento y predicciones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SlaMLController : ControllerBase
    {
        private readonly ISlaMLService _slaMLService;
        private readonly ILogger<SlaMLController> _logger;

        public SlaMLController(ISlaMLService slaMLService, ILogger<SlaMLController> logger)
        {
            _slaMLService = slaMLService;
            _logger = logger;
        }

        // ???????????????????????????????????????????????????????????????????
        // ENTRENAMIENTO DEL MODELO
        // ???????????????????????????????????????????????????????????????????

        /// <summary>
        /// Entrena el modelo ML con solicitudes históricas cerradas
        /// </summary>
        /// <remarks>
        /// Extrae solicitudes con estado CERRADO, INACTIVO o VENCIDO del rango de fechas especificado.
        /// El microservicio ML aplica:
        /// - SMOTE (oversampling) si hay ? 10,000 registros
        /// - Undersampling si hay > 10,000 registros
        /// 
        /// Ejemplo de request:
        /// ```json
        /// {
        ///     "fechaDesde": "2024-01-01",
        ///     "fechaHasta": "2024-12-06"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Rango de fechas para el entrenamiento</param>
        /// <returns>Resultado del entrenamiento con métricas del modelo</returns>
        /// <response code="200">Modelo entrenado exitosamente</response>
        /// <response code="400">Parámetros inválidos o datos insuficientes</response>
        /// <response code="503">Microservicio ML no disponible</response>
        [HttpPost("Entrenar")]
        [Authorize(Roles = "SUPER_ADMIN,Admin")]
        [ProducesResponseType(typeof(EntrenamientoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> EntrenarModelo([FromBody] TrainRequestDTO request)
        {
            try
            {
                // Validaciones
                if (request.FechaHasta < request.FechaDesde)
                {
                    return BadRequest(new { message = "FechaHasta debe ser posterior o igual a FechaDesde" });
                }

                if (request.FechaHasta > DateTime.UtcNow)
                {
                    return BadRequest(new { message = "FechaHasta no puede ser una fecha futura" });
                }

                _logger.LogInformation("Solicitud de entrenamiento recibida. Rango: {FechaDesde} - {FechaHasta}",
                    request.FechaDesde, request.FechaHasta);

                var resultado = await _slaMLService.EntrenarModeloAsync(request.FechaDesde, request.FechaHasta);

                if (resultado is EntrenamientoResponseDTO response && !response.Exitoso)
                {
                    return BadRequest(resultado);
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en endpoint de entrenamiento");
                return StatusCode(503, new { message = "Error al comunicarse con el microservicio ML", error = ex.Message });
            }
        }

        // ???????????????????????????????????????????????????????????????????
        // PREDICCIONES
        // ???????????????????????????????????????????????????????????????????

        /// <summary>
        /// Obtiene predicciones de incumplimiento para todas las solicitudes activas
        /// </summary>
        /// <remarks>
        /// Analiza solicitudes con estado ACTIVO, EN_PROCESO o PENDIENTE.
        /// Devuelve probabilidad de incumplimiento y nivel de riesgo para cada una.
        /// 
        /// Niveles de riesgo:
        /// - CRITICO: Probabilidad ? 80% o (?60% con ?1 día restante)
        /// - ALTO: Probabilidad ? 60% o (?40% con ?2 días restantes)
        /// - MEDIO: Probabilidad ? 40% o ?3 días restantes
        /// - BAJO: Resto de casos
        /// </remarks>
        /// <returns>Lista de predicciones con resumen general</returns>
        /// <response code="200">Predicciones obtenidas exitosamente</response>
        /// <response code="503">Microservicio ML no disponible</response>
        [HttpGet("PrediccionesActuales")]
        [ProducesResponseType(typeof(PrediccionesActualesResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> ObtenerPrediccionesActuales()
        {
            try
            {
                _logger.LogInformation("Solicitud de predicciones actuales recibida");
                var resultado = await _slaMLService.ObtenerPrediccionesActualesAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo predicciones actuales");
                return StatusCode(503, new { message = "Error al obtener predicciones", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene las predicciones más críticas (mayor riesgo de incumplimiento)
        /// </summary>
        /// <param name="limite">Número máximo de predicciones a retornar (default: 10)</param>
        /// <returns>Lista de predicciones críticas y altas ordenadas por probabilidad</returns>
        [HttpGet("PrediccionesCriticas")]
        [ProducesResponseType(typeof(List<PrediccionActualDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPrediccionesCriticas([FromQuery] int limite = 10)
        {
            try
            {
                if (limite < 1 || limite > 100)
                    limite = 10;

                var resultado = await _slaMLService.ObtenerPrediccionesCriticasAsync(limite);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo predicciones críticas");
                return StatusCode(500, new { message = "Error interno", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el resumen de predicciones para el dashboard
        /// </summary>
        /// <returns>Totales por nivel de riesgo y promedio general</returns>
        [HttpGet("Resumen")]
        [ProducesResponseType(typeof(ResumenPrediccionesDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerResumen()
        {
            try
            {
                var resultado = await _slaMLService.ObtenerResumenPrediccionesAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen de predicciones");
                return StatusCode(500, new { message = "Error interno", error = ex.Message });
            }
        }

        // ???????????????????????????????????????????????????????????????????
        // ESTADO DEL SERVICIO
        // ???????????????????????????????????????????????????????????????????

        /// <summary>
        /// Verifica el estado de salud del microservicio ML
        /// </summary>
        /// <returns>Estado del microservicio</returns>
        [HttpGet("Health")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarSalud()
        {
            try
            {
                var resultado = await _slaMLService.VerificarSaludServicioAsync();
                
                if (resultado == null)
                {
                    return StatusCode(503, new 
                    { 
                        status = "unhealthy",
                        message = "Microservicio ML no disponible",
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando salud del microservicio ML");
                return StatusCode(503, new 
                { 
                    status = "unhealthy",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
