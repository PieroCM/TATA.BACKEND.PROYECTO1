using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
    public partial class ReporteDetalle
    {
        public int IdReporte { get; set; }
        public int IdSolicitud { get; set; }

        // Navegaciones (coinciden con el mapeo en DbContext)
        public virtual Reporte? Reporte { get; set; }
        public virtual Solicitud? Solicitud { get; set; }
    }
}
