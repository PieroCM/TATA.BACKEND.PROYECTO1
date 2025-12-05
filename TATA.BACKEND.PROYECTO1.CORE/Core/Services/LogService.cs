using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class LogService : ILogService
    {
        private readonly ILogSistemaService _logSistemaService;

        public LogService(ILogSistemaService logSistemaService)
        {
            _logSistemaService = logSistemaService;
        }

        public async Task RegistrarLogAsync(
            string nivel,
            string mensaje,
            string? detalles = null,
            int? idUsuario = null)
        {
            var dto = new LogSistemaCreateDTO
            {
                Nivel = nivel,
                Mensaje = mensaje,
                Detalles = detalles,
                IdUsuario = idUsuario
            };

            await _logSistemaService.AddAsync(dto);
        }
    }
}
