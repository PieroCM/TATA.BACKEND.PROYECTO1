using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class RolPermisoDTO
    {
        public int IdRolSistema { get; set; }
        public int IdPermiso { get; set; }

        // ✅ Propiedades nuevas para mostrar nombres
        public string? NombreRol { get; set; }
        public string? NombrePermiso { get; set; }

    }
    

    public class RolConPermisosDTO
    {
        public int IdRolSistema { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public List<PermisoDTO> Permisos { get; set; } = new();
    }

    public class PermisoDTO
    {
        public int IdPermiso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }
}
