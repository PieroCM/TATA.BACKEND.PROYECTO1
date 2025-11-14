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

            sb.Append("<h2>Notificación de alerta SLA</h2>");
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

            sb.Append("<p style='margin-top:20px;font-size:12px;color:#666;'>Este es un mensaje automático del sistema de SLA.</p>");

            return sb.ToString();
        }
    }
}
