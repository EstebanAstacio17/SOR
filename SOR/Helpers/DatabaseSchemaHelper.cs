using System;
using System.Configuration;
using System.Data.SqlClient;

namespace SOR.Helpers
{
    public static class DatabaseSchemaHelper
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        public static void AsegurarIntegridadYConcurrencia()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();

                    // 1. Columnas de Concurrencia Optimista y Auditoría
                    string sqlColumnas = @"
                        IF COL_LENGTH('dbo.Iglesias', 'RowVersion') IS NULL
                            ALTER TABLE dbo.Iglesias ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('dbo.Iglesias', 'FechaModificacion') IS NULL
                            ALTER TABLE dbo.Iglesias ADD FechaModificacion DATETIME2 NULL;

                        IF COL_LENGTH('dbo.Iglesias', 'UsuarioModificacion') IS NULL
                            ALTER TABLE dbo.Iglesias ADD UsuarioModificacion INT NULL;

                        IF COL_LENGTH('dbo.Equipos', 'RowVersion') IS NULL
                            ALTER TABLE dbo.Equipos ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('dbo.Equipos', 'FechaModificacion') IS NULL
                            ALTER TABLE dbo.Equipos ADD FechaModificacion DATETIME2 NULL;

                        IF COL_LENGTH('dbo.Equipos', 'UsuarioModificacion') IS NULL
                            ALTER TABLE dbo.Equipos ADD UsuarioModificacion INT NULL;

                        IF COL_LENGTH('dbo.Eventos', 'RowVersion') IS NULL
                            ALTER TABLE dbo.Eventos ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('dbo.Eventos', 'FechaModificacion') IS NULL
                            ALTER TABLE dbo.Eventos ADD FechaModificacion DATETIME2 NULL;

                        IF COL_LENGTH('dbo.Eventos', 'UsuarioModificacion') IS NULL
                            ALTER TABLE dbo.Eventos ADD UsuarioModificacion INT NULL;";

