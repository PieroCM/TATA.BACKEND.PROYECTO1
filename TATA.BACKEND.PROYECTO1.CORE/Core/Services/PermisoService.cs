using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class PermisoService : IPermisoService
    {
        private readonly IPermisoRepository _repo;

        public PermisoService(IPermisoRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<PermisoResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(p => new PermisoResponseDTO {
                IdPermiso = p.IdPermiso,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion
            }).ToList();
        }

        public async Task<PermisoResponseDTO?> GetById(int id)
        {
            var p = await _repo.GetById(id);
            if (p == null) return null;
            return new PermisoResponseDTO {
                IdPermiso = p.IdPermiso,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion
            };
        }

        public async Task<PermisoResponseDTO> Create(PermisoCreateDTO dto)
        {
            var entity = new Permiso {
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion
            };
            await _repo.Add(entity);
            return new PermisoResponseDTO {
                IdPermiso = entity.IdPermiso,
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion
            };
        }

        public async Task<bool> Update(int id, PermisoUpdateDTO dto)
        {
            var existing = await _repo.GetById(id);
            if (existing == null) return false;
            existing.Codigo = dto.Codigo;
            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
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
