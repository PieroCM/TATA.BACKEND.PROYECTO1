using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class ConfigSlaService : IConfigSlaService
    {
        private readonly IRepositoryConfigSLA _repo;

        public ConfigSlaService(IRepositoryConfigSLA repo)
        {
            _repo = repo;
        }

        // -------- mapping --------
        private static ConfigSlaDTO ToDto(ConfigSla e) => new(
            e.IdSla, e.CodigoSla, e.Descripcion, e.DiasUmbral,
            e.TipoSolicitud, e.EsActivo, e.CreadoEn, e.ActualizadoEn
        );

        // -------- read --------
        public async Task<IEnumerable<ConfigSlaDTO>> GetAllAsync(bool soloActivos = true)
        {
            var data = await _repo.GetAllAsync();
            if (soloActivos) data = data.Where(x => x.EsActivo);
            return data.Select(ToDto);
        }

        public async Task<ConfigSlaDTO?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e is null ? null : ToDto(e);
        }

        // -------- create --------
        public async Task<int> CreateAsync(int? id, ConfigSlaCreateDTO dto)
        {
            var now = DateTime.UtcNow;

            var entity = new ConfigSla
            {
                IdSla = id ?? 0,                    // si usas Identity, ignora este valor
                CodigoSla = dto.CodigoSla.Trim(),
                Descripcion = dto.Descripcion,
                DiasUmbral = dto.DiasUmbral,
                TipoSolicitud = dto.TipoSolicitud.Trim(),
                EsActivo = dto.EsActivo,
                CreadoEn = now,
                ActualizadoEn = now
            };

            return await _repo.InsertAsync(entity);
        }

        // -------- update --------
        public async Task<bool> UpdateAsync(int id, ConfigSlaUpdateDTO dto)
        {
            var entity = new ConfigSla
            {
                IdSla = id,
                CodigoSla = dto.CodigoSla.Trim(),
                Descripcion = dto.Descripcion,
                DiasUmbral = dto.DiasUmbral,
                TipoSolicitud = dto.TipoSolicitud.Trim(),
                EsActivo = dto.EsActivo,
                ActualizadoEn = DateTime.UtcNow
            };

            return await _repo.UpdateAsync(entity);
        }

        // -------- delete --------
        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        // -------- disable (lógico) --------
        public Task<bool> DisableAsync(int id) => _repo.DeleteLogicAsync(id);
    }
}