                    using (SqlCommand cmd = new SqlCommand(sqlColumnas, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Deduplicación preventiva antes de restricciones
                    string sqlDeduplicar = @"
                        WITH CTE_Part AS (
                            SELECT IdParticipacion, ROW_NUMBER() OVER(PARTITION BY IdIglesia, IdTemporada ORDER BY IdParticipacion DESC) AS rn
                            FROM dbo.ParticipacionesIglesia
                        )
                        DELETE FROM CTE_Part WHERE rn > 1;

                        WITH CTE_AsistM AS (
                            SELECT IdAsistencia, ROW_NUMBER() OVER(PARTITION BY IdEvento, IdMaestro ORDER BY IdAsistencia DESC) AS rn
                            FROM dbo.AsistenciaMaestro
                        )
                        DELETE FROM CTE_AsistM WHERE rn > 1;";

                    using (SqlCommand cmd = new SqlCommand(sqlDeduplicar, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Restricciones e Índices Únicos
                    string sqlUnicos = @"
                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Participaciones_Iglesia_Temporada')
                            CREATE UNIQUE NONCLUSTERED INDEX UQ_Participaciones_Iglesia_Temporada 
                            ON dbo.ParticipacionesIglesia (IdIglesia, IdTemporada);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_AsistenciaMaestro_Evento_Maestro')
                            CREATE UNIQUE NONCLUSTERED INDEX UQ_AsistenciaMaestro_Evento_Maestro 
                            ON dbo.AsistenciaMaestro (IdEvento, IdMaestro);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EventosAsistentes_Evento_Participacion_Doc')
                            CREATE UNIQUE NONCLUSTERED INDEX UQ_EventosAsistentes_Evento_Participacion_Doc
                            ON dbo.EventosAsistentes (IdEvento, IdParticipacion, Identificacion)
                            WHERE Identificacion IS NOT NULL AND Identificacion <> '';";

                    using (SqlCommand cmd = new SqlCommand(sqlUnicos, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Índices de Rendimiento
                    string sqlIndices = @"
                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Iglesias_IdEquipo')
                            CREATE NONCLUSTERED INDEX IX_Iglesias_IdEquipo ON dbo.Iglesias (IdEquipo)
                            INCLUDE (NombreIglesia, RNC_Cedula, Telefono, Denominacion, TipoOrganizacion);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Iglesias_RNC_Cedula')
                            CREATE NONCLUSTERED INDEX IX_Iglesias_RNC_Cedula ON dbo.Iglesias (RNC_Cedula)
                            WHERE RNC_Cedula IS NOT NULL;

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Participaciones_IdTemporada')
                            CREATE NONCLUSTERED INDEX IX_Participaciones_IdTemporada ON dbo.ParticipacionesIglesia (IdTemporada)
                            INCLUDE (IdIglesia, EstadoEvaluacion, EstatusEvaluacionReporte, EtapaActual);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PersonasIglesia_IdIglesia_Tipo')
                            CREATE NONCLUSTERED INDEX IX_PersonasIglesia_IdIglesia_Tipo ON dbo.PersonasIglesia (IdIglesia, TipoPersona)
                            INCLUDE (Nombres, Apellidos, DocumentoIdentidad, Celular, Correo);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Maestros_IdIglesia_Activo')
                            CREATE NONCLUSTERED INDEX IX_Maestros_IdIglesia_Activo ON dbo.Maestros (IdIglesia, Activo)
                            INCLUDE (Nombres, Apellidos, DocumentoIdentidad, Celular, Correo);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_IdTemporada_Fecha')
                            CREATE NONCLUSTERED INDEX IX_Eventos_IdTemporada_Fecha ON dbo.Eventos (IdTemporada, Fecha DESC)
                            INCLUDE (NombreEvento, TipoEvento, Lugar, Responsable, CantidadAsistentes);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventosAsistentes_IdEvento')
                            CREATE NONCLUSTERED INDEX IX_EventosAsistentes_IdEvento ON dbo.EventosAsistentes (IdEvento, IdParticipacion)
                            INCLUDE (NombreCompleto, Identificacion, Telefono, Correo);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AsignacionesRecursos_IdPart')
                            CREATE NONCLUSTERED INDEX IX_AsignacionesRecursos_IdPart ON dbo.AsignacionesRecursos (IdParticipacion);

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comentarios_IdIglesia')
                            CREATE NONCLUSTERED INDEX IX_Comentarios_IdIglesia ON dbo.ComentariosObservaciones (IdIglesia, FechaCreacion DESC);";

                    using (SqlCommand cmd = new SqlCommand(sqlIndices, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 5. Tabla Central de Auditoría
                    string sqlAuditoria = @"
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
                        END";

                    using (SqlCommand cmd = new SqlCommand(sqlAuditoria, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 6. Tabla de Excepciones a la Regla de 3 Años (Aprobación CE + CMI)
                    string sqlExcepciones = @"
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
                        END";

                    using (SqlCommand cmd = new SqlCommand(sqlExcepciones, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 7. Índice Único Filtrado: Máximo 1 Superadmin Activo simultáneamente en toda la plataforma
                    string sqlUnicoSuperAdmin = @"
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.indexes 
                            WHERE name = 'UQ_Usuarios_UnicoSuperAdminActivo' AND object_id = OBJECT_ID('dbo.Usuarios')
                        )
                        BEGIN
                            CREATE UNIQUE NONCLUSTERED INDEX UQ_Usuarios_UnicoSuperAdminActivo
                            ON dbo.Usuarios(IdRolSeguridad)
                            WHERE IdRolSeguridad = 1 AND IdEstado = 4;
                        END";

                    using (SqlCommand cmd = new SqlCommand(sqlUnicoSuperAdmin, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Registramos o ignoramos si ya existe para no detener el arranque
                System.Diagnostics.Debug.WriteLine("Error al asegurar esquema: " + ex.Message);
            }
        }
    }
}
