using MailKit.Net.Smtp;         // Librería nueva
using MailKit.Security;         // Librería nueva
using Microsoft.Extensions.Options;
using MimeKit;                  // Librería nueva
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

        public async Task SendAsync(string to, string subject, string body)
        {
            // 1. Crear el mensaje (Soporta tildes y emojis nativamente)
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Sistema TATA", _settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = builder.ToMessageBody();

            // 2. Usar el Cliente SMTP de MailKit (El que sí funciona con Gmail)
            using var client = new SmtpClient();

            try
            {
                // Ignora errores de certificado SSL (útil si tienes antivirus bloqueando puertos)
                client.CheckCertificateRevocation = false;

                // Conectar usando TLS seguro al puerto 587
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

                // Autenticarse
                await client.AuthenticateAsync(_settings.User, _settings.Password);

                // Enviar
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                // Lanzamos el error para verlo en el log si falla
                throw new InvalidOperationException($"Error enviando a {to}: {ex.Message}", ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}