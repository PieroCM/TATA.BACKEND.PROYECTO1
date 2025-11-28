using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Repository
{
    public class SolicitudRepository : ISolicitudRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        public SolicitudRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        //Get Solicitudes use include para IDUsuario,IDEstadoSolicitud, IDAlerta, IDConfig_SlA, IDRol_registro
        public async Task<List<Solicitud>> GetSolicitudsAsync()
        {
            return await _context.Solicitud
                .AsNoTracking()
                .Where(s => s.EstadoSolicitud != "ELIMINADO" || s.EstadoSolicitud == null)
                .Include(s => s.CreadoPorNavigation)
                    .ThenInclude(u => u.PersonalNavigation) // ⚠️ Incluir Personal del Usuario para obtener CorreoCorporativo
                .Include(s => s.IdPersonalNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Include(s => s.IdSlaNavigation)
                .Include(s => s.Alerta)
                .Include(s => s.IdReporte)
                .OrderBy(s => s.IdSolicitud)
                .ToListAsync();
        }



        // Get Solicitud by ID
        public async Task<Solicitud?> GetSolicitudByIdAsync(int id)
        {
            return await _context.Solicitud
                .AsNoTracking()
                .Include(s => s.CreadoPorNavigation)
                    .ThenInclude(u => u.PersonalNavigation) // ⚠️ Incluir Personal del Usuario para obtener CorreoCorporativo
                .Include(s => s.IdPersonalNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Include(s => s.IdSlaNavigation)
                .Include(s => s.Alerta)
                .Include(s => s.IdReporte)
                .FirstOrDefaultAsync(s => s.IdSolicitud == id);
        }

        // Post Solicitud y validacion de FK
        public async Task<Solicitud> CreateSolicitudAsync(Solicitud solicitud)
        {
            if (solicitud == null) throw new ArgumentNullException(nameof(solicitud));

            // Validar que las claves foráneas existan en forma individual para identificar cuál falta
            if (!await _context.Personal.AnyAsync(p => p.IdPersonal == solicitud.IdPersonal))
                throw new ArgumentException($"Clave foránea no encontrada: Personal (IdPersonal={solicitud.IdPersonal})", nameof(solicitud.IdPersonal));

            if (!await _context.ConfigSla.AnyAsync(s => s.IdSla == solicitud.IdSla))
                throw new ArgumentException($"Clave foránea no encontrada: ConfigSla (IdSla={solicitud.IdSla})", nameof(solicitud.IdSla));

            if (!await _context.RolRegistro.AnyAsync(r => r.IdRolRegistro == solicitud.IdRolRegistro))
                throw new ArgumentException($"Clave foránea no encontrada: RolRegistro (IdRolRegistro={solicitud.IdRolRegistro})", nameof(solicitud.IdRolRegistro));

            if (!await _context.Usuario.AnyAsync(u => u.IdUsuario == solicitud.CreadoPor))
                throw new ArgumentException($"Clave foránea no encontrada: Usuario (CreadoPor={solicitud.CreadoPor})", nameof(solicitud.CreadoPor));


            _context.Solicitud.Add(solicitud);
            await _context.SaveChangesAsync();
            return solicitud;
        }
        // Infrastructure/Repository/RepositorySolicitud.cs
        public async Task<ConfigSla?> GetConfigSlaByIdAsync(int idSla)
        {
            return await _context.ConfigSla
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdSla == idSla);
        }


        // Put Solicitud by id with validation of FK
        public async Task<Solicitud?> UpdateSolicitudAsync(int id, Solicitud solicitud)
        {
            var existingSolicitud = await _context.Solicitud.FindAsync(id);
            if (existingSolicitud == null) return null;
            // Validar que las claves foráneas existan en forma individual para identificar cuál falta
            if (!await _context.Personal.AnyAsync(p => p.IdPersonal == solicitud.IdPersonal))
                throw new ArgumentException($"Clave foránea no encontrada: Personal (IdPersonal={solicitud.IdPersonal})", nameof(solicitud.IdPersonal));
            if (!await _context.ConfigSla.AnyAsync(s => s.IdSla == solicitud.IdSla))
                throw new ArgumentException($"Clave foránea no encontrada: ConfigSla (IdSla={solicitud.IdSla})", nameof(solicitud.IdSla));
            if (!await _context.RolRegistro.AnyAsync(r => r.IdRolRegistro == solicitud.IdRolRegistro))
                throw new ArgumentException($"Clave foránea no encontrada: RolRegistro (IdRolRegistro={solicitud.IdRolRegistro})", nameof(solicitud.IdRolRegistro));
            if (!await _context.Usuario.AnyAsync(u => u.IdUsuario == solicitud.CreadoPor))
                throw new ArgumentException($"Clave foránea no encontrada: Usuario (CreadoPor={solicitud.CreadoPor})", nameof(solicitud.CreadoPor));
            // Actualizar los campos de la solicitud existente
            _context.Entry(existingSolicitud).CurrentValues.SetValues(solicitud);
            await _context.SaveChangesAsync();
            return existingSolicitud;
        }

        // Delete Solicitud by id
        public async Task<bool> DeleteSolicitudAsync(int id, string deletedState = "ELIMINADO")
        {
            var solicitud = await _context.Solicitud.FindAsync(id);
            if (solicitud == null)
                return false;

            // ya está eliminado
            if (!string.IsNullOrWhiteSpace(solicitud.EstadoSolicitud) &&
                solicitud.EstadoSolicitud.Equals(deletedState, StringComparison.OrdinalIgnoreCase))
                return false;

            solicitud.EstadoSolicitud = deletedState;
            solicitud.ActualizadoEn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }




    }

}
