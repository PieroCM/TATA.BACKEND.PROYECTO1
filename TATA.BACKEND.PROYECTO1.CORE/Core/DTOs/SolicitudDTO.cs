using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class AlertaDto
    {
        public int IdAlerta { get; set; }
        public string? TipoAlerta { get; set; }
        public string? Nivel { get; set; }
        public string? Mensaje { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }

    public class ReporteDto
    {
        public int IdReporte { get; set; }
        public string? TipoReporte { get; set; }
        public string? Formato { get; set; }
    }

    public class PersonalDto
    {
        public int IdPersonal { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? CorreoCorporativo { get; set; }
    }

    public class ConfigSlaDto
    {
        public int IdSla { get; set; }
        public string? CodigoSla { get; set; }
        public string? TipoSolicitud { get; set; }
    }

    public class RolRegistroDto
    {
        public int IdRolRegistro { get; set; }
        public string? NombreRol { get; set; }
    }

    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string? Username { get; set; }
        public string? Correo { get; set; }
    }
    public class SolicitudCreateDto
    {
        public int IdPersonal { get; set; }
        public int IdSla { get; set; }
        public int IdRolRegistro { get; set; }
        public int CreadoPor { get; set; }

        // obligatorias en el form
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaIngreso { get; set; }

        public string? ResumenSla { get; set; }
        public string? OrigenDato { get; set; }
        public string? EstadoSolicitud { get; set; }  // ACTIVO / etc.
    }

    public class SolicitudUpdateDto
    {
        public int IdPersonal { get; set; }
        public int IdSla { get; set; }
        public int IdRolRegistro { get; set; }
        public int CreadoPor { get; set; }

        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaIngreso { get; set; }

        public string? ResumenSla { get; set; }
        public string? OrigenDato { get; set; }
        public string? EstadoSolicitud { get; set; }
    }

    public class SolicitudDto
    {
        public int IdSolicitud { get; set; }
        public string? EstadoSolicitud { get; set; }
        public string? EstadoCumplimientoSla { get; set; }

        public string? ResumenSla { get; set; }
        public string? OrigenDato { get; set; }
        public DateOnly FechaSolicitud { get; set; }
        public DateOnly? FechaIngreso { get; set; }

        public int? NumDiasSla { get; set; }
        public DateTime CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }

        // claves
        public int IdPersonal { get; set; }
        public int IdSla { get; set; }
        public int IdRolRegistro { get; set; }
        public int CreadoPor { get; set; }

        // expandido
        public PersonalDto? Personal { get; set; }
        public ConfigSlaDto? ConfigSla { get; set; }
        public RolRegistroDto? RolRegistro { get; set; }
        public UsuarioDto? CreadoPorUsuario { get; set; }

        public List<AlertaDto> Alertas { get; set; } = new();
        public List<ReporteDto> Reportes { get; set; } = new();
    }
}

