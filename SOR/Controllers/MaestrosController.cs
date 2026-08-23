using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class MaestrosController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Maestros
        public ActionResult Index(int? idIglesia = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            List<Maestro> lista = new List<Maestro>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT m.*, i.NombreIglesia, i.IdEquipo AS IdEquipoIglesia
                    FROM dbo.Maestros m
                    LEFT JOIN dbo.Iglesias i ON m.IdIglesia = i.IdIglesia
                    ORDER BY m.Nombres, m.Apellidos;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Maestro
                        {
                            IdMaestro = Convert.ToInt32(dr["IdMaestro"]),
                            IdIglesia = dr["IdIglesia"] != DBNull.Value ? Convert.ToInt32(dr["IdIglesia"]) : 0,
                            NombreIglesia = dr["NombreIglesia"] != DBNull.Value ? dr["NombreIglesia"].ToString() : "",
                            IdEquipoIglesia = dr["IdEquipoIglesia"] != DBNull.Value ? Convert.ToInt32(dr["IdEquipoIglesia"]) : 0,
                            Nombres = dr["Nombres"].ToString(),
                            Apellidos = dr["Apellidos"].ToString(),
                            DocumentoIdentidad = dr["DocumentoIdentidad"] != DBNull.Value ? dr["DocumentoIdentidad"].ToString() : "",
                            Celular = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : "",
                            Correo = dr["Correo"] != DBNull.Value ? dr["Correo"].ToString() : "",
                            Activo = Convert.ToBoolean(dr["Activo"])
                        });
                    }
                }
            }

            HashSet<int> equiposPermitidos = new HashSet<int>();
            if (u != null && u.IdEquipo.HasValue)
            {
                equiposPermitidos.Add(u.IdEquipo.Value);
                ObtenerEquiposHijosRecursivo(u.IdEquipo.Value, equiposPermitidos);
            }
            ViewBag.EquiposPermitidos = equiposPermitidos;

            CargarIglesiasDisponibles();
            ViewBag.UsuarioActual = u;
            ViewBag.IdIglesiaPreseleccionada = idIglesia;
            if (idIglesia.HasValue && idIglesia.Value > 0)
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sqlIg = "SELECT i.NombreIglesia, i.IdEquipo, e.NombreEquipo FROM dbo.Iglesias i INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo WHERE i.IdIglesia = @Id;";
                    SqlCommand cmdIg = new SqlCommand(sqlIg, cn);
                    cmdIg.Parameters.AddWithValue("@Id", idIglesia.Value);
                    cn.Open();
                    using (SqlDataReader drIg = cmdIg.ExecuteReader())
                    {
                        if (drIg.Read())
                        {
                            ViewBag.NombreIglesiaPreseleccionada = drIg["NombreIglesia"].ToString();
                            ViewBag.IdEquipoIglesiaPreseleccionada = Convert.ToInt32(drIg["IdEquipo"]);
                            ViewBag.NombreEquipoIglesiaPreseleccionada = drIg["NombreEquipo"].ToString();
                        }
                    }
                }
            }
            return View(lista);
        }

        // POST: Maestros/Crear
        [HttpPost]
        public ActionResult Crear(Maestro modelo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (modelo == null || string.IsNullOrWhiteSpace(modelo.Nombres) || string.IsNullOrWhiteSpace(modelo.Apellidos) || modelo.IdIglesia <= 0)
            {
                TempData["MensajeError"] = "Los nombres, apellidos e iglesia son obligatorios.";
                return RedirectToAction("Index");
            }

            if (!PuedeEditarIglesiaPorId(u, modelo.IdIglesia))
            {
                TempData["MensajeError"] = "No tiene permiso para agregar maestros a una iglesia fuera de su equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    INSERT INTO dbo.Maestros (IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo) 
                    VALUES (@IdIglesia, @Nombres, @Apellidos, @Doc, @Celular, @Correo, 1);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);
                cmd.Parameters.AddWithValue("@Nombres", modelo.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", modelo.Apellidos);
                cmd.Parameters.AddWithValue("@Doc", modelo.DocumentoIdentidad ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Celular", modelo.Celular ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Correo", modelo.Correo ?? (object)DBNull.Value);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Maestro registrado con éxito.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult RegistrarMaestroDesdeFicha(int idIglesia, string nombres, string apellidos, string documentoIdentidad, string celular, string correo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos) || idIglesia <= 0)
            {
                TempData["MensajeError"] = "Los nombres, apellidos e iglesia son obligatorios.";
                return RedirectToAction("Detalle", "Iglesia", new { id = idIglesia });
            }

            if (!PuedeEditarIglesiaPorId(u, idIglesia))
            {
                TempData["MensajeError"] = "No tiene permiso para agregar maestros a una iglesia fuera de su equipo o jurisdicción.";
                return RedirectToAction("Detalle", "Iglesia", new { id = idIglesia });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        INSERT INTO dbo.Maestros (IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo) 
                        VALUES (@IdIglesia, @Nombres, @Apellidos, @Doc, @Celular, @Correo, 1);";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmd.Parameters.AddWithValue("@Nombres", nombres.Trim());
                    cmd.Parameters.AddWithValue("@Apellidos", apellidos.Trim());
                    cmd.Parameters.AddWithValue("@Doc", documentoIdentidad ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Celular", celular ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", correo ?? (object)DBNull.Value);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["MensajeExito"] = "Maestro registrado con éxito.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al registrar maestro: " + ex.Message;
            }

            return RedirectToAction("Detalle", "Iglesia", new { id = idIglesia });
        }

        [HttpPost]
        public JsonResult RegistrarMaestroAjax(int idIglesia, string nombres, string apellidos, string documentoIdentidad, string celular, string correo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos) || idIglesia <= 0)
            {
                return Json(new { success = false, message = "Los nombres, apellidos e iglesia son obligatorios." });
            }

            if (!PuedeEditarIglesiaPorId(u, idIglesia))
            {
                return Json(new { success = false, message = "No tiene permiso para agregar maestros a una iglesia fuera de su equipo o jurisdicción." });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        INSERT INTO dbo.Maestros (IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo) 
                        VALUES (@IdIglesia, @Nombres, @Apellidos, @Doc, @Celular, @Correo, 1);";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmd.Parameters.AddWithValue("@Nombres", nombres.Trim());
                    cmd.Parameters.AddWithValue("@Apellidos", apellidos.Trim());
                    cmd.Parameters.AddWithValue("@Doc", documentoIdentidad ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Celular", celular ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Correo", correo ?? (object)DBNull.Value);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Maestro registrado con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar maestro: " + ex.Message });
            }
        }

        // POST: Maestros/Editar
        [HttpPost]
        public ActionResult Editar(Maestro modelo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (modelo == null || string.IsNullOrWhiteSpace(modelo.Nombres) || string.IsNullOrWhiteSpace(modelo.Apellidos) || modelo.IdIglesia <= 0)
            {
                TempData["MensajeError"] = "Los nombres, apellidos e iglesia son obligatorios.";
                return RedirectToAction("Index");
            }

            if (!PuedeEditarMaestro(u, modelo.IdMaestro))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar este maestro porque pertenece a una iglesia fuera de su equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            if (!PuedeEditarIglesiaPorId(u, modelo.IdIglesia))
            {
                TempData["MensajeError"] = "No tiene permiso para asignar este maestro a una iglesia fuera de su equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    UPDATE dbo.Maestros 
                    SET IdIglesia = @IdIglesia, Nombres = @Nombres, Apellidos = @Apellidos, 
                        DocumentoIdentidad = @Doc, Celular = @Celular, Correo = @Correo, Activo = @Activo
                    WHERE IdMaestro = @Id;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);
                cmd.Parameters.AddWithValue("@Nombres", modelo.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", modelo.Apellidos);
                cmd.Parameters.AddWithValue("@Doc", modelo.DocumentoIdentidad ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Celular", modelo.Celular ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Correo", modelo.Correo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Activo", modelo.Activo);
                cmd.Parameters.AddWithValue("@Id", modelo.IdMaestro);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Datos del maestro actualizados con éxito.";
            return RedirectToAction("Index");
        }

        // POST: Maestros/ToggleEstado
        [HttpPost]
        public ActionResult ToggleEstado(int idMaestro, bool activo, string motivo = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarMaestro(u, idMaestro))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar el estado de este maestro porque pertenece a una iglesia fuera de su equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            int idIglesia = 0;
            string nombreMaestro = "";

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Obtener datos del maestro
                string sqlGet = "SELECT IdIglesia, Nombres, Apellidos FROM dbo.Maestros WHERE IdMaestro = @Id;";
                using (SqlCommand cmdGet = new SqlCommand(sqlGet, cn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", idMaestro);
                    using (SqlDataReader dr = cmdGet.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            idIglesia = Convert.ToInt32(dr["IdIglesia"]);
                            nombreMaestro = dr["Nombres"].ToString() + " " + dr["Apellidos"].ToString();
                        }
                    }
                }

                // Actualizar estado del maestro
                string sql = "UPDATE dbo.Maestros SET Activo = @Activo WHERE IdMaestro = @Id;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Activo", activo);
                    cmd.Parameters.AddWithValue("@Id", idMaestro);
                    cmd.ExecuteNonQuery();
                }

                // Registrar en el historial de la iglesia si tiene participación activa
                if (idIglesia > 0)
                {
                    string sqlPart = "SELECT TOP 1 IdParticipacion FROM dbo.ParticipacionesIglesia WHERE IdIglesia = @IdIglesia ORDER BY IdParticipacion DESC;";
                    using (SqlCommand cmdPart = new SqlCommand(sqlPart, cn))
                    {
                        cmdPart.Parameters.AddWithValue("@IdIglesia", idIglesia);
                        object valPart = cmdPart.ExecuteScalar();
                        if (valPart != null)
                        {
                            int idPart = Convert.ToInt32(valPart);
                            string accion = activo ? "Reactivación de Maestro" : "Baja de Maestro";
                            string com = activo ? $"El maestro '{nombreMaestro}' fue reactivado en el sistema." : $"El maestro '{nombreMaestro}' fue dado de baja.";
                            string sqlLog = @"
                                INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario, Razon)
                                VALUES (@IdPart, GETDATE(), @Accion, @Ant, @Nue, @IdUser, @Com, @Raz);";
                            using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                            {
                                cmdLog.Parameters.AddWithValue("@IdPart", idPart);
                                cmdLog.Parameters.AddWithValue("@Accion", accion);
                                cmdLog.Parameters.AddWithValue("@Ant", activo ? "Inactivo" : "Activo");
                                cmdLog.Parameters.AddWithValue("@Nue", activo ? "Activo" : "Inactivo");
                                cmdLog.Parameters.AddWithValue("@IdUser", u.IdUsuario);
                                cmdLog.Parameters.AddWithValue("@Com", com);
                                cmdLog.Parameters.AddWithValue("@Raz", string.IsNullOrWhiteSpace(motivo) ? (object)DBNull.Value : motivo.Trim());
                                cmdLog.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            TempData["MensajeExito"] = activo ? "Maestro reactivado correctamente." : "Maestro dado de baja y registrado en el historial de la iglesia.";
            return RedirectToAction("Index");
        }

        private bool PuedeEditarMaestro(Usuario u, int idMaestro)
        {
            if (u == null) return false;
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT i.IdEquipo 
                    FROM dbo.Maestros m 
                    INNER JOIN dbo.Iglesias i ON m.IdIglesia = i.IdIglesia 
                    WHERE m.IdMaestro = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idMaestro);
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                {
                    int idEquipoIglesia = Convert.ToInt32(val);
                    return PuedeEditarEquipo(u, idEquipoIglesia);
                }
            }
            return false;
        }

        [HttpGet]
        public JsonResult ValidarJurisdiccionIglesia(int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null)
            {
                return Json(new { success = false, message = "Sesión inválida." }, JsonRequestBehavior.AllowGet);
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT i.IdEquipo, e.NombreEquipo 
                    FROM dbo.Iglesias i 
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo 
                    WHERE i.IdIglesia = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idIglesia);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int idEquipo = Convert.ToInt32(dr["IdEquipo"]);
                        string nombreEquipo = dr["NombreEquipo"].ToString();
                        
                        bool allowed = PuedeEditarEquipo(u, idEquipo);
                        return Json(new { 
                            success = true, 
                            allowed = allowed, 
                            nombreEquipo = nombreEquipo 
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(new { success = false, message = "Iglesia no encontrada." }, JsonRequestBehavior.AllowGet);
        }

        private bool PuedeEditarIglesiaPorId(Usuario u, int idIglesia)
        {
            if (u == null) return false;
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdEquipo FROM dbo.Iglesias WHERE IdIglesia = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idIglesia);
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                {
                    return PuedeEditarEquipo(u, Convert.ToInt32(val));
                }
            }
            return false;
        }

        private bool PuedeEditarEquipo(Usuario u, int idEquipoIglesia)
        {
            if (u == null) return false;
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;
            if (u.IdEquipo.HasValue)
            {
                if (u.IdEquipo.Value == idEquipoIglesia) return true;
                return EsEquipoHijo(u.IdEquipo.Value, idEquipoIglesia);
            }
            return false;
        }

        private bool EsEquipoHijo(int idEquipoPadre, int idEquipoHijo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT COUNT(1) FROM dbo.Equipos WHERE IdEquipo = @IdHijo AND IdEquipoPadre = @IdPadre;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdHijo", idEquipoHijo);
                cmd.Parameters.AddWithValue("@IdPadre", idEquipoPadre);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void ObtenerEquiposHijosRecursivo(int idEquipoPadre, HashSet<int> set)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdEquipo FROM dbo.Equipos WHERE IdEquipoPadre = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idEquipoPadre);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int hId = Convert.ToInt32(dr["IdEquipo"]);
                        set.Add(hId);
                        ObtenerEquiposHijosRecursivo(hId, set);
                    }
                }
            }
        }

        private void CargarIglesiasDisponibles()
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            var mapaEquipos = new Dictionary<string, object>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT DISTINCT i.IdIglesia, i.NombreIglesia, e.IdEquipo, e.NombreEquipo
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    WHERE t.Activa = 1
                    ORDER BY i.NombreIglesia;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string idIg = dr["IdIglesia"].ToString();
                        lista.Add(new SelectListItem
                        {
                            Value = idIg,
                            Text = $"{dr["NombreIglesia"]} ({dr["NombreEquipo"]})"
                        });

                        mapaEquipos[idIg] = new {
                            IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                            NombreEquipo = dr["NombreEquipo"].ToString()
                        };
                    }
                }
            }
            ViewBag.ListaIglesias = lista;
            
            // Serializar mapa de equipos a JSON para el buscador JS
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            ViewBag.MapaEquiposJson = serializer.Serialize(mapaEquipos);
        }
    }
}
