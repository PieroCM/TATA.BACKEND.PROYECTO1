namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class UsuarioResponseDTO
    {
        public int IdUsuario { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int IdRolSistema { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? UltimoLogin { get; set; }
        public DateTime? CreadoEn { get; set; }
        public DateTime? ActualizadoEn { get; set; }
    }

}
