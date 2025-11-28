using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IUsuarioService
    {
        // Autenticación
        Task<string?> SignInAsync(SignInRequestDTO dto);
        Task<bool> SignUpAsync(SignUpRequestDTO dto);
        
        // CRUD Completo
        Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync();
        Task<UsuarioResponseDTO?> GetByIdAsync(int id);
        Task<UsuarioResponseDTO?> CreateAsync(UsuarioCreateDTO dto);
        Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleEstadoAsync(int id, UsuarioToggleEstadoDTO dto);

        // Contraseñas
        Task<bool> ChangePasswordAsync(UsuarioChangePasswordDTO dto);
        Task<bool> SolicitarRecuperacionPassword(SolicitarRecuperacionDTO request);
        Task<bool> RestablecerPassword(RestablecerPasswordDTO request);
        
        // ⚠️ NUEVO: Activación de cuenta
        Task<bool> ActivarCuenta(ActivarCuentaDTO request);

        // ⚠️ NUEVO: Vincular Personal existente con nueva cuenta Usuario (ADMIN)
        Task VincularPersonalYActivarAsync(VincularPersonalDTO dto);
    }
}
