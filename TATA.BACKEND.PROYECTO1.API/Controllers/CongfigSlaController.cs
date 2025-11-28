using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // api/configsla
    public class ConfigSlaController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ConfigSlaController));
        
        private readonly IConfigSlaService _service;
        private readonly ILogSistemaService _logService;

        public ConfigSlaController(IConfigSlaService service, ILogSistemaService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("ConfigSlaController inicializado.");
        }

        // GET: api/configsla?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigSlaDTO>>> Get([FromQuery] bool soloActivos = true)
        {
            log.Info($"Get iniciado con soloActivos: {soloActivos}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll ConfigSla",
                Detalles = $"Obteniendo ConfigSla con soloActivos: {soloActivos}",
                IdUsuario = null
            });

            try
            {
                var list = await _service.GetAllAsync(soloActivos);
                
                log.Info("Get completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll ConfigSla",
                    Detalles = $"Total ConfigSla obtenidos: {list.Count()}",
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

        // GET: api/configsla/5
        [HttpGet("{id:int}", Name = "GetConfigSlaById")]
        public async Task<ActionResult<ConfigSlaDTO>> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById ConfigSla {id}",
                Detalles = $"Buscando ConfigSla con id: {id}",
                IdUsuario = null
            });

            try
            {
                var item = await _service.GetByIdAsync(id);
                
                if (item is null)
                {
                    log.Warn($"ConfigSla con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"ConfigSla no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById ConfigSla",
                    Detalles = $"ConfigSla {id} obtenido exitosamente",
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

        // POST: api/configsla
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] ConfigSlaCreateDTO dto)
        {
            log.Info("Post iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create ConfigSla",
                Detalles = $"Creando ConfigSla con código: {dto?.CodigoSla}",
                IdUsuario = null
            });

            if (dto is null)
            {
                log.Warn("Post recibió dto nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: dto nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest();
            }

            try
            {
                var id = await _service.CreateAsync(null, dto);
                
                log.Info($"Post completado correctamente, IdSla: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create ConfigSla",
                    Detalles = $"ConfigSla creado con id: {id}",
                    IdUsuario = null
                });

                return CreatedAtRoute("GetConfigSlaById", new { id }, id);
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

        // PUT: api/configsla/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] ConfigSlaUpdateDTO dto)
        {
            log.Info($"Put iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update ConfigSla {id}",
                Detalles = $"Actualizando ConfigSla con id: {id}",
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

            try
            {
                var ok = await _service.UpdateAsync(id, dto);
                
                if (!ok)
                {
                    log.Warn($"ConfigSla con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"ConfigSla no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Put completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update ConfigSla",
                    Detalles = $"ConfigSla {id} actualizado exitosamente",
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

        // DELETE (físico): api/configsla/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete ConfigSla {id}",
                Detalles = $"Eliminando ConfigSla con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _service.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"ConfigSla con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"ConfigSla no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete ConfigSla",
                    Detalles = $"ConfigSla {id} eliminado exitosamente",
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
