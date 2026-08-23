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
    public class CompanerosOracionController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: CompanerosOracion
        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
            List<CompaneroOracion> lista = new List<CompaneroOracion>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT c.*, i.NombreIglesia, t.NombreTemporada, us.Correo AS CorreoRegistrador
                    FROM dbo.CompanerosOracion c
                    INNER JOIN dbo.Iglesias i ON c.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Temporadas t ON c.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Usuarios us ON c.IdUsuarioRegistro = us.IdUsuario
                    ORDER BY c.FechaRegistro DESC;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new CompaneroOracion
                        {
                            IdCompanero = Convert.ToInt32(dr["IdCompanero"]),
                            NombreCompleto = dr["NombreCompleto"].ToString(),
                            ContactoWhatsApp = dr["ContactoWhatsApp"] != DBNull.Value ? dr["ContactoWhatsApp"].ToString() : "",
                            EsMayorEdad = Convert.ToBoolean(dr["EsMayorEdad"]),
                            IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                            NombreIglesia = dr["NombreIglesia"].ToString(),
                            IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                            NombreTemporada = dr["NombreTemporada"].ToString(),
                            IdUsuarioRegistro = Convert.ToInt32(dr["IdUsuarioRegistro"]),
                            CorreoRegistrador = dr["CorreoRegistrador"].ToString(),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }

            CargarIglesiasYTemporadas();
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeRegistrar = PuedeRegistrarOracion(u);
            return View(lista);
        }

        // POST: CompanerosOracion/Crear
        [HttpPost]
        public ActionResult Crear(CompaneroOracion modelo)
        {
            Usuario u = (Usuario)Session["usuario"];

            if (!PuedeRegistrarOracion(u))
            {
                TempData["MensajeError"] = "No tienes permisos para registrar compañeros de oración. Rol requerido: Coordinador de Oración (CO) o Administrador.";
                return RedirectToAction("Index");
            }

            if (modelo == null || string.IsNullOrWhiteSpace(modelo.NombreCompleto) || modelo.IdIglesia <= 0)
            {
                TempData["MensajeError"] = "El nombre completo y la iglesia asociada son obligatorios.";
                return RedirectToAction("Index");
            }

            // Si no se especificó temporada, obtener la activa
            if (modelo.IdTemporada <= 0)
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cn.Open();
                    object valObj = cmd.ExecuteScalar();
                    if (valObj != null)
                    {
                        modelo.IdTemporada = Convert.ToInt32(valObj);
                    }
                    else
                    {
                        TempData["MensajeError"] = "No hay ninguna temporada registrada en el sistema. Configura una primero.";
                        return RedirectToAction("Index");
                    }
                }
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    INSERT INTO dbo.CompanerosOracion (NombreCompleto, ContactoWhatsApp, EsMayorEdad, IdIglesia, IdTemporada, IdUsuarioRegistro) 
                    VALUES (@Nombre, @WhatsApp, @EsMayor, @IdIglesia, @IdTemp, @IdUsuario);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreCompleto);
                cmd.Parameters.AddWithValue("@WhatsApp", modelo.ContactoWhatsApp ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EsMayor", modelo.EsMayorEdad);
                cmd.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);
                cmd.Parameters.AddWithValue("@IdTemp", modelo.IdTemporada);
                cmd.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Compañero de oración registrado exitosamente.";
            return RedirectToAction("Index");
        }

        // POST: CompanerosOracion/Eliminar
        [HttpPost]
        public ActionResult Eliminar(int idCompanero)
        {
            Usuario u = (Usuario)Session["usuario"];

            if (!PuedeRegistrarOracion(u))
            {
                TempData["MensajeError"] = "Permiso denegado.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "DELETE FROM dbo.CompanerosOracion WHERE IdCompanero = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Id", idCompanero);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Compañero de oración eliminado con éxito.";
            return RedirectToAction("Index");
        }

        private bool PuedeRegistrarOracion(Usuario u)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // Admins
            if (u.IdPosicion == 5) return true; // Coordinador de Oración (CO)
            return false;
        }

        private void CargarIglesiasYTemporadas()
        {
            List<SelectListItem> iglesias = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdIglesia, NombreIglesia FROM dbo.Iglesias ORDER BY NombreIglesia;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        iglesias.Add(new SelectListItem
                        {
                            Value = dr["IdIglesia"].ToString(),
                            Text = dr["NombreIglesia"].ToString()
                        });
                    }
                }
            }
            ViewBag.ListaIglesias = iglesias;

            List<SelectListItem> temporadas = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdTemporada, NombreTemporada FROM dbo.Temporadas ORDER BY IdTemporada DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        temporadas.Add(new SelectListItem
                        {
                            Value = dr["IdTemporada"].ToString(),
                            Text = dr["NombreTemporada"].ToString()
                        });
                    }
                }
            }
            ViewBag.ListaTemporadas = temporadas;
        }
    }
}
