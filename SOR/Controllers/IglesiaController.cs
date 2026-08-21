using SOR.Models;
using SOR.Permisos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class IglesiaController : Controller
    {
        private readonly Services.IglesiaService _iglesiaService = new Services.IglesiaService();

        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Iglesia/Index
        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
            List<Iglesia> lista = _iglesiaService.ObtenerIglesias();

            ViewBag.UsuarioActual = u;
            return View(lista);
        }

        // GET: Iglesia/Crear
        public ActionResult Crear()
        {
            Usuario u = (Usuario)Session["usuario"];
            
            // Validar si la posición tiene permiso de registro (Equipo, Movilización o Admin/SuperAdmin)
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Tu rol o posición de coordinador no posee permisos para registrar nuevas iglesias.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);
            return View(new Iglesia());
        }

        [HttpPost]
        public ActionResult Crear(Iglesia modelo, HttpPostedFileBase docPastor, HttpPostedFileBase docLider)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Permiso denegado para registrar iglesias.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);

            // Proceso de carga de archivos (Uploads)
            string uploadPath = Server.MapPath("~/Uploads/Iglesias/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            if (docPastor != null && docPastor.ContentLength > 0)
            {
                string ext = Path.GetExtension(docPastor.FileName);
                string fileName = $"Pastor_{Guid.NewGuid()}{ext}";
                docPastor.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.Pastor.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }

            if (docLider != null && docLider.ContentLength > 0)
            {
                string ext = Path.GetExtension(docLider.FileName);
                string fileName = $"Lider_{Guid.NewGuid()}{ext}";
                docLider.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }

            try
            {
                if (modelo.IdEquipo <= 0)
                {
                    modelo.IdEquipo = u.IdEquipo ?? 1;
                }

                int idIglesiaNew = _iglesiaService.RegistrarIglesia(modelo, u.IdUsuario);

                TempData["MensajeExito"] = "Iglesia registrada exitosamente con su expediente inicial.";
                return RedirectToAction("Detalle", new { id = idIglesiaNew });
            }
            catch (Exception ex)
            {
                ViewData["MensajeError"] = "Error al registrar la iglesia: " + ex.Message;
                return View(modelo);
            }
        }

        // GET: Iglesia/Detalle/5
        public ActionResult Detalle(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(id);

            if (iglesia == null)
            {
                return HttpNotFound();
            }

            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarIglesia(u, iglesia.IdEquipo);
            return View(iglesia);
        }

        [HttpPost]
        public ActionResult EvaluarParticipacion(int idParticipacion, int idIglesia, string estadoEvaluacion, bool participara, string justificacionNoParticipacion)
        {
            Usuario u = (Usuario)Session["usuario"];

            try
            {
                _iglesiaService.EvaluarParticipacion(idParticipacion, participara, justificacionNoParticipacion, estadoEvaluacion, u.IdUsuario);
                TempData["MensajeExito"] = "Evaluación de participación actualizada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult DespacharRecursos(AsignacionRecursos modelo, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];

            try
            {
                _iglesiaService.DespacharRecursos(modelo, u.IdUsuario);
                TempData["MensajeExito"] = "Asignación de recursos despachados guardada con éxito.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult AgregarComentario(int idIglesia, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];

            try
            {
                _iglesiaService.AgregarComentario(idIglesia, u.IdUsuario, comentario);
                TempData["MensajeExito"] = "Observación guardada en el historial.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // MÉTODOS DE CONTROL DE PERMISOS DE JERARQUÍA Y POSICIÓN
        // ============================================================================

        private bool PuedeRegistrarIglesia(Usuario u)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin
            if (u.RangoJerarquico == 1) return true; // ENL
            if (u.IdPosicion == 1 || u.IdPosicion == 2) return true; // Coord Equipo o Movilización
            return false;
        }

        private bool PuedeEditarIglesia(Usuario u, int idEquipoIglesia)
        {
            // SuperAdmin o Admin -> Puede editar todo
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;

            // ENL (Rango 1) -> Puede editar todo
            if (u.RangoJerarquico == 1) return true;

            // ERLE (Rango 2) -> Puede editar su propio equipo y los ERLs bajo su supervisión
            if (u.RangoJerarquico == 2)
            {
                if (u.IdEquipo.HasValue && u.IdEquipo.Value == idEquipoIglesia) return true;
                return EsEquipoHijo(u.IdEquipo.Value, idEquipoIglesia);
            }

            // ERL (Rango 3) -> Solo puede editar registros de su propio equipo
            if (u.RangoJerarquico == 3)
            {
                return u.IdEquipo.HasValue && u.IdEquipo.Value == idEquipoIglesia;
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

        private void CargarEquiposDisponibles(Usuario u)
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT e.IdEquipo, e.NombreEquipo, n.NombreNivel FROM dbo.Equipos e INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo WHERE e.Activo = 1 ORDER BY n.RangoJerarquico, e.NombreEquipo;";
                SqlCommand cmd = new SqlCommand(sql, cn);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = dr["IdEquipo"].ToString(),
                            Text = $"[{dr["NombreNivel"]}] {dr["NombreEquipo"]}"
                        });
                    }
                }
            }

            ViewBag.ListaEquipos = lista;
        }
    }
}
