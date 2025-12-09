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
using log4net;

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
        private readonly ILogService _logService;

        // límite básico para evitar adjuntos excesivos (10 MB)
        private const int MaxAttachmentBytes = 10 * 1024 * 1024;

        public ReporteController(IReporteService reporteService, IEmailService emailService, ILogService logService)
        {
            _reporteService = reporteService;
            _emailService = emailService;
            _logService = logService;
            log.Debug("ReporteController inicializado.");
        }

        // GET: api/reportes
        [HttpGet]
        public async Task<IActionResult> GetReportes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetReportes iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetReportes", 
                $"Usuario {userId} solicitando todos los reportes", userId);

            try
            {
                var entities = await _reporteService.GetAllAsync();
                var list = entities.Select(MapToDto).ToList();
                
                log.Info($"GetReportes completado correctamente, {list.Count} reportes obtenidos");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetReportes", 
                    $"Total reportes obtenidos: {list.Count}", userId);
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetReportes", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetReportes", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // GET: api/reportes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReporteById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"GetReporteById iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: GetReporteById", 
                $"Usuario {userId} solicitando reporte {id}", userId);

            try
            {
                var entity = await _reporteService.GetByIdAsync(id);
                
                if (entity == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado");
                    await _logService.RegistrarLogAsync("WARN", $"Reporte no encontrado: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"GetReporteById completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: GetReporteById", 
                    $"Reporte {id} obtenido exitosamente", userId);

                return Ok(MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetReporteById para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en GetReporteById", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reportes
        [AllowAnonymous] // si aún necesitas crear sin JWT; quítalo cuando todo requiera token
        [HttpPost]
        public async Task<IActionResult> CreateReporte([FromBody] ReporteCreateRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"CreateReporte iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: CreateReporte", 
                $"Usuario {userId} creando reporte tipo: {request?.TipoReporte}", userId);

            if (request == null)
            {
                log.Warn("CreateReporte recibió request nulo");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: request nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest();
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
                    // FechaGeneracion: la setea SQL con DEFAULT (SYSUTCDATETIME)
                };

                await _reporteService.AddAsync(entity);

                log.Info($"CreateReporte completado correctamente, IdReporte: {entity.IdReporte}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: CreateReporte", 
                    $"Reporte creado con id: {entity.IdReporte}", userId);

                // CreatedAtAction arma la URL: GET api/reportes/{id}
                return CreatedAtAction(nameof(GetReporteById), new { id = entity.IdReporte }, MapToDto(entity));
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante CreateReporte", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en CreateReporte", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reportes/generar
        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarReporteRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"Generar iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: Generar Reporte", 
                $"Usuario {userId} generando reporte para {request?.IdsSolicitudes?.Count ?? 0} solicitudes", userId);

            if (request == null || request.IdsSolicitudes == null || !request.IdsSolicitudes.Any())
            {
                log.Warn("Generar: sin solicitudes para procesar");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: sin solicitudes", 
                    "Debe enviar al menos una solicitud", userId);
                return BadRequest("Debes enviar al menos una solicitud.");
            }

            // Obtener id usuario desde el JWT usando NameIdentifier
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var idUsuarioActual))
            {
                log.Warn("Generar: Token sin NameIdentifier válido");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: Token sin NameIdentifier", 
                    "El token no contiene claim NameIdentifier válido", userId);
                return Unauthorized("Token sin claim NameIdentifier válido.");
            }

            try
            {
                var reporte = await _reporteService.GenerarReporteAsync(request, idUsuarioActual);

                var dto = MapToDto(reporte);
                // set nombre desde claim Name si existe
                dto.GeneradoPorNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? "(sin_username)";

                log.Info($"Generar completado correctamente, IdReporte: {reporte.IdReporte}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: Generar Reporte", 
                    $"Reporte generado con id: {reporte.IdReporte}", userId);

                return Ok(dto);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Generar", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en Generar", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // PUT: api/reportes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReporte(int id, [FromBody] ReporteUpdateRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"UpdateReporte iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: UpdateReporte", 
                $"Usuario {userId} actualizando reporte {id}", userId);

            if (request == null)
            {
                log.Warn($"UpdateReporte recibió request nulo para id: {id}");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: request nulo", 
                    "El cuerpo de la petición es nulo", userId);
                return BadRequest();
            }

            try
            {
                var existing = await _reporteService.GetByIdAsync(id);
                if (existing == null)
                {
                    log.Warn($"Reporte con id {id} no encontrado para actualizar");
                    await _logService.RegistrarLogAsync("WARN", $"Reporte no encontrado para actualizar: {id}", 
                        "Recurso solicitado no existe", userId);
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
                    await _logService.RegistrarLogAsync("WARN", $"No se pudo actualizar reporte: {id}", 
                        "El servicio retornó false", userId);
                    return NotFound(); // por si se borró entre GET y PUT
                }

                log.Info($"UpdateReporte completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: UpdateReporte", 
                    $"Reporte {id} actualizado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante UpdateReporte para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en UpdateReporte", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // DELETE: api/reportes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReporte(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"DeleteReporte iniciado para id: {id}, usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: DeleteReporte", 
                $"Usuario {userId} eliminando reporte {id}", userId);

            try
            {
                var ok = await _reporteService.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"Reporte con id {id} no encontrado para eliminar");
                    await _logService.RegistrarLogAsync("WARN", $"Reporte no encontrado para eliminar: {id}", 
                        "Recurso solicitado no existe", userId);
                    return NotFound();
                }

                log.Info($"DeleteReporte completado correctamente para id: {id}");
                await _logService.RegistrarLogAsync("INFO", "Operación completada correctamente: DeleteReporte", 
                    $"Reporte {id} eliminado exitosamente", userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante DeleteReporte para id: {id}", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en DeleteReporte", ex.ToString(), userId);
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // POST: api/reporte/enviar-correo
        [HttpPost("enviar-correo")] // api/reporte/enviar-correo
        [AllowAnonymous] // quita esto si deseas exigir JWT
        public async Task<IActionResult> EnviarCorreo([FromBody] SendEmailRequest req)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            log.Info($"EnviarCorreo iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: EnviarCorreo", 
                $"Usuario {userId} enviando correo a {req?.To ?? string.Join(", ", req?.Tos ?? new List<string>())}", userId);

            // normaliza destinatarios: usa To si existe, o la lista Tos
            var recipients = new List<string>();
            if (!string.IsNullOrWhiteSpace(req?.To))
                recipients.Add(req!.To!.Trim());
            if (req?.Tos != null && req.Tos.Count > 0)
                recipients.AddRange(req.Tos.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));

            // elimina duplicados
            recipients = recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (recipients.Count == 0)
            {
                log.Warn("EnviarCorreo: sin destinatarios");
                await _logService.RegistrarLogAsync("WARN", "Validación fallida: sin destinatarios", 
                    "Debe enviar To o Tos", userId);
                return BadRequest("Debes enviar 'To' o 'Tos'.");
            }

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
                        log.Warn("EnviarCorreo: PDF con base64 inválido");
                        await _logService.RegistrarLogAsync("WARN", "Validación fallida: PDF base64 inválido", 
                            "El PDF no tiene formato base64 válido", userId);
                        return BadRequest("El PDF no tiene un base64 válido.");
                    }

                    if (bytes.Length > MaxAttachmentBytes)
                    {
                        log.Warn($"EnviarCorreo: Adjunto demasiado grande ({bytes.Length} bytes)");
                        await _logService.RegistrarLogAsync("WARN", "Validación fallida: Adjunto muy grande", 
                            $"Tamaño: {bytes.Length} bytes, máximo: {MaxAttachmentBytes} bytes", userId);
                        return StatusCode(StatusCodes.Status413PayloadTooLarge, new { mensaje = "Adjunto demasiado grande (máx 10MB)." });
                    }
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
                        log.Error($"Error SMTP al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 502, error = ex.Message });
                    }
                    catch (SmtpProtocolException ex)
                    {
                        log.Error($"Error de protocolo SMTP al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 502, error = ex.Message });
                    }
                    catch (AuthenticationException ex)
                    {
                        log.Error($"Error de autenticación al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 401, error = ex.Message });
                    }
                    catch (SocketException ex)
                    {
                        log.Error($"Error de conexión al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                    catch (TimeoutException ex)
                    {
                        log.Error($"Timeout al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 504, error = ex.Message });
                    }
                    catch (OperationCanceledException ex)
                    {
                        log.Error($"Operación cancelada al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 504, error = ex.Message });
                    }
                    catch (ServiceNotConnectedException ex)
                    {
                        log.Error($"Servicio no conectado al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                    catch (ServiceNotAuthenticatedException ex)
                    {
                        log.Error($"Servicio no autenticado al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 401, error = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error desconocido al enviar a {to}", ex);
                        results.Add(new { to, ok = false, status = 503, error = ex.Message });
                    }
                }

                // si todos fallaron, devuelve el peor estado; si alguno ok, 200
                var anyOk = results.Any(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);
                
                if (anyOk)
                {
                    var countOk = results.Count(r => (bool)r.GetType().GetProperty("ok")!.GetValue(r)!);
                    log.Info($"EnviarCorreo completado parcial o totalmente");
                    await _logService.RegistrarLogAsync("INFO", "Operación completada: EnviarCorreo", 
                        $"Correos enviados: {countOk}/{results.Count}", userId);
                    return Ok(new { ok = true, results });
                }
                else
                {
                    // determina el código más representativo entre los errores
                    var status = results.Select(r => (int)r.GetType().GetProperty("status")!.GetValue(r)!).DefaultIfEmpty(503).Max();
                    log.Error($"EnviarCorreo falló para todos los destinatarios");
                    await _logService.RegistrarLogAsync("ERROR", "Fallo completo en EnviarCorreo", 
                        $"Todos los envíos fallaron: {results.Count}", userId);
                    return StatusCode(status, new { ok = false, results });
                }
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado en EnviarCorreo", ex);
                await _logService.RegistrarLogAsync("ERROR", "Error inesperado en EnviarCorreo", ex.ToString(), userId);
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