using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }
}

