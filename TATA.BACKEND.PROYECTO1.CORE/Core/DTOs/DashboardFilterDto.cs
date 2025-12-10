using System.ComponentModel.DataAnnotations;

namespace TATA.BACKEND.PROYECTO1.CORE.Core.DTOs;

/// <summary>
/// DTO para filtrado dinámico del Dashboard de Alertas
/// Soporta filtros múltiples y búsqueda avanzada
/// </summary>
public class DashboardFilterDto
{
    /// <summary>
    /// Filtrar por nivel de alerta: "CRITICO", "ALTO", "MEDIO", "BAJO"
    /// </summary>
    public string? Nivel { get; set; }

    /// <summary>
    /// Filtrar por estado temporal de la solicitud
    /// Valores permitidos: "VIGENTE" | "VENCIDO"
    /// - VIGENTE: FechaVencimiento >= DateTime.Now
    /// - VENCIDO: FechaVencimiento < DateTime.Now
    /// </summary>
    [RegularExpression("^(VIGENTE|VENCIDO)$", ErrorMessage = "EstadoTiempo debe ser 'VIGENTE' o 'VENCIDO'")]
    public string? EstadoTiempo { get; set; }

    /// <summary>
    /// Filtrar por estado de lectura de la alerta
    /// - true: Solo alertas leídas (FechaLectura IS NOT NULL)
    /// - false: Solo alertas no leídas (FechaLectura IS NULL)
    /// - null: Todas las alertas
    /// </summary>
    public bool? EsLeida { get; set; }

    /// <summary>
    /// Filtrar por ID de SLA específico
    /// </summary>
    public int? IdSla { get; set; }

    /// <summary>
    /// Filtrar por ID de Rol de Registro específico
    /// </summary>
    public int? IdRol { get; set; }

    /// <summary>
    /// Búsqueda de texto libre
    /// Busca en: Mensaje de alerta, Nombre del responsable, Email
    /// </summary>
    [MaxLength(200, ErrorMessage = "La búsqueda no puede exceder 200 caracteres")]
    public string? Busqueda { get; set; }

    /// <summary>
    /// Filtrar por estado de la alerta
    /// Valores comunes: "ACTIVA", "INACTIVA", "CERRADA"
    /// </summary>
    public string? EstadoAlerta { get; set; }

    /// <summary>
    /// Ordenar resultados
    /// Valores: "FechaCreacion", "DiasRestantes", "Nivel"
    /// Por defecto: "FechaCreacion DESC"
    /// </summary>
    public string? OrdenarPor { get; set; }

    /// <summary>
    /// Dirección del orden: "ASC" o "DESC"
    /// Por defecto: "DESC"
    /// </summary>
    [RegularExpression("^(ASC|DESC)$", ErrorMessage = "DireccionOrden debe ser 'ASC' o 'DESC'")]
    public string? DireccionOrden { get; set; } = "DESC";

    /// <summary>
    /// Número de página para paginación
    /// Por defecto: 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor a 0")]
    public int? Pagina { get; set; }

    /// <summary>
    /// Cantidad de registros por página
    /// Por defecto: sin paginación (todos los registros)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "El tamaño de página debe estar entre 1 y 1000")]
    public int? TamanoPagina { get; set; }
}

/// <summary>
/// DTO de respuesta paginada para el Dashboard
/// </summary>
/// <typeparam name="T">Tipo de datos (AlertaDashboardDto)</typeparam>
public class DashboardResponseDto<T>
{
    /// <summary>
    /// Lista de datos (alertas)
    /// </summary>
    public List<T> Datos { get; set; } = new();

    /// <summary>
    /// Total de registros sin paginación
    /// </summary>
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Página actual
    /// </summary>
    public int PaginaActual { get; set; }

    /// <summary>
    /// Tamaño de página
    /// </summary>
    public int TamanoPagina { get; set; }

    /// <summary>
    /// Total de páginas
    /// </summary>
    public int TotalPaginas { get; set; }

    /// <summary>
    /// Indica si hay página anterior
    /// </summary>
    public bool TienePaginaAnterior { get; set; }

    /// <summary>
    /// Indica si hay página siguiente
    /// </summary>
    public bool TienePaginaSiguiente { get; set; }

    /// <summary>
    /// Filtros aplicados en la consulta
    /// </summary>
    public DashboardFilterDto? FiltrosAplicados { get; set; }
}
