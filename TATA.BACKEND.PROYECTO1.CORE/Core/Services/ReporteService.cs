using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    /// Servicio de negocio para Reporte (cabecera).
    /// Depende de IReporteRepository con métodos básicos: GetAll/GetById/Add/Update/Delete.

    public class ReporteService : IReporteService
    {
        private readonly IReporteRepository _repo;
        private readonly Proyecto1SlaDbContext _context;

        public ReporteService(IReporteRepository repo, Proyecto1SlaDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public Task<IEnumerable<Reporte>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<Reporte?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task AddAsync(Reporte reporte)
            => _repo.AddAsync(reporte);

        public async Task<bool> UpdateAsync(Reporte reporte)
        {
            var current = await _repo.GetByIdAsync(reporte.IdReporte);
            if (current is null) return false;

            await _repo.UpdateAsync(reporte);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            await _repo.DeleteAsync(current);
            return true;
        }

        public async Task<Reporte> GenerarReporteAsync(GenerarReporteRequest request, int idUsuarioActual, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            using var tx = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var reporte = new Reporte
                {
                    TipoReporte = request.TipoReporte,
                    Formato = request.Formato,
                    FiltrosJson = request.FiltrosJson, // ya viene como string JSON dinámico
                    RutaArchivo = null,
                    GeneradoPor = idUsuarioActual
                    // FechaGeneracion la setea SQL
                };

                // Guardar para obtener IdReporte
                _context.Reporte.Add(reporte);
                await _context.SaveChangesAsync(ct);

                var ids = request.IdsSolicitudes?.Distinct().ToList() ?? new List<int>();

                var detalles = ids.Select(idSol => new ReporteDetalle
                {
                    IdReporte = reporte.IdReporte,
                    IdSolicitud = idSol
                }).ToList();

                if (detalles.Count > 0)
                {
                    _context.ReporteDetalle.AddRange(detalles);
                    await _context.SaveChangesAsync(ct);
                }

                // Opcional: fijar ruta de archivo
                reporte.RutaArchivo = $"\\reports\\reporte_{reporte.IdReporte}.{request.Formato.ToLower()}";

                // Guardar cambio de ruta
                _context.Reporte.Update(reporte);
                await _context.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                // asignar navegación para devolución
                reporte.Detalles = detalles;

                return reporte;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }

}
