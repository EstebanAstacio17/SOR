-- ============================================================================
-- SCRIPT DE CREACIÓN DE BASE DE DATOS Y ESQUEMA: DB_SOR (ORDEN DE DEPENDENCIAS CORREGIDO)
-- Sistema de Gestión Interna OCC Rep Dom (SOR)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DB_SOR')
BEGIN
    CREATE DATABASE DB_SOR;
END
GO

USE DB_SOR;
GO

-- 1. TABLA ESTADOS DE CUENTA / PERFIL
IF OBJECT_ID('dbo.EstadosCuenta', 'U') IS NOT NULL DROP TABLE dbo.EstadosCuenta;
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

-- 2. TABLA ROLES DE SEGURIDAD
IF OBJECT_ID('dbo.RolesSeguridad', 'U') IS NOT NULL DROP TABLE dbo.RolesSeguridad;
CREATE TABLE dbo.RolesSeguridad (
    IdRolSeguridad INT PRIMARY KEY,
    NombreRol VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255) NULL
);

INSERT INTO dbo.RolesSeguridad (IdRolSeguridad, NombreRol, Descripcion) VALUES
(1, 'SuperAdmin', 'Super Administrador con permisos totales y asignación de admins'),
(2, 'Administrador', 'Administrador del sistema'),
(3, 'Coordinador', 'Coordinador estándar (rol por defecto)');

-- 3. TABLA NIVELES DE EQUIPO OCC
IF OBJECT_ID('dbo.NivelesEquipo', 'U') IS NOT NULL DROP TABLE dbo.NivelesEquipo;
CREATE TABLE dbo.NivelesEquipo (
    IdNivelEquipo INT PRIMARY KEY,
    NombreNivel VARCHAR(50) NOT NULL,
    RangoJerarquico INT NOT NULL -- 1: ENL (Mayor), 2: ERLE, 3: ERL (Menor)
);

INSERT INTO dbo.NivelesEquipo (IdNivelEquipo, NombreNivel, RangoJerarquico) VALUES
(1, 'ENL', 1),   -- Equipo Nacional de Liderazgo
(2, 'ERLE', 2),  -- Equipo Estratégico
(3, 'ERL', 3);   -- Equipo Regional

-- 4. TABLA EQUIPOS OCC
IF OBJECT_ID('dbo.Equipos', 'U') IS NOT NULL DROP TABLE dbo.Equipos;
CREATE TABLE dbo.Equipos (
    IdEquipo INT IDENTITY(1,1) PRIMARY KEY,
    NombreEquipo VARCHAR(100) NOT NULL,
    IdNivelEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.NivelesEquipo(IdNivelEquipo),
    IdEquipoPadre INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo)
);

-- Inserción de Equipos Iniciales OCC RD
-- ENL (ID 1)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('Equipo Nacional de Liderazgo', 1, NULL);
DECLARE @IdENL INT = SCOPE_IDENTITY();

-- ERLEs (Nivel 2)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Santo Domingo', 2, @IdENL);
DECLARE @IdERLE_SD INT = SCOPE_IDENTITY();

INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Sur', 2, @IdENL);
DECLARE @IdERLE_Sur INT = SCOPE_IDENTITY();

INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Norte', 2, @IdENL);
DECLARE @IdERLE_Norte INT = SCOPE_IDENTITY();

INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('ERLE Región Este', 2, @IdENL);
DECLARE @IdERLE_Este INT = SCOPE_IDENTITY();

INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES ('Ministerio Creole', 2, @IdENL);

-- ERLs Santo Domingo (Nivel 3)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
('ERL Santo Domingo Este', 3, @IdERLE_SD),
('ERL Santo Domingo Oeste', 3, @IdERLE_SD),
('ERL Santo Domingo Norte', 3, @IdERLE_SD),
('ERL Santo Domingo Nordeste', 3, @IdERLE_SD);

-- ERLs Sur (Nivel 3)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
('ERL Enriquillo', 3, @IdERLE_Sur),
('ERL El Valle', 3, @IdERLE_Sur),
('ERL Valdesia', 3, @IdERLE_Sur);

-- ERLs Norte (Nivel 3)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
('ERL Cibao Norte', 3, @IdERLE_Norte),
('ERL Puerto Plata', 3, @IdERLE_Norte),
('ERL Cibao Noroeste', 3, @IdERLE_Norte),
('ERL Cibao Nordeste', 3, @IdERLE_Norte),
('ERL Cibao Sur', 3, @IdERLE_Norte),
('ERL El Catey', 3, @IdERLE_Norte);

