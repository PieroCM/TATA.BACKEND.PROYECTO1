using System;
using System.Collections.Generic;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;


public partial class Permiso
{
    public int IdPermiso { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<RolesSistema> IdRolSistema { get; set; } = new List<RolesSistema>();
}
