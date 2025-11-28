using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

// MailKit
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        // Legacy simple HTML email (kept for existing callers)
        public async Task SendAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.User, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // New method to support a single attachment (PDF default)
        public async Task SendWithAttachmentAsync(string to, string subject, string body, byte[] attachmentBytes, string attachmentFileName, string attachmentContentType = "application/pdf")
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            if (attachmentBytes != null && attachmentBytes.Length > 0)
            {
                var contentType = ContentType.Parse(attachmentContentType);
                builder.Attachments.Add(attachmentFileName, attachmentBytes, contentType);
            }

            message.Body = builder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.User, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