-- ERLs Este (Nivel 3)
INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre) VALUES 
('ERL Yuma', 3, @IdERLE_Este),
('ERL Higuamo', 3, @IdERLE_Este);

-- 5. TABLA POSICIONES OCC
IF OBJECT_ID('dbo.PosicionesOCC', 'U') IS NOT NULL DROP TABLE dbo.PosicionesOCC;
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

-- 6. TABLA USUARIOS
IF OBJECT_ID('dbo.Usuarios', 'U') IS NOT NULL DROP TABLE dbo.Usuarios;
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
    FechaUltimoAcceso DATETIME NULL
);

-- 7. TABLA ASIGNACIONES DE EQUIPO
IF OBJECT_ID('dbo.AsignacionesEquipo', 'U') IS NOT NULL DROP TABLE dbo.AsignacionesEquipo;
CREATE TABLE dbo.AsignacionesEquipo (
    IdAsignacion INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
    IdEquipo INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
    IdPosicion INT NOT NULL FOREIGN KEY REFERENCES dbo.PosicionesOCC(IdPosicion),
    FechaAsignacion DATETIME DEFAULT GETDATE(),
    Activo BIT DEFAULT 1
);

-- Indice único estricto para evitar que la misma posición activa sea tomada en un mismo equipo por dos usuarios
CREATE UNIQUE INDEX IX_Asignacion_Equipo_Posicion_Activa 
ON dbo.AsignacionesEquipo(IdEquipo, IdPosicion) 
WHERE Activo = 1;

-- 8. TABLA PERFILES DE COORDINADOR
IF OBJECT_ID('dbo.PerfilesCoordinador', 'U') IS NOT NULL DROP TABLE dbo.PerfilesCoordinador;
CREATE TABLE dbo.PerfilesCoordinador (
    IdPerfil INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
    
    -- Sección 1: Datos Personales e Identidad
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
    
    -- Sección 2: Datos Ministeriales, Educativos y Laborales Detallados
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

    -- Sección 3: Asignación OCC
    Ministerio VARCHAR(100) NULL,
    IdEquipo INT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo),
    IdPosicion INT NULL FOREIGN KEY REFERENCES dbo.PosicionesOCC(IdPosicion),
    FechaIngreso DATE NULL,
    FechaCompletado DATETIME DEFAULT GETDATE()
);

-- ============================================================================
-- INSERCIÓN DE USUARIO SUPERADMIN E INICIALIZACIÓN DE ASIGNACIONES Y PERFIL
-- ============================================================================

-- SuperAdmin inicial por defecto (clave sha256 de 'admin123' -> 8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918)
INSERT INTO dbo.Usuarios (Correo, Clave, IdRolSeguridad, IdEstado)
VALUES ('admin@occrd.org', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 1, 4);

DECLARE @IdSuperAdmin INT = SCOPE_IDENTITY();

-- Asignar SuperAdmin inicial a ERLE Santo Domingo (IdEquipo 2), Posición Coordinador de Equipo (IdPosicion 1)
INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo)
VALUES (@IdSuperAdmin, 2, 1, 1);

-- Perfil inicial para SuperAdmin
INSERT INTO dbo.PerfilesCoordinador (
    IdUsuario, PrimerNombre, PrimerApellido, TelefonoCelularWhatsApp, Correo, 
    IglesiaLocal, PastorIglesiaLocal, CargoIglesiaLocal, NivelEducativo, ProfesionCarrera,
    IdEquipo, IdPosicion, FechaIngreso
) VALUES (
    @IdSuperAdmin, 'SuperAdmin', 'OCC', '809-555-0000', 'admin@occrd.org',
    'Iglesia Central OCC', 'Pastor Principal', 'Líder de Red', 'Licenciatura', 'Administración',
    2, 1, GETDATE()
);

-- 9. TABLA IGLESIAS
IF OBJECT_ID('dbo.Iglesias', 'U') IS NOT NULL DROP TABLE dbo.Iglesias;
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
    FechaCreacion DATETIME DEFAULT GETDATE()
);

