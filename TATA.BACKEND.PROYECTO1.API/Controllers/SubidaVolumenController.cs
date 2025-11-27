using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    // ⚠️⚠️⚠️ CONTROLADOR DESHABILITADO TEMPORALMENTE ⚠️⚠️⚠️
    // Este controlador usa el servicio SubidaVolumenServices que está deshabilitado
    // debido a cambios en la arquitectura de Usuario y Personal.
    //
    // Para más información, ver:
    // - TATA.BACKEND.PROYECTO1.CORE/Core/Services/SubidaVolumenServices.cs
    // - USUARIO_BACKEND_GUIA_COMPLETA.md

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
        /// ⚠️ ENDPOINT DESHABILITADO
        /// Recibe un lote de filas provenientes del Excel (convertidas a JSON desde el frontend)
        /// y ejecuta la carga masiva de solicitudes SLA.
        /// 
        /// NOTA: Este endpoint está temporalmente deshabilitado debido a cambios en la arquitectura.
        /// El servicio ahora retorna un mensaje de error indicando que no está disponible.
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

            // El servicio ahora retorna un resultado con mensaje de error
            var resultado = await _subidaVolumenServices.ProcesarSolicitudesAsync(lista);
            
            // Retornar con código 503 (Service Unavailable) para indicar que el servicio está deshabilitado
            return StatusCode(503, resultado);
        }
    }
}
