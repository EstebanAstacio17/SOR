-- ============================================================================
-- SCRIPT MAESTRO DE BASE DE DATOS PARA AZURE SQL DATABASE: DB_SOR
-- Sistema de Gestión Interna OCC República Dominicana (SOR)
-- ============================================================================

-- 1. TABLA ESTADOS DE CUENTA
IF OBJECT_ID('dbo.EstadosCuenta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EstadosCuenta (
        IdEstado INT PRIMARY KEY,
        NombreEstado VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(255) NULL
    );

    INSERT INTO dbo.EstadosCuenta (IdEstado, NombreEstado, Descripcion) VALUES
    (1, 'PendienteAprobacionCorreo', 'Usuario recién registrado, correo pendiente de aprobación por admin'),
    (2, 'CorreoAprobado', 'Correo aprobado por admin, pendiente de llenar formulario de coordinador'),
    (3, 'PerfilPendienteAprobacion', 'Formulario completado, pendiente de aprobación final por admin'),
    (4, 'Activo', 'Usuario plenamente activo con acceso al sistema'),
    (5, 'Rechazado', 'Solicitud rechazada'),
    (6, 'Suspendido', 'Usuario suspendido');
END;

-- 2. TABLA ROLES DE SEGURIDAD
IF OBJECT_ID('dbo.RolesSeguridad', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolesSeguridad (
        IdRolSeguridad INT PRIMARY KEY,
        NombreRol VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(255) NULL
    );

    INSERT INTO dbo.RolesSeguridad (IdRolSeguridad, NombreRol, Descripcion) VALUES
    (1, 'SuperAdmin', 'Super Administrador con permisos totales y asignación de admins'),
    (2, 'Administrador', 'Administrador del sistema'),
    (3, 'Coordinador', 'Coordinador estándar (rol por defecto)');
END;

-- 3. TABLA NIVELES DE EQUIPO OCC
IF OBJECT_ID('dbo.NivelesEquipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NivelesEquipo (
        IdNivelEquipo INT PRIMARY KEY,
        NombreNivel VARCHAR(50) NOT NULL,
        RangoJerarquico INT NOT NULL -- 1: ENL (Mayor), 2: ERLE, 3: ERL (Menor)
    );

    INSERT INTO dbo.NivelesEquipo (IdNivelEquipo, NombreNivel, RangoJerarquico) VALUES
    (1, 'ENL', 1),
    (2, 'ERLE', 2),
    (3, 'ERL', 3);
END;

-- 4. TABLA EQUIPOS OCC
IF OBJECT_ID('dbo.Equipos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipos (
        IdEquipo INT IDENTITY(1,1) PRIMARY KEY,
        NombreEquipo VARCHAR(100) NOT NULL,
        IdNivelEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.NivelesEquipo(IdNivelEquipo),
        IdEquipoPadre INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        RowVersion ROWVERSION,
        FechaModificacion DATETIME2 NULL,
        UsuarioModificacion INT NULL
    );

    -- ENL
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('Equipo Nacional de Liderazgo', 1, NULL);
    DECLARE @IdENL INT = SCOPE_IDENTITY();

    -- ERLEs
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Santo Domingo', 2, @IdENL);
    DECLARE @IdERLE_SD INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Sur', 2, @IdENL);
    DECLARE @IdERLE_Sur INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Norte', 2, @IdENL);
    DECLARE @IdERLE_Norte INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Este', 2, @IdENL);
    DECLARE @IdERLE_Este INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('Ministerio Creole', 2, @IdENL);

    -- ERLs Santo Domingo
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
    ('ERL Santo Domingo Este', 3, @IdERLE_SD),
    ('ERL Santo Domingo Oeste', 3, @IdERLE_SD),
    ('ERL Santo Domingo Norte', 3, @IdERLE_SD),
    ('ERL Santo Domingo Nordeste', 3, @IdERLE_SD);

    -- ERLs Sur
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
    ('ERL Enriquillo', 3, @IdERLE_Sur),
    ('ERL El Valle', 3, @IdERLE_Sur),
    ('ERL Valdesia', 3, @IdERLE_Sur);

    -- ERLs Norte
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
    ('ERL Cibao Norte', 3, @IdERLE_Norte),
    ('ERL Puerto Plata', 3, @IdERLE_Norte),
    ('ERL Cibao Noroeste', 3, @IdERLE_Norte),
    ('ERL Cibao Nordeste', 3, @IdERLE_Norte),
    ('ERL Cibao Sur', 3, @IdERLE_Norte),
    ('ERL El Catey', 3, @IdERLE_Norte);

    -- ERLs Este
    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
    ('ERL Yuma', 3, @IdERLE_Este),
    ('ERL Higuamo', 3, @IdERLE_Este);
END;

-- 5. TABLA POSICIONES OCC
IF OBJECT_ID('dbo.PosicionesOCC', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PosicionesOCC (
        IdPosicion INT PRIMARY KEY,
        NombrePosicion VARCHAR(100) NOT NULL,
        Descripcion VARCHAR(255) NULL
    );

    INSERT INTO dbo.PosicionesOCC (IdPosicion, NombrePosicion, Descripcion) VALUES
    (1, 'Coordinador de Equipo', 'Líder principal del equipo'),
    (2, 'Coordinador de Movilización', 'Encargado de movilización e iglesias'),
    (3, 'Coordinador de Discipulado', 'Encargado de discipulado y capacitaciones'),
    (4, 'Coordinador de Recursos', 'Encargado de inventarios y recursos'),
    (5, 'Coordinador de Oración', 'Encargado de la red de oración'),
    (6, 'Coordinador de Logística', 'Encargado de despachos y logística');
END;

-- 6. TABLA USUARIOS
IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
        Correo VARCHAR(100) NOT NULL UNIQUE,
        Clave VARCHAR(100) NOT NULL,
        IdRolSeguridad INT NOT NULL DEFAULT 3 FOREIGN KEY REFERENCES dbo.RolesSeguridad(IdRolSeguridad),
        IdEstado INT NOT NULL DEFAULT 1 FOREIGN KEY REFERENCES dbo.EstadosCuenta(IdEstado),
        TokenRecuperacion VARCHAR(100) NULL,
        ExpiracionTokenRecuperacion DATETIME NULL,
        IntentosFallidosToken INT DEFAULT 0,
        FechaRegistro DATETIME DEFAULT GETDATE(),
        FechaUltimoAcceso DATETIME NULL,
        IntentosFallidosLogin INT DEFAULT 0,
        FechaUltimoIntentoFallido DATETIME NULL,
        FechaBloqueo DATETIME NULL
    );

    -- SuperAdmin Inicial (admin@occrd.org / admin123)
    INSERT INTO dbo.Usuarios (Correo, Clave, IdRolSeguridad, IdEstado)
    VALUES ('admin@occrd.org', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 1, 4);
END;

-- 7. TABLA ASIGNACIONES DE EQUIPO
IF OBJECT_ID('dbo.AsignacionesEquipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AsignacionesEquipo (
        IdAsignacion INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdPosicion INT NOT NULL FOREIGN KEY REFERENCES dbo.PosicionesOCC(IdPosicion),
        FechaAsignacion DATETIME DEFAULT GETDATE(),
        Activo BIT DEFAULT 1
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.AsignacionesEquipo WHERE IdUsuario = 1)
    BEGIN
        INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo)
        VALUES (1, 1, 1, 1);
    END;
