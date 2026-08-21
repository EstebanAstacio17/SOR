using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;

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
                            Activo = dr["Activo"] != DBNull.Value ? Convert.ToBoolean(dr["Activo"]) : true
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
                            Activo = dr["Activo"] != DBNull.Value ? Convert.ToBoolean(dr["Activo"]) : true
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
                        Activo = @Activo
                    WHERE IdEquipo = @IdEquipo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@NombreEquipo", equipo.NombreEquipo);
                cmd.Parameters.AddWithValue("@IdNivelEquipo", equipo.IdNivelEquipo);
                cmd.Parameters.AddWithValue("@IdEquipoPadre", equipo.IdEquipoPadre ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Activo", equipo.Activo);
                cmd.Parameters.AddWithValue("@IdEquipo", equipo.IdEquipo);

                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool EliminarEquipo(int idEquipo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "DELETE FROM dbo.Equipos WHERE IdEquipo = @IdEquipo;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);

                cn.Open();
                try
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547)
                    {
                        throw new InvalidOperationException("No se puede eliminar físicamente el equipo porque tiene datos asociados.");
                    }
                    throw;
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
