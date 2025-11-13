using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class PersonalService : IPersonalService
    {
        private readonly IPersonalRepository _personalRepository;

        public PersonalService(IPersonalRepository personalRepository)
        {
            _personalRepository = personalRepository;
        }

        public async Task<IEnumerable<PersonalResponseDTO>> GetAllAsync()
        {
            var lista = await _personalRepository.GetAllAsync();
            return lista.Select(p => new PersonalResponseDTO
            {
                IdPersonal = p.IdPersonal,
                Nombres = p.Nombres ?? "",
                Apellidos = p.Apellidos ?? "",
                Documento = p.Documento ?? "",
                CorreoCorporativo = p.CorreoCorporativo ?? "",
                Estado = p.Estado ?? "",
                IdUsuario = p.IdUsuario,
                UsuarioCorreo = p.IdUsuarioNavigation?.Correo ?? ""
            });
        }

        public async Task<PersonalResponseDTO?> GetByIdAsync(int id)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return null;

            return new PersonalResponseDTO
            {
                IdPersonal = p.IdPersonal,
                Nombres = p.Nombres ?? "",
                Apellidos = p.Apellidos ?? "",
                Documento = p.Documento ?? "",
                CorreoCorporativo = p.CorreoCorporativo ?? "",
                Estado = p.Estado ?? "",
                IdUsuario = p.IdUsuario,
                UsuarioCorreo = p.IdUsuarioNavigation?.Correo ?? ""
            };
        }

        public async Task<bool> CreateAsync(PersonalCreateDTO dto)
        {
            var entity = new Personal
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Documento = dto.Documento,
                CorreoCorporativo = dto.CorreoCorporativo,
                Estado = dto.Estado,
                IdUsuario = dto.IdUsuario,
                CreadoEn = DateTime.Now
            };
            await _personalRepository.AddAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(int id, PersonalUpdateDTO dto)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return false;

            p.Nombres = dto.Nombres ?? p.Nombres;
            p.Apellidos = dto.Apellidos ?? p.Apellidos;
            p.Documento = dto.Documento ?? p.Documento;
            p.CorreoCorporativo = dto.CorreoCorporativo ?? p.CorreoCorporativo;
            p.Estado = dto.Estado ?? p.Estado;
            p.ActualizadoEn = DateTime.Now;

            await _personalRepository.UpdateAsync(p);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var p = await _personalRepository.GetByIdAsync(id);
            if (p == null) return false;

            await _personalRepository.DeleteAsync(p);
            return true;
        }
    }
}
