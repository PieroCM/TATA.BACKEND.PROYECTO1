using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Entities
{
    public partial class EmailConfig
    {
        public int Id { get; set; }

        public string DestinatarioResumen { get; set; } = null!;

        public bool EnvioInmediato { get; set; }

        public bool ResumenDiario { get; set; }

        public TimeSpan HoraResumen { get; set; }

        public DateTime? CreadoEn { get; set; }

        public DateTime? ActualizadoEn { get; set; }
    }
}
