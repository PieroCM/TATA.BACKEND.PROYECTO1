using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using AutoMapper;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;


namespace TATA.BACKEND.PROYECTO1.CORE.Core.Mapping
{
    public class RolPermisoProfile : Profile
    {
        public RolPermisoProfile()
        {
            CreateMap<RolesSistema, RolConPermisosDTO>()
                .ForMember(dest => dest.NombreRol, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Permisos, opt => opt.MapFrom(src => src.IdPermiso));

            CreateMap<Permiso, PermisoDTO>();
        }
    }
}
