using System;
using System.Collections.Generic;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
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

        // ÚNICA colección de detalles (join entity explícita)
        public virtual ICollection<ReporteDetalle> Detalles { get; set; } = new List<ReporteDetalle>();
    }
}
