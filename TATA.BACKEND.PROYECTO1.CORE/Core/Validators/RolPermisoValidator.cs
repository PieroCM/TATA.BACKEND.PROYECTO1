using FluentValidation;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Validators
{
    public class RolPermisoRequestValidator : AbstractValidator<RolPermisoDTO>
    {
        public RolPermisoRequestValidator()
        {
            RuleFor(x => x.IdRolSistema)
                .GreaterThan(0).WithMessage("El Id del rol debe ser mayor que 0.");

            RuleFor(x => x.IdPermiso)
                .GreaterThan(0).WithMessage("El Id del permiso debe ser mayor que 0.");
        }
    }
}
