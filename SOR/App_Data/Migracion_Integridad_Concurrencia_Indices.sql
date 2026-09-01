-- ============================================================================
-- SCRIPT DE MIGRACIÓN: INTEGRIDAD DE DATOS, CONCURRENCIA OPTIMISTA, ÍNDICES Y AUDITORÍA
-- Sistema de Gestión Interna OCC Rep Dom (SOR)
-- ============================================================================

USE DB_SOR;
GO

-- 1. COLUMNAS PARA CONTROL DE CONCURRENCIA OPTIMISTA Y AUDITORÍA EN TABLAS CLAVE
IF COL_LENGTH('dbo.Iglesias', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Iglesias ADD RowVersion ROWVERSION;
END
GO
IF COL_LENGTH('dbo.Iglesias', 'FechaModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Iglesias ADD FechaModificacion DATETIME2 NULL;
END
GO
IF COL_LENGTH('dbo.Iglesias', 'UsuarioModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Iglesias ADD UsuarioModificacion INT NULL;
END
GO

IF COL_LENGTH('dbo.Equipos', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Equipos ADD RowVersion ROWVERSION;
END
GO
IF COL_LENGTH('dbo.Equipos', 'FechaModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Equipos ADD FechaModificacion DATETIME2 NULL;
END
GO
IF COL_LENGTH('dbo.Equipos', 'UsuarioModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Equipos ADD UsuarioModificacion INT NULL;
END
GO

IF COL_LENGTH('dbo.Eventos', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Eventos ADD RowVersion ROWVERSION;
END
GO
IF COL_LENGTH('dbo.Eventos', 'FechaModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Eventos ADD FechaModificacion DATETIME2 NULL;
END
GO
IF COL_LENGTH('dbo.Eventos', 'UsuarioModificacion') IS NULL
BEGIN
    ALTER TABLE dbo.Eventos ADD UsuarioModificacion INT NULL;
END
GO

-- 2. DEDUPLICACIÓN PREVENTIVA ANTES DE RESTRICCIONES ÚNICAS
WITH CTE_Part AS (
    SELECT IdParticipacion, ROW_NUMBER() OVER(PARTITION BY IdIglesia, IdTemporada ORDER BY IdParticipacion DESC) AS rn
    FROM dbo.ParticipacionesIglesia
)
DELETE FROM CTE_Part WHERE rn > 1;
GO

WITH CTE_AsistM AS (
    SELECT IdAsistencia, ROW_NUMBER() OVER(PARTITION BY IdEvento, IdMaestro ORDER BY IdAsistencia DESC) AS rn
    FROM dbo.AsistenciaMaestro
)
DELETE FROM CTE_AsistM WHERE rn > 1;
GO

-- 3. RESTRICCIONES E ÍNDICES ÚNICOS (ANTI-CARRERAS CONCURRENTES)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Participaciones_Iglesia_Temporada')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Participaciones_Iglesia_Temporada 
    ON dbo.ParticipacionesIglesia (IdIglesia, IdTemporada);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_AsistenciaMaestro_Evento_Maestro')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AsistenciaMaestro_Evento_Maestro 
    ON dbo.AsistenciaMaestro (IdEvento, IdMaestro);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EventosAsistentes_Evento_Participacion_Doc')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_EventosAsistentes_Evento_Participacion_Doc
    ON dbo.EventosAsistentes (IdEvento, IdParticipacion, Identificacion)
    WHERE Identificacion IS NOT NULL AND Identificacion <> '';
END
GO

-- 4. ÍNDICES DE RENDIMIENTO (ELIMINACIÓN DE TABLE SCANS)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Iglesias_IdEquipo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Iglesias_IdEquipo ON dbo.Iglesias (IdEquipo)
    INCLUDE (NombreIglesia, RNC_Cedula, Telefono, Denominacion, TipoOrganizacion);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Iglesias_RNC_Cedula')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Iglesias_RNC_Cedula ON dbo.Iglesias (RNC_Cedula)
    WHERE RNC_Cedula IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Participaciones_IdTemporada')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Participaciones_IdTemporada ON dbo.ParticipacionesIglesia (IdTemporada)
    INCLUDE (IdIglesia, EstadoEvaluacion, EstatusEvaluacionReporte, EtapaActual);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PersonasIglesia_IdIglesia_Tipo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_PersonasIglesia_IdIglesia_Tipo ON dbo.PersonasIglesia (IdIglesia, TipoPersona)
    INCLUDE (Nombres, Apellidos, DocumentoIdentidad, Celular, Correo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Maestros_IdIglesia_Activo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Maestros_IdIglesia_Activo ON dbo.Maestros (IdIglesia, Activo)
    INCLUDE (Nombres, Apellidos, DocumentoIdentidad, Celular, Correo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_IdTemporada_Fecha')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Eventos_IdTemporada_Fecha ON dbo.Eventos (IdTemporada, Fecha DESC)
    INCLUDE (NombreEvento, TipoEvento, Lugar, Responsable, CantidadAsistentes);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventosAsistentes_IdEvento')
BEGIN
    CREATE NONCLUSTERED INDEX IX_EventosAsistentes_IdEvento ON dbo.EventosAsistentes (IdEvento, IdParticipacion)
    INCLUDE (NombreCompleto, Identificacion, Telefono, Correo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsignacionesRecursos_IdPart')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AsignacionesRecursos_IdPart ON dbo.AsignacionesRecursos (IdParticipacion);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comentarios_IdIglesia')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Comentarios_IdIglesia ON dbo.ComentariosObservaciones (IdIglesia, FechaCreacion DESC);
END
GO

-- 5. TABLA CENTRAL DE AUDITORÍA GENERAL
IF OBJECT_ID('dbo.AuditoriaGeneral', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaGeneral (
        IdAuditoria BIGINT IDENTITY(1,1) PRIMARY KEY,
        FechaHora DATETIME2 DEFAULT GETDATE(),
        IdUsuario INT NULL,
        CorreoUsuario VARCHAR(150) NULL,
        Accion VARCHAR(50) NOT NULL,
        Modulo VARCHAR(50) NOT NULL,
        IdRegistroAfectado VARCHAR(50) NULL,
        Detalles NVARCHAR(MAX) NULL,
        DireccionIP VARCHAR(50) NULL
    );

    CREATE NONCLUSTERED INDEX IX_AuditoriaGeneral_Modulo_Fecha ON dbo.AuditoriaGeneral(Modulo, FechaHora DESC);
END
GO
