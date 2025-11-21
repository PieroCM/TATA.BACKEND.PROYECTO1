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
        // instanciar interfaz de ISolicitudRepository
        private readonly ISolicitudRepository _solicitudRepository;

        public SolicitudService(ISolicitudRepository solicitudRepository)
        {
            _solicitudRepository = solicitudRepository;
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
        // POST: crear y calcular SLA
        public async Task<SolicitudDto> CreateAsync(SolicitudCreateDto dto)
        {
            // 1. leer SLA
            var configSla = await _solicitudRepository.GetConfigSlaByIdAsync(dto.IdSla);
            if (configSla == null)
                throw new ArgumentException($"No existe configuración SLA con Id={dto.IdSla}");

            // 2. calcular días entre fechas (asegurar fechas en UTC date-only)
            var fechaSolicitudDate = dto.FechaSolicitud.Date;
            var fechaIngresoDate = dto.FechaIngreso.Date;
            var dias = (fechaIngresoDate - fechaSolicitudDate).TotalDays;
            if (dias < 0) throw new ArgumentException("FechaIngreso debe ser posterior o igual a FechaSolicitud");

            var numDias = (int)Math.Ceiling(dias);

            // 3. determinar cumplimiento y compose message with codigo SLA
            var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla) ? $"SLA{configSla.IdSla}" : configSla.CodigoSla;
            var cumple = numDias <= configSla.DiasUmbral;
            var estadoCumplimiento = cumple ? $"CUMPLE {codigo}" : $"NO CUMPLE {codigo}";

            // 4. armar entidad (convertir DateTime -> DateOnly)
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

            // 5. guardar
            var created = await _solicitudRepository.CreateSolicitudAsync(entity);

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
