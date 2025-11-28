using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            var mail = new MailMessage(_settings.From, to, subject, body)
            {
                IsBodyHtml = true   // porque le estamos mandando HTML del template
            };

            await client.SendMailAsync(mail);
        }
    }
}
