using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;


public partial class ConfigSla
{
    public int IdSla { get; set; }

    public string CodigoSla { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int DiasUmbral { get; set; }

    public string TipoSolicitud { get; set; } = null!;

    public bool EsActivo { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();
}
