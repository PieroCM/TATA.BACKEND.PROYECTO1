using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // =============== GET (alerta con detalles) ===============
    public class AlertaDTO
    {
        // datos propios de la alerta
        public int IdAlerta { get; set; }
        public int IdSolicitud { get; set; }
        public string TipoAlerta { get; set; } = null!;
        public string? Nivel { get; set; }
        public string Mensaje { get; set; } = null!;
        public string? Estado { get; set; }        // NUEVA / LEIDA / ELIMINADA
        public bool EnviadoEmail { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLectura { get; set; }
        public DateTime? ActualizadoEn { get; set; }

        // datos de la solicitud relacionada (solo lectura)
        public AlertaSolicitudInfoDto? Solicitud { get; set; }
    }

    // =============== POST (crear) ===============
    // lo que te va a mandar el front para crear una alerta nueva
    public class AlertaCreateDto
    {
        public int IdSolicitud { get; set; }             // obligatorio, FK
        public string TipoAlerta { get; set; } = null!;  // INCUMPLIMIENTO_SLA, PREVENTIVA, etc.
        public string? Nivel { get; set; }               // WARNING, CRITICAL
        public string Mensaje { get; set; } = null!;
        public string? Estado { get; set; }              // si no manda, en service pones "NUEVA"
        public bool EnviadoEmail { get; set; } = false;  // normalmente false, lo cambia el servicio de correo
    }

    // =============== PUT (actualizar) ===============
    public class AlertaUpdateDto
    {
        public string? TipoAlerta { get; set; }
        public string? Nivel { get; set; }
        public string? Mensaje { get; set; }
        public string? Estado { get; set; }          // pasar a LEIDA, CERRADA, ELIMINADA
        public bool? EnviadoEmail { get; set; }      // marcar en true cuando se envía
    }

    // =============== SUB-DTOS (para mostrar info de la solicitud) ===============

    // lo que quieres mostrar de la solicitud desde la alerta
    public class AlertaSolicitudInfoDto
    {
        public int IdSolicitud { get; set; }
        public DateOnly FechaSolicitud { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        public int? NumDiasSla { get; set; }
        public string? ResumenSla { get; set; }
        public string? EstadoSolicitud { get; set; }          // ACTIVO / ELIMINADO
        public string? EstadoCumplimientoSla { get; set; }    // CUMPLE_SLA / NO_CUMPLE_SLA

        // subobjetos
        public AlertaSolicitudPersonalDto? Personal { get; set; }
        public AlertaSolicitudRolDto? RolRegistro { get; set; }
        public AlertaSolicitudSlaDto? ConfigSla { get; set; }
    }

    public class AlertaSolicitudPersonalDto
    {
        public int IdPersonal { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? CorreoCorporativo { get; set; }
    }

    public class AlertaSolicitudRolDto
    {
        public int IdRolRegistro { get; set; }
        public string? NombreRol { get; set; }
    }

    public class AlertaSolicitudSlaDto
    {
        public int IdSla { get; set; }
        public string? CodigoSla { get; set; }
        public int DiasUmbral { get; set; }
        public string? TipoSolicitud { get; set; }
    }
}