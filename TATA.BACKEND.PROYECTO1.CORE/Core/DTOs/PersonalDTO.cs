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

    // ===========================
    // ✅ NUEVO: DTO UNIFICADO PARA GESTIÓN DE USUARIOS
    // LEFT JOIN: Personal → Usuario → RolesSistema
    // Este DTO se usa para el listado completo de la tabla de Gestión de Usuarios
    // ===========================
    public class PersonalUsuarioResponseDTO
    {
        // ========== DATOS DE PERSONAL (Siempre presentes) ==========
        
        /// <summary>
        /// Clave Maestra de la Fila - ID del Personal
        /// </summary>
        public int IdPersonal { get; set; }

        /// <summary>
        /// Nombres de la persona (Personal)
        /// </summary>
        public string Nombres { get; set; } = string.Empty;

        /// <summary>
        /// Apellidos de la persona (Personal)
        /// </summary>
        public string Apellidos { get; set; } = string.Empty;

        /// <summary>
        /// Documento de identidad único de la persona (Personal)
        /// </summary>
        public string? Documento { get; set; }

        /// <summary>
        /// Email corporativo de contacto/activación (Personal)
        /// </summary>
        public string? CorreoCorporativo { get; set; }

        /// <summary>
        /// Estado del registro de Personal: "ACTIVO" o "INACTIVO"
        /// </summary>
        public string? EstadoPersonal { get; set; }

        // ========== DATOS DE USUARIO (Pueden ser NULL si no tiene cuenta) =========
        
        /// <summary>
        /// ID de la cuenta de usuario (necesario para PATCH /toggle-estado)
        /// Será NULL si el Personal no tiene cuenta de usuario
        /// </summary>
        public int? IdUsuario { get; set; }

        /// <summary>
        /// Username para login (Usuario)
        /// Será NULL si el Personal no tiene cuenta de usuario
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Estado de la cuenta de acceso: "ACTIVO" o "INACTIVO" (Usuario.Estado)
        /// Será NULL si el Personal no tiene cuenta de usuario
        /// </summary>
        public string? EstadoCuentaAcceso { get; set; }

        /// <summary>
        /// Indica si la cuenta está activada (PasswordHash != NULL)
        /// true = Cuenta activada (tiene contraseña)
        /// false = Cuenta pendiente de activación (sin contraseña) o no tiene cuenta
        /// </summary>
        public bool CuentaActivada { get; set; }

        // ========== DATOS DE ROL (Pueden ser NULL si no tiene cuenta) ==========
        
        /// <summary>
        /// Nombre del Rol del Sistema (RolesSistema.Nombre)
        /// Ej: "Administrador", "Operador", "Técnico"
        /// Será NULL si el Personal no tiene cuenta de usuario
        /// </summary>
        public string? NombreRol { get; set; }

        // Ingresando fecha de creacion
        public DateTime CreadoEn { get; set; }
    }

    // ===========================
    // ✅ NUEVO: DTO PARA DESHABILITACIÓN CONDICIONAL DE PERSONAL Y USUARIO
    // Controla si el Usuario se elimina o solo se deshabilita
    // ===========================
    
    /// <summary>
    /// DTO para la deshabilitación administrativa de Personal con eliminación condicional de Usuario
    /// </summary>
    public class DeshabilitarPersonalDTO
    {
        /// <summary>
        /// Determina la acción sobre la cuenta de usuario:
        /// - true: ELIMINA la cuenta de usuario (DELETE)
        /// - false o null: DESHABILITA la cuenta de usuario (UPDATE Estado = INACTIVO)
        /// </summary>
        public bool EliminarUsuario { get; set; } = false;
    }

    // ===========================
    // ✅ NUEVO: DTO PARA HABILITACIÓN/REACTIVACIÓN DE PERSONAL
    // Reactiva el Personal pero NO reactiva automáticamente el Usuario por seguridad
    // ===========================
    
    /// <summary>
    /// DTO vacío para la habilitación/reactivación de Personal
    /// El endpoint sugiere la acción, no requiere parámetros adicionales
    /// </summary>
    public class HabilitarPersonalDTO
    {
        // Body vacío o ausente - La acción se infiere del endpoint
    }
}