END;

-- 8. TABLA PERFILES DE COORDINADOR
IF OBJECT_ID('dbo.PerfilesCoordinador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilesCoordinador (
        IdPerfil INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        PrimerNombre VARCHAR(50) NOT NULL,
        OtrosNombres VARCHAR(50) NULL,
        PrimerApellido VARCHAR(50) NOT NULL,
        OtrosApellidos VARCHAR(50) NULL,
        FechaNacimiento DATE NULL,
        Calle VARCHAR(100) NULL,
        Numero VARCHAR(20) NULL,
        Sector VARCHAR(100) NULL,
        Ciudad VARCHAR(100) NULL,
        Provincia VARCHAR(100) NULL,
        Pais VARCHAR(100) DEFAULT 'República Dominicana',
        Nacionalidad VARCHAR(50) DEFAULT 'Dominicana',
        Talla VARCHAR(10) NULL,
        NumeroDocumento VARCHAR(30) NULL,
        DocumentoAdjuntoRuta VARCHAR(255) NULL,
        NumeroPasaporte VARCHAR(30) NULL,
        PasaporteAdjuntoRuta VARCHAR(255) NULL,
        TelefonoFijo VARCHAR(20) NULL,
        TelefonoCelularWhatsApp VARCHAR(20) NULL,
        Correo VARCHAR(100) NULL,
        FotoRuta VARCHAR(255) NULL,
        DatosConyugue NVARCHAR(MAX) NULL,
        ContactoEmergencia NVARCHAR(MAX) NULL,
        IglesiaLocal VARCHAR(150) NULL,
        PastorIglesiaLocal VARCHAR(150) NULL,
        CargoIglesiaLocal VARCHAR(100) NULL,
        AniosServicioMinisterial INT NULL,
        InfoMinisterial NVARCHAR(MAX) NULL,
        NivelEducativo VARCHAR(100) NULL,
        ProfesionCarrera VARCHAR(150) NULL,
        InfoEducativa NVARCHAR(MAX) NULL,
        OcupacionEmpresaLaboral VARCHAR(150) NULL,
        TelefonoTrabajo VARCHAR(30) NULL,
        InfoLaboral NVARCHAR(MAX) NULL,
        CapacitacionesOCC NVARCHAR(MAX) NULL,
        Ministerio VARCHAR(100) NULL,
        IdEquipo INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdPosicion INT NULL FOREIGN KEY REFERENCES dbo.PosicionesOCC(IdPosicion),
        FechaIngreso DATE NULL,
        FechaCompletado DATETIME DEFAULT GETDATE()
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.PerfilesCoordinador WHERE IdUsuario = 1)
    BEGIN
        INSERT INTO dbo.PerfilesCoordinador (
            IdUsuario, PrimerNombre, PrimerApellido, TelefonoCelularWhatsApp, Correo, 
            IglesiaLocal, PastorIglesiaLocal, CargoIglesiaLocal, NivelEducativo, ProfesionCarrera,
            IdEquipo, IdPosicion, FechaIngreso
        ) VALUES (
            1, 'SuperAdmin', 'OCC', '809-555-0000', 'admin@occrd.org',
            'Iglesia Central OCC', 'Pastor Principal', 'Líder Nacional', 'Licenciatura', 'Administración',
            1, 1, GETDATE()
        );
    END;
