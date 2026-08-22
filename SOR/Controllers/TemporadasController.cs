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
    public class TemporadasController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Temporadas
        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2) // Solo Admin/SuperAdmin
            {
                return RedirectToAction("Index", "Home");
            }

            List<Temporada> lista = new List<Temporada>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT * FROM dbo.Temporadas ORDER BY IdTemporada DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Temporada
                        {
                            IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                            NombreTemporada = dr["NombreTemporada"].ToString(),
                            FechaInicio = dr["FechaInicio"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaInicio"]) : null,
                            FechaFin = dr["FechaFin"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaFin"]) : null,
                            Activa = Convert.ToBoolean(dr["Activa"])
                        });
                    }
                }
            }

            ViewBag.UsuarioActual = u;
            return View(lista);
        }

        // POST: Temporadas/Crear
        [HttpPost]
        public ActionResult Crear(string nombreTemporada, DateTime? fechaInicio, DateTime? fechaFin)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(nombreTemporada))
            {
                TempData["MensajeError"] = "El nombre de la temporada es obligatorio.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "INSERT INTO dbo.Temporadas (NombreTemporada, FechaInicio, FechaFin, Activa) VALUES (@Nombre, @Inicio, @Fin, 0);";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", nombreTemporada);
                cmd.Parameters.AddWithValue("@Inicio", (object)fechaInicio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fin", (object)fechaFin ?? DBNull.Value);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Temporada creada con éxito.";
            return RedirectToAction("Index");
        }

        // POST: Temporadas/Editar
        [HttpPost]
        public ActionResult Editar(int idTemporada, string nombreTemporada, DateTime? fechaInicio, DateTime? fechaFin)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(nombreTemporada))
            {
                TempData["MensajeError"] = "El nombre de la temporada es obligatorio.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Temporadas SET NombreTemporada = @Nombre, FechaInicio = @Inicio, FechaFin = @Fin WHERE IdTemporada = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", nombreTemporada);
                cmd.Parameters.AddWithValue("@Inicio", (object)fechaInicio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fin", (object)fechaFin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", idTemporada);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Temporada actualizada con éxito.";
            return RedirectToAction("Index");
        }

        // POST: Temporadas/Activar
        [HttpPost]
        public ActionResult Activar(int idTemporada)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Desactivar todas
                        string sqlDesactivar = "UPDATE dbo.Temporadas SET Activa = 0;";
                        using (SqlCommand cmdDes = new SqlCommand(sqlDesactivar, cn, tran))
                        {
                            cmdDes.ExecuteNonQuery();
                        }

                        // Activar la seleccionada
                        string sqlActivar = "UPDATE dbo.Temporadas SET Activa = 1 WHERE IdTemporada = @Id;";
                        using (SqlCommand cmdAct = new SqlCommand(sqlActivar, cn, tran))
                        {
                            cmdAct.Parameters.AddWithValue("@Id", idTemporada);
                            cmdAct.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "La temporada seleccionada ahora es la activa en el sistema.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al activar la temporada: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Temporadas/Inactivar
        [HttpPost]
        public ActionResult Inactivar(int idTemporada)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Temporadas SET Activa = 0 WHERE IdTemporada = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idTemporada);
                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Temporada desactivada/inhabilitada con éxito.";
            return RedirectToAction("Index");
        }
    }
}
