using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IAlertaRepository
    {
        Task<Alerta> CreateAlertaAsync(Alerta alerta);
        Task<bool> DeleteAlertaAsync(int id);
        Task<Alerta?> GetAlertaByIdAsync(int id);
        Task<List<Alerta>> GetAlertasAsync();
        Task<Alerta?> UpdateAlertaAsync(int id, Alerta alerta);
        Task<List<Alerta>> GetAlertasPorVencer(int dias);
        Task<List<Alerta>> GetAlertasByFechaCreacion(DateTime fechaCreacion);
        Task<Alerta?> GetAlertaBySolicitudIdAsync(int idSolicitud);
        Task<List<Alerta>> GetAlertasWithFullNavigationAsync();
    }
}