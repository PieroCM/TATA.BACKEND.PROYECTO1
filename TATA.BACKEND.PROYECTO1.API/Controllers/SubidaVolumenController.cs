using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubidaVolumenController : ControllerBase
    {
        private readonly ISubidaVolumenServices _subidaVolumenServices;

        public SubidaVolumenController(ISubidaVolumenServices subidaVolumenServices)
        {
            _subidaVolumenServices = subidaVolumenServices;
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
            // Validar que el cuerpo de la petición no sea nulo
            if (filas is null)
            {
                return BadRequest(new 
                { 
                    error = "REQUEST_INVALIDO",
                    mensaje = "El cuerpo de la petición no puede ser nulo. Debe enviar un array de filas.",
                    detalles = "Verifique que el Content-Type sea 'application/json' y que el body contenga un array válido."
                });
            }

            var lista = filas.ToList();
            
            // Si no hay filas, devolver resultado vacío con éxito (no es un error de request)
            if (lista.Count == 0)
            {
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
            return Ok(resultado);
        }
    }
}
