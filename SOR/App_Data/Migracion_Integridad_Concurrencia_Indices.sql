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

-- 6. TABLA DE EXCEPCIONES A LA REGLA DE 3 AÑOS (DOBLE APROBACIÓN CE + CMI)
IF OBJECT_ID('dbo.ExcepcionesRegla3Anios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExcepcionesRegla3Anios (
        IdExcepcion INT IDENTITY(1,1) PRIMARY KEY,
        IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        TemporadaPreviaId INT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        DiferenciaTemporadas INT NOT NULL DEFAULT 1,
        Motivo NVARCHAR(250) NOT NULL,
        Justificacion NVARCHAR(MAX) NOT NULL,
        ResultadoDesempeno NVARCHAR(MAX) NULL,
        SolicitadoPor INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaSolicitud DATETIME2 DEFAULT GETDATE(),
        AprobadoCE BIT NOT NULL DEFAULT 0,
        UsuarioAprobacionCE INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaAprobacionCE DATETIME2 NULL,
        ComentarioCE NVARCHAR(500) NULL,
        AprobadoCMI BIT NOT NULL DEFAULT 0,
        UsuarioAprobacionCMI INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaAprobacionCMI DATETIME2 NULL,
        ComentarioCMI NVARCHAR(500) NULL,
        Rechazado BIT NOT NULL DEFAULT 0,
        UsuarioRechazo INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaRechazo DATETIME2 NULL,
        MotivoRechazo NVARCHAR(500) NULL,
        Estado VARCHAR(30) NOT NULL DEFAULT 'PENDIENTE',
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        FechaModificacion DATETIME2 NULL,
        RowVersion ROWVERSION
    );

    CREATE NONCLUSTERED INDEX IX_Excepciones_Iglesia_Temporada 
    ON dbo.ExcepcionesRegla3Anios(IdIglesia, IdTemporada, Estado);

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Excepcion_Iglesia_Temporada_Activa 
    ON dbo.ExcepcionesRegla3Anios(IdIglesia, IdTemporada) 
    WHERE Estado IN ('PENDIENTE', 'APROBADA');
END
GO

-- 7. ÍNDICE ÚNICO FILTRADO: MÁXIMO 1 SUPERADMIN ACTIVO EN TODA LA PLATAFORMA
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'UQ_Usuarios_UnicoSuperAdminActivo' AND object_id = OBJECT_ID('dbo.Usuarios')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Usuarios_UnicoSuperAdminActivo
    ON dbo.Usuarios(IdRolSeguridad)
    WHERE IdRolSeguridad = 1 AND IdEstado = 4;
END
GO

-- ============================================================================
-- MÓDULO 8: LOGÍSTICA, INVENTARIO Y DESPACHO DE MATERIALES OCC
-- Tablas: Materiales, Presentaciones, Almacenes, Recepciones, Inventarios,
--         Movimientos, Transferencias, EventosDespacho, DespachosIglesia
-- ============================================================================

