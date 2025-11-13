using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repositories;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Shared;

var builder = WebApplication.CreateBuilder(args);


var _configuration = builder.Configuration;
var connectionString = _configuration.GetConnectionString("DevConnection");




builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IPersonalRepository, PersonalRepository>();


builder.Services.AddTransient<IUsuarioService, UsuarioService>();
builder.Services.AddTransient<IPersonalService, PersonalService>();

builder.Services.AddSharedInfrastructure(_configuration);


builder.Services.AddDbContext<Proyecto1SlaDbContext>(
    options => options.UseSqlServer(connectionString)
);


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


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); 
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();


app.UseCors("AllowAll");


app.MapControllers();


app.Run();
