using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Shared;
using Microsoft.Extensions.Logging;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class SolicitudService : ISolicitudService
    {
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IAlertaRepository _alertaRepository;
        private readonly ILogger<SolicitudService> _logger;

        // TimeZone de Perú para cálculo correcto de "hoy"
        private static readonly TimeZoneInfo PeruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public SolicitudService(
            ISolicitudRepository solicitudRepository,
            IAlertaRepository alertaRepository,
            ILogger<SolicitudService> logger)
        {
            _solicitudRepository = solicitudRepository;
            _alertaRepository = alertaRepository;
            _logger = logger;
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
                    Correo = s.CreadoPorNavigation.PersonalNavigation?.CorreoCorporativo ?? s.CreadoPorNavigation.Username // ⚠️ Obtener de Personal
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
                    Correo = s.CreadoPorNavigation.PersonalNavigation?.CorreoCorporativo ?? s.CreadoPorNavigation.Username // ⚠️ Obtener de Personal
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
        // POST: crear y calcular SLA
        public async Task<SolicitudDto> CreateAsync(SolicitudCreateDto dto)
        {
            // 1. leer SLA
            var configSla = await _solicitudRepository.GetConfigSlaByIdAsync(dto.IdSla);
            if (configSla == null)
                throw new ArgumentException($"No existe configuración SLA con Id={dto.IdSla}");

            // Usar hora de Perú para el cálculo
            var hoyPeru = PeruTimeProvider.TodayPeru;
            var calc = CalcularSlaYResumen(dto.FechaSolicitud.Date, dto.FechaIngreso?.Date, configSla, hoyPeru);

            // Si DTO trae resumen personalizado, respetarlo
            var resumenFinal = string.IsNullOrWhiteSpace(dto.ResumenSla) ? calc.resumenSla : dto.ResumenSla;
            var estadoSolicitudFinal = string.IsNullOrWhiteSpace(dto.EstadoSolicitud) ? calc.estadoSolicitud : dto.EstadoSolicitud;

            // 4. armar entidad (convertir DateTime -> DateOnly)
            // Usar hora de Perú para CreadoEn
            var entity = new Solicitud
            {
                IdPersonal = dto.IdPersonal,
                IdSla = dto.IdSla,
                IdRolRegistro = dto.IdRolRegistro,
                CreadoPor = dto.CreadoPor,
                FechaSolicitud = DateOnly.FromDateTime(dto.FechaSolicitud),
                FechaIngreso = dto.FechaIngreso.HasValue ? DateOnly.FromDateTime(dto.FechaIngreso.Value) : null,
                NumDiasSla = calc.numDiasSla,
                ResumenSla = resumenFinal,
                OrigenDato = dto.OrigenDato,
                EstadoSolicitud = estadoSolicitudFinal,
                EstadoCumplimientoSla = calc.estadoCumplimientoSla,
                CreadoEn = PeruTimeProvider.NowPeru // ⚠️ Usar hora de Perú
            };

            // 5. TRANSACCIÓN: Guardar solicitud y crear alerta inicial
            Solicitud created;
            try
            {
                // Guardar la solicitud
                created = await _solicitudRepository.CreateSolicitudAsync(entity);

                // Crear alerta inicial vinculada
                var alertaInicial = new Alerta
                {
                    IdSolicitud = created.IdSolicitud,
                    TipoAlerta = "NUEVA",
                    Nivel = "INFO",
                    Mensaje = "Nueva solicitud creada",
                    Estado = "ACTIVA",
                    EnviadoEmail = false,
                    FechaCreacion = PeruTimeProvider.NowPeru // ⚠️ Usar hora de Perú
                };

                await _alertaRepository.CreateAlertaAsync(alertaInicial);
            }
            catch (Exception ex)
            {
                // Si falla algo, propagar el error
                throw new Exception($"Error al crear solicitud con alerta: {ex.Message}", ex);
            }

            // 6. devolver con includes
            return await GetByIdAsync(created.IdSolicitud)
                   ?? throw new Exception("No se pudo obtener la solicitud creada");
        }


        // PUT
        // PUT: actualizar y recalcular SLA
        public async Task<SolicitudDto?> UpdateAsync(int id, SolicitudUpdateDto dto)
        {
            var configSla = await _solicitudRepository.GetConfigSlaByIdAsync(dto.IdSla);
            if (configSla == null)
                throw new ArgumentException($"No existe configuración SLA con Id={dto.IdSla}");

            var hoyPeru = PeruTimeProvider.TodayPeru;
            var calc = CalcularSlaYResumen(dto.FechaSolicitud.Date, dto.FechaIngreso?.Date, configSla, hoyPeru);

            var resumenFinal = string.IsNullOrWhiteSpace(dto.ResumenSla) ? calc.resumenSla : dto.ResumenSla;

            var entity = new Solicitud
            {
                IdSolicitud = id,
                IdPersonal = dto.IdPersonal,
                IdSla = dto.IdSla,
                IdRolRegistro = dto.IdRolRegistro,
                CreadoPor = dto.CreadoPor,
                FechaSolicitud = DateOnly.FromDateTime(dto.FechaSolicitud),
                FechaIngreso = dto.FechaIngreso.HasValue ? DateOnly.FromDateTime(dto.FechaIngreso.Value) : null,
                NumDiasSla = calc.numDiasSla,
                ResumenSla = resumenFinal,
                OrigenDato = dto.OrigenDato,
                EstadoSolicitud = dto.EstadoSolicitud ?? calc.estadoSolicitud,
                EstadoCumplimientoSla = calc.estadoCumplimientoSla,
                ActualizadoEn = PeruTimeProvider.NowPeru // ⚠️ Usar hora de Perú
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

        /// <summary>
        /// Actualiza el SLA diario de todas las solicitudes activas o en proceso.
        /// Recalcula NumDiasSla, EstadoCumplimientoSla y EstadoSolicitud basándose en la fecha actual de Perú.
        /// Optimizado para manejar miles de registros eficientemente.
        /// </summary>
        /// <param name="fechaReferenciaPeru">Fecha de referencia en zona horaria de Perú para el cálculo de SLA</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de solicitudes actualizadas</returns>
        public async Task<int> ActualizarSlaDiarioAsync(DateTime fechaReferenciaPeru, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Iniciando actualización diaria de SLA. Fecha de referencia (Perú): {FechaPeru:yyyy-MM-dd HH:mm:ss}",
                fechaReferenciaPeru);

            var hoyPeru = fechaReferenciaPeru.Date;

            // Obtener solicitudes que necesitan recálculo
            var solicitudes = await _solicitudRepository.GetSolicitudesParaRecalculoAsync();

            _logger.LogDebug("Se encontraron {Total} solicitudes para recalcular SLA", solicitudes.Count);

            int actualizadas = 0;
            int errores = 0;

            foreach (var s in solicitudes)
            {
                // Verificar cancelación
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Actualización de SLA cancelada. Procesadas: {Actualizadas}, Pendientes: {Pendientes}",
                        actualizadas, solicitudes.Count - actualizadas - errores);
                    break;
                }

                try
                {
                    // Validar que tenga ConfigSla
                    if (s.IdSlaNavigation == null)
                    {
                        _logger.LogWarning("Solicitud {IdSolicitud} sin ConfigSla válido, se omite", s.IdSolicitud);
                        errores++;
                        continue;
                    }

                    // Convertir DateOnly a DateTime para cálculo
                    var fechaSol = s.FechaSolicitud.ToDateTime(TimeOnly.MinValue);
                    DateTime? fechaIng = s.FechaIngreso?.ToDateTime(TimeOnly.MinValue);

                    // Guardar valores originales para comparar cambios
                    var estadoSolicitudOriginal = s.EstadoSolicitud;
                    var estadoCumplimientoOriginal = s.EstadoCumplimientoSla;
                    var numDiasOriginal = s.NumDiasSla;

                    // Llamar al método de cálculo común
                    var calc = CalcularSlaYResumen(fechaSol, fechaIng, s.IdSlaNavigation, hoyPeru);

                    // Actualizar campos calculados
                    s.NumDiasSla = calc.numDiasSla;
                    s.EstadoCumplimientoSla = calc.estadoCumplimientoSla;
                    s.EstadoSolicitud = calc.estadoSolicitud;

                    // Actualizar ResumenSla solo si estaba vacío
                    if (string.IsNullOrWhiteSpace(s.ResumenSla))
                    {
                        s.ResumenSla = calc.resumenSla;
                    }

                    s.ActualizadoEn = fechaReferenciaPeru; // Usar la fecha de referencia

                    // Solo actualizar en BD si hubo cambios
                    var huboCambios = estadoSolicitudOriginal != s.EstadoSolicitud ||
                                     estadoCumplimientoOriginal != s.EstadoCumplimientoSla ||
                                     numDiasOriginal != s.NumDiasSla;

                    if (huboCambios)
                    {
                        await _solicitudRepository.UpdateSolicitudAsync(s.IdSolicitud, s);
                        actualizadas++;

                        _logger.LogDebug(
                            "Solicitud {IdSolicitud} actualizada: Estado={EstadoNuevo} (era {EstadoAnterior}), " +
                            "Cumplimiento={CumplimientoNuevo} (era {CumplimientoAnterior}), Días={DiasNuevo}",
                            s.IdSolicitud, s.EstadoSolicitud, estadoSolicitudOriginal,
                            s.EstadoCumplimientoSla, estadoCumplimientoOriginal, s.NumDiasSla);
                    }
                }
                catch (Exception ex)
                {
                    errores++;
                    _logger.LogError(ex, "Error al actualizar SLA de solicitud {IdSolicitud}", s.IdSolicitud);
                    // Continuar con las demás solicitudes
                }
            }

            _logger.LogInformation(
                "Actualización diaria de SLA finalizada. Total procesadas: {Total}, Actualizadas: {Actualizadas}, Errores: {Errores}",
                solicitudes.Count, actualizadas, errores);

            return actualizadas;
        }

        /// <summary>
        /// Método privado que encapsula la lógica de SLA.
        /// 
        /// ESTADOS DE SOLICITUD (EstadoSolicitud):
        /// - "EN_PROCESO": Sin fecha de ingreso y dentro del umbral SLA
        /// - "INACTIVA": Con fecha de ingreso y cumple el SLA (dentro del umbral)
        /// - "VENCIDA": Superó el umbral SLA (con o sin fecha de ingreso)
        /// </summary>
        private (int numDiasSla, string estadoCumplimientoSla, string estadoSolicitud, string resumenSla) CalcularSlaYResumen(
            DateTime fechaSolicitud, DateTime? fechaIngreso, ConfigSla configSla, DateTime hoyPeru)
        {
            int numDiasSla;
            string estadoCumplimiento;
            string estadoSolicitud;
            string resumenSla;

            var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla) ? $"SLA{configSla.IdSla}" : configSla.CodigoSla;

            // Caso A: Sin fecha de ingreso (pendiente/en proceso)
            if (!fechaIngreso.HasValue)
            {
                var diasTranscurridos = (int)Math.Floor((hoyPeru - fechaSolicitud).TotalDays);
                numDiasSla = diasTranscurridos;

                if (diasTranscurridos > configSla.DiasUmbral)
                {
                    // ❌ Ya venció el SLA -> VENCIDA
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    estadoSolicitud = "VENCIDA";
                    resumenSla = $"Solicitud VENCIDA: se excedió el umbral del SLA ({diasTranscurridos} de {configSla.DiasUmbral} días)";
                }
                else
                {
                    // ⏳ Aún dentro del plazo -> EN_PROCESO
                    if (numDiasSla < 0) numDiasSla = 0; // por seguridad
                    estadoCumplimiento = $"EN_PROCESO_{codigo}";
                    estadoSolicitud = "EN_PROCESO";
                    resumenSla = $"Solicitud PENDIENTE dentro del SLA ({numDiasSla} de {configSla.DiasUmbral} días)";
                }
            }
            // Caso B: Con fecha de ingreso (ya cerrada)
            else
            {
                if (fechaIngreso.Value < fechaSolicitud)
                    throw new ArgumentException("FechaIngreso debe ser posterior o igual a FechaSolicitud");

                var dias = (int)Math.Floor((fechaIngreso.Value - fechaSolicitud).TotalDays);
                numDiasSla = dias;

                if (dias <= configSla.DiasUmbral)
                {
                    // ✅ CUMPLE el SLA -> INACTIVA
                    estadoCumplimiento = $"CUMPLE_{codigo}";
                    estadoSolicitud = "INACTIVA";
                    resumenSla = $"Solicitud atendida dentro del SLA ({dias} de {configSla.DiasUmbral} días)";
                }
                else
                {
                    // ❌ NO CUMPLE el SLA -> VENCIDA
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    estadoSolicitud = "VENCIDA";
                    resumenSla = $"Solicitud atendida fuera del SLA ({dias} de {configSla.DiasUmbral} días)";
                }
            }

            return (numDiasSla, estadoCumplimiento, estadoSolicitud, resumenSla);
        }

    }
}
