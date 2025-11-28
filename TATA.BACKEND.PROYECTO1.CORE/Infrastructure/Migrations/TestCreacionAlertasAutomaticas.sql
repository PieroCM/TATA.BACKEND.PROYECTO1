-- =============================================
-- Script de Prueba: Creación Automática de Alertas
-- Descripción: Verifica que las solicitudes crean automáticamente alertas
-- =============================================

USE Proyecto1SLA_DB;
GO

-- =============================================
-- SECCIÓN 1: LIMPIAR DATOS DE PRUEBA ANTERIORES
-- =============================================

PRINT '========================================';
PRINT '1. Limpiando datos de prueba anteriores...';
PRINT '========================================';

-- Eliminar alertas de prueba
DELETE FROM alerta WHERE mensaje LIKE '%PRUEBA AUTOMÁTICA%';

-- Eliminar solicitudes de prueba
DELETE FROM solicitud WHERE resumen_sla LIKE '%PRUEBA AUTOMÁTICA%';

PRINT 'Datos de prueba limpiados.';
PRINT '';

-- =============================================
-- SECCIÓN 2: VERIFICAR ESTADO ACTUAL
-- =============================================

PRINT '========================================';
PRINT '2. Estado actual de la base de datos';
PRINT '========================================';

SELECT 
    COUNT(*) AS TotalSolicitudes
FROM solicitud
WHERE estado_solicitud = 'ACTIVO';

SELECT 
    COUNT(*) AS TotalAlertas
FROM alerta
WHERE estado != 'ELIMINADA';

PRINT '';

-- =============================================
-- SECCIÓN 3: CREAR DATOS DE PRUEBA (MANUAL)
-- =============================================

PRINT '========================================';
PRINT '3. Creando solicitud de prueba manual...';
PRINT '========================================';

-- Obtener IDs necesarios
DECLARE @IdPersonal INT = (SELECT TOP 1 id_personal FROM personal WHERE estado = 'ACTIVO' ORDER BY id_personal);
DECLARE @IdSla INT = (SELECT TOP 1 id_sla FROM config_sla WHERE es_activo = 1 ORDER BY dias_umbral DESC);
DECLARE @IdRol INT = (SELECT TOP 1 id_rol_registro FROM rol_registro WHERE es_activo = 1 ORDER BY id_rol_registro);
DECLARE @IdUsuario INT = (SELECT TOP 1 id_usuario FROM usuario WHERE estado = 'ACTIVO' ORDER BY id_usuario);

-- Obtener DiasUmbral del SLA
DECLARE @DiasUmbral INT = (SELECT dias_umbral FROM config_sla WHERE id_sla = @IdSla);

-- Calcular fechas para tener pocos días restantes (alerta crítica)
DECLARE @FechaSolicitud DATE = DATEADD(DAY, -(@DiasUmbral - 2), GETDATE());
DECLARE @FechaIngreso DATE = GETDATE();

PRINT 'IDs seleccionados:';
PRINT '  - Personal: ' + CAST(@IdPersonal AS VARCHAR);
PRINT '  - SLA: ' + CAST(@IdSla AS VARCHAR) + ' (Umbral: ' + CAST(@DiasUmbral AS VARCHAR) + ' días)';
PRINT '  - Rol: ' + CAST(@IdRol AS VARCHAR);
PRINT '  - Usuario: ' + CAST(@IdUsuario AS VARCHAR);
PRINT '  - FechaSolicitud: ' + CAST(@FechaSolicitud AS VARCHAR);
PRINT '  - FechaIngreso: ' + CAST(@FechaIngreso AS VARCHAR);
PRINT '';

-- Verificar que tenemos todos los IDs necesarios
IF @IdPersonal IS NULL OR @IdSla IS NULL OR @IdRol IS NULL OR @IdUsuario IS NULL
BEGIN
    PRINT 'ERROR: No se encontraron todos los registros necesarios.';
    PRINT 'Asegúrate de tener datos en las tablas: personal, config_sla, rol_registro, usuario';
    RETURN;
END

-- Insertar solicitud de prueba (sin alerta, simulando el comportamiento anterior)
INSERT INTO solicitud (
    id_personal,
    id_sla,
    id_rol_registro,
    creado_por,
    fecha_solicitud,
    fecha_ingreso,
    num_dias_sla,
    resumen_sla,
    origen_dato,
    estado_solicitud,
    estado_cumplimiento_sla,
    creado_en
)
VALUES (
    @IdPersonal,
    @IdSla,
    @IdRol,
    @IdUsuario,
    @FechaSolicitud,
    @FechaIngreso,
    DATEDIFF(DAY, @FechaSolicitud, @FechaIngreso),
    'PRUEBA AUTOMÁTICA - Solicitud sin alerta inicial',
    'TEST',
    'ACTIVO',
    'CUMPLE_TEST',
    GETUTCDATE()
);

