using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using SOR.Models;

namespace SOR.Repositories
{
    public class FinanzasRepository : BaseRepository
    {
        public void AsegurarEsquema()
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();

                string ddl = @"
                -- Categorías Financieras Universales
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Finanzas_Categorias')
                BEGIN
                    CREATE TABLE dbo.Finanzas_Categorias (
                        CategoriaId VARCHAR(10) PRIMARY KEY,
                        Tipo VARCHAR(10) NOT NULL CHECK (Tipo IN ('INGRESO', 'GASTO')),
                        Grupo VARCHAR(50) NOT NULL,
                        Descripcion NVARCHAR(150) NOT NULL,
                        Orden INT NOT NULL DEFAULT 0
                    );

                    INSERT INTO dbo.Finanzas_Categorias (CategoriaId, Tipo, Grupo, Descripcion, Orden) VALUES
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

                -- Techos Presupuestarios por Equipo y Temporada
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Finanzas_PresupuestosAprobados')
                BEGIN
                    CREATE TABLE dbo.Finanzas_PresupuestosAprobados (
                        PresupuestoId INT IDENTITY(1,1) PRIMARY KEY,
                        IdTemporada INT NOT NULL,
                        IdEquipo INT NOT NULL,
                        CategoriaId VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES dbo.Finanzas_Categorias(CategoriaId),
                        MontoAprobadoUSD DECIMAL(18,2) NOT NULL DEFAULT 0,
                        MontoAprobadoDOP AS (CAST(ROUND(MontoAprobadoUSD * 58.63, 2) AS DECIMAL(18,2))),
                        CONSTRAINT UQ_Finanzas_Presupuesto UNIQUE (IdTemporada, IdEquipo, CategoriaId)
                    );
                END;

