namespace TATA.BACKEND.PROYECTO1.API.DTOs.SlaML
{
    // ???????????????????????????????????????????????????????????????????
    // DTOs PARA ENTRENAMIENTO (POST /train)
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Request para iniciar entrenamiento del modelo ML
    /// </summary>
    public class TrainRequestDTO
    {
        /// <summary>
        /// Fecha inicial del rango de solicitudes a usar para entrenar
        /// </summary>
        public DateTime FechaDesde { get; set; }

        /// <summary>
        /// Fecha final del rango de solicitudes a usar para entrenar
        /// </summary>
        public DateTime FechaHasta { get; set; }
    }

    /// <summary>
    /// Solicitud histórica para enviar al microservicio ML (entrenamiento)
    /// </summary>
    public class SolicitudEntrenamientoDTO
    {
        public int IdSolicitud { get; set; }
        public string CodigoSla { get; set; } = string.Empty;
        public string TipoSolicitud { get; set; } = string.Empty;
        public string RolRegistro { get; set; } = string.Empty;
        public int DiasUmbral { get; set; }
        public int NumDiasSla { get; set; }
        public string EstadoCumplimiento { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaIngreso { get; set; }

        /// <summary>
        /// Label binario: 0 = CUMPLE, 1 = NO_CUMPLE
        /// </summary>
        public int Label { get; set; }
    }

    /// <summary>
    /// Payload que se envía al microservicio ML para entrenar
    /// </summary>
    public class TrainPayloadDTO
    {
        public List<SolicitudEntrenamientoDTO> Solicitudes { get; set; } = new();
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
    }

    /// <summary>
    /// Respuesta del microservicio ML después de entrenar
    /// </summary>
    public class TrainResponseDTO
    {
        public string Status { get; set; } = string.Empty;
        public string ModeloVersion { get; set; } = string.Empty;
        public int RegistrosUtilizados { get; set; }
        public string EstrategiaBalanceo { get; set; } = string.Empty;
        public MetricasModeloDTO Metricas { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? Mensaje { get; set; }
    }

    /// <summary>
    /// Métricas del modelo entrenado
    /// </summary>
    public class MetricasModeloDTO
    {
        public double Accuracy { get; set; }
        public double F1Score { get; set; }
        public double RocAuc { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
    }

    // ???????????????????????????????????????????????????????????????????
    // DTOs PARA PREDICCIÓN (POST /predecir) - Compatible con Docker existente
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Request individual para predicción (formato del servicio Docker existente)
    /// </summary>
    public class SolicitudPrediccionDockerDTO
    {
        public int IdSolicitud { get; set; }
        public double DiasTranscurridos { get; set; }
        public double DiasUmbral { get; set; }
        public int IdRol { get; set; }
    }

    /// <summary>
    /// Respuesta del microservicio Docker para predicción individual
    /// </summary>
    public class PrediccionDockerResponseDTO
    {
        public int IdSolicitud { get; set; }
        public double Probabilidad { get; set; }
        public string Clasificacion { get; set; } = string.Empty;
        public double DiasRestantes { get; set; }
        public string NivelRiesgo { get; set; } = string.Empty;
    }

    // ???????????????????????????????????????????????????????????????????
    // DTOs PARA RESPUESTA AL FRONTEND
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Predicción actual enriquecida para mostrar al frontend
    /// </summary>
    public class PrediccionActualDTO
    {
        public int IdSolicitud { get; set; }
        public string CodigoSla { get; set; } = string.Empty;
        public string TipoSolicitud { get; set; } = string.Empty;
        public string RolRegistro { get; set; } = string.Empty;
        public int DiasUmbral { get; set; }
        public int DiasTranscurridos { get; set; }
        public int DiasRestantes { get; set; }
        public double ProbabilidadNoCumple { get; set; }
        public string Prediccion { get; set; } = string.Empty;
        public string NivelRiesgo { get; set; } = string.Empty;
        public string ModeloVersion { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaPrediccion { get; set; }
        public List<string> FactoresRiesgo { get; set; } = new();

        // Datos del personal
        public string NombrePersonal { get; set; } = string.Empty;
        public string CorreoPersonal { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resumen de predicciones para dashboard
    /// </summary>
    public class ResumenPrediccionesDTO
    {
        public int TotalAnalizadas { get; set; }
        public int TotalCriticas { get; set; }
        public int TotalAltas { get; set; }
        public int TotalMedias { get; set; }
        public int TotalBajas { get; set; }
        public double PromedioRiesgo { get; set; }
        public string ModeloVersion { get; set; } = string.Empty;
        public DateTime FechaAnalisis { get; set; }
    }

    /// <summary>
    /// Respuesta completa de predicciones actuales
    /// </summary>
    public class PrediccionesActualesResponseDTO
    {
        public ResumenPrediccionesDTO Resumen { get; set; } = new();
        public List<PrediccionActualDTO> Predicciones { get; set; } = new();
    }

    /// <summary>
        /// Respuesta del entrenamiento para el frontend
        /// </summary>
        public class EntrenamientoResponseDTO
        {
            public bool Exitoso { get; set; }
            public string Mensaje { get; set; } = string.Empty;
            public string ModeloVersion { get; set; } = string.Empty;
            public int RegistrosUtilizados { get; set; }
            public string EstrategiaBalanceo { get; set; } = string.Empty;
            public MetricasModeloDTO? Metricas { get; set; }
            public DateTime FechaEntrenamiento { get; set; }
            public string? RangoFechas { get; set; }
        }
    }
