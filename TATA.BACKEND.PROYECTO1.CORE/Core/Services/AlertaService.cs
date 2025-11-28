using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly IAlertaRepository _RepositoryAlerta;
        private readonly IEmailService _emailService;

        public AlertaService(IAlertaRepository repositoryAlerta, IEmailService emailService)
        {
            _RepositoryAlerta = repositoryAlerta;
            _emailService = emailService;
        }

        // NUEVO MÉTODO: Dashboard con cálculos de SLA
        public async Task<List<AlertaDashboardDto>> GetDashboardAsync()
        {
            var entities = await _RepositoryAlerta.GetAlertasAsync();
            var today = DateTime.Today;
            var todayDateOnly = DateOnly.FromDateTime(today);

            return entities
                .Where(a => a.Estado != "ELIMINADA") // Excluir alertas eliminadas
                .Select(a =>
                {
                    var solicitud = a.IdSolicitudNavigation;
                    var configSla = solicitud?.IdSlaNavigation;
                    var personal = solicitud?.IdPersonalNavigation;
                    var rolRegistro = solicitud?.IdRolRegistroNavigation;

                    // Cálculo de fechas y días
                    var fechaInicio = solicitud != null ? solicitud.FechaSolicitud : todayDateOnly;
                    var diasUmbral = configSla?.DiasUmbral ?? 0;
                    var fechaVencimiento = fechaInicio.AddDays(diasUmbral);
                    
                    // Calcular días transcurridos y restantes
                    var diasTranscurridos = (todayDateOnly.DayNumber - fechaInicio.DayNumber);
                    var diasRestantes = (fechaVencimiento.DayNumber - todayDateOnly.DayNumber);
                    
                    // Calcular porcentaje de progreso (puede superar 100%)
                    double porcentajeProgreso = 0;
                    if (diasUmbral > 0)
                    {
                        porcentajeProgreso = Math.Round((double)diasTranscurridos / diasUmbral * 100, 2);
                        // NO limitar a 100% - permitir valores mayores para mostrar vencidos
                    }
                    
                    // Determinar color según estado
                    string colorEstado;
                    if (diasRestantes < 0)
                    {
                        // VENCIDO - Rojo
                        colorEstado = "#dc3545";
                    }
                    else if (diasRestantes <= 2 || a.Nivel == "CRITICO")
                    {
                        // CRÍTICO - Amarillo/Naranja
                        colorEstado = "#ffc107";
                    }
                    else if (diasRestantes <= 5 || a.Nivel == "ALTO")
                    {
                        // ALTO - Naranja claro
                        colorEstado = "#fd7e14";
                    }
                    else
                    {
                        // NORMAL - Verde
                        colorEstado = "#28a745";
                    }

                    return new AlertaDashboardDto
                    {
                        IdAlerta = a.IdAlerta,
                        IdSolicitud = a.IdSolicitud,
                        NombreSolicitud = solicitud?.ResumenSla ?? "Sin descripción",
                        NombreResponsable = personal != null 
                            ? $"{personal.Nombres} {personal.Apellidos}".Trim() 
                            : "Sin asignar",
                        NombreSla = configSla?.CodigoSla ?? "Sin SLA",
                        Nivel = a.Nivel ?? "MEDIO",
                        DiasTranscurridos = diasTranscurridos,
                        DiasRestantes = diasRestantes,
                        PorcentajeProgreso = porcentajeProgreso,
                        ColorEstado = colorEstado,
                        EstadoLectura = a.Estado ?? "NUEVA",
                        FechaCreacion = a.FechaCreacion,
                        FechaIngreso = solicitud?.FechaIngreso,
                        FechaVencimiento = fechaVencimiento,
                        TipoAlerta = a.TipoAlerta ?? "GENERAL",
                        Mensaje = a.Mensaje ?? "",
                        EnviadoEmail = a.EnviadoEmail,
                        CorreoResponsable = personal?.CorreoCorporativo
                    };
                })
                .OrderByDescending(x => x.FechaCreacion)
                .ToList();
        }

        // Get alerta 
        public async Task<List<AlertaDTO>> GetAllAsync()
        {
            var entities = await _RepositoryAlerta.GetAlertasAsync();

            return entities.Select(a => new AlertaDTO
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
            }).ToList();
        }

        public async Task<AlertaDTO?> GetByIdAsync(int id)
        {
            var a = await _RepositoryAlerta.GetAlertaByIdAsync(id);
            if (a == null) return null;

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

        // ADD ALERTA with 
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

            var created = await _RepositoryAlerta.CreateAlertaAsync(entity);
            var alertaFull = await _RepositoryAlerta.GetAlertaByIdAsync(created.IdAlerta);
            if (alertaFull == null)
                throw new Exception("No se pudo recuperar la alerta creada.");

            // 3) sacar el correo del destinatario
            var destinatario = alertaFull.IdSolicitudNavigation?.IdPersonalNavigation?.CorreoCorporativo;

            // 4) AQUÍ se manda elcorreo con manejo de errores
            if (!string.IsNullOrWhiteSpace(destinatario))
            {
                try
                {
                    var subject = $"[ALERTA SLA] {alertaFull.TipoAlerta} ({alertaFull.Nivel})";
                    var body = EmailTemplates.BuildAlertaBody(alertaFull);

                    await _emailService.SendAsync(destinatario, subject, body);

                    // 5) marcar como enviado en BD via UpdateAlertaAsync
                    alertaFull.EnviadoEmail = true;
                    alertaFull.ActualizadoEn = DateTime.UtcNow;
                    await _RepositoryAlerta.UpdateAlertaAsync(alertaFull.IdAlerta, alertaFull);
                }
                catch (Exception ex)
                {
                    // Log del error (podrías agregar un logger aquí)
                    System.Diagnostics.Debug.WriteLine($"Error al enviar correo a {destinatario}: {ex.Message}");
                    
                    // La alerta se creó pero el correo falló - esto es informativo
                    // No lanzamos excepción para no fallar toda la operación
                    // EnviadoEmail queda en false
                }
            }
            else
            {
                // Advertencia: no hay correo del destinatario
                System.Diagnostics.Debug.WriteLine($"Advertencia: Alerta {alertaFull.IdAlerta} creada pero sin correo de destinatario para notificar.");
            }

            return new AlertaDTO
            {
                IdAlerta = alertaFull.IdAlerta,
                IdSolicitud = alertaFull.IdSolicitud,
                TipoAlerta = alertaFull.TipoAlerta,
                Nivel = alertaFull.Nivel,
                Mensaje = alertaFull.Mensaje,
                Estado = alertaFull.Estado,
                EnviadoEmail = alertaFull.EnviadoEmail,
                FechaCreacion = alertaFull.FechaCreacion,
                FechaLectura = alertaFull.FechaLectura,
                ActualizadoEn = alertaFull.ActualizadoEn,
                Solicitud = alertaFull.IdSolicitudNavigation == null ? null : new AlertaSolicitudInfoDto
                {
                    IdSolicitud = alertaFull.IdSolicitudNavigation.IdSolicitud,
                    FechaSolicitud = alertaFull.IdSolicitudNavigation.FechaSolicitud,
                    FechaIngreso = alertaFull.IdSolicitudNavigation.FechaIngreso,
                    NumDiasSla = alertaFull.IdSolicitudNavigation.NumDiasSla,
                    ResumenSla = alertaFull.IdSolicitudNavigation.ResumenSla,
                    EstadoSolicitud = alertaFull.IdSolicitudNavigation.EstadoSolicitud,
                    EstadoCumplimientoSla = alertaFull.IdSolicitudNavigation.EstadoCumplimientoSla,
                    Personal = alertaFull.IdSolicitudNavigation.IdPersonalNavigation == null ? null : new AlertaSolicitudPersonalDto
                    {
                        IdPersonal = alertaFull.IdSolicitudNavigation.IdPersonalNavigation.IdPersonal,
                        Nombres = alertaFull.IdSolicitudNavigation.IdPersonalNavigation.Nombres,
                        Apellidos = alertaFull.IdSolicitudNavigation.IdPersonalNavigation.Apellidos,
                        CorreoCorporativo = alertaFull.IdSolicitudNavigation.IdPersonalNavigation.CorreoCorporativo
                    },
                    RolRegistro = alertaFull.IdSolicitudNavigation.IdRolRegistroNavigation == null ? null : new AlertaSolicitudRolDto
                    {
                        IdRolRegistro = alertaFull.IdSolicitudNavigation.IdRolRegistroNavigation.IdRolRegistro,
                        NombreRol = alertaFull.IdSolicitudNavigation.IdRolRegistroNavigation.NombreRol
                    },
                    ConfigSla = alertaFull.IdSolicitudNavigation.IdSlaNavigation == null ? null : new AlertaSolicitudSlaDto
                    {
                        IdSla = alertaFull.IdSolicitudNavigation.IdSlaNavigation.IdSla,
                        CodigoSla = alertaFull.IdSolicitudNavigation.IdSlaNavigation.CodigoSla,
                        DiasUmbral = alertaFull.IdSolicitudNavigation.IdSlaNavigation.DiasUmbral,
                        TipoSolicitud = alertaFull.IdSolicitudNavigation.IdSlaNavigation.TipoSolicitud
                    }
                }
            };
        }

        // PUT: actualizar alerta
        public async Task<AlertaDTO?> UpdateAsync(int id, AlertaUpdateDto dto)
        {
            // 1. traer la alerta actual
            var existing = await _RepositoryAlerta.GetAlertaByIdAsync(id);
            if (existing == null) return null;

            // Detectar si hay cambios significativos que requieren notificación
            bool hasSignificantChanges = false;

            // 2. aplicar solo lo que viene en el dto
            if (!string.IsNullOrWhiteSpace(dto.TipoAlerta) && dto.TipoAlerta != existing.TipoAlerta)
            {
                existing.TipoAlerta = dto.TipoAlerta;
                hasSignificantChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Nivel) && dto.Nivel != existing.Nivel)
            {
                existing.Nivel = dto.Nivel;
                hasSignificantChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Mensaje) && dto.Mensaje != existing.Mensaje)
            {
                existing.Mensaje = dto.Mensaje;
                hasSignificantChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Estado) && dto.Estado != existing.Estado)
            {
                existing.Estado = dto.Estado;
                // Cambio de estado no siempre es significativo para notificar
            }

            // Lógica de envío de correo CORREGIDA (sin duplicación):
            bool shouldSendEmail;
            
            if (dto.EnviadoEmail.HasValue)
            {
                // El usuario especificó explícitamente si quiere enviar o no
                shouldSendEmail = dto.EnviadoEmail.Value;
                existing.EnviadoEmail = dto.EnviadoEmail.Value;
            }
            else
            {
                // No se especificó, usar lógica automática
                shouldSendEmail = hasSignificantChanges && !existing.EnviadoEmail;
            }

            existing.ActualizadoEn = DateTime.UtcNow;

            // 3. actualizar en BD
            var updated = await _RepositoryAlerta.UpdateAlertaAsync(id, existing);
            if (updated == null) return null;

            // 4. si se debe enviar correo y hay destinatario
            if (shouldSendEmail)
            {
                var destinatario = updated.IdSolicitudNavigation?.IdPersonalNavigation?.CorreoCorporativo;
                
                if (string.IsNullOrWhiteSpace(destinatario))
                {
                    // Advertencia: se pidió enviar email pero no hay correo válido
                    System.Diagnostics.Debug.WriteLine($"Advertencia: Se solicitó enviar correo para alerta {id} pero no hay correo de destinatario.");
                    throw new InvalidOperationException($"No se puede enviar el correo: el personal vinculado a la solicitud no tiene un correo corporativo registrado.");
                }

                try
                {
                    var subject = $"[ALERTA SLA ACTUALIZADA] {updated.TipoAlerta} ({updated.Nivel})";
                    var body = EmailTemplates.BuildAlertaBody(updated);

                    await _emailService.SendAsync(destinatario, subject, body);

                    // marcar como enviado y persistir
                    updated.EnviadoEmail = true;
                    updated.ActualizadoEn = DateTime.UtcNow;
                    await _RepositoryAlerta.UpdateAlertaAsync(updated.IdAlerta, updated);
                }
                catch (ArgumentException argEx)
                {
                    // Error de validación de correo (formato inválido, etc.)
                    System.Diagnostics.Debug.WriteLine($"Error de validación al enviar correo a {destinatario}: {argEx.Message}");
                    throw new InvalidOperationException($"El correo '{destinatario}' no es válido o no tiene el formato correcto.", argEx);
                }
                catch (Exception ex)
                {
                    // Error general de envío de correo (SMTP, red, etc.)
                    System.Diagnostics.Debug.WriteLine($"Error al enviar correo a {destinatario}: {ex.Message}");
                    throw new InvalidOperationException($"No se pudo enviar el correo a '{destinatario}'. Verifique que el correo existe y que el servidor SMTP está configurado correctamente.", ex);
                }
            }

            // 5. devolver igual que en GetById (re-traer para obtener relaciones actualizadas)
            return await GetByIdAsync(id);
        }

        // DELETE: puede ser lógico si tu repo lo hace así
        public async Task<bool> DeleteAsync(int id)
        {
            // si tu repo hace "estado = ELIMINADA", esto ya vale
            var deleted = await _RepositoryAlerta.DeleteAlertaAsync(id);
            return deleted;
        }
    }
}
