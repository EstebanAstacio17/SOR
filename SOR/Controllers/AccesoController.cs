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
        // Control de SesiÃ³n Ãšnica Activa por Usuario
        public static readonly ConcurrentDictionary<int, string> SesionesActivas = new ConcurrentDictionary<int, string>();

        private static string ObtenerCadenaConexion()
        {
            return SOR.Helpers.ConnectionHelper.ObtenerCadenaConexion();
        }

        private readonly Services.UsuarioService _usuarioService = new Services.UsuarioService();

        // GET: Acceso/Login
        public ActionResult Login(string mensaje)
        {
            if (!string.IsNullOrEmpty(mensaje))
            {
                if (mensaje == "SesionExpirada")
                {
                    ViewData["Mensaje"] = "Su sesiÃ³n ha expirado por inactividad (5 minutos). Por favor inicie sesiÃ³n nuevamente.";
                    ViewData["TipoAlert"] = "alert-warning";
                }
                else if (mensaje == "PermisosModificados")
                {
                    ViewData["Mensaje"] = "Tus permisos, rol o estado de cuenta fueron actualizados por un administrador. Por favor inicia sesiÃ³n nuevamente.";
                    ViewData["TipoAlert"] = "alert-warning";
                }
                else if (mensaje == "SesionDuplicada")
                {
                    ViewData["Mensaje"] = "Se ha detectado un inicio de sesiÃ³n en otra ventana o navegador. Por seguridad, solo se permite una sesiÃ³n activa a la vez por usuario.";
                    ViewData["TipoAlert"] = "alert-danger";
                }
                else if (mensaje == "RegistroPendiente")
                {
                    ViewData["Mensaje"] = "Su solicitud de registro se enviÃ³ a aprobaciÃ³n. Debe estar en espera de que un administrador la apruebe.";
                    ViewData["TipoAlert"] = "alert-info";
                }
            }
            return View();
        }


        // GET: Acceso/Registrar
        public ActionResult Registrar()
        {
            ViewBag.FormTimeToken = DateTime.UtcNow.Ticks.ToString();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(Usuario oUsuario, string website_trap, string form_time_token)
        {
            // 1. DetecciÃ³n Anti-Bot: Campo Honeypot Trampa (debe llegar vacÃ­o)
            if (!string.IsNullOrEmpty(website_trap))
            {
                // Silenciosamente simular Ã©xito para no alertar al atacante
                return RedirectToAction("Login", "Acceso", new { mensaje = "RegistroPendiente" });
            }

            // 2. DetecciÃ³n Anti-Bot: EnvÃ­o instantÃ¡neo inhumano (< 1.2 segundos)
            if (long.TryParse(form_time_token, out long ticksFormulario))
            {
                var tiempoTranscurrido = DateTime.UtcNow - new DateTime(ticksFormulario, DateTimeKind.Utc);
                if (tiempoTranscurrido.TotalMilliseconds < 1200)
                {
                    // Solicitud enviada en menos de 1.2s (script automatizado)
                    return RedirectToAction("Login", "Acceso", new { mensaje = "RegistroPendiente" });
                }
            }

            // 3. ValidaciÃ³n obligatoria de contraseÃ±as en el servidor
            if (string.IsNullOrWhiteSpace(oUsuario.Clave) || oUsuario.Clave != oUsuario.ConfirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseÃ±as no coinciden o estÃ¡n vacÃ­as.";
                ViewData["TipoAlert"] = "alert-danger";
                ViewBag.FormTimeToken = DateTime.UtcNow.Ticks.ToString();
                return View();
            }

            // 4. ValidaciÃ³n de longitud mÃ­nima de contraseÃ±a
            if (oUsuario.Clave.Length < 6)
            {
                ViewData["Mensaje"] = "La contraseÃ±a debe contener al menos 6 caracteres.";
                ViewData["TipoAlert"] = "alert-danger";
                ViewBag.FormTimeToken = DateTime.UtcNow.Ticks.ToString();
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
                ViewBag.FormTimeToken = DateTime.UtcNow.Ticks.ToString();
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
                cmd.Parameters.Add(new SqlParameter("@Correo", correo));

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
                cmd.Parameters.Add(new SqlParameter("@Intentos", intentos));
                cmd.Parameters.Add(new SqlParameter("@UltimoIntento", (object)ultimoIntento ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Bloqueo", (object)bloqueo ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Correo", correo));

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

            int intentos = 0;
            DateTime? ultimoIntento = null;
            DateTime? bloqueo = null;
            string claveGuardada = null;
            int? idEstado = null;

            try
            {
                ObtenerDatosSeguridad(oUsuario.Correo, out intentos, out ultimoIntento, out bloqueo, out claveGuardada, out idEstado);
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = "Error de conexiÃ³n BD: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                ViewData["TipoAlert"] = "alert-danger";
                return View();
            }

            // Si el usuario no existe en la base de datos
            if (claveGuardada == null)
            {
                ViewData["Mensaje"] = "Usuario o contraseÃ±a incorrectos.";
                return View();
            }

            // Si el usuario estÃ¡ en estado Aprobado Restablecimiento (8), redirigir directamente
            if (idEstado == 8)
            {
                TempData["CorreoValido"] = oUsuario.Correo;
                return RedirectToAction("CambiarClave");
            }

            // Validar contraseÃ±a para el resto de los estados
            if (string.IsNullOrWhiteSpace(oUsuario.Clave))
            {
                ViewData["Mensaje"] = "Debe ingresar la contraseÃ±a.";
                return View();
            }

            // Validar si la cuenta estÃ¡ bloqueada por 1 hora
            if (bloqueo.HasValue)
            {
                double minutosBloqueados = (DateTime.Now - bloqueo.Value).TotalMinutes;
                if (minutosBloqueados < 60)
                {
                    int minutosRestantes = 60 - (int)minutosBloqueados;
                    ViewData["Mensaje"] = $"Su cuenta estÃ¡ bloqueada temporalmente debido a mÃºltiples intentos fallidos. Intente nuevamente en {minutosRestantes} minutos.";
                    return View();
                }
                else
                {
                    // Ha pasado mÃ¡s de 1 hora, desbloquear en base de datos
                    intentos = 0;
                    ultimoIntento = null;
                    bloqueo = null;
                    ActualizarDatosSeguridad(oUsuario.Correo, 0, null, null);
                }
            }

            // Validar contraseÃ±a
            if (Helpers.Criptografia.VerificarClave(oUsuario.Clave, claveGuardada))
            {
                // Limpiar intentos fallidos al iniciar sesiÃ³n con Ã©xito
                ActualizarDatosSeguridad(oUsuario.Correo, 0, null, null);

                Usuario usuarioValidador = _usuarioService.ValidarUsuario(oUsuario.Correo, oUsuario.Clave);

                if (usuarioValidador != null)
                {
                    if (usuarioValidador.IdEstado == 1) // PendienteAprobacionCorreo
                    {
                        ViewData["Mensaje"] = "Su solicitud de registro fue enviada y estÃ¡ pendiente de aprobaciÃ³n por un administrador.";
                        return View();
                    }
                    else if (usuarioValidador.IdEstado == 5) // Rechazado
                    {
                        ViewData["Mensaje"] = "Su cuenta ha sido rechazada por la administraciÃ³n.";
                        return View();
                    }
                    else if (usuarioValidador.IdEstado == 6) // Suspendido
                    {
                        ViewData["Mensaje"] = "Su cuenta se encuentra suspendida.";
                        return View();
                    }
                    else if (usuarioValidador.IdEstado == 7) // Pendiente Restablecimiento
                    {
                        ViewData["Mensaje"] = "Su solicitud de restablecimiento de contraseÃ±a fue enviada y estÃ¡ pendiente de aprobaciÃ³n por el administrador. Por favor espere.";
                        return View();
                    }

                    // Generar token Ãºnico de sesiÃ³n para garantizar sesiÃ³n Ãºnica activa
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
                // ContraseÃ±a incorrecta, manejar intentos fallidos
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
                    
                    // Registrar alerta de seguridad en auditorÃ­a
                    SOR.Helpers.AuditoriaHelper.Registrar(
                        null,
                        oUsuario.Correo,
                        "BLOQUEO_CUENTA_FUERZA_BRUTA",
                        "ACCESO_SEGURIDAD",
                        null,
                        $"La cuenta '{oUsuario.Correo}' fue bloqueada temporalmente por 1 hora tras registrar mÃºltiples intentos fallidos de inicio de sesiÃ³n."
                    );

                    ViewData["Mensaje"] = "Su cuenta ha sido bloqueada por 1 hora debido a 3 intentos fallidos de inicio de sesiÃ³n.";
                }
                else
                {
                    ActualizarDatosSeguridad(oUsuario.Correo, nuevosIntentos, ahora, null);
                    int restantes = 3 - nuevosIntentos;
                    ViewData["Mensaje"] = $"Usuario o contraseÃ±a incorrectos. Le quedan {restantes} intentos antes de bloquear la cuenta.";
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
                ViewData["Mensaje"] = "Debe ingresar su correo electrÃ³nico.";
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
                ViewData["Mensaje"] = "El correo electrÃ³nico no se encuentra registrado en el sistema.";
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
                cmd.Parameters.Add(new SqlParameter("@Correo", correo));

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Tu solicitud de restablecimiento ha sido enviada al administrador. Una vez aprobada, podrÃ¡s ingresar para colocar tu nueva contraseÃ±a.";
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
                ViewData["Mensaje"] = "Todos los campos de contraseÃ±a son obligatorios.";
                ViewBag.Correo = correo;
                return View();
            }

            if (nuevaClave != confirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseÃ±as no coinciden.";
                ViewBag.Correo = correo;
                return View();
            }

            if (nuevaClave.Length < 6)
            {
                ViewData["Mensaje"] = "La contraseÃ±a debe tener al menos 6 caracteres.";
                ViewBag.Correo = correo;
                return View();
            }

            // Validar que el usuario siga en estado Aprobado Restablecimiento (8)
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sqlCheck = "SELECT IdEstado FROM dbo.Usuarios WHERE Correo = @Correo;";
                SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn);
                cmdCheck.Parameters.Add(new SqlParameter("@Correo", correo));

                cn.Open();
                object stateObj = cmdCheck.ExecuteScalar();
                if (stateObj == null || Convert.ToInt32(stateObj) != 8)
                {
                    ViewData["Mensaje"] = "Esta solicitud de cambio de contraseÃ±a ya no es vÃ¡lida o no ha sido aprobada.";
                    return View();
                }
            }

            // Hashear nueva contraseÃ±a con Salt Ãºnico
            string nuevaClaveFormateada = Helpers.Criptografia.CrearClaveFormateada(nuevaClave);

            // Guardar nueva clave, restablecer cuenta a Activo (4), limpiar intentos fallidos
            // y auto-restaurar la asignaciÃ³n de equipo si fue desactivada durante el proceso
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
                            cmd.Parameters.Add(new SqlParameter("@Clave", nuevaClaveFormateada));
                            cmd.Parameters.Add(new SqlParameter("@Correo", correo));
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Auto-restaurar asignaciÃ³n de equipo si el perfil tiene equipo/posiciÃ³n
                        //    pero no existe asignaciÃ³n activa en AsignacionesEquipo
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
                            cmdRestore.Parameters.Add(new SqlParameter("@Correo", correo));
                            using (SqlDataReader dr = cmdRestore.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    int idUsuario = Convert.ToInt32(dr["IdUsuario"]);
                                    int idEquipo = Convert.ToInt32(dr["IdEquipo"]);
                                    int idPosicion = Convert.ToInt32(dr["IdPosicion"]);
                                    dr.Close();

                                    // Insertar nueva asignaciÃ³n activa
                                    string sqlIns = "INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo) VALUES (@IdUsuario, @IdEquipo, @IdPosicion, 1);";
                                    using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                                    {
                                        cmdIns.Parameters.Add(new SqlParameter("@IdUsuario", idUsuario));
                                        cmdIns.Parameters.Add(new SqlParameter("@IdEquipo", idEquipo));
                                        cmdIns.Parameters.Add(new SqlParameter("@IdPosicion", idPosicion));
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
                        ViewData["Mensaje"] = "OcurriÃ³ un error al restablecer la contraseÃ±a. Por favor intente nuevamente.";
                        ViewBag.Correo = correo;
                        return View();
                    }
                }
            }

            TempData["MensajeExito"] = "ContraseÃ±a restablecida con Ã©xito. Inicie sesiÃ³n ahora con su nueva clave.";
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
