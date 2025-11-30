-- ====================================================
-- Script de Diagnóstico Rápido
-- Resumen Diario - Sistema TATA SLA
-- ====================================================

USE Proyecto1SLA_DB;
GO

PRINT '========================================';
PRINT 'DIAGNÓSTICO: Resumen Diario No Llega';
PRINT '========================================';
PRINT '';

-- ====================================================
-- 1. VERIFICAR CONFIGURACIÓN DE EMAIL
-- ====================================================
PRINT '1. CONFIGURACIÓN DE EMAIL (email_config)';
PRINT '----------------------------------------';

IF EXISTS (SELECT 1 FROM email_config)
BEGIN
    SELECT 
        Id AS [ID],
        DestinatarioResumen AS [Destinatario],
        CASE WHEN ResumenDiario = 1 THEN 'ACTIVADO ?' ELSE 'DESACTIVADO ?' END AS [Estado Resumen],
        CASE WHEN EnvioInmediato = 1 THEN 'SI' ELSE 'NO' END AS [Envío Inmediato],
        CONVERT(VARCHAR(8), HoraResumen, 108) AS [Hora Envío],
        CreadoEn AS [Fecha Creación],
        ActualizadoEn AS [Última Actualización]
    FROM email_config;
    
    -- Validaciones
    DECLARE @resumenActivo BIT, @destinatario NVARCHAR(190);
    SELECT @resumenActivo = ResumenDiario, @destinatario = DestinatarioResumen FROM email_config WHERE Id = 1;
    
    PRINT '';
    IF @resumenActivo = 1
        PRINT '? Resumen diario ACTIVADO';
    ELSE
        PRINT '? Resumen diario DESACTIVADO - Activar con UPDATE';
        
    IF @destinatario IS NOT NULL AND @destinatario <> ''
        PRINT '? Destinatario configurado: ' + @destinatario;
    ELSE
        PRINT '? No hay destinatario configurado';
END
ELSE
BEGIN
    PRINT '? NO EXISTE CONFIGURACIÓN - Ejecuta las migraciones';
END

PRINT '';
PRINT '';

-- ====================================================
-- 2. VERIFICAR ALERTAS ACTIVAS
-- ====================================================
PRINT '2. ALERTAS CRÍTICAS/ALTAS ACTIVAS';
PRINT '----------------------------------------';

IF EXISTS (SELECT 1 FROM alerta WHERE Estado = 'ACTIVA' AND (Nivel = 'CRITICO' OR Nivel = 'ALTO'))
BEGIN
    SELECT 
        COUNT(*) AS [Total],
        Nivel,
        COUNT(CASE WHEN EnviadoEmail = 1 THEN 1 END) AS [Ya Enviados],
        COUNT(CASE WHEN EnviadoEmail = 0 THEN 1 END) AS [Pendientes]
    FROM alerta
    WHERE Estado = 'ACTIVA' 
      AND (Nivel = 'CRITICO' OR Nivel = 'ALTO')
    GROUP BY Nivel;
    
    PRINT '';
    PRINT '? Hay alertas críticas para enviar';
    
    -- Mostrar las 5 más recientes
    PRINT '';
    PRINT 'Últimas 5 alertas críticas:';
    SELECT TOP 5
        IdAlerta AS [ID],
        Nivel,
        LEFT(Mensaje, 50) + '...' AS [Mensaje],
        CONVERT(VARCHAR(19), FechaCreacion, 120) AS [Fecha],
        CASE WHEN EnviadoEmail = 1 THEN 'SI' ELSE 'NO' END AS [Enviado]
    FROM alerta
    WHERE Estado = 'ACTIVA' 
      AND (Nivel = 'CRITICO' OR Nivel = 'ALTO')
    ORDER BY FechaCreacion DESC;
END
ELSE
BEGIN
    PRINT '? NO HAY ALERTAS CRÍTICAS/ALTAS ACTIVAS';
    PRINT '  ? El resumen diario NO se enviará porque no hay datos';
    PRINT '  ? Verifica que existan solicitudes con SLA vencido o próximo a vencer';
END

PRINT '';
PRINT '';

-- ====================================================
-- 3. VERIFICAR LOGS DE ENVÍO
-- ====================================================
PRINT '3. LOGS DE ENVÍOS DE RESUMEN';
PRINT '----------------------------------------';

