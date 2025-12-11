using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;
using TATA.BACKEND.PROYECTO1.CORE.Core.Entities;
using TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.Services
{
    /// <summary>
    /// Servicio de dominio para carga masiva de Solicitudes SLA
    /// a partir de filas que provienen de un Excel.
    /// Optimizado para soportar 100, 1,000, 10,000+ filas.
    /// 
    /// Versión adaptada al nuevo modelo:
    /// - NO crea usuarios nuevos.
    /// - SÍ crea Personal cuando no existe.
    /// - Usa el idUsuarioCreador recibido como parámetro.
    /// - VALIDA duplicados antes de crear solicitudes.
    /// </summary>
    public class SubidaVolumenServices : ISubidaVolumenServices
    {
        // Logger
        private static readonly ILog log = LogManager.GetLogger(typeof(SubidaVolumenServices));

        // Repositorios necesarios
        private readonly IPersonalRepository _personalRepository;
        private readonly IConfigSLARepository _configSlaRepository;
        private readonly IRolRegistroRepository _rolRegistroRepository;
        private readonly ISolicitudRepository _solicitudRepository;
        private readonly ILogService _logService;

        // TimeZone de Perú para cálculo correcto de "hoy"
        private static readonly TimeZoneInfo PeruTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public SubidaVolumenServices(
            IPersonalRepository personalRepository,
            IConfigSLARepository configSlaRepository,
            IRolRegistroRepository rolRegistroRepository,
            ISolicitudRepository solicitudRepository,
            ILogService logService)
        {
            _personalRepository = personalRepository;
            _configSlaRepository = configSlaRepository;
            _rolRegistroRepository = rolRegistroRepository;
            _solicitudRepository = solicitudRepository;
            _logService = logService;
        }

        /// <summary>
        /// Procesa un conjunto de filas de carga masiva, creando/asegurando
        /// datos maestros (Personal, ConfigSla, RolRegistro)
        /// y finalmente insertando la Solicitud correspondiente.
        /// </summary>
        public async Task<BulkUploadResultDto> ProcesarSolicitudesAsync(
            IEnumerable<SubidaVolumenSolicitudRowDto> filas,
            int idUsuarioCreador)
        {
            var result = new BulkUploadResultDto();

            var total = filas?.Count() ?? 0;
            log.Info($"[SubidaVolumen] Iniciando procesamiento masivo de {total} filas.");

            if (filas == null)
                return result;

            var filasLista = filas.ToList();
            result.TotalFilas = filasLista.Count;

            if (result.TotalFilas == 0)
                return result;

            // 1) Cargar catálogos existentes en memoria para evitar repetir queries
            var configSlaList = (await _configSlaRepository.GetAllAsync()).ToList();
            var rolRegistroList = (await _rolRegistroRepository.GetAllAsync()).ToList();
            var personalList = (await _personalRepository.GetAllAsync()).ToList();
            var solicitudesExistentes = (await _solicitudRepository.GetSolicitudsAsync()).ToList();

            // ⚠️ NUEVO: Construir HashSet de claves de solicitudes existentes para validar duplicados
            var solicitudKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in solicitudesExistentes)
            {
                var key = BuildSolicitudKey(
                    s.IdPersonal,
                    s.IdSla,
                    s.IdRolRegistro,
                    s.FechaSolicitud
                );
                solicitudKeys.Add(key);
            }

            log.Info($"[SubidaVolumen] Solicitudes existentes cargadas: {solicitudKeys.Count} claves únicas.");

            // Diccionarios para búsquedas rápidas O(1) - case-insensitive
            var configSlaByCodigo = configSlaList
                .Where(c => !string.IsNullOrWhiteSpace(c.CodigoSla))
                .ToDictionary(c => c.CodigoSla.Trim(), StringComparer.OrdinalIgnoreCase);

            var rolRegistroByNombre = rolRegistroList
                .Where(r => !string.IsNullOrWhiteSpace(r.NombreRol))
                .ToDictionary(r => r.NombreRol.Trim(), StringComparer.OrdinalIgnoreCase);

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
                    string personalDocumento = row.PersonalDocumento.Trim();
                    string configSlaCodigo = row.ConfigSlaCodigo.Trim();
                    string rolRegistroNombre = row.RolRegistroNombre.Trim();

                    // 2.1) Asegurar Personal (por documento)
                    var personal = await AsegurarPersonal(
                        personalDocumento,
                        row.PersonalNombres,
                        row.PersonalApellidos,
                        row.PersonalCorreo,
                        personalByDocumento);

                    // 2.2) Asegurar ConfigSla (por CodigoSla)
                    var configSla = await AsegurarConfigSla(
                        configSlaCodigo,
                        row.ConfigSlaDescripcion,
                        row.ConfigSlaDiasUmbral,
                        row.ConfigSlaTipoSolicitud,
                        configSlaByCodigo);

                    // 2.3) Asegurar RolRegistro (por NombreRol)
                    var rolRegistro = await AsegurarRolRegistro(
                        rolRegistroNombre,
                        row.RolRegistroBloqueTech,
                        row.RolRegistroDescripcion,
                        rolRegistroByNombre);

                    // ⚠️ NUEVO: Validar duplicado ANTES de crear la solicitud
                    var fechaSolicitudOnly = DateOnly.FromDateTime(fechaSolicitud);
                    var nuevaKey = BuildSolicitudKey(
                        personal.IdPersonal,
                        configSla.IdSla,
                        rolRegistro.IdRolRegistro,
                        fechaSolicitudOnly
                    );

                    if (solicitudKeys.Contains(nuevaKey))
                    {
                        // Solicitud duplicada detectada
                        log.Debug($"[SubidaVolumen] Fila {rowIndex} - Solicitud duplicada detectada: {nuevaKey}");
                        RegistrarError(result, rowIndex,
                            "Solicitud duplicada: ya existe una solicitud para este personal, SLA, rol y fecha de solicitud.");
                        rowIndex++;
                        continue;
                    }

                    // Agregar la nueva clave al HashSet para evitar duplicados dentro del mismo lote
                    solicitudKeys.Add(nuevaKey);

                    // 2.4) Calcular SLA y crear Solicitud
                    var solicitud = CrearSolicitud(
                        row,
                        personal.IdPersonal,
                        configSla,
                        rolRegistro.IdRolRegistro,
                        idUsuarioCreador,
                        fechaSolicitud,
                        fechaIngreso,
                        hoyPeru);

                    await _solicitudRepository.CreateSolicitudAsync(solicitud);

                    result.FilasExitosas++;
                    log.Debug($"[SubidaVolumen] Fila {rowIndex} OK. Solicitud creada para documento: {row.PersonalDocumento}.");
                }
                catch (Exception ex)
                {
                    log.Error($"[SubidaVolumen] Error CRÍTICO en la fila {rowIndex}. Mensaje: {ex.Message}", ex);
                    RegistrarError(result, rowIndex, $"Error inesperado: {ex.Message}");
                }

                rowIndex++;
            }

            log.Info($"[SubidaVolumen] Finalizado. Éxitos: {result.FilasExitosas}, Errores: {result.FilasConError}.");
            return result;
        }

        // ----------------- Métodos privados auxiliares -----------------

        /// <summary>
        /// Construye una clave determinística única para identificar solicitudes duplicadas.
        /// Combina IdPersonal, IdSla, IdRolRegistro y FechaSolicitud.
        /// </summary>
        /// <param name="idPersonal">ID del personal</param>
        /// <param name="idSla">ID del SLA</param>
        /// <param name="idRolRegistro">ID del rol de registro</param>
        /// <param name="fechaSolicitud">Fecha de solicitud</param>
        /// <returns>String con formato: "idPersonal|idSla|idRolRegistro|yyyy-MM-dd"</returns>
        private static string BuildSolicitudKey(int idPersonal, int idSla, int idRolRegistro, DateOnly fechaSolicitud)
        {
            return $"{idPersonal}|{idSla}|{idRolRegistro}|{fechaSolicitud:yyyy-MM-dd}";
        }

        /// <summary>
        /// Valida que todos los campos obligatorios estén presentes
        /// (excepto las fechas, que se validan con TryParse).
        /// 
        /// IMPORTANTE: Aquí ya NO obligamos usuario_correo ni rol_sistema_codigo,
        /// solo los campos necesarios para Personal, ConfigSla, RolRegistro y Solicitud.
        /// </summary>
        private bool ValidarCamposObligatorios(
            SubidaVolumenSolicitudRowDto row,
            BulkUploadResultDto result,
            int rowIndex)
        {
            var camposFaltantes = new List<string>();

            if (row == null)
            {
                RegistrarError(result, rowIndex, "La fila no contiene datos válidos.");
                return false;
            }

            // Personal
            if (string.IsNullOrWhiteSpace(row.PersonalDocumento))
                camposFaltantes.Add("PersonalDocumento");

            if (string.IsNullOrWhiteSpace(row.PersonalNombres))
                camposFaltantes.Add("PersonalNombres");

            if (string.IsNullOrWhiteSpace(row.PersonalApellidos))
                camposFaltantes.Add("PersonalApellidos");

            if (string.IsNullOrWhiteSpace(row.PersonalCorreo))
                camposFaltantes.Add("PersonalCorreo");

            // ConfigSla
            if (string.IsNullOrWhiteSpace(row.ConfigSlaCodigo))
                camposFaltantes.Add("ConfigSlaCodigo");

            if (string.IsNullOrWhiteSpace(row.ConfigSlaTipoSolicitud))
                camposFaltantes.Add("ConfigSlaTipoSolicitud");

            // RolRegistro
            if (string.IsNullOrWhiteSpace(row.RolRegistroNombre))
                camposFaltantes.Add("RolRegistroNombre");

            if (string.IsNullOrWhiteSpace(row.RolRegistroBloqueTech))
                camposFaltantes.Add("RolRegistroBloqueTech");

            // Fecha solicitud (se valida como string requerida)
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
        private bool ValidarFechas(
            DateTime fechaSolicitud,
            DateTime? fechaIngreso,
            DateTime hoyPeru,
            BulkUploadResultDto result,
            int rowIndex)
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
        /// Asegura que exista un Personal, creándolo si es necesario (NUEVO MODELO).
        /// </summary>
        private async Task<Personal> AsegurarPersonal(
            string documento,
            string nombres,
            string apellidos,
            string correo,
            Dictionary<string, Personal> diccionario)
        {
            if (diccionario.TryGetValue(documento, out var personalExistente))
            {
                return personalExistente;
            }

            var personal = new Personal
            {
                Nombres = nombres.Trim(),
                Apellidos = apellidos.Trim(),
                Documento = documento.Trim(),
                CorreoCorporativo = correo.Trim(),
                Estado = "ACTIVO",
                CreadoEn = DateTime.UtcNow
            };

            await _personalRepository.AddAsync(personal);
            diccionario[documento] = personal;

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
        /// 
        /// ESTADOS DE SOLICITUD (EstadoSolicitud):
        /// - "EN_PROCESO": Sin fecha de ingreso y dentro del umbral SLA
        /// - "INACTIVA": Con fecha de ingreso y cumple el SLA (dentro del umbral)
        /// - "VENCIDA": Superó el umbral SLA (con o sin fecha de ingreso)
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

            // 1) Con fechaIngreso explícita -> puede ser INACTIVA (cumple) o VENCIDA (no cumple)
            if (fechaIngreso.HasValue)
            {
                var dias = (int)Math.Floor((fechaIngreso.Value - fechaSolicitud).TotalDays);
                numDiasSla = dias;

                if (dias <= configSla.DiasUmbral)
                {
                    // ✅ CUMPLE el SLA -> INACTIVA
                    estadoCumplimiento = $"CUMPLE_{codigo}";
                    estadoSolicitud = "INACTIVA";
                    resumenSla =
                        $"Solicitud atendida dentro del SLA ({dias} de {configSla.DiasUmbral} días).";
                }
                else
                {
                    // ❌ NO CUMPLE el SLA -> VENCIDA
                    estadoCumplimiento = $"NO_CUMPLE_{codigo}";
                    estadoSolicitud = "VENCIDA";
                    resumenSla =
                        $"Solicitud atendida fuera del SLA ({dias} de {configSla.DiasUmbral} días).";
                }
            }
            // 2) Sin fechaIngreso -> puede ser EN_PROCESO (pendiente) o VENCIDA (autocierre)
            else
            {
                var diasTranscurridos = (int)Math.Floor((hoyPeru - fechaSolicitud).TotalDays);
                numDiasSla = diasTranscurridos;

                // EN_PROCESO: todavía dentro del umbral, esperando fecha de ingreso
                if (diasTranscurridos <= configSla.DiasUmbral)
                {
                    if (numDiasSla < 0) numDiasSla = 0; // por seguridad

                    estadoCumplimiento = $"EN_PROCESO_{codigo}";
                    estadoSolicitud = "EN_PROCESO";
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
                EstadoSolicitud = estadoSolicitud,            // EN_PROCESO / INACTIVA / VENCIDA
                EstadoCumplimientoSla = estadoCumplimiento,   // EN_PROCESO_*, CUMPLE_*, NO_CUMPLE_*
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
    }
}
