using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO para el Dashboard de Alertas con cálculos de SLA y visualización
    /// </summary>
    public class AlertaDashboardDto
    {
        // Identificadores
        public int IdAlerta { get; set; }
        public int IdSolicitud { get; set; }
        
        // Información descriptiva
        public string NombreSolicitud { get; set; } = null!;
        public string NombreResponsable { get; set; } = null!;
        public string NombreSla { get; set; } = null!;
        
        /// <summary>
        /// CRITICO, ALTO, MEDIO, BAJO
        /// </summary>
        public string Nivel { get; set; } = null!;
        
        // Campos calculados para SLA
        /// <summary>
        /// Días transcurridos desde la fecha de inicio hasta hoy
        /// </summary>
        public int DiasTranscurridos { get; set; }
        
        /// <summary>
        /// Días restantes hasta el vencimiento del SLA
        /// </summary>
        public int DiasRestantes { get; set; }
        
        /// <summary>
        /// Porcentaje de progreso (0 a 100) para la barra visual
        /// </summary>
        public double PorcentajeProgreso { get; set; }
        
        /// <summary>
        /// Color sugerido para el estado (hexadecimal o clase CSS)
        /// Ejemplo: "#dc3545" para crítico, "#28a745" para normal
        /// </summary>
        public string ColorEstado { get; set; } = null!;
        
        /// <summary>
        /// NUEVA, LEIDA
        /// </summary>
        public string EstadoLectura { get; set; } = null!;
        
        // Fechas
        public DateTime FechaCreacion { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        
        // Información adicional
        public string TipoAlerta { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public bool EnviadoEmail { get; set; }
        public string? CorreoResponsable { get; set; }
    }
}
