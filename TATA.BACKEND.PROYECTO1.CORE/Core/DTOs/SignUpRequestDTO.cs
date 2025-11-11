namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    public class SignUpRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int IdRolSistema { get; set; }
    }
}
