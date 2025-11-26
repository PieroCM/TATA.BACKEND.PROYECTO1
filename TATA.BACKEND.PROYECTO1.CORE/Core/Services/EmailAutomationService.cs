using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class EmailAutomationService : IEmailAutomationService
    {
        private readonly IEmailConfigRepository _configRepo;
        private readonly IEmailLogRepository _logRepo;
        private readonly IAlertaRepository _alertaRepo;
        private readonly IEmailService _emailService;
        private readonly Proyecto1SlaDbContext _context;

        public EmailAutomationService(
            IEmailConfigRepository configRepo,
            IEmailLogRepository logRepo,
            IAlertaRepository alertaRepo,
            IEmailService emailService,
            Proyecto1SlaDbContext context)
        {
            _configRepo = configRepo;
            _logRepo = logRepo;
            _alertaRepo = alertaRepo;
            _emailService = emailService;
            _context = context;
        }

        #region Dashboard

        public async Task<List<AlertaDashboardDto>> GetDashboardAlertsAsync()
        {
            var alertas = await _alertaRepo.GetAlertasAsync();
            var dashboard = new List<AlertaDashboardDto>();

            foreach (var alerta in alertas)
            {
                if (alerta.IdSolicitudNavigation == null) continue;

                var solicitud = alerta.IdSolicitudNavigation;
                var personal = solicitud.IdPersonalNavigation;
                var sla = solicitud.IdSlaNavigation;

                // Calcular fechas
                var fechaInicio = solicitud.FechaIngreso ?? solicitud.FechaSolicitud;
                var diasSla = sla?.DiasUmbral ?? solicitud.NumDiasSla ?? 30;
                var fechaVencimiento = fechaInicio.AddDays(diasSla);

                // Cálculos SLA
                var hoy = DateOnly.FromDateTime(DateTime.Now);
                var diasTranscurridos = hoy.DayNumber - fechaInicio.DayNumber;
                var diasRestantes = fechaVencimiento.DayNumber - hoy.DayNumber;

                // Porcentaje de progreso
                double porcentajeProgreso = 0;
                if (diasSla > 0)
                {
                    porcentajeProgreso = ((double)diasTranscurridos / diasSla) * 100;
                    porcentajeProgreso = Math.Max(0, Math.Min(100, porcentajeProgreso));
                }

                // Determinar color según días restantes
                string color;
                if (diasRestantes < 0)
                {
                    color = "#dc3545"; // Rojo - Vencido
                }
                else if (diasRestantes <= 2)
                {
                    color = "#ffc107"; // Amarillo - Crítico
                }
                else if (diasRestantes <= 5)
                {
                    color = "#fd7e14"; // Naranja - Advertencia
                }
                else
                {
                    color = "#28a745"; // Verde - Normal
                }

                dashboard.Add(new AlertaDashboardDto
                {
                    IdAlerta = alerta.IdAlerta,
                    IdSolicitud = alerta.IdSolicitud,
                    NombreSolicitud = $"Sol-{solicitud.IdSolicitud}",
                    NombreResponsable = personal != null 
                        ? $"{personal.Nombres} {personal.Apellidos}".Trim() 
                        : "Sin asignar",
                    NombreSla = sla?.CodigoSla ?? "Sin SLA",
                    Nivel = alerta.Nivel ?? "MEDIO",
                    DiasTranscurridos = Math.Max(0, diasTranscurridos),
                    DiasRestantes = diasRestantes,
                    PorcentajeProgreso = porcentajeProgreso,
                    ColorEstado = color,
                    EstadoLectura = alerta.Estado ?? "NUEVA",
                    FechaCreacion = alerta.FechaCreacion,
                    FechaIngreso = solicitud.FechaIngreso,
                    FechaVencimiento = fechaVencimiento,
                    TipoAlerta = alerta.TipoAlerta,
                    Mensaje = alerta.Mensaje,
                    EnviadoEmail = alerta.EnviadoEmail,
                    CorreoResponsable = personal?.CorreoCorporativo
                });
            }

            return dashboard.OrderByDescending(d => d.FechaCreacion).ToList();
        }

        /// <summary>
        /// Obtiene alertas en formato compatible con el frontend Vue.js
        /// </summary>
        public async Task<List<AlertaDashboardFrontendDto>> GetDashboardAlertsFrontendAsync()
        {
            var alertas = await _alertaRepo.GetAlertasAsync();
            var resultado = new List<AlertaDashboardFrontendDto>();

            foreach (var alerta in alertas)
            {
                if (alerta.IdSolicitudNavigation == null) continue;

                var solicitud = alerta.IdSolicitudNavigation;
                var sla = solicitud.IdSlaNavigation;
                var rol = solicitud.IdRolRegistroNavigation;

                // Calcular fechas y métricas SLA
                var fechaInicio = solicitud.FechaIngreso ?? solicitud.FechaSolicitud;
                var diasSla = sla?.DiasUmbral ?? solicitud.NumDiasSla ?? 30;
                var fechaVencimiento = fechaInicio.AddDays(diasSla);

                var hoy = DateOnly.FromDateTime(DateTime.Now);
                var diasTranscurridos = hoy.DayNumber - fechaInicio.DayNumber;
                var diasRestantes = fechaVencimiento.DayNumber - hoy.DayNumber;

                // Calcular porcentaje de progreso (puede superar 100% si está vencido)
                double porcentajeProgreso = 0;
                if (diasSla > 0)
                {
                    porcentajeProgreso = ((double)diasTranscurridos / diasSla) * 100;
                    // NO limitar a 100% - permitir valores > 100 para mostrar vencidos
                    porcentajeProgreso = Math.Max(0, porcentajeProgreso);
                }

                resultado.Add(new AlertaDashboardFrontendDto
                {
                    IdAlerta = alerta.IdAlerta,
                    Nivel = alerta.Nivel ?? "MEDIO",
                    Estado = alerta.Estado ?? "NUEVA",
                    Mensaje = alerta.Mensaje,
                    FechaRegistro = alerta.FechaCreacion,
                    DiasRestantes = diasRestantes,
                    PorcentajeProgreso = Math.Round(porcentajeProgreso, 2),
                    Solicitud = new SolicitudDashboardDto
                    {
                        IdSolicitud = solicitud.IdSolicitud,
                        FechaSolicitud = solicitud.FechaSolicitud.ToDateTime(TimeOnly.MinValue),
                        Descripcion = solicitud.ResumenSla,
                        Estado = solicitud.EstadoSolicitud ?? "ACTIVA",
                        ConfigSla = sla != null ? new ConfigSlaDashboardDto
                        {
                            IdConfigSla = sla.IdSla,
                            NombreSla = sla.CodigoSla,
                            CodigoSla = sla.CodigoSla,
                            DiasUmbral = sla.DiasUmbral,
                            Descripcion = sla.Descripcion
                        } : null,
                        RolRegistro = rol != null ? new RolRegistroDashboardDto
                        {
                            IdRol = rol.IdRolRegistro,
                            NombreRol = rol.NombreRol,
                            Descripcion = rol.Descripcion
                        } : null
                    }
                });
            }

            return resultado.OrderByDescending(d => d.FechaRegistro).ToList();
        }

        #endregion

        #region Configuración

        public async Task<EmailConfigDto?> GetConfigAsync()
        {
            var config = await _configRepo.GetConfigAsync();
            if (config == null) return null;

            return new EmailConfigDto
            {
                Id = config.Id,
                EnvioInmediato = config.EnvioInmediato,
                ResumenDiario = config.ResumenDiario,
                HoraResumen = config.HoraResumen,
                EmailDestinatarioPrueba = config.EmailDestinatarioPrueba,
                CreadoEn = config.CreadoEn,
                ActualizadoEn = config.ActualizadoEn
            };
        }

        public async Task<EmailConfigDto> SaveConfigAsync(EmailConfigCreateUpdateDto dto)
        {
            var entity = new EmailConfig
            {
                EnvioInmediato = dto.EnvioInmediato,
                ResumenDiario = dto.ResumenDiario,
                HoraResumen = dto.HoraResumen,
                EmailDestinatarioPrueba = dto.EmailDestinatarioPrueba
            };

            var saved = await _configRepo.CreateOrUpdateAsync(entity);

            return new EmailConfigDto
            {
                Id = saved.Id,
                EnvioInmediato = saved.EnvioInmediato,
                ResumenDiario = saved.ResumenDiario,
                HoraResumen = saved.HoraResumen,
                EmailDestinatarioPrueba = saved.EmailDestinatarioPrueba,
                CreadoEn = saved.CreadoEn,
                ActualizadoEn = saved.ActualizadoEn
            };
        }

        #endregion

        #region Logs

        public async Task<List<EmailLogDto>> GetLogsAsync(int take = 50)
        {
            var logs = await _logRepo.GetLogsAsync(take);

            var usuarioIds = logs.Where(l => l.EjecutadoPor.HasValue)
                .Select(l => l.EjecutadoPor!.Value)
                .Distinct()
                .ToList();

            var usuarios = await _context.Usuario
                .Where(u => usuarioIds.Contains(u.IdUsuario))
                .ToDictionaryAsync(u => u.IdUsuario, u => u.Username);

            return logs.Select(l => new EmailLogDto
            {
                Id = l.Id,
                FechaEjecucion = l.FechaEjecucion,
                Tipo = l.Tipo,
                CantidadEnviados = l.CantidadEnviados,
                Estado = l.Estado,
                DetalleError = l.DetalleError,
                EjecutadoPor = l.EjecutadoPor,
                NombreUsuario = l.EjecutadoPor.HasValue && usuarios.ContainsKey(l.EjecutadoPor.Value)
                    ? usuarios[l.EjecutadoPor.Value]
                    : null
            }).ToList();
        }

        #endregion

        #region Broadcast (Envío Manual)

        public async Task<EmailLogDto> SendBroadcastAsync(BroadcastRequestDto dto)
        {
            var log = new EmailLog
            {
                Tipo = "MANUAL",
                EjecutadoPor = dto.EjecutadoPor,
                CantidadEnviados = 0,
                Estado = "EXITO"
            };

            try
            {
                // Obtener destinatarios según filtros
                var query = _context.Personal.AsQueryable();

                if (dto.FiltroRolId.HasValue)
                {
                    // Filtrar por rol
                    query = query.Where(p => p.Solicitud.Any(s => s.IdRolRegistro == dto.FiltroRolId.Value));
                }

                if (dto.FiltroSlaId.HasValue)
                {
                    // Filtrar por SLA
                    query = query.Where(p => p.Solicitud.Any(s => s.IdSla == dto.FiltroSlaId.Value));
                }

                var destinatarios = await query
                    .Where(p => !string.IsNullOrEmpty(p.CorreoCorporativo))
                    .Select(p => p.CorreoCorporativo!)
                    .Distinct()
                    .ToListAsync();

                if (!destinatarios.Any())
                {
                    log.Estado = "FALLO";
                    log.DetalleError = "No se encontraron destinatarios con los filtros especificados";
                    await _logRepo.CreateLogAsync(log);
                    
                    return new EmailLogDto
                    {
                        Id = log.Id,
                        FechaEjecucion = log.FechaEjecucion,
                        Tipo = log.Tipo,
                        CantidadEnviados = log.CantidadEnviados,
                        Estado = log.Estado,
                        DetalleError = log.DetalleError,
                        EjecutadoPor = log.EjecutadoPor
                    };
                }

                // Enviar correos
                int exitosos = 0;
                var errores = new List<string>();

                foreach (var email in destinatarios)
                {
                    try
                    {
                        await _emailService.SendAsync(email, dto.Asunto, dto.MensajeHtml);
                        exitosos++;
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"{email}: {ex.Message}");
                    }
                }

                log.CantidadEnviados = exitosos;

                if (exitosos == 0)
                {
                    log.Estado = "FALLO";
                    log.DetalleError = string.Join("; ", errores);
                }
                else if (errores.Any())
                {
                    log.Estado = "PARCIAL";
                    log.DetalleError = $"Errores en {errores.Count} destinatarios: {string.Join("; ", errores.Take(3))}";
                }

                await _logRepo.CreateLogAsync(log);

                return new EmailLogDto
                {
                    Id = log.Id,
                    FechaEjecucion = log.FechaEjecucion,
                    Tipo = log.Tipo,
                    CantidadEnviados = log.CantidadEnviados,
                    Estado = log.Estado,
                    DetalleError = log.DetalleError,
                    EjecutadoPor = log.EjecutadoPor
                };
            }
            catch (Exception ex)
            {
                log.Estado = "FALLO";
                log.DetalleError = $"Error general: {ex.Message}";
                await _logRepo.CreateLogAsync(log);

                return new EmailLogDto
                {
                    Id = log.Id,
                    FechaEjecucion = log.FechaEjecucion,
                    Tipo = log.Tipo,
                    CantidadEnviados = log.CantidadEnviados,
                    Estado = log.Estado,
                    DetalleError = log.DetalleError,
                    EjecutadoPor = log.EjecutadoPor
                };
            }
        }

        #endregion

        #region Resumen Diario

        public async Task SendDailyResumeAsync()
        {
            var config = await _configRepo.GetConfigAsync();
            if (config == null || string.IsNullOrWhiteSpace(config.EmailDestinatarioPrueba))
            {
                return; // No hay configuración o destinatario
            }

            var log = new EmailLog
            {
                Tipo = "RESUMEN",
                CantidadEnviados = 0,
                Estado = "EXITO"
            };

            try
            {
                var alertas = await GetDashboardAlertsAsync();
                var alertasActivas = alertas.Where(a => a.EstadoLectura == "NUEVA").ToList();

                var subject = $"Resumen Diario de Alertas SLA - {DateTime.Now:dd/MM/yyyy}";
                var body = BuildDailyResumeHtml(alertasActivas);

                await _emailService.SendAsync(config.EmailDestinatarioPrueba, subject, body);

                log.CantidadEnviados = 1;
                log.Estado = "EXITO";
            }
            catch (Exception ex)
            {
                log.Estado = "FALLO";
                log.DetalleError = ex.Message;
            }

            await _logRepo.CreateLogAsync(log);
        }

        private string BuildDailyResumeHtml(List<AlertaDashboardDto> alertas)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .alert {{ margin: 10px 0; padding: 15px; border-left: 4px solid; background-color: #f8f9fa; }}
        .alert-critico {{ border-color: #dc3545; }}
        .alert-alto {{ border-color: #ffc107; }}
        .footer {{ margin-top: 30px; padding: 15px; background-color: #f8f9fa; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Resumen Diario de Alertas SLA</h2>
        <p>{DateTime.Now:dddd, dd 'de' MMMM 'de' yyyy}</p>
    </div>
    <div class='content'>
        <h3>Total de Alertas Activas: {alertas.Count}</h3>
        <p>A continuación se muestra el resumen de las alertas pendientes:</p>";

            if (!alertas.Any())
            {
                html += "<p><strong>No hay alertas activas.</strong></p>";
            }
            else
            {
                foreach (var alerta in alertas.Take(10))
                {
                    var cssClass = alerta.DiasRestantes < 0 ? "alert-critico" : "alert-alto";
                    html += $@"
        <div class='alert {cssClass}'>
            <strong>{alerta.NombreSolicitud}</strong> - {alerta.NombreSla}<br>
            Responsable: {alerta.NombreResponsable}<br>
            Mensaje: {alerta.Mensaje}<br>
            Días restantes: <strong>{alerta.DiasRestantes}</strong> | Progreso: {alerta.PorcentajeProgreso:F1}%
        </div>";
                }

                if (alertas.Count > 10)
                {
                    html += $"<p><em>...y {alertas.Count - 10} alertas más.</em></p>";
                }
            }

            html += @"
    </div>
    <div class='footer'>
        <p>Este es un correo automático generado por el sistema de gestión de SLA.</p>
        <p>Por favor, no responda a este mensaje.</p>
    </div>
</body>
</html>";

            return html;
        }

        #endregion
    }
}