                -- Libro Diario de Transacciones por Equipo y Temporada
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Finanzas_Transacciones')
                BEGIN
                    CREATE TABLE dbo.Finanzas_Transacciones (
                        TransaccionId BIGINT IDENTITY(1,1) PRIMARY KEY,
                        IdTemporada INT NOT NULL,
                        IdEquipo INT NOT NULL,
                        Mes VARCHAR(3) NOT NULL CHECK (Mes IN ('SEP','OCT','NOV','DIC','ENE','FEB','MAR','ABR','MAY','JUN','JUL','AGO')),
                        Fecha DATE NOT NULL,
                        NumeroDocumento NVARCHAR(50) NULL,
                        Descripcion NVARCHAR(255) NOT NULL,
                        CategoriaId VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES dbo.Finanzas_Categorias(CategoriaId),
                        GastoDOP DECIMAL(18,2) NOT NULL DEFAULT 0,
                        IngresoDOP DECIMAL(18,2) NOT NULL DEFAULT 0,
                        TasaCambio DECIMAL(10,4) NOT NULL DEFAULT 58.63,
                        GastoUSD AS (CAST(ROUND(GastoDOP / NULLIF(TasaCambio, 0), 2) AS DECIMAL(18,2))),
                        IngresoUSD AS (CAST(ROUND(IngresoDOP / NULLIF(TasaCambio, 0), 2) AS DECIMAL(18,2))),
                        Notas NVARCHAR(255) NULL,
                        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END;";

                using (var cmd = new SqlCommand(ddl, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Procedimientos Almacenados Genéricos
                string sp1 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_Finanzas_ObtenerTransaccionesMes
                    @IdTemporada INT,
                    @IdEquipo INT,
                    @Mes VARCHAR(3)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        t.TransaccionId,
                        t.IdTemporada,
                        t.IdEquipo,
                        t.Mes,
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
                    FROM dbo.Finanzas_Transacciones t
                    INNER JOIN dbo.Finanzas_Categorias c ON t.CategoriaId = c.CategoriaId
                    WHERE t.IdTemporada = @IdTemporada 
                      AND t.IdEquipo = @IdEquipo 
                      AND t.Mes = @Mes
                    ORDER BY t.Fecha ASC, t.TransaccionId ASC;
                END;";
                using (var cmd = new SqlCommand(sp1, conn)) { cmd.ExecuteNonQuery(); }

                string sp2 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_Finanzas_GuardarTransaccion
                    @TransaccionId BIGINT OUTPUT,
                    @IdTemporada INT,
                    @IdEquipo INT,
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
                        INSERT INTO dbo.Finanzas_Transacciones 
                            (IdTemporada, IdEquipo, Mes, Fecha, NumeroDocumento, Descripcion, CategoriaId, GastoDOP, IngresoDOP, TasaCambio, Notas)
                        VALUES 
                            (@IdTemporada, @IdEquipo, @Mes, @Fecha, @NumeroDocumento, @Descripcion, @CategoriaId, @GastoDOP, @IngresoDOP, @TasaCambio, @Notas);
                        SET @TransaccionId = SCOPE_IDENTITY();
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.Finanzas_Transacciones
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
                CREATE OR ALTER PROCEDURE dbo.usp_Finanzas_EliminarTransaccion
                    @TransaccionId BIGINT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM dbo.Finanzas_Transacciones WHERE TransaccionId = @TransaccionId;
                END;";
                using (var cmd = new SqlCommand(sp3, conn)) { cmd.ExecuteNonQuery(); }

                string sp4 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_Finanzas_ObtenerReportePresupuestoVsReal
                    @IdTemporada INT,
                    @IdEquipo INT
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
                    FROM dbo.Finanzas_Categorias c
                    LEFT JOIN dbo.Finanzas_PresupuestosAprobados p 
                        ON c.CategoriaId = p.CategoriaId AND p.IdTemporada = @IdTemporada AND p.IdEquipo = @IdEquipo
                    LEFT JOIN dbo.Finanzas_Transacciones t 
                        ON c.CategoriaId = t.CategoriaId AND t.IdTemporada = @IdTemporada AND t.IdEquipo = @IdEquipo
                    GROUP BY c.Grupo, c.CategoriaId, c.Descripcion, c.Orden, c.Tipo, p.MontoAprobadoUSD, p.MontoAprobadoDOP
                    ORDER BY c.Orden;
                END;";
                using (var cmd = new SqlCommand(sp4, conn)) { cmd.ExecuteNonQuery(); }

                string sp5 = @"
                CREATE OR ALTER PROCEDURE dbo.usp_Finanzas_ObtenerReporteConsolidado
                    @IdTemporada INT,
                    @IdEquipo INT = NULL
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
                    FROM dbo.Finanzas_Categorias c
                    LEFT JOIN dbo.Finanzas_Transacciones t 
                        ON c.CategoriaId = t.CategoriaId 
                       AND t.IdTemporada = @IdTemporada 
                       AND (@IdEquipo IS NULL OR t.IdEquipo = @IdEquipo)
                    GROUP BY c.Grupo, c.CategoriaId, c.Descripcion, c.Orden, c.Tipo
                    ORDER BY c.Orden;
                END;";
                using (var cmd = new SqlCommand(sp5, conn)) { cmd.ExecuteNonQuery(); }
            }
        }

        public HashSet<int> ObtenerEquiposPermitidosJerarquico(Usuario u)
        {
            HashSet<int> set = new HashSet<int>();
            if (u == null) return set;

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            if (esAdmin)
            {
                return null; // Null indica acceso irrestricto a todos los equipos
            }

            if (u.IdEquipo.HasValue && u.IdEquipo.Value > 0)
            {
                set.Add(u.IdEquipo.Value);
                ObtenerEquiposHijosRecursivo(u.IdEquipo.Value, set);
            }

            return set;
        }

        public void ObtenerEquiposHijosRecursivo(int idEquipoPadre, HashSet<int> set)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT IdEquipo FROM dbo.Equipos WHERE IdEquipoPadre = @Id AND Activo = 1;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEquipoPadre);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int hId = Convert.ToInt32(dr["IdEquipo"]);
                            if (!set.Contains(hId))
                            {
                                set.Add(hId);
                                ObtenerEquiposHijosRecursivo(hId, set);
                            }
                        }
                    }
                }
            }
        }

        public List<SelectListItem> ObtenerListaEquipos(HashSet<int> equiposPermitidos = null)
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT IdEquipo, NombreEquipo FROM dbo.Equipos WHERE Activo = 1 ORDER BY NombreEquipo;";

                using (var cmd = new SqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int idEq = Convert.ToInt32(dr["IdEquipo"]);
                        if (equiposPermitidos == null || equiposPermitidos.Contains(idEq))
                        {
                            list.Add(new SelectListItem
                            {
                                Value = idEq.ToString(),
                                Text = dr["NombreEquipo"].ToString()
                            });
                        }
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
                string sql = "SELECT IdTemporada, NombreTemporada FROM dbo.Temporadas ORDER BY Activa DESC, IdTemporada DESC;";
                using (var cmd = new SqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = dr["IdTemporada"].ToString(),
                            Text = dr["NombreTemporada"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public string ObtenerNombreEquipo(int idEquipo)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT NombreEquipo FROM dbo.Equipos WHERE IdEquipo = @Id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEquipo);
                    object val = cmd.ExecuteScalar();
                    return val != null ? val.ToString() : "Equipo " + idEquipo;
                }
            }
        }

        public string ObtenerNombreTemporada(int idTemporada)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            {
                conn.Open();
                string sql = "SELECT NombreTemporada FROM dbo.Temporadas WHERE IdTemporada = @Id;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", idTemporada);
                    object val = cmd.ExecuteScalar();
                    return val != null ? val.ToString() : "Temporada " + idTemporada;
                }
            }
        }

        public decimal CalcularSaldoInicialMes(int idTemporada, int idEquipo, string mesActual)
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
                    FROM dbo.Finanzas_Transacciones
                    WHERE IdTemporada = @IdTemporada AND IdEquipo = @IdEquipo AND Mes IN ({inClause});";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);
                    cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
                    object res = cmd.ExecuteScalar();
                    return res != null ? Convert.ToDecimal(res) : 0m;
                }
            }
        }

        public List<TransaccionFinancieraDTO> ObtenerTransaccionesMes(int idTemporada, int idEquipo, string mes, decimal saldoInicialDOP)
        {
            var lista = new List<TransaccionFinancieraDTO>();
            decimal saldoAcum = saldoInicialDOP;

            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_Finanzas_ObtenerTransaccionesMes", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", SqlDbType.Int) { Value = idTemporada });
                cmd.Parameters.Add(new SqlParameter("@IdEquipo", SqlDbType.Int) { Value = idEquipo });
                cmd.Parameters.Add(new SqlParameter("@Mes", SqlDbType.VarChar, 3) { Value = mes });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var t = new TransaccionFinancieraDTO
                        {
                            TransaccionId = Convert.ToInt64(r["TransaccionId"]),
                            IdTemporada = Convert.ToInt32(r["IdTemporada"]),
                            IdEquipo = Convert.ToInt32(r["IdEquipo"]),
                            Mes = r["Mes"].ToString(),
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

        public long GuardarTransaccion(TransaccionFinancieraDTO t)
        {
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_Finanzas_GuardarTransaccion", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var pId = new SqlParameter("@TransaccionId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = t.TransaccionId == 0 ? (object)DBNull.Value : t.TransaccionId
                };
                cmd.Parameters.Add(pId);
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", SqlDbType.Int) { Value = t.IdTemporada });
                cmd.Parameters.Add(new SqlParameter("@IdEquipo", SqlDbType.Int) { Value = t.IdEquipo });
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
            using (var cmd = new SqlCommand("dbo.usp_Finanzas_EliminarTransaccion", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@TransaccionId", SqlDbType.BigInt) { Value = id });
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<PresupuestoVsRealDTO> ObtenerPresupuestoVsReal(int idTemporada, int idEquipo)
        {
            var list = new List<PresupuestoVsRealDTO>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_Finanzas_ObtenerReportePresupuestoVsReal", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", SqlDbType.Int) { Value = idTemporada });
                cmd.Parameters.Add(new SqlParameter("@IdEquipo", SqlDbType.Int) { Value = idEquipo });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new PresupuestoVsRealDTO
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

        public List<OpcionCategoriaFinancieraDTO> ObtenerCategorias()
        {
            var list = new List<OpcionCategoriaFinancieraDTO>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("SELECT CategoriaId, Descripcion, Tipo FROM dbo.Finanzas_Categorias ORDER BY Orden", conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new OpcionCategoriaFinancieraDTO
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

        public List<ReporteConsolidadoFila> ObtenerReporteConsolidado(int idTemporada, int? idEquipo)
        {
            var list = new List<ReporteConsolidadoFila>();
            using (var conn = new SqlConnection(ObtenerCadenaConexion()))
            using (var cmd = new SqlCommand("dbo.usp_Finanzas_ObtenerReporteConsolidado", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", SqlDbType.Int) { Value = idTemporada });
                cmd.Parameters.Add(new SqlParameter("@IdEquipo", SqlDbType.Int) { Value = (object)idEquipo ?? DBNull.Value });

                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ReporteConsolidadoFila
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

        public bool GuardarPresupuestoAprobado(int idTemporada, int idEquipo, List<PresupuestoItemDTO> items)
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
                                IF EXISTS (SELECT 1 FROM dbo.Finanzas_PresupuestosAprobados WHERE IdTemporada = @IdTemporada AND IdEquipo = @IdEquipo AND CategoriaId = @CatId)
                                BEGIN
                                    UPDATE dbo.Finanzas_PresupuestosAprobados
                                    SET MontoAprobadoUSD = @Monto
                                    WHERE IdTemporada = @IdTemporada AND IdEquipo = @IdEquipo AND CategoriaId = @CatId;
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO dbo.Finanzas_PresupuestosAprobados (IdTemporada, IdEquipo, CategoriaId, MontoAprobadoUSD)
                                    VALUES (@IdTemporada, @IdEquipo, @CatId, @Monto);
                                END";

                            using (var cmd = new SqlCommand(sql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);
                                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
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
