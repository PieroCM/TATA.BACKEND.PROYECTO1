using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class UsuarioDTO
    {
        public int IdUsuario { get; set; }              // Identificador del usuario
        public string Username { get; set; } = string.Empty; // Nombre del usuario
        public string Correo { get; set; } = string.Empty;   // Correo electrónico
        public int IdRolSistema { get; set; }           // Rol del usuario (Admin, etc.)
        public string? Estado { get; set; }             // ACTIVO / INACTIVO
        public DateTime? UltimoLogin { get; set; }      // Último inicio de sesión

    }
}
