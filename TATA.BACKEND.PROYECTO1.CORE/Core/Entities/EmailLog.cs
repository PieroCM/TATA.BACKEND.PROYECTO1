using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
    public partial class EmailLog
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Tipo { get; set; } = null!; // "INMEDIATO", "RESUMEN", "BROADCAST"

        public string Destinatarios { get; set; } = null!;

        public string Estado { get; set; } = null!; // "OK", "ERROR"

        public string? ErrorDetalle { get; set; }
    }
}
