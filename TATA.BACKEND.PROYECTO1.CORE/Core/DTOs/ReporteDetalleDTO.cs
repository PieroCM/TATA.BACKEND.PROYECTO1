using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // DTO de respuesta (lectura)
    public class ReporteDetalleDTO
    {
        public int IdReporte { get; set; }
        public int IdSolicitud { get; set; }
    }

    // DTO para crear la relación (POST)
    public class ReporteDetalleCreateRequest
    {
        [Range(1, int.MaxValue)]
        public int IdReporte { get; set; }

        [Range(1, int.MaxValue)]
        public int IdSolicitud { get; set; }
    }

    // DTO para PUT de la relación.
    // En la join table no hay campos mutables; se mantiene para cumplir el "PUT".
    public class ReporteDetalleUpdateRequest
    {
        [Range(1, int.MaxValue)]
        public int IdReporte { get; set; }

        [Range(1, int.MaxValue)]
        public int IdSolicitud { get; set; }
    }
}
