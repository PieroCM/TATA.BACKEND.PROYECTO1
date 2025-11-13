using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(LogSistemaProfile));
// Add services to the container.
var _config = builder.Configuration;
var _cnx = _config.GetConnectionString("DevConnection");

builder
    .Services
    .AddDbContext<Proyecto1SlaDbContext>(options =>
        options.UseSqlServer(_cnx)
    );

// DI de interfaces (nombres completamente calificados para evitar conflictos)
builder.Services.AddScoped<
    TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IRepositoryConfigSLA,
    TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository.RepositoryConfigSLA>();

builder.Services.AddScoped<
    TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IConfigSlaService,
    TATA.BACKEND.PROYECTO1.CORE.Core.Services.ConfigSlaService>();

// RolRegistro DI
builder.Services.AddScoped<
    TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IRepositoryRolRegistro,
    TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository.RepositoryRolRegistro>();

builder.Services.AddScoped<
    TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IRolRegistroService,
    TATA.BACKEND.PROYECTO1.CORE.Core.Services.RolRegistroService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ?? Inyección de dependencias
builder.Services.AddScoped<IRepositoryRolPermiso, RepositoryRolPermiso>();
builder.Services.AddScoped<IRolPermisoService, RolPermisoService>();

// ?? Configurar AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

//Dependencias de Log_sistemas
builder.Services.AddScoped<IRepositoryLogSistema, RepositoryLogSistema>();
builder.Services.AddScoped<ILogSistemaService, LogSistemaService>();

// ?? Base de datos
builder.Services.AddDbContext<Proyecto1SlaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection")));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
