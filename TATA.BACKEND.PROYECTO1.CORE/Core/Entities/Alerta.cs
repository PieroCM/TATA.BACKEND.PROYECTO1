using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

public partial class Alerta
{
    public int IdAlerta { get; set; }

    public int IdSolicitud { get; set; }

    public string TipoAlerta { get; set; } = null!;

    public string? Nivel { get; set; }

    public string Mensaje { get; set; } = null!;

    public string? Estado { get; set; }

    public bool EnviadoEmail { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaLectura { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual Solicitud IdSolicitudNavigation { get; set; } = null!;
}
