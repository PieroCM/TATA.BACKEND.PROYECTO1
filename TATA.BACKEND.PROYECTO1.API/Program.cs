using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repositories;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------
// ⚙️ CONFIGURACIÓN BASE
// ----------------------------------------------
var _config = builder.Configuration;
var connectionString = _config.GetConnectionString("DevConnection");

// ----------------------------------------------
// 🧩 INYECCIÓN DE DEPENDENCIAS
// ----------------------------------------------

// Repositorios
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();

// Servicios
builder.Services.AddTransient<IUsuarioService, UsuarioService>();

// JWT / Autenticación
builder.Services.AddSharedInfrastructure(_config);

// Base de datos
builder.Services.AddDbContext<Proyecto1SlaDbContext>(
    options => options.UseSqlServer(connectionString)
);


builder.Services.AddScoped<IPersonalRepository, PersonalRepository>();
builder.Services.AddScoped<IPersonalService, PersonalService>();





// ----------------------------------------------
// 🌐 CONFIGURACIÓN CORS
// ----------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ----------------------------------------------
// 🚀 CONTROLADORES
// ----------------------------------------------
builder.Services.AddControllers();

// ----------------------------------------------
// 🧱 CONSTRUCCIÓN DEL APP
// ----------------------------------------------
var app = builder.Build();

// ----------------------------------------------
// 🧭 PIPELINE DEL APP
// ----------------------------------------------
app.UseHttpsRedirection();

// Primero autenticación, luego autorización
app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
