using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // DTO de respuesta (lectura)
    public class ReporteDTO
    {
        public int IdReporte { get; set; }
        public string TipoReporte { get; set; } = default!;
        public string Formato { get; set; } = default!;
        public string? FiltrosJson { get; set; }
        public string? RutaArchivo { get; set; }
        public int GeneradoPor { get; set; }                  // FK -> usuario
        public string? GeneradoPorNombre { get; set; }         // nuevo: username del usuario
        public DateTime FechaGeneracion { get; set; }
        public int TotalSolicitudes { get; set; }
    }

    // DTO para crear (POST)
    public class ReporteCreateRequest
    {
        [Required, StringLength(40)]
        public string TipoReporte { get; set; } = default!;

        [Required, StringLength(10)]
        public string Formato { get; set; } = default!;

        public string? FiltrosJson { get; set; }

        [StringLength(400)]
        public string? RutaArchivo { get; set; }

        [Range(1, int.MaxValue)]
        public int GeneradoPor { get; set; }
    }

    // DTO para actualizar (PUT). El Id viaja en la ruta.
    public class ReporteUpdateRequest
    {
        [Required, StringLength(40)]
        public string TipoReporte { get; set; } = default!;

        [Required, StringLength(10)]
        public string Formato { get; set; } = default!;

        public string? FiltrosJson { get; set; }

        [StringLength(400)]
        public string? RutaArchivo { get; set; }

        [Range(1, int.MaxValue)]
        public int GeneradoPor { get; set; }
    }

    // Nuevo contrato para generación: se elimina objeto Filtros y se usa directamente FiltrosJson dinámico
    public class GenerarReporteRequest
    {
        public string TipoReporte { get; set; } = default!;         // "SLA_MENSUAL"
        public string Formato { get; set; } = default!;             // "XLSX", "PDF", etc.
        public List<int> IdsSolicitudes { get; set; } = new();      // [1,2,4,...]
        public string? FiltrosJson { get; set; }                    // JSON arbitrario enviado por el front
    }

}
