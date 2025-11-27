using System;
using System.Collections.Generic;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class Personal
{
    public int IdPersonal { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string? CorreoCorporativo { get; set; }

    public string? Documento { get; set; }

    public string? Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    // ⚠️ Navegación inversa 1:0..1 - Un Personal puede tener 0 o 1 Usuario
    public virtual Usuario? UsuarioNavigation { get; set; }

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();
}
