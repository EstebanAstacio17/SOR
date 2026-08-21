using SOR.Models;
using SOR.Permisos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class CoordinadorController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Coordinador/EstadoSolicitud
        public ActionResult EstadoSolicitud()
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            ViewBag.Usuario = usuarioActual;
            return View();
        }

        // GET: Coordinador/RegistroPerfil
        public ActionResult RegistroPerfil()
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            if (usuarioActual.IdEstado == 1) // PendienteAprobacionCorreo
            {
                return RedirectToAction("EstadoSolicitud");
            }

            PerfilCoordinador perfil = ObtenerPerfilPorUsuario(usuarioActual.IdUsuario) ?? new PerfilCoordinador
            {
                IdUsuario = usuarioActual.IdUsuario,
                Correo = usuarioActual.Correo
            };

            CargarCombosEquiposYPosiciones();
            return View(perfil);
        }

        [HttpPost]
        public ActionResult RegistroPerfil(PerfilCoordinador modelo, HttpPostedFileBase docAdjunto, HttpPostedFileBase pasaporteAdjunto, HttpPostedFileBase fotoPerfil)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            CargarCombosEquiposYPosiciones();

            // 1. Validar campos obligatorios y determinar en qué pestañas faltan datos
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(modelo.PrimerNombre))
                errores.Add("Pestaña 1 (Datos Personales): 'Primer Nombre' es obligatorio.");
            if (string.IsNullOrWhiteSpace(modelo.PrimerApellido))
                errores.Add("Pestaña 1 (Datos Personales): 'Primer Apellido' es obligatorio.");
            if (string.IsNullOrWhiteSpace(modelo.TelefonoCelularWhatsApp))
                errores.Add("Pestaña 1 (Datos Personales): 'Celular / WhatsApp' es obligatorio.");

            if (string.IsNullOrWhiteSpace(modelo.IglesiaLocal))
                errores.Add("Pestaña 2 (Ministerial / Educativo): 'Iglesia Local' es obligatoria.");

            if (!modelo.IdEquipo.HasValue || modelo.IdEquipo.Value <= 0)
                errores.Add("Pestaña 3 (Datos OCC y Equipo): Debe seleccionar un 'Equipo OCC'.");
            if (!modelo.IdPosicion.HasValue || modelo.IdPosicion.Value <= 0)
                errores.Add("Pestaña 3 (Datos OCC y Equipo): Debe seleccionar una 'Posición / Rol'.");

            if (errores.Any())
            {
                ViewData["MensajeError"] = "Por favor completa los siguientes campos obligatorios:<br/>• " + string.Join("<br/>• ", errores);
                return View(modelo);
            }

            // 2. Validar ocupación de posición en el equipo seleccionado
            if (modelo.IdEquipo.HasValue && modelo.IdPosicion.HasValue)
            {
                if (PosicionEstaOcupada(modelo.IdEquipo.Value, modelo.IdPosicion.Value, usuarioActual.IdUsuario))
                {
                    ViewData["MensajeError"] = "La posición seleccionada en este equipo ya se encuentra ocupada por otro coordinador activo. Por favor selecciona otra posición o equipo en la Pestaña 3 (Datos OCC y Equipo).";
                    return View(modelo);
                }
            }

            // 2. Procesar Carga de Archivos Adjuntos (Uploads)
            string uploadPath = Server.MapPath("~/Uploads/Usuarios/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            if (docAdjunto != null && docAdjunto.ContentLength > 0)
            {
                string ext = Path.GetExtension(docAdjunto.FileName);
                string fileName = $"Doc_{usuarioActual.IdUsuario}_{Guid.NewGuid()}{ext}";
                docAdjunto.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.DocumentoAdjuntoRuta = "/Uploads/Usuarios/" + fileName;
            }

            if (pasaporteAdjunto != null && pasaporteAdjunto.ContentLength > 0)
            {
                string ext = Path.GetExtension(pasaporteAdjunto.FileName);
                string fileName = $"Pas_{usuarioActual.IdUsuario}_{Guid.NewGuid()}{ext}";
                pasaporteAdjunto.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.PasaporteAdjuntoRuta = "/Uploads/Usuarios/" + fileName;
            }

            if (fotoPerfil != null && fotoPerfil.ContentLength > 0)
            {
                string ext = Path.GetExtension(fotoPerfil.FileName);
                string fileName = $"Foto_{usuarioActual.IdUsuario}_{Guid.NewGuid()}{ext}";
                fotoPerfil.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.FotoRuta = "/Uploads/Usuarios/" + fileName;
            }

            // 3. Guardar o Actualizar Perfil en SQL Server
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlCheck = "SELECT COUNT(1) FROM dbo.PerfilesCoordinador WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn);
                cmdCheck.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);
                int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());

                SqlCommand cmd;
                if (existe > 0)
                {
                    // Si el perfil ya existe, preservar IdEquipo e IdPosicion previos si el usuario ya ha sido aprobado o está en proceso de restablecimiento
                    if (usuarioActual.IdEstado == 4 || usuarioActual.IdEstado == 7 || usuarioActual.IdEstado == 8)
                    {
                        string sqlGetPrev = "SELECT IdEquipo, IdPosicion FROM dbo.PerfilesCoordinador WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdPrev = new SqlCommand(sqlGetPrev, cn))
                        {
                            cmdPrev.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);
                            using (SqlDataReader drP = cmdPrev.ExecuteReader())
                            {
                                if (drP.Read())
                                {
                                    if (drP["IdEquipo"] != DBNull.Value) modelo.IdEquipo = Convert.ToInt32(drP["IdEquipo"]);
                                    if (drP["IdPosicion"] != DBNull.Value) modelo.IdPosicion = Convert.ToInt32(drP["IdPosicion"]);
                                }
                            }
                        }
                    }

                    string sqlUpdate = @"
                        UPDATE dbo.PerfilesCoordinador SET
                            PrimerNombre = @PrimerNombre, OtrosNombres = @OtrosNombres,
                            PrimerApellido = @PrimerApellido, OtrosApellidos = @OtrosApellidos,
                            FechaNacimiento = @FechaNacimiento, Calle = @Calle, Numero = @Numero,
                            Sector = @Sector, Ciudad = @Ciudad, Provincia = @Provincia,
                            Pais = @Pais, Nacionalidad = @Nacionalidad, Talla = @Talla,
                            NumeroDocumento = @NumeroDocumento, 
                            DocumentoAdjuntoRuta = ISNULL(@DocumentoAdjuntoRuta, DocumentoAdjuntoRuta),
                            NumeroPasaporte = @NumeroPasaporte, 
                            PasaporteAdjuntoRuta = ISNULL(@PasaporteAdjuntoRuta, PasaporteAdjuntoRuta),
                            TelefonoFijo = @TelefonoFijo, TelefonoCelularWhatsApp = @TelefonoCelularWhatsApp,
                            Correo = @Correo, 
                            FotoRuta = ISNULL(@FotoRuta, FotoRuta),
                            DatosConyugue = @DatosConyugue, ContactoEmergencia = @ContactoEmergencia,
                            
                            IglesiaLocal = @IglesiaLocal, PastorIglesiaLocal = @PastorIglesiaLocal,
                            CargoIglesiaLocal = @CargoIglesiaLocal, AniosServicioMinisterial = @AniosServicioMinisterial,
                            InfoMinisterial = @InfoMinisterial,
                            
                            NivelEducativo = @NivelEducativo, ProfesionCarrera = @ProfesionCarrera,
                            InfoEducativa = @InfoEducativa,
                            
                            OcupacionEmpresaLaboral = @OcupacionEmpresaLaboral, TelefonoTrabajo = @TelefonoTrabajo,
                            InfoLaboral = @InfoLaboral,
                            
                            CapacitacionesOCC = @CapacitacionesOCC,
                            
                            Ministerio = @Ministerio, 
                            IdEquipo = @IdEquipo, 
                            IdPosicion = @IdPosicion, 
                            FechaIngreso = @FechaIngreso,
                            FechaCompletado = GETDATE()
                        WHERE IdUsuario = @IdUsuario;";

                    cmd = new SqlCommand(sqlUpdate, cn);
                }

                else
                {
                    string sqlInsert = @"
                        INSERT INTO dbo.PerfilesCoordinador (
                            IdUsuario, PrimerNombre, OtrosNombres, PrimerApellido, OtrosApellidos, FechaNacimiento,
                            Calle, Numero, Sector, Ciudad, Provincia, Pais, Nacionalidad, Talla,
                            NumeroDocumento, DocumentoAdjuntoRuta, NumeroPasaporte, PasaporteAdjuntoRuta,
                            TelefonoFijo, TelefonoCelularWhatsApp, Correo, FotoRuta, DatosConyugue, ContactoEmergencia,
                            IglesiaLocal, PastorIglesiaLocal, CargoIglesiaLocal, AniosServicioMinisterial, InfoMinisterial,
                            NivelEducativo, ProfesionCarrera, InfoEducativa, OcupacionEmpresaLaboral, TelefonoTrabajo, InfoLaboral,
                            CapacitacionesOCC, Ministerio, IdEquipo, IdPosicion, FechaIngreso
                        ) VALUES (
                            @IdUsuario, @PrimerNombre, @OtrosNombres, @PrimerApellido, @OtrosApellidos, @FechaNacimiento,
                            @Calle, @Numero, @Sector, @Ciudad, @Provincia, @Pais, @Nacionalidad, @Talla,
                            @NumeroDocumento, @DocumentoAdjuntoRuta, @NumeroPasaporte, @PasaporteAdjuntoRuta,
                            @TelefonoFijo, @TelefonoCelularWhatsApp, @Correo, @FotoRuta, @DatosConyugue, @ContactoEmergencia,
                            @IglesiaLocal, @PastorIglesiaLocal, @CargoIglesiaLocal, @AniosServicioMinisterial, @InfoMinisterial,
                            @NivelEducativo, @ProfesionCarrera, @InfoEducativa, @OcupacionEmpresaLaboral, @TelefonoTrabajo, @InfoLaboral,
                            @CapacitacionesOCC, @Ministerio, @IdEquipo, @IdPosicion, @FechaIngreso
                        );";

                    cmd = new SqlCommand(sqlInsert, cn);
                }

                cmd.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);
                cmd.Parameters.AddWithValue("@PrimerNombre", modelo.PrimerNombre ?? "");
                cmd.Parameters.AddWithValue("@OtrosNombres", modelo.OtrosNombres ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PrimerApellido", modelo.PrimerApellido ?? "");
                cmd.Parameters.AddWithValue("@OtrosApellidos", modelo.OtrosApellidos ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaNacimiento", modelo.FechaNacimiento ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Calle", modelo.Calle ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Numero", modelo.Numero ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sector", modelo.Sector ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Ciudad", modelo.Ciudad ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Provincia", modelo.Provincia ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Pais", modelo.Pais ?? "República Dominicana");
                cmd.Parameters.AddWithValue("@Nacionalidad", modelo.Nacionalidad ?? "Dominicana");
                cmd.Parameters.AddWithValue("@Talla", modelo.Talla ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroDocumento", modelo.NumeroDocumento ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DocumentoAdjuntoRuta", modelo.DocumentoAdjuntoRuta ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroPasaporte", modelo.NumeroPasaporte ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PasaporteAdjuntoRuta", modelo.PasaporteAdjuntoRuta ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TelefonoFijo", modelo.TelefonoFijo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TelefonoCelularWhatsApp", modelo.TelefonoCelularWhatsApp ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Correo", usuarioActual.Correo);
                cmd.Parameters.AddWithValue("@FotoRuta", modelo.FotoRuta ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DatosConyugue", modelo.DatosConyugue ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactoEmergencia", modelo.ContactoEmergencia ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@IglesiaLocal", modelo.IglesiaLocal ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PastorIglesiaLocal", modelo.PastorIglesiaLocal ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CargoIglesiaLocal", modelo.CargoIglesiaLocal ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AniosServicioMinisterial", modelo.AniosServicioMinisterial ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@InfoMinisterial", modelo.InfoMinisterial ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@NivelEducativo", modelo.NivelEducativo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfesionCarrera", modelo.ProfesionCarrera ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@InfoEducativa", modelo.InfoEducativa ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@OcupacionEmpresaLaboral", modelo.OcupacionEmpresaLaboral ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TelefonoTrabajo", modelo.TelefonoTrabajo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@InfoLaboral", modelo.InfoLaboral ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@CapacitacionesOCC", modelo.CapacitacionesOCC ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@Ministerio", modelo.Ministerio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdEquipo", modelo.IdEquipo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdPosicion", modelo.IdPosicion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaIngreso", modelo.FechaIngreso ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();

                // 4. Actualizar Estado de Usuario a PerfilPendienteAprobacion (3) si no está activo aún
                // No cambiar el estado si está en proceso de restablecimiento de contraseña (7 u 8)
                if (usuarioActual.IdEstado != 4 && usuarioActual.IdEstado != 7 && usuarioActual.IdEstado != 8)
                {
                    string sqlUpdEstado = "UPDATE dbo.Usuarios SET IdEstado = 3 WHERE IdUsuario = @IdUsuario;";
                    SqlCommand cmdEstado = new SqlCommand(sqlUpdEstado, cn);
                    cmdEstado.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);
                    cmdEstado.ExecuteNonQuery();

                    usuarioActual.IdEstado = 3;
                    usuarioActual.NombreEstado = "PerfilPendienteAprobacion";
                    Session["usuario"] = usuarioActual;
                }
            }

            TempData["MensajeExito"] = "Tus datos han sido guardados exitosamente en el sistema.";
            return RedirectToAction("RegistroPerfil");
        }

        // GET: JSON API - Obtener posiciones ocupadas en un equipo específico
        [HttpGet]
        public JsonResult ObtenerPosicionesOcupadas(int idEquipo)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            List<int> posicionesOcupadas = new List<int>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT DISTINCT IdPosicion FROM (
                        SELECT a.IdPosicion 
                        FROM dbo.AsignacionesEquipo a 
                        INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                        WHERE a.IdEquipo = @IdEquipo AND a.Activo = 1 AND u.IdEstado IN (3, 4, 7, 8) AND a.IdUsuario <> @IdUsuario
                        UNION
                        SELECT p.IdPosicion 
                        FROM dbo.PerfilesCoordinador p 
                        INNER JOIN dbo.Usuarios u ON p.IdUsuario = u.IdUsuario 
                        WHERE p.IdEquipo = @IdEquipo AND p.IdPosicion IS NOT NULL AND u.IdEstado IN (3, 4, 7, 8) AND p.IdUsuario <> @IdUsuario
                    ) AS Ocupados;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
                cmd.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        posicionesOcupadas.Add(Convert.ToInt32(dr["IdPosicion"]));
                    }
                }
            }

            return Json(posicionesOcupadas, JsonRequestBehavior.AllowGet);
        }

        private PerfilCoordinador ObtenerPerfilPorUsuario(int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT * FROM dbo.PerfilesCoordinador WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return new PerfilCoordinador
                        {
                            IdPerfil = Convert.ToInt32(dr["IdPerfil"]),
                            IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                            PrimerNombre = dr["PrimerNombre"].ToString(),
                            OtrosNombres = dr["OtrosNombres"] != DBNull.Value ? dr["OtrosNombres"].ToString() : "",
                            PrimerApellido = dr["PrimerApellido"].ToString(),
                            OtrosApellidos = dr["OtrosApellidos"] != DBNull.Value ? dr["OtrosApellidos"].ToString() : "",
                            FechaNacimiento = dr["FechaNacimiento"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaNacimiento"]) : null,
                            Calle = dr["Calle"] != DBNull.Value ? dr["Calle"].ToString() : "",
                            Numero = dr["Numero"] != DBNull.Value ? dr["Numero"].ToString() : "",
                            Sector = dr["Sector"] != DBNull.Value ? dr["Sector"].ToString() : "",
                            Ciudad = dr["Ciudad"] != DBNull.Value ? dr["Ciudad"].ToString() : "",
                            Provincia = dr["Provincia"] != DBNull.Value ? dr["Provincia"].ToString() : "",
                            Pais = dr["Pais"] != DBNull.Value ? dr["Pais"].ToString() : "República Dominicana",
                            Nacionalidad = dr["Nacionalidad"] != DBNull.Value ? dr["Nacionalidad"].ToString() : "Dominicana",
                            Talla = dr["Talla"] != DBNull.Value ? dr["Talla"].ToString() : "",
                            NumeroDocumento = dr["NumeroDocumento"] != DBNull.Value ? dr["NumeroDocumento"].ToString() : "",
                            DocumentoAdjuntoRuta = dr["DocumentoAdjuntoRuta"] != DBNull.Value ? dr["DocumentoAdjuntoRuta"].ToString() : "",
                            NumeroPasaporte = dr["NumeroPasaporte"] != DBNull.Value ? dr["NumeroPasaporte"].ToString() : "",
                            PasaporteAdjuntoRuta = dr["PasaporteAdjuntoRuta"] != DBNull.Value ? dr["PasaporteAdjuntoRuta"].ToString() : "",
                            TelefonoFijo = dr["TelefonoFijo"] != DBNull.Value ? dr["TelefonoFijo"].ToString() : "",
                            TelefonoCelularWhatsApp = dr["TelefonoCelularWhatsApp"] != DBNull.Value ? dr["TelefonoCelularWhatsApp"].ToString() : "",
                            Correo = dr["Correo"] != DBNull.Value ? dr["Correo"].ToString() : "",
                            FotoRuta = dr["FotoRuta"] != DBNull.Value ? dr["FotoRuta"].ToString() : "",
                            DatosConyugue = dr["DatosConyugue"] != DBNull.Value ? dr["DatosConyugue"].ToString() : "",
                            ContactoEmergencia = dr["ContactoEmergencia"] != DBNull.Value ? dr["ContactoEmergencia"].ToString() : "",
                            IglesiaLocal = dr["IglesiaLocal"] != DBNull.Value ? dr["IglesiaLocal"].ToString() : "",
                            PastorIglesiaLocal = dr["PastorIglesiaLocal"] != DBNull.Value ? dr["PastorIglesiaLocal"].ToString() : "",
                            CargoIglesiaLocal = dr["CargoIglesiaLocal"] != DBNull.Value ? dr["CargoIglesiaLocal"].ToString() : "",
                            AniosServicioMinisterial = dr["AniosServicioMinisterial"] != DBNull.Value ? (int?)Convert.ToInt32(dr["AniosServicioMinisterial"]) : null,
                            InfoMinisterial = dr["InfoMinisterial"] != DBNull.Value ? dr["InfoMinisterial"].ToString() : "",
                            NivelEducativo = dr["NivelEducativo"] != DBNull.Value ? dr["NivelEducativo"].ToString() : "",
                            ProfesionCarrera = dr["ProfesionCarrera"] != DBNull.Value ? dr["ProfesionCarrera"].ToString() : "",
                            InfoEducativa = dr["InfoEducativa"] != DBNull.Value ? dr["InfoEducativa"].ToString() : "",
                            OcupacionEmpresaLaboral = dr["OcupacionEmpresaLaboral"] != DBNull.Value ? dr["OcupacionEmpresaLaboral"].ToString() : "",
                            TelefonoTrabajo = dr["TelefonoTrabajo"] != DBNull.Value ? dr["TelefonoTrabajo"].ToString() : "",
                            InfoLaboral = dr["InfoLaboral"] != DBNull.Value ? dr["InfoLaboral"].ToString() : "",
                            CapacitacionesOCC = dr["CapacitacionesOCC"] != DBNull.Value ? dr["CapacitacionesOCC"].ToString() : "",
                            Ministerio = dr["Ministerio"] != DBNull.Value ? dr["Ministerio"].ToString() : "",
                            IdEquipo = dr["IdEquipo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipo"]) : null,
                            IdPosicion = dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null,
                            FechaIngreso = dr["FechaIngreso"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaIngreso"]) : null
                        };
                    }
                }
            }
            return null;
        }

        private bool PosicionEstaOcupada(int idEquipo, int idPosicion, int idUsuarioActual)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT COUNT(1) FROM (
                        SELECT a.IdPosicion 
                        FROM dbo.AsignacionesEquipo a 
                        INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                        WHERE a.IdEquipo = @IdEquipo AND a.IdPosicion = @IdPosicion AND a.Activo = 1 AND u.IdEstado IN (3, 4, 7, 8) AND a.IdUsuario <> @IdUsuario
                        UNION
                        SELECT p.IdPosicion 
                        FROM dbo.PerfilesCoordinador p 
                        INNER JOIN dbo.Usuarios u ON p.IdUsuario = u.IdUsuario 
                        WHERE p.IdEquipo = @IdEquipo AND p.IdPosicion = @IdPosicion AND u.IdEstado IN (3, 4, 7, 8) AND p.IdUsuario <> @IdUsuario
                    ) AS CheckOcupado;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdEquipo", idEquipo);
                cmd.Parameters.AddWithValue("@IdPosicion", idPosicion);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuarioActual);

                cn.Open();
                int cnt = Convert.ToInt32(cmd.ExecuteScalar());
                return cnt > 0;
            }
        }


        private void CargarCombosEquiposYPosiciones()
        {
            List<SelectListItem> listaEquipos = new List<SelectListItem>();
            List<SelectListItem> listaPosiciones = new List<SelectListItem>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlEquipos = "SELECT e.IdEquipo, e.NombreEquipo, n.NombreNivel FROM dbo.Equipos e INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo WHERE e.Activo = 1 ORDER BY n.RangoJerarquico, e.NombreEquipo;";
                using (SqlCommand cmd = new SqlCommand(sqlEquipos, cn))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            listaEquipos.Add(new SelectListItem
                            {
                                Value = dr["IdEquipo"].ToString(),
                                Text = $"[{dr["NombreNivel"]}] {dr["NombreEquipo"]}"
                            });
                        }
                    }
                }

                string sqlPos = "SELECT IdPosicion, NombrePosicion FROM dbo.PosicionesOCC ORDER BY IdPosicion;";
                using (SqlCommand cmdPos = new SqlCommand(sqlPos, cn))
                {
                    using (SqlDataReader drPos = cmdPos.ExecuteReader())
                    {
                        while (drPos.Read())
                        {
                            listaPosiciones.Add(new SelectListItem
                            {
                                Value = drPos["IdPosicion"].ToString(),
                                Text = drPos["NombrePosicion"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.ListaEquipos = listaEquipos;
            ViewBag.ListaPosiciones = listaPosiciones;
        }

        [HttpPost]
        [ValidarSesion]
        public ActionResult CambiarClavePerfil(string claveActual, string nuevaClave, string confirmarClave)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            if (string.IsNullOrWhiteSpace(claveActual) || string.IsNullOrWhiteSpace(nuevaClave) || string.IsNullOrWhiteSpace(confirmarClave))
            {
                TempData["ErrorClave"] = "Todos los campos de contraseña son obligatorios.";
                TempData["TabActiva"] = "seguridad";
                return RedirectToAction("RegistroPerfil");
            }

            if (nuevaClave != confirmarClave)
            {
                TempData["ErrorClave"] = "Las nuevas contraseñas no coinciden.";
                TempData["TabActiva"] = "seguridad";
                return RedirectToAction("RegistroPerfil");
            }

            string claveGuardada = null;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT Clave FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);

                cn.Open();
                claveGuardada = cmd.ExecuteScalar()?.ToString();
            }

            if (string.IsNullOrEmpty(claveGuardada) || !Helpers.Criptografia.VerificarClave(claveActual, claveGuardada))
            {
                TempData["ErrorClave"] = "La contraseña actual es incorrecta.";
                TempData["TabActiva"] = "seguridad";
                return RedirectToAction("RegistroPerfil");
            }

            // Hashear nueva contraseña
            string nuevaClaveFormateada = Helpers.Criptografia.CrearClaveFormateada(nuevaClave);

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Usuarios SET Clave = @Clave WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Clave", nuevaClaveFormateada);
                cmd.Parameters.AddWithValue("@IdUsuario", usuarioActual.IdUsuario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["ExitoClave"] = "Su contraseña ha sido cambiada exitosamente.";
            TempData["TabActiva"] = "seguridad";
            return RedirectToAction("RegistroPerfil");
        }
    }
}
