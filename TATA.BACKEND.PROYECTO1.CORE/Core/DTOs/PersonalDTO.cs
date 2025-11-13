namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class PersonalCreateDTO
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string CorreoCorporativo { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public int IdUsuario { get; set; }
    }

    public class PersonalUpdateDTO
    {
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Documento { get; set; }
        public string? CorreoCorporativo { get; set; }
        public string? Estado { get; set; }
    }

    public class PersonalResponseDTO
    {
        public int IdPersonal { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string CorreoCorporativo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string UsuarioCorreo { get; set; } = string.Empty;
    }
}