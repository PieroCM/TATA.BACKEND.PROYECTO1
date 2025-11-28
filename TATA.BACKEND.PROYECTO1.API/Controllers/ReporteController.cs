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
            var entities = await _reporteService.GetAllAsync();
            var list = entities.Select(MapToDto).ToList();
            return Ok(list);
        }

        // GET: api/reportes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            var entity = await _reporteService.GetByIdAsync(id);
            if (entity == null) return NotFound();

            return Ok(MapToDto(entity));
        }

        // POST: api/reportes
        [AllowAnonymous] // si aún necesitas crear sin JWT; quítalo cuando todo requiera token
        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReporteCreateRequest request)
        {
            if (request == null) return BadRequest();

            var entity = new Reporte
            {
                TipoReporte = request.TipoReporte,
                Formato = request.Formato,
                FiltrosJson = request.FiltrosJson,
                RutaArchivo = request.RutaArchivo,
                GeneradoPor = request.GeneradoPor
                // FechaGeneracion: la setea SQL con DEFAULT (SYSUTCDATETIME)
            };

            await _reporteService.AddAsync(entity);

            // CreatedAtAction arma la URL: GET api/reportes/{id}
            return CreatedAtAction(nameof(GetReporteById), new { id = entity.IdReporte }, MapToDto(entity));
        }

        // POST: api/reportes/generar
        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarReporteRequest request)
        {
            if (request == null || request.IdsSolicitudes == null || !request.IdsSolicitudes.Any())
                return BadRequest("Debes enviar al menos una solicitud.");

            // Obtener id usuario desde el JWT (claim "UserId")
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var idUsuarioActual))
            {
                return Unauthorized("Token sin claim UserId válido.");
            }

            var reporte = await _reporteService.GenerarReporteAsync(request, idUsuarioActual);

            var dto = MapToDto(reporte);
            // set nombre desde claim Name si existe
            dto.GeneradoPorNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? "(sin_username)";
            return Ok(dto);
        }

        // PUT: api/reportes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReporteUpdateRequest request)
        {
            if (request == null) return BadRequest();

            var existing = await _reporteService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.TipoReporte = request.TipoReporte;
            existing.Formato = request.Formato;
            existing.FiltrosJson = request.FiltrosJson;
            existing.RutaArchivo = request.RutaArchivo;
            existing.GeneradoPor = request.GeneradoPor;

            var ok = await _reporteService.UpdateAsync(existing);
            if (!ok) return NotFound(); // por si se borró entre GET y PUT

            return NoContent();
        }

        // DELETE: api/reportes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            var ok = await _reporteService.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
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
            // GeneradoPorNombre se completa en el endpoint generar usando claims
            FechaGeneracion = r.FechaGeneracion,
            TotalSolicitudes = r.Detalles != null ? r.Detalles.Count : 0
        };
    }

}
