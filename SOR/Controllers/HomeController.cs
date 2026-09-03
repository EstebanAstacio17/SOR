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
            int idTemporadaActiva = 0;
            int totalIglesiasRegistradas = 0;
            int solicitudesPendientes = 0;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                conn.Open();

                // 1. Obtener la temporada activa actual
                string queryTemp = @"
                    SELECT TOP 1 IdTemporada, NombreTemporada 
                    FROM dbo.Temporadas 
                    ORDER BY Activa DESC, FechaInicio DESC, IdTemporada DESC;";
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(queryTemp, conn))
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            idTemporadaActiva = Convert.ToInt32(dr["IdTemporada"]);
                            temporadaActiva = dr["NombreTemporada"].ToString().Replace("Temp ", "").Trim();
                        }
                    }
                }

                // 2. Determinar equipo del usuario (si no es admin o si el usuario tiene equipo asignado)
                bool esAdmin = usuario != null && (usuario.IdRolSeguridad == 1 || usuario.IdRolSeguridad == 2);
                int? idEquipo = (!esAdmin && usuario != null && usuario.IdEquipo.HasValue) 
                                ? usuario.IdEquipo 
                                : (usuario != null && usuario.IdEquipo.HasValue ? usuario.IdEquipo : (int?)null);

                // 3. Conteo de iglesias registradas en la temporada activa para el equipo del usuario
                string queryIglesias = @"
                    SELECT COUNT(DISTINCT i.IdIglesia)
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    WHERE (@IdTemp = 0 OR p.IdTemporada = @IdTemp)
                      AND (@IdEquipo IS NULL OR i.IdEquipo = @IdEquipo);";

                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(queryIglesias, conn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporadaActiva);
                    cmd.Parameters.AddWithValue("@IdEquipo", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    object res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        totalIglesiasRegistradas = Convert.ToInt32(res);
                    }
                }

                // 4. Solicitudes / Evaluaciones pendientes en la temporada para el equipo
                string queryPendientes = @"
                    SELECT COUNT(1)
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    WHERE (@IdTemp = 0 OR p.IdTemporada = @IdTemp)
                      AND (@IdEquipo IS NULL OR i.IdEquipo = @IdEquipo)
                      AND (p.EstadoEvaluacion = 'Pendiente' OR p.EstatusEvaluacionReporte = 'Pendiente');";

                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(queryPendientes, conn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemporadaActiva);
                    cmd.Parameters.AddWithValue("@IdEquipo", idEquipo.HasValue ? (object)idEquipo.Value : DBNull.Value);
                    object res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        solicitudesPendientes = Convert.ToInt32(res);
                    }
                }
            }

            ViewBag.TemporadaActiva = temporadaActiva;
            ViewBag.TotalIglesiasRegistradas = totalIglesiasRegistradas;
            ViewBag.SolicitudesPendientes = solicitudesPendientes;

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