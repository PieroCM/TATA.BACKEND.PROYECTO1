using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;


namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

public partial class RolesSistema
{
    public int IdRolSistema { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool EsActivo { get; set; }

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();

    public virtual ICollection<Permiso> IdPermiso { get; set; } = new List<Permiso>();

    // ✅ Tu relación para RolPermisoEntity
    [NotMapped]
    public virtual ICollection<RolPermisoEntity> RolPermisos { get; set; } = new List<RolPermisoEntity>();
}
  