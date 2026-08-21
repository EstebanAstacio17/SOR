using SOR.Models;
using SOR.Permisos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class AdminController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Admin/Usuarios
        public ActionResult Usuarios()
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            // Verificar si el usuario actual es Admin o SuperAdmin
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            List<SolicitudUsuarioViewModel> listaUsuarios = new List<SolicitudUsuarioViewModel>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 
                        u.IdUsuario, u.Correo, u.IdRolSeguridad, r.NombreRol, 
                        u.IdEstado, e.NombreEstado, u.FechaRegistro,
                        p.*, eq.NombreEquipo, pos.NombrePosicion
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.RolesSeguridad r ON u.IdRolSeguridad = r.IdRolSeguridad
                    INNER JOIN dbo.EstadosCuenta e ON u.IdEstado = e.IdEstado
                    LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                    LEFT JOIN dbo.Equipos eq ON p.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.PosicionesOCC pos ON p.IdPosicion = pos.IdPosicion
                    ORDER BY u.FechaRegistro DESC;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var userVm = new SolicitudUsuarioViewModel
                        {
                            IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                            Correo = dr["Correo"].ToString(),
                            IdRolSeguridad = Convert.ToInt32(dr["IdRolSeguridad"]),
                            NombreRol = dr["NombreRol"].ToString(),
                            IdEstado = Convert.ToInt32(dr["IdEstado"]),
                            NombreEstado = dr["NombreEstado"].ToString(),
                            FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaRegistro"]) : null,

                            Perfil = new PerfilCoordinador
                            {
                                IdPerfil = dr["IdPerfil"] != DBNull.Value ? Convert.ToInt32(dr["IdPerfil"]) : 0,
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                PrimerNombre = dr["PrimerNombre"] != DBNull.Value ? dr["PrimerNombre"].ToString() : "",
                                OtrosNombres = dr["OtrosNombres"] != DBNull.Value ? dr["OtrosNombres"].ToString() : "",
                                PrimerApellido = dr["PrimerApellido"] != DBNull.Value ? dr["PrimerApellido"].ToString() : "",
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
                                NombreEquipo = dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString() : "",
                                IdPosicion = dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null,
                                NombrePosicion = dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString() : "",
                                FechaIngreso = dr["FechaIngreso"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaIngreso"]) : null
                            }
                        };
                        listaUsuarios.Add(userVm);
                    }
                }
            }

            CargarCombosAdmin();
            ViewBag.UsuarioActual = usuarioActual;
            return View(listaUsuarios);
        }

        [HttpPost]
        public ActionResult AprobarCorreo(int idUsuario, string correoCorregido)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                // Actualizar estado a CorreoAprobado (2) y corregir correo si hubo errata
                string sql = "UPDATE dbo.Usuarios SET IdEstado = 2, Correo = ISNULL(NULLIF(@Correo, ''), Correo) WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Correo", correoCorregido ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Correo aprobado con éxito. Se habilitó el enlace para completar el Perfil de Coordinador.";
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult RechazarUsuario(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Usuarios SET IdEstado = 5 WHERE IdUsuario = @IdUsuario;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "La solicitud de usuario ha sido rechazada.";
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult AprobarPerfil(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                SqlTransaction tran = cn.BeginTransaction();

                try
                {
                    int? idEquipo = null;
                    int? idPosicion = null;

                    string sqlGetPerfil = "SELECT IdEquipo, IdPosicion FROM dbo.PerfilesCoordinador WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmdGet = new SqlCommand(sqlGetPerfil, cn, tran))
                    {
                        cmdGet.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        using (SqlDataReader dr = cmdGet.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                idEquipo = dr["IdEquipo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipo"]) : null;
                                idPosicion = dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null;
                            }
                        }
                    }

                    // Verificar unicidad de posición si fue seleccionada
                    if (idEquipo.HasValue && idPosicion.HasValue)
                    {
                        string sqlCheck = "SELECT COUNT(1) FROM dbo.AsignacionesEquipo WHERE IdEquipo = @IdEquipo AND IdPosicion = @IdPosicion AND Activo = 1 AND IdUsuario <> @IdUsuario;";
                        using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdCheck.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdCheck.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            int ocupado = Convert.ToInt32(cmdCheck.ExecuteScalar());

                            if (ocupado > 0)
                            {
                                tran.Rollback();
                                TempData["MensajeError"] = "La posición seleccionada ya está ocupada por otro usuario activo en ese equipo.";
                                return RedirectToAction("Usuarios");
                            }
                        }

                        // Desactivar asignaciones anteriores y asignar nueva
                        string sqlDisable = "UPDATE dbo.AsignacionesEquipo SET Activo = 0 WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdDis = new SqlCommand(sqlDisable, cn, tran))
                        {
                            cmdDis.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdDis.ExecuteNonQuery();
                        }

                        string sqlInsAsig = "INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo) VALUES (@IdUsuario, @IdEquipo, @IdPosicion, 1);";
                        using (SqlCommand cmdIns = new SqlCommand(sqlInsAsig, cn, tran))
                        {
                            cmdIns.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdIns.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdIns.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdIns.ExecuteNonQuery();
                        }
                    }

                    // Actualizar estado a Activo (4)
                    string sqlUpdateUser = "UPDATE dbo.Usuarios SET IdEstado = 4 WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmdUpd = new SqlCommand(sqlUpdateUser, cn, tran))
                    {
                        cmdUpd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmdUpd.ExecuteNonQuery();
                    }

                    tran.Commit();
                    TempData["MensajeExito"] = "Perfil de Coordinador aprobado con éxito. El usuario está plenamente activo.";
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["MensajeError"] = "Error al aprobar perfil: " + ex.Message;
                }
            }

            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult EditarUsuarioAdmin(int idUsuario, int idRolSeguridad, int idEstado, int? idEquipo, int? idPosicion)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            // Solo SuperAdmin (1) o Admin (2)
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                TempData["MensajeError"] = "Permiso denegado para editar usuarios.";
                return RedirectToAction("Usuarios");
            }

            // Solo SuperAdmin (1) puede asignar o quitar roles de Admin / SuperAdmin
            if (idRolSeguridad != 3 && usuarioActual.IdRolSeguridad != 1)
            {
                TempData["MensajeError"] = "Solo un Super Admin puede asignar roles de Administrador o Super Admin.";
                return RedirectToAction("Usuarios");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                SqlTransaction tran = cn.BeginTransaction();

                try
                {
                    // 1. Actualizar Rol y Estado de Usuario
                    string sqlUser = "UPDATE dbo.Usuarios SET IdRolSeguridad = @IdRolSeguridad, IdEstado = @IdEstado WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmdU = new SqlCommand(sqlUser, cn, tran))
                    {
                        cmdU.Parameters.AddWithValue("@IdRolSeguridad", idRolSeguridad);
                        cmdU.Parameters.AddWithValue("@IdEstado", idEstado);
                        cmdU.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmdU.ExecuteNonQuery();
                    }

                    // 2. Si se especificó equipo y posición, validar y actualizar
                    if (idEquipo.HasValue && idPosicion.HasValue)
                    {
                        string sqlCheck = "SELECT COUNT(1) FROM dbo.AsignacionesEquipo WHERE IdEquipo = @IdEquipo AND IdPosicion = @IdPosicion AND Activo = 1 AND IdUsuario <> @IdUsuario;";
                        using (SqlCommand cmdC = new SqlCommand(sqlCheck, cn, tran))
                        {
                            cmdC.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdC.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdC.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            int cnt = Convert.ToInt32(cmdC.ExecuteScalar());

                            if (cnt > 0)
                            {
                                tran.Rollback();
                                TempData["MensajeError"] = "La posición seleccionada ya está ocupada en ese equipo.";
                                return RedirectToAction("Usuarios");
                            }
                        }

                        // Actualizar perfil
                        string sqlPerf = "UPDATE dbo.PerfilesCoordinador SET IdEquipo = @IdEquipo, IdPosicion = @IdPosicion WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdP = new SqlCommand(sqlPerf, cn, tran))
                        {
                            cmdP.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdP.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdP.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdP.ExecuteNonQuery();
                        }

                        // Actualizar asignación activa
                        string sqlDis = "UPDATE dbo.AsignacionesEquipo SET Activo = 0 WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdD = new SqlCommand(sqlDis, cn, tran))
                        {
                            cmdD.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdD.ExecuteNonQuery();
                        }

                        string sqlIns = "INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo) VALUES (@IdUsuario, @IdEquipo, @IdPosicion, 1);";
                        using (SqlCommand cmdI = new SqlCommand(sqlIns, cn, tran))
                        {
                            cmdI.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdI.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdI.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdI.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                    TempData["MensajeExito"] = "Datos y permisos de usuario actualizados correctamente.";
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["MensajeError"] = "Error al actualizar usuario: " + ex.Message;
                }
            }

            return RedirectToAction("Usuarios");
        }

        private void CargarCombosAdmin()
        {
            List<SelectListItem> listaRoles = new List<SelectListItem>();
            List<SelectListItem> listaEstados = new List<SelectListItem>();
            List<SelectListItem> listaEquipos = new List<SelectListItem>();
            List<SelectListItem> listaPosiciones = new List<SelectListItem>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT IdRolSeguridad, NombreRol FROM dbo.RolesSeguridad ORDER BY IdRolSeguridad;", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        listaRoles.Add(new SelectListItem { Value = dr["IdRolSeguridad"].ToString(), Text = dr["NombreRol"].ToString() });
                }

                using (SqlCommand cmd = new SqlCommand("SELECT IdEstado, NombreEstado FROM dbo.EstadosCuenta ORDER BY IdEstado;", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        listaEstados.Add(new SelectListItem { Value = dr["IdEstado"].ToString(), Text = dr["NombreEstado"].ToString() });
                }

                using (SqlCommand cmd = new SqlCommand("SELECT e.IdEquipo, e.NombreEquipo, n.NombreNivel FROM dbo.Equipos e INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo ORDER BY n.RangoJerarquico, e.NombreEquipo;", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        listaEquipos.Add(new SelectListItem { Value = dr["IdEquipo"].ToString(), Text = $"[{dr["NombreNivel"]}] {dr["NombreEquipo"]}" });
                }

                using (SqlCommand cmd = new SqlCommand("SELECT IdPosicion, NombrePosicion FROM dbo.PosicionesOCC ORDER BY IdPosicion;", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        listaPosiciones.Add(new SelectListItem { Value = dr["IdPosicion"].ToString(), Text = dr["NombrePosicion"].ToString() });
                }
            }

            ViewBag.ListaRolesAdmin = listaRoles;
            ViewBag.ListaEstadosAdmin = listaEstados;
            ViewBag.ListaEquiposAdmin = listaEquipos;
            ViewBag.ListaPosicionesAdmin = listaPosiciones;
        }
    }
}
