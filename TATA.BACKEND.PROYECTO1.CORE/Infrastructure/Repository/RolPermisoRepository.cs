using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;


namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{

    public class RolPermisoRepository : IRolPermisoRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public RolPermisoRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. Obtener todos los registros (GET ALL)
        // ============================================================
        public async Task<IEnumerable<RolPermisoEntity>> GetAllAsync()
        {
            var query = "SELECT id_rol_sistema AS IdRolSistema, id_permiso AS IdPermiso FROM rol_permiso";
            return await _context.Database.SqlQueryRaw<RolPermisoEntity>(query).ToListAsync();
        }

        // ============================================================
        // 🔹 2. Obtener por Ids (GET BY IDS)
        // ============================================================
        public async Task<RolPermisoEntity?> GetByIdsAsync(int idRolSistema, int idPermiso)
        {
            var query = @"
                SELECT id_rol_sistema AS IdRolSistema, id_permiso AS IdPermiso
                FROM rol_permiso
                WHERE id_rol_sistema = {0} AND id_permiso = {1}";

            return await _context.Database.SqlQueryRaw<RolPermisoEntity>(query, idRolSistema, idPermiso)
                                          .FirstOrDefaultAsync();
        }

        // ============================================================
        // 🔹 3. Agregar un nuevo registro (CREATE)
        // ============================================================
        public async Task<bool> AddAsync(RolPermisoEntity entity)
        {
            var query = @"
                INSERT INTO rol_permiso (id_rol_sistema, id_permiso)
                VALUES ({0}, {1})";

            var rows = await _context.Database.ExecuteSqlRawAsync(query, entity.IdRolSistema, entity.IdPermiso);
            return rows > 0;
        }

        // ============================================================
        // 🔹 4. Actualizar un registro (UPDATE)
        // ============================================================
        public async Task<bool> UpdateAsync(int idRolSistema, int idPermiso, RolPermisoEntity entity)
        {
            // 1️⃣ Validar si el registro original existe
            var exists = await GetByIdsAsync(idRolSistema, idPermiso);
            if (exists == null)
                return false; // No se encontró el registro a actualizar

            // 2️⃣ Validar si la nueva combinación YA existe (y no es la misma)
            if (entity.IdPermiso != idPermiso)
            {
                var duplicate = await GetByIdsAsync(idRolSistema, entity.IdPermiso);
                if (duplicate != null)
                {
                    // ❌ Ya existe un registro con esta PK → No se puede actualizar
                    // Devuelve false y que el Service/Controller manejen el mensaje
                    return false;
                }
            }

            // 3️⃣ Realizar el UPDATE
            var query = @"
        UPDATE rol_permiso
        SET id_permiso = {2}
        WHERE id_rol_sistema = {0} AND id_permiso = {1}";

            var rows = await _context.Database.ExecuteSqlRawAsync(
                query,
                idRolSistema,       // {0}
                idPermiso,          // {1}
                entity.IdPermiso    // {2}
            );

            return rows > 0;
        }

        // ============================================================
        // 🔹 5. Eliminar un registro (DELETE)
        // ============================================================
        public async Task<bool> RemoveAsync(int idRolSistema, int idPermiso)
        {
            var query = @"
                DELETE FROM rol_permiso
                WHERE id_rol_sistema = {0} AND id_permiso = {1}";

            var rows = await _context.Database.ExecuteSqlRawAsync(query, idRolSistema, idPermiso);
            return rows > 0;
        }

        // ============================================================
        // 🔹 6. Obtener con JOIN (roles y permisos con nombres)
        // ============================================================
        public async Task<IEnumerable<RolPermisoDTO>> GetAllWithNamesAsync()
        {
            var query = @"
            SELECT 
                rs.id_rol_sistema AS IdRolSistema,
                rs.nombre AS NombreRol,
                p.id_permiso AS IdPermiso,
                p.nombre AS NombrePermiso
            FROM rol_permiso rp
            INNER JOIN roles_sistema rs ON rp.id_rol_sistema = rs.id_rol_sistema
            INNER JOIN permiso p ON rp.id_permiso = p.id_permiso";

            return await _context.Database.SqlQueryRaw<RolPermisoDTO>(query).ToListAsync();
        }

    }
}