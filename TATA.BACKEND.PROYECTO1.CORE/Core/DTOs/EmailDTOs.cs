using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO para leer/guardar la configuración de emails
    /// </summary>
    public class EmailConfigDto
    {
        public int Id { get; set; }
        public bool EnvioInmediato { get; set; }
        public bool ResumenDiario { get; set; }
        public TimeSpan HoraResumen { get; set; }
        public string? EmailDestinatarioPrueba { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }
    
    /// <summary>
    /// DTO para crear/actualizar la configuración de emails
    /// </summary>
    public class EmailConfigCreateUpdateDto
    {
        public bool EnvioInmediato { get; set; }
        public bool ResumenDiario { get; set; }
        public TimeSpan HoraResumen { get; set; }
        public string? EmailDestinatarioPrueba { get; set; }
    }
    
    /// <summary>
    /// DTO para el historial de logs de email
    /// </summary>
    public class EmailLogDto
    {
        public int Id { get; set; }
        public DateTime FechaEjecucion { get; set; }
        public string Tipo { get; set; } = null!;
        public int CantidadEnviados { get; set; }
        public string Estado { get; set; } = null!;
        public string? DetalleError { get; set; }
        public int? EjecutadoPor { get; set; }
        public string? NombreUsuario { get; set; }
    }
    
    /// <summary>
    /// DTO para envío manual desde "Sala de Comunicaciones"
    /// </summary>
    public class BroadcastRequestDto
    {
        /// <summary>
        /// Filtro por rol del personal (opcional)
        /// </summary>
        public int? FiltroRolId { get; set; }
        
        /// <summary>
        /// Filtro por SLA (opcional)
        /// </summary>
        public int? FiltroSlaId { get; set; }
        
        /// <summary>
        /// Asunto del correo
        /// </summary>
        public string Asunto { get; set; } = null!;
        
        /// <summary>
        /// Cuerpo del mensaje en HTML
        /// </summary>
        public string MensajeHtml { get; set; } = null!;
        
        /// <summary>
        /// Usuario que ejecuta el envío
        /// </summary>
        public int EjecutadoPor { get; set; }
    }
}
