using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
    /// <summary>
    /// Historial de envíos de correos (automáticos, manuales, resúmenes)
    /// </summary>
    public class EmailLog
    {
        public int Id { get; set; }
        
        public DateTime FechaEjecucion { get; set; }
        
        /// <summary>
        /// AUTOMATICO, MANUAL, RESUMEN
        /// </summary>
        public string Tipo { get; set; } = null!;
        
        /// <summary>
        /// Cantidad de correos enviados exitosamente
        /// </summary>
        public int CantidadEnviados { get; set; }
        
        /// <summary>
        /// EXITO, FALLO, PARCIAL
        /// </summary>
        public string Estado { get; set; } = null!;
        
        /// <summary>
        /// Detalle del error si hubo algún problema
        /// </summary>
        public string? DetalleError { get; set; }
        
        /// <summary>
        /// Usuario que ejecutó el envío (para envíos manuales)
        /// </summary>
        public int? EjecutadoPor { get; set; }
    }
}
