-- =========================================================================
-- PLAN DE MANTENIMIENTO PREVENTIVO DE BASE DE DATOS — DB_SOR
-- Compatible con: Microsoft SQL Server 2019 / 2022 (Express / Standard / Enterprise)
-- =========================================================================

USE [DB_SOR];
GO

-- -------------------------------------------------------------------------
-- 1. REORGANIZACIÓN Y RECONSTRUCCIÓN INTELIGENTE DE ÍNDICES
-- -------------------------------------------------------------------------
PRINT '>>> Iniciando mantenimiento y optimización de índices en DB_SOR...';

DECLARE @TableName NVARCHAR(256);
DECLARE @IndexName NVARCHAR(256);
DECLARE @AvgFragmentation FLOAT;
DECLARE @Sql NVARCHAR(MAX);

DECLARE IndexCursor CURSOR FOR
SELECT 
    dbschemas.name + '.' + dbtables.name AS TableName,
    dbindexes.name AS IndexName,
    indexstats.avg_fragmentation_in_percent AS AvgFragmentation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS indexstats
INNER JOIN sys.tables AS dbtables ON dbtables.object_id = indexstats.object_id
INNER JOIN sys.schemas AS dbschemas ON dbtables.schema_id = dbschemas.schema_id
INNER JOIN sys.indexes AS dbindexes ON dbindexes.object_id = indexstats.object_id AND indexstats.index_id = dbindexes.index_id
WHERE indexstats.avg_fragmentation_in_percent > 10.0
  AND indexstats.page_count > 10
  AND dbindexes.name IS NOT NULL;

OPEN IndexCursor;
FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @AvgFragmentation;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF @AvgFragmentation >= 30.0
    BEGIN
        -- Si la fragmentación es mayor o igual a 30%, se RECONSTRUYE
        SET @Sql = 'ALTER INDEX [' + @IndexName + '] ON ' + @TableName + ' REBUILD WITH (ONLINE = OFF);';
        PRINT 'Reconstruyendo índice: ' + @IndexName + ' en ' + @TableName + ' (Fragmentación: ' + CAST(@AvgFragmentation AS NVARCHAR(20)) + '%)';
    END
    ELSE
    BEGIN
        -- Si la fragmentación está entre 10% y 29.9%, se REORGANIZA
        SET @Sql = 'ALTER INDEX [' + @IndexName + '] ON ' + @TableName + ' REORGANIZE;';
        PRINT 'Reorganizando índice: ' + @IndexName + ' en ' + @TableName + ' (Fragmentación: ' + CAST(@AvgFragmentation AS NVARCHAR(20)) + '%)';
    END

    EXEC sp_executesql @Sql;

    FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @AvgFragmentation;
END

CLOSE IndexCursor;
DEALLOCATE IndexCursor;

PRINT '>>> Mantenimiento de índices completado exitosamente.';
GO

-- -------------------------------------------------------------------------
-- 2. ACTUALIZACIÓN COMPLETA DE ESTADÍSTICAS
-- -------------------------------------------------------------------------
PRINT '>>> Actualizando estadísticas de consultas en DB_SOR...';
EXEC sp_updatestats;
PRINT '>>> Estadísticas actualizadas con éxito.';
GO

-- -------------------------------------------------------------------------
-- 3. PLANTILLA DE RESPALDO COMPLETO (FULL BACKUP)
-- -------------------------------------------------------------------------
/*
DECLARE @BackupPath NVARCHAR(500) = N'C:\Backups\DB_SOR_Full_' + REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(20), GETDATE(), 120), '-', ''), ' ', '_'), ':', '') + N'.bak';

BACKUP DATABASE [DB_SOR]
TO DISK = @BackupPath
WITH FORMAT, INIT, COMPRESSION, STATS = 10, CHECKSUM,
NAME = N'DB_SOR - Respaldo Completo Diario';
*/
GO
