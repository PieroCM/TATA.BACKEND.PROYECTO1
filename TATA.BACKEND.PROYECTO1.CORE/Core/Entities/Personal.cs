using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;


public partial class Personal
{
    public int IdPersonal { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string? CorreoCorporativo { get; set; }

    public string? Documento { get; set; }

    public string? Estado { get; set; }

    public int IdUsuario { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();
}
