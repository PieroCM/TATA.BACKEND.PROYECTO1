using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface ISubidaVolumenServices
    {
        Task<BulkUploadResultDto> ProcesarSolicitudesAsync(IEnumerable<SubidaVolumenSolicitudRowDto> filas);
    }
}