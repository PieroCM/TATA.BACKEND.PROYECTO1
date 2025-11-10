using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public record ConfigSlaDTO(
        int IdSla,
        string CodigoSla,
        string? Descripcion,
        int DiasUmbral,
        string TipoSolicitud,
        bool EsActivo,
        DateTime CreadoEn,
        DateTime? ActualizadoEn
    );

    public class ConfigSlaCreateDTO
    {
        public string CodigoSla { get; set; } = default!;
        public string? Descripcion { get; set; }
        public int DiasUmbral { get; set; }
        public string TipoSolicitud { get; set; } = default!;
        public bool EsActivo { get; set; } = true;
    }

    public class ConfigSlaUpdateDTO
    {
        public string CodigoSla { get; set; } = default!;
        public string? Descripcion { get; set; }
        public int DiasUmbral { get; set; }
        public string TipoSolicitud { get; set; } = default!;
        public bool EsActivo { get; set; }
    }
}
