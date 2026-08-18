using SOR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

using System.Data.SqlClient;
using System.Data;

namespace SOR.Controllers
{
    public class AccesoController : Controller
    {

        static string cadena = @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";

        // GET: Acceso
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registrar(Usuario oUsuario)
        {
            bool registrado;
            string mensaje;

            if (oUsuario.Clave == oUsuario.ConfirmarClave) {

                oUsuario.Clave = ConvertirSha256(oUsuario.Clave);
            }
            else {
                ViewData["Mensaje"] = "Las Contraseñas no coinciden";
                return View();
            }

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("sp_RegistrarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.Parameters.Add("Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();

                cmd.ExecuteNonQuery();

                registrado = Convert.ToBoolean(cmd.Parameters["Registrado"].Value);
                mensaje = cmd.Parameters["Mensaje"].Value.ToString();

            }

            ViewData["Mensaje"] = mensaje;

            if (registrado)
            {
                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                return View();
            }

        }


        [HttpPost]
        public ActionResult Login(Usuario oUsuario)
        {
            // Validar correo
            if (string.IsNullOrWhiteSpace(oUsuario.Correo))
            {
                ViewData["Mensaje"] = "Debe ingresar el correo.";
                return View();
            }

            // Validar contraseña
            if (string.IsNullOrWhiteSpace(oUsuario.Clave))
            {
                ViewData["Mensaje"] = "Debe ingresar la contraseña.";
                return View();
            }

            // Ambos campos están completos, continúa el proceso
            oUsuario.Clave = ConvertirSha256(oUsuario.Clave);

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("sp_ValidarUsuario", cn);

                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();

                object resultado = cmd.ExecuteScalar();

                if (resultado != null)
                {
                    oUsuario.IdUsuario = Convert.ToInt32(resultado);
                }
                else
                {
                    oUsuario.IdUsuario = 0;
                }
            }

            if (oUsuario.IdUsuario != 0)
            {
                Session["usuario"] = oUsuario;
                return RedirectToAction("Index", "Home");
            }

            ViewData["Mensaje"] = "Usuario o contraseña incorrectos.";
            return View();
        }

        public static string ConvertirSha256(string texto)
        {
            

            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

    }
}