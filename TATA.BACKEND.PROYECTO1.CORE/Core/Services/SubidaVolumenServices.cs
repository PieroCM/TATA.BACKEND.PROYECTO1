using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;
using log4net;
using System;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    /// <summary>
    /// Servicio de dominio para carga masiva de Solicitudes SLA
    /// a partir de filas que provienen de un Excel.
    /// Optimizado para soportar 100, 1,000, 10,000+ filas.
    /// </summary>
    public class SubidaVolumenServices : ISubidaVolumenServices
    {
        // 2. DECLARACIÓN ESTÁTICA DEL LOGGER
        private static readonly ILog log = LogManager.GetLogger(typeof(SubidaVolumenServices));

        // Repositorios necesarios
        private readonly IRolesSistemaRepository _rolesSistemaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPersonalRepository _personalRepository;
        private readonly IConfigSLARepository _configSlaRepository;
        private readonly IRolRegistroRepository _rolRegistroRepository;
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly ILogService _logService;

        // TimeZone de Perú para cálculo correcto de "hoy"
        private static readonly TimeZoneInfo PeruTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public SubidaVolumenServices(
            IRolesSistemaRepository rolesSistemaRepository,
            IUsuarioRepository usuarioRepository,
            IPersonalRepository personalRepository,
            IConfigSLARepository configSlaRepository,
            IRolRegistroRepository rolRegistroRepository,
            ISolicitudRepository solicitudRepository,
            ILogService logService)
        {
            _rolesSistemaRepository = rolesSistemaRepository;
            _usuarioRepository = usuarioRepository;
            _personalRepository = personalRepository;
            _configSlaRepository = configSlaRepository;
            _rolRegistroRepository = rolRegistroRepository;
            _solicitudRepository = solicitudRepository;
            _logService = logService;
        }

        /// <summary>
        /// Procesa un conjunto de filas de carga masiva, creando/asegurando
        /// datos maestros (RolesSistema, Usuario, Personal, ConfigSla, RolRegistro)
        /// y finalmente insertando la Solicitud correspondiente.
        /// Optimizado para rendimiento O(n) con grandes volúmenes.
        /// </summary>
        public async Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
            IEnumerable<SubidaVolumenSolicitudRowDto> filas)
        {
            var result = new BulkUploadResultDto();

            // Loguear el inicio de la operación
            log.Info($"Iniciando procesamiento masivo de {filas?.Count() ?? 0} filas.");


            if (filas == null)
                return result;

            var filasLista = filas.ToList();
            result.TotalFilas = filasLista.Count;

            if (result.TotalFilas == 0)
                return result;

            // 1) Cargar catálogos existentes en memoria para evitar repetir queries
            var rolesSistemaList = await _rolesSistemaRepository.GetAll();
            var configSlaList = (await _configSlaRepository.GetAllAsync()).ToList();
            var rolRegistroList = (await _rolRegistroRepository.GetAllAsync()).ToList();
            var usuariosList = (await _usuarioRepository.GetAllAsync()).ToList();
            var personalList = (await _personalRepository.GetAllAsync()).ToList();

            // Diccionarios para búsquedas rápidas O(1) - case-insensitive
            var rolesSistemaByCodigo = rolesSistemaList
                .Where(r => !string.IsNullOrWhiteSpace(r.Codigo))
                .ToDictionary(r => r.Codigo.Trim(), StringComparer.OrdinalIgnoreCase);

            var configSlaByCodigo = configSlaList
                .Where(c => !string.IsNullOrWhiteSpace(c.CodigoSla))
                .ToDictionary(c => c.CodigoSla.Trim(), StringComparer.OrdinalIgnoreCase);

            var rolRegistroByNombre = rolRegistroList
                .Where(r => !string.IsNullOrWhiteSpace(r.NombreRol))
                .ToDictionary(r => r.NombreRol.Trim(), StringComparer.OrdinalIgnoreCase);

            // Diccionario por CORREO (clave principal de negocio)
            var usuarioByCorreo = usuariosList
                .Where(u => !string.IsNullOrWhiteSpace(u.Correo))
                .ToDictionary(u => u.Correo.Trim(), StringComparer.OrdinalIgnoreCase);

            // Diccionario por USERNAME (para evitar duplicados de username)
            var usuarioByUsername = usuariosList
                .Where(u => !string.IsNullOrWhiteSpace(u.Username))
                .ToDictionary(u => u.Username.Trim(), StringComparer.OrdinalIgnoreCase);

            var personalByDocumento = personalList
                .Where(p => !string.IsNullOrWhiteSpace(p.Documento))
                .ToDictionary(p => p.Documento.Trim(), StringComparer.OrdinalIgnoreCase);

            // Obtener fecha actual en zona horaria de Perú
            var hoyPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone).Date;

            // 2) Recorrer filas
            int rowIndex = 1; // 1-based, parecido a Excel

            foreach (var row in filasLista)
            {
                try
                {
                    // Validaciones mínimas de campos obligatorios (excepto fechas)
                    if (!ValidarCamposObligatorios(row, result, rowIndex))
                    {
                        rowIndex++;
                        continue;
                    }

                    // ============================
                    // 2.0 Parseo de fechas (string)
                    // ============================

                    // SolFechaSolicitud es OBLIGATORIA
                    if (string.IsNullOrWhiteSpace(row.SolFechaSolicitud))
                    {
                        RegistrarError(result, rowIndex,
                            "La fecha de solicitud (SolFechaSolicitud) es obligatoria.");
                        rowIndex++;
                        continue;
                    }

                    if (!DateTime.TryParse(row.SolFechaSolicitud, out var fechaSolicitud))
                    {
                        RegistrarError(result, rowIndex,
                            "La fecha de solicitud (SolFechaSolicitud) tiene un formato inválido.");
                        rowIndex++;
                        continue;
                    }

                    fechaSolicitud = fechaSolicitud.Date;

                    // SolFechaIngreso es OPCIONAL
                    DateTime? fechaIngreso = null;
                    if (!string.IsNullOrWhiteSpace(row.SolFechaIngreso))
                    {
                        if (!DateTime.TryParse(row.SolFechaIngreso, out var fechaIngresoParsed))
                        {
                            RegistrarError(result, rowIndex,
                                "La fecha de ingreso (SolFechaIngreso) tiene un formato inválido.");
                            rowIndex++;
                            continue;
                        }

                        fechaIngreso = fechaIngresoParsed.Date;
                    }

                    // Validar lógica de fechas (no futuro, ingreso >= solicitud, etc.)
                    if (!ValidarFechas(fechaSolicitud, fechaIngreso, hoyPeru, result, rowIndex))
                    {
                        rowIndex++;
                        continue;
                    }

                    // ============================
                    // Normalizar valores (trim)
                    // ============================
                    string rolSistemaCodigo = row.RolSistemaCodigo.Trim();
                    string usuarioCorreo = row.UsuarioCorreo.Trim();
                    string personalDocumento = row.PersonalDocumento.Trim();
                    string configSlaCodigo = row.ConfigSlaCodigo.Trim();
                    string rolRegistroNombre = row.RolRegistroNombre.Trim();

                    // 2.1) Asegurar RolesSistema
                    var rolSistema = await AsegurarRolSistema(
                        rolSistemaCodigo,
                        row.RolSistemaNombre,
                        row.RolSistemaDescripcion,
                        rolesSistemaByCodigo);

                    // 2.2) Asegurar Usuario (por correo, validando también username)
                    var usuario = await AsegurarUsuario(
                        usuarioCorreo,
                        row.UsuarioUsername,
                        rolSistema.IdRolSistema,
                        usuarioByCorreo,
                        usuarioByUsername);

                    // 2.3) Asegurar Personal (por documento)
                    var personal = await AsegurarPersonal(
                        personalDocumento,
                        row.PersonalNombres,
                        row.PersonalApellidos,
                        row.PersonalCorreo,
                        usuario.IdUsuario,
                        personalByDocumento);

                    // 2.4) Asegurar ConfigSla (por CodigoSla)
                    var configSla = await AsegurarConfigSla(
                        configSlaCodigo,
                        row.ConfigSlaDescripcion,
                        row.ConfigSlaDiasUmbral,
                        row.ConfigSlaTipoSolicitud,
                        configSlaByCodigo);

                    // 2.5) Asegurar RolRegistro (por NombreRol)
                    var rolRegistro = await AsegurarRolRegistro(
                        rolRegistroNombre,
                        row.RolRegistroBloqueTech,
                        row.RolRegistroDescripcion,
                        rolRegistroByNombre);

                    // 2.6) Calcular SLA y crear Solicitud
                    var solicitud = CrearSolicitud(
                        row,
                        personal.IdPersonal,
                        configSla,
                        rolRegistro.IdRolRegistro,
                        usuario.IdUsuario,
                        fechaSolicitud,
                        fechaIngreso,
                        hoyPeru);

                    await _solicitudRepository.CreateSolicitudAsync(solicitud);

                    result.FilasExitosas++;
                    // Loguear cada fila exitosa (opcional, pero útil para depurar grandes cargas)
                    log.Debug($"Fila {rowIndex} procesada exitosamente. Solicitud creada para documento: {row.PersonalDocumento}");
                }
                catch (Exception ex)
                {
                    // LOGUEAR EL ERROR CRÍTICO con la excepción para guardar detalles en la BD
                    log.Error($"Error CRÍTICO en la fila {rowIndex}. Mensaje: {ex.Message}", ex);

                    RegistrarError(result, rowIndex, $"Error inesperado: {ex.Message}");
                }

                rowIndex++;
            }
            // Loguear el resultado final de la operación
            log.Info($"Procesamiento masivo finalizado. Éxitos: {result.FilasExitosas}, Errores: {result.FilasConError}.");

            return result;
        }

        // ----------------- Métodos privados auxiliares -----------------

        /// <summary>
        /// Valida que todos los campos obligatorios estén presentes
        /// (excepto las fechas, que se validan con TryParse).
        /// </summary>
        private bool ValidarCamposObligatorios(SubidaVolumenSolicitudRowDto row,
            BulkUploadResultDto result, int rowIndex)
        {
            var camposFaltantes = new List<string>();

            if (row == null)
            {
                RegistrarError(result, rowIndex, "La fila no contiene datos válidos.");
                return false;
            }

            // Validar campos de texto obligatorios
            if (string.IsNullOrWhiteSpace(row.RolSistemaCodigo))
                camposFaltantes.Add("RolSistemaCodigo");

            if (string.IsNullOrWhiteSpace(row.UsuarioCorreo))
                camposFaltantes.Add("UsuarioCorreo");

            if (string.IsNullOrWhiteSpace(row.PersonalDocumento))
                camposFaltantes.Add("PersonalDocumento");

            if (string.IsNullOrWhiteSpace(row.PersonalNombres))
                camposFaltantes.Add("PersonalNombres");

            if (string.IsNullOrWhiteSpace(row.PersonalApellidos))
                camposFaltantes.Add("PersonalApellidos");

            if (string.IsNullOrWhiteSpace(row.PersonalCorreo))
                camposFaltantes.Add("PersonalCorreo");

            if (string.IsNullOrWhiteSpace(row.ConfigSlaCodigo))
                camposFaltantes.Add("ConfigSlaCodigo");

            if (string.IsNullOrWhiteSpace(row.ConfigSlaTipoSolicitud))
                camposFaltantes.Add("ConfigSlaTipoSolicitud");

            if (string.IsNullOrWhiteSpace(row.RolRegistroNombre))
                camposFaltantes.Add("RolRegistroNombre");

            if (string.IsNullOrWhiteSpace(row.RolRegistroBloqueTech))
                camposFaltantes.Add("RolRegistroBloqueTech");

            // SolFechaSolicitud se valida como string requerida
            if (string.IsNullOrWhiteSpace(row.SolFechaSolicitud))
                camposFaltantes.Add("SolFechaSolicitud");

            if (camposFaltantes.Count > 0)
            {
                var mensaje = $"Campos obligatorios faltantes: {string.Join(", ", camposFaltantes)}";
                RegistrarError(result, rowIndex, mensaje);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida la lógica de fechas según las reglas del negocio.
        /// </summary>
        private bool ValidarFechas(DateTime fechaSolicitud, DateTime? fechaIngreso,
            DateTime hoyPeru, BulkUploadResultDto result, int rowIndex)
        {
            // Validar que fecha de solicitud no esté en el futuro
            if (fechaSolicitud > hoyPeru)
            {
                RegistrarError(result, rowIndex,
                    "La fecha de solicitud está en el futuro respecto a la fecha actual.");
                return false;
            }

            // Si hay fecha de ingreso, validar que no sea menor a fecha de solicitud
            if (fechaIngreso.HasValue && fechaIngreso.Value < fechaSolicitud)
            {
                RegistrarError(result, rowIndex,
                    "La fecha de ingreso es menor que la fecha de solicitud.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Asegura que exista un RolSistema, creándolo si es necesario.
        /// </summary>
        private async Task<RolesSistema> AsegurarRolSistema(
            string codigo,
            string? nombre,
            string? descripcion,
            Dictionary<string, RolesSistema> diccionario)
        {
            if (!diccionario.TryGetValue(codigo, out var rolSistema))
            {
                rolSistema = new RolesSistema
                {
                    Codigo = codigo,
                    Nombre = string.IsNullOrWhiteSpace(nombre) ? codigo : nombre.Trim(),
                    Descripcion = descripcion?.Trim(),
                    EsActivo = true
                };

                await _rolesSistemaRepository.Add(rolSistema);
                diccionario[codigo] = rolSistema;
            }

            return rolSistema;
        }

        /// <summary>
        /// Asegura que exista un Usuario, creándolo si es necesario.
        /// Valida tanto por correo como por username para evitar duplicados.
        /// </summary>
        private async Task<Usuario> AsegurarUsuario(
            string correo,
            string? username,
            int idRolSistema,
            Dictionary<string, Usuario> diccionarioCorreo,
            Dictionary<string, Usuario> diccionarioUsername)
        {
            if (diccionarioCorreo.TryGetValue(correo, out var usuario))
            {
                return usuario;
            }

            string usernameCalculado = string.IsNullOrWhiteSpace(username)
                ? correo.Split('@')[0]
                : username.Trim();

            if (diccionarioUsername.TryGetValue(usernameCalculado, out var _))
            {
                int contador = 1;
                string usernameBase = usernameCalculado;

                while (diccionarioUsername.ContainsKey(usernameCalculado))
                {
                    usernameCalculado = $"{usernameBase}{contador}";
                    contador++;
                }
            }

            var passwordRandom = GenerarPasswordSeguro(12);

            usuario = new Usuario
            {
                Username = usernameCalculado,
                Correo = correo,
                PasswordHash = passwordRandom, // TODO: aplicar hashing real si es necesario
                IdRolSistema = idRolSistema,
                Estado = "INACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            await _usuarioRepository.AddAsync(usuario);

            diccionarioCorreo[correo] = usuario;
            diccionarioUsername[usernameCalculado] = usuario;

            return usuario;
        }

        /// <summary>
        /// Asegura que exista un Personal, creándolo si es necesario.
        /// </summary>
        private async Task<Personal> AsegurarPersonal(
            string documento,
            string nombres,
            string apellidos,
            string correo,
            int idUsuario,
            Dictionary<string, Personal> diccionario)
        {
            if (!diccionario.TryGetValue(documento, out var personal))
            {
                personal = new Personal
                {
                    Nombres = nombres.Trim(),
                    Apellidos = apellidos.Trim(),
                    Documento = documento,
                    CorreoCorporativo = correo.Trim(),
                    Estado = "ACTIVO",
                    IdUsuario = idUsuario,
                    CreadoEn = DateTime.UtcNow
                };

                await _personalRepository.AddAsync(personal);
                diccionario[documento] = personal;
            }

            return personal;
        }

        /// <summary>
        /// Asegura que exista un ConfigSla, creándolo si es necesario.
        /// </summary>
        private async Task<ConfigSla> AsegurarConfigSla(
            string codigo,
            string? descripcion,
            int diasUmbral,
            string tipoSolicitud,
            Dictionary<string, ConfigSla> diccionario)
        {
            if (!diccionario.TryGetValue(codigo, out var configSla))
            {
                configSla = new ConfigSla
                {
                    CodigoSla = codigo,
                    Descripcion = descripcion?.Trim(),
                    DiasUmbral = diasUmbral,
                    TipoSolicitud = tipoSolicitud.Trim(),
                    EsActivo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };

                var idNuevo = await _configSlaRepository.InsertAsync(configSla);
                configSla.IdSla = idNuevo;
                diccionario[codigo] = configSla;
            }

            return configSla;
        }

        /// <summary>
        /// Asegura que exista un RolRegistro, creándolo si es necesario.
        /// </summary>
        private async Task<RolRegistro> AsegurarRolRegistro(
            string nombre,
            string bloqueTech,
            string? descripcion,
            Dictionary<string, RolRegistro> diccionario)
        {
            if (!diccionario.TryGetValue(nombre, out var rolRegistro))
            {
                rolRegistro = new RolRegistro
                {
                    NombreRol = nombre,
                    BloqueTech = bloqueTech.Trim(),
                    Descripcion = descripcion?.Trim(),
                    EsActivo = true
                };

                var idRol = await _rolRegistroRepository.InsertAsync(rolRegistro);
                rolRegistro.IdRolRegistro = idRol;
                diccionario[nombre] = rolRegistro;
            }

            return rolRegistro;
        }

        /// <summary>
        /// Crea la entidad Solicitud con el cálculo de SLA según las reglas de negocio.
        /// Reglas:
        /// - ACTIVA: sin fechaIngreso, dentro del umbral (EN_PROCESO_{codigo}).
        /// - INACTIVA: con fechaIngreso, cumpla o no el umbral.
        /// - VENCIDA: sin fechaIngreso, pero el SLA ya se venció. Se autocierra
        ///            usando fechaIngreso = fechaSolicitud + DiasUmbral y NO_CUMPLE_{codigo}.
        /// </summary>
        private Solicitud CrearSolicitud(
            SubidaVolumenSolicitudRowDto row,
            int idPersonal,
            ConfigSla configSla,
            int idRolRegistro,
            int creadoPor,
            DateTime fechaSolicitud,
            DateTime? fechaIngreso,
            DateTime hoyPeru)
        {
            int numDiasSla;
            string estadoCumplimiento;
            string estadoSolicitud;
            string resumenSla;

            var codigo = string.IsNullOrWhiteSpace(configSla.CodigoSla)
                ? $"SLA{configSla.IdSla}"
                : configSla.CodigoSla;

            // 1) Con fechaIngreso explícita -> INACTIVA (cumpla o no el SLA)
            if (fechaIngreso.HasValue)
            {
                var dias = (int)Math.Floor((fechaIngreso.Value - fechaSolicitud).TotalDays);
                numDiasSla = dias;

                if (dias <= configSla.DiasUmbral)
                {
                    estadoCumplimiento = $"CUMPLE_{codigo}";
                    resumenSla =
                        $"Solicitud atendida dentro del SLA ({dias} de {configSla.DiasUmbral} días).";
                }
                else
                {
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    resumenSla =
                        $"Solicitud atendida fuera del SLA ({dias} de {configSla.DiasUmbral} días).";
                }

                estadoSolicitud = "INACTIVA";
            }
            // 2) Sin fechaIngreso -> puede ser ACTIVA (en proceso) o VENCIDA (autocierre)
            else
            {
                var diasTranscurridos = (int)Math.Floor((hoyPeru - fechaSolicitud).TotalDays);
                numDiasSla = diasTranscurridos;

                // ACTIVA: todavía dentro del umbral
                if (diasTranscurridos <= configSla.DiasUmbral)
                {
                    if (numDiasSla < 0) numDiasSla = 0; // por seguridad

                    estadoCumplimiento = $"EN_PROCESO_{codigo}";
                    estadoSolicitud = "ACTIVA";
                    resumenSla =
                        $"Solicitud PENDIENTE dentro del SLA ({numDiasSla} de {configSla.DiasUmbral} días).";
                }
                // VENCIDA: se pasó el umbral sin registrar fechaIngreso -> autocerrar
                else
                {
                    var fechaIngresoAuto = fechaSolicitud.AddDays(configSla.DiasUmbral);

                    fechaIngreso = fechaIngresoAuto;
                    numDiasSla = configSla.DiasUmbral;

                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    estadoSolicitud = "VENCIDA";
                    resumenSla =
                        $"Solicitud VENCIDA: no se registró fecha de ingreso y se superó el umbral del SLA " +
                        $"({diasTranscurridos} de {configSla.DiasUmbral} días). " +
                        $"Se cerró automáticamente en la fecha límite del SLA.";
                }
            }

            // OrigenDato: default IMPORT si viene vacío
            var origenDato = string.IsNullOrWhiteSpace(row.SolOrigenDato)
                ? "IMPORT"
                : row.SolOrigenDato.Trim();

            // NO permitimos que Excel sobreescriba EstadoSolicitud,
            // solo permitimos sobrescribir el resumen si viene lleno.
            if (!string.IsNullOrWhiteSpace(row.SolResumen))
            {
                resumenSla = row.SolResumen.Trim();
            }

            return new Solicitud
            {
                IdPersonal = idPersonal,
                IdSla = configSla.IdSla,
                IdRolRegistro = idRolRegistro,
                CreadoPor = creadoPor,
                FechaSolicitud = DateOnly.FromDateTime(fechaSolicitud),
                FechaIngreso = fechaIngreso.HasValue
                    ? DateOnly.FromDateTime(fechaIngreso.Value)
                    : null,
                NumDiasSla = numDiasSla,
                ResumenSla = resumenSla,
                OrigenDato = origenDato,
                EstadoSolicitud = estadoSolicitud,            // ACTIVA / INACTIVA / VENCIDA
                EstadoCumplimientoSla = estadoCumplimiento,   // EN_PROCESO_, CUMPLE_, NO_CUMPLE_*
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Registra un error en el resultado de la carga masiva.
        /// </summary>
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
            const string alfabeto =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
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