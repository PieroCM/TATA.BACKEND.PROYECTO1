using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;


namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface ISolicitudRepository
    {
        Task<Solicitud> CreateSolicitudAsync(Solicitud solicitud);
        Task<bool> DeleteSolicitudAsync(int id, string deletedState = "ELIMINADO");
        Task<Solicitud?> GetSolicitudByIdAsync(int id);
        Task<List<Solicitud>> GetSolicitudsAsync();
        Task<Solicitud?> UpdateSolicitudAsync(int id, Solicitud solicitud);
        Task<ConfigSla?> GetConfigSlaByIdAsync(int idSla);
    }
}