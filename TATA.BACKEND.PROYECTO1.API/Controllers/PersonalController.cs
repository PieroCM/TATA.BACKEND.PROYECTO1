using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Requiere token JWT válido
    public class PersonalController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PersonalController));
        
        private readonly IPersonalService _personalService;
        private readonly ILogSistemaService _logService;

        public PersonalController(IPersonalService personalService, ILogSistemaService logService)
        {
            _personalService = personalService;
            _logService = logService;
            log.Debug("PersonalController inicializado.");
        }

        // ✅ OBTENER TODOS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            log.Info("GetAll iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetAll Personal",
                Detalles = "Obteniendo todo el personal",
                IdUsuario = null
            });

            try
            {
                var personales = await _personalService.GetAllAsync();
                
                log.Info("GetAll completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetAll Personal",
                    Detalles = $"Total personal obtenido: {personales.Count()}",
                    IdUsuario = null
                });
                
                return Ok(personales);
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

        // ✅ OBTENER UNO POR ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            log.Info($"GetById iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: GetById Personal {id}",
                Detalles = $"Buscando personal con id: {id}",
                IdUsuario = null
            });

            try
            {
                var personal = await _personalService.GetByIdAsync(id);
                
                if (personal == null)
                {
                    log.Warn($"Personal con id {id} no encontrado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Personal no encontrado: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Personal no encontrado" });
                }

                log.Info($"GetById completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetById Personal",
                    Detalles = $"Personal {id} obtenido exitosamente",
                    IdUsuario = null
                });

                return Ok(personal);
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

        // ✅ CREAR NUEVO
        [HttpPost]
        [Authorize(Roles = "1")] // opcional: solo admin (idRolSistema = 1)
        public async Task<IActionResult> Create([FromBody] PersonalCreateDTO dto)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create Personal",
                Detalles = $"Creando personal: {dto?.Nombres} {dto?.Apellidos}",
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
                return BadRequest(new { message = "El cuerpo de la petición no puede ser nulo" });
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
                var ok = await _personalService.CreateAsync(dto);
                
                if (!ok)
                {
                    log.Warn("No se pudo crear el personal");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Error al registrar personal",
                        Detalles = "El servicio retornó false",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Error al registrar personal" });
                }

                log.Info("Create completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create Personal",
                    Detalles = $"Personal creado: {dto.Nombres} {dto.Apellidos}",
                    IdUsuario = null
                });

                return Ok(new { message = "Personal registrado correctamente" });
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

        // ✅ ACTUALIZAR
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonalUpdateDTO dto)
        {
            log.Info($"Update iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Update Personal {id}",
                Detalles = $"Actualizando personal con id: {id}",
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
                return BadRequest(new { message = "El cuerpo de la petición no puede ser nulo" });
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
                var ok = await _personalService.UpdateAsync(id, dto);
                
                if (!ok)
                {
                    log.Warn($"Personal con id {id} no encontrado para actualizar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Personal no encontrado para actualizar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Personal no encontrado" });
                }

                log.Info($"Update completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Update Personal",
                    Detalles = $"Personal {id} actualizado exitosamente",
                    IdUsuario = null
                });

                return Ok(new { message = "Personal actualizado correctamente" });
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

        // ✅ ELIMINAR
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            log.Info($"Delete iniciado para id: {id}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = $"Petición recibida: Delete Personal {id}",
                Detalles = $"Eliminando personal con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _personalService.DeleteAsync(id);
                
                if (!ok)
                {
                    log.Warn($"Personal con id {id} no encontrado para eliminar");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = $"Personal no encontrado para eliminar: {id}",
                        Detalles = "Recurso solicitado no existe",
                        IdUsuario = null
                    });
                    return NotFound(new { message = "Personal no encontrado" });
                }

                log.Info($"Delete completado correctamente para id: {id}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Delete Personal",
                    Detalles = $"Personal {id} eliminado exitosamente",
                    IdUsuario = null
                });

                return Ok(new { message = "Personal eliminado correctamente" });
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
