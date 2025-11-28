using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
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

        /// <summary>
        /// Envío simple HTML (compatible con llamadas anteriores).
        /// </summary>
        public async Task SendAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();

            // Nombre visible + correo configurado
            message.From.Add(new MailboxAddress("Sistema TATA", _settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                client.CheckCertificateRevocation = false;

                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_settings.User, _settings.Password);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error enviando correo a {to}: {ex.Message}", ex);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }

        /// <summary>
        /// Envío con un archivo adjunto (PDF por defecto).
        /// </summary>
        public async Task SendWithAttachmentAsync(
            string to,
            string subject,
            string body,
            byte[] attachmentBytes,
            string attachmentFileName,
            string attachmentContentType = "application/pdf")
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Sistema TATA", _settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            if (attachmentBytes != null && attachmentBytes.Length > 0)
            {
                var contentType = ContentType.Parse(attachmentContentType);
                builder.Attachments.Add(attachmentFileName, attachmentBytes, contentType);
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                client.CheckCertificateRevocation = false;

                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_settings.User, _settings.Password);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error enviando correo con adjunto a {to}: {ex.Message}", ex);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}
