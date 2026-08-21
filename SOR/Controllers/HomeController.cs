using SOR.Models;
using SOR.Permisos;
using System;
using System.Web.Mvc;

namespace SOR.Controllers
{
    public class HomeController : Controller
    {
        // Acción pública: Pantalla de Bienvenida / Landing Page para Voluntarios
        public ActionResult Landing()
        {
            return View();
        }

        [ValidarSesion]
        public ActionResult Index()
        {
            Usuario usuario = (Usuario)Session["usuario"];
            ViewBag.Usuario = usuario;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Sistema de Gestión Interna OCC Rep Dom (SOR)";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contacto OCC República Dominicana";
            return View();
        }

        public ActionResult SolicitudVoluntario()
        {
            return View();
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Acceso");
        }
    }
}