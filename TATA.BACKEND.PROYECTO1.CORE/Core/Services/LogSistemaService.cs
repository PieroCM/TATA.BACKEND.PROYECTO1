using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class LogSistemaService : ILogSistemaService
    {
        private readonly IRepositoryLogSistema _repository;
        private readonly IMapper _mapper;

        public LogSistemaService(IRepositoryLogSistema repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LogSistemaDTO>> GetAllAsync()
        {
            var logs = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<LogSistemaDTO>>(logs);
        }

        public async Task<LogSistemaDTO?> GetByIdAsync(long id)
        {
            var log = await _repository.GetByIdAsync(id);
            return _mapper.Map<LogSistemaDTO>(log);
        }

        public async Task<bool> AddAsync(LogSistemaCreateDTO dto)
        {
            var entity = _mapper.Map<LogSistema>(dto);
            entity.FechaHora = DateTime.UtcNow; // Asignar automáticamente
            return await _repository.AddAsync(entity);
        }

        public async Task<bool> RemoveAsync(long id)
        {
            return await _repository.RemoveAsync(id);
        }
    }
}