DECLARE @IdSolicitudPrueba INT = SCOPE_IDENTITY();

PRINT 'Solicitud de prueba creada: ID = ' + CAST(@IdSolicitudPrueba AS VARCHAR);
PRINT '';

-- =============================================
-- SECCIÓN 4: VERIFICAR QUE NO TIENE ALERTA
-- =============================================

PRINT '========================================';
PRINT '4. Verificando estado ANTES del cambio';
PRINT '========================================';

SELECT 
    s.id_solicitud,
    s.resumen_sla,
    COUNT(a.id_alerta) AS CantidadAlertas
FROM solicitud s
LEFT JOIN alerta a ON s.id_solicitud = a.id_solicitud
WHERE s.id_solicitud = @IdSolicitudPrueba
GROUP BY s.id_solicitud, s.resumen_sla;

PRINT 'Resultado esperado: CantidadAlertas = 0 (sin alerta inicial)';
PRINT '';

-- =============================================
-- SECCIÓN 5: SIMULAR CREACIÓN CON EL NUEVO CÓDIGO
-- =============================================

PRINT '========================================';
PRINT '5. Simulando comportamiento del nuevo código...';
PRINT '========================================';

-- Calcular días restantes
DECLARE @DiasTranscurridos INT = DATEDIFF(DAY, @FechaSolicitud, @FechaIngreso);
DECLARE @DiasRestantes INT = @DiasUmbral - @DiasTranscurridos;

-- Determinar nivel de alerta
DECLARE @NivelAlerta VARCHAR(20);
IF @DiasRestantes < 0
    SET @NivelAlerta = 'CRITICO';
ELSE IF @DiasRestantes <= 2
    SET @NivelAlerta = 'CRITICO';
ELSE IF @DiasRestantes <= 5
    SET @NivelAlerta = 'ALTO';
ELSE
    SET @NivelAlerta = 'MEDIO';

-- Generar mensaje
DECLARE @MensajeAlerta NVARCHAR(500);
IF @DiasRestantes < 0
    SET @MensajeAlerta = '?? URGENTE: Solicitud #' + CAST(@IdSolicitudPrueba AS VARCHAR) + ' VENCIDA. Se excedió el SLA por ' + CAST(ABS(@DiasRestantes) AS VARCHAR) + ' día(s).';
ELSE IF @DiasRestantes = 0
    SET @MensajeAlerta = '?? ATENCIÓN: Solicitud #' + CAST(@IdSolicitudPrueba AS VARCHAR) + ' vence HOY. Requiere acción inmediata.';
ELSE IF @DiasRestantes <= 2
    SET @MensajeAlerta = 'Solicitud #' + CAST(@IdSolicitudPrueba AS VARCHAR) + ' está cerca de vencer el SLA. Quedan solo ' + CAST(@DiasRestantes AS VARCHAR) + ' día(s).';
ELSE
    SET @MensajeAlerta = 'PRUEBA AUTOMÁTICA - Solicitud #' + CAST(@IdSolicitudPrueba AS VARCHAR) + ' creada. Vencimiento en ' + CAST(@DiasRestantes AS VARCHAR) + ' día(s) (SLA: ' + CAST(@DiasUmbral AS VARCHAR) + ' días).';

PRINT 'Cálculos realizados:';
PRINT '  - Días transcurridos: ' + CAST(@DiasTranscurridos AS VARCHAR);
PRINT '  - Días restantes: ' + CAST(@DiasRestantes AS VARCHAR);
PRINT '  - Nivel calculado: ' + @NivelAlerta;
PRINT '  - Mensaje: ' + @MensajeAlerta;
PRINT '';

-- Crear la alerta (simulando el comportamiento del nuevo código)
INSERT INTO alerta (
    id_solicitud,
    tipo_alerta,
    nivel,
    mensaje,
    estado,
    enviado_email,
    fecha_creacion
)
VALUES (
    @IdSolicitudPrueba,
    'NUEVA_ASIGNACION',
    @NivelAlerta,
    @MensajeAlerta,
    'NUEVA',
    0,
    GETUTCDATE()
);

