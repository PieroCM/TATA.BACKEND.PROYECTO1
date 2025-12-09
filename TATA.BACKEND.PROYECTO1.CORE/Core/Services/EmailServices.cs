using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
using System.Net.Sockets;
using System.Net.Security;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Envío simple HTML (compatible con llamadas anteriores).
        /// </summary>
        public async Task SendAsync(string to, string subject, string body)
        {
            _logger.LogInformation("🔵 INICIANDO envío de correo a {To} con asunto: {Subject}", to, subject);
            
            ValidateEmailSettings();
            ValidateParameters(to, subject, body);

            var message = new MimeMessage();

            try
            {
                // Construir mensaje
                message.From.Add(new MailboxAddress("Sistema TATA", _settings.From));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                message.Body = builder.ToMessageBody();

                _logger.LogInformation("📧 Mensaje construido correctamente");

                // Enviar con cliente SMTP mejorado
                await SendWithImprovedSmtpClientAsync(message, to);

                _logger.LogInformation("✅ ÉXITO: Correo enviado exitosamente a {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR CRÍTICO al enviar correo a {To}. Tipo: {Type}, Mensaje: {Message}", 
                    to, ex.GetType().Name, ex.Message);
                
                // RE-LANZAR la excepción para que el llamador la maneje
                throw new InvalidOperationException(
                    $"❌ FALLO SMTP al enviar a {to}: {GetDetailedErrorMessage(ex)}", 
                    ex);
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
            _logger.LogInformation("🔵 INICIANDO envío con adjunto a {To}", to);
            
            ValidateEmailSettings();
            ValidateParameters(to, subject, body);

            if (attachmentBytes == null || attachmentBytes.Length == 0)
            {
                _logger.LogWarning("⚠️ Adjunto vacío o nulo, enviando correo sin adjunto");
                await SendAsync(to, subject, body);
                return;
            }

            var message = new MimeMessage();

            try
            {
                message.From.Add(new MailboxAddress("Sistema TATA", _settings.From));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                
                var contentType = ContentType.Parse(attachmentContentType);
                builder.Attachments.Add(attachmentFileName, attachmentBytes, contentType);
                
                message.Body = builder.ToMessageBody();

                _logger.LogInformation("📎 Adjunto agregado ({Size} bytes)", attachmentBytes.Length);

                await SendWithImprovedSmtpClientAsync(message, to);

                _logger.LogInformation("✅ ÉXITO: Correo con adjunto enviado a {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR CRÍTICO al enviar correo con adjunto a {To}", to);
                throw new InvalidOperationException(
                    $"❌ FALLO SMTP al enviar adjunto a {to}: {GetDetailedErrorMessage(ex)}", 
                    ex);
            }
        }

        #region Métodos Privados Mejorados

        private async Task SendWithImprovedSmtpClientAsync(MimeMessage message, string recipient)
        {
            using var client = new SmtpClient();
            
            try
            {
                // Configurar cliente con mejor manejo de errores
                client.CheckCertificateRevocation = false;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true; // Para desarrollo
                
                _logger.LogDebug("🔌 Conectando a servidor SMTP {Host}:{Port}...", _settings.Host, _settings.Port);

                // Conectar con timeout
                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                if (!client.IsConnected)
                {
                    throw new InvalidOperationException("❌ No se pudo establecer conexión con el servidor SMTP");
                }

                _logger.LogInformation("✅ Conectado a {Host}:{Port}", _settings.Host, _settings.Port);
                _logger.LogDebug("🔐 Autenticando usuario {User}...", _settings.User);

                // Autenticar
                await client.AuthenticateAsync(_settings.User, _settings.Password);

                if (!client.IsAuthenticated)
                {
                    throw new InvalidOperationException("❌ Falló la autenticación SMTP. Verifica credenciales.");
                }

                _logger.LogInformation("✅ Autenticado correctamente");
                _logger.LogDebug("📤 Enviando mensaje...");

                // Enviar mensaje
                var response = await client.SendAsync(message);

                _logger.LogInformation("✅ Mensaje enviado. Respuesta del servidor: {Response}", response ?? "OK");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "🔒 ERROR DE AUTENTICACIÓN SMTP. Usuario: {User}", _settings.User);
                throw new InvalidOperationException(
                    $"❌ Autenticación SMTP falló. Usuario: {_settings.User}. " +
                    $"Verifica la contraseña de app de Gmail. Error: {ex.Message}", 
                    ex);
            }
            catch (SslHandshakeException ex)
            {
                _logger.LogError(ex, "🔐 ERROR SSL/TLS al conectar al servidor SMTP");
                throw new InvalidOperationException(
                    $"❌ Error SSL/TLS. Verifica EnableSsl=true y puerto 587. Error: {ex.Message}", 
                    ex);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "🌐 ERROR DE RED al conectar a {Host}:{Port}", _settings.Host, _settings.Port);
                throw new InvalidOperationException(
                    $"❌ Error de red. No se puede conectar a {_settings.Host}:{_settings.Port}. " +
                    $"Verifica firewall o proxy. Error: {ex.Message}", 
                    ex);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "⏱️ TIMEOUT al conectar al servidor SMTP");
                throw new InvalidOperationException(
                    $"❌ Timeout de conexión. El servidor {_settings.Host} no responde. Error: {ex.Message}", 
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR INESPERADO en cliente SMTP. Tipo: {Type}", ex.GetType().Name);
                throw new InvalidOperationException(
                    $"❌ Error inesperado en SMTP: {ex.Message}", 
                    ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    try
                    {
                        await client.DisconnectAsync(true);
                        _logger.LogDebug("🔌 Desconectado del servidor SMTP");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error al desconectar del servidor SMTP (no crítico)");
                    }
                }
            }
        }

        private void ValidateEmailSettings()
        {
            if (_settings == null)
            {
                _logger.LogError("❌ SmtpSettings es NULL");
                throw new InvalidOperationException("❌ Configuración SMTP no encontrada en appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogError("❌ Host SMTP no configurado");
                throw new InvalidOperationException("❌ Host SMTP vacío. Verifica appsettings.json > SmtpSettings > Host");
            }

            if (_settings.Port <= 0 || _settings.Port > 65535)
            {
                _logger.LogError("❌ Puerto SMTP inválido: {Port}", _settings.Port);
                throw new InvalidOperationException($"❌ Puerto SMTP inválido: {_settings.Port}. Usa 587 para Gmail");
            }

            if (string.IsNullOrWhiteSpace(_settings.User))
            {
                _logger.LogError("❌ Usuario SMTP no configurado");
                throw new InvalidOperationException("❌ Usuario SMTP vacío. Verifica appsettings.json > SmtpSettings > User");
            }

            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogError("❌ Contraseña SMTP no configurada");
                throw new InvalidOperationException("❌ Password SMTP vacía. Verifica appsettings.json > SmtpSettings > Password");
            }

            if (string.IsNullOrWhiteSpace(_settings.From))
            {
                _logger.LogError("❌ Dirección 'From' no configurada");
                throw new InvalidOperationException("❌ From vacío. Verifica appsettings.json > SmtpSettings > From");
            }

            _logger.LogDebug("✅ Configuración SMTP validada: Host={Host}, Port={Port}, User={User}", 
                _settings.Host, _settings.Port, _settings.User);
        }

        private void ValidateParameters(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogError("❌ Destinatario vacío");
                throw new ArgumentException("❌ Destinatario no puede estar vacío", nameof(to));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogError("❌ Asunto vacío");
                throw new ArgumentException("❌ Asunto no puede estar vacío", nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("❌ Cuerpo del mensaje vacío");
                throw new ArgumentException("❌ Cuerpo del mensaje no puede estar vacío", nameof(body));
            }

            // Validar formato de email
            try
            {
                var addr = new System.Net.Mail.MailAddress(to);
                if (addr.Address != to)
                {
                    throw new ArgumentException($"❌ Formato de email inválido: {to}", nameof(to));
                }
            }
            catch (FormatException)
            {
                _logger.LogError("❌ Formato de email inválido: {To}", to);
                throw new ArgumentException($"❌ Formato de email inválido: {to}", nameof(to));
            }
        }

        private string GetDetailedErrorMessage(Exception ex)
        {
            return ex switch
            {
                MailKit.Security.AuthenticationException => 
                    "Autenticación falló. Verifica usuario y contraseña de app de Gmail.",
                SslHandshakeException => 
                    "Error SSL/TLS. Verifica EnableSsl=true y puerto 587.",
                SocketException => 
                    $"Error de red. No se puede conectar a {_settings.Host}:{_settings.Port}. Verifica firewall/proxy.",
                TimeoutException => 
                    "Timeout de conexión. El servidor SMTP no responde.",
                FormatException => 
                    "Error en el formato del mensaje o direcciones de email.",
                _ => ex.Message
            };
        }

        #endregion
    }
}
