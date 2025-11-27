namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // ===========================
    // DTOs PARA PERSONAL
    // ===========================
    
    public class PersonalCreateDTO
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Documento { get; set; }
        public string? CorreoCorporativo { get; set; }
        public string? Estado { get; set; }
    }

    // ⚠️ NUEVO: DTO para crear Personal con Cuenta de Usuario Condicional
    public class PersonalCreateWithAccountDTO
    {
        // ===========================
        // DATOS DE PERSONAL (Obligatorios solo Nombres y Apellidos)
        // ===========================
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? CorreoCorporativo { get; set; }
        public string? Documento { get; set; }
        public string? Estado { get; set; }

        // ===========================
        // CONTROL DE CREACIÓN DE CUENTA
        // ===========================
        public bool CrearCuentaUsuario { get; set; }

        // ===========================
        // DATOS DE USUARIO (Condicionales - Solo si CrearCuentaUsuario = true)
        // ===========================
        public string? Username { get; set; }
        public int? IdRolSistema { get; set; }
    }

    public class PersonalUpdateDTO
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Documento { get; set; }
        public string? CorreoCorporativo { get; set; }
        public string? Estado { get; set; }
    }

    public class PersonalResponseDTO
    {
        public int IdPersonal { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Documento { get; set; }
        public string? CorreoCorporativo { get; set; }
        public string? Estado { get; set; }
        
        // ⚠️ Datos de Usuario vinculado (si existe)
        public int? IdUsuario { get; set; }
        public string? Username { get; set; }
        public bool TieneCuentaUsuario { get; set; }
        public bool? CuentaActivada { get; set; }
    }
}