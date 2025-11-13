using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class RolesSistemaService : IRolesSistemaService
    {
        private readonly IRolesSistemaRepository _repo;

        public RolesSistemaService(IRolesSistemaRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<RolesSistemaResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(r => new RolesSistemaResponseDTO {
                IdRolSistema = r.IdRolSistema,
                Codigo = r.Codigo,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                EsActivo = r.EsActivo
            }).ToList();
        }

        public async Task<RolesSistemaResponseDTO?> GetById(int id)
        {
            var r = await _repo.GetById(id);
            if (r == null) return null;
            return new RolesSistemaResponseDTO {
                IdRolSistema = r.IdRolSistema,
                Codigo = r.Codigo,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                EsActivo = r.EsActivo
            };
        }

        public async Task<RolesSistemaResponseDTO> Create(RolesSistemaCreateDTO dto)
        {
            var entity = new RolesSistema {
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion
            };
            await _repo.Add(entity);
            return new RolesSistemaResponseDTO {
                IdRolSistema = entity.IdRolSistema,
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                EsActivo = entity.EsActivo
            };
        }

        public async Task<bool> Update(int id, RolesSistemaUpdateDTO dto)
        {
            var existing = await _repo.GetById(id);
            if (existing == null) return false;
            existing.Codigo = dto.Codigo;
            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
            existing.EsActivo = dto.EsActivo;
            await _repo.Update(existing);
            return true;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _repo.GetById(id);
            if (existing == null) return false;
            await _repo.Delete(id);
            return true;
        }
    }
}
