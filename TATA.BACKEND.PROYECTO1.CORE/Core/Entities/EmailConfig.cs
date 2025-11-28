using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
    /// <summary>
    /// Configuración global de envío de emails (solo existirá 1 registro)
    /// </summary>
    public class EmailConfig
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Si está activado, envía email inmediatamente al crear una alerta
        /// </summary>
        public bool EnvioInmediato { get; set; }
        
        /// <summary>
        /// Si está activado, envía un resumen diario automático
        /// </summary>
        public bool ResumenDiario { get; set; }
        
        /// <summary>
        /// Hora del día para enviar el resumen (ej: 08:00:00)
        /// </summary>
        public TimeSpan HoraResumen { get; set; }
        
        /// <summary>
        /// Email destinatario para pruebas o resumen general (Admin)
        /// </summary>
        public string? EmailDestinatarioPrueba { get; set; }
        
        public DateTime CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }
}
