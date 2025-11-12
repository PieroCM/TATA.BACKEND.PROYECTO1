using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class UsuarioChangePasswordDTO
    {
        public string Correo { get; set; } = null!;          // Para identificar al usuario
        public string PasswordActual { get; set; } = null!;  // Contraseña actual
        public string NuevaPassword { get; set; } = null!;   // Nueva contraseña
    }
}