PRINT 'Alerta creada automáticamente para la solicitud #' + CAST(@IdSolicitudPrueba AS VARCHAR);
PRINT '';

-- =============================================
-- SECCIÓN 6: VERIFICAR RESULTADO FINAL
-- =============================================

PRINT '========================================';
PRINT '6. Verificando estado DESPUÉS del cambio';
PRINT '========================================';

SELECT 
    s.id_solicitud AS 'ID Solicitud',
    s.resumen_sla AS 'Resumen',
    s.num_dias_sla AS 'Días SLA',
    a.id_alerta AS 'ID Alerta',
    a.tipo_alerta AS 'Tipo',
    a.nivel AS 'Nivel',
    a.mensaje AS 'Mensaje',
    a.estado AS 'Estado'
FROM solicitud s
INNER JOIN alerta a ON s.id_solicitud = a.id_solicitud
WHERE s.id_solicitud = @IdSolicitudPrueba;

PRINT '';
PRINT 'Resultado esperado: 1 registro con la alerta creada automáticamente';
PRINT '';

-- =============================================
-- SECCIÓN 7: ESTADÍSTICAS FINALES
-- =============================================

PRINT '========================================';
PRINT '7. Estadísticas Finales';
PRINT '========================================';

-- Solicitudes con y sin alertas
SELECT 
    CASE 
        WHEN a.id_alerta IS NULL THEN 'SIN ALERTA'
        ELSE 'CON ALERTA'
    END AS Estado,
    COUNT(*) AS Cantidad
FROM solicitud s
LEFT JOIN alerta a ON s.id_solicitud = a.id_solicitud
WHERE s.estado_solicitud = 'ACTIVO'
GROUP BY 
    CASE 
        WHEN a.id_alerta IS NULL THEN 'SIN ALERTA'
        ELSE 'CON ALERTA'
    END;

-- Distribución de niveles de alerta
PRINT '';
PRINT 'Distribución de alertas por nivel:';
SELECT 
    nivel AS 'Nivel',
    COUNT(*) AS 'Cantidad'
FROM alerta
WHERE estado != 'ELIMINADA'
GROUP BY nivel
ORDER BY 
    CASE nivel
        WHEN 'CRITICO' THEN 1
        WHEN 'ALTO' THEN 2
        WHEN 'MEDIO' THEN 3
        WHEN 'BAJO' THEN 4
    END;

PRINT '';

-- =============================================
-- SECCIÓN 8: CONSULTA DASHBOARD SIMULADA
-- =============================================

PRINT '========================================';
PRINT '8. Consulta Dashboard (simulada)';
PRINT '========================================';

SELECT 
    a.id_alerta AS 'ID Alerta',
    a.nivel AS 'Nivel',
    a.estado AS 'Estado',
    a.mensaje AS 'Mensaje',
    a.fecha_creacion AS 'Fecha Registro',
    s.id_solicitud AS 'ID Solicitud',
    s.fecha_solicitud AS 'Fecha Solicitud',
    s.resumen_sla AS 'Descripción',
    c.codigo_sla AS 'Código SLA',
    c.dias_umbral AS 'Días Umbral',
    r.nombre_rol AS 'Rol Responsable'
FROM alerta a
INNER JOIN solicitud s ON a.id_solicitud = s.id_solicitud
INNER JOIN config_sla c ON s.id_sla = c.id_sla
INNER JOIN rol_registro r ON s.id_rol_registro = r.id_rol_registro
WHERE a.estado != 'ELIMINADA'
ORDER BY a.fecha_creacion DESC;

PRINT '';

-- =============================================
-- SECCIÓN 9: LIMPIEZA OPCIONAL
-- =============================================

PRINT '========================================';
PRINT '9. Limpieza (OPCIONAL - comentar si no deseas eliminar)';
PRINT '========================================';

-- Descomentar las siguientes líneas si deseas limpiar los datos de prueba
/*
DELETE FROM alerta WHERE id_solicitud = @IdSolicitudPrueba;
DELETE FROM solicitud WHERE id_solicitud = @IdSolicitudPrueba;
PRINT 'Datos de prueba eliminados.';
*/

PRINT 'Script de prueba completado.';
PRINT '';
PRINT '========================================';
PRINT 'RESULTADO ESPERADO:';
PRINT '- Solicitud creada: ID = ' + CAST(@IdSolicitudPrueba AS VARCHAR);
PRINT '- Alerta creada automáticamente: Nivel = ' + @NivelAlerta;
PRINT '- Estado final: SOLICITUD + ALERTA vinculadas';
PRINT '========================================';

GO
