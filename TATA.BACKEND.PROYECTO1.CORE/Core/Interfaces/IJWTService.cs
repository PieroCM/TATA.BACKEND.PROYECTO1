using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    public interface IJWTService
    {
        JWTSettings _settings { get; }
        string GenerateJWToken(Usuario usuario);
    }
}
