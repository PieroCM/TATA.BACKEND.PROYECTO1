using System;
using System.ComponentModel.DataAnnotations;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    /// <summary>
    /// DTO PLANO enriquecido para el Dashboard del Frontend
    /// Sin objetos anidados profundos - Fácil de consumir
    /// </summary>
    public class AlertaDashboardDto
    {
        // ========== DATOS DE LA ALERTA ==========
        public int IdAlerta { get; set; }
        public string TipoAlerta { get; set; } = null!;
        public string? Nivel { get; set; }
        public string Mensaje { get; set; } = null!;
        public string? Estado { get; set; }
        public bool EnviadoEmail { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLectura { get; set; }

        // ========== DATOS DE LA SOLICITUD (PLANOS) ==========
        public int IdSolicitud { get; set; }
        public string? CodigoSolicitud { get; set; } // Ej: "SOL-2025-001"
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string? EstadoSolicitud { get; set; }
        public string? EstadoCumplimientoSla { get; set; }
        
        /// <summary>
        /// Fecha calculada de vencimiento: FechaSolicitud + DiasUmbral
        /// Utilizado internamente para filtros de VIGENTE/VENCIDO
        /// </summary>
        public DateTime FechaVencimiento { get; set; }

        // ========== DATOS DEL RESPONSABLE (PLANOS - CRÍTICO) ==========
        public int IdPersonal { get; set; }
        public string? NombreResponsable { get; set; } // "Juan Pérez"
        public string? EmailResponsable { get; set; } // CRÍTICO para envíos
        public string? DocumentoResponsable { get; set; }

        // ========== DATOS DEL ROL (PLANOS) ==========
        public int IdRolRegistro { get; set; }
        public string? NombreRol { get; set; }
        public string? BloqueTech { get; set; }

        // ========== DATOS DEL SLA (PLANOS) ==========
        public int IdSla { get; set; }
        public string? CodigoSla { get; set; }
        public string? NombreSla { get; set; } // Descripción del SLA
        public int DiasUmbral { get; set; }
        public string? TipoSolicitud { get; set; }

        // ========== CÁLCULOS MATEMÁTICOS PARA EL FRONTEND ==========
        public int DiasRestantes { get; set; } // Puede ser negativo si venció
        public int PorcentajeProgreso { get; set; } // 0-100
        public string ColorEstado { get; set; } = "#2196F3"; // Hex color para UI
        public string IconoEstado { get; set; } = "info"; // Para iconografía del frontend
        public bool EstaVencida { get; set; } // true si DiasRestantes < 0
        public bool EsCritica { get; set; } // true si Nivel == CRITICO
    }
}
