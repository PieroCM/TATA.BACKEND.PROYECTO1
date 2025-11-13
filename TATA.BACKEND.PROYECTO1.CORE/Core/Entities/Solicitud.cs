using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class Solicitud
{
    public int IdSolicitud { get; set; }

    public int IdPersonal { get; set; }

    public int IdSla { get; set; }

    public int IdRolRegistro { get; set; }

    public int CreadoPor { get; set; }

    public DateOnly FechaSolicitud { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public int? NumDiasSla { get; set; }

    public string? ResumenSla { get; set; }

    public string? OrigenDato { get; set; }

    public string? EstadoSolicitud { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    public virtual Usuario CreadoPorNavigation { get; set; } = null!;

    public virtual Personal IdPersonalNavigation { get; set; } = null!;

    public virtual RolRegistro IdRolRegistroNavigation { get; set; } = null!;

    public virtual ConfigSla IdSlaNavigation { get; set; } = null!;

    //se borró esta línea porque no hay relación entre Solicitud y Reporte xD
    //public virtual ICollection<Reporte> IdReporte { get; set; } = new List<Reporte>();
}
