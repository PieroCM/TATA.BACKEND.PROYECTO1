using System;
using System.ComponentModel.DataAnnotations;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO de configuración de email
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
    /// DTO para actualizar configuración de email
    /// </summary>
    public class EmailConfigUpdateDTO
    {
        public string? DestinatarioResumen { get; set; }
        public bool? EnvioInmediato { get; set; }
        public bool? ResumenDiario { get; set; }
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
}
