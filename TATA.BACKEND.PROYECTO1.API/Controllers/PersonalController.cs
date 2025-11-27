using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Requiere token JWT válido
    public class PersonalController : ControllerBase
    {
        private readonly IPersonalService _personalService;

        public PersonalController(IPersonalService personalService)
        {
            _personalService = personalService;
        }

        // ✅ OBTENER TODOS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var personales = await _personalService.GetAllAsync();
            return Ok(personales);
        }

        // ✅ OBTENER UNO POR ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var personal = await _personalService.GetByIdAsync(id);
            if (personal == null)
                return NotFound(new { message = "Personal no encontrado" });

            return Ok(personal);
        }

        // ✅ CREAR NUEVO (Simple - sin cuenta de usuario)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonalCreateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombres) || string.IsNullOrWhiteSpace(dto.Apellidos))
                return BadRequest(new { message = "Nombres y Apellidos son obligatorios" });

            var ok = await _personalService.CreateAsync(dto);
            if (!ok)
                return BadRequest(new { 
                    message = "Error al registrar personal",
                    detalle = !string.IsNullOrWhiteSpace(dto.Documento) 
                        ? "El documento proporcionado ya está registrado en el sistema" 
                        : "Verifica que todos los datos sean válidos"
                });

            return Ok(new { message = "Personal registrado correctamente" });
        }

        /// <summary>
        /// ⚠️ NUEVO: Crear Personal con Cuenta de Usuario (Condicional)
        /// POST /api/personal/with-account
        /// </summary>
        [HttpPost("with-account")]
        public async Task<IActionResult> CreateWithAccount([FromBody] PersonalCreateWithAccountDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombres) || string.IsNullOrWhiteSpace(dto.Apellidos))
                return BadRequest(new { message = "Nombres y Apellidos son obligatorios" });

            if (dto.CrearCuentaUsuario)
            {
                // Validaciones adicionales si se va a crear cuenta de usuario
                if (string.IsNullOrWhiteSpace(dto.Username))
                    return BadRequest(new { message = "Username es obligatorio cuando se crea cuenta de usuario" });

                if (string.IsNullOrWhiteSpace(dto.CorreoCorporativo))
                    return BadRequest(new { message = "Correo corporativo es obligatorio cuando se crea cuenta de usuario" });
            }

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
                
                return BadRequest(new { 
                    message = mensaje,
                    detalle = detalle
                });
            }

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

        // ✅ ACTUALIZAR
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonalUpdateDTO dto)
        {
            var ok = await _personalService.UpdateAsync(id, dto);
            if (!ok)
            {
                // Si se proporcionó documento, probablemente sea duplicado
                if (!string.IsNullOrWhiteSpace(dto.Documento))
                {
                    return BadRequest(new { 
                        message = "No se pudo actualizar el personal",
                        detalle = "El documento proporcionado ya está registrado en otro personal o el personal no existe"
                    });
                }
                
                return NotFound(new { message = "Personal no encontrado" });
            }

            return Ok(new { message = "Personal actualizado correctamente" });
        }

        // ✅ ELIMINAR
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _personalService.DeleteAsync(id);
            if (!ok)
                return NotFound(new { message = "Personal no encontrado" });

            return Ok(new { message = "Personal eliminado correctamente" });
        }

        /// <summary>
        /// Verificar si un documento ya existe en el sistema
        /// GET /api/personal/verificar-documento/{documento}
        /// </summary>
        [HttpGet("verificar-documento/{documento}")]
        public async Task<IActionResult> VerificarDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return BadRequest(new { message = "Documento es requerido" });

            var personales = await _personalService.GetAllAsync();
            var existe = personales.Any(p => p.Documento == documento);

            return Ok(new { 
                existe = existe,
                documento = documento,
                mensaje = existe 
                    ? "El documento ya está registrado en el sistema" 
                    : "El documento está disponible"
            });
        }
    }
}
