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
    public class ReporteController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Reporte/Index
        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
            List<ReporteEvento> listaReportes = new List<ReporteEvento>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT r.*, i.NombreIglesia
                    FROM dbo.ReportesEventos r
                    INNER JOIN dbo.ParticipacionesIglesia p ON r.IdParticipacion = p.IdParticipacion
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    ORDER BY r.FechaCreacion DESC;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        listaReportes.Add(new ReporteEvento
                        {
                            IdReporteEvento = Convert.ToInt32(dr["IdReporteEvento"]),
                            IdParticipacion = Convert.ToInt32(dr["IdParticipacion"]),
                            TipoReporte = dr["TipoReporte"].ToString(),
                            Fecha = dr["Fecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["Fecha"]) : null,
                            CantidadNinos = dr["CantidadNinos"] != DBNull.Value ? Convert.ToInt32(dr["CantidadNinos"]) : 0,
                            CantidadClases = dr["CantidadClases"] != DBNull.Value ? Convert.ToInt32(dr["CantidadClases"]) : 0,
                            CuantosAceptaronSenor = dr["CuantosAceptaronSenor"] != DBNull.Value ? Convert.ToInt32(dr["CuantosAceptaronSenor"]) : 0,
                            CuantosGraduaron = dr["CuantosGraduaron"] != DBNull.Value ? Convert.ToInt32(dr["CuantosGraduaron"]) : 0,
                            ReporteAdjuntoRuta = dr["ReporteAdjuntoRuta"] != DBNull.Value ? dr["ReporteAdjuntoRuta"].ToString() : "",
                            Notas = dr["Notas"] != DBNull.Value ? dr["Notas"].ToString() : "",
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"])
                        });
                    }
                }
            }

            ViewBag.UsuarioActual = u;
            return View(listaReportes);
        }

        [HttpPost]
        public ActionResult CrearReporteEvento(ReporteEvento modelo, int idIglesia, HttpPostedFileBase adjuntoReporte)
        {
            Usuario u = (Usuario)Session["usuario"];

            string uploadPath = Server.MapPath("~/Uploads/Reportes/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            if (adjuntoReporte != null && adjuntoReporte.ContentLength > 0)
            {
                string ext = Path.GetExtension(adjuntoReporte.FileName);
                string fileName = $"Rep_{Guid.NewGuid()}{ext}";
                adjuntoReporte.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.ReporteAdjuntoRuta = "/Uploads/Reportes/" + fileName;
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Obtener participacion activa de la iglesia
                string sqlPart = "SELECT TOP 1 p.IdParticipacion FROM dbo.ParticipacionesIglesia p INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada WHERE p.IdIglesia = @IdIglesia AND t.Activa = 1;";
                SqlCommand cmdPart = new SqlCommand(sqlPart, cn);
                cmdPart.Parameters.AddWithValue("@IdIglesia", idIglesia);
                object partObj = cmdPart.ExecuteScalar();

                if (partObj != null)
                {
                    int idParticipacion = Convert.ToInt32(partObj);

                    string sql = @"
                        INSERT INTO dbo.ReportesEventos (
                            IdParticipacion, TipoReporte, Fecha, CantidadNinos, CantidadClases, 
                            AsistenciaPorClase, CuantosAceptaronSenor, CuantosComprometieron, 
                            CuantosGraduaron, ReporteAdjuntoRuta, Notas
                        ) VALUES (
                            @IdParticipacion, @TipoReporte, @Fecha, @CantidadNinos, @CantidadClases, 
                            @AsistenciaPorClase, @CuantosAceptaronSenor, @CuantosComprometieron, 
                            @CuantosGraduaron, @ReporteAdjuntoRuta, @Notas
                        );";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@IdParticipacion", idParticipacion);
                    cmd.Parameters.AddWithValue("@TipoReporte", modelo.TipoReporte ?? "Evangelistico");
                    cmd.Parameters.AddWithValue("@Fecha", modelo.Fecha ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CantidadNinos", modelo.CantidadNinos);
                    cmd.Parameters.AddWithValue("@CantidadClases", modelo.CantidadClases);
                    cmd.Parameters.AddWithValue("@AsistenciaPorClase", modelo.AsistenciaPorClase ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CuantosAceptaronSenor", modelo.CuantosAceptaronSenor);
                    cmd.Parameters.AddWithValue("@CuantosComprometieron", modelo.CuantosComprometieron);
                    cmd.Parameters.AddWithValue("@CuantosGraduaron", modelo.CuantosGraduaron);
                    cmd.Parameters.AddWithValue("@ReporteAdjuntoRuta", modelo.ReporteAdjuntoRuta ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notas", modelo.Notas ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["MensajeExito"] = "Reporte guardado exitosamente.";
            return RedirectToAction("Index");
        }
    }
}
