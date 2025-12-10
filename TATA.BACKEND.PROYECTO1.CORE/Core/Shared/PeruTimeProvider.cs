using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Shared
{
    /// <summary>
    /// Proveedor de fecha/hora para la zona horaria de Perú (UTC-5).
    /// Proporciona una abstracción centralizada para obtener la hora actual en Perú,
    /// evitando inconsistencias en el manejo de zonas horarias a lo largo del sistema.
    /// </summary>
    public static class PeruTimeProvider
    {
        /// <summary>
        /// Zona horaria de Perú: "SA Pacific Standard Time" (UTC-5, sin horario de verano)
        /// Equivalente a America/Lima en IANA
        /// </summary>
        private static readonly TimeZoneInfo PeruTimeZone = 
            TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        /// <summary>
        /// Obtiene la fecha y hora actual en la zona horaria de Perú.
        /// </summary>
        /// <remarks>
        /// Este valor se usa como referencia para:
        /// - Cálculo de días SLA
        /// - Determinación de vencimientos
        /// - Ejecución del worker diario
        /// - Creación/actualización de registros de Solicitud
        /// </remarks>
        public static DateTime NowPeru => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone);

        /// <summary>
        /// Obtiene solo la fecha actual en Perú (sin componente de hora).
        /// </summary>
        public static DateTime TodayPeru => NowPeru.Date;

        /// <summary>
        /// Convierte una fecha UTC a la zona horaria de Perú.
        /// </summary>
        /// <param name="utcDateTime">Fecha en UTC</param>
        /// <returns>Fecha convertida a hora de Perú</returns>
        public static DateTime ToPeruTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La fecha debe estar en UTC", nameof(utcDateTime));
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PeruTimeZone);
        }

        /// <summary>
        /// Convierte una fecha de Perú a UTC.
        /// </summary>
        /// <param name="peruDateTime">Fecha en hora de Perú</param>
        /// <returns>Fecha convertida a UTC</returns>
        public static DateTime ToUtcFromPeru(DateTime peruDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(peruDateTime, PeruTimeZone);
        }

        /// <summary>
        /// Obtiene el nombre de la zona horaria utilizada.
        /// </summary>
        public static string TimeZoneName => PeruTimeZone.DisplayName;
    }
}
