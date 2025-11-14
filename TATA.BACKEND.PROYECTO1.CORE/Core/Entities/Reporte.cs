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

    // colección de filas en la tabla intermedia
    public virtual ICollection<ReporteDetalle> Detalles { get; set; } = new List<ReporteDetalle>();

    // skip-navigation many-to-many: solicitudes relacionadas
    public virtual ICollection<Solicitud> Solicitudes { get; set; } = new List<Solicitud>();
}
