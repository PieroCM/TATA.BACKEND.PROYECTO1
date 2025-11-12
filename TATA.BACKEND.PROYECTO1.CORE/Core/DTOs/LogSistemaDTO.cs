using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class LogSistemaDTO
    {
        public long IdLog { get; set; }
        public DateTime FechaHora { get; set; }
        public string Nivel { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? Detalles { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class LogSistemaCreateDTO
    {
        public string Nivel { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? Detalles { get; set; }
        public int? IdUsuario { get; set; }
    }
}
