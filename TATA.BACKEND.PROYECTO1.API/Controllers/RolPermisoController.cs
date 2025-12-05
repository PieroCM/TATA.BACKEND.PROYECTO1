using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolPermisoController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RolPermisoController));
        
        private readonly IRolPermisoService _service;
        private readonly ILogSistemaService _logService;

        public RolPermisoController(IRolPermisoService service, ILogSistemaService logService)
        {
            _service = service;
            _logService = logService;
            log.Debug("RolPermisoController inicializado.");
        }

        // GET: api/rolpermiso
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll RolPermiso",
                Detalles = "Obteniendo todos los RolPermiso",
                IdUsuario = null
            });

            try
            {
                var result = await _service.GetAllAsync();
                
                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll RolPermiso",
                    Detalles = $"Total RolPermiso obtenidos: {result.Count()}",
                    IdUsuario = null
                });
                
                return Ok(result);
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

        // GET: api/rolpermiso/nombres
        [HttpGet("nombres")]
        public async Task<IActionResult> GetAllWithNames()
        {
            log.Info("GetAllWithNames iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAllWithNames RolPermiso",
                Detalles = "Obteniendo todos los RolPermiso con nombres",
                IdUsuario = null
            });

            try
            {
                var result = await _service.GetAllWithNamesAsync();
                
                log.Info("GetAllWithNames completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAllWithNames RolPermiso",
                    Detalles = $"Total RolPermiso obtenidos: {result.Count()}",
                    IdUsuario = null
                });
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetAllWithNames", ex);
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

        // GET: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpGet("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> GetByIds(int idRolSistema, int idPermiso)
        {
            log.Info($"GetByIds iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetByIds RolPermiso",
                Detalles = $"Buscando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                IdUsuario = null
            });

            try
            {
                var result = await _service.GetByIdsAsync(idRolSistema, idPermiso);
                
                if (result == null)
                {
                    log.Warn($"RolPermiso no encontrado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "RolPermiso no encontrado",
                        Detalles = $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                        IdUsuario = null
                    });
                    return NotFound();
                }

                log.Info($"GetByIds completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetByIds RolPermiso",
                    Detalles = "RolPermiso obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante GetByIds", ex);
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

        // POST: api/rolpermiso
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RolPermisoEntity entity)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create RolPermiso",
                Detalles = $"Creando RolPermiso con idRolSistema: {entity?.IdRolSistema}, idPermiso: {entity?.IdPermiso}",
                IdUsuario = null
            });

            if (entity == null)
            {
                log.Warn("Create recibió entity nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: entity nulo",
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
                var created = await _service.AddAsync(entity);
                
                if (!created)
                {
                    log.Warn("No se pudo crear el RolPermiso");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "No se pudo crear el registro",
                        Detalles = "El servicio retornó false",
                        IdUsuario = null
                    });
                    return BadRequest("No se pudo crear el registro");
                }

                log.Info("Create completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create RolPermiso",
                    Detalles = "RolPermiso creado exitosamente",
                    IdUsuario = null
                });

                return Ok("Registro creado correctamente");
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

        [HttpPut("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Update(int idRolSistema, int idPermiso, [FromBody] RolPermisoEntity entity)
        {
            log.Info($"Update iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Update RolPermiso",
                Detalles = $"Actualizando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                IdUsuario = null
            });

            if (entity == null)
            {
                log.Warn($"Update recibió entity nulo");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: entity nulo",
                    Detalles = "El cuerpo de la petición es nulo",
                    IdUsuario = null
                });
                return BadRequest(new { mensaje = "El cuerpo de la petición no puede ser nulo" });
            }

            if (!ModelState.IsValid)
            {
                log.Warn($"Update: Validación de ModelState fallida");
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
                var updated = await _service.UpdateAsync(idRolSistema, idPermiso, entity);

                if (!updated)
                {
                    var exists = await _service.GetByIdsAsync(idRolSistema, idPermiso);
                    if (exists == null)
                    {
                        log.Warn($"RolPermiso original no encontrado");
                        await _logService.AddAsync(new LogSistemaCreateDTO
                        {
                            Nivel = "WARN",
                            Mensaje = "RolPermiso no encontrado",
                            Detalles = $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                            IdUsuario = null
                        });
                        return NotFound("❌ No se encontró el registro original a actualizar.");
                    }

                    var duplicate = await _service.GetByIdsAsync(entity.IdRolSistema, entity.IdPermiso);
                    if (duplicate != null)
                    {
                        log.Warn($"Combinación duplicada al actualizar RolPermiso");
                        await _logService.AddAsync(new LogSistemaCreateDTO
                        {
                            Nivel = "WARN",
                            Mensaje = "Combinación duplicada",
                            Detalles = "Ya existe un permiso con esta combinación",
                            IdUsuario = null
                        });
                        return BadRequest("❌ No se puede actualizar. Ya existe un permiso asignado con esta combinación.");
                    }
                }

                log.Info("Update completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update RolPermiso",
                    Detalles = "RolPermiso actualizado exitosamente",
                    IdUsuario = null
                });

                return Ok("✔ Registro actualizado correctamente.");
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Update", ex);
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

        // DELETE: api/rolpermiso/{idRolSistema}/{idPermiso}
        [HttpDelete("{idRolSistema}/{idPermiso}")]
        public async Task<IActionResult> Delete(int idRolSistema, int idPermiso)
        {
            log.Info($"Delete iniciado para idRolSistema: {idRolSistema}, idPermiso: {idPermiso}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Delete RolPermiso",
                Detalles = $"Eliminando RolPermiso con idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                IdUsuario = null
            });

            try
            {
                var deleted = await _service.RemoveAsync(idRolSistema, idPermiso);
                
                if (!deleted)
                {
                    log.Warn($"RolPermiso no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "RolPermiso no encontrado para eliminar",
                        Detalles = $"idRolSistema: {idRolSistema}, idPermiso: {idPermiso}",
                        IdUsuario = null
                    });
                    return NotFound("No se encontró el registro");
                }

                log.Info("Delete completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete RolPermiso",
                    Detalles = "RolPermiso eliminado exitosamente",
                    IdUsuario = null
                });

                return Ok("Registro eliminado correctamente");
            }
            catch (Exception ex)
            {
                log.Error($"Error inesperado durante Delete", ex);
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
