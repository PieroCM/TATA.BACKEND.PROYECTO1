using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario usuario);
        Task DeleteAsync(int id);
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByUsernameAsync(string username);
        Task<Usuario?> GetByEmailAsync(string email); // ⚠️ NUEVO: Buscar por correo
        Task<Usuario?> GetByRecoveryTokenAsync(string token);
        Task<Usuario?> GetByPersonalIdAsync(int idPersonal); // ⚠️ NUEVO: Buscar Usuario por IdPersonal
        Task UpdateAsync(Usuario usuario);
    }
}