-- 10. TABLA PERSONAS DE IGLESIAS (PASTOR, LÍDER, MAESTROS)
IF OBJECT_ID('dbo.PersonasIglesia', 'U') IS NOT NULL DROP TABLE dbo.PersonasIglesia;
CREATE TABLE dbo.PersonasIglesia (
    IdPersonaIglesia INT IDENTITY(1,1) PRIMARY KEY,
    IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
    TipoPersona VARCHAR(20) NOT NULL, -- 'Pastor', 'LiderMinisterial', 'Maestro'
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

-- 11. TABLA TEMPORADAS
IF OBJECT_ID('dbo.Temporadas', 'U') IS NOT NULL DROP TABLE dbo.Temporadas;
CREATE TABLE dbo.Temporadas (
    IdTemporada INT IDENTITY(1,1) PRIMARY KEY,
    NombreTemporada VARCHAR(50) NOT NULL,
    FechaInicio DATE NULL,
    FechaFin DATE NULL,
    Activa BIT DEFAULT 1
);

INSERT INTO dbo.Temporadas (NombreTemporada, Activa) VALUES ('Temporada 2026', 1);

-- 12. TABLA PARTICIPACIONES EN TEMPORADA
IF OBJECT_ID('dbo.ParticipacionesIglesia', 'U') IS NOT NULL DROP TABLE dbo.ParticipacionesIglesia;
CREATE TABLE dbo.ParticipacionesIglesia (
    IdParticipacion INT IDENTITY(1,1) PRIMARY KEY,
    IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
    IdTemporada INT NOT NULL FOREIGN KEY REFERENCES dbo.Temporadas(IdTemporada),
    Participara BIT DEFAULT 1,
    JustificacionNoParticipacion NVARCHAR(MAX) NULL,
    EstadoEvaluacion VARCHAR(50) DEFAULT 'Pendiente',
    IdUsuarioEvaluador INT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
    FechaSolicitud DATETIME DEFAULT GETDATE(),
    FechaEvaluacion DATETIME NULL
);

-- 13. TABLA ASIGNACIONES DE RECURSOS (DESPACHOS)
IF OBJECT_ID('dbo.AsignacionesRecursos', 'U') IS NOT NULL DROP TABLE dbo.AsignacionesRecursos;
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

-- 14. TABLA REPORTES DE EVENTOS Y LA GRAN AVENTURA
IF OBJECT_ID('dbo.ReportesEventos', 'U') IS NOT NULL DROP TABLE dbo.ReportesEventos;
CREATE TABLE dbo.ReportesEventos (
    IdReporteEvento INT IDENTITY(1,1) PRIMARY KEY,
    IdParticipacion INT NOT NULL FOREIGN KEY REFERENCES dbo.ParticipacionesIglesia(IdParticipacion),
    TipoReporte VARCHAR(30) NOT NULL, -- 'Evangelistico' o 'GranAventura'
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

-- 15. TABLA COMENTARIOS Y OBSERVACIONES DE IGLESIAS
IF OBJECT_ID('dbo.ComentariosObservaciones', 'U') IS NOT NULL DROP TABLE dbo.ComentariosObservaciones;
CREATE TABLE dbo.ComentariosObservaciones (
    IdComentario INT IDENTITY(1,1) PRIMARY KEY,
    IdIglesia INT NOT NULL FOREIGN KEY REFERENCES dbo.Iglesias(IdIglesia),
    IdUsuario INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuarios(IdUsuario),
    Comentario NVARCHAR(MAX) NOT NULL,
    FechaCreacion DATETIME DEFAULT GETDATE()
);

GO

-- ============================================================================
-- PROCEDIMIENTOS ALMACENADOS ACTUALIZADOS
-- ============================================================================

-- Procedure: sp_RegistrarUsuario
IF OBJECT_ID('dbo.sp_RegistrarUsuario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistrarUsuario;
GO
CREATE PROCEDURE dbo.sp_RegistrarUsuario
    @Correo VARCHAR(100),
    @Clave VARCHAR(100),
    @Registrado BIT OUTPUT,
    @Mensaje VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = @Correo)
    BEGIN
        SET @Registrado = 0;
        SET @Mensaje = 'El correo ya se encuentra registrado.';
        RETURN;
    END

    INSERT INTO dbo.Usuarios (Correo, Clave, IdRolSeguridad, IdEstado)
    VALUES (@Correo, @Clave, 3, 1); -- Coordinador, PendienteAprobacionCorreo

    SET @Registrado = 1;
    SET @Mensaje = 'Usuario registrado con éxito. Su cuenta está pendiente de aprobación por un administrador.';
END;
GO

-- Procedure: sp_ValidarUsuario (Con JOIN a NivelesEquipo para obtener Nivel y RangoJerarquico exactos)
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
