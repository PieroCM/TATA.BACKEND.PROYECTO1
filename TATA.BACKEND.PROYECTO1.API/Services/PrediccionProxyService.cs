using System.Text.Json;
using TATA.BACKEND.PROYECTO1.API.DTOs;

namespace TATA.BACKEND.PROYECTO1.API.Services
{
    public class PrediccionProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PrediccionProxyService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PrediccionProxyService(HttpClient httpClient, ILogger<PrediccionProxyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        public async Task<HealthCheckDTO?> GetHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HealthCheckDTO>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar salud del servicio de predicción");
                return null;
            }
        }

        public async Task<ResumenPrediccionDTO?> GetResumenAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/resumen");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ResumenPrediccionDTO>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de predicciones");
                return null;
            }
        }

        public async Task<List<PrediccionSlaDTO>> GetPrediccionesCriticasAsync(int limite = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/predecir/criticas?limite={limite}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PrediccionSlaDTO>>(content, _jsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener predicciones críticas");
                return new List<PrediccionSlaDTO>();
            }
        }

        public async Task<PrediccionPaginadaDTO?> GetPrediccionesPaginadasAsync(int pagina = 1, int tamano = 50)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/predecir/paginado?pagina={pagina}&tamano={tamano}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PrediccionPaginadaDTO>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener predicciones paginadas");
                return null;
            }
        }

        public async Task<ModeloInfoDTO?> ReentrenarModeloAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/modelo/reentrenar", null);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ModeloInfoDTO>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reentrenar modelo");
                return null;
            }
        }
    }
}
