using System;
using System.Collections.Generic;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class LogSistema
{
    public long IdLog { get; set; }

    public DateTime FechaHora { get; set; }

    public string Nivel { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string? Detalles { get; set; }

    public int? IdUsuario { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
