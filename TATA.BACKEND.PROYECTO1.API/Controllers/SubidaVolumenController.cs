using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net; // 1. Agregar el using

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubidaVolumenController : ControllerBase
    {
        // 2. DECLARACIÓN ESTÁTICA DEL LOGGER
        private static readonly ILog log = LogManager.GetLogger(typeof(SubidaVolumenController));

        private readonly ISubidaVolumenServices _subidaVolumenServices;
        private readonly ILogSistemaService _logService;

        public SubidaVolumenController(
            ISubidaVolumenServices subidaVolumenServices, 
            ILogSistemaService logService)
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
            [FromBody] IEnumerable<SubidaVolumenSolicitudRowDto>? filas)
        {
            // Loguear que se recibió la petición
            log.Info("Petición CargarSolicitudes recibida.");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición CargarSolicitudes recibida",
                Detalles = "Petición recibida desde FRONTEND",
                IdUsuario = 1   // si tienes el usuario en JWT, úsalo
            });

            if (filas is null)
            {
                log.Warn("Petición CargarSolicitudes recibida con cuerpo nulo.");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Body nulo en CargarSolicitudes",
                    Detalles = "El frontend envió null",
                    IdUsuario = null
                });
                return BadRequest("El cuerpo de la petición no puede ser nulo.");
            }

            var lista = filas.ToList();
            
            // Si no hay filas, devolver resultado vacío con éxito (no es un error de request)
            if (lista.Count == 0)
            {
                log.Warn("Petición CargarSolicitudes recibida sin filas para procesar.");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Sin filas para procesar",
                    Detalles = "Lista vacía enviada por frontend",
                    IdUsuario = null
                });
                return BadRequest("No se encontraron filas para procesar.");
                return Ok(new BulkUploadResultDto
                {
                    TotalFilas = 0,
                    FilasExitosas = 0,
                    FilasConError = 0,
                    Errores = new List<BulkUploadErrorDto>()
                });
            }

            // Procesar las filas y siempre devolver 200 OK con el resultado
            var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista);
            log.Info($"Petición CargarSolicitudes finalizada. Filas procesadas: {resultado.TotalFilas}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Carga masiva completada",
                Detalles = $"Total: {resultado.TotalFilas}, Exitosas: {resultado.FilasExitosas}, Errores: {resultado.FilasConError}",
                IdUsuario = 1
            });
            return Ok(resultado);
        }
    }
}
