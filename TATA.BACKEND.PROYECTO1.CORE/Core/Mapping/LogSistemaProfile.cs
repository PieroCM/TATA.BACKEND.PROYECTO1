using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Mapping
{
    public class LogSistemaProfile : Profile
    {
        public LogSistemaProfile()
        {
            CreateMap<LogSistema, LogSistemaDTO>().ReverseMap();
            CreateMap<LogSistemaCreateDTO, LogSistema>();
        }
    }
}
