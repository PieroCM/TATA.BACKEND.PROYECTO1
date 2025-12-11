using System.Text;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public static class EmailTemplates
    {
        public static string BuildAlertaBody(Alerta alerta)
        {
            // alerta.IdSolicitudNavigation puede venir nulo si no se hizo include
            var sol = alerta.IdSolicitudNavigation;
            var sb = new StringBuilder();

            sb.Append("<h2>Notificacion de alerta SLA</h2>");
            sb.Append($"<p><strong>Tipo:</strong> {alerta.TipoAlerta}</p>");
            sb.Append($"<p><strong>Nivel:</strong> {alerta.Nivel}</p>");
            sb.Append($"<p><strong>Mensaje:</strong> {alerta.Mensaje}</p>");
            sb.Append($"<p><strong>Fecha:</strong> {alerta.FechaCreacion:yyyy-MM-dd HH:mm}</p>");

            if (sol != null)
            {
                sb.Append("<hr>");
                sb.Append("<h3>Datos de la solicitud</h3>");
                sb.Append($"<p><strong>ID Solicitud:</strong> {sol.IdSolicitud}</p>");
                sb.Append($"<p><strong>Fecha Solicitud:</strong> {sol.FechaSolicitud:yyyy-MM-dd}</p>");

                if (sol.FechaIngreso.HasValue)
                    sb.Append($"<p><strong>Fecha Ingreso:</strong> {sol.FechaIngreso:yyyy-MM-dd}</p>");

                if (!string.IsNullOrWhiteSpace(sol.ResumenSla))
                    sb.Append($"<p><strong>Resumen SLA:</strong> {sol.ResumenSla}</p>");

                if (!string.IsNullOrWhiteSpace(sol.EstadoCumplimientoSla))
                    sb.Append($"<p><strong>Estado SLA:</strong> {sol.EstadoCumplimientoSla}</p>");

                if (sol.IdPersonalNavigation != null)
                {
                    sb.Append("<h4>Personal</h4>");
                    sb.Append($"<p>{sol.IdPersonalNavigation.Nombres} {sol.IdPersonalNavigation.Apellidos}</p>");
                    sb.Append($"<p>{sol.IdPersonalNavigation.CorreoCorporativo}</p>");
                }
            }

            sb.Append("<p style='margin-top:20px;font-size:12px;color:#666;'>Este es un mensaje automatico del sistema de SLA.</p>");

            return sb.ToString();
        }
        // ===========================
        // TEMPLATE: RECUPERACIÓN DE CONTRASEÑA
        // ===========================
        public static string BuildRecuperacionPasswordBody(string username, string recoveryUrl)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.Append(".header { background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }");
            sb.Append(".content { background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }");
            sb.Append(".button-box { text-align: center; margin: 30px 0; }");
            sb.Append(".btn { display: inline-block; background-color: #007bff; color: white !important; padding: 15px 40px; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold; }");
            sb.Append(".btn:hover { background-color: #0056b3; }");
            sb.Append(".footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }");
            sb.Append(".warning { color: #dc3545; font-weight: bold; margin-top: 15px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107; }");
            sb.Append(".info-box { background-color: #e7f3ff; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>🔑 Recuperación de Contraseña</h1>");
            sb.Append("</div>");

            // Content
            sb.Append("<div class='content'>");
            sb.Append($"<h2>Hola {username},</h2>");
            sb.Append("<p>Hemos recibido una solicitud para restablecer tu contraseña en el <strong>Sistema de Gestión SLA</strong>.</p>");
            sb.Append("<p>Para continuar con el proceso de recuperación, haz clic en el siguiente botón:</p>");

            // Button with recovery link
            sb.Append("<div class='button-box'>");
            sb.Append($"<a href='{recoveryUrl}' class='btn'>Restablecer Contraseña</a>");
            sb.Append("</div>");

            sb.Append("<div class='info-box'>");
            sb.Append("<p><strong>⏰ Este enlace expirará en 1 hora.</strong></p>");
            sb.Append("<p>Por razones de seguridad, este enlace solo puede ser utilizado <strong>una vez</strong>.</p>");
            sb.Append("</div>");

            sb.Append("<p style='font-size: 12px; color: #666; margin-top: 20px;'>Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:</p>");
            sb.Append($"<p style='font-size: 11px; word-break: break-all; color: #007bff;'>{recoveryUrl}</p>");

            // Warning
            sb.Append("<div class='warning'>");
            sb.Append("<p style='margin: 0;'>⚠️ <strong>IMPORTANTE:</strong> Si no solicitaste este cambio, ignora este correo. Tu contraseña permanecerá segura.</p>");
            sb.Append("</div>");

            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p>Este es un mensaje automático del Sistema de Gestión SLA.</p>");
            sb.Append("<p>Por favor, no respondas a este correo.</p>");
            sb.Append("<p style='margin-top:10px;'>© 2024 Sistema de Gestión SLA - Todos los derechos reservados</p>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }
        // ===========================
        // TEMPLATE: ACTIVACIÓN/BIENVENIDA DE CUENTA
        // ===========================
        public static string BuildActivacionBienvenidaBody(string nombres, string apellidos, string username, string activacionUrl)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.Append(".header { background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }");
            sb.Append(".content { background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }");
            sb.Append(".footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }");
            sb.Append(".welcome-icon { color: #007bff; font-size: 48px; text-align: center; margin: 20px 0; }");
            sb.Append(".info-box { background-color: #e7f3ff; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }");
            sb.Append(".button-box { text-align: center; margin: 30px 0; }");
            sb.Append(".btn { display: inline-block; background-color: #007bff; color: white !important; padding: 15px 40px; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold; }");
            sb.Append(".btn:hover { background-color: #0056b3; }");
            sb.Append(".warning-box { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>Bienvenido a SLA Manager</h1>");
            sb.Append("</div>");

            // Content
            sb.Append("<div class='content'>");
            sb.Append("<div class='welcome-icon'>&#128075;</div>"); // 👋 emoji en código HTML
            sb.Append($"<h2>Hola, {nombres} {apellidos}</h2>");
            sb.Append("<p>Tu cuenta para el sistema <strong>SLA Manager</strong> ha sido creada exitosamente por un Administrador.</p>");

            // Info box con username
            sb.Append("<div class='info-box'>");
            sb.Append($"<p style='margin: 0;'><strong>Tu nombre de usuario es:</strong> <code style='background-color: #fff; padding: 5px 10px; border-radius: 3px;'>{username}</code></p>");
            sb.Append("</div>");

            sb.Append("<p>Para activar tu cuenta y establecer tu contraseña por primera vez, haz clic en el siguiente boton.</p>");

            // Button
            sb.Append("<div class='button-box'>");
            sb.Append($"<a href='{activacionUrl}' class='btn'>Activar Mi Cuenta</a>");
            sb.Append("</div>");

            // Warning box
            sb.Append("<div class='warning-box'>");
            sb.Append("<p><strong>Importante:</strong></p>");
            sb.Append("<p style='margin: 0;'>Este enlace <strong>caducara en 24 horas</strong>. Por razones de seguridad, solo puede ser utilizado una vez.</p>");
            sb.Append("</div>");

            sb.Append("<p style='font-size: 12px; color: #666; margin-top: 20px;'>Si el boton no funciona, copia y pega el siguiente enlace en tu navegador:</p>");
            sb.Append($"<p style='font-size: 11px; word-break: break-all; color: #007bff;'>{activacionUrl}</p>");

            sb.Append("<p style='margin-top: 30px;'>Saludos cordiales,</p>");
            sb.Append("<p><strong>El Equipo de SLA Manager</strong></p>");

            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p>Si no solicitaste esta activacion, puedes ignorar este correo.</p>");
            sb.Append("<p>Este es un mensaje automatico del Sistema de Gestion SLA.</p>");
            sb.Append("<p>Por favor, no respondas a este correo.</p>");
            sb.Append("<p style='margin-top:10px;'>© 2024 Sistema de Gestion SLA - Todos los derechos reservados</p>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        // ===========================
        // TEMPLATE: CONFIRMACIÓN DE CAMBIO DE CONTRASEÑA
        // ===========================
        public static string BuildPasswordChangedBody(string username)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.Append(".header { background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }");
            sb.Append(".content { background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }");
            sb.Append(".footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }");
            sb.Append(".success-icon { color: #28a745; font-size: 48px; text-align: center; margin: 20px 0; }");
            sb.Append(".alert-box { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            // Header
            sb.Append("<div class='header'>");
            sb.Append("<h1>Contraseña Actualizada</h1>");
            sb.Append("</div>");

            // Content
            sb.Append("<div class='content'>");
            sb.Append("<div class='success-icon'>&#10003;</div>"); // ✓ checkmark en código HTML
            sb.Append($"<h2>Hola {username},</h2>");
            sb.Append("<p>Te confirmamos que tu contraseña ha sido <strong>actualizada exitosamente</strong> en el Sistema de Gestión SLA.</p>");
            sb.Append("<p>Ya puedes iniciar sesión con tu nueva contraseña.</p>");

            // Alert box
            sb.Append("<div class='alert-box'>");
            sb.Append("<p><strong>Importante:</strong></p>");
            sb.Append("<p>Si <strong>NO realizaste este cambio</strong>, tu cuenta podría estar comprometida. Por favor, contacta inmediatamente al administrador del sistema.</p>");
            sb.Append("</div>");

            sb.Append("<p style='margin-top: 20px;'>Por tu seguridad, te recomendamos:</p>");
            sb.Append("<ul>");
            sb.Append("<li>No compartir tu contraseña con nadie</li>");
            sb.Append("<li>Usar una contraseña única y segura</li>");
            sb.Append("<li>Cambiar tu contraseña periódicamente</li>");
            sb.Append("</ul>");

            sb.Append("</div>");

            // Footer
            sb.Append("<div class='footer'>");
            sb.Append("<p>Este es un mensaje automático del Sistema de Gestión SLA.</p>");
            sb.Append("<p>Por favor, no respondas a este correo.</p>");
            sb.Append($"<p style='margin-top:10px;'>Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.Append("<p style='margin-top:10px;'>© 2024 Sistema de Gestión SLA - Todos los derechos reservados</p>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }
    }
}
