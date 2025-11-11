using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();  // <-- NECESARIO
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register DbContext and repositories/services
builder.Services.AddDbContext<Proyecto1SlaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection")));

builder.Services.AddScoped<TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IRolesSistemaRepository, RolesSistemaRepository>();
builder.Services.AddScoped<TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces.IPermisoRepository, PermisoRepository>();

builder.Services.AddScoped<TATA.BACKEND.PROYECTO1.CORE.Core.Services.IRolesSistemaService, TATA.BACKEND.PROYECTO1.CORE.Core.Services.RolesSistemaService>();
builder.Services.AddScoped<TATA.BACKEND.PROYECTO1.CORE.Core.Services.IPermisoService, TATA.BACKEND.PROYECTO1.CORE.Core.Services.PermisoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Keep using HTTP (no HTTPS redirection)

app.UseAuthorization();

app.MapControllers();

app.Run();
