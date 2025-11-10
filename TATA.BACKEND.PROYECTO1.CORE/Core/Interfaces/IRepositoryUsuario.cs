using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IRepositoryUsuario
    {
        Task AddUsuario(Usuario usuario);
        Task DeleteUsuario(int id);
        Task<List<Usuario>> GetAllUsuarios();
        Task<Usuario?> GetUsuarioById(int id);
        Task<Usuario?> SignIn(string correo, string password);
        Task<bool> SignUp(Usuario newUser);
        Task UpdateUsuario(Usuario usuario);
    }
}