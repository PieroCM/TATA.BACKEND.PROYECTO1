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
        [HttpPost("solicitudes")]
        [ProducesResponseType(typeof(BulkUploadResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BulkUploadResultDto>> CargarSolicitudes(
            [FromBody] IEnumerable<SubidaVolumenSolicitudRowDto> filas)
        {
            if (filas is null)
            {
                return BadRequest("El cuerpo de la petición no puede ser nulo.");
            }

            var lista = filas.ToList();
            if (lista.Count == 0)
            {
                return BadRequest("No se encontraron filas para procesar.");
            }

            var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista);
            return Ok(resultado);
        }
    }
}
