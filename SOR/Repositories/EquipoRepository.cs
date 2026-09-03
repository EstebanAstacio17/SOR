using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;
using SOR.Helpers;

namespace SOR.Repositories
{
    public class EquipoRepository : BaseRepository
    {
        public List<EquipoConDetalles> ListarEquipos()
        {
            List<EquipoConDetalles> lista = new List<EquipoConDetalles>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 
                        e.IdEquipo, 
                        e.NombreEquipo, 
                        e.IdNivelEquipo, 
                        n.NombreNivel, 
                        n.RangoJerarquico, 
                        e.IdEquipoPadre,
                        ep.NombreEquipo AS NombreEquipoPadre,
                        e.Activo
                    FROM dbo.Equipos e
                    INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo
                    LEFT JOIN dbo.Equipos ep ON e.IdEquipoPadre = ep.IdEquipo
                    ORDER BY n.RangoJerarquico, e.NombreEquipo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new EquipoConDetalles
                        {
                            IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                            NombreEquipo = dr["NombreEquipo"].ToString(),
                            IdNivelEquipo = Convert.ToInt32(dr["IdNivelEquipo"]),
                            NombreNivel = dr["NombreNivel"].ToString(),
                            RangoJerarquico = Convert.ToInt32(dr["RangoJerarquico"]),
                            IdEquipoPadre = dr["IdEquipoPadre"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoPadre"]) : null,
                            NombreEquipoPadre = dr["NombreEquipoPadre"] != DBNull.Value ? dr["NombreEquipoPadre"].ToString() : "Nivel Nacional (Sin Padre)",
                            Activo = dr["Activo"] != DBNull.Value ? Convert.ToBoolean(dr["Activo"]) : true,
                            RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null
                        });
                    }
                }
            }

            return lista;
        }

        public EquipoConDetalles ObtenerEquipoPorId(int idEquipo)
        {
            EquipoConDetalles equipo = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 
                        e.IdEquipo, 
                        e.NombreEquipo, 
                        e.IdNivelEquipo, 
                        n.NombreNivel, 
                        n.RangoJerarquico, 
                        e.IdEquipoPadre,
                        ep.NombreEquipo AS NombreEquipoPadre,
                        e.Activo
                    FROM dbo.Equipos e
                    INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo
                    LEFT JOIN dbo.Equipos ep ON e.IdEquipoPadre = ep.IdEquipo
                    WHERE e.IdEquipo = @IdEquipo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        equipo = new EquipoConDetalles
                        {
                            IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                            NombreEquipo = dr["NombreEquipo"].ToString(),
                            IdNivelEquipo = Convert.ToInt32(dr["IdNivelEquipo"]),
                            NombreNivel = dr["NombreNivel"].ToString(),
                            RangoJerarquico = Convert.ToInt32(dr["RangoJerarquico"]),
                            IdEquipoPadre = dr["IdEquipoPadre"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoPadre"]) : null,
                            NombreEquipoPadre = dr["NombreEquipoPadre"] != DBNull.Value ? dr["NombreEquipoPadre"].ToString() : "Nivel Nacional (Sin Padre)",
                            Activo = dr["Activo"] != DBNull.Value ? Convert.ToBoolean(dr["Activo"]) : true,
                            RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null
                        };
                    }
                }
            }

            return equipo;
        }

        public int InsertarEquipo(EquipoConDetalles equipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    INSERT INTO dbo.Equipos (NombreEquipo, IdNivelEquipo, IdEquipoPadre)
                    VALUES (@NombreEquipo, @IdNivelEquipo, @IdEquipoPadre);
                    SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@NombreEquipo", equipo.NombreEquipo);
                cmd.Parameters.AddWithValue("@IdNivelEquipo", equipo.IdNivelEquipo);
                cmd.Parameters.AddWithValue("@IdEquipoPadre", equipo.IdEquipoPadre ?? (object)DBNull.Value);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool ActualizarEquipo(EquipoConDetalles equipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    UPDATE dbo.Equipos 
                    SET NombreEquipo = @NombreEquipo, 
                        IdNivelEquipo = @IdNivelEquipo, 
                        IdEquipoPadre = @IdEquipoPadre,
                        Activo = @Activo,
                        FechaModificacion = GETUTCDATE()
                    WHERE IdEquipo = @IdEquipo
                      AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@NombreEquipo", equipo.NombreEquipo);
                cmd.Parameters.AddWithValue("@IdNivelEquipo", equipo.IdNivelEquipo);
                cmd.Parameters.AddWithValue("@IdEquipoPadre", equipo.IdEquipoPadre ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Activo", equipo.Activo);
                var pRowVer = new SqlParameter("@RowVersion", SqlDbType.Timestamp);
                pRowVer.Value = (equipo.RowVersion != null && equipo.RowVersion.Length > 0) ? (object)equipo.RowVersion : DBNull.Value;
                cmd.Parameters.Add(pRowVer);
                cmd.Parameters.AddWithValue("@IdEquipo", equipo.IdEquipo);

                cn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    throw new System.Data.DBConcurrencyException("El equipo fue modificado concurrentemente por otro usuario. Actualice la página antes de continuar.");
                }
                SOR.Helpers.AuditoriaHelper.Registrar(null, "", "UPDATE", "Equipo", equipo.IdEquipo.ToString(), "Actualización de equipo: " + equipo.NombreEquipo);
                return true;
            }
        }

        public bool EliminarEquipo(int idEquipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Obtener datos del equipo a eliminar
                        int? idPadre = null;
                        int idNivel = 0;
                        string nombre = "";

                        using (SqlCommand cmdGet = new SqlCommand("SELECT NombreEquipo, IdNivelEquipo, IdEquipoPadre FROM dbo.Equipos WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdGet.Parameters.AddWithValue("@Id", idEquipo);
                            using (SqlDataReader dr = cmdGet.ExecuteReader())
                            {
                                if (!dr.Read()) throw new InvalidOperationException("El equipo especificado no existe.");
                                nombre = dr["NombreEquipo"].ToString();
                                idNivel = Convert.ToInt32(dr["IdNivelEquipo"]);
                                idPadre = dr["IdEquipoPadre"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipoPadre"]) : null;
                            }
                        }

                        if (idNivel == 1)
                        {
                            throw new InvalidOperationException("No se puede eliminar el Equipo Nacional de Liderazgo.");
                        }

                        int idSustituto = (idPadre.HasValue && idPadre.Value > 0) ? idPadre.Value : 1;

                        // 2. Reasignar sub-equipos dependientes al equipo superior
                        using (SqlCommand cmdSub = new SqlCommand("UPDATE dbo.Equipos SET IdEquipoPadre = @IdSustituto WHERE IdEquipoPadre = @Id;", cn, tran))
                        {
                            cmdSub.Parameters.AddWithValue("@IdSustituto", (object)idPadre ?? DBNull.Value);
                            cmdSub.Parameters.AddWithValue("@Id", idEquipo);
                            cmdSub.ExecuteNonQuery();
                        }

                        // 3. Reasignar Iglesias asociadas
                        using (SqlCommand cmdIg = new SqlCommand("UPDATE dbo.Iglesias SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdIg.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdIg.Parameters.AddWithValue("@Id", idEquipo);
                            cmdIg.ExecuteNonQuery();
                        }

                        // 4. Reasignar Perfiles de Coordinador
                        using (SqlCommand cmdPerf = new SqlCommand("UPDATE dbo.PerfilesCoordinador SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdPerf.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdPerf.Parameters.AddWithValue("@Id", idEquipo);
                            cmdPerf.ExecuteNonQuery();
                        }

                        // 5. Limpiar Asignaciones de Equipo
                        using (SqlCommand cmdAsig = new SqlCommand("DELETE FROM dbo.AsignacionesEquipo WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdAsig.Parameters.AddWithValue("@Id", idEquipo);
                            cmdAsig.ExecuteNonQuery();
                        }

                        // 6. Reasignar módulos complementarios
                        using (SqlCommand cmdEos1 = new SqlCommand("UPDATE dbo.EOS_IglesiasPlantadas SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdEos1.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdEos1.Parameters.AddWithValue("@Id", idEquipo);
                            cmdEos1.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdEos2 = new SqlCommand("UPDATE dbo.EOS_GruposNoAlcanzados SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdEos2.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdEos2.Parameters.AddWithValue("@Id", idEquipo);
                            cmdEos2.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdEos3 = new SqlCommand("UPDATE dbo.EOS_MentoreoViajes SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdEos3.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdEos3.Parameters.AddWithValue("@Id", idEquipo);
                            cmdEos3.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdEd = new SqlCommand("UPDATE dbo.EventosDespacho SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdEd.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdEd.Parameters.AddWithValue("@Id", idEquipo);
                            cmdEd.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdDi = new SqlCommand("UPDATE dbo.DespachosIglesia SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdDi.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdDi.Parameters.AddWithValue("@Id", idEquipo);
                            cmdDi.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdAlm = new SqlCommand("DELETE FROM dbo.AlmacenesEquipos WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdAlm.Parameters.AddWithValue("@Id", idEquipo);
                            cmdAlm.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdInv = new SqlCommand("DELETE FROM dbo.InventarioEquipo WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdInv.Parameters.AddWithValue("@Id", idEquipo);
                            cmdInv.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdFinP = new SqlCommand("UPDATE dbo.Finanzas_PresupuestosAprobados SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdFinP.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdFinP.Parameters.AddWithValue("@Id", idEquipo);
                            cmdFinP.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdFinT = new SqlCommand("UPDATE dbo.Finanzas_Transacciones SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdFinT.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdFinT.Parameters.AddWithValue("@Id", idEquipo);
                            cmdFinT.ExecuteNonQuery();
                        }
                        using (SqlCommand cmdFinR = new SqlCommand("UPDATE dbo.FinanzasReportes SET IdEquipo = @IdSustituto WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdFinR.Parameters.AddWithValue("@IdSustituto", idSustituto);
                            cmdFinR.Parameters.AddWithValue("@Id", idEquipo);
                            cmdFinR.ExecuteNonQuery();
                        }

                        // 7. Eliminar físicamente el equipo
                        using (SqlCommand cmdDel = new SqlCommand("DELETE FROM dbo.Equipos WHERE IdEquipo = @Id;", cn, tran))
                        {
                            cmdDel.Parameters.AddWithValue("@Id", idEquipo);
                            int del = cmdDel.ExecuteNonQuery();
                            if (del == 0) throw new InvalidOperationException("No se pudo eliminar el equipo.");
                        }

                        SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, 1, "admin@occrd.org", "DELETE", "Equipo", idEquipo.ToString(), "Eliminación completa de equipo: " + nombre);

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

        public int ObtenerCantidadDependencias(int idEquipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 
                        (SELECT COUNT(1) FROM dbo.Equipos WHERE IdEquipoPadre = @IdEquipo) +
                        (SELECT COUNT(1) FROM dbo.PerfilesCoordinador WHERE IdEquipo = @IdEquipo) +
                        (SELECT COUNT(1) FROM dbo.AsignacionesEquipo WHERE IdEquipo = @IdEquipo) +
                        (SELECT COUNT(1) FROM dbo.Iglesias WHERE IdEquipo = @IdEquipo);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void DesactivarEquipo(int idEquipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Equipos SET Activo = 0 WHERE IdEquipo = @IdEquipo;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<NivelEquipo> ListarNiveles()
        {
            List<NivelEquipo> lista = new List<NivelEquipo>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdNivelEquipo, NombreNivel, RangoJerarquico FROM dbo.NivelesEquipo ORDER BY RangoJerarquico;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new NivelEquipo
                        {
                            IdNivelEquipo = Convert.ToInt32(dr["IdNivelEquipo"]),
                            NombreNivel = dr["NombreNivel"].ToString(),
                            RangoJerarquico = Convert.ToInt32(dr["RangoJerarquico"])
                        });
                    }
                }
            }

            return lista;
        }

        public bool EsHijoDe(int idPadre, int idHijo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT COUNT(1) FROM dbo.Equipos WHERE IdEquipo = @IdHijo AND IdEquipoPadre = @IdPadre;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdHijo", idHijo);
                cmd.Parameters.AddWithValue("@IdPadre", idPadre);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
    }
}
