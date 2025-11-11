using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRepositoryAlerta
    {
        Task<Alerta> CreateAlertaAsync(Alerta alerta);
        Task<bool> DeleteAlertaAsync(int id);
        Task<Alerta?> GetAlertaByIdAsync(int id);
        Task<List<Alerta>> GetAlertasAsync();
        Task<Alerta?> UpdateAlertaAsync(int id, Alerta alerta);
    }
}