namespace TATA.BACKEND.PROYECTO1.CORE.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de Machine Learning de predicción SLA
    /// Define las operaciones de negocio relacionadas con el módulo ML
    /// </summary>
    public interface ISlaMLService
    {
        /// <summary>
        /// Entrena el modelo ML con solicitudes históricas cerradas
        /// </summary>
        /// <param name="fechaDesde">Fecha inicial del rango de datos</param>
        /// <param name="fechaHasta">Fecha final del rango de datos</param>
        /// <returns>Resultado del entrenamiento con métricas</returns>
        Task<object> EntrenarModeloAsync(DateTime fechaDesde, DateTime fechaHasta);

        /// <summary>
        /// Obtiene predicciones de incumplimiento para solicitudes activas
        /// </summary>
        /// <returns>Lista de predicciones enriquecidas</returns>
        Task<object> ObtenerPrediccionesActualesAsync();

        /// <summary>
        /// Obtiene predicciones críticas (alto riesgo de incumplimiento)
        /// </summary>
        /// <param name="limite">Número máximo de predicciones a retornar</param>
        /// <returns>Lista de predicciones críticas</returns>
        Task<object> ObtenerPrediccionesCriticasAsync(int limite = 10);

        /// <summary>
        /// Obtiene el resumen de predicciones para dashboard
        /// </summary>
        /// <returns>Resumen con totales por nivel de riesgo</returns>
        Task<object> ObtenerResumenPrediccionesAsync();

        /// <summary>
        /// Verifica el estado de salud del microservicio ML
        /// </summary>
        /// <returns>Estado del servicio</returns>
        Task<object?> VerificarSaludServicioAsync();
    }
}
