using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var _config = builder.Configuration;
var _cnx = _config.GetConnectionString("DevConnection");

builder
    .Services
    .AddDbContext<Proyecto1SlaDbContext>(options =>
        options.UseSqlServer(_cnx)
    );

// DI de interfaces
builder.Services.AddScoped<IRepositoryConfigSLA, RepositoryConfigSLA>();
builder.Services.AddScoped<IConfigSlaService, ConfigSlaService>();

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
