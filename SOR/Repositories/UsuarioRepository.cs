using System;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;

namespace SOR.Repositories
{
    public class UsuarioRepository : BaseRepository
    {
        public Usuario ObtenerUsuarioPorId(int idUsuario)
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
                        COALESCE(a.IdEquipo, pco.IdEquipo) AS IdEquipo,
                        COALESCE(eq.NombreEquipo, eqPco.NombreEquipo) AS NombreEquipo,
                        COALESCE(neq.NombreNivel, neqPco.NombreNivel) AS NombreNivel,
                        COALESCE(neq.RangoJerarquico, neqPco.RangoJerarquico) AS RangoJerarquico,
                        COALESCE(a.IdPosicion, pco.IdPosicion) AS IdPosicion,
                        COALESCE(p.NombrePosicion, posPco.NombrePosicion) AS NombrePosicion,
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
                    LEFT JOIN dbo.Equipos eqPco ON pco.IdEquipo = eqPco.IdEquipo
                    LEFT JOIN dbo.NivelesEquipo neqPco ON eqPco.IdNivelEquipo = neqPco.IdNivelEquipo
                    LEFT JOIN dbo.PosicionesOCC posPco ON pco.IdPosicion = posPco.IdPosicion
                    WHERE u.IdUsuario = @IdUsuario;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdUsuario", idUsuario));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        usuario = MapearUsuarioDesdeReader(dr);
                    }
                }
            }

            return usuario;
        }

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
                        COALESCE(a.IdEquipo, pco.IdEquipo) AS IdEquipo,
                        COALESCE(eq.NombreEquipo, eqPco.NombreEquipo) AS NombreEquipo,
                        COALESCE(neq.NombreNivel, neqPco.NombreNivel) AS NombreNivel,
                        COALESCE(neq.RangoJerarquico, neqPco.RangoJerarquico) AS RangoJerarquico,
                        COALESCE(a.IdPosicion, pco.IdPosicion) AS IdPosicion,
                        COALESCE(p.NombrePosicion, posPco.NombrePosicion) AS NombrePosicion,
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
                    LEFT JOIN dbo.Equipos eqPco ON pco.IdEquipo = eqPco.IdEquipo
                    LEFT JOIN dbo.NivelesEquipo neqPco ON eqPco.IdNivelEquipo = neqPco.IdNivelEquipo
                    LEFT JOIN dbo.PosicionesOCC posPco ON pco.IdPosicion = posPco.IdPosicion
                    WHERE u.Correo = @Correo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Correo", correo));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        usuario = MapearUsuarioDesdeReader(dr);
                    }
                }
            }

            return usuario;
        }

        private static Usuario MapearUsuarioDesdeReader(SqlDataReader dr)
        {
            return new Usuario
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

        public bool RegistrarUsuario(string correo, string claveHash, out string mensaje)
        {
            bool registrado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // 1. Asegurar SET QUOTED_IDENTIFIER y ANSI_NULLS activos para la sesiÃ³n SQL
                using (var setCmd = new SqlCommand("SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;", cn))
                {
                    setCmd.ExecuteNonQuery();
                }

                // 2. Recrear/actualizar sp_RegistrarUsuario con QUOTED_IDENTIFIER ON explÃ­cito si fuese necesario
                string sqlFixSp = @"
                    CREATE OR ALTER PROCEDURE dbo.sp_RegistrarUsuario
                        @Correo VARCHAR(100),
                        @Clave VARCHAR(100),
                        @Registrado BIT OUTPUT,
                        @Mensaje VARCHAR(100) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        SET ANSI_NULLS ON;
                        SET QUOTED_IDENTIFIER ON;
                        
                        IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = @Correo)
                        BEGIN
                            SET @Registrado = 0;
                            SET @Mensaje = 'El correo ya se encuentra registrado.';
                            RETURN;
                        END

                        INSERT INTO dbo.Usuarios (Correo, Clave, IdRolSeguridad, IdEstado)
                        VALUES (@Correo, @Clave, 3, 1); -- Coordinador, PendienteAprobacionCorreo

                        SET @Registrado = 1;
                        SET @Mensaje = 'Usuario registrado con Ã©xito. Su cuenta estÃ¡ pendiente de aprobaciÃ³n por un administrador.';
                    END;";

                using (var cmdFix = new SqlCommand(sqlFixSp, cn))
                {
                    try { cmdFix.ExecuteNonQuery(); } catch { }
                }

                // 3. Ejecutar sp_RegistrarUsuario
                using (SqlCommand cmd = new SqlCommand("dbo.sp_RegistrarUsuario", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Correo", correo));
                    cmd.Parameters.Add(new SqlParameter("@Clave", claveHash));
                    cmd.Parameters.Add("@Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    registrado = Convert.ToBoolean(cmd.Parameters["@Registrado"].Value);
                    mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? string.Empty;
                }
            }

            return registrado;
        }

        public System.Collections.Generic.List<string> ObtenerCorreosCoordinadoresEquipo(int idEquipo)
        {
            var correos = new System.Collections.Generic.List<string>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // 1. Buscar Coordinadores de Equipo (IdPosicion = 1) activos en este equipo
                string sql = @"
                    SELECT DISTINCT u.Correo 
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario
                    WHERE a.IdEquipo = @IdEquipo 
                      AND a.IdPosicion = 1 
                      AND a.Activo = 1 
                      AND u.IdEstado = 4
                      AND u.Correo IS NOT NULL AND u.Correo <> '';";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add(new SqlParameter("@IdEquipo", idEquipo));
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string email = dr["Correo"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(email) && !correos.Contains(email))
                            {
                                correos.Add(email);
                            }
                        }
                    }
                }

                // 2. Si no hay Coordinador de Equipo especÃ­fico activo, notificar a los Superadmins y Administradores
                if (correos.Count == 0)
                {
                    string sqlAdmins = @"
                        SELECT DISTINCT Correo 
                        FROM dbo.Usuarios 
                        WHERE IdRolSeguridad IN (1, 2) 
                          AND IdEstado = 4 
                          AND Correo IS NOT NULL AND Correo <> '';";

                    using (SqlCommand cmdAdm = new SqlCommand(sqlAdmins, cn))
                    using (SqlDataReader drAdm = cmdAdm.ExecuteReader())
                    {
                        while (drAdm.Read())
                        {
                            string email = drAdm["Correo"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(email) && !correos.Contains(email))
                            {
                                correos.Add(email);
                            }
                        }
                    }
                }
            }
            return correos;
        }
    }
}
