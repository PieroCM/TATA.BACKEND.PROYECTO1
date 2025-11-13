using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class Reporte
{
    public int IdReporte { get; set; }

    public string TipoReporte { get; set; } = null!;

    public string Formato { get; set; } = null!;

    public string? FiltrosJson { get; set; }

    public string? RutaArchivo { get; set; }

    public int GeneradoPor { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public virtual Usuario GeneradoPorNavigation { get; set; } = null!;

    public virtual ICollection<Solicitud> IdSolicitud { get; set; } = new List<Solicitud>();
}
