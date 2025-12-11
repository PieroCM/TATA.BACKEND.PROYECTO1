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
        // TEMPLATE: RECUPERACIÓN DE CONTRASEÑA (SIMPLIFICADO)
        // ===========================
        public static string BuildRecuperacionPasswordBody(string username, string recoveryUrl)
        {
            return $@"
                <h2>Recuperacion de Contrasena - Sistema SLA</h2>
                <p>Hola {username},</p>
                <p>Hemos recibido una solicitud para restablecer tu contrasena en el <strong>Sistema de Gestion SLA</strong>.</p>
                <p>Para continuar con el proceso de recuperacion, haz clic en el siguiente enlace:</p>
                <p><a href='{recoveryUrl}'>Restablecer Contrasena</a></p>
                <p>O copia y pega este enlace en tu navegador:</p>
                <p>{recoveryUrl}</p>
                <p><strong>Este enlace expirara en 1 hora.</strong></p>
                <p>Por razones de seguridad, este enlace solo puede ser utilizado una vez.</p>
                <br/>
                <p><strong>IMPORTANTE:</strong> Si no solicitaste este cambio, ignora este correo. Tu contrasena permanecera segura.</p>
                <hr/>
                <p style='font-size:12px;color:#666;'>Este es un mensaje automatico del Sistema de Gestion SLA.</p>
            ";
        }

        // ===========================
        // TEMPLATE: CONFIRMACIÓN DE CAMBIO DE CONTRASEÑA (SIMPLIFICADO)
        // ===========================
        public static string BuildPasswordChangedBody(string username)
        {
            return $@"
                <h2>Contrasena Actualizada - Sistema SLA</h2>
                <p>Hola {username},</p>
                <p>Te confirmamos que tu contrasena ha sido <strong>actualizada exitosamente</strong> en el Sistema de Gestion SLA.</p>
                <p>Ya puedes iniciar sesion con tu nueva contrasena.</p>
                <br/>
                <p><strong>IMPORTANTE:</strong></p>
                <p>Si NO realizaste este cambio, tu cuenta podria estar comprometida. Por favor, contacta inmediatamente al administrador del sistema.</p>
                <br/>
                <p>Por tu seguridad, te recomendamos:</p>
                <ul>
                    <li>No compartir tu contrasena con nadie</li>
                    <li>Usar una contrasena unica y segura</li>
                    <li>Cambiar tu contrasena periodicamente</li>
                </ul>
                <hr/>
                <p style='font-size:12px;color:#666;'>Este es un mensaje automatico del Sistema de Gestion SLA.</p>
                <p style='font-size:12px;color:#666;'>Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            ";
        }
    }
}
