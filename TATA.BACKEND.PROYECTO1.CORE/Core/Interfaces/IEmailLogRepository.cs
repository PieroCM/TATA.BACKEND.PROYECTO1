using System.Collections.Generic;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IEmailLogRepository
    {
        Task<List<EmailLog>> GetLogsAsync(int take = 50);
        Task<EmailLog> CreateLogAsync(EmailLog log);
    }
}
