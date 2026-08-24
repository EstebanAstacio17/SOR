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

        private readonly Services.UsuarioService _usuarioService = new Services.UsuarioService();

        // GET: Acceso/Login
        public ActionResult Login(string mensaje)
        {
            if (!string.IsNullOrEmpty(mensaje))
            {
                if (mensaje == "SesionExpirada")
                {
                    ViewData["Mensaje"] = "Su sesión ha expirado por inactividad (5 minutos). Por favor inicie sesión nuevamente.";
                    ViewData["TipoAlert"] = "alert-warning";
                }
                else if (mensaje == "PermisosModificados")
                {
                    ViewData["Mensaje"] = "Tus permisos, rol o estado de cuenta fueron actualizados por un administrador. Por favor inicia sesión nuevamente.";
                    ViewData["TipoAlert"] = "alert-warning";
                }
                else if (mensaje == "SesionDuplicada")
                {
                    ViewData["Mensaje"] = "Se ha detectado un inicio de sesión en otra ventana o navegador. Por seguridad, solo se permite una sesión activa a la vez por usuario.";
                    ViewData["TipoAlert"] = "alert-danger";
                }
                else if (mensaje == "RegistroPendiente")
                {
                    ViewData["Mensaje"] = "Su solicitud de registro se envió a aprobación. Debe estar en espera de que un administrador la apruebe.";
                    ViewData["TipoAlert"] = "alert-info";
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
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(Usuario oUsuario)
        {
            if (oUsuario.Clave != oUsuario.ConfirmarClave)
            {
                ViewData["Mensaje"] = "Las Contraseñas no coinciden";
                ViewData["TipoAlert"] = "alert-danger";
                return View();
            }

            string mensaje;
            bool registrado = _usuarioService.RegistrarUsuario(oUsuario, out mensaje);

            ViewData["Mensaje"] = mensaje;
            ViewData["TipoAlert"] = "alert-danger";

            if (registrado)
            {
                return RedirectToAction("Login", "Acceso", new { mensaje = "RegistroPendiente" });
            }
            else
            {
                return View();
            }
        }

        private void ObtenerDatosSeguridad(string correo, out int intentos, out DateTime? ultimoIntento, out DateTime? bloqueo, out string claveGuardada, out int? idEstado)
        {
            intentos = 0;
            ultimoIntento = null;
            bloqueo = null;
            claveGuardada = null;
            idEstado = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IntentosFallidosLogin, FechaUltimoIntentoFallido, FechaBloqueo, Clave, IdEstado FROM dbo.Usuarios WHERE Correo = @Correo;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Correo", correo);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        intentos = dr["IntentosFallidosLogin"] != DBNull.Value ? Convert.ToInt32(dr["IntentosFallidosLogin"]) : 0;
                        ultimoIntento = dr["FechaUltimoIntentoFallido"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaUltimoIntentoFallido"]) : null;
                        bloqueo = dr["FechaBloqueo"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaBloqueo"]) : null;
                        claveGuardada = dr["Clave"].ToString();
                        idEstado = Convert.ToInt32(dr["IdEstado"]);
                    }
                }
            }
        }

        private void ActualizarDatosSeguridad(string correo, int intentos, DateTime? ultimoIntento, DateTime? bloqueo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Usuarios SET IntentosFallidosLogin = @Intentos, FechaUltimoIntentoFallido = @UltimoIntento, FechaBloqueo = @Bloqueo WHERE Correo = @Correo;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Intentos", intentos);
                cmd.Parameters.AddWithValue("@UltimoIntento", (object)ultimoIntento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bloqueo", (object)bloqueo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Correo", correo);

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Usuario oUsuario)
        {
            // Validar correo
            if (string.IsNullOrWhiteSpace(oUsuario.Correo))
            {
                ViewData["Mensaje"] = "Debe ingresar el correo.";
                return View();
            }

            int intentos;
            DateTime? ultimoIntento;
            DateTime? bloqueo;
            string claveGuardada;
            int? idEstado;

            ObtenerDatosSeguridad(oUsuario.Correo, out intentos, out ultimoIntento, out bloqueo, out claveGuardada, out idEstado);

            // Si el usuario no existe en la base de datos
            if (claveGuardada == null)
            {
                ViewData["Mensaje"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            // Si el usuario está en estado Aprobado Restablecimiento (8), redirigir directamente
            if (idEstado == 8)
            {
                TempData["CorreoValido"] = oUsuario.Correo;
                return RedirectToAction("CambiarClave");
            }

            // Validar contraseña para el resto de los estados
            if (string.IsNullOrWhiteSpace(oUsuario.Clave))
            {
                ViewData["Mensaje"] = "Debe ingresar la contraseña.";
                return View();
            }

            // Validar si la cuenta está bloqueada por 1 hora
            if (bloqueo.HasValue)
            {
                double minutosBloqueados = (DateTime.Now - bloqueo.Value).TotalMinutes;
                if (minutosBloqueados < 60)
                {
                    int minutosRestantes = 60 - (int)minutosBloqueados;
                    ViewData["Mensaje"] = $"Su cuenta está bloqueada temporalmente debido a múltiples intentos fallidos. Intente nuevamente en {minutosRestantes} minutos.";
                    return View();
                }
                else
                {
                    // Ha pasado más de 1 hora, desbloquear en base de datos
                    intentos = 0;
                    ultimoIntento = null;
                    bloqueo = null;
                    ActualizarDatosSeguridad(oUsuario.Correo, 0, null, null);
                }
            }

            // Validar contraseña
            if (Helpers.Criptografia.VerificarClave(oUsuario.Clave, claveGuardada))
            {
                // Limpiar intentos fallidos al iniciar sesión con éxito
                ActualizarDatosSeguridad(oUsuario.Correo, 0, null, null);

                Usuario usuarioValidador = _usuarioService.ValidarUsuario(oUsuario.Correo, oUsuario.Clave);

                if (usuarioValidador != null)
                {
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
                    else if (usuarioValidador.IdEstado == 7) // Pendiente Restablecimiento
                    {
                        ViewData["Mensaje"] = "Su solicitud de restablecimiento de contraseña fue enviada y está pendiente de aprobación por el administrador. Por favor espere.";
                        return View();
                    }

                    // Generar token único de sesión para garantizar sesión única activa
                    string tokenSesion = Guid.NewGuid().ToString();
                    Session["SesionToken"] = tokenSesion;
                    SesionesActivas[usuarioValidador.IdUsuario] = tokenSesion;

                    Session["usuario"] = usuarioValidador;
                    Session["UltimoAcceso"] = DateTime.Now;
                    Session["RecienLogueado"] = true;
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // Contraseña incorrecta, manejar intentos fallidos
                int nuevosIntentos = 1;
                DateTime ahora = DateTime.Now;

                if (ultimoIntento.HasValue && (ahora - ultimoIntento.Value).TotalMinutes < 5)
                {
                    nuevosIntentos = intentos + 1;
                }

                if (nuevosIntentos >= 3)
                {
                    // Bloquear por 1 hora
                    ActualizarDatosSeguridad(oUsuario.Correo, nuevosIntentos, ahora, ahora);
                    ViewData["Mensaje"] = "Su cuenta ha sido bloqueada por 1 hora debido a 3 intentos fallidos de inicio de sesión.";
                }
                else
                {
                    ActualizarDatosSeguridad(oUsuario.Correo, nuevosIntentos, ahora, null);
                    int restantes = 3 - nuevosIntentos;
                    ViewData["Mensaje"] = $"Usuario o contraseña incorrectos. Le quedan {restantes} intentos antes de bloquear la cuenta.";
                }
            }

            return View();
        }

        // GET: Acceso/RecuperarClave
        public ActionResult RecuperarClave()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecuperarClave(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                ViewData["Mensaje"] = "Debe ingresar su correo electrónico.";
                return View();
            }

            int intentos;
            DateTime? ultimoIntento;
            DateTime? bloqueo;
            string claveGuardada;
            int? idEstado;

            ObtenerDatosSeguridad(correo, out intentos, out ultimoIntento, out bloqueo, out claveGuardada, out idEstado);

            // Si el correo no existe
            if (claveGuardada == null)
            {
                ViewData["Mensaje"] = "El correo electrónico no se encuentra registrado en el sistema.";
                return View();
            }

            // Cambiar estado a Pendiente Restablecimiento (7) y limpiar tokens
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    UPDATE dbo.Usuarios 
                    SET IdEstado = 7, 
                        TokenRecuperacion = NULL, 
                        ExpiracionTokenRecuperacion = NULL 
                    WHERE Correo = @Correo;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Correo", correo);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Tu solicitud de restablecimiento ha sido enviada al administrador. Una vez aprobada, podrás ingresar para colocar tu nueva contraseña.";
            return RedirectToAction("Login");
        }

        // GET: Acceso/CambiarClave
        public ActionResult CambiarClave()
        {
            string correo = TempData["CorreoValido"] as string;

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Correo = correo;
            TempData.Keep("CorreoValido");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarClave(string correo, string nuevaClave, string confirmarClave)
        {
            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
            {
                ViewData["Mensaje"] = "Todos los campos de contraseña son obligatorios.";
                ViewBag.Correo = correo;
                return View();
            }

            if (nuevaClave != confirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden.";
                ViewBag.Correo = correo;
                return View();
            }

            if (nuevaClave.Length < 6)
            {
                ViewData["Mensaje"] = "La contraseña debe tener al menos 6 caracteres.";
                ViewBag.Correo = correo;
                return View();
            }

            // Validar que el usuario siga en estado Aprobado Restablecimiento (8)
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sqlCheck = "SELECT IdEstado FROM dbo.Usuarios WHERE Correo = @Correo;";
                SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn);
                cmdCheck.Parameters.AddWithValue("@Correo", correo);

                cn.Open();
                object stateObj = cmdCheck.ExecuteScalar();
                if (stateObj == null || Convert.ToInt32(stateObj) != 8)
                {
                    ViewData["Mensaje"] = "Esta solicitud de cambio de contraseña ya no es válida o no ha sido aprobada.";
                    return View();
                }
            }

            // Hashear nueva contraseña con Salt único
            string nuevaClaveFormateada = Helpers.Criptografia.CrearClaveFormateada(nuevaClave);

            // Guardar nueva clave, restablecer cuenta a Activo (4), limpiar intentos fallidos
            // y auto-restaurar la asignación de equipo si fue desactivada durante el proceso
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar clave y estado
                        string sqlUpdate = @"
                            UPDATE dbo.Usuarios 
                            SET Clave = @Clave, 
                                IdEstado = 4,
                                TokenRecuperacion = NULL, 
                                ExpiracionTokenRecuperacion = NULL,
                                IntentosFallidosLogin = 0,
                                FechaUltimoIntentoFallido = NULL,
                                FechaBloqueo = NULL 
                            WHERE Correo = @Correo;";
                        using (SqlCommand cmd = new SqlCommand(sqlUpdate, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Clave", nuevaClaveFormateada);
                            cmd.Parameters.AddWithValue("@Correo", correo);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Auto-restaurar asignación de equipo si el perfil tiene equipo/posición
                        //    pero no existe asignación activa en AsignacionesEquipo
                        string sqlRestoreCheck = @"
                            SELECT u.IdUsuario, p.IdEquipo, p.IdPosicion
                            FROM dbo.Usuarios u
                            INNER JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                            WHERE u.Correo = @Correo 
                              AND p.IdEquipo IS NOT NULL 
                              AND p.IdPosicion IS NOT NULL
                              AND NOT EXISTS (
                                  SELECT 1 FROM dbo.AsignacionesEquipo a 
                                  WHERE a.IdUsuario = u.IdUsuario AND a.Activo = 1
                              );";
                        using (SqlCommand cmdRestore = new SqlCommand(sqlRestoreCheck, cn, tran))
                        {
                            cmdRestore.Parameters.AddWithValue("@Correo", correo);
                            using (SqlDataReader dr = cmdRestore.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    int idUsuario = Convert.ToInt32(dr["IdUsuario"]);
                                    int idEquipo = Convert.ToInt32(dr["IdEquipo"]);
                                    int idPosicion = Convert.ToInt32(dr["IdPosicion"]);
                                    dr.Close();

                                    // Insertar nueva asignación activa
                                    string sqlIns = "INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo) VALUES (@IdUsuario, @IdEquipo, @IdPosicion, 1);";
                                    using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                                    {
                                        cmdIns.Parameters.AddWithValue("@IdUsuario", idUsuario);
                                        cmdIns.Parameters.AddWithValue("@IdEquipo", idEquipo);
                                        cmdIns.Parameters.AddWithValue("@IdPosicion", idPosicion);
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        ViewData["Mensaje"] = "Ocurrió un error al restablecer la contraseña. Por favor intente nuevamente.";
                        ViewBag.Correo = correo;
                        return View();
                    }
                }
            }

            TempData["MensajeExito"] = "Contraseña restablecida con éxito. Inicie sesión ahora con su nueva clave.";
            return RedirectToAction("Login");
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