IF EXISTS (SELECT 1 FROM email_log WHERE Tipo = 'RESUMEN')
BEGIN
    -- Estadísticas generales
    SELECT 
        COUNT(*) AS [Total Envíos],
        SUM(CASE WHEN Estado = 'OK' THEN 1 ELSE 0 END) AS [Exitosos],
        SUM(CASE WHEN Estado = 'ERROR' THEN 1 ELSE 0 END) AS [Fallidos],
        MAX(Fecha) AS [Último Envío]
    FROM email_log
    WHERE Tipo = 'RESUMEN';
    
    PRINT '';
    PRINT 'Últimos 10 intentos de envío de resumen:';
    PRINT '';
    
    SELECT TOP 10
        Id,
        CONVERT(VARCHAR(19), Fecha, 120) AS [Fecha],
        Destinatarios AS [Destinatario],
        Estado,
        CASE 
            WHEN ErrorDetalle IS NULL THEN 'Sin errores'
            WHEN LEN(ErrorDetalle) > 80 THEN LEFT(ErrorDetalle, 77) + '...'
            ELSE ErrorDetalle
        END AS [Detalle]
    FROM email_log
    WHERE Tipo = 'RESUMEN'
    ORDER BY Fecha DESC;
    
    -- Verificar último estado
    DECLARE @ultimoEstado NVARCHAR(20);
    SELECT TOP 1 @ultimoEstado = Estado FROM email_log WHERE Tipo = 'RESUMEN' ORDER BY Fecha DESC;
    
    PRINT '';
    IF @ultimoEstado = 'OK'
        PRINT '? Último envío: EXITOSO';
    ELSE
        PRINT '? Último envío: FALLÓ - Revisar detalle arriba';
END
ELSE
BEGIN
    PRINT '? NO HAY LOGS DE RESUMEN DIARIO';
    PRINT '  ? Esto es normal si nunca se ha ejecutado';
    PRINT '  ? Ejecuta: POST /api/email/send-summary para probar';
END

PRINT '';
PRINT '';

-- ====================================================
-- 4. VERIFICAR SOLICITUDES CRÍTICAS
-- ====================================================
PRINT '4. SOLICITUDES CON SLA CRÍTICO';
PRINT '----------------------------------------';

IF EXISTS (SELECT 1 FROM solicitud WHERE EstadoSolicitud <> 'CERRADO' AND EstadoSolicitud <> 'ELIMINADO')
BEGIN
    DECLARE @hoy DATE = GETUTCDATE();
    
    SELECT 
        COUNT(*) AS [Total Solicitudes Abiertas],
        SUM(CASE WHEN EstadoCumplimientoSla = 'NO_CUMPLE_SLA' THEN 1 ELSE 0 END) AS [Vencidas],
        SUM(CASE WHEN DATEDIFF(DAY, FechaSolicitud, @hoy) > (SELECT TOP 1 DiasUmbral FROM config_sla WHERE IdSla = solicitud.IdSla) - 2 THEN 1 ELSE 0 END) AS [Críticas (< 2 días)]
    FROM solicitud
    WHERE EstadoSolicitud NOT IN ('CERRADO', 'ELIMINADO');
    
    -- Mostrar top 5 más críticas
    PRINT '';
    PRINT 'Top 5 solicitudes más críticas:';
    SELECT TOP 5
        s.IdSolicitud AS [ID],
        p.Nombres + ' ' + p.Apellidos AS [Responsable],
        cs.CodigoSla AS [SLA],
        cs.DiasUmbral AS [Días Límite],
        DATEDIFF(DAY, s.FechaSolicitud, @hoy) AS [Días Transcurridos],
        cs.DiasUmbral - DATEDIFF(DAY, s.FechaSolicitud, @hoy) AS [Días Restantes],
        s.EstadoCumplimientoSla AS [Estado SLA]
    FROM solicitud s
    LEFT JOIN personal p ON s.IdPersonal = p.IdPersonal
    LEFT JOIN config_sla cs ON s.IdSla = cs.IdSla
    WHERE s.EstadoSolicitud NOT IN ('CERRADO', 'ELIMINADO')
    ORDER BY (cs.DiasUmbral - DATEDIFF(DAY, s.FechaSolicitud, @hoy)) ASC;
END
ELSE
BEGIN
    PRINT '? NO HAY SOLICITUDES ABIERTAS';
    PRINT '  ? No se generarán alertas automáticas';
END

PRINT '';
PRINT '';

-- ====================================================
-- 5. RESUMEN Y RECOMENDACIONES
-- ====================================================
PRINT '========================================';
PRINT 'RESUMEN Y RECOMENDACIONES';
PRINT '========================================';
PRINT '';

-- Verificar cada requisito
DECLARE @todoOK BIT = 1;

