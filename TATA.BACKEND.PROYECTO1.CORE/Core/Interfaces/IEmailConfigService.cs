using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IEmailConfigService
    {
        Task<EmailConfigDTO?> GetConfigAsync();
        Task<EmailConfigDTO?> UpdateConfigAsync(int id, EmailConfigUpdateDTO dto);
    }
}
