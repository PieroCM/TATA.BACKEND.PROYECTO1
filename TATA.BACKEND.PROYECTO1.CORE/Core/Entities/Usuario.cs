using System;
using System.Collections.Generic;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Username { get; set; } = null!;

    public string? PasswordHash { get; set; } // ⚠️ Nullable para cuentas pendientes de activación

    public int IdRolSistema { get; set; }

    public int? IdPersonal { get; set; } // ⚠️ FK nullable - Relación 1:0..1 con Personal

    public string? Estado { get; set; }

    public DateTime? UltimoLogin { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public string? token_recuperacion { get; set; }

    public DateTime? expiracion_token { get; set; }

    // Navegación
    public virtual RolesSistema IdRolSistemaNavigation { get; set; } = null!;
    
    public virtual Personal? PersonalNavigation { get; set; } // ⚠️ Navegación 1:0..1 con Personal

    public virtual ICollection<LogSistema> LogSistema { get; set; } = new List<LogSistema>();

    public virtual ICollection<Reporte> Reporte { get; set; } = new List<Reporte>();

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();
}

