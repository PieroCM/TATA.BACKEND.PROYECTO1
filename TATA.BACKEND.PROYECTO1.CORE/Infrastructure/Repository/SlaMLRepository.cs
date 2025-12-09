using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Repository
{
    /// <summary>
    /// Repositorio para operaciones de Machine Learning de predicción SLA
    /// </summary>
    public class SlaMLRepository : ISlaMLRepository
    {
        private readonly Proyecto1SlaDbContext _context;

        // Estados válidos para solicitudes cerradas (entrenamiento)
        private static readonly string[] EstadosCerrados = { "CERRADO", "INACTIVO", "VENCIDO" };

        // Estados válidos para solicitudes activas (predicción)
        private static readonly string[] EstadosActivos = { "ACTIVO", "EN_PROCESO", "PENDIENTE" };

        public SlaMLRepository(Proyecto1SlaDbContext context)
        {
            _context = context;
        }

        // ???????????????????????????????????????????????????????????????????
        // SOLICITUDES PARA ENTRENAMIENTO
        // ???????????????????????????????????????????????????????????????????

        public async Task<List<Solicitud>> GetSolicitudesParaEntrenamientoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeOnly = DateOnly.FromDateTime(fechaDesde);
            var fechaHastaOnly = DateOnly.FromDateTime(fechaHasta);

            return await _context.Solicitud
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.IdSlaNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Include(s => s.IdPersonalNavigation)
                .Where(s =>
                    s.EstadoSolicitud != null &&
                    EstadosCerrados.Contains(s.EstadoSolicitud) &&
                    s.EstadoCumplimientoSla != null &&
                    (s.EstadoCumplimientoSla.StartsWith("CUMPLE_") || s.EstadoCumplimientoSla.StartsWith("NO_CUMPLE_")) &&
                    s.FechaSolicitud >= fechaDesdeOnly &&
                    s.FechaSolicitud <= fechaHastaOnly &&
                    s.NumDiasSla.HasValue)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<int> CountSolicitudesEntrenamientoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeOnly = DateOnly.FromDateTime(fechaDesde);
            var fechaHastaOnly = DateOnly.FromDateTime(fechaHasta);

            return await _context.Solicitud
                .AsNoTracking()
                .CountAsync(s =>
                    s.EstadoSolicitud != null &&
                    EstadosCerrados.Contains(s.EstadoSolicitud) &&
                    s.EstadoCumplimientoSla != null &&
                    (s.EstadoCumplimientoSla.StartsWith("CUMPLE_") || s.EstadoCumplimientoSla.StartsWith("NO_CUMPLE_")) &&
                    s.FechaSolicitud >= fechaDesdeOnly &&
                    s.FechaSolicitud <= fechaHastaOnly);
        }

        // ???????????????????????????????????????????????????????????????????
        // SOLICITUDES PARA PREDICCIÓN
        // ???????????????????????????????????????????????????????????????????

        public async Task<List<Solicitud>> GetSolicitudesActivasParaPrediccionAsync()
        {
            return await _context.Solicitud
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.IdSlaNavigation)
                .Include(s => s.IdRolRegistroNavigation)
                .Include(s => s.IdPersonalNavigation)
                .Where(s =>
                    s.EstadoSolicitud != null &&
                    EstadosActivos.Contains(s.EstadoSolicitud) &&
                    s.FechaIngreso == null) // Sin fecha de cierre
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<int> CountSolicitudesActivasAsync()
                {
                    return await _context.Solicitud
                        .AsNoTracking()
                        .CountAsync(s =>
                            s.EstadoSolicitud != null &&
                            EstadosActivos.Contains(s.EstadoSolicitud) &&
                            s.FechaIngreso == null);
                }
            }
        }
