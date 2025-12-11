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
/// Maneja la configuración de resumen diario y envío inmediato
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
                _logger.LogWarning("?? No se encontró configuración de email en la BD");
                return null;
            }

            _logger.LogDebug("? Configuración de email obtenida: Id={Id}, ResumenDiario={ResumenDiario}, HoraResumen={Hora}", 
                config.Id, config.ResumenDiario, config.HoraResumen);

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
            _logger.LogError(ex, "? Error al obtener configuración de email");
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
                _logger.LogWarning("?? Configuración de email con Id={Id} no encontrada", id);
                return null;
            }

            var hasChanges = false;
            var cambios = new System.Collections.Generic.List<string>();

            // ============================================
            // CAMPO 1 DEL FRONTEND: ResumenDiario (Toggle)
            // ============================================
            if (dto.ResumenDiario.HasValue && dto.ResumenDiario.Value != config.ResumenDiario)
            {
                var estadoAnterior = config.ResumenDiario;
                config.ResumenDiario = dto.ResumenDiario.Value;
                hasChanges = true;
                
                var emoji = dto.ResumenDiario.Value ? "?" : "?";
                var estado = dto.ResumenDiario.Value ? "ACTIVADO" : "DESACTIVADO";
                
                cambios.Add($"ResumenDiario: {estadoAnterior} ? {dto.ResumenDiario.Value}");
                _logger.LogInformation("{Emoji} Resumen Diario {Estado} por actualización manual", emoji, estado);
            }

            // ============================================
            // CAMPO 2 DEL FRONTEND: HoraResumen (Time Picker)
            // ============================================
            if (dto.HoraResumen.HasValue && dto.HoraResumen.Value != config.HoraResumen)
            {
                var horaAnterior = config.HoraResumen;
                config.HoraResumen = dto.HoraResumen.Value;
                hasChanges = true;
                
                cambios.Add($"HoraResumen: {horaAnterior:hh\\:mm\\:ss} ? {dto.HoraResumen.Value:hh\\:mm\\:ss}");
                _logger.LogInformation("? Hora de Resumen Diario actualizada: {HoraAnterior} ? {HoraNueva}", 
                    horaAnterior.ToString(@"hh\:mm\:ss"), 
                    dto.HoraResumen.Value.ToString(@"hh\:mm\:ss"));
            }

            // Actualizar destinatario si viene en el DTO
            if (!string.IsNullOrWhiteSpace(dto.DestinatarioResumen) &&
                dto.DestinatarioResumen != config.DestinatarioResumen)
            {
                var destinatarioAnterior = config.DestinatarioResumen;
                config.DestinatarioResumen = dto.DestinatarioResumen;
                hasChanges = true;
                
                cambios.Add($"Destinatario: {destinatarioAnterior} ? {dto.DestinatarioResumen}");
                _logger.LogInformation("?? Destinatario de Resumen actualizado: {NuevoDestinatario}", dto.DestinatarioResumen);
            }

            // Actualizar envío inmediato si viene en el DTO
            if (dto.EnvioInmediato.HasValue && dto.EnvioInmediato.Value != config.EnvioInmediato)
            {
                var estadoAnterior = config.EnvioInmediato;
                config.EnvioInmediato = dto.EnvioInmediato.Value;
                hasChanges = true;
                
                cambios.Add($"EnvioInmediato: {estadoAnterior} ? {dto.EnvioInmediato.Value}");
                _logger.LogDebug("EnvíoInmediato actualizado: {EnvioInmediato}", dto.EnvioInmediato.Value);
            }

            // Guardar cambios si hubo modificaciones
            if (hasChanges)
            {
                config.ActualizadoEn = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("?? Configuración de email {Id} actualizada exitosamente. Cambios: {Cambios}", 
                    id, string.Join(", ", cambios));
            }
            else
            {
                _logger.LogDebug("?? No se detectaron cambios en la configuración de email {Id}", id);
            }

            // Retornar configuración actualizada
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
            _logger.LogError(ex, "? Error al actualizar configuración de email {Id}", id);
            throw;
        }
    }
}
