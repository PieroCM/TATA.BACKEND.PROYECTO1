using log4net; // Necesario para LogManager
using log4net.Config; // Necesario para XmlConfigurator
using Microsoft.EntityFrameworkCore;
using System.Reflection; // Necesario para Assembly
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Seed;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
using TATA.BACKEND.PROYECTO1.CORE.Core.Workers;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var _configuration = builder.Configuration;
var connectionString = _configuration.GetConnectionString("DevConnection");



// =====================================================
// 1) Cargar archivo log4net.config (FORMA CORRECTA .NET 9)
// =====================================================
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));


// =====================================================
// 2) Providers de Logging de .NET 9 (NO usar AddLog4Net)
// =====================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // Para ver logs en consola
builder.Logging.AddDebug();   // Para ver logs en VS Debug Output


builder.Services.AddDbContext<Proyecto1SlaDbContext>(options =>
    options.UseSqlServer(connectionString));

// SEEEDERA USUARIO PRO
builder.Services.AddScoped<DataSeeder>();


// CORREO Y ALERTAS
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IAlertaRepository, AlertaRepository>();
builder.Services.AddTransient<IAlertaService, AlertaService>();

// EMAIL AUTOMATION (NUEVOS SERVICIOS)
builder.Services.AddTransient<IEmailAutomationService, EmailAutomationService>();
builder.Services.AddTransient<IEmailConfigService, EmailConfigService>();

// SOLICITUD
builder.Services.AddTransient<ISolicitudRepository, SolicitudRepository>();
builder.Services.AddTransient<ISolicitudService, SolicitudService>();

// ConfigSLA
builder.Services.AddTransient<IConfigSLARepository, ConfigSLARepository>();
builder.Services.AddTransient<IConfigSlaService, ConfigSlaService>();

// RolRegistro
builder.Services.AddTransient<IRolRegistroRepository, RolRegistroRepository>();
builder.Services.AddTransient<IRolRegistroService, RolRegistroService>();

// RolPermiso
builder.Services.AddTransient<IRolPermisoRepository, RolPermisoRepository>();
builder.Services.AddTransient<IRolPermisoService, RolPermisoService>();

// LogSistema
builder.Services.AddTransient<ILogSistemaRepository, LogSistemaRepository>();
builder.Services.AddTransient<ILogSistemaService, LogSistemaService>();

// Personal
builder.Services.AddTransient<IPersonalRepository, PersonalRepository>();
builder.Services.AddTransient<IPersonalService, PersonalService>();

// Usuario
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IUsuarioService, UsuarioService>();

// Roles Sistema y Permisos
builder.Services.AddTransient<IRolesSistemaRepository, RolesSistemaRepository>();
builder.Services.AddTransient<IPermisoRepository, PermisoRepository>();
builder.Services.AddTransient<IRolesSistemaService, RolesSistemaService>();
builder.Services.AddTransient<IPermisoService, PermisoService>();

//Reporte y ReporteDetalle
builder.Services.AddTransient<IReporteRepository, ReporteRepository>();
builder.Services.AddTransient<IReporteService, ReporteService>();
builder.Services.AddTransient<IReporteDetalleRepository, ReporteDetalleRepository>();
builder.Services.AddTransient<IReporteDetalleService, ReporteDetalleService>();

//Subida volumen
builder.Services.AddTransient<ISubidaVolumenServices, SubidaVolumenServices>();

// BACKGROUND WORKER - Resumen diario autom�tico
builder.Services.AddHostedService<DailySummaryWorker>();

// Shared Infrastructure (JWT, etc.)
builder.Services.AddSharedInfrastructure(_configuration);
//logService
builder.Services.AddTransient<ILogService, LogService>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowQuasarApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:9000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// =====================================================
// 3) Migración automática y Seeder inicial
// =====================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Proyecto1SlaDbContext>();
        await context.Database.MigrateAsync();

        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o ejecutar el seeder");
        throw;
    }
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowQuasarApp");


app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