-- Check 1: EmailConfig
IF NOT EXISTS (SELECT 1 FROM email_config WHERE Id = 1 AND ResumenDiario = 1)
BEGIN
    PRINT '? PROBLEMA: Resumen diario desactivado o no configurado';
    PRINT '  SOLUCIÓN: UPDATE email_config SET ResumenDiario = 1 WHERE Id = 1;';
    PRINT '';
    SET @todoOK = 0;
END

-- Check 2: Destinatario
IF NOT EXISTS (SELECT 1 FROM email_config WHERE Id = 1 AND DestinatarioResumen IS NOT NULL AND DestinatarioResumen <> '')
BEGIN
    PRINT '? PROBLEMA: No hay destinatario configurado';
    PRINT '  SOLUCIÓN: UPDATE email_config SET DestinatarioResumen = ''tu@email.com'' WHERE Id = 1;';
    PRINT '';
    SET @todoOK = 0;
END

-- Check 3: Alertas
IF NOT EXISTS (SELECT 1 FROM alerta WHERE Estado = 'ACTIVA' AND (Nivel = 'CRITICO' OR Nivel = 'ALTO'))
BEGIN
    PRINT '? ADVERTENCIA: No hay alertas críticas/altas';
    PRINT '  RESULTADO: El resumen diario no se enviará (no hay datos)';
    PRINT '  ACCIÓN: Esto es normal si no hay SLAs vencidos. Espera a que se generen alertas.';
    PRINT '';
    SET @todoOK = 0;
END

-- Check 4: Logs con errores recientes
IF EXISTS (SELECT 1 FROM email_log WHERE Tipo = 'RESUMEN' AND Estado = 'ERROR' AND Fecha > DATEADD(DAY, -1, GETUTCDATE()))
BEGIN
    PRINT '? ADVERTENCIA: Hay errores recientes en envío de resumen';
    PRINT '  ACCIÓN: Revisar ErrorDetalle en la tabla arriba';
    PRINT '';
    SET @todoOK = 0;
END

IF @todoOK = 1
BEGIN
    PRINT '??? CONFIGURACIÓN CORRECTA ???';
    PRINT '';
    PRINT 'Todo parece estar bien configurado.';
    PRINT 'Si el correo no llega, revisa:';
    PRINT '  1. Carpeta de SPAM/Correo no deseado';
    PRINT '  2. Carpeta Promociones (Gmail)';
    PRINT '  3. Busca: [RESUMEN DIARIO SLA]';
    PRINT '';
    PRINT 'Para probar manualmente:';
    PRINT '  POST https://localhost:7152/api/email/send-summary';
END
ELSE
BEGIN
    PRINT '? HAY PROBLEMAS DE CONFIGURACIÓN';
    PRINT 'Revisa las soluciones indicadas arriba.';
END

PRINT '';
PRINT '========================================';
PRINT 'FIN DEL DIAGNÓSTICO';
PRINT '========================================';

-- ====================================================
-- SCRIPTS DE CORRECCIÓN RÁPIDA
-- ====================================================
PRINT '';
PRINT '';
PRINT '-- SCRIPTS DE CORRECCIÓN RÁPIDA --';
PRINT '-- (Descomenta y ejecuta solo si es necesario) --';
PRINT '';

PRINT '/*';
PRINT '-- Activar resumen diario y configurar destinatario';
PRINT 'UPDATE email_config ';
PRINT 'SET DestinatarioResumen = ''22200150@ue.edu.pe'',  -- CAMBIA POR TU EMAIL';
PRINT '    ResumenDiario = 1,';
PRINT '    HoraResumen = ''08:00:00''';
PRINT 'WHERE Id = 1;';
PRINT '';
PRINT '-- Crear alerta de prueba (necesitas un IdSolicitud válido)';
PRINT 'DECLARE @idSolicitud INT;';
PRINT 'SELECT TOP 1 @idSolicitud = IdSolicitud FROM solicitud WHERE EstadoSolicitud <> ''CERRADO'';';
PRINT '';
PRINT 'IF @idSolicitud IS NOT NULL';
PRINT 'BEGIN';
PRINT '    INSERT INTO alerta (IdSolicitud, TipoAlerta, Nivel, Mensaje, Estado, EnviadoEmail, FechaCreacion)';
PRINT '    VALUES (@idSolicitud, ''SLA_VENCIMIENTO_INMEDIATO'', ''CRITICO'', ';
PRINT '            ''?? URGENTE: Solicitud de prueba vence en 1 día'', ''ACTIVA'', 0, GETUTCDATE());';
PRINT 'END';
PRINT '';
PRINT '-- Ver alertas creadas';
PRINT 'SELECT TOP 5 * FROM alerta ORDER BY FechaCreacion DESC;';
PRINT '*/';

GO
