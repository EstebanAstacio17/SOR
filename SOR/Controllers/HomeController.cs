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
            if (Session["usuario"] != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [ValidarSesion]
        public ActionResult Index()
        {
            Usuario usuario = (Usuario)Session["usuario"];
            ViewBag.Usuario = usuario;

            string temporadaActiva = "No Definida";
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                string query = @"
                    SELECT TOP 1 NombreTemporada 
                    FROM Temporadas 
                    WHERE GETDATE() BETWEEN FechaInicio AND FechaFin
                    ORDER BY IdTemporada DESC";
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                {
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        temporadaActiva = result.ToString().Replace("Temp ", "").Trim();
                    }
                }
            }
            ViewBag.TemporadaActiva = temporadaActiva;

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