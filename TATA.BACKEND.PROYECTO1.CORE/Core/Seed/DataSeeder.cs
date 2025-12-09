using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // ===============================
            // 1. Seed Permisos (siempre)
            // ===============================
            await SeedPermisosAsync();

            // ===============================
            // 2. Seed Roles y sus Permisos (siempre)
            // ===============================
            await SeedRolesYSusPermisosAsync();

            // ===============================
            // 3. Crear usuario SUPER_ADMIN solo si no existen usuarios
            // ===============================
            await SeedSuperAdminUsuarioAsync();
        }

        /// <summary>
        /// Crea los permisos del sistema si no existen (idempotente).
        /// </summary>
        private async Task SeedPermisosAsync()
        {
            var permisosACrear = new[]
            {
                new { Codigo = "DASHBOARD_EJECUTIVO", Nombre = "Dashboard Ejecutivo" },
                new { Codigo = "ANALISIS_INTERACTIVO", Nombre = "Análisis Interactivo" },
                new { Codigo = "CARGA_DATOS", Nombre = "Carga de Datos" },
                new { Codigo = "GESTION_SOLICITUD", Nombre = "Gestión de Solicitud" },
                new { Codigo = "REPORTE_CUMPLIMIENTO", Nombre = "Reporte de Cumplimiento" },
                new { Codigo = "HISTORIAL_REPORTES", Nombre = "Historial de Reportes" },
                new { Codigo = "GESTION_ALERTAS", Nombre = "Gestión de Alertas" },
                new { Codigo = "CONFIGURAR_EMAIL", Nombre = "Configurar Email" },
                new { Codigo = "LOGS", Nombre = "Logs del Sistema" },
                new { Codigo = "GESTION_USUARIOS", Nombre = "Gestión de Usuarios" },
                new { Codigo = "CONFIGURACIONES", Nombre = "Configuraciones" }
            };

            foreach (var p in permisosACrear)
            {
                var existe = await _context.Permiso.AnyAsync(x => x.Codigo == p.Codigo);
                if (!existe)
                {
                    _context.Permiso.Add(new Permiso
                    {
                        Codigo = p.Codigo,
                        Nombre = p.Nombre,
                        Descripcion = null
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Crea los roles del sistema y asigna sus permisos (idempotente).
        /// </summary>
        private async Task SeedRolesYSusPermisosAsync()
        {
            // 1. Crear roles si no existen
            var rolesACrear = new[]
            {
                new { Codigo = "SUPER_ADMIN", Nombre = "Super Administrador", Descripcion = "Rol con acceso completo al sistema" },
                new { Codigo = "ADMIN", Nombre = "Administrador", Descripcion = "Administrador del sistema" },
                new { Codigo = "ANALISTA", Nombre = "Analista SLA", Descripcion = "Analista de reportes y métricas" },
                new { Codigo = "ESPECIALISTA", Nombre = "Especialista SLA", Descripcion = "Especialista en gestión de solicitudes" }
            };

            foreach (var r in rolesACrear)
            {
                var existe = await _context.RolesSistema.AnyAsync(x => x.Codigo == r.Codigo);
                if (!existe)
                {
                    _context.RolesSistema.Add(new RolesSistema
                    {
                        Codigo = r.Codigo,
                        Nombre = r.Nombre,
                        Descripcion = r.Descripcion,
                        EsActivo = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            // 2. Cargar todos los permisos en un diccionario
            var todosLosPermisos = await _context.Permiso.ToListAsync();
            var permisosPorCodigo = todosLosPermisos.ToDictionary(p => p.Codigo, p => p);

            // 3. Cargar todos los roles
            var todosLosRoles = await _context.RolesSistema
                .Include(r => r.IdPermiso)
                .ToListAsync();

            var rolesPorCodigo = todosLosRoles.ToDictionary(r => r.Codigo, r => r);

            // 4. Definir permisos por rol
            var permisosParaSuperAdmin = new[]
            {
                "DASHBOARD_EJECUTIVO", "ANALISIS_INTERACTIVO", "CARGA_DATOS",
                "GESTION_SOLICITUD", "REPORTE_CUMPLIMIENTO", "HISTORIAL_REPORTES",
                "GESTION_ALERTAS", "CONFIGURAR_EMAIL", "LOGS", "GESTION_USUARIOS", "CONFIGURACIONES"
            };

            var permisosParaAdmin = new[]
            {
                "DASHBOARD_EJECUTIVO", "ANALISIS_INTERACTIVO", "CARGA_DATOS",
                "GESTION_SOLICITUD", "REPORTE_CUMPLIMIENTO", "HISTORIAL_REPORTES",
                "GESTION_ALERTAS", "CONFIGURAR_EMAIL", "GESTION_USUARIOS"
            };

            var permisosParaAnalista = new[]
            {
                "DASHBOARD_EJECUTIVO", "ANALISIS_INTERACTIVO",
                "REPORTE_CUMPLIMIENTO", "HISTORIAL_REPORTES"
            };

            var permisosParaEspecialista = new[]
            {
                "CARGA_DATOS", "GESTION_SOLICITUD", "REPORTE_CUMPLIMIENTO",
                "HISTORIAL_REPORTES", "GESTION_ALERTAS", "CONFIGURAR_EMAIL"
            };

            // 5. Asignar permisos a roles
            AsignarPermisosARol(rolesPorCodigo["SUPER_ADMIN"], permisosParaSuperAdmin, permisosPorCodigo);
            AsignarPermisosARol(rolesPorCodigo["ADMIN"], permisosParaAdmin, permisosPorCodigo);
            AsignarPermisosARol(rolesPorCodigo["ANALISTA"], permisosParaAnalista, permisosPorCodigo);
            AsignarPermisosARol(rolesPorCodigo["ESPECIALISTA"], permisosParaEspecialista, permisosPorCodigo);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Asigna permisos a un rol (evita duplicados).
        /// </summary>
        private void AsignarPermisosARol(
            RolesSistema rol,
            string[] codigosPermisos,
            Dictionary<string, Permiso> permisosPorCodigo)
        {
            foreach (var codigoPermiso in codigosPermisos)
            {
                if (!permisosPorCodigo.ContainsKey(codigoPermiso))
                    continue; // permiso no existe

                var permiso = permisosPorCodigo[codigoPermiso];

                // Verificar si ya existe la relación
                if (!rol.IdPermiso.Any(p => p.IdPermiso == permiso.IdPermiso))
                {
                    rol.IdPermiso.Add(permiso);
                }
            }
        }

        /// <summary>
        /// Crea el usuario SUPER_ADMIN por defecto si no existen usuarios.
        /// </summary>
        private async Task SeedSuperAdminUsuarioAsync()
        {
            // Si ya existe al menos un usuario, no hacemos nada
            if (await _context.Usuario.AnyAsync())
                return;

            // ===============================
            // 1. Datos base del super admin
            // ===============================
            const string adminEmail = "22200150@ue.edu.pe";
            const string adminUsername = "superadmin@sla.local";
            const string adminPasswordPlano = "Admin123!";

            // ===============================
            // 2. Obtener rol SUPER_ADMIN (ya debe existir)
            // ===============================
            var adminRole = await _context.RolesSistema
                .FirstOrDefaultAsync(r => r.Codigo == "SUPER_ADMIN");

            if (adminRole == null)
            {
                throw new InvalidOperationException("El rol SUPER_ADMIN debe existir antes de crear el usuario.");
            }

            // ===============================
            // 3. Crear Personal base para el súper admin
            // ===============================
            var adminPersonal = new Personal
            {
                Nombres = "Super",
                Apellidos = "Admin",
                CorreoCorporativo = adminEmail,
                Documento = "99999999",
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            _context.Personal.Add(adminPersonal);
            await _context.SaveChangesAsync();

            // ===============================
            // 4. Crear Usuario vinculado a ese Personal
            // ===============================
            var adminUsuario = new Usuario
            {
                IdPersonal = adminPersonal.IdPersonal,
                IdRolSistema = adminRole.IdRolSistema,
                Username = adminUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPasswordPlano),
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            _context.Usuario.Add(adminUsuario);
            await _context.SaveChangesAsync();
        }
    }
}
