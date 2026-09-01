-- ============================================================================
-- ESTRATEGIA DE RESPALDO Y RECUPERACIÓN ANTE DESASTRES (RPO & RTO)
-- Base de Datos: DB_SOR
-- ============================================================================
-- MÉTRICAS OPERATIVAS:
-- RPO (Recovery Point Objective): Máximo 1 hora de tolerancia a pérdida de datos.
-- RTO (Recovery Time Objective): Máximo 2 horas para restauración completa del servicio.
-- FRECUENCIA RECOMENDADA:
-- 1. Full Backup (Completo): Diario a las 01:00 AM.
-- 2. Differential Backup (Diferencial): Cada 6 horas durante la jornada activa (07:00, 13:00, 19:00).
-- 3. Log Backup (Transaccional, si modelo es FULL): Cada 1 hora.
-- ============================================================================

USE DB_SOR;
GO

-- 1. PROCEDIMIENTO ALMACENADO PARA RESPALDO COMPLETO (FULL BACKUP)
IF OBJECT_ID('dbo.sp_SOR_GenerarBackupCompleto', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SOR_GenerarBackupCompleto;
GO

CREATE PROCEDURE dbo.sp_SOR_GenerarBackupCompleto
    @RutaDirectorio NVARCHAR(500) = 'C:\Backups_SOR\'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NombreArchivo NVARCHAR(600);
    DECLARE @FechaStr NVARCHAR(30);
    
    SET @FechaStr = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(20), GETDATE(), 120), '-', '_'), ':', ''), ' ', '_');
    SET @NombreArchivo = @RutaDirectorio + 'DB_SOR_FULL_' + @FechaStr + '.bak';

    PRINT 'Iniciando respaldo completo de DB_SOR en: ' + @NombreArchivo;

    BACKUP DATABASE DB_SOR
    TO DISK = @NombreArchivo
    WITH 
        FORMAT,
        INIT,
        NAME = 'DB_SOR-Full Database Backup',
        SKIP,
        NOREWIND,
        NOUNLOAD,
        COMPRESSION,
        CHECKSUM,
        STATS = 10;

    -- Verificación de integridad del archivo de respaldo
    RESTORE VERIFYONLY 
    FROM DISK = @NombreArchivo 
    WITH CHECKSUM;

    IF @@ERROR = 0
    BEGIN
        PRINT 'Respaldo completo verificado exitosamente.';
        -- Registrar en bitácora de auditoría
        IF OBJECT_ID('dbo.AuditoriaGeneral', 'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.AuditoriaGeneral (Accion, Modulo, Detalles)
            VALUES ('BACKUP_FULL', 'SISTEMA', 'Respaldo completo exitoso: ' + @NombreArchivo);
        END
    END
    ELSE
    BEGIN
        RAISERROR('Error durante la verificación del archivo de respaldo.', 16, 1);
    END
END;
GO

-- 2. PROCEDIMIENTO ALMACENADO PARA RESPALDO DIFERENCIAL
IF OBJECT_ID('dbo.sp_SOR_GenerarBackupDiferencial', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SOR_GenerarBackupDiferencial;
GO

CREATE PROCEDURE dbo.sp_SOR_GenerarBackupDiferencial
    @RutaDirectorio NVARCHAR(500) = 'C:\Backups_SOR\'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NombreArchivo NVARCHAR(600);
    DECLARE @FechaStr NVARCHAR(30);
    
    SET @FechaStr = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(20), GETDATE(), 120), '-', '_'), ':', ''), ' ', '_');
    SET @NombreArchivo = @RutaDirectorio + 'DB_SOR_DIFF_' + @FechaStr + '.bak';

    PRINT 'Iniciando respaldo diferencial de DB_SOR en: ' + @NombreArchivo;

    BACKUP DATABASE DB_SOR
    TO DISK = @NombreArchivo
    WITH 
        DIFFERENTIAL,
        FORMAT,
        INIT,
        NAME = 'DB_SOR-Differential Database Backup',
        SKIP,
        NOREWIND,
        NOUNLOAD,
        COMPRESSION,
        CHECKSUM,
        STATS = 10;

    RESTORE VERIFYONLY 
    FROM DISK = @NombreArchivo 
    WITH CHECKSUM;

    IF @@ERROR = 0
    BEGIN
        PRINT 'Respaldo diferencial verificado exitosamente.';
        IF OBJECT_ID('dbo.AuditoriaGeneral', 'U') IS NOT NULL
        BEGIN
            INSERT INTO dbo.AuditoriaGeneral (Accion, Modulo, Detalles)
            VALUES ('BACKUP_DIFF', 'SISTEMA', 'Respaldo diferencial exitoso: ' + @NombreArchivo);
        END
    END
    ELSE
    BEGIN
        RAISERROR('Error durante la verificación del respaldo diferencial.', 16, 1);
    END
END;
GO

-- 3. GUÍA RÁPIDA DE RESTAURACIÓN ANTE FALLAS CATASTRÓFICAS
/*
-- Paso 1: Poner la base de datos en modo SINGLE_USER para cerrar conexiones activas
ALTER DATABASE DB_SOR SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Paso 2: Restaurar el último Full Backup con NORECOVERY
RESTORE DATABASE DB_SOR
FROM DISK = 'C:\Backups_SOR\DB_SOR_FULL_YYYY_MM_DD_HHMMSS.bak'
WITH REPLACE, NORECOVERY;

-- Paso 3: Restaurar el último Differential Backup con RECOVERY
RESTORE DATABASE DB_SOR
FROM DISK = 'C:\Backups_SOR\DB_SOR_DIFF_YYYY_MM_DD_HHMMSS.bak'
WITH RECOVERY;

-- Paso 4: Devolver la base de datos a MULTI_USER
ALTER DATABASE DB_SOR SET MULTI_USER;
*/
