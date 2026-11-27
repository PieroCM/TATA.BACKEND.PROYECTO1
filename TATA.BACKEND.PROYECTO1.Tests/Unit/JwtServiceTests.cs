using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;

namespace TATA.BACKEND.PROYECTO1.Tests.Unit;

/// <summary>
/// Pruebas de JWTService: contenido del token, firma y vencimiento.
/// </summary>
public class JwtServiceTests
{
    private static readonly JWTSettings Settings = new()
    {
        SecretKey = "clave-de-pruebas-unitarias-con-mas-de-32-caracteres",
        Issuer = "Proyecto1SLAAuthServer",
        Audience = "Proyecto1SLAUsers",
        DurationInMinutes = 120
    };

    private static readonly Usuario Usuario = new()
    {
        IdUsuario = 7,
        Username = "analista@sla.local",
        IdRolSistema = 3,
        IdPersonal = 15,
        PersonalNavigation = new Personal { IdPersonal = 15, CorreoCorporativo = "analista@empresa.pe", Nombres = "Ana", Apellidos = "Ríos" }
    };

    private static string GenerarToken() => new JWTService(Options.Create(Settings)).GenerateJWToken(Usuario);

    private static ClaimsPrincipal Validar(string token, string secretKey) =>
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = Settings.Issuer,
            ValidateAudience = true,
            ValidAudience = Settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

    [Fact]
    public void Token_IncluyeLosClaimsDelUsuario()
    {
        var principal = Validar(GenerarToken(), Settings.SecretKey);

        Assert.Equal("7", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("analista@sla.local", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("analista@empresa.pe", principal.FindFirstValue(ClaimTypes.Email)); // correo del Personal vinculado
        Assert.Equal("3", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("15", principal.FindFirstValue("IdPersonal"));
    }

    [Fact]
    public void Token_FirmadoConOtraClave_EsRechazado()
    {
        var token = GenerarToken();

        Assert.Equal(SecurityAlgorithms.HmacSha256, new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Alg);
        Assert.ThrowsAny<SecurityTokenException>(() =>
            Validar(token, "otra-clave-distinta-tambien-de-mas-de-32-caracteres"));
    }

    [Fact]
    public void Token_VenceSegunLaDuracionConfigurada()
    {
        var antes = DateTime.UtcNow;
        var token = new JwtSecurityTokenHandler().ReadJwtToken(GenerarToken());

        var esperado = antes.AddMinutes(Settings.DurationInMinutes);
        Assert.InRange(token.ValidTo, esperado.AddSeconds(-5), esperado.AddSeconds(5));
    }
}
