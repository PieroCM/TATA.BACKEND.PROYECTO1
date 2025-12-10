using System.Text;
using System.Text.Json;
using TATA.BACKEND.PROYECTO1.API.DTOs.SlaML;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Services
{
    /// <summary>
    /// Servicio para integración con el microservicio ML de predicción SLA
    /// </summary>
    public class SlaMLService : ISlaMLService
    {
        private readonly HttpClient _httpClient;
        private readonly ISlaMLRepository _repository;
        private readonly ILogger<SlaMLService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // TimeZone de Perú para cálculos de fecha
        private static readonly TimeZoneInfo PeruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public SlaMLService(
            HttpClient httpClient,
            ISlaMLRepository repository,
            ILogger<SlaMLService> logger)
        {
            _httpClient = httpClient;
            _repository = repository;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        // ???????????????????????????????????????????????????????????????????
        // ENTRENAMIENTO DEL MODELO
        // ???????????????????????????????????????????????????????????????????

        public async Task<object> EntrenarModeloAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            try
            {
                _logger.LogInformation("Iniciando entrenamiento del modelo ML. Rango: {FechaDesde} - {FechaHasta}", 
                    fechaDesde, fechaHasta);

                // 1. Obtener solicitudes cerradas de la BD
                var solicitudes = await _repository.GetSolicitudesParaEntrenamientoAsync(fechaDesde, fechaHasta);

                if (solicitudes.Count < 10)
                {
                    return new EntrenamientoResponseDTO
                    {
                        Exitoso = false,
                        Mensaje = $"Se requieren al menos 10 solicitudes cerradas para entrenar. Encontradas: {solicitudes.Count}",
                        FechaEntrenamiento = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Solicitudes obtenidas para entrenamiento: {Count}", solicitudes.Count);

                // 2. Mapear a DTOs para el microservicio
                var payload = new TrainPayloadDTO
                {
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Solicitudes = solicitudes.Select(s => new SolicitudEntrenamientoDTO
                    {
                        IdSolicitud = s.IdSolicitud,
                        CodigoSla = s.IdSlaNavigation?.CodigoSla ?? $"SLA{s.IdSla}",
                        TipoSolicitud = s.IdSlaNavigation?.TipoSolicitud ?? "DESCONOCIDO",
                        RolRegistro = s.IdRolRegistroNavigation?.NombreRol ?? "DESCONOCIDO",
                        DiasUmbral = s.IdSlaNavigation?.DiasUmbral ?? 5,
                        NumDiasSla = s.NumDiasSla ?? 0,
                        EstadoCumplimiento = s.EstadoCumplimientoSla ?? "DESCONOCIDO",
                        FechaSolicitud = s.FechaSolicitud.ToDateTime(TimeOnly.MinValue),
                        FechaIngreso = s.FechaIngreso?.ToDateTime(TimeOnly.MinValue),
                        // Label: 0 = CUMPLE, 1 = NO_CUMPLE
                        Label = s.EstadoCumplimientoSla?.StartsWith("NO_CUMPLE") == true ? 1 : 0
                    }).ToList()
                };

                // 3. Llamar al microservicio ML
                var jsonContent = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando datos al microservicio ML para entrenamiento...");
                var response = await _httpClient.PostAsync("/modelo/reentrenar", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error del microservicio ML: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    return new EntrenamientoResponseDTO
                    {
                        Exitoso = false,
                        Mensaje = $"Error del microservicio ML: {response.StatusCode}",
                        FechaEntrenamiento = DateTime.UtcNow
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var trainResponse = JsonSerializer.Deserialize<TrainResponseDTO>(responseContent, _jsonOptions);

                if (trainResponse == null)
                {
                    return new EntrenamientoResponseDTO
                    {
                        Exitoso = false,
                        Mensaje = "Respuesta inválida del microservicio ML",
                        FechaEntrenamiento = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Modelo entrenado exitosamente. Versión: {Version}, Accuracy: {Accuracy}", 
                    trainResponse.ModeloVersion, trainResponse.Metricas?.Accuracy);

                // 4. Retornar respuesta al frontend
                return new EntrenamientoResponseDTO
                {
                    Exitoso = true,
                    Mensaje = "Modelo entrenado exitosamente",
                    ModeloVersion = trainResponse.ModeloVersion,
                    RegistrosUtilizados = trainResponse.RegistrosUtilizados,
                    EstrategiaBalanceo = trainResponse.EstrategiaBalanceo,
                    Metricas = trainResponse.Metricas,
                    FechaEntrenamiento = DateTime.UtcNow,
                    RangoFechas = $"{fechaDesde:yyyy-MM-dd} a {fechaHasta:yyyy-MM-dd}"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión con el microservicio ML");
                return new EntrenamientoResponseDTO
                {
                    Exitoso = false,
                    Mensaje = "No se pudo conectar con el microservicio ML. Verifique que esté en ejecución.",
                    FechaEntrenamiento = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el entrenamiento");
                return new EntrenamientoResponseDTO
                {
                    Exitoso = false,
                    Mensaje = $"Error inesperado: {ex.Message}",
                    FechaEntrenamiento = DateTime.UtcNow
                };
            }
        }

        // ???????????????????????????????????????????????????????????????????
        // OBTENER PREDICCIONES
        // ???????????????????????????????????????????????????????????????????

        public async Task<object> ObtenerPrediccionesActualesAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo predicciones actuales para solicitudes activas");

                // 1. Obtener solicitudes activas de la BD
                var solicitudes = await _repository.GetSolicitudesActivasParaPrediccionAsync();

                if (solicitudes.Count == 0)
                {
                    return new PrediccionesActualesResponseDTO
                    {
                        Resumen = new ResumenPrediccionesDTO
                        {
                            TotalAnalizadas = 0,
                            FechaAnalisis = DateTime.UtcNow
                        },
                        Predicciones = new List<PrediccionActualDTO>()
                    };
                }

                var hoyPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone).Date;

                // 2. Procesar cada solicitud individualmente (el servicio Docker solo acepta una a la vez)
                var prediccionesEnriquecidas = new List<PrediccionActualDTO>();

                foreach (var s in solicitudes)
                {
                    try
                    {
                        var fechaSolicitud = s.FechaSolicitud.ToDateTime(TimeOnly.MinValue);
                        var diasTranscurridos = (hoyPeru - fechaSolicitud).TotalDays;
                        var diasUmbral = s.IdSlaNavigation?.DiasUmbral ?? 5;
                        
                        // Crear payload individual para el servicio Docker
                        var payload = new SolicitudPrediccionDockerDTO
                        {
                            IdSolicitud = s.IdSolicitud,
                            DiasTranscurridos = diasTranscurridos,
                            DiasUmbral = diasUmbral,
                            IdRol = s.IdRolRegistro
                        };

                        // 3. Llamar al microservicio ML
                        var jsonContent = JsonSerializer.Serialize(payload, _jsonOptions);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        
                        var response = await _httpClient.PostAsync("/predecir", content);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning("Error prediciendo solicitud {IdSolicitud}: {StatusCode} - {Content}", 
                                s.IdSolicitud, response.StatusCode, errorContent);
                            continue;
                        }

                        var responseContent = await response.Content.ReadAsStringAsync();
                        var pred = JsonSerializer.Deserialize<PrediccionDockerResponseDTO>(responseContent, _jsonOptions);

                        if (pred == null) continue;

                        // 4. Enriquecer predicción con datos de la solicitud
                        var diasRestantes = (int)pred.DiasRestantes;
                        var factoresRiesgo = IdentificarFactoresRiesgo(pred.Probabilidad, diasRestantes, diasUmbral);

                        var prediccionEnriquecida = new PrediccionActualDTO
                        {
                            IdSolicitud = s.IdSolicitud,
                            CodigoSla = s.IdSlaNavigation?.CodigoSla ?? $"SLA{s.IdSla}",
                            TipoSolicitud = s.IdSlaNavigation?.TipoSolicitud ?? "DESCONOCIDO",
                            RolRegistro = s.IdRolRegistroNavigation?.NombreRol ?? "DESCONOCIDO",
                            DiasUmbral = diasUmbral,
                            DiasTranscurridos = (int)diasTranscurridos,
                            DiasRestantes = diasRestantes,
                            ProbabilidadNoCumple = pred.Probabilidad,
                            Prediccion = pred.Clasificacion,
                            NivelRiesgo = pred.NivelRiesgo,
                            ModeloVersion = "Docker-v1",
                            FechaSolicitud = fechaSolicitud,
                            FechaPrediccion = DateTime.UtcNow,
                            FactoresRiesgo = factoresRiesgo,
                            NombrePersonal = $"{s.IdPersonalNavigation?.Nombres} {s.IdPersonalNavigation?.Apellidos}".Trim(),
                            CorreoPersonal = s.IdPersonalNavigation?.CorreoCorporativo ?? ""
                        };

                        prediccionesEnriquecidas.Add(prediccionEnriquecida);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando solicitud {IdSolicitud}", s.IdSolicitud);
                    }
                }

                _logger.LogInformation("Predicciones procesadas: {Count} de {Total}", 
                    prediccionesEnriquecidas.Count, solicitudes.Count);

                // 5. Construir respuesta con resumen
                var resumen = new ResumenPrediccionesDTO
                {
                    TotalAnalizadas = prediccionesEnriquecidas.Count,
                    TotalCriticas = prediccionesEnriquecidas.Count(p => p.NivelRiesgo == "CRITICO" || p.NivelRiesgo == "Crítico"),
                    TotalAltas = prediccionesEnriquecidas.Count(p => p.NivelRiesgo == "ALTO" || p.NivelRiesgo == "Alto"),
                    TotalMedias = prediccionesEnriquecidas.Count(p => p.NivelRiesgo == "MEDIO" || p.NivelRiesgo == "Medio"),
                    TotalBajas = prediccionesEnriquecidas.Count(p => p.NivelRiesgo == "BAJO" || p.NivelRiesgo == "Bajo"),
                    PromedioRiesgo = prediccionesEnriquecidas.Count > 0 
                        ? prediccionesEnriquecidas.Average(p => p.ProbabilidadNoCumple) 
                        : 0,
                    ModeloVersion = "Docker-v1",
                    FechaAnalisis = DateTime.UtcNow
                };

                return new PrediccionesActualesResponseDTO
                {
                    Resumen = resumen,
                    Predicciones = prediccionesEnriquecidas.OrderByDescending(p => p.ProbabilidadNoCumple).ToList()
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión con el microservicio ML");
                throw new Exception("No se pudo conectar con el microservicio ML. Verifique que esté en ejecución.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo predicciones actuales");
                throw;
            }
        }

        public async Task<object> ObtenerPrediccionesCriticasAsync(int limite = 10)
        {
            // Obtener predicciones actuales y filtrar las críticas en memoria
            var resultado = await ObtenerPrediccionesActualesAsync();
            
            if (resultado is PrediccionesActualesResponseDTO response)
            {
                return response.Predicciones
                    .Where(p => p.NivelRiesgo == "CRITICO" || p.NivelRiesgo == "ALTO")
                    .Take(limite)
                    .ToList();
            }
            
            return new List<PrediccionActualDTO>();
        }

        public async Task<object> ObtenerResumenPrediccionesAsync()
        {
            // Obtener predicciones actuales y devolver solo el resumen
            var resultado = await ObtenerPrediccionesActualesAsync();
            
            if (resultado is PrediccionesActualesResponseDTO response)
            {
                return response.Resumen;
            }
            
            return new ResumenPrediccionesDTO
            {
                TotalAnalizadas = 0,
                FechaAnalisis = DateTime.UtcNow,
                ModeloVersion = "N/A"
            };
        }

        public async Task<object?> VerificarSaludServicioAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando salud del microservicio ML");
                return null;
            }
        }

        // ???????????????????????????????????????????????????????????????????
        // MÉTODOS AUXILIARES
        // ???????????????????????????????????????????????????????????????????

        /// <summary>
        /// Calcula el nivel de riesgo basado en probabilidad y días restantes
        /// </summary>
        private static string CalcularNivelRiesgo(double probabilidad, int diasRestantes)
        {
            // Riesgo crítico: alta probabilidad Y pocos días
            if (probabilidad >= 0.8 || (probabilidad >= 0.6 && diasRestantes <= 1))
                return "CRITICO";

            // Riesgo alto: probabilidad considerable o días muy justos
            if (probabilidad >= 0.6 || (probabilidad >= 0.4 && diasRestantes <= 2))
                return "ALTO";

            // Riesgo medio
            if (probabilidad >= 0.4 || diasRestantes <= 3)
                return "MEDIO";

            // Riesgo bajo
            return "BAJO";
        }

        /// <summary>
        /// Identifica los factores de riesgo principales
        /// </summary>
        private static List<string> IdentificarFactoresRiesgo(double probabilidad, int diasRestantes, int diasUmbral)
        {
            var factores = new List<string>();

            if (diasRestantes <= 0)
                factores.Add("SLA VENCIDO - Plazo excedido");
            else if (diasRestantes == 1)
                factores.Add("Último día del SLA");
            else if (diasRestantes <= 2)
                factores.Add("Menos de 48 horas para vencimiento");

            if (probabilidad >= 0.8)
                factores.Add("Probabilidad muy alta de incumplimiento según modelo ML");
            else if (probabilidad >= 0.6)
                factores.Add("Probabilidad alta de incumplimiento según modelo ML");

            var porcentajeConsumido = diasUmbral > 0 
                ? (double)(diasUmbral - diasRestantes) / diasUmbral * 100 
                : 100;

            if (porcentajeConsumido >= 80)
                factores.Add($"80%+ del tiempo SLA consumido ({porcentajeConsumido:F0}%)");
            else if (porcentajeConsumido >= 60)
                factores.Add($"60%+ del tiempo SLA consumido ({porcentajeConsumido:F0}%)");

            return factores;
        }
    }
}
