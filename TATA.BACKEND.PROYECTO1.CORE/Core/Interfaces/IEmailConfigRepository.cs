using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IEmailConfigRepository
    {
        Task<EmailConfig?> GetConfigAsync();
        Task<EmailConfig> CreateOrUpdateAsync(EmailConfig config);
    }
}
