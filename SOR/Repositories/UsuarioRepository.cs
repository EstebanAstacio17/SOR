using System;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;

namespace SOR.Repositories
{
    public class UsuarioRepository : BaseRepository
    {
        public Usuario ObtenerUsuarioPorCorreo(string correo)
        {
            Usuario usuario = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 
                        u.IdUsuario,
                        u.Correo,
                        u.Clave,
                        u.IdRolSeguridad,
                        r.NombreRol,
                        u.IdEstado,
                        e.NombreEstado,
                        a.IdEquipo,
                        eq.NombreEquipo,
                        neq.NombreNivel,
                        neq.RangoJerarquico,
                        a.IdPosicion,
                        p.NombrePosicion,
                        u.FechaRegistro,
                        pco.PrimerNombre,
                        pco.PrimerApellido
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.RolesSeguridad r ON u.IdRolSeguridad = r.IdRolSeguridad
                    INNER JOIN dbo.EstadosCuenta e ON u.IdEstado = e.IdEstado
                    LEFT JOIN dbo.PerfilesCoordinador pco ON u.IdUsuario = pco.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                    LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.NivelesEquipo neq ON eq.IdNivelEquipo = neq.IdNivelEquipo
                    LEFT JOIN dbo.PosicionesOCC p ON a.IdPosicion = p.IdPosicion
                    WHERE u.Correo = @Correo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Correo", correo);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        usuario = new Usuario
                        {
                            IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                            Correo = dr["Correo"].ToString(),
                            Clave = dr["Clave"].ToString(),
                            IdRolSeguridad = Convert.ToInt32(dr["IdRolSeguridad"]),
                            NombreRol = dr["NombreRol"].ToString(),
                            IdEstado = Convert.ToInt32(dr["IdEstado"]),
                            NombreEstado = dr["NombreEstado"].ToString(),
                            IdEquipo = dr["IdEquipo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipo"]) : null,
                            NombreEquipo = dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString() : null,
                            NombreNivel = dr["NombreNivel"] != DBNull.Value ? dr["NombreNivel"].ToString() : null,
                            RangoJerarquico = dr["RangoJerarquico"] != DBNull.Value ? (int?)Convert.ToInt32(dr["RangoJerarquico"]) : null,
                            IdPosicion = dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null,
                            NombrePosicion = dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString() : null,
                            FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaRegistro"]) : null,
                            PrimerNombre = dr["PrimerNombre"] != DBNull.Value ? dr["PrimerNombre"].ToString() : null,
                            PrimerApellido = dr["PrimerApellido"] != DBNull.Value ? dr["PrimerApellido"].ToString() : null
                        };
                    }
                }
            }

            return usuario;
        }

        public bool RegistrarUsuario(string correo, string claveHash, out string mensaje)
        {
            bool registrado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                SqlCommand cmd = new SqlCommand("sp_RegistrarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", correo);
                cmd.Parameters.AddWithValue("Clave", claveHash);
                cmd.Parameters.Add("Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                cmd.ExecuteNonQuery();

                registrado = Convert.ToBoolean(cmd.Parameters["Registrado"].Value);
                mensaje = cmd.Parameters["Mensaje"].Value.ToString();
            }

            return registrado;
        }
    }
}
