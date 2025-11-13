using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario usuario);
        Task DeleteAsync(int id);
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByCorreoAsync(string correo);
        Task<Usuario?> GetByIdAsync(int id);
        Task UpdateAsync(Usuario usuario);
    }
}