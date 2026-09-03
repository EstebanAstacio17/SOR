using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using SOR.Models.ERLE;

namespace SOR.DAL.ERLE
{
    public class ErleFinanzasRepository
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            if (ConfigurationManager.ConnectionStrings["DefaultConnection"] != null)
            {
                return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        public void AsegurarEsquema()
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();

                string ddlTablas = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERLE_Temporadas')
                BEGIN
                    CREATE TABLE dbo.ERLE_Temporadas (
                        TemporadaId INT IDENTITY(1,1) PRIMARY KEY,
                        Nombre VARCHAR(50) NOT NULL UNIQUE,
                        TasaCambioReferencia DECIMAL(10,4) NOT NULL DEFAULT 58.63,
                        Activa BIT NOT NULL DEFAULT 1
                    );
                    INSERT INTO dbo.ERLE_Temporadas (Nombre, TasaCambioReferencia, Activa) 
                    VALUES ('2026 - 2027', 58.6300, 1);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERLE_Equipos')
                BEGIN
                    CREATE TABLE dbo.ERLE_Equipos (
                        EquipoId INT IDENTITY(1,1) PRIMARY KEY,
                        Codigo VARCHAR(50) NOT NULL UNIQUE,
                        Nombre NVARCHAR(150) NOT NULL,
                        EsENL BIT NOT NULL DEFAULT 0,
                        Activo BIT NOT NULL DEFAULT 1
                    );
                    INSERT INTO dbo.ERLE_Equipos (Codigo, Nombre, EsENL) 
                    VALUES ('DN-ERLE', 'Distrito Nacional - ERLE', 1);
                END;

                -- Sincronizar automáticamente equipos de la plataforma
                IF OBJECT_ID('dbo.Equipos', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO dbo.ERLE_Equipos (Codigo, Nombre, EsENL, Activo)
                    SELECT 
                        'EQ-' + CAST(e.IdEquipo AS VARCHAR(10)),
                        e.NombreEquipo,
                        CASE WHEN e.NombreEquipo LIKE '%Nacional%' OR e.NombreEquipo LIKE '%ENL%' THEN 1 ELSE 0 END,
                        e.Activo
                    FROM dbo.Equipos e
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.ERLE_Equipos x WHERE x.Nombre = e.NombreEquipo OR x.Codigo = 'EQ-' + CAST(e.IdEquipo AS VARCHAR(10)));
                END;

                -- Sincronizar automáticamente temporadas de la plataforma
                IF OBJECT_ID('dbo.Temporadas', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO dbo.ERLE_Temporadas (Nombre, TasaCambioReferencia, Activa)
                    SELECT 
                        t.NombreTemporada,
                        58.6300,
                        t.Activa
                    FROM dbo.Temporadas t
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.ERLE_Temporadas x WHERE x.Nombre = t.NombreTemporada);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERLE_Categorias')
                BEGIN
                    CREATE TABLE dbo.ERLE_Categorias (
                        CategoriaId VARCHAR(10) PRIMARY KEY,
                        Tipo VARCHAR(10) NOT NULL CHECK (Tipo IN ('INGRESO', 'GASTO')),
                        Grupo VARCHAR(50) NOT NULL,
                        Descripcion NVARCHAR(150) NOT NULL,
                        Orden INT NOT NULL DEFAULT 0
                    );
                    INSERT INTO dbo.ERLE_Categorias (CategoriaId, Tipo, Grupo, Descripcion, Orden) VALUES
                    ('I-1', 'INGRESO', 'INGRESOS', 'Subvención - Entrenamientos', 1),
                    ('I-2', 'INGRESO', 'INGRESOS', 'Subvención - Mentoreo', 2),
                    ('I-3', 'INGRESO', 'INGRESOS', 'Ingresos para Logística', 3),
                    ('I-4', 'INGRESO', 'INGRESOS', 'Otros Ingresos', 4),
                    ('E-0', 'GASTO', 'ENTRENAMIENTO', 'Envío, Retiro o Transferencia para Entrenamientos', 5),
                    ('E-1', 'GASTO', 'ENTRENAMIENTO', 'Transporte', 6),
                    ('E-2', 'GASTO', 'ENTRENAMIENTO', 'Snacks o Refrigerios', 7),
                    ('E-3', 'GASTO', 'ENTRENAMIENTO', 'Alimento', 8),
                    ('E-4', 'GASTO', 'ENTRENAMIENTO', 'Administración y Otros Gastos de Oficina', 9),
                    ('M-0', 'GASTO', 'MENTOREO', 'Envío, Retiro o Transferencia para Mentoreo', 10),
                    ('M-1', 'GASTO', 'MENTOREO', 'Transporte', 11),
                    ('M-2', 'GASTO', 'MENTOREO', 'Alimento', 12),
                    ('M-3', 'GASTO', 'MENTOREO', 'Hospedaje', 13),
                    ('M-4', 'GASTO', 'MENTOREO', 'Administración y Otros Gastos de Oficina', 14),
                    ('L-1', 'GASTO', 'LOGISTICA', 'Transporte de Cajitas y Literatura', 15),
                    ('L-2', 'GASTO', 'LOGISTICA', 'Almacenaje de Cajitas y Literatura', 16),
                    ('L-3', 'GASTO', 'LOGISTICA', 'Otros Gastos de Logística', 17),
                    ('O-1', 'GASTO', 'OTROS', 'Otros eventos o gastos aprobados', 18);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERLE_PresupuestosAprobados')
                BEGIN
                    CREATE TABLE dbo.ERLE_PresupuestosAprobados (
                        PresupuestoId INT IDENTITY(1,1) PRIMARY KEY,
                        TemporadaId INT NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Temporadas(TemporadaId),
                        EquipoId INT NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Equipos(EquipoId),
                        CategoriaId VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Categorias(CategoriaId),
                        MontoAprobadoUSD DECIMAL(18,2) NOT NULL DEFAULT 0,
                        MontoAprobadoDOP AS (CAST(ROUND(MontoAprobadoUSD * 58.63, 2) AS DECIMAL(18,2))),
                        CONSTRAINT UQ_ERLE_Presupuesto UNIQUE (TemporadaId, EquipoId, CategoriaId)
                    );

                    INSERT INTO dbo.ERLE_PresupuestosAprobados (TemporadaId, EquipoId, CategoriaId, MontoAprobadoUSD) VALUES
                    (1, 1, 'I-1', 3685.00),
                    (1, 1, 'I-2', 1200.00),
                    (1, 1, 'E-1', 546.00),
                    (1, 1, 'E-2', 298.00),
                    (1, 1, 'E-3', 2148.00),
                    (1, 1, 'E-4', 693.00),
                    (1, 1, 'M-1', 140.00),
                    (1, 1, 'M-2', 160.00),
                    (1, 1, 'M-3', 150.00),
                    (1, 1, 'M-4', 750.00);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERLE_Transacciones')
                BEGIN
                    CREATE TABLE dbo.ERLE_Transacciones (
                        TransaccionId BIGINT IDENTITY(1,1) PRIMARY KEY,
                        TemporadaId INT NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Temporadas(TemporadaId),
                        EquipoId INT NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Equipos(EquipoId),
                        Mes VARCHAR(3) NOT NULL CHECK (Mes IN ('SEP','OCT','NOV','DIC','ENE','FEB','MAR','ABR','MAY','JUN','JUL','AGO')),
                        Fecha DATE NOT NULL,
                        NumeroDocumento NVARCHAR(50) NULL,
                        Descripcion NVARCHAR(255) NOT NULL,
                        CategoriaId VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES dbo.ERLE_Categorias(CategoriaId),
                        GastoDOP DECIMAL(18,2) NOT NULL DEFAULT 0,
                        IngresoDOP DECIMAL(18,2) NOT NULL DEFAULT 0,
                        TasaCambio DECIMAL(10,4) NOT NULL DEFAULT 58.63,
                        GastoUSD AS (CAST(ROUND(GastoDOP / NULLIF(TasaCambio, 0), 2) AS DECIMAL(18,2))),
                        IngresoUSD AS (CAST(ROUND(IngresoDOP / NULLIF(TasaCambio, 0), 2) AS DECIMAL(18,2))),
                        Notas NVARCHAR(255) NULL,
                        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END;";

                using (var cmd = new SqlCommand(ddlTablas, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Procedimientos Almacenados
                string sp1 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_ERLE_ObtenerTransaccionesMes
                    @TemporadaId INT,
                    @EquipoId INT,
                    @Mes VARCHAR(3)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        t.TransaccionId,
                        t.Fecha,
                        t.NumeroDocumento,
                        t.Descripcion,
                        t.CategoriaId,
                        c.Descripcion AS CategoriaDescripcion,
                        t.GastoDOP,
                        t.GastoUSD,
                        t.IngresoDOP,
                        t.IngresoUSD,
                        t.TasaCambio,
                        t.Notas
                    FROM dbo.ERLE_Transacciones t
                    INNER JOIN dbo.ERLE_Categorias c ON t.CategoriaId = c.CategoriaId
                    WHERE t.TemporadaId = @TemporadaId 
                      AND t.EquipoId = @EquipoId 
                      AND t.Mes = @Mes
                    ORDER BY t.Fecha ASC, t.TransaccionId ASC;
                END;";
                using (var cmd = new SqlCommand(sp1, conn)) { cmd.ExecuteNonQuery(); }

                string sp2 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_ERLE_GuardarTransaccion
                    @TransaccionId BIGINT OUTPUT,
                    @TemporadaId INT,
                    @EquipoId INT,
                    @Mes VARCHAR(3),
                    @Fecha DATE,
                    @NumeroDocumento NVARCHAR(50),
                    @Descripcion NVARCHAR(255),
                    @CategoriaId VARCHAR(10),
                    @GastoDOP DECIMAL(18,2),
                    @IngresoDOP DECIMAL(18,2),
                    @TasaCambio DECIMAL(10,4),
                    @Notas NVARCHAR(255)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF @TransaccionId IS NULL OR @TransaccionId = 0
                    BEGIN
                        INSERT INTO dbo.ERLE_Transacciones 
                            (TemporadaId, EquipoId, Mes, Fecha, NumeroDocumento, Descripcion, CategoriaId, GastoDOP, IngresoDOP, TasaCambio, Notas)
                        VALUES 
                            (@TemporadaId, @EquipoId, @Mes, @Fecha, @NumeroDocumento, @Descripcion, @CategoriaId, @GastoDOP, @IngresoDOP, @TasaCambio, @Notas);
                        SET @TransaccionId = SCOPE_IDENTITY();
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.ERLE_Transacciones
                        SET Fecha = @Fecha,
                            NumeroDocumento = @NumeroDocumento,
                            Descripcion = @Descripcion,
                            CategoriaId = @CategoriaId,
                            GastoDOP = @GastoDOP,
                            IngresoDOP = @IngresoDOP,
                            TasaCambio = @TasaCambio,
                            Notas = @Notas
                        WHERE TransaccionId = @TransaccionId;
                    END
                END;";
                using (var cmd = new SqlCommand(sp2, conn)) { cmd.ExecuteNonQuery(); }

                string sp3 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_ERLE_EliminarTransaccion
                    @TransaccionId BIGINT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM dbo.ERLE_Transacciones WHERE TransaccionId = @TransaccionId;
                END;";
                using (var cmd = new SqlCommand(sp3, conn)) { cmd.ExecuteNonQuery(); }

                string sp4 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_ERLE_ObtenerReportePresupuestoVsReal
                    @TemporadaId INT,
                    @EquipoId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        c.Grupo,
                        c.CategoriaId,
                        c.Descripcion,
                        ISNULL(p.MontoAprobadoUSD, 0) AS PresupuestoAprobadoUSD,
                        ISNULL(p.MontoAprobadoDOP, 0) AS PresupuestoAprobadoDOP,
                        ISNULL(SUM(CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoUSD ELSE t.GastoUSD END), 0) AS EjecutadoUSD,
                        ISNULL(SUM(CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END), 0) AS EjecutadoDOP,
                        (ISNULL(p.MontoAprobadoUSD, 0) - ISNULL(SUM(CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoUSD ELSE t.GastoUSD END), 0)) AS RemanenteUSD,
                        (ISNULL(p.MontoAprobadoDOP, 0) - ISNULL(SUM(CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END), 0)) AS RemanenteDOP
                    FROM dbo.ERLE_Categorias c
                    LEFT JOIN dbo.ERLE_PresupuestosAprobados p 
                        ON c.CategoriaId = p.CategoriaId AND p.TemporadaId = @TemporadaId AND p.EquipoId = @EquipoId
                    LEFT JOIN dbo.ERLE_Transacciones t 
                        ON c.CategoriaId = t.CategoriaId AND t.TemporadaId = @TemporadaId AND t.EquipoId = @EquipoId
                    GROUP BY c.Grupo, c.CategoriaId, c.Descripcion, c.Orden, c.Tipo, p.MontoAprobadoUSD, p.MontoAprobadoDOP
                    ORDER BY c.Orden;
                END;";
                using (var cmd = new SqlCommand(sp4, conn)) { cmd.ExecuteNonQuery(); }

                string sp5 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_ERLE_ObtenerReporteConsolidado
                    @TemporadaId INT,
                    @EquipoId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        c.Grupo,
                        c.CategoriaId,
                        c.Descripcion,
                        c.Tipo,
                        ISNULL(SUM(CASE WHEN t.Mes = 'SEP' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS SEP,
                        ISNULL(SUM(CASE WHEN t.Mes = 'OCT' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS OCT,
                        ISNULL(SUM(CASE WHEN t.Mes = 'NOV' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS NOV,
                        ISNULL(SUM(CASE WHEN t.Mes = 'DIC' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS DIC,
                        ISNULL(SUM(CASE WHEN t.Mes = 'ENE' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS ENE,
                        ISNULL(SUM(CASE WHEN t.Mes = 'FEB' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS FEB,
                        ISNULL(SUM(CASE WHEN t.Mes = 'MAR' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS MAR,
                        ISNULL(SUM(CASE WHEN t.Mes = 'ABR' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS ABR,
                        ISNULL(SUM(CASE WHEN t.Mes = 'MAY' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS MAY,
                        ISNULL(SUM(CASE WHEN t.Mes = 'JUN' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS JUN,
                        ISNULL(SUM(CASE WHEN t.Mes = 'JUL' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS JUL,
                        ISNULL(SUM(CASE WHEN t.Mes = 'AGO' THEN (CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END) ELSE 0 END), 0) AS AGO,
                        ISNULL(SUM(CASE WHEN c.Tipo = 'INGRESO' THEN t.IngresoDOP ELSE t.GastoDOP END), 0) AS TotalDOP
                    FROM dbo.ERLE_Categorias c
                    LEFT JOIN dbo.ERLE_Transacciones t 
                        ON c.CategoriaId = t.CategoriaId 
                       AND t.TemporadaId = @TemporadaId 
                       AND (@EquipoId IS NULL OR t.EquipoId = @EquipoId)
                    GROUP BY c.Grupo, c.CategoriaId, c.Descripcion, c.Orden, c.Tipo
                    ORDER BY c.Orden;
                END;";
                using (var cmd = new SqlCommand(sp5, conn)) { cmd.ExecuteNonQuery(); }
            }
        }

        public List<SelectListItem> ObtenerListaEquipos()
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT EquipoId, Nombre FROM dbo.ERLE_Equipos WHERE Activo = 1 ORDER BY Nombre;";
                using (var cmd = new SqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = dr["EquipoId"].ToString(),
                            Text = dr["Nombre"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public List<SelectListItem> ObtenerListaTemporadas()
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT TemporadaId, Nombre FROM dbo.ERLE_Temporadas ORDER BY Activa DESC, TemporadaId DESC;";
                using (var cmd = new SqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = dr["TemporadaId"].ToString(),
                            Text = dr["Nombre"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public string ObtenerNombreEquipo(int equipoId)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT Nombre FROM dbo.ERLE_Equipos WHERE EquipoId = @Id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", equipoId);
                    object val = cmd.ExecuteScalar();
                    return val != null ? val.ToString() : "Equipo " + equipoId;
                }
            }
        }

        public string ObtenerNombreTemporada(int temporadaId)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT Nombre FROM dbo.ERLE_Temporadas WHERE TemporadaId = @Id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", temporadaId);
                    object val = cmd.ExecuteScalar();
                    return val != null ? val.ToString() : "Temporada " + temporadaId;
                }
            }
        }

        public decimal CalcularSaldoInicialMes(int temporadaId, int equipoId, string mesActual)
        {
            string[] meses = new[] { "SEP", "OCT", "NOV", "DIC", "ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO" };
            int idxActual = Array.IndexOf(meses, mesActual.ToUpper());
            if (idxActual <= 0) return 0m;

            var mesesPrevios = new List<string>();
            for (int i = 0; i < idxActual; i++)
            {
                mesesPrevios.Add("'" + meses[i] + "'");
            }

            string inClause = string.Join(",", mesesPrevios);

            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = $@"
                    SELECT ISNULL(SUM(IngresoDOP - GastoDOP), 0)
                    FROM dbo.ERLE_Transacciones
                    WHERE TemporadaId = @TemporadaId AND EquipoId = @EquipoId AND Mes IN ({inClause});";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                    cmd.Parameters.AddWithValue("@EquipoId", equipoId);
                    object res = cmd.ExecuteScalar();
                    return res != null ? Convert.ToDecimal(res) : 0m;
                }
            }
        }

        public List<ErleTransaccionDTO> ObtenerTransaccionesMes(int temporadaId, int equipoId, string mes, decimal saldoInicialDOP)
        {
            var lista = new List<ErleTransaccionDTO>();
            decimal saldoAcum = saldoInicialDOP;

            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_ERLE_ObtenerTransaccionesMes", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@TemporadaId", SqlDbType.Int) { Value = temporadaId });
                cmd.Parameters.Add(new SqlParameter("@EquipoId", SqlDbType.Int) { Value = equipoId });
                cmd.Parameters.Add(new SqlParameter("@Mes", SqlDbType.VarChar, 3) { Value = mes });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var t = new ErleTransaccionDTO
                        {
                            TransaccionId = Convert.ToInt64(r["TransaccionId"]),
                            Fecha = Convert.ToDateTime(r["Fecha"]),
                            NumeroDocumento = r["NumeroDocumento"] != DBNull.Value ? r["NumeroDocumento"].ToString() : string.Empty,
                            Descripcion = r["Descripcion"].ToString(),
                            CategoriaId = r["CategoriaId"].ToString(),
                            CategoriaDescripcion = r["CategoriaDescripcion"].ToString(),
                            GastoDOP = Convert.ToDecimal(r["GastoDOP"]),
                            GastoUSD = Convert.ToDecimal(r["GastoUSD"]),
                            IngresoDOP = Convert.ToDecimal(r["IngresoDOP"]),
                            IngresoUSD = Convert.ToDecimal(r["IngresoUSD"]),
                            TasaCambio = Convert.ToDecimal(r["TasaCambio"]),
                            Notas = r["Notas"] != DBNull.Value ? r["Notas"].ToString() : string.Empty
                        };

                        saldoAcum = saldoAcum + t.IngresoDOP - t.GastoDOP;
                        t.SaldoDOP = saldoAcum;
                        t.SaldoUSD = t.TasaCambio > 0 ? Math.Round(saldoAcum / t.TasaCambio, 2) : 0m;
                        lista.Add(t);
                    }
                }
            }
            return lista;
        }

        public long GuardarTransaccion(ErleTransaccionDTO t)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_ERLE_GuardarTransaccion", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var pId = new SqlParameter("@TransaccionId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = t.TransaccionId == 0 ? (object)DBNull.Value : t.TransaccionId
                };
                cmd.Parameters.Add(pId);
                cmd.Parameters.Add(new SqlParameter("@TemporadaId", SqlDbType.Int) { Value = t.TemporadaId });
                cmd.Parameters.Add(new SqlParameter("@EquipoId", SqlDbType.Int) { Value = t.EquipoId });
                cmd.Parameters.Add(new SqlParameter("@Mes", SqlDbType.VarChar, 3) { Value = t.Mes });
                cmd.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.Date) { Value = t.Fecha });
                cmd.Parameters.Add(new SqlParameter("@NumeroDocumento", SqlDbType.NVarChar, 50) { Value = (object)t.NumeroDocumento ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Descripcion", SqlDbType.NVarChar, 255) { Value = t.Descripcion });
                cmd.Parameters.Add(new SqlParameter("@CategoriaId", SqlDbType.VarChar, 10) { Value = t.CategoriaId });
                cmd.Parameters.Add(new SqlParameter("@GastoDOP", SqlDbType.Decimal) { Value = t.GastoDOP, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new SqlParameter("@IngresoDOP", SqlDbType.Decimal) { Value = t.IngresoDOP, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new SqlParameter("@TasaCambio", SqlDbType.Decimal) { Value = t.TasaCambio, Precision = 10, Scale = 4 });
                cmd.Parameters.Add(new SqlParameter("@Notas", SqlDbType.NVarChar, 255) { Value = (object)t.Notas ?? DBNull.Value });

                conn.Open();
                cmd.ExecuteNonQuery();
                return Convert.ToInt64(pId.Value);
            }
        }

        public bool EliminarTransaccion(long id)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_ERLE_EliminarTransaccion", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@TransaccionId", SqlDbType.BigInt) { Value = id });
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<ErlePresupuestoVsRealDTO> ObtenerPresupuestoVsReal(int temporadaId, int equipoId)
        {
            var list = new List<ErlePresupuestoVsRealDTO>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_ERLE_ObtenerReportePresupuestoVsReal", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@TemporadaId", SqlDbType.Int) { Value = temporadaId });
                cmd.Parameters.Add(new SqlParameter("@EquipoId", SqlDbType.Int) { Value = equipoId });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ErlePresupuestoVsRealDTO
                        {
                            Grupo = r["Grupo"].ToString(),
                            CategoriaId = r["CategoriaId"].ToString(),
                            Descripcion = r["Descripcion"].ToString(),
                            PresupuestoAprobadoUSD = Convert.ToDecimal(r["PresupuestoAprobadoUSD"]),
                            PresupuestoAprobadoDOP = Convert.ToDecimal(r["PresupuestoAprobadoDOP"]),
                            EjecutadoUSD = Convert.ToDecimal(r["EjecutadoUSD"]),
                            EjecutadoDOP = Convert.ToDecimal(r["EjecutadoDOP"]),
                            RemanenteUSD = Convert.ToDecimal(r["RemanenteUSD"]),
                            RemanenteDOP = Convert.ToDecimal(r["RemanenteDOP"])
                        });
                    }
                }
            }
            return list;
        }

        public List<ErleOpcionCategoriaDTO> ObtenerCategorias()
        {
            var list = new List<ErleOpcionCategoriaDTO>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("SELECT CategoriaId, Descripcion, Tipo FROM dbo.ERLE_Categorias ORDER BY Orden", conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ErleOpcionCategoriaDTO
                        {
                            Value = r["CategoriaId"].ToString(),
                            Text = $"{r["CategoriaId"]} - {r["Descripcion"]}",
                            Tipo = r["Tipo"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public List<ErleReporteConsolidadoFila> ObtenerReporteConsolidado(int temporadaId, int? equipoId)
        {
            var list = new List<ErleReporteConsolidadoFila>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_ERLE_ObtenerReporteConsolidado", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@TemporadaId", SqlDbType.Int) { Value = temporadaId });
                cmd.Parameters.Add(new SqlParameter("@EquipoId", SqlDbType.Int) { Value = (object)equipoId ?? DBNull.Value });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ErleReporteConsolidadoFila
                        {
                            Grupo = r["Grupo"].ToString(),
                            CategoriaId = r["CategoriaId"].ToString(),
                            Descripcion = r["Descripcion"].ToString(),
                            Tipo = r["Tipo"].ToString(),
                            SEP = Convert.ToDecimal(r["SEP"]),
                            OCT = Convert.ToDecimal(r["OCT"]),
                            NOV = Convert.ToDecimal(r["NOV"]),
                            DIC = Convert.ToDecimal(r["DIC"]),
                            ENE = Convert.ToDecimal(r["ENE"]),
                            FEB = Convert.ToDecimal(r["FEB"]),
                            MAR = Convert.ToDecimal(r["MAR"]),
                            ABR = Convert.ToDecimal(r["ABR"]),
                            MAY = Convert.ToDecimal(r["MAY"]),
                            JUN = Convert.ToDecimal(r["JUN"]),
                            JUL = Convert.ToDecimal(r["JUL"]),
                            AGO = Convert.ToDecimal(r["AGO"]),
                            TotalDOP = Convert.ToDecimal(r["TotalDOP"])
                        });
                    }
                }
            }
            return list;
        }

        public bool GuardarPresupuestoAprobado(int temporadaId, int equipoId, List<ErlePresupuestoItemDTO> items)
        {
            if (items == null || items.Count == 0) return false;

            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in items)
                        {
                            string sql = @"
                                IF EXISTS (SELECT 1 FROM dbo.ERLE_PresupuestosAprobados WHERE TemporadaId = @TemporadaId AND EquipoId = @EquipoId AND CategoriaId = @CatId)
                                BEGIN
                                    UPDATE dbo.ERLE_PresupuestosAprobados
                                    SET MontoAprobadoUSD = @Monto
                                    WHERE TemporadaId = @TemporadaId AND EquipoId = @EquipoId AND CategoriaId = @CatId;
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO dbo.ERLE_PresupuestosAprobados (TemporadaId, EquipoId, CategoriaId, MontoAprobadoUSD)
                                    VALUES (@TemporadaId, @EquipoId, @CatId, @Monto);
                                END";

                            using (var cmd = new SqlCommand(sql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@TemporadaId", temporadaId);
                                cmd.Parameters.AddWithValue("@EquipoId", equipoId);
                                cmd.Parameters.AddWithValue("@CatId", item.CategoriaId);
                                cmd.Parameters.AddWithValue("@Monto", item.MontoAprobadoUSD);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
