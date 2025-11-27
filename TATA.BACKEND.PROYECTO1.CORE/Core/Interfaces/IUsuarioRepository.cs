using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario usuario);
        Task DeleteAsync(int id);
        Task<IEnumerable<Usuario>> GetAllAsync();
        
        // ⚠️ CAMBIO: GetByCorreoAsync → GetByUsernameAsync
        Task<Usuario?> GetByUsernameAsync(string username);
        
        Task<Usuario?> GetByIdAsync(int id);
        Task UpdateAsync(Usuario usuario);
        
        // Métodos para recuperación de contraseña
        Task<Usuario?> GetByRecoveryTokenAsync(string token);
    }
}