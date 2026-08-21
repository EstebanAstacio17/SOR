using SOR.Models;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace SOR.Controllers
{
    public class AccesoController : Controller
    {
        // Control de Sesión Única Activa por Usuario
        public static readonly ConcurrentDictionary<int, string> SesionesActivas = new ConcurrentDictionary<int, string>();

        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Acceso/Login
        public ActionResult Login(string mensaje)
        {
            if (!string.IsNullOrEmpty(mensaje))
            {
                if (mensaje == "SesionExpirada")
                {
                    ViewData["Mensaje"] = "Su sesión ha expirado por inactividad (5 minutos). Por favor inicie sesión nuevamente.";
                }
                else if (mensaje == "PermisosModificados")
                {
                    ViewData["Mensaje"] = "Tus permisos, rol o estado de cuenta fueron actualizados por un administrador. Por favor inicia sesión nuevamente.";
                }
                else if (mensaje == "SesionDuplicada")
                {
                    ViewData["Mensaje"] = "Se ha detectado un inicio de sesión en otra ventana o navegador. Por seguridad, solo se permite una sesión activa a la vez por usuario.";
                }
            }
            return View();
        }


        // GET: Acceso/Registrar
        public ActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registrar(Usuario oUsuario)
        {
            bool registrado;
            string mensaje;

            if (oUsuario.Clave == oUsuario.ConfirmarClave)
            {
                oUsuario.Clave = ConvertirSha256(oUsuario.Clave);
            }
            else
            {
                ViewData["Mensaje"] = "Las Contraseñas no coinciden";
                return View();
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                SqlCommand cmd = new SqlCommand("sp_RegistrarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.Parameters.Add("Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                cmd.ExecuteNonQuery();

                registrado = Convert.ToBoolean(cmd.Parameters["Registrado"].Value);
                mensaje = cmd.Parameters["Mensaje"].Value.ToString();
            }

            ViewData["Mensaje"] = mensaje;

            if (registrado)
            {
                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Login(Usuario oUsuario)
        {
            // Validar correo
            if (string.IsNullOrWhiteSpace(oUsuario.Correo))
            {
                ViewData["Mensaje"] = "Debe ingresar el correo.";
                return View();
            }

            // Validar contraseña
            if (string.IsNullOrWhiteSpace(oUsuario.Clave))
            {
                ViewData["Mensaje"] = "Debe ingresar la contraseña.";
                return View();
            }

            oUsuario.Clave = ConvertirSha256(oUsuario.Clave);
            Usuario usuarioValidador = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                SqlCommand cmd = new SqlCommand("sp_ValidarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int idUsuario = Convert.ToInt32(dr[0]);
                        string correo = TieneColumna(dr, "Correo") ? dr["Correo"].ToString() : oUsuario.Correo;
                        int idRol = TieneColumna(dr, "IdRolSeguridad") ? Convert.ToInt32(dr["IdRolSeguridad"]) : 3;
                        string nombreRol = TieneColumna(dr, "NombreRol") ? dr["NombreRol"].ToString() : "Coordinador";
                        int idEstado = TieneColumna(dr, "IdEstado") ? Convert.ToInt32(dr["IdEstado"]) : 4; // Activo por defecto si es SP antiguo
                        string nombreEstado = TieneColumna(dr, "NombreEstado") ? dr["NombreEstado"].ToString() : "Activo";
                        int? idEquipo = TieneColumna(dr, "IdEquipo") && dr["IdEquipo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipo"]) : null;
                        string nombreEquipo = TieneColumna(dr, "NombreEquipo") && dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString() : null;
                        string nombreNivel = TieneColumna(dr, "NombreNivel") && dr["NombreNivel"] != DBNull.Value ? dr["NombreNivel"].ToString() : null;
                        int? rangoJerarquico = TieneColumna(dr, "RangoJerarquico") && dr["RangoJerarquico"] != DBNull.Value ? (int?)Convert.ToInt32(dr["RangoJerarquico"]) : null;
                        int? idPosicion = TieneColumna(dr, "IdPosicion") && dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null;
                        string nombrePosicion = TieneColumna(dr, "NombrePosicion") && dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString() : null;

                        usuarioValidador = new Usuario
                        {
                            IdUsuario = idUsuario,
                            Correo = correo,
                            IdRolSeguridad = idRol,
                            NombreRol = nombreRol,
                            IdEstado = idEstado,
                            NombreEstado = nombreEstado,
                            IdEquipo = idEquipo,
                            NombreEquipo = nombreEquipo,
                            NombreNivel = nombreNivel,
                            RangoJerarquico = rangoJerarquico,
                            IdPosicion = idPosicion,
                            NombrePosicion = nombrePosicion
                        };

                    }
                }
            }


            if (usuarioValidador != null)
            {
                // Verificar si la cuenta está pendiente de aprobación de correo
                if (usuarioValidador.IdEstado == 1) // PendienteAprobacionCorreo
                {
                    ViewData["Mensaje"] = "Su solicitud de registro fue enviada y está pendiente de aprobación por un administrador.";
                    return View();
                }
                else if (usuarioValidador.IdEstado == 5) // Rechazado
                {
                    ViewData["Mensaje"] = "Su cuenta ha sido rechazada por la administración.";
                    return View();
                }
                else if (usuarioValidador.IdEstado == 6) // Suspendido
                {
                    ViewData["Mensaje"] = "Su cuenta se encuentra suspendida.";
                    return View();
                }

                // Generar token único de sesión para garantizar sesión única activa
                string tokenSesion = Guid.NewGuid().ToString();
                Session["SesionToken"] = tokenSesion;
                SesionesActivas[usuarioValidador.IdUsuario] = tokenSesion;

                Session["usuario"] = usuarioValidador;
                Session["UltimoAcceso"] = DateTime.Now;
                return RedirectToAction("Index", "Home");
            }

            ViewData["Mensaje"] = "Usuario o contraseña incorrectos.";
            return View();
        }

        public ActionResult CerrarSesion()
        {
            if (Session["usuario"] != null)
            {
                Usuario u = (Usuario)Session["usuario"];
                string dummy;
                SesionesActivas.TryRemove(u.IdUsuario, out dummy);
            }
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Acceso");
        }

        public static string ConvertirSha256(string texto)
        {
            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        private static bool TieneColumna(SqlDataReader reader, string nombreColumna)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(nombreColumna, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