END;

-- 9. TABLA IGLESIAS
IF OBJECT_ID('dbo.Iglesias', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Iglesias (
        IdIglesia INT IDENTITY(1,1) PRIMARY KEY,
        NombreIglesia VARCHAR(150) NOT NULL,
        RNC_Cedula VARCHAR(30) NULL,
        Telefono VARCHAR(20) NULL,
        Calle VARCHAR(100) NULL,
        Numero VARCHAR(20) NULL,
        Sector VARCHAR(100) NULL,
        Ciudad VARCHAR(100) NULL,
        Provincia VARCHAR(100) NULL,
        Referencia NVARCHAR(MAX) NULL,
        IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdUsuarioCreacion INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaCreacion DATETIME DEFAULT GETDATE(),
        RowVersion ROWVERSION,
        FechaModificacion DATETIME2 NULL,
        UsuarioModificacion INT NULL
    );
END;

-- 10. TABLA PERSONAS DE IGLESIAS
IF OBJECT_ID('dbo.PersonasIglesia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PersonasIglesia (
        IdPersonaIglesia INT IDENTITY(1,1) PRIMARY KEY,
        IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        TipoPersona VARCHAR(20) NOT NULL,
        Nombres VARCHAR(100) NOT NULL,
        Apellidos VARCHAR(100) NOT NULL,
        DocumentoIdentidad VARCHAR(30) NULL,
        DocumentoAdjuntoRuta VARCHAR(255) NULL,
        Celular VARCHAR(20) NULL,
        Correo VARCHAR(100) NULL,
        Calle VARCHAR(100) NULL,
        Numero VARCHAR(20) NULL,
        Sector VARCHAR(100) NULL,
        Referencia NVARCHAR(MAX) NULL
    );
END;

-- 11. TABLA TEMPORADAS
IF OBJECT_ID('dbo.Temporadas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Temporadas (
        IdTemporada INT IDENTITY(1,1) PRIMARY KEY,
        NombreTemporada VARCHAR(50) NOT NULL,
        FechaInicio DATE NULL,
        FechaFin DATE NULL,
        Activa BIT DEFAULT 1
    );

    INSERT INTO dbo.Temporadas (NombreTemporada, Activa) VALUES ('Temp 2026-2027', 1);
END;

-- 12. TABLA PARTICIPACIONES EN TEMPORADA
IF OBJECT_ID('dbo.ParticipacionesIglesia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParticipacionesIglesia (
        IdParticipacion INT IDENTITY(1,1) PRIMARY KEY,
        IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        Participara BIT DEFAULT 1,
        EtapaActual INT NOT NULL DEFAULT 1,
        JustificacionNoParticipacion NVARCHAR(MAX) NULL,
        EstadoEvaluacion VARCHAR(50) DEFAULT 'Pendiente',
        IdUsuarioEvaluador INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaSolicitud DATETIME DEFAULT GETDATE(),
        FechaEvaluacion DATETIME NULL,
        VisionFecha DATETIME NULL,
        VisionLugar NVARCHAR(150) NULL,
        VisionAsistio BIT NULL,
        VisionResultado NVARCHAR(50) NULL,
        EvalTallerEstado NVARCHAR(50) NULL,
        EvalTallerFecha DATETIME NULL,
        TallerParticipo BIT NULL,
        TallerNombre NVARCHAR(150) NULL,
        TallerFecha DATETIME NULL,
        EstatusEvaluacionReporte NVARCHAR(50) NULL
    );
END;

-- 13. TABLA ASIGNACIONES DE RECURSOS
IF OBJECT_ID('dbo.AsignacionesRecursos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AsignacionesRecursos (
        IdAsignacionRecurso INT IDENTITY(1,1) PRIMARY KEY,
        IdParticipacion INT NOT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
        OportunidadesEvangelisticas INT DEFAULT 0,
        LibrosMejorRegalo INT DEFAULT 0,
        LibrosMaestros INT DEFAULT 0,
        LibrosAlumno INT DEFAULT 0,
        Posters INT DEFAULT 0,
        NuevosTestamentos INT DEFAULT 0,
        FechaDespacho DATETIME NULL,
        IdUsuarioDespacho INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario)
    );
