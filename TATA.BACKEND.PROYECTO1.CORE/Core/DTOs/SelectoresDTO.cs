using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO simplificado para selectores de Rol
    /// </summary>
    public class RolSelectorDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
    }

    /// <summary>
    /// DTO simplificado para selectores de SLA
    /// </summary>
    public class SlaSelectorDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
    }
}
