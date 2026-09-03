using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SOR.Models;
using SOR.Helpers;

namespace SOR.Repositories
{
    /// <summary>
    /// Repositorio de Logística — Inventario Central, Transferencias y Despacho.
    /// Todas las operaciones críticas usan SqlTransaction con IsolationLevel.ReadCommitted.
    /// </summary>
    public class LogisticaRepository
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // =====================================================================
        // MATERIALES Y PRESENTACIONES
        // =====================================================================

        public List<Material> ObtenerMateriales(bool soloActivos = true)
        {
            var lista = new List<Material>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT m.*, p.IdPresentacion, p.TipoEmpaque, p.UnidadesPorEmpaque,
                           p.IdTemporadaVigencia, p.FechaVigenciaInicio, p.Activo AS PActivo
                    FROM dbo.Materiales m
                    LEFT JOIN dbo.PresentacionesMaterial p ON m.IdMaterial = p.IdMaterial
                    WHERE (@SoloActivos = 0 OR m.Activo = 1)
                    ORDER BY m.IdMaterial, p.UnidadesPorEmpaque;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@SoloActivos", soloActivos ? 1 : 0);
                    using (var dr = cmd.ExecuteReader())
                    {
                        var dict = new Dictionary<int, Material>();
                        while (dr.Read())
                        {
                            int idMat = Convert.ToInt32(dr["IdMaterial"]);
                            if (!dict.ContainsKey(idMat))
                            {
                                dict[idMat] = new Material
                                {
                                    IdMaterial = idMat,
                                    Codigo = dr["Codigo"].ToString(),
                                    NombreMaterial = dr["NombreMaterial"].ToString(),
                                    UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                    MomentoEntrega = dr["MomentoEntrega"].ToString(),
                                    Activo = Convert.ToBoolean(dr["Activo"])
                                };
                                lista.Add(dict[idMat]);
                            }
                            if (dr["IdPresentacion"] != DBNull.Value)
                            {
                                dict[idMat].Presentaciones.Add(new PresentacionMaterial
                                {
                                    IdPresentacion = Convert.ToInt32(dr["IdPresentacion"]),
                                    IdMaterial = idMat,
                                    TipoEmpaque = dr["TipoEmpaque"].ToString(),
                                    UnidadesPorEmpaque = Convert.ToInt32(dr["UnidadesPorEmpaque"]),
                                    Activo = Convert.ToBoolean(dr["PActivo"])
                                });
                            }
                        }
                    }
                }
            }
            return lista;
        }

        public List<PresentacionMaterial> ObtenerPresentaciones(bool soloActivas = true, int? idTemporada = null)
        {
            var lista = new List<PresentacionMaterial>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT p.*, m.Codigo, m.NombreMaterial, t.NombreTemporada,
                           ISNULL((SELECT COUNT(1) FROM dbo.RecepcionesContenedorDetalle rcd WHERE rcd.IdPresentacion = p.IdPresentacion), 0) AS TotalMovimientos
                    FROM dbo.PresentacionesMaterial p
                    INNER JOIN dbo.Materiales m ON p.IdMaterial = m.IdMaterial
                    LEFT JOIN dbo.Temporadas t ON p.IdTemporadaVigencia = t.IdTemporada
                    WHERE (@SoloActivas = 0 OR p.Activo = 1)
                      AND (@IdTemp IS NULL OR @IdTemp = 0 OR p.IdTemporadaVigencia = @IdTemp OR p.IdTemporadaVigencia IS NULL)
                    ORDER BY ISNULL(t.FechaInicio, '1900-01-01') DESC, m.Codigo, p.UnidadesPorEmpaque;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@SoloActivas", soloActivas ? 1 : 0);
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue && idTemporada.Value > 0 ? (object)idTemporada.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new PresentacionMaterial
                            {
                                IdPresentacion = Convert.ToInt32(dr["IdPresentacion"]),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                TipoEmpaque = dr["TipoEmpaque"].ToString(),
                                UnidadesPorEmpaque = Convert.ToInt32(dr["UnidadesPorEmpaque"]),
                                IdTemporadaVigencia = dr["IdTemporadaVigencia"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdTemporadaVigencia"]) : null,
                                NombreTemporada = dr["NombreTemporada"] != DBNull.Value ? dr["NombreTemporada"].ToString() : "Global / Todas",
                                FechaVigenciaInicio = dr["FechaVigenciaInicio"] != DBNull.Value ? Convert.ToDateTime(dr["FechaVigenciaInicio"]) : DateTime.Now,
                                Activo = Convert.ToBoolean(dr["Activo"]),
                                TotalMovimientosRegistrados = dr["TotalMovimientos"] != DBNull.Value ? Convert.ToInt32(dr["TotalMovimientos"]) : 0
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public void GuardarPresentacion(PresentacionMaterial modelo)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                if (modelo.IdPresentacion == 0)
                {
                    string sql = @"
                        INSERT INTO dbo.PresentacionesMaterial 
                            (IdMaterial, TipoEmpaque, UnidadesPorEmpaque, IdTemporadaVigencia, FechaVigenciaInicio, Activo)
                        VALUES 
                            (@IdMat, @Tipo, @Uds, @IdTemp, GETDATE(), @Activo);";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@IdMat", modelo.IdMaterial);
                        cmd.Parameters.AddWithValue("@Tipo", modelo.TipoEmpaque ?? "Caja");
                        cmd.Parameters.AddWithValue("@Uds", modelo.UnidadesPorEmpaque);
                        cmd.Parameters.AddWithValue("@IdTemp", (object)modelo.IdTemporadaVigencia ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Activo", modelo.Activo ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string sql = @"
                        UPDATE dbo.PresentacionesMaterial SET 
                            IdMaterial = @IdMat,
                            TipoEmpaque = @Tipo,
                            UnidadesPorEmpaque = @Uds,
                            IdTemporadaVigencia = @IdTemp,
                            Activo = @Activo 
                        WHERE IdPresentacion = @Id;";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@IdMat", modelo.IdMaterial);
                        cmd.Parameters.AddWithValue("@Tipo", modelo.TipoEmpaque ?? "Caja");
                        cmd.Parameters.AddWithValue("@Uds", modelo.UnidadesPorEmpaque);
                        cmd.Parameters.AddWithValue("@IdTemp", (object)modelo.IdTemporadaVigencia ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Activo", modelo.Activo ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Id", modelo.IdPresentacion);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void AlternarEstadoPresentacion(int idPresentacion, bool activo)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = "UPDATE dbo.PresentacionesMaterial SET Activo = @Activo WHERE IdPresentacion = @Id;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Activo", activo ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Id", idPresentacion);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // =====================================================================
        // ALMACENES
        // =====================================================================

        public List<Almacen> ObtenerAlmacenes(bool soloActivos = true)
        {
            var lista = new List<Almacen>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT a.*, 
                           p.PrimerNombre, p.PrimerApellido, p.TelefonoCelularWhatsApp AS TelUsuario
                    FROM dbo.Almacenes a
                    LEFT JOIN dbo.Usuarios u ON a.IdUsuarioResponsable = u.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                    WHERE (@SoloActivos = 0 OR a.Activo = 1) 
                    ORDER BY a.NombreAlmacen;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@SoloActivos", soloActivos ? 1 : 0);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string nomResp = dr["Responsable"] != DBNull.Value ? dr["Responsable"].ToString() : "";
                            if (dr["PrimerNombre"] != DBNull.Value)
                            {
                                nomResp = $"{dr["PrimerNombre"]} {dr["PrimerApellido"]}".Trim();
                            }

                            string tel = dr["Telefono"] != DBNull.Value ? dr["Telefono"].ToString() : "";
                            if (string.IsNullOrEmpty(tel) && dr["TelUsuario"] != DBNull.Value)
                            {
                                tel = dr["TelUsuario"].ToString();
                            }

                            bool esCentral = true;
                            if (dr["EsCentral"] != DBNull.Value)
                            {
                                esCentral = Convert.ToBoolean(dr["EsCentral"]);
                            }

                            lista.Add(new Almacen
                            {
                                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                                NombreAlmacen = dr["NombreAlmacen"].ToString(),
                                Direccion = dr["Direccion"] != DBNull.Value ? dr["Direccion"].ToString() : "",
                                Responsable = nomResp,
                                IdUsuarioResponsable = dr["IdUsuarioResponsable"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioResponsable"]) : null,
                                Telefono = tel,
                                EsCentral = esCentral,
                                Activo = Convert.ToBoolean(dr["Activo"])
                            });
                        }
                    }
                }

                // Cargar equipos asignados a cada almacén
                if (lista.Any())
                {
                    string sqlEq = @"
                        SELECT ae.IdAlmacen, ae.IdEquipo, e.NombreEquipo
                        FROM dbo.AlmacenesEquipos ae
                        INNER JOIN dbo.Equipos e ON ae.IdEquipo = e.IdEquipo
                        ORDER BY e.NombreEquipo;";
                    using (var cmdEq = new SqlCommand(sqlEq, cn))
                    using (var drEq = cmdEq.ExecuteReader())
                    {
                        var dict = lista.ToDictionary(x => x.IdAlmacen);
                        while (drEq.Read())
                        {
                            int idAlm = Convert.ToInt32(drEq["IdAlmacen"]);
                            int idEq = Convert.ToInt32(drEq["IdEquipo"]);
                            string nomEq = drEq["NombreEquipo"].ToString();
                            if (dict.ContainsKey(idAlm))
                            {
                                dict[idAlm].IdsEquipos.Add(idEq);
                                dict[idAlm].NombresEquipos.Add(nomEq);
                            }
                        }
                    }
                }
            }
            return lista;
        }

        public List<Almacen> ObtenerAlmacenesPorEquipo(int? idEquipo, bool soloActivos = true)
        {
            var todos = ObtenerAlmacenes(soloActivos);
            if (!idEquipo.HasValue || idEquipo.Value <= 0) return todos;
            return todos.Where(a => a.EsCentral || a.IdsEquipos.Contains(idEquipo.Value)).ToList();
        }

        public void GuardarAlmacen(Almacen modelo)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        string nombreResp = modelo.Responsable;
                        string telResp = modelo.Telefono;
                        if (modelo.IdUsuarioResponsable.HasValue && modelo.IdUsuarioResponsable.Value > 0)
                        {
                            string sqlU = "SELECT PrimerNombre, PrimerApellido, TelefonoCelularWhatsApp FROM dbo.PerfilesCoordinador WHERE IdUsuario = @IdU;";
                            using (var cmdU = new SqlCommand(sqlU, cn, tx))
                            {
                                cmdU.Parameters.AddWithValue("@IdU", modelo.IdUsuarioResponsable.Value);
                                using (var dr = cmdU.ExecuteReader())
                                {
                                    if (dr.Read())
                                    {
                                        nombreResp = $"{dr["PrimerNombre"]} {dr["PrimerApellido"]}".Trim();
                                        if (string.IsNullOrEmpty(telResp) && dr["TelefonoCelularWhatsApp"] != DBNull.Value)
                                        {
                                            telResp = dr["TelefonoCelularWhatsApp"].ToString();
                                        }
                                    }
                                }
                            }
                        }

                        int idAlm = modelo.IdAlmacen;
                        if (idAlm == 0)
                        {
                            string sql = @"
                                INSERT INTO dbo.Almacenes 
                                    (NombreAlmacen, Direccion, Responsable, IdUsuarioResponsable, Telefono, EsCentral, Activo)
                                VALUES 
                                    (@Nombre, @Dir, @Resp, @IdUResp, @Tel, @EsCentral, 1);
                                SELECT SCOPE_IDENTITY();";
                            using (var cmd = new SqlCommand(sql, cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreAlmacen);
                                cmd.Parameters.AddWithValue("@Dir", modelo.Direccion ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Resp", (object)nombreResp ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IdUResp", modelo.IdUsuarioResponsable.HasValue && modelo.IdUsuarioResponsable.Value > 0 ? (object)modelo.IdUsuarioResponsable.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@Tel", (object)telResp ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EsCentral", modelo.EsCentral ? 1 : 0);
                                idAlm = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            string sql = @"
                                UPDATE dbo.Almacenes SET 
                                    NombreAlmacen = @Nombre, 
                                    Direccion = @Dir, 
                                    Responsable = @Resp, 
                                    IdUsuarioResponsable = @IdUResp,
                                    Telefono = @Tel, 
                                    EsCentral = @EsCentral,
                                    Activo = @Activo 
                                WHERE IdAlmacen = @Id;";
                            using (var cmd = new SqlCommand(sql, cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreAlmacen);
                                cmd.Parameters.AddWithValue("@Dir", modelo.Direccion ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Resp", (object)nombreResp ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IdUResp", modelo.IdUsuarioResponsable.HasValue && modelo.IdUsuarioResponsable.Value > 0 ? (object)modelo.IdUsuarioResponsable.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@Tel", (object)telResp ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EsCentral", modelo.EsCentral ? 1 : 0);
                                cmd.Parameters.AddWithValue("@Activo", modelo.Activo ? 1 : 0);
                                cmd.Parameters.AddWithValue("@Id", idAlm);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Sincronizar Equipos Asignados en dbo.AlmacenesEquipos
                        string sqlDelEq = "DELETE FROM dbo.AlmacenesEquipos WHERE IdAlmacen = @IdAlm;";
                        using (var cmdDel = new SqlCommand(sqlDelEq, cn, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@IdAlm", idAlm);
                            cmdDel.ExecuteNonQuery();
                        }

                        if (!modelo.EsCentral && modelo.IdsEquipos != null && modelo.IdsEquipos.Count > 0)
                        {
                            string sqlInsEq = "INSERT INTO dbo.AlmacenesEquipos (IdAlmacen, IdEquipo) VALUES (@IdAlm, @IdEq);";
                            foreach (int idEq in modelo.IdsEquipos)
                            {
                                if (idEq > 0)
                                {
                                    using (var cmdIns = new SqlCommand(sqlInsEq, cn, tx))
                                    {
                                        cmdIns.Parameters.AddWithValue("@IdAlm", idAlm);
                                        cmdIns.Parameters.AddWithValue("@IdEq", idEq);
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // =====================================================================
        // RECEPCIÓN DE CONTENEDORES (TRANSACCIÓN ACID Y CONTROL DE DUPLICIDAD)
        // =====================================================================

        public int RegistrarRecepcion(RecepcionContenedor modelo, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. Obtener temporada activa
                        int idTemporada = modelo.IdTemporada;
                        if (idTemporada <= 0)
                        {
                            using (var cmd = new SqlCommand("SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;", cn, tran))
                            {
                                object val = cmd.ExecuteScalar();
                                if (val == null || val == DBNull.Value) throw new InvalidOperationException("No hay una temporada activa en el sistema.");
                                idTemporada = Convert.ToInt32(val);
                            }
                        }

                        // 1b. Validar que el almacén exista y esté activo
                        using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Almacenes WHERE IdAlmacen = @IdAlm AND Activo = 1;", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacen);
                            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                                throw new InvalidOperationException("El almacén seleccionado no es válido o está inactivo.");
                        }

                        // 1c. Control de concurrencia e Idempotencia (Evitar doble recepción)
                        using (var cmd = new SqlCommand(
                            "SELECT COUNT(1) FROM dbo.RecepcionesContenedor WITH (UPDLOCK, HOLDLOCK) WHERE IdTemporada = @IdTemp AND LOWER(LTRIM(RTRIM(NumeroContenedor))) = LOWER(LTRIM(RTRIM(@Num))) AND IdAlmacen = @IdAlm AND EstadoRecepcion != 'ANULADA';", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                            cmd.Parameters.AddWithValue("@Num", modelo.NumeroContenedor ?? "");
                            cmd.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacen);
                            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                            {
                                throw new InvalidOperationException($"El contenedor '{modelo.NumeroContenedor}' ya fue recibido y confirmado previamente en este almacén para la temporada actual.");
                            }
                        }

                        // 2. Insertar encabezado de recepción
                        int idRecepcion = 0;
                        string sqlRecep = @"
                            INSERT INTO dbo.RecepcionesContenedor 
                                (NumeroContenedor, IdTemporada, IdAlmacen, FechaRecepcion, HoraRecepcion, IdEquipoReceptor, 
                                 ResponsableRecepcion, Observaciones, EstadoRecepcion, IdUsuarioRegistro, FechaRegistro)
                            OUTPUT INSERTED.IdRecepcion
                            VALUES 
                                (@Num, @IdTemp, @IdAlm, @Fecha, @Hora, @IdEq, 
                                 @Resp, @Obs, 'CONFIRMADA', @IdUser, GETDATE());";

                        using (var cmd = new SqlCommand(sqlRecep, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Num", modelo.NumeroContenedor.Trim());
                            cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                            cmd.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacen);
                            cmd.Parameters.AddWithValue("@Fecha", modelo.FechaRecepcion != DateTime.MinValue ? modelo.FechaRecepcion : DateTime.Now.Date);
                            cmd.Parameters.AddWithValue("@Hora", !string.IsNullOrWhiteSpace(modelo.HoraRecepcion) ? (object)modelo.HoraRecepcion.Trim() : DateTime.Now.ToString("hh:mm tt"));
                            cmd.Parameters.AddWithValue("@IdEq", modelo.IdEquipoReceptor.HasValue && modelo.IdEquipoReceptor.Value > 0 ? (object)modelo.IdEquipoReceptor.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Resp", modelo.ResponsableRecepcion ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", modelo.Observaciones ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            idRecepcion = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 3. Insertar detalles y actualizar inventario central
                        if (modelo.Detalles == null || modelo.Detalles.Count == 0)
                            throw new InvalidOperationException("Debe agregar al menos un material con empaques y unidades recibidas.");

                        foreach (var det in modelo.Detalles)
                        {
                            if (det.CantidadEmpaques <= 0 || det.UnidadesPorEmpaque <= 0) continue;

                            // 3a. Guardar detalle
                            string sqlDet = @"
                                INSERT INTO dbo.RecepcionesContenedorDetalle (IdRecepcion, IdMaterial, IdPresentacion, CantidadEmpaques, UnidadesPorEmpaque)
                                VALUES (@IdRec, @IdMat, @IdPres, @Empaques, @Uds);";
                            using (var cmd = new SqlCommand(sqlDet, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdRec", idRecepcion);
                                cmd.Parameters.AddWithValue("@IdMat", det.IdMaterial);
                                cmd.Parameters.AddWithValue("@IdPres", det.IdPresentacion);
                                cmd.Parameters.AddWithValue("@Empaques", det.CantidadEmpaques);
                                cmd.Parameters.AddWithValue("@Uds", det.UnidadesPorEmpaque);
                                cmd.ExecuteNonQuery();
                            }

                            int totalUnidades = det.CantidadEmpaques * det.UnidadesPorEmpaque;

                            // 3b. Upsert en InventarioCentral con bloqueo
                            string sqlInv = @"
                                MERGE dbo.InventarioCentral WITH (UPDLOCK, ROWLOCK) AS tgt
                                USING (SELECT @IdTemp AS IdTemporada, @IdAlm AS IdAlmacen, @IdMat AS IdMaterial) AS src
                                ON tgt.IdTemporada = src.IdTemporada AND tgt.IdAlmacen = src.IdAlmacen AND tgt.IdMaterial = src.IdMaterial
                                WHEN MATCHED THEN
                                    UPDATE SET CantidadFisica = tgt.CantidadFisica + @Total,
                                               CantidadDisponible = tgt.CantidadDisponible + @Total
                                WHEN NOT MATCHED THEN
                                    INSERT (IdTemporada, IdAlmacen, IdMaterial, CantidadFisica, CantidadTransferida, CantidadDisponible)
                                    VALUES (@IdTemp, @IdAlm, @IdMat, @Total, 0, @Total);";
                            using (var cmd = new SqlCommand(sqlInv, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                                cmd.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacen);
                                cmd.Parameters.AddWithValue("@IdMat", det.IdMaterial);
                                cmd.Parameters.AddWithValue("@Total", totalUnidades);
                                cmd.ExecuteNonQuery();
                            }

                            // 3c. Si la recepción está asignada a un equipo o almacén de equipo, acreditar en InventarioEquipo
                            int? idEquipoReceptorFinal = (modelo.IdEquipoReceptor.HasValue && modelo.IdEquipoReceptor.Value > 0) ? modelo.IdEquipoReceptor : null;
                            if (!idEquipoReceptorFinal.HasValue)
                            {
                                using (var cmdAlmEq = new SqlCommand("SELECT TOP 1 IdEquipo FROM dbo.AlmacenesEquipos WHERE IdAlmacen = @IdAlm;", cn, tran))
                                {
                                    cmdAlmEq.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacen);
                                    object valEq = cmdAlmEq.ExecuteScalar();
                                    if (valEq != null && valEq != DBNull.Value)
                                    {
                                        idEquipoReceptorFinal = Convert.ToInt32(valEq);
                                    }
                                }
                            }

                            if (idEquipoReceptorFinal.HasValue && idEquipoReceptorFinal.Value > 0)
                            {
                                string sqlEquipo = @"
                                    MERGE dbo.InventarioEquipo WITH (UPDLOCK, ROWLOCK) AS tgt
                                    USING (SELECT @IdTemp AS IdTemporada, @IdEq AS IdEquipo, @IdMat AS IdMaterial) AS src
                                    ON tgt.IdTemporada = src.IdTemporada AND tgt.IdEquipo = src.IdEquipo AND tgt.IdMaterial = src.IdMaterial
                                    WHEN MATCHED THEN
                                        UPDATE SET CantidadRecibida = tgt.CantidadRecibida + @Total,
                                                   CantidadDisponible = tgt.CantidadDisponible + @Total
                                    WHEN NOT MATCHED THEN
                                        INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible)
                                        VALUES (@IdTemp, @IdEq, @IdMat, @Total, 0, 0, @Total);";
                                using (var cmdEquipo = new SqlCommand(sqlEquipo, cn, tran))
                                {
                                    cmdEquipo.Parameters.AddWithValue("@IdTemp", idTemporada);
                                    cmdEquipo.Parameters.AddWithValue("@IdEq", idEquipoReceptorFinal.Value);
                                    cmdEquipo.Parameters.AddWithValue("@IdMat", det.IdMaterial);
                                    cmdEquipo.Parameters.AddWithValue("@Total", totalUnidades);
                                    cmdEquipo.ExecuteNonQuery();
                                }
                            }

                            // 3d. Kárdex de entrada
                            RegistrarMovimiento(cn, tran, idTemporada, "RECEPCION_CONTENEDOR", det.IdMaterial,
                                totalUnidades, null, modelo.IdAlmacen, idEquipoReceptorFinal, null,
                                "REC-" + idRecepcion, idUsuario, $"Recepción de contenedor #{modelo.NumeroContenedor} en almacén ID {modelo.IdAlmacen}");
                        }

                        // 4. Guardar evidencias adjuntas (si existen)
                        if (modelo.Evidencias != null && modelo.Evidencias.Count > 0)
                        {
                            string sqlEv = @"
                                INSERT INTO dbo.EvidenciasRecepcionContenedor 
                                    (IdRecepcion, NombreArchivo, RutaArchivo, TipoContenido, TamanoBytes, IdUsuarioRegistro, FechaRegistro)
                                VALUES 
                                    (@IdRec, @Nom, @Ruta, @Tipo, @Size, @IdUser, GETDATE());";
                            foreach (var ev in modelo.Evidencias)
                            {
                                using (var cmdEv = new SqlCommand(sqlEv, cn, tran))
                                {
                                    cmdEv.Parameters.AddWithValue("@IdRec", idRecepcion);
                                    cmdEv.Parameters.AddWithValue("@Nom", ev.NombreArchivo ?? "Evidencia");
                                    cmdEv.Parameters.AddWithValue("@Ruta", ev.RutaArchivo ?? "");
                                    cmdEv.Parameters.AddWithValue("@Tipo", ev.TipoContenido ?? (object)DBNull.Value);
                                    cmdEv.Parameters.AddWithValue("@Size", ev.TamanoBytes.HasValue ? (object)ev.TamanoBytes.Value : DBNull.Value);
                                    cmdEv.Parameters.AddWithValue("@IdUser", idUsuario);
                                    cmdEv.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();
                        AuditoriaHelper.Registrar("Recepción Contenedor", "Logistica", idRecepcion.ToString(), idUsuario,
                            $"Contenedor {modelo.NumeroContenedor} recibido y confirmado exitosamente en almacén ID {modelo.IdAlmacen}. Total materiales: {modelo.Detalles.Count}.");
                        return idRecepcion;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        // =====================================================================
        // INVENTARIO CENTRAL
        // =====================================================================

        public List<ItemInventarioCentral> ObtenerInventarioCentral(int? idTemporada = null, int? idAlmacen = null)
        {
            var lista = new List<ItemInventarioCentral>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT ic.*, t.NombreTemporada, a.NombreAlmacen, m.Codigo, m.NombreMaterial, m.UnidadEntrega
                    FROM dbo.InventarioCentral ic
                    INNER JOIN dbo.Temporadas t ON ic.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Almacenes a ON ic.IdAlmacen = a.IdAlmacen
                    INNER JOIN dbo.Materiales m ON ic.IdMaterial = m.IdMaterial
                    WHERE (@IdTemp IS NULL OR ic.IdTemporada = @IdTemp)
                      AND (@IdAlm IS NULL OR ic.IdAlmacen = @IdAlm)
                    ORDER BY m.Codigo;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdAlm", idAlmacen.HasValue ? (object)idAlmacen.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new ItemInventarioCentral
                            {
                                IdInventarioCentral = Convert.ToInt32(dr["IdInventarioCentral"]),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                                NombreAlmacen = dr["NombreAlmacen"].ToString(),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                CantidadFisica = Convert.ToInt32(dr["CantidadFisica"]),
                                CantidadTransferida = Convert.ToInt32(dr["CantidadTransferida"]),
                                CantidadDisponible = Convert.ToInt32(dr["CantidadDisponible"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // =====================================================================
        // TRANSFERENCIA A EQUIPOS (TRANSACCIÓN ACID Y TRAZABILIDAD COMPLETA)
        // =====================================================================

        public int RegistrarTransferencia(TransferenciaEquipo modelo, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. Temporada activa
                        int idTemporada = modelo.IdTemporada;
                        if (idTemporada <= 0)
                        {
                            using (var cmd = new SqlCommand("SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;", cn, tran))
                            {
                                object objTemp = cmd.ExecuteScalar();
                                if (objTemp == null || objTemp == DBNull.Value)
                                    throw new InvalidOperationException("No hay una temporada activa registrada en el sistema.");
                                idTemporada = Convert.ToInt32(objTemp);
                            }
                        }

                        // 2. Fechas de Emisión y Recepción
                        DateTime fechaEmision = modelo.FechaEmision ?? (modelo.FechaTransferencia != DateTime.MinValue ? modelo.FechaTransferencia : DateTime.Now);
                        modelo.FechaTransferencia = fechaEmision;

                        bool esRecibidaInmediata = modelo.FechaRecepcion.HasValue && !string.IsNullOrWhiteSpace(modelo.PersonaReceptoraEquipo);
                        if (modelo.FechaRecepcion.HasValue && modelo.FechaRecepcion.Value < fechaEmision)
                        {
                            throw new InvalidOperationException("La fecha de recepción no puede ser anterior a la fecha de emisión.");
                        }

                        string estado = esRecibidaInmediata ? "RECIBIDA" : "EMITIDA";

                        // 3. Número de constancia único
                        string constancia = "TRF-" + fechaEmision.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);

                        // 4. Insertar encabezado con trazabilidad
                        int idTransf = 0;
                        string sqlTransf = @"
                            INSERT INTO dbo.TransferenciasEquipo 
                                (NumeroConstancia, IdTemporada, IdEquipo, IdEquipoEmisor, IdAlmacenOrigen, 
                                 FechaTransferencia, FechaEmision, FechaRecepcion, 
                                 IdUsuarioEmisor, CoordinadorEmisor, IdUsuarioReceptor, PersonaReceptoraEquipo, 
                                 Observaciones, Estado, IdUsuarioRegistro)
                            OUTPUT INSERTED.IdTransferencia
                            VALUES 
                                (@Const, @IdTemp, @IdEqReceptor, @IdEqEmisor, @IdAlm, 
                                 @Fecha, @FechaEmision, @FechaRecepcion, 
                                 @IdUserEmisor, @Emisor, @IdUserReceptor, @Receptor, 
                                 @Obs, @Estado, @IdUser);";

                        using (var cmd = new SqlCommand(sqlTransf, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Const", constancia);
                            cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                            cmd.Parameters.AddWithValue("@IdEqReceptor", modelo.IdEquipo);
                            cmd.Parameters.AddWithValue("@IdEqEmisor", modelo.IdEquipoEmisor.HasValue ? (object)modelo.IdEquipoEmisor.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdAlm", modelo.IdAlmacenOrigen);
                            cmd.Parameters.AddWithValue("@Fecha", fechaEmision);
                            cmd.Parameters.AddWithValue("@FechaEmision", fechaEmision);
                            cmd.Parameters.AddWithValue("@FechaRecepcion", modelo.FechaRecepcion.HasValue ? (object)modelo.FechaRecepcion.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUserEmisor", modelo.IdUsuarioEmisor.HasValue ? (object)modelo.IdUsuarioEmisor.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Emisor", modelo.CoordinadorEmisor ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUserReceptor", modelo.IdUsuarioReceptor.HasValue ? (object)modelo.IdUsuarioReceptor.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Receptor", modelo.PersonaReceptoraEquipo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", modelo.Observaciones ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Estado", estado);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            idTransf = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 5. Detalles + movimientos de stock
                        foreach (var det in modelo.Detalles)
                        {
                            if (det.CantidadUnidades <= 0) continue;

                            // 5a. Verificar stock suficiente en almacén origen con bloqueo de lectura
                            int disp = 0;
                            using (var cmd = new SqlCommand(
                                "SELECT ISNULL(CantidadDisponible,0) FROM dbo.InventarioCentral WITH (UPDLOCK, ROWLOCK) WHERE IdTemporada=@IdT AND IdAlmacen=@IdA AND IdMaterial=@IdM;", cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdT", idTemporada);
                                cmd.Parameters.AddWithValue("@IdA", modelo.IdAlmacenOrigen);
                                cmd.Parameters.AddWithValue("@IdM", det.IdMaterial);
                                object val = cmd.ExecuteScalar();
                                disp = val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
                            }
                            if (disp < det.CantidadUnidades)
                                throw new InvalidOperationException($"Stock insuficiente en el almacén emisor para el material ID {det.IdMaterial}. Disponible: {disp}, Solicitado: {det.CantidadUnidades}.");

                            // 5b. Detalle de transferencia
                            string sqlDet = @"INSERT INTO dbo.TransferenciasEquipoDetalle (IdTransferencia, IdMaterial, CantidadUnidades) VALUES (@IdT, @IdM, @Cant);";
                            using (var cmd = new SqlCommand(sqlDet, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdT", idTransf);
                                cmd.Parameters.AddWithValue("@IdM", det.IdMaterial);
                                cmd.Parameters.AddWithValue("@Cant", det.CantidadUnidades);
                                cmd.ExecuteNonQuery();
                            }

                            // 5c. Descontar del inventario central del almacén origen
                            string sqlCentral = @"
                                UPDATE dbo.InventarioCentral
                                SET CantidadTransferida = CantidadTransferida + @Cant,
                                    CantidadDisponible  = CantidadDisponible  - @Cant
                                WHERE IdTemporada=@IdT AND IdAlmacen=@IdA AND IdMaterial=@IdM;";
                            using (var cmd = new SqlCommand(sqlCentral, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@Cant", det.CantidadUnidades);
                                cmd.Parameters.AddWithValue("@IdT", idTemporada);
                                cmd.Parameters.AddWithValue("@IdA", modelo.IdAlmacenOrigen);
                                cmd.Parameters.AddWithValue("@IdM", det.IdMaterial);
                                cmd.ExecuteNonQuery();
                            }

                            // 5c.2 Si la transferencia sale de un equipo emisor, descontar del inventario del equipo emisor
                            if (modelo.IdEquipoEmisor.HasValue && modelo.IdEquipoEmisor.Value > 0)
                            {
                                string sqlDescEmisor = @"
                                    UPDATE dbo.InventarioEquipo
                                    SET CantidadRecibida = CantidadRecibida - @Cant,
                                        CantidadDisponible = CantidadDisponible - @Cant
                                    WHERE IdTemporada=@IdT AND IdEquipo=@IdEq AND IdMaterial=@IdM;";
                                using (var cmdEmisor = new SqlCommand(sqlDescEmisor, cn, tran))
                                {
                                    cmdEmisor.Parameters.AddWithValue("@Cant", det.CantidadUnidades);
                                    cmdEmisor.Parameters.AddWithValue("@IdT", idTemporada);
                                    cmdEmisor.Parameters.AddWithValue("@IdEq", modelo.IdEquipoEmisor.Value);
                                    cmdEmisor.Parameters.AddWithValue("@IdM", det.IdMaterial);
                                    cmdEmisor.ExecuteNonQuery();
                                }
                            }

                            // 5d. Si la recepción es inmediata, acreditar en inventario del equipo receptor
                            if (esRecibidaInmediata)
                            {
                                string sqlEquipo = @"
                                    MERGE dbo.InventarioEquipo AS tgt
                                    USING (SELECT @IdT AS IdTemporada, @IdEq AS IdEquipo, @IdM AS IdMaterial) AS src
                                    ON tgt.IdTemporada=src.IdTemporada AND tgt.IdEquipo=src.IdEquipo AND tgt.IdMaterial=src.IdMaterial
                                    WHEN MATCHED THEN
                                        UPDATE SET CantidadRecibida = tgt.CantidadRecibida + @Cant,
                                                   CantidadDisponible = tgt.CantidadDisponible + @Cant
                                    WHEN NOT MATCHED THEN
                                        INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible)
                                        VALUES (@IdT, @IdEq, @IdM, @Cant, 0, 0, @Cant);";
                                using (var cmd = new SqlCommand(sqlEquipo, cn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@IdT", idTemporada);
                                    cmd.Parameters.AddWithValue("@IdEq", modelo.IdEquipo);
                                    cmd.Parameters.AddWithValue("@IdM", det.IdMaterial);
                                    cmd.Parameters.AddWithValue("@Cant", det.CantidadUnidades);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // 5e. Registrar movimiento en Kárdex
                            RegistrarMovimiento(cn, tran, idTemporada, "TRANSFERENCIA_EQUIPO", det.IdMaterial,
                                det.CantidadUnidades, modelo.IdAlmacenOrigen, null, modelo.IdEquipo, null,
                                constancia, idUsuario, $"Transferencia {constancia} de material ID {det.IdMaterial} al equipo ID {modelo.IdEquipo}");
                        }

                        tran.Commit();
                        AuditoriaHelper.Registrar("Transferencia Equipo", "Logistica", idTransf.ToString(), idUsuario,
                            $"Transferencia {constancia} registrada con éxito. Estado: {estado}.");
                        return idTransf;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void ConfirmarRecepcionTransferencia(int idTransferencia, DateTime fechaRecepcion, string personaReceptora, int? idUsuarioReceptor, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int idTemporada = 0;
                        int idEquipoReceptor = 0;
                        string estadoActual = "";
                        DateTime fechaEmision = DateTime.MinValue;
                        string constancia = "";

                        string sqlHead = @"
                            SELECT IdTemporada, IdEquipo, Estado, FechaTransferencia, NumeroConstancia 
                            FROM dbo.TransferenciasEquipo WITH (UPDLOCK, ROWLOCK)
                            WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlHead, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    idTemporada = Convert.ToInt32(dr["IdTemporada"]);
                                    idEquipoReceptor = Convert.ToInt32(dr["IdEquipo"]);
                                    estadoActual = dr["Estado"].ToString();
                                    fechaEmision = Convert.ToDateTime(dr["FechaTransferencia"]);
                                    constancia = dr["NumeroConstancia"].ToString();
                                }
                                else
                                {
                                    throw new InvalidOperationException("La transferencia especificada no existe.");
                                }
                            }
                        }

                        if (estadoActual == "RECIBIDA" || estadoActual == "COMPLETADA")
                            throw new InvalidOperationException("La transferencia ya se encuentra confirmada como RECIBIDA.");
                        if (estadoActual == "CANCELADA")
                            throw new InvalidOperationException("No se puede confirmar la recepción de una transferencia cancelada.");

                        if (fechaRecepcion < fechaEmision)
                            throw new InvalidOperationException("La fecha de recepción no puede ser anterior a la fecha de emisión.");

                        // 1. Actualizar estado y fecha en encabezado
                        string sqlUpd = @"
                            UPDATE dbo.TransferenciasEquipo 
                            SET Estado = 'RECIBIDA', 
                                FechaRecepcion = @FechaRec, 
                                PersonaReceptoraEquipo = @PersonaRec,
                                IdUsuarioReceptor = @IdUserRec
                            WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlUpd, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            cmd.Parameters.AddWithValue("@FechaRec", fechaRecepcion);
                            cmd.Parameters.AddWithValue("@PersonaRec", personaReceptora ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUserRec", idUsuarioReceptor.HasValue ? (object)idUsuarioReceptor.Value : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Acreditar materiales en inventario del equipo receptor
                        var detalles = new List<Tuple<int, int>>();
                        string sqlDet = "SELECT IdMaterial, CantidadUnidades FROM dbo.TransferenciasEquipoDetalle WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlDet, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    detalles.Add(Tuple.Create(Convert.ToInt32(dr["IdMaterial"]), Convert.ToInt32(dr["CantidadUnidades"])));
                                }
                            }
                        }

                        foreach (var item in detalles)
                        {
                            int idMat = item.Item1;
                            int cant = item.Item2;

                            string sqlEquipo = @"
                                MERGE dbo.InventarioEquipo AS tgt
                                USING (SELECT @IdT AS IdTemporada, @IdEq AS IdEquipo, @IdM AS IdMaterial) AS src
                                ON tgt.IdTemporada=src.IdTemporada AND tgt.IdEquipo=src.IdEquipo AND tgt.IdMaterial=src.IdMaterial
                                WHEN MATCHED THEN
                                    UPDATE SET CantidadRecibida = tgt.CantidadRecibida + @Cant,
                                               CantidadDisponible = tgt.CantidadDisponible + @Cant
                                WHEN NOT MATCHED THEN
                                    INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible)
                                    VALUES (@IdT, @IdEq, @IdM, @Cant, 0, 0, @Cant);";
                            using (var cmd = new SqlCommand(sqlEquipo, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdT", idTemporada);
                                cmd.Parameters.AddWithValue("@IdEq", idEquipoReceptor);
                                cmd.Parameters.AddWithValue("@IdM", idMat);
                                cmd.Parameters.AddWithValue("@Cant", cant);
                                cmd.ExecuteNonQuery();
                            }

                            RegistrarMovimiento(cn, tran, idTemporada, "RECEPCION_TRANSFERENCIA", idMat,
                                cant, null, null, idEquipoReceptor, null,
                                constancia, idUsuario, $"Confirmación de recepción física de transferencia {constancia} por el equipo receptor ID {idEquipoReceptor}");
                        }

                        tran.Commit();
                        AuditoriaHelper.Registrar("Confirmar Recepción", "Logistica", idTransferencia.ToString(), idUsuario,
                            $"Transferencia {constancia} confirmada como RECIBIDA por {personaReceptora}.");
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void CancelarTransferencia(int idTransferencia, string motivo, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int idTemporada = 0;
                        int idAlmacenOrigen = 0;
                        int? idEquipoEmisor = null;
                        string estadoActual = "";
                        string constancia = "";

                        string sqlHead = @"
                            SELECT IdTemporada, IdAlmacenOrigen, IdEquipoEmisor, Estado, NumeroConstancia 
                            FROM dbo.TransferenciasEquipo WITH (UPDLOCK, ROWLOCK)
                            WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlHead, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    idTemporada = Convert.ToInt32(dr["IdTemporada"]);
                                    idAlmacenOrigen = Convert.ToInt32(dr["IdAlmacenOrigen"]);
                                    idEquipoEmisor = dr["IdEquipoEmisor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoEmisor"]) : null;
                                    estadoActual = dr["Estado"].ToString();
                                    constancia = dr["NumeroConstancia"].ToString();
                                }
                                else
                                {
                                    throw new InvalidOperationException("La transferencia especificada no existe.");
                                }
                            }
                        }

                        if (estadoActual == "RECIBIDA" || estadoActual == "COMPLETADA")
                            throw new InvalidOperationException("No se puede cancelar una transferencia que ya fue recibida físicamente por el equipo receptor.");
                        if (estadoActual == "CANCELADA")
                            throw new InvalidOperationException("La transferencia ya se encuentra cancelada.");

                        // 1. Revertir el stock en Inventario Central
                        var detalles = new List<Tuple<int, int>>();
                        string sqlDet = "SELECT IdMaterial, CantidadUnidades FROM dbo.TransferenciasEquipoDetalle WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlDet, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    detalles.Add(Tuple.Create(Convert.ToInt32(dr["IdMaterial"]), Convert.ToInt32(dr["CantidadUnidades"])));
                                }
                            }
                        }

                        foreach (var item in detalles)
                        {
                            int idMat = item.Item1;
                            int cant = item.Item2;

                            string sqlRev = @"
                                UPDATE dbo.InventarioCentral
                                SET CantidadTransferida = CantidadTransferida - @Cant,
                                    CantidadDisponible  = CantidadDisponible  + @Cant
                                WHERE IdTemporada=@IdT AND IdAlmacen=@IdA AND IdMaterial=@IdM;";
                            using (var cmd = new SqlCommand(sqlRev, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@Cant", cant);
                                cmd.Parameters.AddWithValue("@IdT", idTemporada);
                                cmd.Parameters.AddWithValue("@IdA", idAlmacenOrigen);
                                cmd.Parameters.AddWithValue("@IdM", idMat);
                                cmd.ExecuteNonQuery();
                            }

                            // Revertir inventario del equipo emisor si correspondía
                            if (idEquipoEmisor.HasValue && idEquipoEmisor.Value > 0)
                            {
                                string sqlRevEq = @"
                                    UPDATE dbo.InventarioEquipo
                                    SET CantidadRecibida = CantidadRecibida + @Cant,
                                        CantidadDisponible = CantidadDisponible + @Cant
                                    WHERE IdTemporada=@IdT AND IdEquipo=@IdEq AND IdMaterial=@IdM;";
                                using (var cmdRevEq = new SqlCommand(sqlRevEq, cn, tran))
                                {
                                    cmdRevEq.Parameters.AddWithValue("@Cant", cant);
                                    cmdRevEq.Parameters.AddWithValue("@IdT", idTemporada);
                                    cmdRevEq.Parameters.AddWithValue("@IdEq", idEquipoEmisor.Value);
                                    cmdRevEq.Parameters.AddWithValue("@IdM", idMat);
                                    cmdRevEq.ExecuteNonQuery();
                                }
                            }

                            RegistrarMovimiento(cn, tran, idTemporada, "CANCELACION_TRANSFERENCIA", idMat,
                                cant, idAlmacenOrigen, null, null, null,
                                constancia, idUsuario, $"Cancelación de transferencia {constancia}. Motivo: {motivo}");
                        }

                        // 2. Marcar como CANCELADA
                        string sqlCancel = @"
                            UPDATE dbo.TransferenciasEquipo 
                            SET Estado = 'CANCELADA', 
                                Observaciones = ISNULL(Observaciones + ' | ', '') + 'Cancelada: ' + @Motivo
                            WHERE IdTransferencia = @Id;";
                        using (var cmd = new SqlCommand(sqlCancel, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", idTransferencia);
                            cmd.Parameters.AddWithValue("@Motivo", string.IsNullOrWhiteSpace(motivo) ? "Cancelado por el usuario" : motivo);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        AuditoriaHelper.Registrar("Cancelar Transferencia", "Logistica", idTransferencia.ToString(), idUsuario,
                            $"Transferencia {constancia} cancelada. Motivo: {motivo}");
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public TransferenciaEquipo ObtenerTransferenciaDetalle(int idTransferencia)
        {
            TransferenciaEquipo t = null;
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlHead = @"
                    SELECT tr.*, tp.NombreTemporada, 
                           eqReceptor.NombreEquipo AS NombreEquipoReceptor,
                           eqEmisor.NombreEquipo AS NombreEquipoEmisor,
                           a.NombreAlmacen
                    FROM dbo.TransferenciasEquipo tr
                    INNER JOIN dbo.Temporadas tp ON tr.IdTemporada = tp.IdTemporada
                    INNER JOIN dbo.Equipos eqReceptor ON tr.IdEquipo = eqReceptor.IdEquipo
                    LEFT JOIN dbo.Equipos eqEmisor ON tr.IdEquipoEmisor = eqEmisor.IdEquipo
                    INNER JOIN dbo.Almacenes a ON tr.IdAlmacenOrigen = a.IdAlmacen
                    WHERE tr.IdTransferencia = @Id;";
                using (var cmd = new SqlCommand(sqlHead, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idTransferencia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            t = new TransferenciaEquipo
                            {
                                IdTransferencia = Convert.ToInt32(dr["IdTransferencia"]),
                                NumeroConstancia = dr["NumeroConstancia"].ToString(),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdEquipoEmisor = dr["IdEquipoEmisor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoEmisor"]) : null,
                                NombreEquipoEmisor = dr["NombreEquipoEmisor"] != DBNull.Value ? dr["NombreEquipoEmisor"].ToString() : "Almacén Central / Nacional",
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipoReceptor"].ToString(),
                                IdAlmacenOrigen = Convert.ToInt32(dr["IdAlmacenOrigen"]),
                                NombreAlmacenOrigen = dr["NombreAlmacen"].ToString(),
                                FechaTransferencia = Convert.ToDateTime(dr["FechaTransferencia"]),
                                FechaEmision = dr["FechaEmision"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaEmision"]) : Convert.ToDateTime(dr["FechaTransferencia"]),
                                FechaRecepcion = dr["FechaRecepcion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaRecepcion"]) : null,
                                IdUsuarioEmisor = dr["IdUsuarioEmisor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioEmisor"]) : null,
                                CoordinadorEmisor = dr["CoordinadorEmisor"] != DBNull.Value ? dr["CoordinadorEmisor"].ToString() : "",
                                IdUsuarioReceptor = dr["IdUsuarioReceptor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioReceptor"]) : null,
                                PersonaReceptoraEquipo = dr["PersonaReceptoraEquipo"] != DBNull.Value ? dr["PersonaReceptoraEquipo"].ToString() : "",
                                Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : "",
                                Estado = dr["Estado"].ToString()
                            };
                        }
                    }
                }
                if (t == null) return null;

                string sqlDet = @"
                    SELECT d.*, m.Codigo, m.NombreMaterial, m.UnidadEntrega
                    FROM dbo.TransferenciasEquipoDetalle d
                    INNER JOIN dbo.Materiales m ON d.IdMaterial = m.IdMaterial
                    WHERE d.IdTransferencia = @Id;";
                using (var cmd = new SqlCommand(sqlDet, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idTransferencia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            t.Detalles.Add(new TransferenciaEquipoDetalle
                            {
                                IdTransferenciaDetalle = Convert.ToInt32(dr["IdTransferenciaDetalle"]),
                                IdTransferencia = Convert.ToInt32(dr["IdTransferencia"]),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                CantidadUnidades = Convert.ToInt32(dr["CantidadUnidades"])
                            });
                        }
                    }
                }
            }
            return t;
        }

        public List<TransferenciaEquipo> ObtenerTransferencias(int? idTemporada = null, int? idEquipo = null)
        {
            var lista = new List<TransferenciaEquipo>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT tr.*, tp.NombreTemporada, 
                           eqReceptor.NombreEquipo AS NombreEquipoReceptor,
                           eqEmisor.NombreEquipo AS NombreEquipoEmisor,
                           a.NombreAlmacen
                    FROM dbo.TransferenciasEquipo tr
                    INNER JOIN dbo.Temporadas tp ON tr.IdTemporada = tp.IdTemporada
                    INNER JOIN dbo.Equipos eqReceptor ON tr.IdEquipo = eqReceptor.IdEquipo
                    LEFT JOIN dbo.Equipos eqEmisor ON tr.IdEquipoEmisor = eqEmisor.IdEquipo
                    INNER JOIN dbo.Almacenes a ON tr.IdAlmacenOrigen = a.IdAlmacen
                    WHERE (@IdTemp IS NULL OR tr.IdTemporada = @IdTemp)
                      AND (@IdEq IS NULL OR tr.IdEquipo = @IdEq OR tr.IdEquipoEmisor = @IdEq)
                    ORDER BY tr.FechaTransferencia DESC;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new TransferenciaEquipo
                            {
                                IdTransferencia = Convert.ToInt32(dr["IdTransferencia"]),
                                NumeroConstancia = dr["NumeroConstancia"].ToString(),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdEquipoEmisor = dr["IdEquipoEmisor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoEmisor"]) : null,
                                NombreEquipoEmisor = dr["NombreEquipoEmisor"] != DBNull.Value ? dr["NombreEquipoEmisor"].ToString() : "Almacén Central / Nacional",
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipoReceptor"].ToString(),
                                IdAlmacenOrigen = Convert.ToInt32(dr["IdAlmacenOrigen"]),
                                NombreAlmacenOrigen = dr["NombreAlmacen"].ToString(),
                                FechaTransferencia = Convert.ToDateTime(dr["FechaTransferencia"]),
                                FechaEmision = dr["FechaEmision"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaEmision"]) : Convert.ToDateTime(dr["FechaTransferencia"]),
                                FechaRecepcion = dr["FechaRecepcion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaRecepcion"]) : null,
                                IdUsuarioEmisor = dr["IdUsuarioEmisor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioEmisor"]) : null,
                                CoordinadorEmisor = dr["CoordinadorEmisor"] != DBNull.Value ? dr["CoordinadorEmisor"].ToString() : "",
                                IdUsuarioReceptor = dr["IdUsuarioReceptor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioReceptor"]) : null,
                                PersonaReceptoraEquipo = dr["PersonaReceptoraEquipo"] != DBNull.Value ? dr["PersonaReceptoraEquipo"].ToString() : "",
                                Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : "",
                                Estado = dr["Estado"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // =====================================================================
        // INVENTARIO POR EQUIPO
        // =====================================================================

        public List<ItemInventarioEquipo> ObtenerInventarioEquipo(int? idTemporada = null, int? idEquipo = null)
        {
            var lista = new List<ItemInventarioEquipo>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Asegurar columna e índices si faltaran
                using (var cmdCol = new SqlCommand("IF COL_LENGTH('dbo.RecepcionesContenedor', 'IdEquipoReceptor') IS NULL ALTER TABLE dbo.RecepcionesContenedor ADD IdEquipoReceptor INT NULL;", cn))
                {
                    cmdCol.ExecuteNonQuery();
                }

                // Auto-sincronizar recepciones de contenedor y transferencias hacia InventarioEquipo
                string sqlSync = @"
                    MERGE dbo.InventarioEquipo AS tgt
                    USING (
                        SELECT t_all.IdTemporada, t_all.IdEquipo, t_all.IdMaterial, 
                               CASE WHEN SUM(t_all.TotalRecibido) < 0 THEN 0 ELSE SUM(t_all.TotalRecibido) END AS TotalRecibido
                        FROM (
                            -- 1. Recepciones directas de contenedor
                            SELECT rc.IdTemporada, 
                                   COALESCE(rc.IdEquipoReceptor, ae.IdEquipo, asig_alm.IdEquipo, asig_reg.IdEquipo) AS IdEquipo, 
                                   rd.IdMaterial,
                                   SUM(rd.CantidadEmpaques * rd.UnidadesPorEmpaque) AS TotalRecibido
                            FROM dbo.RecepcionesContenedor rc
                            INNER JOIN dbo.RecepcionesContenedorDetalle rd ON rc.IdRecepcion = rd.IdRecepcion
                            LEFT JOIN dbo.Almacenes a ON rc.IdAlmacen = a.IdAlmacen
                            LEFT JOIN dbo.AlmacenesEquipos ae ON rc.IdAlmacen = ae.IdAlmacen
                            LEFT JOIN dbo.AsignacionesEquipo asig_alm ON a.IdUsuarioResponsable = asig_alm.IdUsuario AND asig_alm.Activo = 1
                            LEFT JOIN dbo.AsignacionesEquipo asig_reg ON rc.IdUsuarioRegistro = asig_reg.IdUsuario AND asig_reg.Activo = 1
                            WHERE rc.EstadoRecepcion != 'ANULADA'
                              AND COALESCE(rc.IdEquipoReceptor, ae.IdEquipo, asig_alm.IdEquipo, asig_reg.IdEquipo) IS NOT NULL
                            GROUP BY rc.IdTemporada, COALESCE(rc.IdEquipoReceptor, ae.IdEquipo, asig_alm.IdEquipo, asig_reg.IdEquipo), rd.IdMaterial

                            UNION ALL

                            -- 2. Transferencias recibidas por el equipo receptor (Entradas)
                            SELECT te.IdTemporada,
                                   te.IdEquipo AS IdEquipo,
                                   td.IdMaterial,
                                   SUM(td.CantidadUnidades) AS TotalRecibido
                            FROM dbo.TransferenciasEquipo te
                            INNER JOIN dbo.TransferenciasEquipoDetalle td ON te.IdTransferencia = td.IdTransferencia
                            WHERE te.Estado IN ('RECIBIDA', 'COMPLETADA')
                            GROUP BY te.IdTemporada, te.IdEquipo, td.IdMaterial

                            UNION ALL

                            -- 3. Transferencias enviadas por un equipo emisor (Salidas)
                            SELECT te.IdTemporada,
                                   te.IdEquipoEmisor AS IdEquipo,
                                   td.IdMaterial,
                                   -SUM(td.CantidadUnidades) AS TotalRecibido
                            FROM dbo.TransferenciasEquipo te
                            INNER JOIN dbo.TransferenciasEquipoDetalle td ON te.IdTransferencia = td.IdTransferencia
                            WHERE te.Estado IN ('RECIBIDA', 'COMPLETADA', 'EMITIDA', 'EN_TRANSITO')
                              AND te.IdEquipoEmisor IS NOT NULL
                            GROUP BY te.IdTemporada, te.IdEquipoEmisor, td.IdMaterial
                        ) t_all
                        WHERE t_all.IdEquipo IS NOT NULL
                        GROUP BY t_all.IdTemporada, t_all.IdEquipo, t_all.IdMaterial
                    ) AS src
                    ON tgt.IdTemporada = src.IdTemporada AND tgt.IdEquipo = src.IdEquipo AND tgt.IdMaterial = src.IdMaterial
                    WHEN MATCHED THEN
                        UPDATE SET CantidadRecibida = src.TotalRecibido,
                                   CantidadDisponible = CASE 
                                       WHEN (src.TotalRecibido - ISNULL(tgt.CantidadDespachada, 0)) < 0 THEN 0 
                                       ELSE (src.TotalRecibido - ISNULL(tgt.CantidadDespachada, 0)) 
                                   END
                    WHEN NOT MATCHED THEN
                        INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible)
                        VALUES (src.IdTemporada, src.IdEquipo, src.IdMaterial, 
                                src.TotalRecibido, 0, 0, src.TotalRecibido);";
                using (var cmdSync = new SqlCommand(sqlSync, cn))
                {
                    cmdSync.ExecuteNonQuery();
                }

                string sql = @"
                    SELECT ie.*, t.NombreTemporada, eq.NombreEquipo, m.Codigo, m.NombreMaterial, m.UnidadEntrega
                    FROM dbo.InventarioEquipo ie
                    INNER JOIN dbo.Temporadas t ON ie.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Equipos eq ON ie.IdEquipo = eq.IdEquipo
                    INNER JOIN dbo.Materiales m ON ie.IdMaterial = m.IdMaterial
                    WHERE (@IdTemp IS NULL OR ie.IdTemporada = @IdTemp)
                      AND (@IdEq IS NULL OR ie.IdEquipo = @IdEq)
                    ORDER BY eq.NombreEquipo, m.Codigo;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new ItemInventarioEquipo
                            {
                                IdInventarioEquipo = Convert.ToInt32(dr["IdInventarioEquipo"]),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipo"].ToString(),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                CantidadRecibida = Convert.ToInt32(dr["CantidadRecibida"]),
                                CantidadAsignada = Convert.ToInt32(dr["CantidadAsignada"]),
                                CantidadDespachada = Convert.ToInt32(dr["CantidadDespachada"]),
                                CantidadDisponible = Convert.ToInt32(dr["CantidadDisponible"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<ResumenInventarioEquipo> ObtenerResumenInventarioEquipos(int? idTemporada = null, int? idEquipo = null)
        {
            var lista = new List<ResumenInventarioEquipo>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // 1. Obtener lista de equipos con su almacén asignado y coordinador responsable
                string sqlEquipos = @"
                    SELECT 
                        e.IdEquipo,
                        e.NombreEquipo,
                        n.NombreNivel,
                        alm.IdAlmacen,
                        ISNULL(alm.NombreAlmacen, 'Sin Almacén Asignado') AS NombreAlmacen,
                        coord.IdUsuario AS IdUsuarioCoordinador,
                        ISNULL(coord.NombreCompleto, ISNULL(alm.Responsable, 'Sin Coordinador')) AS NombreCoordinador,
                        ISNULL(coord.Telefono, ISNULL(alm.Telefono, '')) AS TelefonoCoordinador,
                        ISNULL(coord.NombrePosicion, 'Coordinador') AS PosicionCoordinador,
                        coord.Correo AS CorreoCoordinador
                    FROM dbo.Equipos e
                    INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo
                    OUTER APPLY (
                        SELECT TOP 1 a.IdAlmacen, a.NombreAlmacen, a.Responsable, a.Telefono
                        FROM dbo.AlmacenesEquipos ae
                        INNER JOIN dbo.Almacenes a ON ae.IdAlmacen = a.IdAlmacen
                        WHERE ae.IdEquipo = e.IdEquipo AND a.Activo = 1
                        ORDER BY a.EsCentral DESC, a.IdAlmacen ASC
                    ) alm
                    OUTER APPLY (
                        SELECT TOP 1 
                            u.IdUsuario,
                            CONCAT(p.PrimerNombre, ' ', p.PrimerApellido) AS NombreCompleto,
                            p.TelefonoCelularWhatsApp AS Telefono,
                            pos.NombrePosicion,
                            u.Correo
                        FROM dbo.AsignacionesEquipo asig
                        INNER JOIN dbo.Usuarios u ON asig.IdUsuario = u.IdUsuario
                        LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                        LEFT JOIN dbo.PosicionesOCC pos ON asig.IdPosicion = pos.IdPosicion
                        WHERE asig.IdEquipo = e.IdEquipo AND asig.Activo = 1
                        ORDER BY 
                            CASE 
                                WHEN pos.NombrePosicion LIKE '%Logística%' OR pos.NombrePosicion LIKE '%Logistica%' THEN 1
                                WHEN pos.NombrePosicion LIKE '%Equipo%' OR pos.NombrePosicion LIKE '%Líder%' OR pos.NombrePosicion LIKE '%Lider%' THEN 2
                                ELSE 3 
                            END,
                            asig.IdAsignacion ASC
                    ) coord
                    WHERE e.Activo = 1
                      AND (@IdEq IS NULL OR e.IdEquipo = @IdEq)
                    ORDER BY n.RangoJerarquico, e.NombreEquipo;";

                using (var cmd = new SqlCommand(sqlEquipos, cn))
                {
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new ResumenInventarioEquipo
                            {
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipo"].ToString(),
                                NombreNivel = dr["NombreNivel"].ToString(),
                                IdAlmacen = dr["IdAlmacen"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdAlmacen"]) : null,
                                NombreAlmacen = dr["NombreAlmacen"].ToString(),
                                IdUsuarioCoordinador = dr["IdUsuarioCoordinador"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdUsuarioCoordinador"]) : null,
                                NombreCoordinador = dr["NombreCoordinador"].ToString().Trim(),
                                TelefonoCoordinador = dr["TelefonoCoordinador"].ToString().Trim(),
                                PosicionCoordinador = dr["PosicionCoordinador"].ToString().Trim(),
                                CorreoCoordinador = dr["CorreoCoordinador"] != DBNull.Value ? dr["CorreoCoordinador"].ToString() : ""
                            });
                        }
                    }
                }

                // 2. Cargar todos los materiales de inventario agrupados por equipo
                var todosLosItems = ObtenerInventarioEquipo(idTemporada, idEquipo);
                var lookup = System.Linq.Enumerable.ToDictionary(
                    System.Linq.Enumerable.GroupBy(todosLosItems, x => x.IdEquipo),
                    g => g.Key,
                    g => System.Linq.Enumerable.ToList(g)
                );

                foreach (var eq in lista)
                {
                    if (lookup.TryGetValue(eq.IdEquipo, out var mats))
                    {
                        eq.Materiales = mats;
                        eq.TotalRecibido = System.Linq.Enumerable.Sum(mats, m => m.CantidadRecibida);
                        eq.TotalAsignado = System.Linq.Enumerable.Sum(mats, m => m.CantidadAsignada);
                        eq.TotalDespachado = System.Linq.Enumerable.Sum(mats, m => m.CantidadDespachada);
                        eq.TotalDisponible = System.Linq.Enumerable.Sum(mats, m => m.CantidadDisponible);
                    }
                }
            }
            return lista;
        }

        // =====================================================================
        // RECEPCIONES — CONSULTA
        // =====================================================================

        public List<RecepcionContenedor> ObtenerRecepciones(int? idTemporada = null, int? idAlmacen = null, int? idEquipo = null)
        {
            var lista = new List<RecepcionContenedor>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT rc.*, t.NombreTemporada, a.NombreAlmacen, eq.NombreEquipo AS NombreEquipoReceptor
                    FROM dbo.RecepcionesContenedor rc
                    INNER JOIN dbo.Temporadas t ON rc.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Almacenes a ON rc.IdAlmacen = a.IdAlmacen
                    LEFT JOIN dbo.Equipos eq ON rc.IdEquipoReceptor = eq.IdEquipo
                    WHERE (@IdTemp IS NULL OR rc.IdTemporada = @IdTemp)
                      AND (@IdAlm IS NULL OR rc.IdAlmacen = @IdAlm)
                      AND (@IdEq IS NULL OR rc.IdEquipoReceptor = @IdEq OR a.EsCentral = 1 OR EXISTS (SELECT 1 FROM dbo.AlmacenesEquipos ae WHERE ae.IdAlmacen = rc.IdAlmacen AND ae.IdEquipo = @IdEq))
                    ORDER BY rc.FechaRegistro DESC;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue && idTemporada.Value > 0 ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdAlm", idAlmacen.HasValue && idAlmacen.Value > 0 ? (object)idAlmacen.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue && idEquipo.Value > 0 ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new RecepcionContenedor
                            {
                                IdRecepcion = Convert.ToInt32(dr["IdRecepcion"]),
                                NumeroContenedor = dr["NumeroContenedor"].ToString(),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                                NombreAlmacen = dr["NombreAlmacen"].ToString(),
                                IdEquipoReceptor = dr["IdEquipoReceptor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoReceptor"]) : null,
                                NombreEquipoReceptor = dr["NombreEquipoReceptor"] != DBNull.Value ? dr["NombreEquipoReceptor"].ToString() : "",
                                FechaRecepcion = Convert.ToDateTime(dr["FechaRecepcion"]),
                                HoraRecepcion = dr["HoraRecepcion"] != DBNull.Value ? dr["HoraRecepcion"].ToString() : "",
                                ResponsableRecepcion = dr["ResponsableRecepcion"] != DBNull.Value ? dr["ResponsableRecepcion"].ToString() : "",
                                Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : "",
                                EstadoRecepcion = dr["EstadoRecepcion"].ToString(),
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public RecepcionContenedor ObtenerRecepcionDetalle(int idRecepcion)
        {
            RecepcionContenedor recep = null;
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlHead = @"
                    SELECT rc.*, t.NombreTemporada, a.NombreAlmacen, eq.NombreEquipo AS NombreEquipoReceptor
                    FROM dbo.RecepcionesContenedor rc
                    INNER JOIN dbo.Temporadas t ON rc.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Almacenes a ON rc.IdAlmacen = a.IdAlmacen
                    LEFT JOIN dbo.Equipos eq ON rc.IdEquipoReceptor = eq.IdEquipo
                    WHERE rc.IdRecepcion = @Id;";
                using (var cmd = new SqlCommand(sqlHead, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idRecepcion);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            recep = new RecepcionContenedor
                            {
                                IdRecepcion = Convert.ToInt32(dr["IdRecepcion"]),
                                NumeroContenedor = dr["NumeroContenedor"].ToString(),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                                NombreAlmacen = dr["NombreAlmacen"].ToString(),
                                IdEquipoReceptor = dr["IdEquipoReceptor"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoReceptor"]) : null,
                                NombreEquipoReceptor = dr["NombreEquipoReceptor"] != DBNull.Value ? dr["NombreEquipoReceptor"].ToString() : "",
                                FechaRecepcion = Convert.ToDateTime(dr["FechaRecepcion"]),
                                HoraRecepcion = dr["HoraRecepcion"] != DBNull.Value ? dr["HoraRecepcion"].ToString() : "",
                                ResponsableRecepcion = dr["ResponsableRecepcion"] != DBNull.Value ? dr["ResponsableRecepcion"].ToString() : "",
                                Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : "",
                                EstadoRecepcion = dr["EstadoRecepcion"].ToString(),
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                            };
                        }
                    }
                }
                if (recep == null) return null;

                // Cargar equipos servidos por el almacén
                string sqlEq = @"
                    SELECT e.NombreEquipo 
                    FROM dbo.AlmacenesEquipos ae
                    INNER JOIN dbo.Equipos e ON ae.IdEquipo = e.IdEquipo
                    WHERE ae.IdAlmacen = @IdAlm
                    ORDER BY e.NombreEquipo;";
                using (var cmdEq = new SqlCommand(sqlEq, cn))
                {
                    cmdEq.Parameters.AddWithValue("@IdAlm", recep.IdAlmacen);
                    using (var drEq = cmdEq.ExecuteReader())
                    {
                        while (drEq.Read())
                        {
                            recep.NombresEquiposAlmacen.Add(drEq["NombreEquipo"].ToString());
                        }
                    }
                }

                // Cargar Detalles de Materiales
                string sqlDet = @"
                    SELECT d.*, m.Codigo, m.NombreMaterial, m.UnidadEntrega, p.TipoEmpaque
                    FROM dbo.RecepcionesContenedorDetalle d
                    INNER JOIN dbo.Materiales m ON d.IdMaterial = m.IdMaterial
                    INNER JOIN dbo.PresentacionesMaterial p ON d.IdPresentacion = p.IdPresentacion
                    WHERE d.IdRecepcion = @Id;";
                using (var cmd = new SqlCommand(sqlDet, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idRecepcion);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            recep.Detalles.Add(new RecepcionContenedorDetalle
                            {
                                IdRecepcionDetalle = Convert.ToInt32(dr["IdRecepcionDetalle"]),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                TipoEmpaque = dr["TipoEmpaque"].ToString(),
                                CantidadEmpaques = Convert.ToInt32(dr["CantidadEmpaques"]),
                                UnidadesPorEmpaque = Convert.ToInt32(dr["UnidadesPorEmpaque"]),
                                CantidadTotalUnidades = Convert.ToInt32(dr["CantidadTotalUnidades"])
                            });
                        }
                    }
                }

                // Cargar Evidencias
                string sqlEv = @"
                    SELECT IdEvidencia, IdRecepcion, NombreArchivo, RutaArchivo, TipoContenido, TamanoBytes, FechaRegistro
                    FROM dbo.EvidenciasRecepcionContenedor
                    WHERE IdRecepcion = @Id
                    ORDER BY FechaRegistro;";
                using (var cmd = new SqlCommand(sqlEv, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idRecepcion);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            recep.Evidencias.Add(new EvidenciaRecepcion
                            {
                                IdEvidencia = Convert.ToInt32(dr["IdEvidencia"]),
                                IdRecepcion = Convert.ToInt32(dr["IdRecepcion"]),
                                NombreArchivo = dr["NombreArchivo"].ToString(),
                                RutaArchivo = dr["RutaArchivo"].ToString(),
                                TipoContenido = dr["TipoContenido"] != DBNull.Value ? dr["TipoContenido"].ToString() : "",
                                TamanoBytes = dr["TamanoBytes"] != DBNull.Value ? (long?)Convert.ToInt64(dr["TamanoBytes"]) : null,
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                            });
                        }
                    }
                }
            }
            return recep;
        }

        // =====================================================================
        // EVENTOS DE DESPACHO
        // =====================================================================

        public int CrearEventoDespacho(int idEvento, int idEquipo, int? idAlmacen, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Verificar que el evento existe y es de tipo Despacho
                int cnt = 0;
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Eventos WHERE IdEvento=@Id AND TipoEvento='Despacho';", cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEvento);
                    cnt = Convert.ToInt32(cmd.ExecuteScalar());
                }
                if (cnt == 0) throw new InvalidOperationException("El evento no existe o no es de tipo Despacho.");

                // Verificar que no esté ya registrado
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.EventosDespacho WHERE IdEvento=@Id;", cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEvento);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) throw new InvalidOperationException("El evento ya está registrado como evento de despacho.");
                }

                string sql = @"INSERT INTO dbo.EventosDespacho (IdEvento, IdAlmacen, IdEquipo, EstadoDespachoEvento)
                               OUTPUT INSERTED.IdEventoDespacho
                               VALUES (@IdEv, @IdAlm, @IdEq, 'PROGRAMADO');";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    cmd.Parameters.AddWithValue("@IdAlm", idAlmacen.HasValue ? (object)idAlmacen.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo);
                    int idCreado = Convert.ToInt32(cmd.ExecuteScalar());
                    AuditoriaHelper.Registrar("Evento Despacho", "Logistica", idEvento.ToString(), idUsuario, "Creación de evento de despacho");
                    return idCreado;
                }
            }
        }

        public List<EventoDespacho> ObtenerEventosDespacho(int? idTemporada = null, int? idEquipo = null)
        {
            var lista = new List<EventoDespacho>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT ed.*, e.NombreEvento, e.Fecha, e.Lugar, e.Hora, e.IdTemporada,
                           t.NombreTemporada, eq.NombreEquipo,
                           a.NombreAlmacen,
                           (SELECT COUNT(1) FROM dbo.DespachosIglesia d WHERE d.IdEvento = e.IdEvento) AS TotalIglesias,
                           (SELECT COUNT(1) FROM dbo.DespachosIglesia d WHERE d.IdEvento = e.IdEvento AND d.EstadoDespacho = 'DESPACHADA') AS TotalDespachadas
                    FROM dbo.EventosDespacho ed
                    INNER JOIN dbo.Eventos e ON ed.IdEvento = e.IdEvento
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Equipos eq ON ed.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.Almacenes a ON ed.IdAlmacen = a.IdAlmacen
                    WHERE (@IdTemp IS NULL OR e.IdTemporada = @IdTemp)
                      AND (@IdEq IS NULL OR ed.IdEquipo = @IdEq)
                    ORDER BY e.Fecha DESC;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int total = Convert.ToInt32(dr["TotalIglesias"]);
                            int despachadas = Convert.ToInt32(dr["TotalDespachadas"]);
                            lista.Add(new EventoDespacho
                            {
                                IdEventoDespacho = Convert.ToInt32(dr["IdEventoDespacho"]),
                                IdEvento = Convert.ToInt32(dr["IdEvento"]),
                                NombreEvento = dr["NombreEvento"].ToString(),
                                FechaEvento = dr["Fecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["Fecha"]) : null,
                                Lugar = dr["Lugar"] != DBNull.Value ? dr["Lugar"].ToString() : "",
                                Hora = dr["Hora"] != DBNull.Value ? dr["Hora"].ToString() : "",
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipo"].ToString(),
                                NombreAlmacen = dr["NombreAlmacen"] != DBNull.Value ? dr["NombreAlmacen"].ToString() : "",
                                EstadoDespachoEvento = dr["EstadoDespachoEvento"].ToString(),
                                TotalIglesiasAsignadas = total,
                                TotalIglesiasDespachadas = despachadas,
                                TotalIglesiasPendientes = total - despachadas
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public EventoDespacho ObtenerDetalleEventoDespacho(int idEvento)
        {
            var lista = ObtenerEventosDespacho();
            EventoDespacho ev = null;
            foreach (var e in lista) { if (e.IdEvento == idEvento) { ev = e; break; } }
            if (ev == null) return null;

            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Coordinadores
                string sqlCoord = @"
                    SELECT c.*, u.Correo AS CorreoUsuario
                    FROM dbo.CoordinadoresEventoDespacho c
                    INNER JOIN dbo.Usuarios u ON c.IdUsuario = u.IdUsuario
                    WHERE c.IdEvento = @IdEv;";
                using (var cmd = new SqlCommand(sqlCoord, cn))
                {
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ev.Coordinadores.Add(new CoordinadorEvento
                            {
                                IdCoordinadorEvento = Convert.ToInt32(dr["IdCoordinadorEvento"]),
                                IdEvento = idEvento,
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                CorreoUsuario = dr["CorreoUsuario"].ToString(),
                                Presente = Convert.ToBoolean(dr["Presente"])
                            });
                        }
                    }
                }

                // Iglesias asignadas
                string sqlIg = @"
                    SELECT di.*, ig.NombreIglesia,
                           CONCAT(ig.Calle, ' ', ig.Numero, ', ', ig.Sector, ', ', ig.Ciudad) AS DireccionIglesia,
                           e.NombreEvento
                    FROM dbo.DespachosIglesia di
                    INNER JOIN dbo.Iglesias ig ON di.IdIglesia = ig.IdIglesia
                    INNER JOIN dbo.Eventos e ON di.IdEvento = e.IdEvento
                    WHERE di.IdEvento = @IdEv
                    ORDER BY di.EstadoDespacho, ig.NombreIglesia;";
                using (var cmd = new SqlCommand(sqlIg, cn))
                {
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ev.Iglesias.Add(new DespachoIglesiaItem
                            {
                                IdDespachoIglesia = Convert.ToInt32(dr["IdDespachoIglesia"]),
                                NumeroComprobanteDespacho = dr["NumeroComprobanteDespacho"].ToString(),
                                IdEvento = idEvento,
                                NombreEvento = dr["NombreEvento"].ToString(),
                                IdParticipacion = Convert.ToInt32(dr["IdParticipacion"]),
                                IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                                NombreIglesia = dr["NombreIglesia"].ToString(),
                                DireccionIglesia = dr["DireccionIglesia"] != DBNull.Value ? dr["DireccionIglesia"].ToString() : "",
                                EstadoDespacho = dr["EstadoDespacho"].ToString(),
                                TipoReceptor = dr["TipoReceptor"] != DBNull.Value ? dr["TipoReceptor"].ToString() : "",
                                NombreReceptor = dr["NombreReceptor"] != DBNull.Value ? dr["NombreReceptor"].ToString() : "",
                                DocumentoIdentidadReceptor = dr["DocumentoIdentidadReceptor"] != DBNull.Value ? dr["DocumentoIdentidadReceptor"].ToString() : "",
                                MotivoNoDespacho = dr["MotivoNoDespacho"] != DBNull.Value ? dr["MotivoNoDespacho"].ToString() : "",
                                FechaHoraEntrega = dr["FechaHoraEntrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaHoraEntrega"]) : null,
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                            });
                        }
                    }
                }
            }
            return ev;
        }

        // =====================================================================
        // PROGRAMAR IGLESIA EN EVENTO DE DESPACHO
        // =====================================================================

        public int ProgramarIglesiaEnDespacho(int idEvento, int idParticipacion, int idIglesia, int idEquipo, int idTemporada, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Verificar no duplicado
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.DespachosIglesia WHERE IdEvento=@IdEv AND IdParticipacion=@IdPart;", cn))
                {
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        throw new InvalidOperationException("Esta iglesia ya está programada en este evento de despacho.");
                }

                // Verificar que la iglesia tenga recursos disponibles para despacho
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.AsignacionesRecursos WHERE IdParticipacion=@IdPart AND (EstadoAsignacion IS NULL OR EstadoAsignacion IN ('ASIGNADO','DISPONIBLE_PARA_DESPACHO'));", cn))
                {
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        throw new InvalidOperationException("La iglesia no tiene recursos asignados disponibles para despacho.");
                }

                // Obtener datos del Pastor y Líder para precarga
                string nomPastor = "", cedPastor = "", telPastor = "";
                string nomLider = "", cedLider = "", telLider = "";
                string sqlPersonas = @"
                    SELECT TipoPersona, CONCAT(Nombres, ' ', Apellidos) AS NombreCompleto,
                           DocumentoIdentidad, Celular
                    FROM dbo.PersonasIglesia WHERE IdIglesia=@IdIg AND TipoPersona IN ('Pastor','LiderMinisterial');";
                using (var cmd = new SqlCommand(sqlPersonas, cn))
                {
                    cmd.Parameters.AddWithValue("@IdIg", idIglesia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string tipo = dr["TipoPersona"].ToString();
                            if (tipo == "Pastor") { nomPastor = dr["NombreCompleto"].ToString(); cedPastor = dr["DocumentoIdentidad"] != DBNull.Value ? dr["DocumentoIdentidad"].ToString() : ""; telPastor = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : ""; }
                            else { nomLider = dr["NombreCompleto"].ToString(); cedLider = dr["DocumentoIdentidad"] != DBNull.Value ? dr["DocumentoIdentidad"].ToString() : ""; telLider = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : ""; }
                        }
                    }
                }

                string comprobante = "DSP-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);

                // Insertar registro de despacho en estado PROGRAMADA
                int idDespacho = 0;
                string sql = @"
                    INSERT INTO dbo.DespachosIglesia (NumeroComprobanteDespacho, IdEvento, IdParticipacion, IdIglesia, IdTemporada, IdEquipo, EstadoDespacho, FechaRegistro)
                    OUTPUT INSERTED.IdDespachoIglesia
                    VALUES (@Comp, @IdEv, @IdPart, @IdIg, @IdTemp, @IdEq, 'PROGRAMADA', GETDATE());";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Comp", comprobante);
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmd.Parameters.AddWithValue("@IdIg", idIglesia);
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo);
                    idDespacho = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Insertar detalle de materiales asignados
                string sqlMats = @"
                    SELECT 
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='OE') AS IdMat_OE,
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='MR') AS IdMat_MR,
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='GA') AS IdMat_GA,
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='GM') AS IdMat_GM,
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='NT') AS IdMat_NT,
                        (SELECT IdMaterial FROM dbo.Materiales WHERE Codigo='PO') AS IdMat_PO,
                        ar.OportunidadesEvangelisticas, ar.LibrosMejorRegalo, ar.LibrosAlumno, ar.LibrosMaestros, ar.NuevosTestamentos, ar.Posters
                    FROM dbo.AsignacionesRecursos ar WHERE ar.IdParticipacion = @IdPart;";
                using (var cmd = new SqlCommand(sqlMats, cn))
                {
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            var pares = new List<(int, int)>
                            {
                                (dr["IdMat_OE"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_OE"]) : 0, Convert.ToInt32(dr["OportunidadesEvangelisticas"])),
                                (dr["IdMat_MR"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_MR"]) : 0, Convert.ToInt32(dr["LibrosMejorRegalo"])),
                                (dr["IdMat_GA"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_GA"]) : 0, Convert.ToInt32(dr["LibrosAlumno"])),
                                (dr["IdMat_GM"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_GM"]) : 0, Convert.ToInt32(dr["LibrosMaestros"])),
                                (dr["IdMat_NT"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_NT"]) : 0, Convert.ToInt32(dr["NuevosTestamentos"])),
                                (dr["IdMat_PO"] != DBNull.Value ? Convert.ToInt32(dr["IdMat_PO"]) : 0, Convert.ToInt32(dr["Posters"]))
                            };
                            dr.Close();
                            string sqlDetIns = @"INSERT INTO dbo.DespachosIglesiaDetalle (IdDespachoIglesia, IdMaterial, CantidadAsignada, CantidadDespachada) VALUES (@IdD, @IdM, @Cant, 0);";
                            foreach (var (idMat, cant) in pares)
                            {
                                if (idMat == 0 || cant < 0) continue;
                                using (var cmdIns = new SqlCommand(sqlDetIns, cn))
                                {
                                    cmdIns.Parameters.AddWithValue("@IdD", idDespacho);
                                    cmdIns.Parameters.AddWithValue("@IdM", idMat);
                                    cmdIns.Parameters.AddWithValue("@Cant", cant);
                                    cmdIns.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

                // Actualizar estado de asignación
                using (var cmd = new SqlCommand(
                    "UPDATE dbo.AsignacionesRecursos SET EstadoAsignacion='PROGRAMADA_DESPACHO', IdEventoDespachoActual=@IdEv WHERE IdParticipacion=@IdPart;", cn))
                {
                    cmd.Parameters.AddWithValue("@IdEv", idEvento);
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmd.ExecuteNonQuery();
                }

                AuditoriaHelper.Registrar("Programar Despacho", "Logistica", idDespacho.ToString(), idUsuario, $"Programación de despacho {comprobante} para la iglesia ID {idIglesia}");
                return idDespacho;
            }
        }

        // =====================================================================
        // CONFIRMAR DESPACHO CON CÉDULA EN MANO (TRANSACCIÓN ACID Y CONTROL DE ROL CL)
        // =====================================================================

        public void ConfirmarDespacho(ConfirmarDespachoViewModel vm, int idEquipo, int idTemporada, int idUsuario, string nombreCoordinador, int? idRolSeguridad = null, int? idPosicion = null)
        {
            // Validar autorización estricta: Solo CL (IdPosicion == 6) o Admin (IdRolSeguridad in (1, 2))
            using (var cnAuth = new SqlConnection(ObtenerCadenaConexion()))
            {
                cnAuth.Open();
                bool esAutorizado = false;
                if (idRolSeguridad.HasValue && (idRolSeguridad.Value == 1 || idRolSeguridad.Value == 2))
                {
                    esAutorizado = true;
                }
                else if (idPosicion.HasValue && idPosicion.Value == 6)
                {
                    esAutorizado = true;
                }
                else
                {
                    string sqlCheckCL = @"
                        SELECT u.IdRolSeguridad, a.IdPosicion, p.NombrePosicion
                        FROM dbo.Usuarios u
                        LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                        LEFT JOIN dbo.PosicionesOCC p ON a.IdPosicion = p.IdPosicion
                        WHERE u.IdUsuario = @IdU;";
                    using (var cmdAuth = new SqlCommand(sqlCheckCL, cnAuth))
                    {
                        cmdAuth.Parameters.AddWithValue("@IdU", idUsuario);
                        using (var drAuth = cmdAuth.ExecuteReader())
                        {
                            if (drAuth.Read())
                            {
                                int rol = Convert.ToInt32(drAuth["IdRolSeguridad"]);
                                int? pos = drAuth["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(drAuth["IdPosicion"]) : null;
                                string nomPos = drAuth["NombrePosicion"] != DBNull.Value ? drAuth["NombrePosicion"].ToString() : "";
                                if (rol == 1 || rol == 2 || pos == 6 || nomPos.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0 || nomPos.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    esAutorizado = true;
                                }
                            }
                        }
                    }
                }

                if (!esAutorizado)
                {
                    throw new UnauthorizedAccessException("Acceso denegado: Únicamente el Coordinador de Logística (CL) tiene autorización para confirmar y ejecutar el despacho de materiales.");
                }
            }

            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var tran = cn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. Cargar datos actuales de la iglesia
                        string sqlLoad = @"
                            SELECT di.EstadoDespacho, di.IdParticipacion, di.IdIglesia, di.IdEvento,
                                   pastor.DocumentoIdentidad AS CedulaPastor,
                                   CONCAT(pastor.Nombres,' ',pastor.Apellidos) AS NombrePastor,
                                   pastor.Celular AS TelPastor,
                                   lider.DocumentoIdentidad AS CedulaLider,
                                   CONCAT(lider.Nombres,' ',lider.Apellidos) AS NombreLider,
                                   lider.Celular AS TelLider
                            FROM dbo.DespachosIglesia di
                            LEFT JOIN dbo.PersonasIglesia pastor ON pastor.IdIglesia = di.IdIglesia AND pastor.TipoPersona = 'Pastor'
                            LEFT JOIN dbo.PersonasIglesia lider ON lider.IdIglesia = di.IdIglesia AND lider.TipoPersona = 'LiderMinisterial'
                            WHERE di.IdDespachoIglesia = @Id;";

                        string nombreReceptor = "", cedulaFinal = "", telefonoReceptor = "";
                        int idParticipacion = 0, idIglesia = 0, idEvento = 0;
                        string estadoActual = "";

                        using (var cmd = new SqlCommand(sqlLoad, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", vm.IdDespachoIglesia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                if (!dr.Read()) throw new InvalidOperationException("No se encontró el despacho de la iglesia indicada.");
                                estadoActual = dr["EstadoDespacho"].ToString();
                                idParticipacion = Convert.ToInt32(dr["IdParticipacion"]);
                                idIglesia = Convert.ToInt32(dr["IdIglesia"]);
                                idEvento = Convert.ToInt32(dr["IdEvento"]);

                                string nomPast = dr["NombrePastor"] != DBNull.Value ? dr["NombrePastor"].ToString().Trim() : "";
                                string cedPast = dr["CedulaPastor"] != DBNull.Value ? dr["CedulaPastor"].ToString().Trim() : "";
                                string telPast = dr["TelPastor"] != DBNull.Value ? dr["TelPastor"].ToString().Trim() : "";

                                string nomLid = dr["NombreLider"] != DBNull.Value ? dr["NombreLider"].ToString().Trim() : "";
                                string cedLid = dr["CedulaLider"] != DBNull.Value ? dr["CedulaLider"].ToString().Trim() : "";
                                string telLid = dr["TelLider"] != DBNull.Value ? dr["TelLider"].ToString().Trim() : "";

                                if (vm.TipoReceptor == "LIDER_MINISTERIAL")
                                {
                                    nombreReceptor = !string.IsNullOrEmpty(vm.NombreReceptor) ? vm.NombreReceptor : nomLid;
                                    cedulaFinal = !string.IsNullOrEmpty(vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) ? (vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) : cedLid;
                                    telefonoReceptor = !string.IsNullOrEmpty(vm.TelefonoReceptor) ? vm.TelefonoReceptor : telLid;
                                }
                                else if (vm.TipoReceptor == "AMBOS")
                                {
                                    nombreReceptor = !string.IsNullOrEmpty(vm.NombreReceptor) ? vm.NombreReceptor : $"{nomPast} y {nomLid}".Trim();
                                    cedulaFinal = !string.IsNullOrEmpty(vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) ? (vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) : $"{cedPast} / {cedLid}";
                                    telefonoReceptor = !string.IsNullOrEmpty(vm.TelefonoReceptor) ? vm.TelefonoReceptor : $"{telPast}, {telLid}".Trim(',', ' ');
                                }
                                else // PASTOR
                                {
                                    nombreReceptor = !string.IsNullOrEmpty(vm.NombreReceptor) ? vm.NombreReceptor : nomPast;
                                    cedulaFinal = !string.IsNullOrEmpty(vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) ? (vm.DocumentoIdentidadReceptor ?? vm.DocumentoCedulaReceptor) : cedPast;
                                    telefonoReceptor = !string.IsNullOrEmpty(vm.TelefonoReceptor) ? vm.TelefonoReceptor : telPast;
                                }
                            }
                        }

                        if (estadoActual == "DESPACHADA") throw new InvalidOperationException("Esta iglesia ya fue despachada.");

                        // 2. Parsear cantidades
                        var cantidades = new Dictionary<string, int>();
                        if (!string.IsNullOrEmpty(vm.CantidadesJson))
                        {
                            try { cantidades = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(vm.CantidadesJson) ?? new Dictionary<string, int>(); } catch { }
                        }
                        if (!string.IsNullOrEmpty(vm.MaterialesJson))
                        {
                            try 
                            {
                                var listaMats = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DespachoDetalleMaterial>>(vm.MaterialesJson);
                                if (listaMats != null)
                                {
                                    foreach (var itemM in listaMats)
                                    {
                                        cantidades[itemM.IdMaterial.ToString()] = itemM.CantidadDespachada;
                                    }
                                }
                            }
                            catch { }
                        }

                        // 3. Actualizar detalles y descontar del inventario del equipo
                        string sqlDetSel = @"SELECT IdDespachoDetalle, IdMaterial, CantidadAsignada FROM dbo.DespachosIglesiaDetalle WHERE IdDespachoIglesia = @Id;";
                        var detalles = new List<(int IdDet, int IdMat, int CantAsig)>();
                        using (var cmd = new SqlCommand(sqlDetSel, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Id", vm.IdDespachoIglesia);
                            using (var dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                    detalles.Add((Convert.ToInt32(dr["IdDespachoDetalle"]), Convert.ToInt32(dr["IdMaterial"]), Convert.ToInt32(dr["CantidadAsignada"])));
                            }
                        }

                        foreach (var (idDet, idMat, cantAsig) in detalles)
                        {
                            cantidades.TryGetValue(idMat.ToString(), out int cantDesp);
                            if (cantDesp < 0) cantDesp = 0;
                            if (cantDesp > cantAsig) cantDesp = cantAsig; // No puede despachar más de lo asignado

                            // Verificar stock disponible en el equipo
                            int dispEq = 0;
                            using (var cmd = new SqlCommand("SELECT ISNULL(CantidadDisponible,0) FROM dbo.InventarioEquipo WHERE IdTemporada=@T AND IdEquipo=@Eq AND IdMaterial=@M;", cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@T", idTemporada);
                                cmd.Parameters.AddWithValue("@Eq", idEquipo);
                                cmd.Parameters.AddWithValue("@M", idMat);
                                object vl = cmd.ExecuteScalar();
                                dispEq = vl != null && vl != DBNull.Value ? Convert.ToInt32(vl) : 0;
                            }
                            if (cantDesp > dispEq)
                                throw new InvalidOperationException($"Stock insuficiente en el equipo para el material ID {idMat}. Disponible: {dispEq}, Solicitado: {cantDesp}.");

                            // Actualizar detalle
                            using (var cmd = new SqlCommand("UPDATE dbo.DespachosIglesiaDetalle SET CantidadDespachada=@Cant WHERE IdDespachoDetalle=@Id;", cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@Cant", cantDesp);
                                cmd.Parameters.AddWithValue("@Id", idDet);
                                cmd.ExecuteNonQuery();
                            }

                            if (cantDesp > 0)
                            {
                                // Descontar inventario del equipo
                                using (var cmd = new SqlCommand(@"
                                    UPDATE dbo.InventarioEquipo
                                    SET CantidadDespachada = CantidadDespachada + @Cant,
                                        CantidadAsignada = CantidadAsignada + @Cant,
                                        CantidadDisponible = CantidadDisponible - @Cant
                                    WHERE IdTemporada=@T AND IdEquipo=@Eq AND IdMaterial=@M;", cn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@Cant", cantDesp);
                                    cmd.Parameters.AddWithValue("@T", idTemporada);
                                    cmd.Parameters.AddWithValue("@Eq", idEquipo);
                                    cmd.Parameters.AddWithValue("@M", idMat);
                                    cmd.ExecuteNonQuery();
                                }

                                // Kárdex de salida
                                RegistrarMovimiento(cn, tran, idTemporada, "DESPACHO_IGLESIA", idMat,
                                    cantDesp, null, null, idEquipo, idIglesia,
                                    "DSP-" + vm.IdDespachoIglesia, idUsuario, "Despacho a iglesia ID " + idIglesia);
                            }
                        }

                        // 5. Actualizar encabezado del despacho
                        string sqlUpd = @"
                            UPDATE dbo.DespachosIglesia SET
                                EstadoDespacho = 'DESPACHADA',
                                TipoReceptor = @TipoR,
                                NombreReceptor = @NomR,
                                DocumentoIdentidadReceptor = @CedR,
                                TelefonoReceptor = @TelR,
                                FechaHoraEntrega = GETDATE(),
                                CoordinadorDespachador = @CoorD,
                                IdUsuarioDespacho = @IdUser,
                                Observaciones = @Obs
                            WHERE IdDespachoIglesia = @Id;";
                        using (var cmd = new SqlCommand(sqlUpd, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@TipoR", vm.TipoReceptor);
                            cmd.Parameters.AddWithValue("@NomR", (object)nombreReceptor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CedR", (object)cedulaFinal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TelR", (object)telefonoReceptor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CoorD", vm.CoordinadorDespachador ?? nombreCoordinador);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            cmd.Parameters.AddWithValue("@Obs", vm.Observaciones ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Id", vm.IdDespachoIglesia);
                            cmd.ExecuteNonQuery();
                        }

                        // 6. Actualizar estado de asignación de recursos → DESPACHADA
                        using (var cmd = new SqlCommand(
                            "UPDATE dbo.AsignacionesRecursos SET EstadoAsignacion='DESPACHADA', FechaDisponibleDespacho=GETDATE() WHERE IdParticipacion=@IdPart;", cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        AuditoriaHelper.Registrar("Despacho Confirmado", "Logistica", vm.IdDespachoIglesia.ToString(), idUsuario,
                            $"Iglesia ID {idIglesia} despachada. Receptor: {nombreReceptor} ({vm.TipoReceptor}).");
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        // =====================================================================
        // MARCAR IGLESIA COMO NO DESPACHADA (SIN DESCUENTO DE STOCK)
        // =====================================================================

        public void MarcarNoDespacho(NoDespachoBecauseViewModel vm, int idUsuario)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Verificar estado actual
                string estadoActual = "";
                int idParticipacion = 0;
                using (var cmd = new SqlCommand("SELECT EstadoDespacho, IdParticipacion FROM dbo.DespachosIglesia WHERE IdDespachoIglesia=@Id;", cn))
                {
                    cmd.Parameters.AddWithValue("@Id", vm.IdDespachoIglesia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) throw new InvalidOperationException("Despacho no encontrado.");
                        estadoActual = dr["EstadoDespacho"].ToString();
                        idParticipacion = Convert.ToInt32(dr["IdParticipacion"]);
                    }
                }
                if (estadoActual == "DESPACHADA") throw new InvalidOperationException("No se puede revertir un despacho ya confirmado.");

                // Actualizar sin tocar el inventario
                string sql = @"
                    UPDATE dbo.DespachosIglesia SET
                        EstadoDespacho = 'NO_DESPACHADA',
                        MotivoNoDespacho = @Motivo,
                        CoordinadorDespachador = @Coord,
                        IdUsuarioDespacho = @IdUser,
                        FechaHoraEntrega = GETDATE()
                    WHERE IdDespachoIglesia = @Id;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Motivo", vm.MotivoNoDespacho);
                    cmd.Parameters.AddWithValue("@Coord", vm.CoordinadorDespachador ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                    cmd.Parameters.AddWithValue("@Id", vm.IdDespachoIglesia);
                    cmd.ExecuteNonQuery();
                }

                // Restaurar a DISPONIBLE_PARA_DESPACHO para reprogramación
                using (var cmd = new SqlCommand(
                    "UPDATE dbo.AsignacionesRecursos SET EstadoAsignacion='DISPONIBLE_PARA_DESPACHO', IdEventoDespachoActual=NULL WHERE IdParticipacion=@IdPart;", cn))
                {
                    cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmd.ExecuteNonQuery();
                }

                AuditoriaHelper.Registrar("No Despachado", "Logistica", vm.IdDespachoIglesia.ToString(), idUsuario,
                    $"Iglesia marcada como NO DESPACHADA. Motivo: {vm.MotivoNoDespacho}");
            }
        }

        // =====================================================================
        // KARDEX DE MOVIMIENTOS
        // =====================================================================

        public List<MovimientoInventario> ObtenerKardex(int? idTemporada = null, int? idMaterial = null, int? idEquipo = null)
        {
            var lista = new List<MovimientoInventario>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT mv.*, t.NombreTemporada, m.Codigo AS CodigoMaterial, m.NombreMaterial,
                           ao.NombreAlmacen AS NombreAlmacenOrigen,
                           ad.NombreAlmacen AS NombreAlmacenDestino,
                           eq.NombreEquipo AS NombreEquipoDestino,
                           ig.NombreIglesia,
                           u.Correo AS CorreoUsuario
                    FROM dbo.MovimientosInventario mv
                    INNER JOIN dbo.Temporadas t ON mv.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Materiales m ON mv.IdMaterial = m.IdMaterial
                    LEFT JOIN dbo.Almacenes ao ON mv.IdAlmacenOrigen = ao.IdAlmacen
                    LEFT JOIN dbo.Almacenes ad ON mv.IdAlmacenDestino = ad.IdAlmacen
                    LEFT JOIN dbo.Equipos eq ON mv.IdEquipoDestino = eq.IdEquipo
                    LEFT JOIN dbo.Iglesias ig ON mv.IdIglesia = ig.IdIglesia
                    LEFT JOIN dbo.Usuarios u ON mv.IdUsuario = u.IdUsuario
                    WHERE (@IdTemp IS NULL OR mv.IdTemporada = @IdTemp)
                      AND (@IdMat IS NULL OR mv.IdMaterial = @IdMat)
                      AND (@IdEq IS NULL OR mv.IdEquipoDestino = @IdEq)
                    ORDER BY mv.FechaHora DESC;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada.HasValue ? (object)idTemporada.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdMat", idMaterial.HasValue ? (object)idMaterial.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new MovimientoInventario
                            {
                                IdMovimiento = Convert.ToInt32(dr["IdMovimiento"]),
                                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                TipoMovimiento = dr["TipoMovimiento"].ToString(),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["CodigoMaterial"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                Cantidad = Convert.ToInt32(dr["Cantidad"]),
                                NombreAlmacenOrigen = dr["NombreAlmacenOrigen"] != DBNull.Value ? dr["NombreAlmacenOrigen"].ToString() : "",
                                NombreAlmacenDestino = dr["NombreAlmacenDestino"] != DBNull.Value ? dr["NombreAlmacenDestino"].ToString() : "",
                                NombreEquipoDestino = dr["NombreEquipoDestino"] != DBNull.Value ? dr["NombreEquipoDestino"].ToString() : "",
                                NombreIglesia = dr["NombreIglesia"] != DBNull.Value ? dr["NombreIglesia"].ToString() : "",
                                IdDocumentoReferencia = dr["IdDocumentoReferencia"] != DBNull.Value ? dr["IdDocumentoReferencia"].ToString() : "",
                                FechaHora = Convert.ToDateTime(dr["FechaHora"]),
                                CorreoUsuario = dr["CorreoUsuario"] != DBNull.Value ? dr["CorreoUsuario"].ToString() : "",
                                Justificacion = dr["Justificacion"] != DBNull.Value ? dr["Justificacion"].ToString() : ""
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // =====================================================================
        // IGLESIAS DISPONIBLES PARA DESPACHO
        // =====================================================================

        public List<DespachoIglesiaItem> ObtenerIglesiasDisponiblesDespacho(int idEquipo, int idTemporada)
        {
            var lista = new List<DespachoIglesiaItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT p.IdParticipacion, i.IdIglesia, i.NombreIglesia,
                           CONCAT(i.Calle,' ',i.Numero,', ',i.Sector,', ',i.Ciudad) AS DireccionIglesia,
                           ar.EstadoAsignacion
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.AsignacionesRecursos ar ON ar.IdParticipacion = p.IdParticipacion
                    INNER JOIN dbo.Equipos eq ON i.IdEquipo = eq.IdEquipo
                    WHERE p.IdTemporada = @IdTemp
                      AND i.IdEquipo = @IdEq
                      AND ar.EstadoAsignacion IN ('ASIGNADO','DISPONIBLE_PARA_DESPACHO')
                    ORDER BY i.NombreIglesia;";
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                    cmd.Parameters.AddWithValue("@IdEq", idEquipo);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new DespachoIglesiaItem
                            {
                                IdParticipacion = Convert.ToInt32(dr["IdParticipacion"]),
                                IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                                NombreIglesia = dr["NombreIglesia"].ToString(),
                                DireccionIglesia = dr["DireccionIglesia"] != DBNull.Value ? dr["DireccionIglesia"].ToString() : "",
                                EstadoDespacho = dr["EstadoAsignacion"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // =====================================================================
        // HELPER PRIVADO: REGISTRO EN KÁRDEX
        // =====================================================================

        private void RegistrarMovimiento(SqlConnection cn, SqlTransaction tran, int idTemporada,
            string tipo, int idMaterial, int cantidad,
            int? idAlmacenOrigen, int? idAlmacenDestino, int? idEquipoDestino, int? idIglesia,
            string idDocRef, int idUsuario, string justificacion)
        {
            string sql = @"
                INSERT INTO dbo.MovimientosInventario (IdTemporada, TipoMovimiento, IdMaterial, Cantidad,
                    IdAlmacenOrigen, IdAlmacenDestino, IdEquipoDestino, IdIglesia, IdDocumentoReferencia, IdUsuario, Justificacion)
                VALUES (@IdTemp, @Tipo, @IdMat, @Cant, @IdAO, @IdAD, @IdEqD, @IdIg, @IdDoc, @IdUser, @Just);";
            using (var cmd = new SqlCommand(sql, cn, tran))
            {
                cmd.Parameters.AddWithValue("@IdTemp", idTemporada);
                cmd.Parameters.AddWithValue("@Tipo", tipo);
                cmd.Parameters.AddWithValue("@IdMat", idMaterial);
                cmd.Parameters.AddWithValue("@Cant", cantidad);
                cmd.Parameters.AddWithValue("@IdAO", idAlmacenOrigen.HasValue ? (object)idAlmacenOrigen.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IdAD", idAlmacenDestino.HasValue ? (object)idAlmacenDestino.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IdEqD", idEquipoDestino.HasValue ? (object)idEquipoDestino.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IdIg", idIglesia.HasValue ? (object)idIglesia.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@IdDoc", idDocRef ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                cmd.Parameters.AddWithValue("@Just", justificacion ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // =====================================================================
        // DESPACHO DETALLE PARA COMPROBANTE
        // =====================================================================

        public DespachoIglesiaItem ObtenerDespachoDetalle(int idDespachoIglesia)
        {
            DespachoIglesiaItem item = null;
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlHead = @"
                    SELECT di.*, ig.NombreIglesia,
                           CONCAT(ig.Calle,' ',ig.Numero,', ',ig.Sector,', ',ig.Ciudad) AS DireccionIglesia,
                           t.NombreTemporada, eq.NombreEquipo, e.NombreEvento,
                           CONCAT(pastor.Nombres, ' ', pastor.Apellidos) AS NombrePastor,
                           pastor.DocumentoIdentidad AS CedulaPastor,
                           pastor.Celular AS TelefonoPastor,
                           CONCAT(lider.Nombres, ' ', lider.Apellidos) AS NombreLiderMinisterial,
                           lider.DocumentoIdentidad AS CedulaLiderMinisterial,
                           lider.Celular AS TelefonoLiderMinisterial
                    FROM dbo.DespachosIglesia di
                    INNER JOIN dbo.Iglesias ig ON di.IdIglesia = ig.IdIglesia
                    INNER JOIN dbo.Temporadas t ON di.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Equipos eq ON di.IdEquipo = eq.IdEquipo
                    INNER JOIN dbo.Eventos e ON di.IdEvento = e.IdEvento
                    LEFT JOIN dbo.PersonasIglesia pastor ON ig.IdIglesia = pastor.IdIglesia AND pastor.TipoPersona = 'Pastor'
                    LEFT JOIN dbo.PersonasIglesia lider ON ig.IdIglesia = lider.IdIglesia AND lider.TipoPersona = 'LiderMinisterial'
                    WHERE di.IdDespachoIglesia = @Id;";
                using (var cmd = new SqlCommand(sqlHead, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idDespachoIglesia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            item = new DespachoIglesiaItem
                            {
                                IdDespachoIglesia = Convert.ToInt32(dr["IdDespachoIglesia"]),
                                NumeroComprobanteDespacho = dr["NumeroComprobanteDespacho"].ToString(),
                                IdEvento = Convert.ToInt32(dr["IdEvento"]),
                                NombreEvento = dr["NombreEvento"].ToString(),
                                IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                                NombreIglesia = dr["NombreIglesia"].ToString(),
                                DireccionIglesia = dr["DireccionIglesia"] != DBNull.Value ? dr["DireccionIglesia"].ToString() : "",
                                NombreTemporada = dr["NombreTemporada"].ToString(),
                                NombreEquipo = dr["NombreEquipo"].ToString(),
                                EstadoDespacho = dr["EstadoDespacho"].ToString(),
                                TipoReceptor = dr["TipoReceptor"] != DBNull.Value ? dr["TipoReceptor"].ToString() : "",
                                NombreReceptor = dr["NombreReceptor"] != DBNull.Value ? dr["NombreReceptor"].ToString() : "",
                                DocumentoIdentidadReceptor = dr["DocumentoIdentidadReceptor"] != DBNull.Value ? dr["DocumentoIdentidadReceptor"].ToString() : "",
                                TelefonoReceptor = dr["TelefonoReceptor"] != DBNull.Value ? dr["TelefonoReceptor"].ToString() : "",
                                FechaHoraEntrega = dr["FechaHoraEntrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaHoraEntrega"]) : null,
                                CoordinadorDespachador = dr["CoordinadorDespachador"] != DBNull.Value ? dr["CoordinadorDespachador"].ToString() : "",
                                MotivoNoDespacho = dr["MotivoNoDespacho"] != DBNull.Value ? dr["MotivoNoDespacho"].ToString() : "",
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                                NombrePastor = dr["NombrePastor"] != DBNull.Value ? dr["NombrePastor"].ToString().Trim() : "",
                                CedulaPastor = dr["CedulaPastor"] != DBNull.Value ? dr["CedulaPastor"].ToString() : "",
                                TelefonoPastor = dr["TelefonoPastor"] != DBNull.Value ? dr["TelefonoPastor"].ToString() : "",
                                NombreLiderMinisterial = dr["NombreLiderMinisterial"] != DBNull.Value ? dr["NombreLiderMinisterial"].ToString().Trim() : "",
                                CedulaLiderMinisterial = dr["CedulaLiderMinisterial"] != DBNull.Value ? dr["CedulaLiderMinisterial"].ToString() : "",
                                TelefonoLiderMinisterial = dr["TelefonoLiderMinisterial"] != DBNull.Value ? dr["TelefonoLiderMinisterial"].ToString() : ""
                            };
                        }
                    }
                }
                if (item == null) return null;

                // Sincronizar / auto-completar materiales asignados que falten en el detalle del despacho
                string sqlSync = @"
                    INSERT INTO dbo.DespachosIglesiaDetalle (IdDespachoIglesia, IdMaterial, CantidadAsignada, CantidadDespachada)
                    SELECT di.IdDespachoIglesia, m.IdMaterial, 
                           CASE m.Codigo
                               WHEN 'OE' THEN ISNULL(ar.OportunidadesEvangelisticas, 0)
                               WHEN 'MR' THEN ISNULL(ar.LibrosMejorRegalo, 0)
                               WHEN 'GA' THEN ISNULL(ar.LibrosAlumno, 0)
                               WHEN 'GM' THEN ISNULL(ar.LibrosMaestros, 0)
                               WHEN 'NT' THEN ISNULL(ar.NuevosTestamentos, 0)
                               WHEN 'PO' THEN ISNULL(ar.Posters, 0)
                               ELSE 0
                           END, 0
                    FROM dbo.DespachosIglesia di
                    INNER JOIN dbo.AsignacionesRecursos ar ON di.IdParticipacion = ar.IdParticipacion
                    CROSS JOIN dbo.Materiales m
                    WHERE di.IdDespachoIglesia = @Id
                      AND m.Codigo IN ('OE', 'MR', 'GA', 'GM', 'NT', 'PO')
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.DespachosIglesiaDetalle dd 
                          WHERE dd.IdDespachoIglesia = di.IdDespachoIglesia AND dd.IdMaterial = m.IdMaterial
                      );";
                using (var cmdSync = new SqlCommand(sqlSync, cn))
                {
                    cmdSync.Parameters.AddWithValue("@Id", idDespachoIglesia);
                    cmdSync.ExecuteNonQuery();
                }

                // Cargar materiales ordenados según flujo oficial
                string sqlDet = @"
                    SELECT d.*, m.Codigo, m.NombreMaterial, m.UnidadEntrega
                    FROM dbo.DespachosIglesiaDetalle d
                    INNER JOIN dbo.Materiales m ON d.IdMaterial = m.IdMaterial
                    WHERE d.IdDespachoIglesia = @Id
                    ORDER BY CASE m.Codigo
                        WHEN 'OE' THEN 1
                        WHEN 'MR' THEN 2
                        WHEN 'GA' THEN 3
                        WHEN 'GM' THEN 4
                        WHEN 'NT' THEN 5
                        WHEN 'PO' THEN 6
                        ELSE 7 END;";
                using (var cmd = new SqlCommand(sqlDet, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idDespachoIglesia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            item.Materiales.Add(new DespachoDetalleMaterial
                            {
                                IdDespachoDetalle = Convert.ToInt32(dr["IdDespachoDetalle"]),
                                IdMaterial = Convert.ToInt32(dr["IdMaterial"]),
                                CodigoMaterial = dr["Codigo"].ToString(),
                                NombreMaterial = dr["NombreMaterial"].ToString(),
                                UnidadEntrega = dr["UnidadEntrega"].ToString(),
                                CantidadAsignada = Convert.ToInt32(dr["CantidadAsignada"]),
                                CantidadDespachada = Convert.ToInt32(dr["CantidadDespachada"]),
                                CantidadNoDespachada = Convert.ToInt32(dr["CantidadNoDespachada"])
                            });
                        }
                    }
                }

                // Cargar datos de Pastor y Líder
                string sqlPersonas = @"
                    SELECT TipoPersona, CONCAT(Nombres,' ',Apellidos) AS NombreCompleto, DocumentoIdentidad, Celular
                    FROM dbo.PersonasIglesia WHERE IdIglesia=@IdIg AND TipoPersona IN ('Pastor','LiderMinisterial');";
                using (var cmd = new SqlCommand(sqlPersonas, cn))
                {
                    cmd.Parameters.AddWithValue("@IdIg", item.IdIglesia);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string tipo = dr["TipoPersona"].ToString();
                            string nom = dr["NombreCompleto"].ToString();
                            string ced = dr["DocumentoIdentidad"] != DBNull.Value ? dr["DocumentoIdentidad"].ToString() : "";
                            string tel = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : "";
                            if (tipo == "Pastor") { item.NombrePastor = nom; item.CedulaPastor = ced; item.TelefonoPastor = tel; }
                            else { item.NombreLiderMinisterial = nom; item.CedulaLiderMinisterial = ced; item.TelefonoLiderMinisterial = tel; }
                        }
                    }
                }
            }
            return item;
        }
    }
}
