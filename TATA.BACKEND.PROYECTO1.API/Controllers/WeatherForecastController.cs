using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WeatherForecastController));
        
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ILogService _logService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ILogService logService)
        {
            _logger = logger;
            _logService = logService;
            log.Debug("WeatherForecastController inicializado.");
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Get iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetWeatherForecast", 
                "Obteniendo pronóstico del clima", userId);

            try
            {
                var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

                log.Info($"Get completado correctamente, {forecast.Length} pronósticos generados");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetWeatherForecast", 
                    $"Total pronósticos generados: {forecast.Length}", userId);

                return forecast;
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Get", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetWeatherForecast", 
                    ex.ToString(), userId);
                throw;
            }
        }
    }
}
