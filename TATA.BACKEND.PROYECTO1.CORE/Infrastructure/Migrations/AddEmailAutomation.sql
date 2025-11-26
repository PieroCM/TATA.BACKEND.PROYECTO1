-- =============================================
-- Migration: Add EmailConfig and EmailLog tables
-- Description: Adds tables for email automation configuration and logging
-- Date: 2024
-- =============================================

-- Table: email_config
-- Stores global email configuration (only 1 record expected)
CREATE TABLE email_config (
    id INT IDENTITY(1,1) PRIMARY KEY,
    envio_inmediato BIT NOT NULL DEFAULT 0,
    resumen_diario BIT NOT NULL DEFAULT 0,
    hora_resumen TIME NOT NULL DEFAULT '08:00:00',
    email_destinatario_prueba NVARCHAR(190) NULL,
    creado_en DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    actualizado_en DATETIME2 NULL
);

-- Table: email_log
-- Stores history of email sendings (automatic, manual, resume)
CREATE TABLE email_log (
    id INT IDENTITY(1,1) PRIMARY KEY,
    fecha_ejecucion DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    tipo NVARCHAR(20) NOT NULL, -- AUTOMATICO, MANUAL, RESUMEN
    cantidad_enviados INT NOT NULL DEFAULT 0,
    estado NVARCHAR(20) NOT NULL, -- EXITO, FALLO, PARCIAL
    detalle_error NVARCHAR(MAX) NULL,
    ejecutado_por INT NULL
);

-- Indexes for email_log
CREATE INDEX IX_email_log_fecha ON email_log(fecha_ejecucion DESC);
CREATE INDEX IX_email_log_tipo ON email_log(tipo);

-- Insert default configuration
INSERT INTO email_config (envio_inmediato, resumen_diario, hora_resumen, email_destinatario_prueba)
VALUES (0, 0, '08:00:00', NULL);

GO