END;

-- 14. TABLA REPORTES DE EVENTOS Y LA GRAN AVENTURA
IF OBJECT_ID('dbo.ReportesEventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportesEventos (
        IdReporteEvento INT IDENTITY(1,1) PRIMARY KEY,
        IdParticipacion INT NOT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
        TipoReporte VARCHAR(30) NOT NULL,
        Fecha DATE NULL,
        CantidadNinos INT DEFAULT 0,
        CantidadClases INT DEFAULT 0,
        AsistenciaPorClase NVARCHAR(MAX) NULL,
        CuantosAceptaronSenor INT DEFAULT 0,
        CuantosComprometieron INT DEFAULT 0,
        CuantosGraduaron INT DEFAULT 0,
        ReporteAdjuntoRuta VARCHAR(255) NULL,
        Notas NVARCHAR(MAX) NULL,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END;

-- 15. TABLA EVENTOS Y ASISTENCIAS
IF OBJECT_ID('dbo.Eventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Eventos (
        IdEvento INT IDENTITY(1,1) PRIMARY KEY,
        NombreEvento VARCHAR(150) NOT NULL,
        TipoEvento VARCHAR(50) NOT NULL,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        Fecha DATE NOT NULL,
        Lugar VARCHAR(200) NOT NULL,
        Responsable VARCHAR(150) NOT NULL,
        IdUsuarioCreacion INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaCreacion DATETIME DEFAULT GETDATE(),
        TipoLugar NVARCHAR(50) NULL,
        Hora NVARCHAR(20) NULL,
        CantidadAsistentes INT DEFAULT 0,
        RowVersion ROWVERSION,
        FechaModificacion DATETIME2 NULL,
        UsuarioModificacion INT NULL
    );
END;

IF OBJECT_ID('dbo.EventosAsistentes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventosAsistentes (
        IdAsistente INT IDENTITY(1,1) PRIMARY KEY,
        IdEvento INT NOT NULL FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdParticipacion INT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
        NombreCompleto NVARCHAR(150) NOT NULL,
        Telefono NVARCHAR(30) NULL,
        Identificacion NVARCHAR(30) NULL,
        Rol NVARCHAR(50) NULL
    );
END;

IF OBJECT_ID('dbo.EventosParticipacionIglesia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventosParticipacionIglesia (
        IdEvento INT NOT NULL FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdParticipacion INT NOT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
        PRIMARY KEY (IdEvento, IdParticipacion)
    );
END;

-- 16. TABLA MAESTROS Y ASISTENCIA
IF OBJECT_ID('dbo.Maestros', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Maestros (
        IdMaestro INT IDENTITY(1,1) PRIMARY KEY,
        IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        NombreCompleto NVARCHAR(150) NOT NULL,
        Telefono NVARCHAR(30) NULL,
        DocumentoIdentidad NVARCHAR(30) NULL,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END;

IF OBJECT_ID('dbo.AsistenciaMaestro', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AsistenciaMaestro (
        IdAsistencia INT IDENTITY(1,1) PRIMARY KEY,
        IdEvento INT NOT NULL FOREIGN KEY REFERENCES dbo.Eventos(IdEvento),
        IdMaestro INT NOT NULL FOREIGN KEY REFERENCES dbo.Maestros(IdMaestro),
        Asistio BIT DEFAULT 0,
        FechaRegistro DATETIME DEFAULT GETDATE()
    );
END;

-- 17. TABLA COMPAÑEROS DE ORACIÓN
IF OBJECT_ID('dbo.CompanerosOracion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanerosOracion (
        IdCompanero INT IDENTITY(1,1) PRIMARY KEY,
        NombreCompleto NVARCHAR(150) NOT NULL,
        ContactoWhatsApp NVARCHAR(30) NULL,
        EsMayorEdad BIT NOT NULL DEFAULT 1,
        IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdUsuarioRegistro INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaRegistro DATETIME DEFAULT GETDATE()
    );
END;

-- 18. TABLAS DE LOGÍSTICA E INVENTARIOS
IF OBJECT_ID('dbo.RecepcionesContenedor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionesContenedor (
        IdRecepcion INT IDENTITY(1,1) PRIMARY KEY,
        NumeroBL_Contenedor NVARCHAR(50) NOT NULL,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdUsuarioCreacion INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaRecepcion DATETIME NOT NULL DEFAULT GETDATE(),
        Notas NVARCHAR(255) NULL
    );
END;

IF OBJECT_ID('dbo.RecepcionesContenedorDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionesContenedorDetalle (
        IdDetalle INT IDENTITY(1,1) PRIMARY KEY,
        IdRecepcion INT NOT NULL FOREIGN KEY REFERENCES dbo.RecepcionesContenedor(IdRecepcion) ON DELETE CASCADE,
        TipoMaterial NVARCHAR(50) NOT NULL,
        CantidadCajasFisicas INT NOT NULL DEFAULT 0,
        UnidadesPorCaja INT NOT NULL DEFAULT 1,
        TotalUnidades INT NOT NULL DEFAULT 0
    );
END;

IF OBJECT_ID('dbo.TransferenciasEquipos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferenciasEquipos (
        IdTransferencia INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipoOrigen INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdEquipoDestino INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        IdUsuarioDespacho INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        IdUsuarioRecepcion INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        Estado NVARCHAR(30) NOT NULL DEFAULT 'EnTransito',
        FechaDespacho DATETIME NOT NULL DEFAULT GETDATE(),
        FechaRecepcion DATETIME NULL,
        NumeroGuia NVARCHAR(50) NULL,
        Notas NVARCHAR(255) NULL
    );
END;

IF OBJECT_ID('dbo.TransferenciasEquiposDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferenciasEquiposDetalle (
        IdDetalle INT IDENTITY(1,1) PRIMARY KEY,
        IdTransferencia INT NOT NULL FOREIGN KEY REFERENCES dbo.TransferenciasEquipos(IdTransferencia) ON DELETE CASCADE,
        TipoMaterial NVARCHAR(50) NOT NULL,
        CantidadCajas INT NOT NULL DEFAULT 0,
        TotalUnidades INT NOT NULL DEFAULT 0
    );
END;

IF OBJECT_ID('dbo.KardexMovimientos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KardexMovimientos (
        IdKardex INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        TipoMaterial NVARCHAR(50) NOT NULL,
        TipoMovimiento NVARCHAR(30) NOT NULL,
        Entrada INT NOT NULL DEFAULT 0,
        Salida INT NOT NULL DEFAULT 0,
        SaldoFinal INT NOT NULL DEFAULT 0,
        Referencia NVARCHAR(100) NULL,
        FechaMovimiento DATETIME DEFAULT GETDATE(),
        IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario)
    );
END;

-- 19. TABLAS DE REPORTES EOS: IGLESIAS PLANTADAS Y GNA
IF OBJECT_ID('dbo.EOS_IglesiasPlantadas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EOS_IglesiasPlantadas (
        IdIglesiaPlantada INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        NombreIglesia NVARCHAR(150) NOT NULL,
        PastorPrincipal NVARCHAR(150) NOT NULL,
        Ubicacion NVARCHAR(200) NOT NULL,
        CajitasEntregadas INT NOT NULL DEFAULT 0,
        InscritosLGA INT NOT NULL DEFAULT 0,
        FechaPlantacion DATE NULL,
        Notas NVARCHAR(255) NULL,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END;

IF OBJECT_ID('dbo.EOS_GruposNoAlcanzados', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EOS_GruposNoAlcanzados (
        IdGNA INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        NombreGNA NVARCHAR(150) NOT NULL,
        CompaneroMinisterio NVARCHAR(150) NOT NULL,
        CajitasEntregadas INT NOT NULL DEFAULT 0,
        InscritosLGA INT NOT NULL DEFAULT 0,
        NinosCreenJesus INT NOT NULL DEFAULT 0,
        NinosOranComparten INT NOT NULL DEFAULT 0,
        NinosGraduados INT NOT NULL DEFAULT 0,
        Notas NVARCHAR(255) NULL,
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END;

-- 20. TABLAS DE FINANZAS
IF OBJECT_ID('dbo.FinanzasReportes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinanzasReportes (
        IdReporteFinanzas INT IDENTITY(1,1) PRIMARY KEY,
        IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
        IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
        Mes INT NOT NULL,
        Anio INT NOT NULL,
        SaldoInicial DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalIngresos DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalEgresos DECIMAL(18,2) NOT NULL DEFAULT 0,
        SaldoFinal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Estado NVARCHAR(30) NOT NULL DEFAULT 'Borrador',
        IdUsuarioCreacion INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
        FechaCreacion DATETIME DEFAULT GETDATE()
    );
END;

IF OBJECT_ID('dbo.FinanzasReportesDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinanzasReportesDetalle (
        IdDetalle INT IDENTITY(1,1) PRIMARY KEY,
        IdReporteFinanzas INT NOT NULL FOREIGN KEY REFERENCES dbo.FinanzasReportes(IdReporteFinanzas) ON DELETE CASCADE,
        Fecha DATE NOT NULL,
        NumeroComprobante NVARCHAR(50) NULL,
        Concepto NVARCHAR(255) NOT NULL,
        Categoria NVARCHAR(50) NOT NULL,
        TipoTransaccion NVARCHAR(20) NOT NULL, -- 'Ingreso' o 'Egreso'
        Monto DECIMAL(18,2) NOT NULL DEFAULT 0
    );
END;

-- 21. TABLA AUDITORÍA
IF OBJECT_ID('dbo.AuditoriaLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaLogs (
        IdLog INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NULL,
        Modulo NVARCHAR(50) NOT NULL,
        Accion NVARCHAR(50) NOT NULL,
        Detalle NVARCHAR(MAX) NULL,
        FechaHora DATETIME DEFAULT GETDATE(),
        DireccionIP NVARCHAR(50) NULL
    );
END;

-- 22. PROCEDIMIENTOS ALMACENADOS DE ACCESO
IF OBJECT_ID('dbo.sp_ValidarUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ValidarUsuario;
GO
CREATE PROCEDURE dbo.sp_ValidarUsuario
    @Correo VARCHAR(100),
    @Clave VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.IdUsuario,
        u.Correo,
        u.IdRolSeguridad,
        r.NombreRol,
        u.IdEstado,
        e.NombreEstado,
        a.IdEquipo,
        eq.NombreEquipo,
        neq.NombreNivel,
        neq.RangoJerarquico,
        a.IdPosicion,
        p.NombrePosicion
    FROM dbo.Usuarios u
    INNER JOIN dbo.RolesSeguridad r ON u.IdRolSeguridad = r.IdRolSeguridad
    INNER JOIN dbo.EstadosCuenta e ON u.IdEstado = e.IdEstado
    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
    LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
    LEFT JOIN dbo.NivelesEquipo neq ON eq.IdNivelEquipo = neq.IdNivelEquipo
    LEFT JOIN dbo.PosicionesOCC p ON a.IdPosicion = p.IdPosicion
    WHERE u.Correo = @Correo AND u.Clave = @Clave;
END;
GO
