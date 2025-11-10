using System;
using System.Collections.Generic;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class RolRegistro
{
    public int IdRolRegistro { get; set; }

    public string NombreRol { get; set; } = null!;

    public string? BloqueTech { get; set; }

    public string? Descripcion { get; set; }

    public bool EsActivo { get; set; }

    public virtual ICollection<Solicitud> Solicitud { get; set; } = new List<Solicitud>();
}
