-- =====================================================
-- SCRIPT SQL PARA TABLAS DE EMAIL AUTOMATION
-- Sistema de Gestión de Alertas SLA TATA
-- Fecha: 25/01/2025
-- =====================================================

-- Tabla: email_config
-- Configuración de envíos automáticos y resúmenes diarios
CREATE TABLE [dbo].[email_config](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[destinatario_resumen] [nvarchar](190) NOT NULL,
	[envio_inmediato] [bit] NOT NULL DEFAULT (1),
	[resumen_diario] [bit] NOT NULL DEFAULT (0),
	[hora_resumen] [time](7) NOT NULL,
	[creado_en] [datetime2](7) DEFAULT (sysutcdatetime()),
	[actualizado_en] [datetime2](7) NULL,
	CONSTRAINT [PK_email_config] PRIMARY KEY CLUSTERED ([id] ASC)
);
GO

-- Tabla: email_log
-- Auditoría de envíos de correos electrónicos
CREATE TABLE [dbo].[email_log](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[fecha] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
	[tipo] [nvarchar](20) NOT NULL,
	[destinatarios] [nvarchar](2000) NOT NULL,
	[estado] [nvarchar](20) NOT NULL,
	[error_detalle] [nvarchar](max) NULL,
	CONSTRAINT [PK_email_log] PRIMARY KEY CLUSTERED ([id] ASC)
);
GO

-- Índices para email_log
CREATE NONCLUSTERED INDEX [IX_email_log_fecha] ON [dbo].[email_log]
(
	[fecha] DESC
);
GO

CREATE NONCLUSTERED INDEX [IX_email_log_tipo] ON [dbo].[email_log]
(
	[tipo] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_email_log_estado] ON [dbo].[email_log]
(
	[estado] ASC
);
GO

-- Seed inicial de email_config
INSERT INTO [dbo].[email_config] 
	([destinatario_resumen], [envio_inmediato], [resumen_diario], [hora_resumen], [creado_en])
VALUES 
	('admin@tata.com', 1, 0, '08:00:00', SYSUTCDATETIME());
GO

-- Verificar creación
SELECT * FROM email_config;
SELECT * FROM email_log;
GO

-- =====================================================
-- FIN DEL SCRIPT
-- =====================================================
