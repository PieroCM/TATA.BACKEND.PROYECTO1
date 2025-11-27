using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // ===========================
    // AUTENTICACIÓN
    // ===========================
    
    public class SignInRequestDTO
    {
        public string Username { get; set; } = string.Empty; // ⚠️ Ahora usa Username en lugar de Correo
        public string Password { get; set; } = string.Empty;
    }
    
    public class SignUpRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int? IdPersonal { get; set; } // ⚠️ Opcional: vincular con Personal existente
    }

    // ===========================
    // GESTIÓN DE USUARIOS
    // ===========================

    public class UsuarioResponseDTO
    {
        public int IdUsuario { get; set; }
        public string Username { get; set; } = string.Empty;
        public int IdRolSistema { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int? IdPersonal { get; set; } // ⚠️ Nullable
        public string? NombresPersonal { get; set; } // ⚠️ De PersonalNavigation
        public string? ApellidosPersonal { get; set; } // ⚠️ De PersonalNavigation
        public string? CorreoPersonal { get; set; } // ⚠️ De PersonalNavigation.CorreoCorporativo
        public bool CuentaActivada { get; set; } // ⚠️ true si PasswordHash != null
        public DateTime? UltimoLogin { get; set; }
        public DateTime? CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }

    public class UsuarioCreateDTO
    {
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; } // ⚠️ Nullable para cuentas sin activar
        public int IdRolSistema { get; set; }
        public int? IdPersonal { get; set; } // ⚠️ Opcional
        public string Estado { get; set; } = "ACTIVO";
    }

    public class UsuarioUpdateDTO
    {
        public string? Username { get; set; }
        public int? IdRolSistema { get; set; }
        public string? Estado { get; set; }
    }

    public class UsuarioToggleEstadoDTO
    {
        public string Estado { get; set; } = null!; // ACTIVO o INACTIVO
    }

    // ===========================
    // CONTRASEÑAS
    // ===========================
  
    public class UsuarioChangePasswordDTO
    {
        public string Username { get; set; } = null!;        // ⚠️ Ahora usa Username
        public string PasswordActual { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }

    // ===========================
    // ACTIVACIÓN DE CUENTA
    // ===========================
    
    public class ActivarCuentaDTO
    {
        public string Username { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }
}
