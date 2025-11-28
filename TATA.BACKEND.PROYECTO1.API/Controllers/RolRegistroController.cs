using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolRegistroController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RolRegistroController));
        
        private readonly IRolRegistroService _service;
        private readonly ILogSistemaService _logService;

        public RolRegistroController(IRolRegistroService service, ILogSistemaService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("RolRegistroController inicializado.");
        }

        // GET: api/rolregistro?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolRegistroDTO>>> Get([FromQuery] bool soloActivos = true)
        {
            log.Info($"Get iniciado con soloActivos: {soloActivos}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll RolRegistro",
                Detalles = $"Obteniendo RolRegistro con soloActivos: {soloActivos}",
                IdUsuario = null
            });

            try
            {
                var list = await _service.GetAllAsync(soloActivos);
                
                log.Info("Get completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll RolRegistro",
                    Detalles = $"Total RolRegistro obtenidos: {list.Count()}",
                    IdUsuario = null
                });
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Get", ex);
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

        // GET: api/rolregistro/5
        [HttpGet("{id:int}", Name = "GetRolRegistroById")]
        public async Task<ActionResult<RolRegistroDTO>> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById RolRegistro {id}",
                Detalles = $"Buscando RolRegistro con id: {id}",
                IdUsuario = null
            });

            try
            {
                var item = await _service.GetByIdAsync(id);
                
                if (item is null)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"RolRegistro no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById RolRegistro",
                    Detalles = $"RolRegistro {id} obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(item);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetById para id: {id}", ex);
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

        // POST: api/rolregistro
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] RolRegistroCreateDTO dto)
        {
            log.Info("Post iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create RolRegistro",
                Detalles = $"Creando RolRegistro: {dto?.NombreRol}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn("Post recibió dto nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn("Post: Validación de ModelState fallida");
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
                var id = await _service.CreateAsync(null, dto);
                
                log.Info($"Post completado correctamente, IdRolRegistro: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create RolRegistro",
                    Detalles = $"RolRegistro creado con id: {id}",
                    IdUsuario = null
                });

                return CreatedAtRoute("GetRolRegistroById", new { id }, id);
            }
            catch (DuplicateNameException ex)
            {
                log.Warn($"NombreRol duplicado: {ex.Message}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Conflicto: NombreRol ya existe",
                    Detalles = ex.ToString(),
                    IdUsuario = null
                });
                return Conflict(new { message = "NombreRol ya existe" });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Post", ex);
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

        // PUT: api/rolregistro/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] RolRegistroUpdateDTO dto)
        {
            log.Info($"Put iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update RolRegistro {id}",
                Detalles = $"Actualizando RolRegistro con id: {id}",
                IdUsuario = null
            });

            if (dto is null)
            {
                log.Warn($"Put recibió dto nulo para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Put: Validación de ModelState fallida para id: {id}");
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
                var ok = await _service.UpdateAsync(id, dto);
                
                if (!ok)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"RolRegistro no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Put completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update RolRegistro",
                    Detalles = $"RolRegistro {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Put para id: {id}", ex);
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

        // DELETE: api/rolregistro/5 (físico)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete RolRegistro {id}",
                Detalles = $"Eliminando RolRegistro con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _service.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"RolRegistro con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"RolRegistro no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete RolRegistro",
                    Detalles = $"RolRegistro {id} eliminado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete para id: {id}", ex);
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
    }
}
