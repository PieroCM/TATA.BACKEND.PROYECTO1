using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Infraestructure.Data;


namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly IRepositoryAlerta _RepositoryAlerta;
        private readonly IEmailService _emailService;

        public AlertaService(IRepositoryAlerta repositoryAlerta, IEmailService emailService)
        {
            _RepositoryAlerta = repositoryAlerta;
            _emailService = emailService;
        }

        // Get alerta 
        public async Task<List<AlertaDto>> GetAllAsync()
        {
            var entities = await _RepositoryAlerta.GetAlertasAsync();

            return entities.Select(a => new AlertaDto
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
        public async Task<AlertaDto?> GetByIdAsync(int id)
        {
            var a = await _RepositoryAlerta.GetAlertaByIdAsync(id);
            if (a == null) return null;

            return new AlertaDto
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
        public async Task<AlertaDto> CreateAsync(AlertaCreateDto dto)
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

            // 4) AQUÍ se manda el correo
            if (!string.IsNullOrWhiteSpace(destinatario))
            {
                var subject = $"[ALERTA SLA] {alertaFull.TipoAlerta} ({alertaFull.Nivel})";
                var body = EmailTemplates.BuildAlertaBody(alertaFull);

                await _emailService.SendAsync(destinatario, subject, body);

                // 5) marcar como enviado en BD via UpdateAlertaAsync
                alertaFull.EnviadoEmail = true;
                alertaFull.ActualizadoEn = DateTime.UtcNow;
                await _RepositoryAlerta.UpdateAlertaAsync(alertaFull.IdAlerta, alertaFull);
            }

            return new AlertaDto
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
        public async Task<AlertaDto?> UpdateAsync(int id, AlertaUpdateDto dto)
        {
            // 1. traer la alerta actual
            var existing = await _RepositoryAlerta.GetAlertaByIdAsync(id);
            if (existing == null) return null;

            // 2. aplicar solo lo que viene en el dto
            if (!string.IsNullOrWhiteSpace(dto.TipoAlerta))
                existing.TipoAlerta = dto.TipoAlerta;

            if (!string.IsNullOrWhiteSpace(dto.Nivel))
                existing.Nivel = dto.Nivel;

            if (!string.IsNullOrWhiteSpace(dto.Mensaje))
                existing.Mensaje = dto.Mensaje;

            if (!string.IsNullOrWhiteSpace(dto.Estado))
                existing.Estado = dto.Estado;

            if (dto.EnviadoEmail.HasValue)
                existing.EnviadoEmail = dto.EnviadoEmail.Value;

            existing.ActualizadoEn = DateTime.UtcNow;

            // 3. actualizar en BD
            var updated = await _RepositoryAlerta.UpdateAlertaAsync(id, existing);
            if (updated == null) return null;

            // 4. devolver igual que en GetById
            return new AlertaDto
            {
                IdAlerta = updated.IdAlerta,
                IdSolicitud = updated.IdSolicitud,
                TipoAlerta = updated.TipoAlerta,
                Nivel = updated.Nivel,
                Mensaje = updated.Mensaje,
                Estado = updated.Estado,
                EnviadoEmail = updated.EnviadoEmail,
                FechaCreacion = updated.FechaCreacion,
                FechaLectura = updated.FechaLectura,
                ActualizadoEn = updated.ActualizadoEn,
                Solicitud = updated.IdSolicitudNavigation == null ? null : new AlertaSolicitudInfoDto
                {
                    IdSolicitud = updated.IdSolicitudNavigation.IdSolicitud,
                    FechaSolicitud = updated.IdSolicitudNavigation.FechaSolicitud,
                    FechaIngreso = updated.IdSolicitudNavigation.FechaIngreso,
                    NumDiasSla = updated.IdSolicitudNavigation.NumDiasSla,
                    ResumenSla = updated.IdSolicitudNavigation.ResumenSla,
                    EstadoSolicitud = updated.IdSolicitudNavigation.EstadoSolicitud,
                    EstadoCumplimientoSla = updated.IdSolicitudNavigation.EstadoCumplimientoSla,
                    Personal = updated.IdSolicitudNavigation.IdPersonalNavigation == null ? null : new AlertaSolicitudPersonalDto
                    {
                        IdPersonal = updated.IdSolicitudNavigation.IdPersonalNavigation.IdPersonal,
                        Nombres = updated.IdSolicitudNavigation.IdPersonalNavigation.Nombres,
                        Apellidos = updated.IdSolicitudNavigation.IdPersonalNavigation.Apellidos,
                        CorreoCorporativo = updated.IdSolicitudNavigation.IdPersonalNavigation.CorreoCorporativo
                    },
                    RolRegistro = updated.IdSolicitudNavigation.IdRolRegistroNavigation == null ? null : new AlertaSolicitudRolDto
                    {
                        IdRolRegistro = updated.IdSolicitudNavigation.IdRolRegistroNavigation.IdRolRegistro,
                        NombreRol = updated.IdSolicitudNavigation.IdRolRegistroNavigation.NombreRol
                    },
                    ConfigSla = updated.IdSolicitudNavigation.IdSlaNavigation == null ? null : new AlertaSolicitudSlaDto
                    {
                        IdSla = updated.IdSolicitudNavigation.IdSlaNavigation.IdSla,
                        CodigoSla = updated.IdSolicitudNavigation.IdSlaNavigation.CodigoSla,
                        DiasUmbral = updated.IdSolicitudNavigation.IdSlaNavigation.DiasUmbral,
                        TipoSolicitud = updated.IdSolicitudNavigation.IdSlaNavigation.TipoSolicitud
                    }
                }
            };
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
