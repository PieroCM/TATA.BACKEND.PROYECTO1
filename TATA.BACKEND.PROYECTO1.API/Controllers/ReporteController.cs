using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using System.Collections.Generic;
using System.Linq;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    public class SendEmailRequest
    {
        // admite uno (To) o varios (Tos)
        public string? To { get; set; } // un solo destinatario opcional
        public List<string>? Tos { get; set; } // múltiples destinatarios opcional
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
        public string? PdfBase64 { get; set; }
        public string? FileName { get; set; } = "reporte.pdf";
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ReporteController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ReporteController));
        
        private readonly IReporteService _reporteService;
        private readonly IEmailService _emailService;

        // límite básico para evitar adjuntos excesivos (10 MB)
        private const int MaxAttachmentBytes = 10 * 1024 * 1024;

        public ReporteController(IReporteService reporteService, IEmailService emailService)
        {
            _reporteService = reporteService;
            _emailService = emailService;
        }

        // GET: api/reportes
        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            log.Info("GetReportes iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetReportes",
                Detalles = "Obteniendo todos los reportes",
                IdUsuario = null
            });

            try
            {
                var entities = await _reporteService.GetAllAsync();
                var list = entities.Select(MapToDto).ToList();
                
                log.Info("GetReportes completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetReportes",
                    Detalles = $"Total reportes obtenidos: {list.Count}",
                    IdUsuario = null
                });
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetReportes", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/reportes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            log.Info($"GetReporteById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetReporteById {id}",
                Detalles = $"Buscando reporte con id: {id}",
                IdUsuario = null
            });

            try
            {
                var entity = await _reporteService.GetByIdAsync(id);
                
                if (entity == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetReporteById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetReporteById",
                    Detalles = $"Reporte {id} obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetReporteById para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reportes
        [AllowAnonymous] // si aún necesitas crear sin JWT; quítalo cuando todo requiera token
        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReporteCreateRequest request)
        {
            log.Info("CreateReporte iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: CreateReporte",
                Detalles = $"Creando reporte tipo: {request?.TipoReporte}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn("CreateReporte recibió request nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: request nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                log.Warn("CreateReporte: Validación de ModelState fallida");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
                return BadRequest(ModelState);
            }

            try
            {
                var entity = new Reporte
                {
                    TipoReporte = request.TipoReporte,
                    Formato = request.Formato,
                    FiltrosJson = request.FiltrosJson,
                    RutaArchivo = request.RutaArchivo,
                    GeneradoPor = request.GeneradoPor
                };

                await _reporteService.AddAsync(entity);

                log.Info($"CreateReporte completado correctamente, IdReporte: {entity.IdReporte}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: CreateReporte",
                    Detalles = $"Reporte creado con id: {entity.IdReporte}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetReporteById), new { id = entity.IdReporte }, MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante CreateReporte", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reportes/generar
        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarReporteRequest request)
        {
            log.Info("Generar iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Generar Reporte",
                Detalles = $"Generando reporte para {request?.IdsSolicitudes?.Count ?? 0} solicitudes",
                IdUsuario = null
            });

            if (request == null || request.IdsSolicitudes == null || !request.IdsSolicitudes.Any())
            {
                log.Warn("Generar: Solicitudes vacías o nulas");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: sin solicitudes",
                    Detalles = "Debe enviar al menos una solicitud",
                    IdUsuario = null
                });
                return BadRequest("Debes enviar al menos una solicitud.");
            }

            try
            {
                // Obtener id usuario desde el JWT (claim "UserId")
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var idUsuarioActual))
                {
                    log.Warn("Generar: Token sin UserId válido");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Validación fallida: Token sin UserId",
                        Detalles = "El token no contiene claim UserId válido",
                        IdUsuario = null
                    });
                    return Unauthorized("Token sin claim UserId válido.");
                }

                var reporte = await _reporteService.GenerarReporteAsync(request, idUsuarioActual);

                var dto = MapToDto(reporte);
                dto.GeneradoPorNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? "(sin_username)";

                log.Info($"Generar completado correctamente, IdReporte: {reporte.IdReporte}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Generar Reporte",
                    Detalles = $"Reporte generado con id: {reporte.IdReporte}",
                    IdUsuario = idUsuarioActual
                });

                return Ok(dto);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Generar", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // PUT: api/reportes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReporteUpdateRequest request)
        {
            log.Info($"UpdateReporte iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: UpdateReporte {id}",
                Detalles = $"Actualizando reporte con id: {id}",
                IdUsuario = null
            });

            if (request == null)
            {
                log.Warn($"UpdateReporte recibió request nulo para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: request nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"UpdateReporte: Validación de ModelState fallida para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: ModelState inválido",
                    Detalles = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    IdUsuario = null
                });
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _reporteService.GetByIdAsync(id);
                if (existing == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                existing.TipoReporte = request.TipoReporte;
                existing.Formato = request.Formato;
                existing.FiltrosJson = request.FiltrosJson;
                existing.RutaArchivo = request.RutaArchivo;
                existing.GeneradoPor = request.GeneradoPor;

                var ok = await _reporteService.UpdateAsync(existing);
                if (!ok)
                {
                    log.Warn($"No se pudo actualizar reporte {id}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"No se pudo actualizar reporte: {id}",
                        Detalles = "El servicio retornó false",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"UpdateReporte completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: UpdateReporte",
                    Detalles = $"Reporte {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante UpdateReporte para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/reportes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            log.Info($"DeleteReporte iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: DeleteReporte {id}",
                Detalles = $"Eliminando reporte con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _reporteService.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"Reporte con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Reporte no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"DeleteReporte completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: DeleteReporte",
                    Detalles = $"Reporte {id} eliminado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante DeleteReporte para id: {id}", ex);
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "ERROR",
                    Mensaje = ex.Message,
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reporte/enviar-correo
        [HttpPost("enviar-correo")] // api/reporte/enviar-correo
        [AllowAnonymous] // quita esto si deseas exigir JWT
        public async Task<IActionResult> EnviarCorreo([FromBody] SendEmailRequest req)
        {
            // normaliza destinatarios: usa To si existe, o la lista Tos
            var recipients = new List<string>();
            if (!string.IsNullOrWhiteSpace(req?.To))
                recipients.Add(req!.To!.Trim());
            if (req?.Tos != null && req.Tos.Count > 0)
                recipients.AddRange(req.Tos.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));

            // elimina duplicados
            recipients = recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (recipients.Count == 0)
                return BadRequest("Debes enviar 'To' o 'Tos'.");

            var subject = string.IsNullOrWhiteSpace(req!.Subject) ? "Reporte SLA" : req.Subject;
            var body = string.IsNullOrWhiteSpace(req!.Message) ? "Adjunto reporte." : req.Message;

            try
            {
                byte[]? bytes = null;
                if (!string.IsNullOrWhiteSpace(req.PdfBase64))
                {
                    try
                    {
                        bytes = Convert.FromBase64String(req.PdfBase64);
                    }
                    catch
                    {
                        return BadRequest("El PDF no tiene un base64 válido.");
                    }

                    if (bytes.Length > MaxAttachmentBytes)
                        return StatusCode(StatusCodes.Status413PayloadTooLarge, new { mensaje = "Adjunto demasiado grande (máx 10MB)." });
                }

                // envía a cada destinatario individualmente para aislar errores por correo
                var results = new List<object>();
                foreach (var to in recipients)
                {
                    try
                    {
                        if (bytes != null)
                            await _emailService.SendWithAttachmentAsync(to, subject, body, bytes, req.FileName ?? "reporte.pdf");
                        else
                            await _emailService.SendAsync(to, subject, body);

                        results.Add(new { to, ok = true });
                    }
                    catch (SmtpCommandException ex)
                    {
                        results.Add(new { to, ok = false, status = 502, error = ex.Message });
                    }
                    catch (SmtpProtocolException ex)
                    {
                        results.Add(new { to, ok = false, status = 502, error = ex.Message });
                    }
                    catch (AuthenticationException ex)
                    {
                        results.Add(new { to, ok = false, status = 401, error = ex.Message });
                    }
                    catch (SocketException ex)
                    {
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                    catch (TimeoutException ex)
                    {
                        results.Add(new { to, ok = false, status = 504, error = ex.Message });
                    }
                    catch (OperationCanceledException ex)
                    {
                        results.Add(new { to, ok = false, status = 504, error = ex.Message });
                    }
                    catch (ServiceNotConnectedException ex)
                    {
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                    catch (ServiceNotAuthenticatedException ex)
                    {
                        results.Add(new { to, ok = false, status = 401, error = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                }

                // si todos fallaron, devuelve el peor estado; si alguno ok, 200
                var anyOk = results.Any(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);
                if (anyOk)
                    return Ok(new { ok = true, results });
                else
                {
                    // determina el código más representativo entre los errores
                    var status = results.Select(r => (int)r.GetType().GetProperty("status")!.GetValue(r)!).DefaultIfEmpty(503).Max();
                    return StatusCode(status, new { ok = false, results });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = "No fue posible enviar el correo en este entorno.", detalle = ex.Message });
            }
        }

        private static ReporteDTO MapToDto(Reporte r) => new ReporteDTO
        {
            IdReporte = r.IdReporte,
            TipoReporte = r.TipoReporte,
            Formato = r.Formato,
            FiltrosJson = r.FiltrosJson,
            RutaArchivo = r.RutaArchivo,
            GeneradoPor = r.GeneradoPor,
            FechaGeneracion = r.FechaGeneracion,
            TotalSolicitudes = r.Detalles != null ? r.Detalles.Count : 0
        };
    }
}
