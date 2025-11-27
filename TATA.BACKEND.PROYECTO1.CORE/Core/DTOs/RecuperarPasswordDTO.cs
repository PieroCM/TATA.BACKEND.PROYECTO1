namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs
{
    // DTO para solicitar recuperación de contraseña
    public class SolicitarRecuperacionDTO
    {
        public string Email { get; set; } = null!; // ?? CAMBIO: Email en lugar de Username
    }

    // DTO para cambiar la contraseña con el token
    public class RestablecerPasswordDTO
    {
        public string Email { get; set; } = null!; // ?? CAMBIO: Email en lugar de Username
        public string Token { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }
}
