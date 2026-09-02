using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace SOR.Helpers
{
    public static class AuditoriaHelper
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        public static void Registrar(int? idUsuario, string correoUsuario, string accion, string modulo, string idRegistroAfectado, string detalles)
        {
            try
            {
                string ip = "";
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    ip = HttpContext.Current.Request.UserHostAddress ?? "";
                }

                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        INSERT INTO dbo.AuditoriaGeneral (IdUsuario, CorreoUsuario, Accion, Modulo, IdRegistroAfectado, Detalles, DireccionIP)
                        VALUES (@IdUsuario, @Correo, @Accion, @Modulo, @IdReg, @Detalles, @IP);";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.HasValue ? (object)idUsuario.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Correo", string.IsNullOrEmpty(correoUsuario) ? (object)DBNull.Value : correoUsuario);
                        cmd.Parameters.AddWithValue("@Accion", accion ?? "OPERACION");
                        cmd.Parameters.AddWithValue("@Modulo", modulo ?? "GENERAL");
                        cmd.Parameters.AddWithValue("@IdReg", string.IsNullOrEmpty(idRegistroAfectado) ? (object)DBNull.Value : idRegistroAfectado);
                        cmd.Parameters.AddWithValue("@Detalles", string.IsNullOrEmpty(detalles) ? (object)DBNull.Value : detalles);
                        cmd.Parameters.AddWithValue("@IP", string.IsNullOrEmpty(ip) ? (object)DBNull.Value : ip);

                        cn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al registrar auditoría: " + ex.Message);
            }
        }

        public static void Registrar(SqlConnection cn, SqlTransaction tran, int? idUsuario, string correoUsuario, string accion, string modulo, string idRegistroAfectado, string detalles)
        {
            try
            {
                string ip = "";
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    ip = HttpContext.Current.Request.UserHostAddress ?? "";
                }

                string sql = @"
                    INSERT INTO dbo.AuditoriaGeneral (IdUsuario, CorreoUsuario, Accion, Modulo, IdRegistroAfectado, Detalles, DireccionIP)
                    VALUES (@IdUsuario, @Correo, @Accion, @Modulo, @IdReg, @Detalles, @IP);";

                using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.HasValue ? (object)idUsuario.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", string.IsNullOrEmpty(correoUsuario) ? (object)DBNull.Value : correoUsuario);
                    cmd.Parameters.AddWithValue("@Accion", accion ?? "OPERACION");
                    cmd.Parameters.AddWithValue("@Modulo", modulo ?? "GENERAL");
                    cmd.Parameters.AddWithValue("@IdReg", string.IsNullOrEmpty(idRegistroAfectado) ? (object)DBNull.Value : idRegistroAfectado);
                    cmd.Parameters.AddWithValue("@Detalles", string.IsNullOrEmpty(detalles) ? (object)DBNull.Value : detalles);
                    cmd.Parameters.AddWithValue("@IP", string.IsNullOrEmpty(ip) ? (object)DBNull.Value : ip);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al registrar auditoría: " + ex.Message);
            }
        }

        /// <summary>Sobrecarga de conveniencia para módulos que solo tienen el IdUsuario (sin correo).</summary>
        public static void Registrar(string accion, string modulo, string idRegistroAfectado, int idUsuario, string detalles)
        {
            Registrar(idUsuario, null, accion, modulo, idRegistroAfectado, detalles);
        }
    }
}
