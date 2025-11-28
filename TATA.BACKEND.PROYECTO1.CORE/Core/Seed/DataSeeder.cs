using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Seed
{
    public class DataSeeder
    {
        private readonly Proyecto1SlaDbContext _context;

        public DataSeeder(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Si ya existe al menos un usuario, no hacemos nada
            if (await _context.Usuario.AnyAsync())
                return;

            // ===============================
            // 0. Datos base del super admin
            // ===============================
            const string adminEmail = "22200150@ue.edu.pe";
            const string adminUsername = "superadmin@sla.local"; // usamos el mismo patrón que SignUp
            const string adminPasswordPlano = "Admin123!";       // demo. Cambiar en PROD

            // ===============================
            // 1. Crear rol SUPER_ADMIN
            // ===============================
            var adminRole = await _context.RolesSistema
                .FirstOrDefaultAsync(r => r.Codigo == "SUPER_ADMIN");

            if (adminRole == null)
            {
                adminRole = new RolesSistema
                {
                    Codigo = "SUPER_ADMIN",
                    Nombre = "Super Administrador",
                    Descripcion = "Rol con acceso completo al sistema",
                    EsActivo = true
                };

                _context.RolesSistema.Add(adminRole);
                await _context.SaveChangesAsync();
            }

            // ===============================
            // 2. Crear Personal base para el súper admin
            // ===============================
            var adminPersonal = new Personal
            {
                Nombres = "Super",
                Apellidos = "Admin",
                CorreoCorporativo = adminEmail,   // 👈 este correo es el que usarás en el login
                Documento = "99999999",
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            _context.Personal.Add(adminPersonal);
            await _context.SaveChangesAsync();

            // ===============================
            // 3. Crear Usuario vinculado a ese Personal
            // ===============================
            var adminUsuario = new Usuario
            {
                IdPersonal = adminPersonal.IdPersonal,
                IdRolSistema = adminRole.IdRolSistema,
                Username = adminUsername,                            // 👈 suele ser el email
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPasswordPlano), // 👈 HASH REAL
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            _context.Usuario.Add(adminUsuario);
            await _context.SaveChangesAsync();
            // ✅ Listo: ya tienes un súper admin por defecto
        }
    }
}
