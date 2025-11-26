using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infrastructure.Data;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class SolicitudService : ISolicitudService
    {
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IAlertaRepository _alertaRepository;
        private readonly IEmailService _emailService;
        private readonly Proyecto1SlaDbContext _context;

        public SolicitudService(
            ISolicitudRepository solicitudRepository,
            IAlertaRepository alertaRepository,
            IEmailService emailService,
            Proyecto1SlaDbContext context)
        {
            _solicitudRepository = solicitudRepository;
            _alertaRepository = alertaRepository;
            _emailService = emailService;
            _context = context;
        }

        // Get all solicitudes with dtos
        // GET ALL
        public async Task<List<SolicitudDto>> GetAllAsync()
        {
            var entities = await _solicitudRepository.GetSolicitudsAsync();

            var result = entities.Select(s => new SolicitudDto
            {
                IdSolicitud = s.IdSolicitud,
                EstadoSolicitud = s.EstadoSolicitud,
                EstadoCumplimientoSla = s.EstadoCumplimientoSla,
                ResumenSla = s.ResumenSla,
                OrigenDato = s.OrigenDato,
                FechaSolicitud = s.FechaSolicitud,
                FechaIngreso = s.FechaIngreso,
                NumDiasSla = s.NumDiasSla,
                CreadoEn = s.CreadoEn,
                ActualizadoEn = s.ActualizadoEn,

                IdPersonal = s.IdPersonal,
                IdSla = s.IdSla,
                IdRolRegistro = s.IdRolRegistro,
                CreadoPor = s.CreadoPor,

                Personal = s.IdPersonalNavigation == null ? null : new PersonalDto
                {
                    IdPersonal = s.IdPersonalNavigation.IdPersonal,
                    Nombres = s.IdPersonalNavigation.Nombres,
                    Apellidos = s.IdPersonalNavigation.Apellidos,
                    CorreoCorporativo = s.IdPersonalNavigation.CorreoCorporativo
                },

                ConfigSla = s.IdSlaNavigation == null ? null : new ConfigSlaDto
                {
                    IdSla = s.IdSlaNavigation.IdSla,
                    CodigoSla = s.IdSlaNavigation.CodigoSla,
                    TipoSolicitud = s.IdSlaNavigation.TipoSolicitud
                },

                RolRegistro = s.IdRolRegistroNavigation == null ? null : new RolRegistroDto
                {
                    IdRolRegistro = s.IdRolRegistroNavigation.IdRolRegistro,
                    NombreRol = s.IdRolRegistroNavigation.NombreRol
                },

                CreadoPorUsuario = s.CreadoPorNavigation == null ? null : new UsuarioDto
                {
                    IdUsuario = s.CreadoPorNavigation.IdUsuario,
                    Username = s.CreadoPorNavigation.Username,
                    Correo = s.CreadoPorNavigation.Correo
                },

                Alertas = s.Alerta.Select(a => new AlertaDto
                {
                    IdAlerta = a.IdAlerta,
                    TipoAlerta = a.TipoAlerta,
                    Nivel = a.Nivel,
                    Mensaje = a.Mensaje,
                    Estado = a.Estado,
                    FechaCreacion = a.FechaCreacion
                }).ToList(),

                Reportes = s.IdReporte.Select(r => new ReporteDto
                {
                    IdReporte = r.IdReporte,
                    TipoReporte = r.TipoReporte,
                    Formato = r.Formato
                }).ToList()
            }).ToList();

            return result;
        }

        // GET BY ID
        public async Task<SolicitudDto?> GetByIdAsync(int id)
        {
            var s = await _solicitudRepository.GetSolicitudByIdAsync(id);
            if (s == null) return null;

            return new SolicitudDto
            {
                IdSolicitud = s.IdSolicitud,
                EstadoSolicitud = s.EstadoSolicitud,
                EstadoCumplimientoSla = s.EstadoCumplimientoSla,
                ResumenSla = s.ResumenSla,
                OrigenDato = s.OrigenDato,
                FechaSolicitud = s.FechaSolicitud,
                FechaIngreso = s.FechaIngreso,
                NumDiasSla = s.NumDiasSla,
                CreadoEn = s.CreadoEn,
                ActualizadoEn = s.ActualizadoEn,
                IdPersonal = s.IdPersonal,
                IdSla = s.IdSla,
                IdRolRegistro = s.IdRolRegistro,
                CreadoPor = s.CreadoPor,
                Personal = s.IdPersonalNavigation == null ? null : new PersonalDto
                {
                    IdPersonal = s.IdPersonalNavigation.IdPersonal,
                    Nombres = s.IdPersonalNavigation.Nombres,
                    Apellidos = s.IdPersonalNavigation.Apellidos,
                    CorreoCorporativo = s.IdPersonalNavigation.CorreoCorporativo
                },
                ConfigSla = s.IdSlaNavigation == null ? null : new ConfigSlaDto
                {
                    IdSla = s.IdSlaNavigation.IdSla,
                    CodigoSla = s.IdSlaNavigation.CodigoSla,
                    TipoSolicitud = s.IdSlaNavigation.TipoSolicitud
                },
                RolRegistro = s.IdRolRegistroNavigation == null ? null : new RolRegistroDto
                {
                    IdRolRegistro = s.IdRolRegistroNavigation.IdRolRegistro,
                    NombreRol = s.IdRolRegistroNavigation.NombreRol
                },
                CreadoPorUsuario = s.CreadoPorNavigation == null ? null : new UsuarioDto
                {
                    IdUsuario = s.CreadoPorNavigation.IdUsuario,
                    Username = s.CreadoPorNavigation.Username,
                    Correo = s.CreadoPorNavigation.Correo
                },
                Alertas = s.Alerta.Select(a => new AlertaDto
                {
                    IdAlerta = a.IdAlerta,
                    TipoAlerta = a.TipoAlerta,
                    Nivel = a.Nivel,
                    Mensaje = a.Mensaje,
                    Estado = a.Estado,
                    FechaCreacion = a.FechaCreacion
                }).ToList(),
                Reportes = s.IdReporte.Select(r => new ReporteDto
                {
                    IdReporte = r.IdReporte,
                    TipoReporte = r.TipoReporte,
                    Formato = r.Formato
                }).ToList()
            };


        }
        // POST
        // POST: crear solicitud, alerta y enviar correo si es crítico
        public async Task<SolicitudDto> CreateAsync(SolicitudCreateDto dto)
        {
            // Usar transacción para garantizar integridad de datos
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. leer SLA
                var configSla = await _solicitudRepository.GetConfigSlaByIdAsync(dto.IdSla);
                if (configSla == null)
                    throw new ArgumentException($"No existe configuración SLA con Id={dto.IdSla}");

                // 2. calcular días entre fechas
                var fechaSolicitudDate = dto.FechaSolicitud.Date;
                var fechaIngresoDate = dto.FechaIngreso.Date;
                var dias = (fechaIngresoDate - fechaSolicitudDate).TotalDays;
                if (dias < 0) throw new ArgumentException("FechaIngreso debe ser posterior o igual a FechaSolicitud");

                var numDias = (int)Math.Ceiling(dias);

                // 3. determinar cumplimiento
                var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla) ? $"SLA{configSla.IdSla}" : configSla.CodigoSla;
                var cumple = numDias <= configSla.DiasUmbral;
                var estadoCumplimiento = cumple ? $"CUMPLE {codigo}" : $"NO CUMPLE {codigo}";

                // 4. crear solicitud
                var entity = new Solicitud
                {
                    IdPersonal = dto.IdPersonal,
                    IdSla = dto.IdSla,
                    IdRolRegistro = dto.IdRolRegistro,
                    CreadoPor = dto.CreadoPor,
                    FechaSolicitud = DateOnly.FromDateTime(dto.FechaSolicitud),
                    FechaIngreso = DateOnly.FromDateTime(dto.FechaIngreso),
                    NumDiasSla = numDias,
                    ResumenSla = dto.ResumenSla,
                    OrigenDato = dto.OrigenDato,
                    EstadoSolicitud = dto.EstadoSolicitud ?? "ACTIVO",
                    EstadoCumplimientoSla = estadoCumplimiento,
                    CreadoEn = DateTime.UtcNow
                };

                var created = await _solicitudRepository.CreateSolicitudAsync(entity);

                // 5. CREAR ALERTA AUTOMÁTICAMENTE
                var diasUmbral = configSla.DiasUmbral;
                var diasRestantes = diasUmbral - numDias;
                
                // Determinar nivel de alerta según días restantes
                string nivelAlerta;
                bool esCritico = false;
                
                if (diasRestantes < 0)
                {
                    nivelAlerta = "CRITICO";
                    esCritico = true;
                }
                else if (diasRestantes <= 2)
                {
                    nivelAlerta = "CRITICO";
                    esCritico = true;
                }
                else if (diasRestantes <= 5)
                {
                    nivelAlerta = "ALTO";
                }
                else
                {
                    nivelAlerta = "MEDIO";
                }

                // Construir mensaje descriptivo
                var mensaje = diasRestantes < 0
                    ? $"⚠️ URGENTE: Solicitud #{created.IdSolicitud} VENCIDA. Se excedió el SLA por {Math.Abs(diasRestantes)} día(s)."
                    : diasRestantes == 0
                        ? $"⚠️ ATENCIÓN: Solicitud #{created.IdSolicitud} vence HOY. Requiere acción inmediata."
                        : diasRestantes <= 2
                            ? $"⚠️ CRÍTICO: Solicitud #{created.IdSolicitud} está cerca de vencer el SLA. Quedan solo {diasRestantes} día(s)."
                            : $"Solicitud #{created.IdSolicitud} creada. Vencimiento en {diasRestantes} día(s) (SLA: {diasUmbral} días).";

                var alerta = new Alerta
                {
                    IdSolicitud = created.IdSolicitud,
                    TipoAlerta = "NUEVA_ASIGNACION",
                    Nivel = nivelAlerta,
                    Mensaje = mensaje,
                    Estado = "NUEVA",
                    EnviadoEmail = false,
                    FechaCreacion = DateTime.UtcNow
                };

                var alertaCreada = await _alertaRepository.CreateAlertaAsync(alerta);

                // 6. ENVIAR CORREO AUTOMÁTICAMENTE SI ES CRÍTICO
                if (esCritico)
                {
                    // Obtener la alerta completa con todas las relaciones
                    var alertaCompleta = await _alertaRepository.GetAlertaByIdAsync(alertaCreada.IdAlerta);
                    
                    if (alertaCompleta != null)
                    {
                        var destinatario = alertaCompleta.IdSolicitudNavigation?.IdPersonalNavigation?.CorreoCorporativo;

                        if (!string.IsNullOrWhiteSpace(destinatario))
                        {
                            try
                            {
                                var subject = $"🚨 [ALERTA CRÍTICA SLA] Solicitud #{created.IdSolicitud} requiere atención URGENTE";
                                var body = EmailTemplates.BuildAlertaBody(alertaCompleta);

                                await _emailService.SendAsync(destinatario, subject, body);

                                // Marcar como enviado
                                alertaCompleta.EnviadoEmail = true;
                                alertaCompleta.ActualizadoEn = DateTime.UtcNow;
                                await _alertaRepository.UpdateAlertaAsync(alertaCompleta.IdAlerta, alertaCompleta);

                                System.Diagnostics.Debug.WriteLine($"✅ Correo enviado exitosamente a {destinatario} para solicitud crítica #{created.IdSolicitud}");
                            }
                            catch (Exception ex)
                            {
                                // Log pero NO fallar la transacción
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error al enviar correo a {destinatario}: {ex.Message}");
                                // La alerta se guarda pero EnviadoEmail queda en false
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Alerta crítica {alertaCompleta.IdAlerta} creada pero sin correo de destinatario.");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ Alerta {nivelAlerta} creada para solicitud #{created.IdSolicitud}. Correo no enviado (solo se envían alertas CRÍTICAS automáticamente).");
                }

                // 7. Commit de la transacción si todo salió bien
                await transaction.CommitAsync();

                // 8. devolver con includes
                return await GetByIdAsync(created.IdSolicitud)
                       ?? throw new Exception("No se pudo obtener la solicitud creada");
            }
            catch (Exception)
            {
                // Rollback en caso de error
                await transaction.RollbackAsync();
                throw;
            }
        }


        // PUT
        // PUT: actualizar y recalcular SLA
        public async Task<SolicitudDto?> UpdateAsync(int id, SolicitudUpdateDto dto)
        {
            var configSla = await _solicitudRepository.GetConfigSlaByIdAsync(dto.IdSla);
            if (configSla == null)
                throw new ArgumentException($"No existe configuración SLA con Id={dto.IdSla}");

            var fechaSolicitudDate = dto.FechaSolicitud.Date;
            var fechaIngresoDate = dto.FechaIngreso.Date;
            var dias = (fechaIngresoDate - fechaSolicitudDate).TotalDays;
            if (dias < 0) throw new ArgumentException("FechaIngreso debe ser posterior o igual a FechaSolicitud");

            var numDias = (int)Math.Ceiling(dias);

            var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla) ? $"SLA{configSla.IdSla}" : configSla.CodigoSla;
            var cumple = numDias <= configSla.DiasUmbral;
            var estadoCumplimiento = cumple ? $"CUMPLE_{codigo}" : $"NO_CUMPLE_{codigo}";

            var entity = new Solicitud
            {
                IdSolicitud = id,
                IdPersonal = dto.IdPersonal,
                IdSla = dto.IdSla,
                IdRolRegistro = dto.IdRolRegistro,
                CreadoPor = dto.CreadoPor,
                FechaSolicitud = DateOnly.FromDateTime(dto.FechaSolicitud),
                FechaIngreso = DateOnly.FromDateTime(dto.FechaIngreso),
                NumDiasSla = numDias,
                ResumenSla = dto.ResumenSla,
                OrigenDato = dto.OrigenDato,
                EstadoSolicitud = dto.EstadoSolicitud,
                EstadoCumplimientoSla = estadoCumplimiento,
                ActualizadoEn = DateTime.UtcNow
            };

            var updated = await _solicitudRepository.UpdateSolicitudAsync(id, entity);
            if (updated == null) return null;

            return await GetByIdAsync(id);
        }

        // tus GETs que ya hiciste van aquí (no los repito)
        // tu DeleteAsync también


        // DELETE (lógico)
        public async Task<bool> DeleteAsync(int id)
        {
            return await _solicitudRepository.DeleteSolicitudAsync(id, "ELIMINADO");
        }

    }
}
