using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.Tests.Integration;

/// <summary>
/// Levanta la API completa en memoria (pipeline HTTP, autenticación JWT, controladores y DataSeeder).
/// Cambia solo dos cosas respecto de producción:
/// - SQL Server por una base de datos en memoria, nueva para cada fábrica.
/// - Quita los workers de segundo plano (correos, alertas, recálculo diario) para que no corran durante las pruebas.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "22200150@ue.edu.pe";
    public const string AdminPassword = "Admin123!";

    private readonly string _nombreBd = "sla-pruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Clave propia para las pruebas: no dependen de los secretos de appsettings.json
        builder.UseSetting("JWTSettings:SecretKey", "clave-de-pruebas-de-integracion-con-mas-de-32-caracteres");
        builder.UseSetting("JWTSettings:Issuer", "Proyecto1SLAAuthServer");
        builder.UseSetting("JWTSettings:Audience", "Proyecto1SLAUsers");
        builder.UseSetting("ConnectionStrings:DevConnection", "Server=no-se-usa;Database=no-se-usa");
        // Contraseña con la que el DataSeeder crea el administrador inicial
        builder.UseSetting("Seed:AdminPassword", AdminPassword);

        builder.ConfigureTestServices(services =>
        {
            // Base de datos en memoria en lugar de SQL Server
            services.RemoveAll<DbContextOptions<Proyecto1SlaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<Proyecto1SlaDbContext>>();
            services.AddDbContext<Proyecto1SlaDbContext>(options => options
                .UseInMemoryDatabase(_nombreBd)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            // Sin workers: solo se quitan los del proyecto, no los servicios internos de ASP.NET Core
            var workers = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.Namespace?.EndsWith(".Workers") == true)
                .ToList();
            foreach (var worker in workers)
            {
                services.Remove(worker);
            }
        });
    }
}
