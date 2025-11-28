using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermisoController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PermisoController));
        
        private readonly IPermisoService _service;
        private readonly ILogSistemaService _logService;

        public PermisoController(IPermisoService service, ILogSistemaService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("PermisoController inicializado.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll Permisos",
                Detalles = "Obteniendo todos los permisos",
                IdUsuario = null
            });

            try
            {
                var list = await _service.GetAll();
                
                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll Permisos",
                    Detalles = $"Total permisos obtenidos: {list.Count}",
                    IdUsuario = null
                });
                
                return Ok(list);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAll", ex);
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById Permiso {id}",
                Detalles = $"Buscando permiso con id: {id}",
                IdUsuario = null
            });

            try
            {
                var item = await _service.GetById(id);
                
                if (item == null)
                {
                    log.Warn($"Permiso con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Permiso no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById Permiso",
                    Detalles = $"Permiso {id} obtenido exitosamente",
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PermisoCreateDTO dto)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create Permiso",
                Detalles = $"Creando permiso con código: {dto?.Codigo}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn("Create recibió dto nulo");
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
                log.Warn("Create: Validación de ModelState fallida");
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
                var created = await _service.Create(dto);
                
                log.Info($"Create completado correctamente, IdPermiso: {created.IdPermiso}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create Permiso",
                    Detalles = $"Permiso creado con id: {created.IdPermiso}",
                    IdUsuario = null
                });

                return CreatedAtAction(nameof(GetById), new { id = created.IdPermiso }, created);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante Create", ex);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PermisoUpdateDTO dto)
        {
            log.Info($"Update iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update Permiso {id}",
                Detalles = $"Actualizando permiso con id: {id}",
                IdUsuario = null
            });

            if (dto == null)
            {
                log.Warn($"Update recibió dto nulo para id: {id}");
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
                log.Warn($"Update: Validación de ModelState fallida para id: {id}");
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
                var ok = await _service.Update(id, dto);
                
                if (!ok)
                {
                    log.Warn($"Permiso con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Permiso no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update Permiso",
                    Detalles = $"Permiso {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update para id: {id}", ex);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete Permiso {id}",
                Detalles = $"Eliminando permiso con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _service.Delete(id);
                
                if (!ok)
                {
                    log.Warn($"Permiso con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Permiso no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete Permiso",
                    Detalles = $"Permiso {id} eliminado exitosamente",
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
