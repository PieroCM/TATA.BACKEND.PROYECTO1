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
                Detalles = "Obteniendo todos los registros de Personal",
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
                    Detalles = $"Total Personal obtenidos: {personales.Count()}",
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
                Detalles = $"Buscando Personal con id: {id}",
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

        // ✅ CREAR NUEVO (Simple - sin cuenta de usuario)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonalCreateDTO dto)
        {
            log.Info("Create iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create Personal",
                Detalles = $"Creando Personal: {dto?.Nombres} {dto?.Apellidos}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombres) || string.IsNullOrWhiteSpace(dto.Apellidos))
            {
                log.Warn("Create: Validación fallida - Nombres y Apellidos son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Nombres y Apellidos son obligatorios",
                    Detalles = "El cuerpo de la petición no cumple con los requisitos",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Nombres y Apellidos son obligatorios" });
            }

            try
            {
                var ok = await _personalService.CreateAsync(dto);
                
                if (!ok)
                {
                    log.Warn("Create: No se pudo registrar personal - posible documento duplicado");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Error al registrar personal",
                        Detalles = !string.IsNullOrWhiteSpace(dto.Documento) 
                            ? "El documento proporcionado ya está registrado en el sistema" 
                            : "Verifica que todos los datos sean válidos",
                        IdUsuario = null
                    });
                    return BadRequest(new { 
                        message = "Error al registrar personal",
                        detalle = !string.IsNullOrWhiteSpace(dto.Documento) 
                            ? "El documento proporcionado ya está registrado en el sistema" 
                            : "Verifica que todos los datos sean válidos"
                    });
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

        /// <summary>
        /// ⚠️ NUEVO: Crear Personal con Cuenta de Usuario (Condicional)
        /// POST /api/personal/with-account
        /// </summary>
        [HttpPost("with-account")]
        public async Task<IActionResult> CreateWithAccount([FromBody] PersonalCreateWithAccountDTO dto)
        {
            log.Info("CreateWithAccount iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Create Personal With Account",
                Detalles = $"Creando Personal con cuenta: {dto?.CrearCuentaUsuario}, Nombres: {dto?.Nombres} {dto?.Apellidos}",
                IdUsuario = null
            });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombres) || string.IsNullOrWhiteSpace(dto.Apellidos))
            {
                log.Warn("CreateWithAccount: Validación fallida - Nombres y Apellidos son obligatorios");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Nombres y Apellidos son obligatorios",
                    Detalles = "El cuerpo de la petición no cumple con los requisitos",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Nombres y Apellidos son obligatorios" });
            }

            if (dto.CrearCuentaUsuario)
            {
                // Validaciones adicionales si se va a crear cuenta de usuario
                if (string.IsNullOrWhiteSpace(dto.Username))
                {
                    log.Warn("CreateWithAccount: Validación fallida - Username obligatorio cuando se crea cuenta");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Validación fallida: Username es obligatorio cuando se crea cuenta de usuario",
                        Detalles = "Falta Username para crear cuenta",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Username es obligatorio cuando se crea cuenta de usuario" });
                }

                if (string.IsNullOrWhiteSpace(dto.CorreoCorporativo))
                {
                    log.Warn("CreateWithAccount: Validación fallida - Correo corporativo obligatorio cuando se crea cuenta");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = "Validación fallida: Correo corporativo es obligatorio cuando se crea cuenta de usuario",
                        Detalles = "Falta Correo Corporativo para crear cuenta",
                        IdUsuario = null
                    });
                    return BadRequest(new { message = "Correo corporativo es obligatorio cuando se crea cuenta de usuario" });
                }
            }

            try
            {
                var success = await _personalService.CreateWithAccountAsync(dto);
                
                if (!success)
                {
                    var mensaje = "No se pudo crear el personal";
                    var detalle = dto.CrearCuentaUsuario 
                        ? "Verifica que el username no exista y que todos los datos sean válidos" 
                        : "Verifica que los datos sean válidos";
                    
                    // Si se proporcionó documento, probablemente sea duplicado
                    if (!string.IsNullOrWhiteSpace(dto.Documento))
                    {
                        detalle = "El documento proporcionado ya está registrado en el sistema";
                    }
                    
                    log.Warn($"CreateWithAccount: {mensaje} - {detalle}");
                    await _logService.AddAsync(new LogSistemaCreateDTO
                    {
                        Nivel = "WARN",
                        Mensaje = mensaje,
                        Detalles = detalle,
                        IdUsuario = null
                    });
                    
                    return BadRequest(new { 
                        message = mensaje,
                        detalle = detalle
                    });
                }

                log.Info("CreateWithAccount completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Create Personal With Account",
                    Detalles = dto.CrearCuentaUsuario 
                        ? $"Personal creado con cuenta de usuario: {dto.Username}" 
                        : $"Personal creado sin cuenta: {dto.Nombres} {dto.Apellidos}",
                    IdUsuario = null
                });

                return Ok(new { 
                    message = dto.CrearCuentaUsuario 
                        ? "Personal creado con cuenta de usuario. Se ha enviado un correo de activación." 
                        : "Personal creado exitosamente",
                    conCuentaUsuario = dto.CrearCuentaUsuario,
                    username = dto.CrearCuentaUsuario ? dto.Username : null,
                    instrucciones = dto.CrearCuentaUsuario 
                        ? "El usuario recibirá un correo con el enlace de activación. Tiene 24 horas para activar su cuenta." 
                        : null
                });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante CreateWithAccount", ex);
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
                Detalles = $"Actualizando Personal con id: {id}",
                IdUsuario = null
            });

            try
            {
                var ok = await _personalService.UpdateAsync(id, dto);
                
                if (!ok)
                {
                    // Si se proporcionó documento, probablemente sea duplicado
                    if (!string.IsNullOrWhiteSpace(dto.Documento))
                    {
                        log.Warn($"Update: No se pudo actualizar - documento duplicado o personal no existe para id: {id}");
                        await _logService.AddAsync(new LogSistemaCreateDTO
                        {
                            Nivel = "WARN",
                            Mensaje = $"No se pudo actualizar el personal: {id}",
                            Detalles = "El documento proporcionado ya está registrado en otro personal o el personal no existe",
                            IdUsuario = null
                        });
                        return BadRequest(new { 
                            message = "No se pudo actualizar el personal",
                            detalle = "El documento proporcionado ya está registrado en otro personal o el personal no existe"
                        });
                    }
                    
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
                Detalles = $"Eliminando Personal con id: {id}",
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

        /// <summary>
        /// Verificar si un documento ya existe en el sistema
        /// GET /api/personal/verificar-documento/{documento}
        /// </summary>
        [HttpGet("verificar-documento/{documento}")]
        public async Task<IActionResult> VerificarDocumento(string documento)
        {
            log.Info($"VerificarDocumento iniciado para documento: {documento}");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: Verificar Documento",
                Detalles = $"Verificando si el documento {documento} existe",
                IdUsuario = null
            });

            if (string.IsNullOrWhiteSpace(documento))
            {
                log.Warn("VerificarDocumento: Validación fallida - Documento es requerido");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "WARN",
                    Mensaje = "Validación fallida: Documento es requerido",
                    Detalles = "No se proporcionó documento para verificar",
                    IdUsuario = null
                });
                return BadRequest(new { message = "Documento es requerido" });
            }

            try
            {
                var personales = await _personalService.GetAllAsync();
                var existe = personales.Any(p => p.Documento == documento);

                log.Info($"VerificarDocumento completado correctamente - Existe: {existe}");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: Verificar Documento",
                    Detalles = $"Documento {documento} - Existe: {existe}",
                    IdUsuario = null
                });

                return Ok(new { 
                    existe = existe,
                    documento = documento,
                    mensaje = existe 
                        ? "El documento ya está registrado en el sistema" 
                        : "El documento está disponible"
                });
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante VerificarDocumento", ex);
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

        // ===========================
        // ✅ NUEVO: LISTADO UNIFICADO PARA GESTIÓN DE USUARIOS
        // ===========================

        /// <summary>
        /// Obtener listado completo para Gestión de Usuarios (LEFT JOIN Personal → Usuario → RolesSistema)
        /// GET /api/personal/gestion-usuarios
        /// </summary>
        [HttpGet("gestion-usuarios")]
        public async Task<IActionResult> GetGestionUsuarios()
        {
            log.Info("GetGestionUsuarios iniciado");
            await _logService.AddAsync(new LogSistemaCreateDTO
            {
                Nivel = "INFO",
                Mensaje = "Petición recibida: GetGestionUsuarios",
                Detalles = "Obteniendo listado unificado de Personal con Usuarios",
                IdUsuario = null
            });

            try
            {
                var lista = await _personalService.GetUnifiedListAsync();
                
                log.Info("GetGestionUsuarios completado correctamente");
                await _logService.AddAsync(new LogSistemaCreateDTO
                {
                    Nivel = "INFO",
                    Mensaje = "Operación completada correctamente: GetGestionUsuarios",
                    Detalles = $"Total registros obtenidos: {lista.Count()}",
                    IdUsuario = null
                });
                
                return Ok(lista);
            }
            catch (Exception ex)
            {
                log.Error("Error inesperado durante GetGestionUsuarios", ex);
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

        // ===========================
        // ENDPOINTS BÁSICOS DE PERSONAL
        // ===========================
    }
}
