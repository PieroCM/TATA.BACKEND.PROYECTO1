
namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public interface ILogService
    {
        Task RegistrarLogAsync(string nivel, 
            string mensaje, 
            string? detalles = null, 
            int? idUsuario = null
            );
    }
}