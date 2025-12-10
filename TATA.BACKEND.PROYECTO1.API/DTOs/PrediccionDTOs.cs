namespace TATA.BACKEND.PROYECTO1.API.DTOs
{
    public class PrediccionSlaDTO
    {
        public int IdSolicitud { get; set; }
        public string CodigoSla { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public string? EstadoCumplimientoSla { get; set; }
        public double ProbabilidadIncumplimiento { get; set; }
        public string NivelRiesgo { get; set; } = string.Empty;
        public int DiasRestantes { get; set; }
        public DateTime FechaPrediccion { get; set; }
        public List<string> FactoresRiesgo { get; set; } = new();
    }

    public class ResumenPrediccionDTO
    {
        public int TotalAnalizadas { get; set; }
        public int Criticas { get; set; }
        public int Altas { get; set; }
        public int Medias { get; set; }
        public int Bajas { get; set; }
        public double PromedioRiesgo { get; set; }
    }

    public class PrediccionPaginadaDTO
    {
        public List<PrediccionSlaDTO> Predicciones { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    public class ModeloInfoDTO
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int SamplesUsed { get; set; }
        public double Accuracy { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class HealthCheckDTO
    {
        public string Status { get; set; } = string.Empty;
        public bool ModelLoaded { get; set; }
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}
