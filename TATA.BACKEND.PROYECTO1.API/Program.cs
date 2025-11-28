using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;
using log4net; // Necesario para LogManager
using log4net.Config; // Necesario para XmlConfigurator
using System.Reflection; // Necesario para Assembly

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

// CORREO Y ALERTAS
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IAlertaRepository, AlertaRepository>();
builder.Services.AddTransient<IAlertaService, AlertaService>();

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
