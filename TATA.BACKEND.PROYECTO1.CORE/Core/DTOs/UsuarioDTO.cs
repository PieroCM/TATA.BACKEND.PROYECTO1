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
        public string Email { get; set; } = string.Empty; // ⚠️ CAMBIO: Ahora usa Email (CorreoCorporativo)
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de respuesta completa del login con datos del usuario, rol y permisos
    /// </summary>
    public class SignInResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? IdPersonal { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public int IdRolSistema { get; set; }
        public string RolCodigo { get; set; } = string.Empty;
        public string RolNombre { get; set; } = string.Empty;
        public List<string> Permisos { get; set; } = new List<string>();
    }
    
    public class SignUpRequestDTO
    {
        public string Email { get; set; } = string.Empty; // ⚠️ CAMBIO: Email en lugar de Username
        public string Password { get; set; } = string.Empty;
        public int? IdPersonal { get; set; }
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
        public int? IdPersonal { get; set; }
        public string? NombresPersonal { get; set; }
        public string? ApellidosPersonal { get; set; }
        public string? CorreoPersonal { get; set; }
        public bool CuentaActivada { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public DateTime? CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }

    public class UsuarioCreateDTO
    {
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int IdRolSistema { get; set; }
        public int? IdPersonal { get; set; }
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
        public string Estado { get; set; } = null!;
    }

    // ===========================
    // CONTRASEÑAS
    // ===========================
  
    public class UsuarioChangePasswordDTO
    {
        public string Email { get; set; } = null!; // ⚠️ CAMBIO: Email en lugar de Username
        public string PasswordActual { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }

    // ===========================
    // ACTIVACIÓN DE CUENTA
    // ===========================
    
    public class ActivarCuentaDTO
    {
        public string Email { get; set; } = null!; // ⚠️ CAMBIO: Email en lugar de Username
        public string Token { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }

    // ===========================
    // VINCULACIÓN PERSONAL → USUARIO (ADMIN)
    // ===========================
    
    public class VincularPersonalDTO
    {
        public int IdPersonal { get; set; }
        public string Username { get; set; } = null!;
        public int IdRolSistema { get; set; }
    }
}
