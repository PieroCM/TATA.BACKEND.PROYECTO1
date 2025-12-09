using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IPersonalService
    {
        Task<bool> CreateAsync(PersonalCreateDTO dto);
        
        // ⚠️ NUEVO: Crear Personal con cuenta de usuario condicional
        Task<bool> CreateWithAccountAsync(PersonalCreateWithAccountDTO dto);
        
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PersonalResponseDTO>> GetAllAsync();
        Task<PersonalResponseDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto);

        // ✅ NUEVO: Obtener listado unificado con LEFT JOIN (Personal → Usuario → RolesSistema)
        Task<IEnumerable<PersonalUsuarioResponseDTO>> GetUnifiedListAsync();

        // ✅ NUEVO: Deshabilitación Administrativa Total con eliminación condicional de Usuario
        // - eliminarUsuario = true: ELIMINA la cuenta de usuario (DELETE)
        // - eliminarUsuario = false: DESHABILITA la cuenta de usuario (UPDATE Estado = INACTIVO)
        Task<bool> DeshabilitarPersonalYUsuarioAsync(int idPersonal, bool eliminarUsuario = false);

        // ✅ NUEVO: Habilitación/Reactivación de Personal (SIN reactivar automáticamente el Usuario)
        // Solo reactiva el Personal (Estado = ACTIVO), el Usuario debe habilitarse manualmente por seguridad
        Task<bool> HabilitarPersonalAsync(int idPersonal);
    }
}