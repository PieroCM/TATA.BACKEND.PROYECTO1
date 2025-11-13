using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class RolPermisoService : IRolPermisoService
    {
        private readonly IRepositoryRolPermiso _repository;

        public RolPermisoService(IRepositoryRolPermiso repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RolPermisoEntity>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<IEnumerable<object>> GetAllWithNamesAsync() => await _repository.GetAllWithNamesAsync();

        public async Task<RolPermisoEntity?> GetByIdsAsync(int idRolSistema, int idPermiso)
            => await _repository.GetByIdsAsync(idRolSistema, idPermiso);

        public async Task<bool> AddAsync(RolPermisoEntity entity)
            => await _repository.AddAsync(entity);

        public async Task<bool> UpdateAsync(int idRolSistema, int idPermiso, RolPermisoEntity entity)
            => await _repository.UpdateAsync(idRolSistema, idPermiso, entity);

        public async Task<bool> RemoveAsync(int idRolSistema, int idPermiso)
            => await _repository.RemoveAsync(idRolSistema, idPermiso);
    }


}
