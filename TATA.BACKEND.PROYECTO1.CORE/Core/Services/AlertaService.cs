using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services;

/// <summary>
/// Servicio inteligente de Alertas con sincronización automática y dashboard enriquecido
/// Utiliza Primary Constructor (.NET 9)
/// </summary>
public class AlertaService(
    IAlertaRepository alertaRepository,
    IEmailService emailService,
    ISolicitudRepository solicitudRepository,
    ILogger<AlertaService> logger) : IAlertaService
{
    private readonly IAlertaRepository _alertaRepository = alertaRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ISolicitudRepository _solicitudRepository = solicitudRepository;
    private readonly ILogger<AlertaService> _logger = logger;

    // ============================================
    // MÉTODOS DE NEGOCIO INTELIGENTE
    // ============================================

    /// <summary>
    /// UPSERT LÓGICO: Sincroniza alertas desde solicitudes
    /// - INSERT: Crea alertas nuevas para solicitudes sin alerta
    /// - UPDATE: Actualiza nivel/mensaje si cambió desde la última sincronización
    /// </summary>
    public async Task SyncAlertasFromSolicitudesAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando sincronización de alertas desde solicitudes...");

            // 1. Obtener solicitudes activas con sus relaciones
            var solicitudes = await _solicitudRepository.GetSolicitudsAsync();
            var solicitudesActivas = solicitudes
                .Where(s => s.EstadoSolicitud != "CERRADO" && 
                           s.EstadoSolicitud != "ELIMINADO" &&
                           s.IdSlaNavigation != null)
                .ToList();

            var hoy = DateTime.UtcNow.Date;
            var alertasCreadas = 0;
            var alertasActualizadas = 0;

            foreach (var solicitud in solicitudesActivas)
            {
                try
                {
                    // 2. Calcular días restantes y nivel
                    var fechaSolicitud = solicitud.FechaSolicitud.ToDateTime(TimeOnly.MinValue);
                    var diasUmbral = solicitud.IdSlaNavigation.DiasUmbral;
                    var fechaLimite = fechaSolicitud.AddDays(diasUmbral);
                    var diasRestantes = (int)Math.Ceiling((fechaLimite - hoy).TotalDays);

                    // 3. Determinar nivel según días restantes
                    var (nivel, tipoAlerta) = CalcularNivelAlerta(diasRestantes);
                    var mensaje = GenerarMensajeAlerta(diasRestantes, diasUmbral, solicitud);

                    // 4. Buscar alerta existente para esta solicitud
                    var alertaExistente = await _alertaRepository.GetAlertaBySolicitudIdAsync(solicitud.IdSolicitud);

                    if (alertaExistente == null)
                    {
                        // INSERT: Crear nueva alerta
                        var nuevaAlerta = new Alerta
                        {
                            IdSolicitud = solicitud.IdSolicitud,
                            TipoAlerta = tipoAlerta,
                            Nivel = nivel,
                            Mensaje = mensaje,
                            Estado = "ACTIVA",
                            EnviadoEmail = false,
                            FechaCreacion = DateTime.UtcNow
                        };

                        await _alertaRepository.CreateAlertaAsync(nuevaAlerta);
                        alertasCreadas++;
                        _logger.LogDebug("Alerta creada para solicitud {IdSolicitud}, Nivel: {Nivel}", 
                            solicitud.IdSolicitud, nivel);
                    }
                    else if (alertaExistente.Nivel != nivel || alertaExistente.Estado == "INACTIVA")
                    {
                        // UPDATE: Solo si el nivel cambió o estaba inactiva
                        alertaExistente.Nivel = nivel;
                        alertaExistente.TipoAlerta = tipoAlerta;
                        alertaExistente.Mensaje = mensaje;
                        alertaExistente.Estado = "ACTIVA";
                        alertaExistente.ActualizadoEn = DateTime.UtcNow;

                        await _alertaRepository.UpdateAlertaAsync(alertaExistente.IdAlerta, alertaExistente);
                        alertasActualizadas++;
                        _logger.LogDebug("Alerta actualizada para solicitud {IdSolicitud}, Nivel: {Nivel}", 
                            solicitud.IdSolicitud, nivel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al sincronizar alerta para solicitud {IdSolicitud}", 
                        solicitud.IdSolicitud);
                    // Continuar con las demás solicitudes
                }
            }

            _logger.LogInformation(
                "Sincronización completada: {Creadas} alertas creadas, {Actualizadas} alertas actualizadas de {Total} solicitudes activas",
                alertasCreadas, alertasActualizadas, solicitudesActivas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico durante la sincronización de alertas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene datos PLANOS enriquecidos para el Dashboard del Frontend
    /// Sin objetos anidados - Optimizado para consumo directo
    /// </summary>
    public async Task<List<AlertaDashboardDto>> GetAllDashboardAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo datos enriquecidos para el dashboard...");

            var alertas = await _alertaRepository.GetAlertasWithFullNavigationAsync();
            var hoy = DateTime.UtcNow.Date;

            var dashboard = alertas.Select(a =>
            {
                var solicitud = a.IdSolicitudNavigation;
                if (solicitud == null)
                {
                    _logger.LogWarning("Alerta {IdAlerta} sin solicitud asociada", a.IdAlerta);
                    return CrearAlertaHuerfana(a);
                }

                var personal = solicitud.IdPersonalNavigation;
                var rol = solicitud.IdRolRegistroNavigation;
                var sla = solicitud.IdSlaNavigation;

                // Cálculos matemáticos
                var fechaSolicitud = solicitud.FechaSolicitud.ToDateTime(TimeOnly.MinValue);
                var diasUmbral = sla?.DiasUmbral ?? 0;
                var fechaLimite = fechaSolicitud.AddDays(diasUmbral);
                var diasRestantes = (int)Math.Ceiling((fechaLimite - hoy).TotalDays);
                var diasTranscurridos = Math.Max(0, (int)Math.Floor((hoy - fechaSolicitud).TotalDays));
                var porcentaje = diasUmbral > 0 
                    ? Math.Min(100, Math.Max(0, (int)((double)diasTranscurridos / diasUmbral * 100))) 
                    : 100;

                // Colores e iconos según nivel
                var (color, icono) = ObtenerEstilosPorNivel(a.Nivel);

                return new AlertaDashboardDto
                {
                    // Datos de la alerta
                    IdAlerta = a.IdAlerta,
                    TipoAlerta = a.TipoAlerta,
                    Nivel = a.Nivel,
                    Mensaje = a.Mensaje,
                    Estado = a.Estado,
                    EnviadoEmail = a.EnviadoEmail,
                    FechaCreacion = a.FechaCreacion,
                    FechaLectura = a.FechaLectura,

                    // Datos de la solicitud (PLANOS)
                    IdSolicitud = solicitud.IdSolicitud,
                    CodigoSolicitud = $"SOL-{solicitud.IdSolicitud:D6}",
                    FechaSolicitud = fechaSolicitud,
                    FechaIngreso = solicitud.FechaIngreso?.ToDateTime(TimeOnly.MinValue),
                    EstadoSolicitud = solicitud.EstadoSolicitud,
                    EstadoCumplimientoSla = solicitud.EstadoCumplimientoSla,

                    // Datos del responsable (PLANOS - CRÍTICO)
                    IdPersonal = personal?.IdPersonal ?? 0,
                    NombreResponsable = personal != null 
                        ? $"{personal.Nombres} {personal.Apellidos}".Trim() 
                        : "Sin asignar",
                    EmailResponsable = personal?.CorreoCorporativo ?? "",
                    DocumentoResponsable = personal?.Documento,

                    // Datos del rol (PLANOS)
                    IdRolRegistro = rol?.IdRolRegistro ?? 0,
                    NombreRol = rol?.NombreRol,
                    BloqueTech = rol?.BloqueTech,

                    // Datos del SLA (PLANOS)
                    IdSla = sla?.IdSla ?? 0,
                    CodigoSla = sla?.CodigoSla,
                    NombreSla = sla?.Descripcion,
                    DiasUmbral = diasUmbral,
                    TipoSolicitud = sla?.TipoSolicitud,

                    // Cálculos para el Frontend
                    DiasRestantes = diasRestantes,
                    PorcentajeProgreso = porcentaje,
                    ColorEstado = color,
                    IconoEstado = icono,
                    EstaVencida = diasRestantes < 0,
                    EsCritica = a.Nivel == "CRITICO"
                };
            }).ToList();

            _logger.LogInformation("Dashboard generado con {Count} alertas", dashboard.Count);
            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar datos del dashboard");
            throw;
        }
    }

    // ============================================
    // MÉTODOS CRUD BÁSICOS (Mantenidos)
    // ============================================

    public async Task<List<AlertaDTO>> GetAllAsync()
    {
        var entities = await _alertaRepository.GetAlertasAsync();
        return entities.Select(MapToDTO).ToList();
    }

    public async Task<AlertaDTO?> GetByIdAsync(int id)
    {
        var a = await _alertaRepository.GetAlertaByIdAsync(id);
        return a == null ? null : MapToDTO(a);
    }

    public async Task<AlertaDTO> CreateAsync(AlertaCreateDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var entity = new Alerta
        {
            IdSolicitud = dto.IdSolicitud,
            TipoAlerta = dto.TipoAlerta,
            Nivel = dto.Nivel,
            Mensaje = dto.Mensaje,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "NUEVA" : dto.Estado,
            EnviadoEmail = false,
            FechaCreacion = DateTime.UtcNow
        };

        var created = await _alertaRepository.CreateAlertaAsync(entity);
        var alertaFull = await _alertaRepository.GetAlertaByIdAsync(created.IdAlerta);
        
        if (alertaFull == null)
            throw new Exception("No se pudo recuperar la alerta creada.");

        // Enviar correo si está configurado
        await EnviarCorreoAlertaAsync(alertaFull);

        return MapToDTO(alertaFull);
    }

    public async Task<AlertaDTO?> UpdateAsync(int id, AlertaUpdateDto dto)
    {
        var existing = await _alertaRepository.GetAlertaByIdAsync(id);
        if (existing == null) return null;

        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(dto.TipoAlerta) && dto.TipoAlerta != existing.TipoAlerta)
        {
            existing.TipoAlerta = dto.TipoAlerta;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Nivel) && dto.Nivel != existing.Nivel)
        {
            existing.Nivel = dto.Nivel;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Mensaje) && dto.Mensaje != existing.Mensaje)
        {
            existing.Mensaje = dto.Mensaje;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Estado) && dto.Estado != existing.Estado)
        {
            existing.Estado = dto.Estado;
        }

        bool shouldSendEmail = dto.EnviadoEmail ?? (hasChanges && !existing.EnviadoEmail);

        existing.ActualizadoEn = DateTime.UtcNow;
        var updated = await _alertaRepository.UpdateAlertaAsync(id, existing);
        
        if (updated == null) return null;

        if (shouldSendEmail)
        {
            await EnviarCorreoAlertaAsync(updated);
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _alertaRepository.DeleteAlertaAsync(id);
    }

    // ============================================
    // MÉTODOS PRIVADOS DE UTILIDAD
    // ============================================

    private (string nivel, string tipoAlerta) CalcularNivelAlerta(int diasRestantes)
    {
        return diasRestantes switch
        {
            < 0 => ("CRITICO", "SLA_VENCIDO"),
            <= 2 => ("CRITICO", "SLA_VENCIMIENTO_INMEDIATO"),
            <= 5 => ("ALTO", "SLA_PREVENTIVA"),
            <= 10 => ("MEDIO", "SLA_SEGUIMIENTO"),
            _ => ("BAJO", "SLA_NORMAL")
        };
    }

    private string GenerarMensajeAlerta(int diasRestantes, int diasUmbral, Solicitud solicitud)
    {
        var codigoSol = $"SOL-{solicitud.IdSolicitud:D6}";
        
        return diasRestantes switch
        {
            < 0 => $"🔴 CRÍTICO: {codigoSol} VENCIDA hace {Math.Abs(diasRestantes)} día(s). SLA: {diasUmbral} días",
            <= 2 => $"🔴 URGENTE: {codigoSol} vence en {diasRestantes} día(s). SLA: {diasUmbral} días",
            <= 5 => $"🟠 ALERTA: {codigoSol} vence en {diasRestantes} día(s). SLA: {diasUmbral} días",
            <= 10 => $"🟡 PREVENTIVA: {codigoSol} tiene {diasRestantes} día(s) restantes. SLA: {diasUmbral} días",
            _ => $"🟢 NORMAL: {codigoSol} tiene {diasRestantes} día(s) restantes. SLA: {diasUmbral} días"
        };
    }

    private (string color, string icono) ObtenerEstilosPorNivel(string? nivel)
    {
        return nivel switch
        {
            "CRITICO" => ("#D32F2F", "error"),
            "ALTO" => ("#F57C00", "warning"),
            "MEDIO" => ("#FBC02D", "info"),
            "BAJO" => ("#388E3C", "check_circle"),
            _ => ("#2196F3", "help")
        };
    }

    private AlertaDashboardDto CrearAlertaHuerfana(Alerta a)
    {
        return new AlertaDashboardDto
        {
            IdAlerta = a.IdAlerta,
            TipoAlerta = a.TipoAlerta,
            Nivel = "ERROR",
            Mensaje = "⚠️ Alerta sin solicitud asociada",
            Estado = a.Estado,
            EnviadoEmail = a.EnviadoEmail,
            FechaCreacion = a.FechaCreacion,
            DiasRestantes = 0,
            PorcentajeProgreso = 0,
            ColorEstado = "#9E9E9E",
            IconoEstado = "error_outline",
            EstaVencida = true,
            EsCritica = true,
            NombreResponsable = "N/A",
            EmailResponsable = "",
            CodigoSolicitud = "N/A"
        };
    }

    private async Task EnviarCorreoAlertaAsync(Alerta alerta)
    {
        var destinatario = alerta.IdSolicitudNavigation?.IdPersonalNavigation?.CorreoCorporativo;
        
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            _logger.LogWarning("Alerta {IdAlerta} sin destinatario de correo", alerta.IdAlerta);
            return;
        }

        try
        {
            var subject = $"[ALERTA SLA] {alerta.TipoAlerta} - {alerta.Nivel}";
            var body = EmailTemplates.BuildAlertaBody(alerta);

            await _emailService.SendAsync(destinatario, subject, body);

            alerta.EnviadoEmail = true;
            alerta.ActualizadoEn = DateTime.UtcNow;
            await _alertaRepository.UpdateAlertaAsync(alerta.IdAlerta, alerta);

            _logger.LogInformation("Correo enviado a {Destinatario} para alerta {IdAlerta}", 
                destinatario, alerta.IdAlerta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo para alerta {IdAlerta}", alerta.IdAlerta);
        }
    }

    private AlertaDTO MapToDTO(Alerta a)
    {
        return new AlertaDTO
        {
            IdAlerta = a.IdAlerta,
            IdSolicitud = a.IdSolicitud,
            TipoAlerta = a.TipoAlerta,
            Nivel = a.Nivel,
            Mensaje = a.Mensaje,
            Estado = a.Estado,
            EnviadoEmail = a.EnviadoEmail,
            FechaCreacion = a.FechaCreacion,
            FechaLectura = a.FechaLectura,
            ActualizadoEn = a.ActualizadoEn,
            Solicitud = a.IdSolicitudNavigation == null ? null : new AlertaSolicitudInfoDto
            {
                IdSolicitud = a.IdSolicitudNavigation.IdSolicitud,
                FechaSolicitud = a.IdSolicitudNavigation.FechaSolicitud,
                FechaIngreso = a.IdSolicitudNavigation.FechaIngreso,
                NumDiasSla = a.IdSolicitudNavigation.NumDiasSla,
                ResumenSla = a.IdSolicitudNavigation.ResumenSla,
                EstadoSolicitud = a.IdSolicitudNavigation.EstadoSolicitud,
                EstadoCumplimientoSla = a.IdSolicitudNavigation.EstadoCumplimientoSla,
                Personal = a.IdSolicitudNavigation.IdPersonalNavigation == null ? null : new AlertaSolicitudPersonalDto
                {
                    IdPersonal = a.IdSolicitudNavigation.IdPersonalNavigation.IdPersonal,
                    Nombres = a.IdSolicitudNavigation.IdPersonalNavigation.Nombres,
                    Apellidos = a.IdSolicitudNavigation.IdPersonalNavigation.Apellidos,
                    CorreoCorporativo = a.IdSolicitudNavigation.IdPersonalNavigation.CorreoCorporativo
                },
                RolRegistro = a.IdSolicitudNavigation.IdRolRegistroNavigation == null ? null : new AlertaSolicitudRolDto
                {
                    IdRolRegistro = a.IdSolicitudNavigation.IdRolRegistroNavigation.IdRolRegistro,
                    NombreRol = a.IdSolicitudNavigation.IdRolRegistroNavigation.NombreRol
                },
                ConfigSla = a.IdSolicitudNavigation.IdSlaNavigation == null ? null : new AlertaSolicitudSlaDto
                {
                    IdSla = a.IdSolicitudNavigation.IdSlaNavigation.IdSla,
                    CodigoSla = a.IdSolicitudNavigation.IdSlaNavigation.CodigoSla,
                    DiasUmbral = a.IdSolicitudNavigation.IdSlaNavigation.DiasUmbral,
                    TipoSolicitud = a.IdSolicitudNavigation.IdSlaNavigation.TipoSolicitud
                }
            }
        };
    }
}
