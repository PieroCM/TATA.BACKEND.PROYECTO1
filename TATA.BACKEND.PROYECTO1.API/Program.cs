using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;

using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var _configuration = builder.Configuration;
var connectionString = _configuration.GetConnectionString("DevConnection");

builder.Services.AddDbContext<Proyecto1SlaDbContext>(options =>
    options.UseSqlServer(connectionString));


//SOLICITUD
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


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
