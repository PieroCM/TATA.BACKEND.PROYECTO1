using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.Services;
using log4net;
using System.Security.Claims;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubidaVolumenController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SubidaVolumenController));

        private readonly ISubidaVolumenServices _subidaVolumenServices;
        private readonly ILogService _logService;

        public SubidaVolumenController(
            ISubidaVolumenServices subidaVolumenServices, 
            ILogService logService)
        {
            _subidaVolumenServices = subidaVolumenServices;
            _logService = logService;
            log.Debug("SubidaVolumenController inicializado.");
        }

        /// <summary>
        /// Recibe un lote de filas provenientes del Excel (convertidas a JSON desde el frontend)
        /// y ejecuta la carga masiva de solicitudes SLA.
        /// </summary>
        /// <param name="filas">Colección de filas de carga masiva.</param>
        /// <param name="idUsuarioCreador">ID del usuario que ejecuta la carga masiva. Por defecto es 1 (superadmin). 
        /// En producción, debe obtenerse del contexto de autenticación (JWT claims).</param>
        /// <returns>
        /// Siempre devuelve 200 OK con un objeto BulkUploadResultDto que contiene:
        /// - Total de filas procesadas
        /// - Filas exitosas
        /// - Filas con error y sus detalles
        /// Solo devuelve 400 si el request es inválido (sin cuerpo, formato incorrecto, etc.)
        /// </returns>
        [HttpPost("solicitudes")]
        [ProducesResponseType(typeof(BulkUploadResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BulkUploadResultDto>> CargarSolicitudes(
            [FromBody] IEnumerable<SubidaVolumenSolicitudRowDto>? filas,
            [FromQuery] int idUsuarioCreador = 1)
        {
            // Intentar obtener userId del JWT, usar el query param como fallback
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? idUsuarioCreador.ToString());
            
            log.Info($"CargarSolicitudes iniciado para usuario {userId}");
            await _logService.RegistrarLogAsync("INFO", "Petición recibida: CargarSolicitudes", 
                $"Petición recibida desde FRONTEND. Usuario creador: {userId}", userId);

            if (filas is null)
            {
                log.Warn("Petición CargarSolicitudes recibida con cuerpo nulo");
                await _logService.RegistrarLogAsync("WARN", "Body nulo en CargarSolicitudes", 
                    "El frontend envió null", userId);
                return BadRequest("El cuerpo de la petición no puede ser nulo.");
            }

            var lista = filas.ToList();
            
            // Si no hay filas, devolver resultado vacío con error
            if (lista.Count == 0)
            {
                log.Warn("Petición CargarSolicitudes recibida sin filas para procesar");
                await _logService.RegistrarLogAsync("WARN", "Sin filas para procesar", 
                    "Lista vacía enviada por frontend", userId);
                return BadRequest("No se encontraron filas para procesar.");
            }

            // Procesar las filas y siempre devolver 200 OK con el resultado
            var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista, userId);
            
            log.Info($"CargarSolicitudes finalizado. Filas procesadas: {resultado.TotalFilas}");
            await _logService.RegistrarLogAsync("INFO", "Carga masiva completada", 
                $"Total: {resultado.TotalFilas}, Exitosas: {resultado.FilasExitosas}, Errores: {resultado.FilasConError}", userId);
            
            return Ok(resultado);
        }
    }
}
