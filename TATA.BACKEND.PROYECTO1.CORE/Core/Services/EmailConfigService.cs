using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services;

/// <summary>
/// Servicio de configuración de Email con Primary Constructor (.NET 9)
/// </summary>
public class EmailConfigService(
    Proyecto1SlaDbContext context,
    ILogger<EmailConfigService> logger) : IEmailConfigService
{
    private readonly Proyecto1SlaDbContext _context = context;
    private readonly ILogger<EmailConfigService> _logger = logger;

    public async Task<EmailConfigDTO?> GetConfigAsync()
    {
        try
        {
            var config = await _context.EmailConfig.FirstOrDefaultAsync();
            if (config == null)
            {
                _logger.LogWarning("No se encontró configuración de email en la BD");
                return null;
            }

            _logger.LogDebug("Configuración de email obtenida: Id={Id}", config.Id);

            return new EmailConfigDTO
            {
                Id = config.Id,
                DestinatarioResumen = config.DestinatarioResumen,
                EnvioInmediato = config.EnvioInmediato,
                ResumenDiario = config.ResumenDiario,
                HoraResumen = config.HoraResumen,
                CreadoEn = config.CreadoEn,
                ActualizadoEn = config.ActualizadoEn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de email");
            throw;
        }
    }

    public async Task<EmailConfigDTO?> UpdateConfigAsync(int id, EmailConfigUpdateDTO dto)
    {
        try
        {
            var config = await _context.EmailConfig.FindAsync(id);
            if (config == null)
            {
                _logger.LogWarning("Configuración de email con Id={Id} no encontrada", id);
                return null;
            }

            // Actualizar solo los campos que vienen en el DTO
            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.DestinatarioResumen) &&
                dto.DestinatarioResumen != config.DestinatarioResumen)
            {
                config.DestinatarioResumen = dto.DestinatarioResumen;
                hasChanges = true;
                _logger.LogDebug("Destinatario actualizado: {Destinatario}", dto.DestinatarioResumen);
            }

            if (dto.EnvioInmediato.HasValue && dto.EnvioInmediato.Value != config.EnvioInmediato)
            {
                config.EnvioInmediato = dto.EnvioInmediato.Value;
                hasChanges = true;
                _logger.LogDebug("EnvioInmediato actualizado: {EnvioInmediato}", dto.EnvioInmediato.Value);
            }

            if (dto.ResumenDiario.HasValue && dto.ResumenDiario.Value != config.ResumenDiario)
            {
                config.ResumenDiario = dto.ResumenDiario.Value;
                hasChanges = true;
                _logger.LogDebug("ResumenDiario actualizado: {ResumenDiario}", dto.ResumenDiario.Value);
            }

            if (dto.HoraResumen.HasValue && dto.HoraResumen.Value != config.HoraResumen)
            {
                config.HoraResumen = dto.HoraResumen.Value;
                hasChanges = true;
                _logger.LogDebug("HoraResumen actualizada: {HoraResumen}", dto.HoraResumen.Value);
            }

            if (hasChanges)
            {
                config.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Configuración de email {Id} actualizada exitosamente", id);
            }
            else
            {
                _logger.LogDebug("No se detectaron cambios en la configuración de email {Id}", id);
            }

            return new EmailConfigDTO
            {
                Id = config.Id,
                DestinatarioResumen = config.DestinatarioResumen,
                EnvioInmediato = config.EnvioInmediato,
                ResumenDiario = config.ResumenDiario,
                HoraResumen = config.HoraResumen,
                CreadoEn = config.CreadoEn,
                ActualizadoEn = config.ActualizadoEn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar configuración de email {Id}", id);
            throw;
        }
    }
}
