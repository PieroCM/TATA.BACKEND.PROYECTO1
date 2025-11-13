using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public record RolRegistroDTO(
        int IdRolRegistro,
        string NombreRol,
        string? BloqueTech,
        string? Descripcion,
        bool EsActivo
    );

    public class RolRegistroCreateDTO
    {
        public string NombreRol { get; set; } = default!;
        public string? BloqueTech { get; set; }
        public string? Descripcion { get; set; }
    }

    public class RolRegistroUpdateDTO
    {
        public string NombreRol { get; set; } = default!;
        public string? BloqueTech { get; set; }
        public string? Descripcion { get; set; }
        public bool EsActivo { get; set; }
    }
}
