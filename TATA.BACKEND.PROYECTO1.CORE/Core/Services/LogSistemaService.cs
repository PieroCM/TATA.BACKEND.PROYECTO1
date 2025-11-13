using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class LogSistemaService : ILogSistemaService
    {
        private readonly IRepositoryLogSistema _repository;

        public LogSistemaService(IRepositoryLogSistema repository)
        {
            _repository = repository;
        }

        private static LogSistemaDTO ToDto(LogSistema entity) => new()
        {
            IdLog = entity.IdLog,
            FechaHora = entity.FechaHora,
            Nivel = entity.Nivel,
            Mensaje = entity.Mensaje,
            Detalles = entity.Detalles,
            IdUsuario = entity.IdUsuario
        };

        private static LogSistema ToEntity(LogSistemaCreateDTO dto) => new()
        {
            FechaHora = DateTime.UtcNow,
            Nivel = dto.Nivel,
            Mensaje = dto.Mensaje,
            Detalles = dto.Detalles,
            IdUsuario = dto.IdUsuario
        };

        public async Task<IEnumerable<LogSistemaDTO>> GetAllAsync()
        {
            var logs = await _repository.GetAllAsync();
            return logs.Select(ToDto);
        }

        public async Task<LogSistemaDTO?> GetByIdAsync(long id)
        {
            var log = await _repository.GetByIdAsync(id);
            return log is null ? null : ToDto(log);
        }

        public async Task<bool> AddAsync(LogSistemaCreateDTO dto)
        {
            var entity = ToEntity(dto);
            return await _repository.AddAsync(entity);
        }

        public async Task<bool> RemoveAsync(long id)
        {
            return await _repository.RemoveAsync(id);
        }
    }
}
