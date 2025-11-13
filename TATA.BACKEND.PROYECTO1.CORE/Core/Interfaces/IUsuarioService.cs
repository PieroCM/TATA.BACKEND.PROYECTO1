using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IUsuarioService
    {
        Task<string?> SignInAsync(SignInRequestDTO dto);
        Task<bool> SignUpAsync(SignUpRequestDTO dto);
        Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync();
        Task<UsuarioResponseDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto);
        Task<bool> DeleteAsync(int id);


        //CAMBIAR CONTRASEÑA
        Task<bool> ChangePasswordAsync(UsuarioChangePasswordDTO dto);


    }
}
