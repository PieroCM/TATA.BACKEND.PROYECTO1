using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;

using TATA.BACKEND.PROYECTO1.CORE.Core.Settings;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var _config = builder.Configuration;
var _cnx = _config.GetConnectionString("DevConnection");

builder
    .Services
    .AddDbContext<Proyecto1SlaDbContext>(options =>
        options.UseSqlServer(_cnx)
    );


builder.Services.AddTransient<IRolesSistemaRepository, RolesSistemaRepository>();
builder.Services.AddTransient<IPermisoRepository, PermisoRepository>();
builder.Services.AddTransient<IRolesSistemaService, RolesSistemaService>();
builder.Services.AddTransient<IPermisoService, PermisoService>();

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
var _configuration = builder.Configuration;
var connectionString = _configuration.GetConnectionString("DevConnection");

builder.Services.AddDbContext<Proyecto1SlaDbContext>(options =>
    options.UseSqlServer(connectionString));

// CORREO Y ALERTAS
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IRepositoryAlerta, RepositoryAlerta>();
builder.Services.AddTransient<IAlertaService, AlertaService>();

// SOLICITUD
builder.Services.AddTransient<IRepositorySolicitud, RepositorySolicitud>();
builder.Services.AddTransient<ISolicitudService, SolicitudService>();

// ConfigSLA
builder.Services.AddTransient<IRepositoryConfigSLA, RepositoryConfigSLA>();
builder.Services.AddTransient<IConfigSlaService, ConfigSlaService>();

// RolRegistro
builder.Services.AddTransient<IRepositoryRolRegistro, RepositoryRolRegistro>();
builder.Services.AddTransient<IRolRegistroService, RolRegistroService>();

// RolPermiso
builder.Services.AddTransient<IRepositoryRolPermiso, RepositoryRolPermiso>();
builder.Services.AddTransient<IRolPermisoService, RolPermisoService>();

// LogSistema
builder.Services.AddTransient<IRepositoryLogSistema, RepositoryLogSistema>();
builder.Services.AddTransient<ILogSistemaService, LogSistemaService>();

// Usuario y Personal
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IPersonalRepository, PersonalRepository>();
builder.Services.AddTransient<IUsuarioService, UsuarioService>();
builder.Services.AddTransient<IPersonalService, PersonalService>();

// Shared Infrastructure (JWT, etc.)
builder.Services.AddSharedInfrastructure(_configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();
app.Run();
