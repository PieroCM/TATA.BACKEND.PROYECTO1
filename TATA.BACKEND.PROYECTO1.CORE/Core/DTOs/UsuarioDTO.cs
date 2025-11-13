using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
 
    public class SignInRequestDTO
    {
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class SignUpRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int IdRolSistema { get; set; }
    }

    public class UsuarioResponseDTO
    {
        public int IdUsuario { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int IdRolSistema { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? UltimoLogin { get; set; }
        public DateTime? CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }
    public class UsuarioUpdateDTO
    {
        public string Estado { get; set; }
    }
    public class UsuarioChangePasswordDTO
    {
        public string Correo { get; set; } = null!;          // Para identificar al usuario
        public string PasswordActual { get; set; } = null!;  // Contraseña actual
        public string NuevaPassword { get; set; } = null!;   // Nueva contraseña
    }
}
