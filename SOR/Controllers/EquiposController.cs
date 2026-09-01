using System;
using System.Collections.Generic;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;
using SOR.Services;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class EquiposController : Controller
    {
        private readonly EquipoService _equipoService = new EquipoService();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            
            // Restricción estricta: Solo SuperAdmin (1)
            if (usuarioActual == null || usuarioActual.IdRolSeguridad != 1)
            {
                filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new { controller = "Home", action = "Index" }));
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: Equipos/Index
        public ActionResult Index()
        {
            List<EquipoConDetalles> equipos = _equipoService.ListarEquipos();
            
            // Cargar datos para los modales de creación/edición
            ViewBag.Niveles = _equipoService.ListarNiveles();
            ViewBag.TodosEquipos = equipos;

            return View(equipos);
        }

        // GET: Equipos/ObtenerEquipo/5
        [HttpGet]
        public JsonResult ObtenerEquipo(int id)
        {
            try
            {
                var equipo = _equipoService.ObtenerEquipoPorId(id);
                if (equipo == null)
                {
                    return Json(new { success = false, message = "Equipo no encontrado." }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = true, data = equipo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Equipos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(EquipoConDetalles modelo)
        {
            try
            {
                _equipoService.RegistrarEquipo(modelo);
                TempData["MensajeExito"] = "Equipo creado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al crear equipo: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Equipos/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(EquipoConDetalles modelo)
        {
            try
            {
                _equipoService.ActualizarEquipo(modelo);
                TempData["MensajeExito"] = "Equipo actualizado correctamente.";
            }
            catch (System.Data.DBConcurrencyException exConc)
            {
                TempData["MensajeError"] = "Conflicto de concurrencia: " + exConc.Message;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al actualizar equipo: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Equipos/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int idEquipo)
        {
            try
            {
                string msg = _equipoService.EliminarEquipo(idEquipo);
                TempData["MensajeExito"] = msg;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
