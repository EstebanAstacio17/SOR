using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;

namespace SOR.Controllers
{
    public class HealthCheckController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Head)]
        public ActionResult Status()
        {
            var sw = Stopwatch.StartNew();
            string connectionString = ConfigurationManager.ConnectionStrings["ConexionSOR"] != null
                ? ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString
                : null;

            if (string.IsNullOrEmpty(connectionString))
            {
                Response.StatusCode = 503;
                return Json(new
                {
                    status = "FAIL",
                    database = "NoConnectionString",
                    error = "Cadena ConexionSOR no configurada.",
                    timestamp = DateTime.UtcNow.ToString("o")
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT 1 AS HealthPing, DB_NAME() AS DatabaseName;", cn))
                    {
                        cmd.CommandTimeout = 5;
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                sw.Stop();
                                string dbName = dr["DatabaseName"] != DBNull.Value ? dr["DatabaseName"].ToString() : "DB_SOR";
                                
                                return Json(new
                                {
                                    status = "OK",
                                    database = "Connected",
                                    databaseName = dbName,
                                    latencyMs = sw.ElapsedMilliseconds,
                                    serverTimeUtc = DateTime.UtcNow.ToString("o"),
                                    environment = "Azure App Service"
                                }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }

                sw.Stop();
                Response.StatusCode = 503;
                return Json(new
                {
                    status = "FAIL",
                    database = "NoDataReturned",
                    latencyMs = sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow.ToString("o")
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Response.StatusCode = 503;
                return Json(new
                {
                    status = "FAIL",
                    database = "Disconnected",
                    latencyMs = sw.ElapsedMilliseconds,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("o")
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("Status");
        }
    }
}
