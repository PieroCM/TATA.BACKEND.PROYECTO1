using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    public class RepositoryRolRegistro : IRepositoryRolRegistro
    {
        private readonly Proyecto1SlaDbContext _context;

        public RepositoryRolRegistro(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        public IEnumerable<RolRegistro> GetAll() => _context.RolRegistro.AsNoTracking().ToList();

        public async Task<IEnumerable<RolRegistro>> GetAllAsync() => await _context.RolRegistro.AsNoTracking().ToListAsync();

        public async Task<RolRegistro?> GetByIdAsync(int id) => await _context.RolRegistro.AsNoTracking().FirstOrDefaultAsync(r => r.IdRolRegistro == id);
        public Task<bool> ExistsByNombreAsync(string nombreRol)
        {
            var n = nombreRol.Trim();
            return _context.RolRegistro.AsNoTracking()
                .AnyAsync(r => r.NombreRol == n);
        }

        public async Task<int> InsertAsync(RolRegistro entity)
        {
            await _context.RolRegistro.AddAsync(entity);
            try
            {
                await _context.SaveChangesAsync();
                return entity.IdRolRegistro;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException sql &&
                (sql.Number == 2627 || sql.Number == 2601) // UNIQUE/PK
            )
            {
                // Lo subimos como DuplicateNameException para que el controller responda 409
                throw new DuplicateNameException("El NombreRol ya existe.", ex);
            }
        }


        public async Task<bool> UpdateAsync(RolRegistro entity)
        {
            var current = await _context.RolRegistro.FindAsync(entity.IdRolRegistro);
            if (current is null) return false;
            current.NombreRol = entity.NombreRol;
            current.BloqueTech = entity.BloqueTech;
            current.Descripcion = entity.Descripcion;
            current.EsActivo = entity.EsActivo;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _context.RolRegistro.FindAsync(id);
            if (current is null) return false;
            _context.RolRegistro.Remove(current);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLogicAsync(int id)
        {
            var current = await _context.RolRegistro.FindAsync(id);
            if (current is null) return false;
            current.EsActivo = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
