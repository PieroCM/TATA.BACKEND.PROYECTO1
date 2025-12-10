using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.API.Services;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrediccionController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PrediccionController));
        
        private readonly PrediccionProxyService _prediccionService;
        private readonly ILogger<PrediccionController> _logger;
        private readonly ILogService _logService;

        public PrediccionController(
            PrediccionProxyService prediccionService, 
            ILogger<PrediccionController> logger,
            ILogService logService)
        {
            _prediccionService = prediccionService;
            _logger = logger;
            _logService = logService;
            log.Debug("PrediccionController inicializado.");
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            log.Info("GetHealth iniciado");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetHealth Predicción", 
                "Verificando salud del servicio de predicción", null);

            try
            {
                var result = await _prediccionService.GetHealthAsync();
                
                if (result == null)
                {
                    log.Warn("Servicio de predicción no disponible");
                    await _logService.RegistrarLogAsync("WARN", "Servicio de predicción no disponible", 
                        "GetHealthAsync retornó null", null);
                    return StatusCode(503, new { message = "Servicio de predicción no disponible" });
                }

                log.Info("GetHealth completado correctamente");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetHealth", 
                    $"Status: {result.Status}, ModelLoaded: {result.ModelLoaded}", null);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetHealth", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetHealth", 
                    ex.ToString(), null);
                return StatusCode(503, new { message = "Error al verificar salud del servicio de predicción" });
            }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetResumen iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetResumen Predicción", 
                "Obteniendo resumen de predicciones", userId);

            try
            {
                var result = await _prediccionService.GetResumenAsync();
                
                if (result == null)
                {
                    log.Warn("Error al obtener resumen de predicciones");
                    await _logService.RegistrarLogAsync("WARN", "Error al obtener resumen", 
                        "GetResumenAsync retornó null", userId);
                    return StatusCode(503, new { message = "Error al obtener resumen" });
                }

                log.Info($"GetResumen completado correctamente, {result.TotalAnalizadas} solicitudes analizadas");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetResumen", 
                    $"Total: {result.TotalAnalizadas}, Críticas: {result.Criticas}, Altas: {result.Altas}, Medias: {result.Medias}, Bajas: {result.Bajas}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetResumen", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetResumen", 
                    ex.ToString(), userId);
                return StatusCode(503, new { message = "Error al obtener resumen de predicciones" });
            }
        }

        [HttpGet("criticas")]
        public async Task<IActionResult> GetCriticas([FromQuery] int limite = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetCriticas iniciado para usuario {userId} con límite: {limite}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetCriticas Predicción", 
                $"Obteniendo predicciones críticas con límite: {limite}", userId);

            try
            {
                var result = await _prediccionService.GetPrediccionesCriticasAsync(limite);
                
                log.Info($"GetCriticas completado correctamente, {result.Count} predicciones críticas obtenidas");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetCriticas", 
                    $"Total predicciones críticas obtenidas: {result.Count}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetCriticas", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetCriticas", 
                    ex.ToString(), userId);
                return StatusCode(503, new { message = "Error al obtener predicciones críticas" });
            }
        }

        [HttpGet("paginado")]
        public async Task<IActionResult> GetPaginado([FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetPaginado iniciado para usuario {userId} - Página: {pagina}, Tamaño: {tamano}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetPaginado Predicción", 
                $"Obteniendo predicciones paginadas - Página: {pagina}, Tamaño: {tamano}", userId);

            try
            {
                var result = await _prediccionService.GetPrediccionesPaginadasAsync(pagina, tamano);
                
                if (result == null)
                {
                    log.Warn("Error al obtener predicciones paginadas");
                    await _logService.RegistrarLogAsync("WARN", "Error al obtener predicciones paginadas", 
                        "GetPrediccionesPaginadasAsync retornó null", userId);
                    return StatusCode(503, new { message = "Error al obtener predicciones" });
                }

                log.Info($"GetPaginado completado correctamente, {result.Predicciones.Count} predicciones obtenidas de {result.Total} totales");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetPaginado", 
                    $"Predicciones obtenidas: {result.Predicciones.Count}/{result.Total}, Página: {result.Pagina}/{result.TotalPaginas}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetPaginado", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetPaginado", 
                    ex.ToString(), userId);
                return StatusCode(503, new { message = "Error al obtener predicciones paginadas" });
            }
        }

        [HttpPost("reentrenar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reentrenar()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Reentrenar iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Reentrenar Modelo", 
                "Solicitando reentrenamiento del modelo de predicción", userId);

            try
            {
                var result = await _prediccionService.ReentrenarModeloAsync();
                
                if (result == null)
                {
                    log.Warn("Error al reentrenar modelo de predicción");
                    await _logService.RegistrarLogAsync("WARN", "Error al reentrenar modelo", 
                        "ReentrenarModeloAsync retornó null", userId);
                    return StatusCode(503, new { message = "Error al reentrenar modelo" });
                }

                log.Info($"Reentrenar completado correctamente - Status: {result.Status}, Accuracy: {result.Accuracy}, Samples: {result.SamplesUsed}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Reentrenar Modelo", 
                    $"Status: {result.Status}, Message: {result.Message}, SamplesUsed: {result.SamplesUsed}, Accuracy: {result.Accuracy}", userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Reentrenar", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Reentrenar", 
                    ex.ToString(), userId);
                return StatusCode(503, new { message = "Error al reentrenar modelo de predicción" });
            }
        }
    }
}
