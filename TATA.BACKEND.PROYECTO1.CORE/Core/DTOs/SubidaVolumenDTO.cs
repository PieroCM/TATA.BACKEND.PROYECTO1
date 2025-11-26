using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// Representa UNA fila del Excel/JSON de carga masiva de solicitudes SLA.
    /// </summary>
    public class SubidaVolumenSolicitudRowDto
    {
        // -------- RolSistema + Usuario + Personal --------

        [JsonPropertyName("rol_sistema_codigo")]
        public string RolSistemaCodigo { get; set; } = string.Empty;

        [JsonPropertyName("rol_sistema_nombre")]
        public string? RolSistemaNombre { get; set; }

        [JsonPropertyName("rol_sistema_descripcion")]
        public string? RolSistemaDescripcion { get; set; }

        [JsonPropertyName("usuario_correo")]
        public string UsuarioCorreo { get; set; } = string.Empty;

        [JsonPropertyName("usuario_username")]
        public string? UsuarioUsername { get; set; }

        [JsonPropertyName("personal_nombres")]
        public string PersonalNombres { get; set; } = string.Empty;

        [JsonPropertyName("personal_apellidos")]
        public string PersonalApellidos { get; set; } = string.Empty;

        [JsonPropertyName("personal_documento")]
        public string PersonalDocumento { get; set; } = string.Empty;

        [JsonPropertyName("personal_correo")]
        public string PersonalCorreo { get; set; } = string.Empty;

        // -------------------- ConfigSla --------------------

        [JsonPropertyName("config_sla_codigo")]
        public string ConfigSlaCodigo { get; set; } = string.Empty;

        [JsonPropertyName("config_sla_descripcion")]
        public string? ConfigSlaDescripcion { get; set; }

        [JsonPropertyName("config_sla_dias_umbral")]
        public int ConfigSlaDiasUmbral { get; set; }

        [JsonPropertyName("config_sla_tipo_solicitud")]
        public string ConfigSlaTipoSolicitud { get; set; } = string.Empty;

        // ------------------- RolRegistro -------------------

        [JsonPropertyName("rol_registro_nombre")]
        public string RolRegistroNombre { get; set; } = string.Empty;

        [JsonPropertyName("rol_registro_bloque_tech")]
        public string RolRegistroBloqueTech { get; set; } = string.Empty;

        [JsonPropertyName("rol_registro_descripcion")]
        public string? RolRegistroDescripcion { get; set; }

        // --------------------- Solicitud --------------------

        /// <summary>
        /// Fecha de solicitud OBLIGATORIA, viene como string desde el Excel/JSON.
        /// El servicio la parsea con DateTime.TryParse y, si falla, marca la fila con error.
        /// </summary>
        [JsonPropertyName("sol_fecha_solicitud")]
        public string SolFechaSolicitud { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de ingreso de la solicitud (OPCIONAL).
        /// Viene como string; si está vacío, el servicio la toma como null.
        /// </summary>
        [JsonPropertyName("sol_fecha_ingreso")]
        public string? SolFechaIngreso { get; set; }

        [JsonPropertyName("sol_resumen")]
        public string? SolResumen { get; set; }

        [JsonPropertyName("sol_origen_dato")]
        public string? SolOrigenDato { get; set; }

        [JsonPropertyName("sol_estado")]
        public string? SolEstado { get; set; }
    }

    /// <summary>
    /// Detalle de error por fila en la carga masiva.
    /// </summary>
    public class BulkUploadErrorDto
    {
        /// <summary>
        /// Índice de fila (1-based) tal como viene del Excel/JSON.
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Mensaje descriptivo del error.
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado agregado de la carga masiva.
    /// </summary>
    public class BulkUploadResultDto
    {
        public int TotalFilas { get; set; }
        public int FilasExitosas { get; set; }
        public int FilasConError { get; set; }
        public List<BulkUploadErrorDto> Errores { get; set; } = new();
    }
}
