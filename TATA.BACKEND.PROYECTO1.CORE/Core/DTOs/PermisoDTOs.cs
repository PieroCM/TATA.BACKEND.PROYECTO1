using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class PermisoCreateDTO
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }

    public class PermisoUpdateDTO
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }

    public class PermisoResponseDTO
    {
        public int IdPermiso { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}
