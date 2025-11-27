namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // DTO para solicitar recuperación de contraseña
    public class SolicitarRecuperacionDTO
    {
        public string Username { get; set; } = null!; // ?? Ahora usa Username en lugar de Email
    }

    // DTO para cambiar la contraseña con el token
    public class RestablecerPasswordDTO
    {
        public string Username { get; set; } = null!; // ?? Ahora usa Username en lugar de Email
        public string Token { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }
}
