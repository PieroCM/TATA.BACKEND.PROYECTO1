using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ?? Inyección de dependencias
builder.Services.AddScoped<IRepositoryRolPermiso, RepositoryRolPermiso>();
builder.Services.AddScoped<IRolPermisoService, RolPermisoService>();

// ?? Configurar AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

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
