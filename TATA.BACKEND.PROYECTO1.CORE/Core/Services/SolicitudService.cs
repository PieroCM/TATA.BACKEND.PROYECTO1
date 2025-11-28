using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class SolicitudService : ISolicitudService
    {
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly IAlertaRepository _alertaRepository;

        // TimeZone de Perú para cálculo correcto de "hoy"
        private static readonly TimeZoneInfo PeruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public SolicitudService(
            ISolicitudRepository solicitudRepository,
            IAlertaRepository alertaRepository)
        {
            _solicitudRepository = solicitudRepository;
            _alertaRepository = alertaRepository;
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

            // Llamar al calculador común
            var hoyPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone).Date;
            var calc = CalcularSlaYResumen(dto.FechaSolicitud.Date, dto.FechaIngreso?.Date, configSla, hoyPeru);

            // Si DTO trae resumen personalizado, respetarlo
            var resumenFinal = string.IsNullOrWhiteSpace(dto.ResumenSla) ? calc.resumenSla : dto.ResumenSla;
            var estadoSolicitudFinal = string.IsNullOrWhiteSpace(dto.EstadoSolicitud) ? calc.estadoSolicitud : dto.EstadoSolicitud;

            // 4. armar entidad (convertir DateTime -> DateOnly)
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
                CreadoEn = DateTime.UtcNow
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
                    FechaCreacion = DateTime.UtcNow
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

            var hoyPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone).Date;
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

        // Método privado que encapsula la lógica de SLA usada en SubidaVolumenServices
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
                    // Ya venció el SLA
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    estadoSolicitud = "VENCIDO";
                    resumenSla = $"Solicitud INCUMPLIDA: se excedió el umbral del SLA ({diasTranscurridos} de {configSla.DiasUmbral} días)";
                }
                else
                {
                    // Aún dentro del plazo
                    estadoCumplimiento = $"EN_PROCESO_{codigo}";
                    estadoSolicitud = "EN_PROCESO";
                    resumenSla = $"Solicitud PENDIENTE dentro del SLA ({diasTranscurridos} de {configSla.DiasUmbral} días)";
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
                    estadoCumplimiento = $"CUMPLE_{codigo}";
                    resumenSla = $"Solicitud atendida dentro del SLA ({dias} de {configSla.DiasUmbral} días)";
                }
                else
                {
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    resumenSla = $"Solicitud atendida fuera del SLA ({dias} de {configSla.DiasUmbral} días)";
                }

                estadoSolicitud = "CERRADO";
            }

            return (numDiasSla, estadoCumplimiento, estadoSolicitud, resumenSla);
        }

    }
}