-- 8.1 Catálogo de Materiales
IF OBJECT_ID('dbo.Materiales', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Materiales (
        IdMaterial       INT IDENTITY(1,1) PRIMARY KEY,
        Codigo           VARCHAR(30)  NOT NULL,
        NombreMaterial   VARCHAR(100) NOT NULL,
        UnidadEntrega    VARCHAR(50)  NOT NULL DEFAULT 'Unidad',
        MomentoEntrega   VARCHAR(50)  NOT NULL DEFAULT 'DESPACHO',
        Activo           BIT          NOT NULL DEFAULT 1,
        CONSTRAINT UQ_Materiales_Codigo UNIQUE (Codigo)
    );
    -- Catálogo base OCC
    INSERT INTO dbo.Materiales (Codigo, NombreMaterial, UnidadEntrega, MomentoEntrega) VALUES
        ('GM',  'Guías del Maestro',             'Guía',     'TALLER_OCC'),
        ('GA',  'Guías del Alumno',              'Guía',     'TALLER_OCC'),
        ('OE',  'Oportunidades Evangelísticas',  'Libro',    'DESPACHO'),
        ('MR',  'El Mejor Regalo',               'Libro',    'DESPACHO'),
        ('PO',  'Posters',                       'Poster',   'DESPACHO'),
        ('NT',  'Nuevos Testamentos',            'Ejemplar', 'DESPACHO'),
        ('BR',  'Brochures',                     'Brochure', 'PRESENTACION_VISION');
END
GO

-- 8.2 Presentaciones / Empaques por Material
IF OBJECT_ID('dbo.PresentacionesMaterial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PresentacionesMaterial (
        IdPresentacion       INT IDENTITY(1,1) PRIMARY KEY,
        IdMaterial           INT          NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        TipoEmpaque          VARCHAR(50)  NOT NULL DEFAULT 'Caja',
        UnidadesPorEmpaque   INT          NOT NULL,
        IdTemporadaVigencia  INT          NULL,
        FechaVigenciaInicio  DATETIME2    NOT NULL DEFAULT GETDATE(),
        Activo               BIT          NOT NULL DEFAULT 1
    );
    -- Presentaciones estándar iniciales
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 20  FROM dbo.Materiales WHERE Codigo = 'GM';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 45  FROM dbo.Materiales WHERE Codigo = 'GA';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 12  FROM dbo.Materiales WHERE Codigo = 'OE';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 16  FROM dbo.Materiales WHERE Codigo = 'OE';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 200 FROM dbo.Materiales WHERE Codigo = 'MR';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Rollo', 25 FROM dbo.Materiales WHERE Codigo = 'PO';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Caja', 30  FROM dbo.Materiales WHERE Codigo = 'NT';
    INSERT INTO dbo.PresentacionesMaterial (IdMaterial, TipoEmpaque, UnidadesPorEmpaque)
    SELECT IdMaterial, 'Paquete', 100 FROM dbo.Materiales WHERE Codigo = 'BR';
END
GO

-- 8.3 Almacenes
IF OBJECT_ID('dbo.Almacenes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Almacenes (
        IdAlmacen     INT IDENTITY(1,1) PRIMARY KEY,
        NombreAlmacen VARCHAR(150) NOT NULL,
        Direccion     VARCHAR(255) NULL,
        Responsable   VARCHAR(150) NULL,
        Telefono      VARCHAR(50)  NULL,
        Activo        BIT          NOT NULL DEFAULT 1
    );
    INSERT INTO dbo.Almacenes (NombreAlmacen, Direccion, Responsable)
    VALUES ('Almacén Central OCC Santo Domingo', 'Santo Domingo, República Dominicana', 'Dirección Nacional');
END
GO

-- 8.4 Recepciones de Contenedor (encabezado)
IF OBJECT_ID('dbo.RecepcionesContenedor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionesContenedor (
        IdRecepcion         INT IDENTITY(1,1) PRIMARY KEY,
        NumeroContenedor    VARCHAR(100)    NOT NULL,
        IdTemporada         INT             NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdAlmacen           INT             NOT NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        FechaRecepcion      DATETIME2       NOT NULL,
        ResponsableRecepcion VARCHAR(150)   NULL,
        Observaciones       NVARCHAR(MAX)   NULL,
        EstadoRecepcion     VARCHAR(30)     NOT NULL DEFAULT 'CONFIRMADA',
        IdUsuarioRegistro   INT             NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaRegistro       DATETIME2       NOT NULL DEFAULT GETDATE()
    );
    CREATE NONCLUSTERED INDEX IX_Recepciones_Temporada ON dbo.RecepcionesContenedor(IdTemporada, IdAlmacen);
END
GO

-- 8.5 Detalle de Recepciones (inmutable histórico)
IF OBJECT_ID('dbo.RecepcionesContenedorDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionesContenedorDetalle (
        IdRecepcionDetalle    INT IDENTITY(1,1) PRIMARY KEY,
        IdRecepcion           INT NOT NULL FOREIGN KEY REFERENCES dbo.RecepcionesContenedor(IdRecepcion),
        IdMaterial            INT NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        IdPresentacion        INT NOT NULL FOREIGN KEY REFERENCES dbo.PresentacionesMaterial(IdPresentacion),
        CantidadEmpaques      INT NOT NULL,
        UnidadesPorEmpaque    INT NOT NULL,
        CantidadTotalUnidades AS (CantidadEmpaques * UnidadesPorEmpaque) PERSISTED
    );
END
GO

-- 8.6 Inventario Central (por Temporada y Almacén)
IF OBJECT_ID('dbo.InventarioCentral', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventarioCentral (
        IdInventarioCentral  INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada          INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdAlmacen            INT NOT NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        IdMaterial           INT NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        CantidadFisica       INT NOT NULL DEFAULT 0,
        CantidadTransferida  INT NOT NULL DEFAULT 0,
        CantidadDisponible   INT NOT NULL DEFAULT 0,
        CONSTRAINT CK_InventarioCentral_Disp CHECK (CantidadDisponible >= 0),
        CONSTRAINT UQ_InventarioCentral_Temp_Alm_Mat UNIQUE (IdTemporada, IdAlmacen, IdMaterial)
    );
END
GO

-- 8.7 Kárdex de Movimientos (inmutable)
IF OBJECT_ID('dbo.MovimientosInventario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientosInventario (
        IdMovimiento          INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada           INT             NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        TipoMovimiento        VARCHAR(50)     NOT NULL,
        IdMaterial            INT             NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        Cantidad              INT             NOT NULL,
        IdAlmacenOrigen       INT             NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        IdAlmacenDestino      INT             NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        IdEquipoDestino       INT             NULL,
        IdIglesia             INT             NULL,
        IdDocumentoReferencia VARCHAR(100)    NULL,
        FechaHora             DATETIME2       NOT NULL DEFAULT GETDATE(),
        IdUsuario             INT             NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        Justificacion         NVARCHAR(500)   NULL
    );
    CREATE NONCLUSTERED INDEX IX_Movimientos_Temp_Mat ON dbo.MovimientosInventario(IdTemporada, IdMaterial, FechaHora DESC);
END
GO

-- 8.8 Transferencias a Equipos (encabezado)
IF OBJECT_ID('dbo.TransferenciasEquipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferenciasEquipo (
        IdTransferencia        INT IDENTITY(1,1) PRIMARY KEY,
        NumeroConstancia       VARCHAR(50)  NOT NULL,
        IdTemporada            INT          NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo               INT          NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdAlmacenOrigen        INT          NOT NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        FechaTransferencia     DATETIME2    NOT NULL,
        CoordinadorEmisor      VARCHAR(150) NULL,
        PersonaReceptoraEquipo VARCHAR(150) NULL,
        Observaciones          NVARCHAR(500) NULL,
        Estado                 VARCHAR(30)  NOT NULL DEFAULT 'COMPLETADA',
        IdUsuarioRegistro      INT          NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT UQ_TransferenciasEquipo_Constancia UNIQUE (NumeroConstancia)
    );
END
GO

-- 8.9 Detalle de Transferencias
IF OBJECT_ID('dbo.TransferenciasEquipoDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferenciasEquipoDetalle (
        IdTransferenciaDetalle INT IDENTITY(1,1) PRIMARY KEY,
        IdTransferencia        INT NOT NULL FOREIGN KEY REFERENCES dbo.TransferenciasEquipo(IdTransferencia),
        IdMaterial             INT NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        CantidadUnidades       INT NOT NULL
    );
END
GO

-- 8.10 Inventario por Equipo
IF OBJECT_ID('dbo.InventarioEquipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventarioEquipo (
        IdInventarioEquipo INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada        INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo           INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdMaterial         INT NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        CantidadRecibida   INT NOT NULL DEFAULT 0,
        CantidadAsignada   INT NOT NULL DEFAULT 0,
        CantidadDespachada INT NOT NULL DEFAULT 0,
        CantidadDisponible INT NOT NULL DEFAULT 0,
        CONSTRAINT CK_InventarioEquipo_Disp CHECK (CantidadDisponible >= 0),
        CONSTRAINT UQ_InventarioEquipo_Temp_Eq_Mat UNIQUE (IdTemporada, IdEquipo, IdMaterial)
    );
END
GO

-- 8.11 Extensión de Eventos para tipo Despacho
IF OBJECT_ID('dbo.EventosDespacho', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventosDespacho (
        IdEventoDespacho       INT IDENTITY(1,1) PRIMARY KEY,
        IdEvento               INT NOT NULL UNIQUE FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdAlmacen              INT NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen),
        IdEquipo               INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        EstadoDespachoEvento   VARCHAR(30) NOT NULL DEFAULT 'PROGRAMADO'
    );
END
GO

-- 8.12 Coordinadores asistentes al evento de despacho
IF OBJECT_ID('dbo.CoordinadoresEventoDespacho', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CoordinadoresEventoDespacho (
        IdCoordinadorEvento INT IDENTITY(1,1) PRIMARY KEY,
        IdEvento            INT NOT NULL FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdUsuario           INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        HoraEntrada         TIME NULL,
        HoraSalida          TIME NULL,
        Presente            BIT NOT NULL DEFAULT 1,
        CONSTRAINT UQ_Coordinador_Evento UNIQUE (IdEvento, IdUsuario)
    );
END
GO

-- 8.13 Despachos por Iglesia (encabezado)
IF OBJECT_ID('dbo.DespachosIglesia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespachosIglesia (
        IdDespachoIglesia           INT IDENTITY(1,1) PRIMARY KEY,
        NumeroComprobanteDespacho   VARCHAR(50)     NOT NULL,
        IdEvento                    INT             NOT NULL FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdParticipacion             INT             NOT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
        IdIglesia                   INT             NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        IdTemporada                 INT             NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo                    INT             NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        EstadoDespacho              VARCHAR(30)     NOT NULL DEFAULT 'PROGRAMADA',
        TipoReceptor                VARCHAR(30)     NULL,
        NombreReceptor              VARCHAR(150)    NULL,
        DocumentoIdentidadReceptor  VARCHAR(50)     NULL,
        TelefonoReceptor            VARCHAR(50)     NULL,
        FechaHoraEntrega            DATETIME2       NULL,
        CoordinadorDespachador      VARCHAR(150)    NULL,
        IdUsuarioDespacho           INT             NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        MotivoNoDespacho            NVARCHAR(500)   NULL,
        Observaciones               NVARCHAR(MAX)   NULL,
        FechaRegistro               DATETIME2       NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_DespachosIglesia_Comprobante UNIQUE (NumeroComprobanteDespacho)
    );
    CREATE NONCLUSTERED INDEX IX_Despachos_Evento ON dbo.DespachosIglesia(IdEvento, EstadoDespacho);
    CREATE NONCLUSTERED INDEX IX_Despachos_Iglesia ON dbo.DespachosIglesia(IdIglesia, IdTemporada);
END
GO

-- 8.14 Detalle de cantidades despachadas por material
IF OBJECT_ID('dbo.DespachosIglesiaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespachosIglesiaDetalle (
        IdDespachoDetalle    INT IDENTITY(1,1) PRIMARY KEY,
        IdDespachoIglesia    INT NOT NULL FOREIGN KEY REFERENCES dbo.DespachosIglesia(IdDespachoIglesia),
        IdMaterial           INT NOT NULL FOREIGN KEY REFERENCES dbo.Materiales(IdMaterial),
        CantidadAsignada     INT NOT NULL DEFAULT 0,
        CantidadDespachada   INT NOT NULL DEFAULT 0,
        CantidadNoDespachada AS (CantidadAsignada - CantidadDespachada) PERSISTED
    );
END
GO

-- 8.15 Columnas de extensión en AsignacionesRecursos (si aún no existen)
IF COL_LENGTH('dbo.AsignacionesRecursos', 'EstadoAsignacion') IS NULL
    ALTER TABLE dbo.AsignacionesRecursos ADD EstadoAsignacion VARCHAR(40) NULL DEFAULT 'ASIGNADO';
GO
IF COL_LENGTH('dbo.AsignacionesRecursos', 'FechaDisponibleDespacho') IS NULL
    ALTER TABLE dbo.AsignacionesRecursos ADD FechaDisponibleDespacho DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.AsignacionesRecursos', 'IdEventoDespachoActual') IS NULL
    ALTER TABLE dbo.AsignacionesRecursos ADD IdEventoDespachoActual INT NULL;
GO

