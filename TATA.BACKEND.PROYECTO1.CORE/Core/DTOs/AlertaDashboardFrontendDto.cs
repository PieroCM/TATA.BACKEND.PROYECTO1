using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO compatible con el frontend Vue.js para el dashboard de alertas
    /// </summary>
    public class AlertaDashboardFrontendDto
    {
        public int IdAlerta { get; set; }
        public string Nivel { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public DateTime FechaRegistro { get; set; }
        public int DiasRestantes { get; set; }
        public double PorcentajeProgreso { get; set; }
        public SolicitudDashboardDto? Solicitud { get; set; }
    }

    public class SolicitudDashboardDto
    {
        public int IdSolicitud { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = null!;
        public ConfigSlaDashboardDto? ConfigSla { get; set; }
        public RolRegistroDashboardDto? RolRegistro { get; set; }
    }

    public class ConfigSlaDashboardDto
    {
        public int IdConfigSla { get; set; }
        public string NombreSla { get; set; } = null!;
        public string CodigoSla { get; set; } = null!;
        public int DiasUmbral { get; set; }
        public string? Descripcion { get; set; }
    }

    public class RolRegistroDashboardDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}
