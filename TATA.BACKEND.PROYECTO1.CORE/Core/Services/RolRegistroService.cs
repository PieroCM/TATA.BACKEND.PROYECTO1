using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class RolRegistroService : IRolRegistroService
    {
        private readonly IRolRegistroRepository _repo;

        public RolRegistroService(IRolRegistroRepository repo)
        {
            _repo = repo;
        }

        private static RolRegistroDTO ToDto(RolRegistro e) => new(
            e.IdRolRegistro,
            e.NombreRol,
            e.BloqueTech,
            e.Descripcion,
            e.EsActivo
        );

        public async Task<IEnumerable<RolRegistroDTO>> GetAllAsync(bool soloActivos = true)
        {
            var data = await _repo.GetAllAsync();
            if (soloActivos) data = data.Where(x => x.EsActivo);
            return data.Select(ToDto);
        }

        public async Task<RolRegistroDTO?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e is null ? null : ToDto(e);
        }

        public Task<bool> ExistsByNombreAsync(string nombreRol) =>
            _repo.ExistsByNombreAsync(nombreRol);

        public async Task<int> CreateAsync(int? id, RolRegistroCreateDTO dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.NombreRol))
                throw new ArgumentException("NombreRol es requerido.");

            var nombre = dto.NombreRol.Trim();

            if (await _repo.ExistsByNombreAsync(nombre))
                throw new DuplicateNameException("El NombreRol ya existe.");

            var entity = new RolRegistro
            {
                IdRolRegistro = id ?? 0,
                NombreRol = nombre,
                BloqueTech = dto.BloqueTech,
                Descripcion = dto.Descripcion,
                EsActivo = true
            };

            return await _repo.InsertAsync(entity); // igual atrapamos race condition en repo
        }

        public async Task<bool> UpdateAsync(int id, RolRegistroUpdateDTO dto)
        {
            if (dto is null) return false;
            if (string.IsNullOrWhiteSpace(dto.NombreRol)) return false;

            var entity = new RolRegistro
            {
                IdRolRegistro = id,
                NombreRol = dto.NombreRol.Trim(),
                BloqueTech = dto.BloqueTech,
                Descripcion = dto.Descripcion,
                EsActivo = dto.EsActivo
            };
            return await _repo.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
        public Task<bool> DisableAsync(int id) => _repo.DeleteLogicAsync(id);
    }
}
