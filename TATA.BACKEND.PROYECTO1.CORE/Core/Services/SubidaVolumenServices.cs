using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    // ⚠️⚠️⚠️ SERVICIO DESHABILITADO TEMPORALMENTE ⚠️⚠️⚠️
    // Este servicio usa la estructura antigua de Usuario y Personal:
    // - Usuario.Correo (ya no existe, ahora se usa Username)
    // - Personal.IdUsuario (ya no existe, ahora Usuario tiene IdPersonal)
    // 
    // TODO: Refactorizar este servicio para usar la nueva estructura:
    // 1. Usuario ahora tiene IdPersonal (nullable) - Relación 1:0..1
    // 2. Personal no tiene IdUsuario
    // 3. Login usa Username en lugar de Correo
    // 4. El correo corporativo viene de Personal.CorreoCorporativo
    //
    // Para más información, ver: USUARIO_BACKEND_GUIA_COMPLETA.md

    /// <summary>
    /// Servicio de dominio para carga masiva de Solicitudes SLA
    /// a partir de filas que provienen de un Excel.
    /// ⚠️ DESHABILITADO: Usa estructura antigua de DB
    /// </summary>
    public class SubidaVolumenServices : ISubidaVolumenServices
    {
        /*
        // Repositorios necesarios
        private readonly IRolesSistemaRepository _rolesSistemaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPersonalRepository _personalRepository;
        private readonly IConfigSLARepository _configSlaRepository;
        private readonly IRolRegistroRepository _rolRegistroRepository;
        private readonly ISolicitudRepository _solicitudRepository;

        public SubidaVolumenServices(
            IRolesSistemaRepository rolesSistemaRepository,
            IUsuarioRepository usuarioRepository,
            IPersonalRepository personalRepository,
            IConfigSLARepository configSlaRepository,
            IRolRegistroRepository rolRegistroRepository,
            ISolicitudRepository solicitudRepository)
        {
            _rolesSistemaRepository = rolesSistemaRepository;
            _usuarioRepository = usuarioRepository;
            _personalRepository = personalRepository;
            _configSlaRepository = configSlaRepository;
            _rolRegistroRepository = rolRegistroRepository;
            _solicitudRepository = solicitudRepository;
        }
        */

        /// <summary>
        /// ⚠️ MÉTODO DESHABILITADO - Retorna error indicando que el servicio no está disponible
        /// </summary>
        public Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
            IEnumerable<SubidaVolumenSolicitudRowDto> filas)
        {
            var result = new BulkUploadResultDto
            {
                TotalFilas = filas?.Count() ?? 0,
                FilasConError = filas?.Count() ?? 0
            };

            result.Errores.Add(new BulkUploadErrorDto
            {
                RowIndex = 0,
                Mensaje = "⚠️ SERVICIO DESHABILITADO: El servicio de carga masiva está temporalmente deshabilitado debido a cambios en la arquitectura de Usuario y Personal. Por favor, contacte al administrador del sistema."
            });

            return Task.FromResult(result);
        }

        #region CÓDIGO ORIGINAL COMENTADO

        /*
        /// <summary>
        /// Procesa un conjunto de filas de carga masiva, creando/asegurando
        /// datos maestros (RolesSistema, Usuario, Personal, ConfigSla, RolRegistro)
        /// y finalmente insertando la Solicitud correspondiente.
        /// 
        /// ⚠️ ESTE CÓDIGO USA LA ESTRUCTURA ANTIGUA:
        /// - Usuario.Correo (ya no existe)
        /// - Personal.IdUsuario (ya no existe)
        /// </summary>
        public async Task<BulkUploadResultDto> ProcesarSolicitudesAsync_OLD(
            IEnumerable<SubidaVolumenSolicitudRowDto> filas)
        {
            var result = new BulkUploadResultDto();

            if (filas == null)
                return result;

            // 1) Cargar catálogos existentes en memoria para evitar repetir queries
            var rolesSistemaList = await _rolesSistemaRepository.GetAll();
            var configSlaList = (await _configSlaRepository.GetAllAsync()).ToList();
            var rolRegistroList = (await _rolRegistroRepository.GetAllAsync()).ToList();
            var usuariosList = (await _usuarioRepository.GetAllAsync()).ToList();
            var personalList = (await _personalRepository.GetAllAsync()).ToList();

            // Diccionarios para búsquedas rápidas (case-insensitive donde aplica)
            var rolesSistemaByCodigo = rolesSistemaList
                .Where(r => !string.IsNullOrWhiteSpace(r.Codigo))
                .ToDictionary(r => r.Codigo.Trim(), StringComparer.OrdinalIgnoreCase);

            var configSlaByCodigo = configSlaList
                .Where(c => !string.IsNullOrWhiteSpace(c.CodigoSla))
                .ToDictionary(c => c.CodigoSla.Trim(), StringComparer.OrdinalIgnoreCase);

            var rolRegistroByNombre = rolRegistroList
                .Where(r => !string.IsNullOrWhiteSpace(r.NombreRol))
                .ToDictionary(r => r.NombreRol.Trim(), StringComparer.OrdinalIgnoreCase);

            // ⚠️ PROBLEMA: Usuario ya no tiene campo Correo
            // var usuarioByCorreo = usuariosList
            //     .Where(u => !string.IsNullOrWhiteSpace(u.Correo))
            //     .ToDictionary(u => u.Correo.Trim(), StringComparer.OrdinalIgnoreCase);

            var personalByDocumento = personalList
                .Where(p => !string.IsNullOrWhiteSpace(p.Documento))
                .ToDictionary(p => p.Documento.Trim(), StringComparer.OrdinalIgnoreCase);

            // 2) Recorrer filas
            var filasLista = filas.ToList();
            result.TotalFilas = filasLista.Count;

            int rowIndex = 1; // 1-based, parecido a Excel

            foreach (var row in filasLista)
            {
                try
                {
                    // Validaciones mínimas de campos obligatorios
                    if (row == null ||
                        string.IsNullOrWhiteSpace(row.RolSistemaCodigo) ||
                        string.IsNullOrWhiteSpace(row.UsuarioCorreo) ||
                        string.IsNullOrWhiteSpace(row.PersonalDocumento) ||
                        string.IsNullOrWhiteSpace(row.PersonalNombres) ||
                        string.IsNullOrWhiteSpace(row.PersonalApellidos) ||
                        string.IsNullOrWhiteSpace(row.ConfigSlaCodigo) ||
                        string.IsNullOrWhiteSpace(row.RolRegistroNombre))
                    {
                        RegistrarError(result, rowIndex, "Campos obligatorios faltantes en la fila.");
                        rowIndex++;
                        continue;
                    }

                    // Validación de fechas coherentes
                    if (row.SolFechaIngreso < row.SolFechaSolicitud)
                    {
                        RegistrarError(result, rowIndex, "La fecha de ingreso es menor que la fecha de solicitud.");
                        rowIndex++;
                        continue;
                    }

                    // Normalizar algunos valores (trim)
                    string rolSistemaCodigo = row.RolSistemaCodigo.Trim();
                    string usuarioCorreo = row.UsuarioCorreo.Trim();
                    string personalDocumento = row.PersonalDocumento.Trim();
                    string configSlaCodigo = row.ConfigSlaCodigo.Trim();
                    string rolRegistroNombre = row.RolRegistroNombre.Trim();

                    // 2.1) Asegurar RolesSistema
                    if (!rolesSistemaByCodigo.TryGetValue(rolSistemaCodigo, out var rolSistema))
                    {
                        rolSistema = new RolesSistema
                        {
                            Codigo = rolSistemaCodigo,
                            Nombre = string.IsNullOrWhiteSpace(row.RolSistemaNombre)
                                ? rolSistemaCodigo
                                : row.RolSistemaNombre!.Trim(),
                            Descripcion = row.RolSistemaDescripcion?.Trim(),
                            EsActivo = true
                        };

                        await _rolesSistemaRepository.Add(rolSistema);
                        rolesSistemaByCodigo[rolSistemaCodigo] = rolSistema;
                    }

                    // 2.2) Asegurar Usuario (por correo) ⚠️ YA NO FUNCIONA
                    // if (!usuarioByCorreo.TryGetValue(usuarioCorreo, out var usuario))
                    // {
                    //     var passwordRandom = GenerarPasswordSeguro(12);
                    //
                    //     usuario = new Usuario
                    //     {
                    //         Username = string.IsNullOrWhiteSpace(row.UsuarioUsername)
                    //             ? usuarioCorreo.Split('@')[0]
                    //             : row.UsuarioUsername!.Trim(),
                    //         Correo = usuarioCorreo, // ⚠️ Ya no existe
                    //         PasswordHash = passwordRandom,
                    //         IdRolSistema = rolSistema.IdRolSistema,
                    //         Estado = "INACTIVO",
                    //         CreadoEn = DateTime.UtcNow
                    //     };
                    //
                    //     await _usuarioRepository.AddAsync(usuario);
                    //     usuarioByCorreo[usuarioCorreo] = usuario;
                    // }

                    // 2.3) Asegurar Personal (por documento) ⚠️ YA NO FUNCIONA
                    // if (!personalByDocumento.TryGetValue(personalDocumento, out var personal))
                    // {
                    //     personal = new Personal
                    //     {
                    //         Nombres = row.PersonalNombres.Trim(),
                    //         Apellidos = row.PersonalApellidos.Trim(),
                    //         Documento = personalDocumento,
                    //         CorreoCorporativo = row.PersonalCorreo.Trim(),
                    //         Estado = "INACTIVO",
                    //         IdUsuario = usuario.IdUsuario, // ⚠️ Ya no existe
                    //         CreadoEn = DateTime.UtcNow
                    //     };
                    //
                    //     await _personalRepository.AddAsync(personal);
                    //     personalByDocumento[personalDocumento] = personal;
                    // }

                    // 2.4) Asegurar ConfigSla (por CodigoSla)
                    if (!configSlaByCodigo.TryGetValue(configSlaCodigo, out var configSla))
                    {
                        configSla = new ConfigSla
                        {
                            CodigoSla = configSlaCodigo,
                            Descripcion = row.ConfigSlaDescripcion?.Trim(),
                            DiasUmbral = row.ConfigSlaDiasUmbral,
                            TipoSolicitud = row.ConfigSlaTipoSolicitud.Trim(),
                            EsActivo = true,
                            CreadoEn = DateTime.UtcNow,
                            ActualizadoEn = DateTime.UtcNow
                        };

                        var idNuevo = await _configSlaRepository.InsertAsync(configSla);
                        configSla.IdSla = idNuevo;
                        configSlaByCodigo[configSlaCodigo] = configSla;
                    }

                    // 2.5) Asegurar RolRegistro (por NombreRol)
                    if (!rolRegistroByNombre.TryGetValue(rolRegistroNombre, out var rolRegistro))
                    {
                        rolRegistro = new RolRegistro
                        {
                            NombreRol = rolRegistroNombre,
                            BloqueTech = row.RolRegistroBloqueTech.Trim(),
                            Descripcion = row.RolRegistroDescripcion?.Trim(),
                            EsActivo = true
                        };

                        var idRol = await _rolRegistroRepository.InsertAsync(rolRegistro);
                        rolRegistro.IdRolRegistro = idRol;
                        rolRegistroByNombre[rolRegistroNombre] = rolRegistro;
                    }

                    // 2.6) Crear Solicitud
                    var origenDato = string.IsNullOrWhiteSpace(row.SolOrigenDato)
                        ? "IMPORT"
                        : row.SolOrigenDato!.Trim();

                    var estadoSolicitud = string.IsNullOrWhiteSpace(row.SolEstado)
                        ? "ACTIVO"
                        : row.SolEstado!.Trim();

                    var fechaSolicitudDate = row.SolFechaSolicitud.Date;
                    var fechaIngresoDate = row.SolFechaIngreso.Date;

                    var dias = (fechaIngresoDate - fechaSolicitudDate).TotalDays;
                    if (dias < 0)
                    {
                        RegistrarError(result, rowIndex, "La fecha de ingreso es menor que la fecha de solicitud.");
                        rowIndex++;
                        continue;
                    }

                    var numDias = (int)Math.Ceiling(dias);

                    var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla)
                        ? $"SLA{configSla.IdSla}"
                        : configSla.CodigoSla;

                    var cumple = numDias <= configSla.DiasUmbral;
                    var estadoCumplimiento = cumple
                        ? $"CUMPLE_{codigo}"
                        : $"NO_CUMPLE_{codigo}";

                    // var solicitud = new Solicitud
                    // {
                    //     IdPersonal = personal.IdPersonal,
                    //     IdSla = configSla.IdSla,
                    //     IdRolRegistro = rolRegistro.IdRolRegistro,
                    //     CreadoPor = usuario.IdUsuario,
                    //     FechaSolicitud = DateOnly.FromDateTime(fechaSolicitudDate),
                    //     FechaIngreso = DateOnly.FromDateTime(fechaIngresoDate),
                    //     NumDiasSla = numDias,
                    //     ResumenSla = row.SolResumen?.Trim() ?? string.Empty,
                    //     OrigenDato = origenDato,
                    //     EstadoSolicitud = estadoSolicitud,
                    //     EstadoCumplimientoSla = estadoCumplimiento,
                    //     CreadoEn = DateTime.UtcNow,
                    //     ActualizadoEn = DateTime.UtcNow
                    // };
                    //
                    // await _solicitudRepository.CreateSolicitudAsync(solicitud);
                    //
                    // result.FilasExitosas++;
                }
                catch (Exception ex)
                {
                    RegistrarError(result, rowIndex, ex.Message);
                }

                rowIndex++;
            }

            return result;
        }
        */

        #endregion

        // ----------------- helpers privados -----------------

        private static void RegistrarError(BulkUploadResultDto result, int rowIndex, string mensaje)
        {
            result.FilasConError++;
            result.Errores.Add(new BulkUploadErrorDto
            {
                RowIndex = rowIndex,
                Mensaje = mensaje
            });
        }

        /// <summary>
        /// Genera una contraseña aleatoria "segura" de longitud dada.
        /// Aquí solo se genera en texto plano; el hash real debería
        /// aplicarse en otro punto si lo necesitas.
        /// </summary>
        private static string GenerarPasswordSeguro(int longitud)
        {
            const string alfabeto = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[longitud];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var sb = new StringBuilder(longitud);
            for (int i = 0; i < longitud; i++)
            {
                sb.Append(alfabeto[bytes[i] % alfabeto.Length]);
            }
            return sb.ToString();
        }
    }
}
