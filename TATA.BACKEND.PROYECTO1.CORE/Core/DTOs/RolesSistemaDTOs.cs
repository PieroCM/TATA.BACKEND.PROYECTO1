using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class RolesSistemaCreateDTO
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }

    public class RolesSistemaUpdateDTO
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool EsActivo { get; set; }
    }

    public class RolesSistemaResponseDTO
    {
        public int IdRolSistema { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool EsActivo { get; set; }
    }
}
