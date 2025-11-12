using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Username { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int IdRolSistema { get; set; }

    public string? Estado { get; set; }

    public DateTime? UltimoLogin { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual RolesSistema IdRolSistemaNavigation { get; set; } = null!;

    public virtual ICollection<LogSistema> LogSistema { get; set; } = new List<LogSistema>();

    public virtual ICollection<Personal> Personal { get; set; } = new List<Personal>();

    public virtual ICollection<Reporte> Reporte { get; set; } = new List<Reporte>();

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();

}

