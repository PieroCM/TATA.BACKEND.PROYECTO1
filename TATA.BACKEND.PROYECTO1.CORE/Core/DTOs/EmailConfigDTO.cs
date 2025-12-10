using System;
using System.ComponentModel.DataAnnotations;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO de configuración de email (lectura)
    /// </summary>
    public class EmailConfigDTO
    {
        public int Id { get; set; }
        public string DestinatarioResumen { get; set; } = null!;
        public bool EnvioInmediato { get; set; }
        public bool ResumenDiario { get; set; }
        public TimeSpan HoraResumen { get; set; }
        public DateTime? CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }

    /// <summary>
    /// DTO para actualizar configuración de email desde el frontend
    /// Campos opcionales: solo se actualizan los que se envían
    /// </summary>
    public class EmailConfigUpdateDTO
    {
        /// <summary>
        /// Email destinatario para resumen diario
        /// </summary>
        [EmailAddress(ErrorMessage = "El formato del email es inválido")]
        public string? DestinatarioResumen { get; set; }

        /// <summary>
        /// Activar envío inmediato de alertas
        /// </summary>
        public bool? EnvioInmediato { get; set; }

        /// <summary>
        /// Toggle del frontend: Activar/Desactivar resumen diario automático
        /// true = ACTIVADO, false = DESACTIVADO
        /// </summary>
        public bool? ResumenDiario { get; set; }

        /// <summary>
        /// Hora de envío del resumen diario (formato "HH:mm:ss")
        /// Ejemplo: "08:00:00", "14:30:00"
        /// El model binder de .NET 9 convierte automáticamente string a TimeSpan
        /// </summary>
        public TimeSpan? HoraResumen { get; set; }
    }

    /// <summary>
    /// DTO para envío individual de notificaciones (Dashboard)
    /// </summary>
    public class NotificationDto
    {
        [Required(ErrorMessage = "El destinatario es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email es inválido")]
        public string Destinatario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El asunto es obligatorio")]
        [StringLength(200, ErrorMessage = "El asunto no puede exceder 200 caracteres")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cuerpo del correo es obligatorio")]
        public string CuerpoHtml { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de respuesta para el envío del resumen diario
    /// Contiene el resultado de la operación y mensaje dinámico
    /// </summary>
    public class EmailSummaryResponseDto
    {
        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool Exito { get; set; }

        /// <summary>
        /// Mensaje dinámico del resultado de la operación
        /// Ejemplos:
        /// - "No se encontraron alertas para enviar"
        /// - "Resumen diario enviado exitosamente con 5 alertas"
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Número de alertas incluidas en el resumen (0 si no se envió)
        /// </summary>
        public int CantidadAlertas { get; set; }

        /// <summary>
        /// Indica si se envió el correo (false si no había alertas)
        /// </summary>
        public bool CorreoEnviado { get; set; }

        /// <summary>
        /// Destinatario del correo (si aplica)
        /// </summary>
        public string? Destinatario { get; set; }

        /// <summary>
        /// Timestamp de la operación
        /// </summary>
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO para envío masivo de correos (Broadcast)
    /// </summary>
    public class BroadcastDto
    {
        /// <summary>
        /// Asunto del correo
        /// </summary>
        [Required(ErrorMessage = "El asunto es obligatorio")]
        [StringLength(200, ErrorMessage = "El asunto no puede exceder 200 caracteres")]
        public string Asunto { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje HTML del correo
        /// </summary>
        [Required(ErrorMessage = "El mensaje HTML es obligatorio")]
        public string MensajeHtml { get; set; } = string.Empty;

        /// <summary>
        /// Filtrar por Rol (opcional)
        /// </summary>
        public int? IdRol { get; set; }

        /// <summary>
        /// Filtrar por SLA (opcional)
        /// </summary>
        public int? IdSla { get; set; }

        /// <summary>
        /// Modo de prueba: envía solo a un correo de prueba
        /// </summary>
        public bool EsPrueba { get; set; } = false;

        /// <summary>
        /// Email de prueba (obligatorio si EsPrueba = true)
        /// </summary>
        [EmailAddress(ErrorMessage = "El formato del email de prueba es inválido")]
        public string? EmailPrueba { get; set; }
    }

    /// <summary>
    /// DTO para preview de destinatarios antes del envío
    /// </summary>
    public class DestinatarioPreviewDto
    {
        public int IdPersonal { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public string? FotoUrl { get; set; }
        public string Correo { get; set; } = string.Empty;
    }
}
