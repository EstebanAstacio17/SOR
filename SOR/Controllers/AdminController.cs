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
                cn.Open();

                // Saneamiento preventivo: inactivar registros de asignación cuyos usuarios estén inactivos o suspendidos
                // Estados válidos que conservan su asignación: 3 (Perfil Pendiente), 4 (Activo), 7 (Pend. Restablecimiento), 8 (Apro. Restablecimiento)
                string sqlSaneamiento = @"
                    UPDATE a
                    SET a.Activo = 0
                    FROM dbo.AsignacionesEquipo a
                    INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                    WHERE a.Activo = 1 AND u.IdEstado NOT IN (3, 4, 7, 8);";
                using (SqlCommand cmdSanear = new SqlCommand(sqlSaneamiento, cn))
                {
                    cmdSanear.ExecuteNonQuery();
                }

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

            CargarCombosAdmin(usuarioActual);
            ViewBag.UsuarioActual = usuarioActual;
            return View(listaUsuarios);
        }

        // ============================================================================
        // MÉTODOS AUXILIARES DE SEGURIDAD Y PROTECCIÓN DE SUPERADMIN
        // ============================================================================

        private bool PuedeModificarUsuarioObjetivo(Usuario usuarioActual, int idUsuarioObjetivo, out string mensajeError)
        {
            mensajeError = null;
            if (usuarioActual == null)
            {
                mensajeError = "No hay sesión de usuario activa.";
                return false;
            }

            // Si el usuario actual es Superadmin, tiene facultad total para administrar usuarios
            if (usuarioActual.IdRolSeguridad == 1)
            {
                return true;
            }

            // Consultar el rol del usuario objetivo directamente de la base de datos (seguridad server-side)
            int rolObjetivo = 0;
            string correoObjetivo = "";
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdRolSeguridad, Correo FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuarioObjetivo);
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            rolObjetivo = Convert.ToInt32(dr["IdRolSeguridad"]);
                            correoObjetivo = dr["Correo"].ToString();
                        }
                    }
                }
            }

            // Regla 1: Si el usuario objetivo es Superadmin (1), solo otro Superadmin puede modificarlo
            if (rolObjetivo == 1)
            {
                mensajeError = $"Acceso denegado: El usuario Superadmin ({correoObjetivo}) únicamente puede ser modificado o administrado por otro Superadmin.";
                SOR.Helpers.AuditoriaHelper.Registrar(
                    usuarioActual.IdUsuario,
                    usuarioActual.Correo,
                    "INTENTO_NO_AUTORIZADO_SUPERADMIN",
                    "ADMINISTRACION_USUARIOS",
                    idUsuarioObjetivo.ToString(),
                    $"El usuario '{usuarioActual.Correo}' (Rol: {usuarioActual.IdRolSeguridad}) intentó realizar una operación administrativa no autorizada sobre el Superadmin #{idUsuarioObjetivo} ({correoObjetivo})."
                );
                return false;
            }

            return true;
        }

        private bool EsUnicoSuperadminActivo(int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = @"
                    SELECT CASE 
                        WHEN u.IdRolSeguridad = 1 AND u.IdEstado = 4 AND 
                             (SELECT COUNT(1) FROM dbo.Usuarios WHERE IdRolSeguridad = 1 AND IdEstado = 4 AND IdUsuario <> @IdUsuario) = 0 
                        THEN 1 ELSE 0 END
                    FROM dbo.Usuarios u 
                    WHERE u.IdUsuario = @IdUsuario;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    object val = cmd.ExecuteScalar();
                    return val != null && Convert.ToInt32(val) == 1;
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarCorreo(int idUsuario, string correoCorregido)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
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
        [ValidateAntiForgeryToken]
        public ActionResult RechazarUsuario(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
            }

            if (EsUnicoSuperadminActivo(idUsuario))
            {
                TempData["MensajeError"] = "Operación denegada: No se puede rechazar ni suspender al único Superadmin activo del sistema.";
                return RedirectToAction("Usuarios");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                SqlTransaction tran = cn.BeginTransaction();
                try
                {
                    string sql = "UPDATE dbo.Usuarios SET IdEstado = 5 WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmd.ExecuteNonQuery();
                    }

                    string sqlDisable = "UPDATE dbo.AsignacionesEquipo SET Activo = 0 WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmdDis = new SqlCommand(sqlDisable, cn, tran))
                    {
                        cmdDis.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmdDis.ExecuteNonQuery();
                    }

                    SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, usuarioActual.IdUsuario, usuarioActual.Correo,
                        "RECHAZAR_USUARIO", "ADMINISTRACION_USUARIOS", idUsuario.ToString(), $"Solicitud de usuario #{idUsuario} rechazada.");

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["MensajeError"] = "Error al rechazar usuario: " + ex.Message;
                    return RedirectToAction("Usuarios");
                }
            }

            TempData["MensajeExito"] = "La solicitud de usuario ha sido rechazada.";
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarPerfil(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
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
                        string sqlCheck = @"
                            SELECT COUNT(1) 
                            FROM dbo.AsignacionesEquipo a
                            INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                            WHERE a.IdEquipo = @IdEquipo AND a.IdPosicion = @IdPosicion AND a.Activo = 1 AND u.IdEstado IN (3, 4, 7, 8) AND a.IdUsuario <> @IdUsuario;";
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

                        // Desactivar preventivamente asignaciones activas de esta posición si el dueño anterior está inactivo
                        string sqlDisableInactive = @"
                            UPDATE a
                            SET a.Activo = 0
                            FROM dbo.AsignacionesEquipo a
                            INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                            WHERE a.IdEquipo = @IdEquipo AND a.IdPosicion = @IdPosicion AND a.Activo = 1 AND u.IdEstado NOT IN (3, 4, 7, 8);";
                        using (SqlCommand cmdDisInactive = new SqlCommand(sqlDisableInactive, cn, tran))
                        {
                            cmdDisInactive.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdDisInactive.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdDisInactive.ExecuteNonQuery();
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
        [ValidateAntiForgeryToken]
        public ActionResult EditarUsuarioAdmin(int idUsuario, int idRolSeguridad, int idEstado, int? idEquipo, int? idPosicion)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            // Solo SuperAdmin (1) o Admin (2)
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                TempData["MensajeError"] = "Permiso denegado para editar usuarios.";
                return RedirectToAction("Usuarios");
            }

            // Regla 1: Superadmin solo puede ser editado por otro Superadmin
            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
            }

            // Prevención de Escalamiento de Privilegios: Solo Superadmin (1) puede asignar o quitar roles de Admin / SuperAdmin
            if (idRolSeguridad != 3 && usuarioActual.IdRolSeguridad != 1)
            {
                TempData["MensajeError"] = "Acceso denegado: Solo un Superadmin puede asignar roles de Administrador o Superadmin.";
                SOR.Helpers.AuditoriaHelper.Registrar(usuarioActual.IdUsuario, usuarioActual.Correo,
                    "INTENTO_ESCALAMIENTO_ROL", "ADMINISTRACION_USUARIOS", idUsuario.ToString(),
                    $"Intento no autorizado de asignar rol {idRolSeguridad} al usuario #{idUsuario}.");
                return RedirectToAction("Usuarios");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                SqlTransaction tran = cn.BeginTransaction();

                try
                {
                    // 0. Obtener estado actual del usuario objetivo en la base de datos
                    int rolAnterior = 0;
                    int estadoAnterior = 0;
                    string correoUsuario = "";
                    using (SqlCommand cmdGet = new SqlCommand("SELECT IdRolSeguridad, IdEstado, Correo FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario;", cn, tran))
                    {
                        cmdGet.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        using (SqlDataReader dr = cmdGet.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                rolAnterior = Convert.ToInt32(dr["IdRolSeguridad"]);
                                estadoAnterior = Convert.ToInt32(dr["IdEstado"]);
                                correoUsuario = dr["Correo"].ToString();
                            }
                            else
                            {
                                tran.Rollback();
                                TempData["MensajeError"] = "El usuario seleccionado no existe.";
                                return RedirectToAction("Usuarios");
                            }
                        }
                    }

                    // Regla de Prevención de Orfandad: Si el usuario es el único Superadmin activo, no permitir quitarle el rol ni suspenderlo
                    if (rolAnterior == 1 && estadoAnterior == 4 && (idRolSeguridad != 1 || idEstado != 4))
                    {
                        int otrosSuperadminsActivos = 0;
                        using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(1) FROM dbo.Usuarios WHERE IdRolSeguridad = 1 AND IdEstado = 4 AND IdUsuario <> @IdUsuario;", cn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            otrosSuperadminsActivos = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        }

                        if (otrosSuperadminsActivos == 0)
                        {
                            tran.Rollback();
                            TempData["MensajeError"] = "Operación denegada: No se puede revocar el rol ni suspender al único Superadmin activo del sistema. La plataforma requiere obligatoriamente un Superadmin activo. Para cambiar de titular, utilice la opción de Reemplazo / Transferencia de Superadmin.";
                            return RedirectToAction("Usuarios");
                        }
                    }

                    // Regla 2: Máximo 1 Superadmin activo simultáneamente
                    // Si se intenta activar un Superadmin cuando ya existe uno activo diferente
                    if (idRolSeguridad == 1 && idEstado == 4 && !(rolAnterior == 1 && estadoAnterior == 4))
                    {
                        int superadminsActivosActuales = 0;
                        using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(1) FROM dbo.Usuarios WHERE IdRolSeguridad = 1 AND IdEstado = 4 AND IdUsuario <> @IdUsuario;", cn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            superadminsActivosActuales = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        }

                        if (superadminsActivosActuales > 0)
                        {
                            tran.Rollback();
                            TempData["MensajeError"] = "Operación denegada: Ya existe un Superadmin activo en la plataforma. Debe transferir o reemplazar al Superadmin actual antes de activar uno nuevo.";
                            return RedirectToAction("Usuarios");
                        }
                    }

                    // 1. Actualizar Rol y Estado de Usuario
                    string sqlUser = "UPDATE dbo.Usuarios SET IdRolSeguridad = @IdRolSeguridad, IdEstado = @IdEstado WHERE IdUsuario = @IdUsuario;";
                    using (SqlCommand cmdU = new SqlCommand(sqlUser, cn, tran))
                    {
                        cmdU.Parameters.AddWithValue("@IdRolSeguridad", idRolSeguridad);
                        cmdU.Parameters.AddWithValue("@IdEstado", idEstado);
                        cmdU.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmdU.ExecuteNonQuery();
                    }

                    // Si el estado es Suspendido (6) o Rechazado (5), inactivar todas sus asignaciones en el equipo
                    if (idEstado == 5 || idEstado == 6)
                    {
                        string sqlDisable = "UPDATE dbo.AsignacionesEquipo SET Activo = 0 WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdDis = new SqlCommand(sqlDisable, cn, tran))
                        {
                            cmdDis.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdDis.ExecuteNonQuery();
                        }
                    }

                    // 2. Si se especificó equipo y posición, validar y actualizar
                    if (idEquipo.HasValue && idPosicion.HasValue)
                    {
                        string sqlCheck = @"
                            SELECT COUNT(1) 
                            FROM dbo.AsignacionesEquipo a
                            INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                            WHERE a.IdEquipo = @IdEquipo AND a.IdPosicion = @IdPosicion AND a.Activo = 1 AND u.IdEstado IN (3, 4, 7, 8) AND a.IdUsuario <> @IdUsuario;";
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

                        // Desactivar preventivamente asignaciones activas de esta posición si el dueño anterior está inactivo
                        string sqlDisableInactive = @"
                            UPDATE a
                            SET a.Activo = 0
                            FROM dbo.AsignacionesEquipo a
                            INNER JOIN dbo.Usuarios u ON a.IdUsuario = u.IdUsuario
                            WHERE a.IdEquipo = @IdEquipo AND a.IdPosicion = @IdPosicion AND a.Activo = 1 AND u.IdEstado NOT IN (3, 4, 7, 8);";
                        using (SqlCommand cmdDisInactive = new SqlCommand(sqlDisableInactive, cn, tran))
                        {
                            cmdDisInactive.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                            cmdDisInactive.Parameters.AddWithValue("@IdPosicion", idPosicion.Value);
                            cmdDisInactive.ExecuteNonQuery();
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

                    // 3. Registrar auditoría exhaustiva
                    string operacionAuditoria = (rolAnterior == 1 || idRolSeguridad == 1) ? "EDITAR_SUPERADMIN" : "EDITAR_USUARIO";
                    string detalleAuditoria = $"Edición de usuario #{idUsuario} ({correoUsuario}): Rol anterior: {rolAnterior} -> Rol nuevo: {idRolSeguridad}, Estado anterior: {estadoAnterior} -> Estado nuevo: {idEstado}.";
                    SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, usuarioActual.IdUsuario, usuarioActual.Correo,
                        operacionAuditoria, "ADMINISTRACION_USUARIOS", idUsuario.ToString(), detalleAuditoria);

                    tran.Commit();

                    // Si el usuario modificó su propia cuenta, refrescar la sesión
                    if (usuarioActual.IdUsuario == idUsuario)
                    {
                        usuarioActual.IdRolSeguridad = idRolSeguridad;
                        usuarioActual.IdEstado = idEstado;
                        Session["usuario"] = usuarioActual;
                    }

                    TempData["MensajeExito"] = "Datos y permisos de usuario actualizados correctamente.";
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                {
                    tran.Rollback();
                    TempData["MensajeError"] = "Restricción de integridad en base de datos: Solamente puede existir un Superadmin activo simultáneamente en la plataforma.";
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["MensajeError"] = "Error al actualizar usuario: " + ex.Message;
                }
            }

            return RedirectToAction("Usuarios");
        }

        // ============================================================================
        // REEMPLAZO / TRANSFERENCIA ATÓMICA DE TITULARIDAD DE SUPERADMIN
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReemplazarSuperadmin(int idNuevoSuperadmin, int idRolAnteriorSuperadmin, string motivoTransferencia)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];

            // Solo el Superadmin activo actual puede transferir la titularidad
            if (usuarioActual == null || usuarioActual.IdRolSeguridad != 1)
            {
                TempData["MensajeError"] = "Acceso denegado: Solo el Superadmin activo puede transferir la titularidad de la cuenta Superadmin.";
                return RedirectToAction("Usuarios");
            }

            if (usuarioActual.IdUsuario == idNuevoSuperadmin)
            {
                TempData["MensajeError"] = "No puede transferir la titularidad de Superadmin a usted mismo. Seleccione a otro usuario de la plataforma.";
                return RedirectToAction("Usuarios");
            }

            if (string.IsNullOrWhiteSpace(motivoTransferencia))
            {
                TempData["MensajeError"] = "Debe indicar obligatoriamente el motivo de la transferencia de Superadmin.";
                return RedirectToAction("Usuarios");
            }

            // El rol al que pasará el Superadmin anterior debe ser Administrador (2) o Coordinador (3)
            if (idRolAnteriorSuperadmin != 2 && idRolAnteriorSuperadmin != 3)
            {
                idRolAnteriorSuperadmin = 2; // Default a Administrador
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Verificar existencia y estado del nuevo usuario
                        string correoNuevo = "";
                        int estadoNuevo = 0;
                        using (SqlCommand cmdGet = new SqlCommand("SELECT Correo, IdEstado FROM dbo.Usuarios WHERE IdUsuario = @IdNuevo;", cn, tran))
                        {
                            cmdGet.Parameters.AddWithValue("@IdNuevo", idNuevoSuperadmin);
                            using (SqlDataReader dr = cmdGet.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    correoNuevo = dr["Correo"].ToString();
                                    estadoNuevo = Convert.ToInt32(dr["IdEstado"]);
                                }
                                else
                                {
                                    tran.Rollback();
                                    TempData["MensajeError"] = "El usuario seleccionado como nuevo Superadmin no existe.";
                                    return RedirectToAction("Usuarios");
                                }
                            }
                        }

                        // 2. Degradar al Superadmin anterior en la misma transacción atómica
                        string sqlAnterior = "UPDATE dbo.Usuarios SET IdRolSeguridad = @IdRolAnterior WHERE IdUsuario = @IdActual;";
                        using (SqlCommand cmdAnt = new SqlCommand(sqlAnterior, cn, tran))
                        {
                            cmdAnt.Parameters.AddWithValue("@IdRolAnterior", idRolAnteriorSuperadmin);
                            cmdAnt.Parameters.AddWithValue("@IdActual", usuarioActual.IdUsuario);
                            cmdAnt.ExecuteNonQuery();
                        }

                        // 3. Asignar el rol Superadmin (1) y activar (4) al nuevo Superadmin
                        string sqlNuevo = "UPDATE dbo.Usuarios SET IdRolSeguridad = 1, IdEstado = 4 WHERE IdUsuario = @IdNuevo;";
                        using (SqlCommand cmdNue = new SqlCommand(sqlNuevo, cn, tran))
                        {
                            cmdNue.Parameters.AddWithValue("@IdNuevo", idNuevoSuperadmin);
                            cmdNue.ExecuteNonQuery();
                        }

                        // 4. Registrar auditoría atómica
                        string detalleAuditoria = $"Transferencia formal de titularidad de Superadmin. Titular anterior: #{usuarioActual.IdUsuario} ({usuarioActual.Correo}) pasa a rol {idRolAnteriorSuperadmin}. Nuevo titular: #{idNuevoSuperadmin} ({correoNuevo}) pasa a SuperAdmin Activo. Motivo: {motivoTransferencia.Trim()}.";
                        SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, usuarioActual.IdUsuario, usuarioActual.Correo,
                            "TRANSFERENCIA_SUPERADMIN", "ADMINISTRACION_USUARIOS", idNuevoSuperadmin.ToString(), detalleAuditoria);

                        tran.Commit();

                        // Actualizar la sesión del usuario actual que cedió el rol
                        usuarioActual.IdRolSeguridad = idRolAnteriorSuperadmin;
                        Session["usuario"] = usuarioActual;

                        TempData["MensajeExito"] = $"La titularidad de Superadmin ha sido transferida exitosamente a '{correoNuevo}'. Su usuario ahora tiene rol de {(idRolAnteriorSuperadmin == 2 ? "Administrador" : "Coordinador")}.";
                    }
                    catch (SqlException sqlEx) when (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Restricción de integridad en base de datos: Solamente puede existir un Superadmin activo simultáneamente en la plataforma.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al transferir la titularidad de Superadmin: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Usuarios");
        }

        private void CargarCombosAdmin(Usuario usuarioActual = null)
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
                    {
                        int idRol = Convert.ToInt32(dr["IdRolSeguridad"]);
                        // Si el usuario actual no es Superadmin, no mostrar la opción de Superadmin para asignar
                        if (idRol == 1 && (usuarioActual == null || usuarioActual.IdRolSeguridad != 1))
                        {
                            continue;
                        }
                        listaRoles.Add(new SelectListItem { Value = idRol.ToString(), Text = dr["NombreRol"].ToString() });
                    }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarRestablecimiento(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Cambiar estado a Aprobado Restablecimiento (8)
                        string sql = "UPDATE dbo.Usuarios SET IdEstado = 8 WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Auto-restaurar asignación de equipo si fue desactivada durante el proceso
                        string sqlRestoreCheck = @"
                            SELECT p.IdEquipo, p.IdPosicion
                            FROM dbo.PerfilesCoordinador p
                            WHERE p.IdUsuario = @IdUsuario 
                              AND p.IdEquipo IS NOT NULL 
                              AND p.IdPosicion IS NOT NULL
                              AND NOT EXISTS (
                                  SELECT 1 FROM dbo.AsignacionesEquipo a 
                                  WHERE a.IdUsuario = @IdUsuario AND a.Activo = 1
                              );";
                        using (SqlCommand cmdRestore = new SqlCommand(sqlRestoreCheck, cn, tran))
                        {
                            cmdRestore.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            using (SqlDataReader dr = cmdRestore.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    int idEquipo = Convert.ToInt32(dr["IdEquipo"]);
                                    int idPosicion = Convert.ToInt32(dr["IdPosicion"]);
                                    dr.Close();

                                    string sqlIns = "INSERT INTO dbo.AsignacionesEquipo (IdUsuario, IdEquipo, IdPosicion, Activo) VALUES (@IdUsuario, @IdEquipo, @IdPosicion, 1);";
                                    using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                                    {
                                        cmdIns.Parameters.AddWithValue("@IdUsuario", idUsuario);
                                        cmdIns.Parameters.AddWithValue("@IdEquipo", idEquipo);
                                        cmdIns.Parameters.AddWithValue("@IdPosicion", idPosicion);
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, usuarioActual.IdUsuario, usuarioActual.Correo,
                            "APROBAR_RESTABLECIMIENTO", "ADMINISTRACION_USUARIOS", idUsuario.ToString(), $"Aprobada solicitud de restablecimiento para usuario #{idUsuario}.");

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error al aprobar el restablecimiento.";
                        return RedirectToAction("Usuarios");
                    }
                }
            }

            TempData["MensajeExito"] = "La solicitud de restablecimiento ha sido aprobada. El usuario podrá colocar su nueva clave al ingresar su correo.";
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarRestablecimiento(int idUsuario)
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!PuedeModificarUsuarioObjetivo(usuarioActual, idUsuario, out string errPermiso))
            {
                TempData["MensajeError"] = errPermiso;
                return RedirectToAction("Usuarios");
            }

            if (EsUnicoSuperadminActivo(idUsuario))
            {
                TempData["MensajeError"] = "Operación denegada: No se puede suspender al único Superadmin activo del sistema mediante rechazo de restablecimiento.";
                return RedirectToAction("Usuarios");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Cambiar estado a Suspendido (6)
                        string sqlUser = "UPDATE dbo.Usuarios SET IdEstado = 6 WHERE IdUsuario = @IdUsuario;";
                        using (SqlCommand cmdUser = new SqlCommand(sqlUser, cn, tran))
                        {
                            cmdUser.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdUser.ExecuteNonQuery();
                        }

                        // 2. Liberar su posición/rol en el equipo (Activo = 0 en AsignacionesEquipo)
                        string sqlAsig = "UPDATE dbo.AsignacionesEquipo SET Activo = 0 WHERE IdUsuario = @IdUsuario AND Activo = 1;";
                        using (SqlCommand cmdAsig = new SqlCommand(sqlAsig, cn, tran))
                        {
                            cmdAsig.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmdAsig.ExecuteNonQuery();
                        }

                        SOR.Helpers.AuditoriaHelper.Registrar(cn, tran, usuarioActual.IdUsuario, usuarioActual.Correo,
                            "RECHAZAR_RESTABLECIMIENTO", "ADMINISTRACION_USUARIOS", idUsuario.ToString(), $"Rechazada solicitud de restablecimiento para usuario #{idUsuario}. Cuenta suspendida.");

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error al procesar el rechazo de restablecimiento.";
                        return RedirectToAction("Usuarios");
                    }
                }
            }

            TempData["MensajeExito"] = "La solicitud de restablecimiento fue rechazada. La cuenta del usuario ha sido suspendida y su rol ha sido liberado.";
            return RedirectToAction("Usuarios");
        }

        // ============================================================================
        // MANTENEDOR DE CATÁLOGOS: DENOMINACIONES Y TIPOS DE ORGANIZACIÓN
        // ============================================================================

        private void AsegurarTablasCatalogos()
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    IF OBJECT_ID('dbo.Denominaciones', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Denominaciones (
                            IdDenominacion INT IDENTITY(1,1) PRIMARY KEY,
                            Nombre NVARCHAR(255) NOT NULL,
                            Activo BIT NOT NULL DEFAULT 1
                        );
                        INSERT INTO dbo.Denominaciones (Nombre, Activo) VALUES 
                        ('Asambleas de Dios', 1),
                        ('Bautista', 1),
                        ('Metodista', 1),
                        ('Iglesia de Dios', 1),
                        ('Pentecostal', 1),
                        ('Independiente / No Denominacional', 1),
                        ('Alianza Cristiana y Misionera', 1);
                    END

                    IF OBJECT_ID('dbo.TiposOrganizacion', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.TiposOrganizacion (
                            IdTipoOrg INT IDENTITY(1,1) PRIMARY KEY,
                            Nombre NVARCHAR(255) NOT NULL,
                            Activo BIT NOT NULL DEFAULT 1
                        );
                        INSERT INTO dbo.TiposOrganizacion (Nombre, Activo) VALUES 
                        ('Iglesia Local', 1),
                        ('Misión / Extensión', 1),
                        ('Ministerio Paraeclesiástico', 1),
                        ('Fundación / ONG', 1),
                        ('Colegio Cristiano', 1);
                    END
                    
                    IF OBJECT_ID('dbo.RolesEvento', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.RolesEvento (
                            IdRolEvento INT IDENTITY(1,1) PRIMARY KEY,
                            Nombre NVARCHAR(150) NOT NULL,
                            Descripcion NVARCHAR(255) NULL,
                            Activo BIT NOT NULL DEFAULT 1,
                            FechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
                        );
                        INSERT INTO dbo.RolesEvento (Nombre, Descripcion, Activo, FechaCreacion) VALUES 
                        ('Coordinador Principal / Encargado', 'Responsable general de la conducción del evento', 1, GETDATE()),
                        ('Facilitador / Expositor', 'Imparte el contenido, dinámicas o presentaciones del evento', 1, GETDATE()),
                        ('Logística y Despacho', 'Coordinación de paquetes, materiales y suministros', 1, GETDATE()),
                        ('Registro y Asistencia', 'Mesa de recepción, validación de cédulas y asistencia', 1, GETDATE()),
                        ('Acompañamiento y Bienvenida', 'Atención personalizada a pastores y líderes asistentes', 1, GETDATE()),
                        ('Intercesión y Oración', 'Cobertura espiritual y oración durante el desarrollo del evento', 1, GETDATE()),
                        ('Apoyo General', 'Soporte y asistencia operativa en diversas áreas', 1, GETDATE());
                    END
                    ELSE
                    BEGIN
                        IF COL_LENGTH('dbo.RolesEvento', 'FechaCreacion') IS NULL
                        BEGIN
                            ALTER TABLE dbo.RolesEvento ADD FechaCreacion DATETIME NOT NULL DEFAULT GETDATE();
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (
                                SELECT 1 FROM sys.default_constraints dc 
                                JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                                WHERE parent_object_id = OBJECT_ID('dbo.RolesEvento') AND c.name = 'FechaCreacion'
                            )
                            BEGIN
                                BEGIN TRY
                                    ALTER TABLE dbo.RolesEvento ADD CONSTRAINT DF_RolesEvento_FechaCreacion DEFAULT GETDATE() FOR FechaCreacion;
                                END TRY
                                BEGIN CATCH
                                END CATCH
                            END
                        END
                    END";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // GET: Admin/Catalogos
        public ActionResult Catalogos()
        {
            Usuario usuarioActual = (Usuario)Session["usuario"];
            if (usuarioActual.IdRolSeguridad != 1 && usuarioActual.IdRolSeguridad != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            AsegurarTablasCatalogos();

            List<CatalogoItemViewModel> denominaciones = new List<CatalogoItemViewModel>();
            List<CatalogoItemViewModel> tiposOrg = new List<CatalogoItemViewModel>();
            List<CatalogoItemViewModel> rolesEvento = new List<CatalogoItemViewModel>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlD = "SELECT IdDenominacion AS Id, Nombre, Activo FROM dbo.Denominaciones ORDER BY Nombre;";
                using (SqlCommand cmdD = new SqlCommand(sqlD, cn))
                using (SqlDataReader drD = cmdD.ExecuteReader())
                {
                    while (drD.Read())
                    {
                        denominaciones.Add(new CatalogoItemViewModel
                        {
                            Id = Convert.ToInt32(drD["Id"]),
                            Nombre = drD["Nombre"].ToString(),
                            Activo = Convert.ToBoolean(drD["Activo"])
                        });
                    }
                }

                string sqlT = "SELECT IdTipoOrg AS Id, Nombre, Activo FROM dbo.TiposOrganizacion ORDER BY Nombre;";
                using (SqlCommand cmdT = new SqlCommand(sqlT, cn))
                using (SqlDataReader drT = cmdT.ExecuteReader())
                {
                    while (drT.Read())
                    {
                        tiposOrg.Add(new CatalogoItemViewModel
                        {
                            Id = Convert.ToInt32(drT["Id"]),
                            Nombre = drT["Nombre"].ToString(),
                            Activo = Convert.ToBoolean(drT["Activo"])
                        });
                    }
                }

                string sqlR = "SELECT IdRolEvento AS Id, Nombre, Descripcion, Activo FROM dbo.RolesEvento ORDER BY IdRolEvento ASC;";
                using (SqlCommand cmdR = new SqlCommand(sqlR, cn))
                using (SqlDataReader drR = cmdR.ExecuteReader())
                {
                    while (drR.Read())
                    {
                        rolesEvento.Add(new CatalogoItemViewModel
                        {
                            Id = Convert.ToInt32(drR["Id"]),
                            Nombre = drR["Nombre"].ToString(),
                            Descripcion = drR["Descripcion"] != DBNull.Value ? drR["Descripcion"].ToString() : "",
                            Activo = Convert.ToBoolean(drR["Activo"])
                        });
                    }
                }
            }

            ViewBag.Denominaciones = denominaciones;
            ViewBag.TiposOrg = tiposOrg;
            ViewBag.RolesEvento = rolesEvento;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearDenominacion(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                AsegurarTablasCatalogos();
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "INSERT INTO dbo.Denominaciones (Nombre, Activo) VALUES (@Nombre, 1);";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = "Denominación agregada correctamente.";
            }
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleDenominacion(int id, bool activo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Denominaciones SET Activo = @Activo WHERE IdDenominacion = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["MensajeExito"] = "Estado de denominación actualizado.";
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearTipoOrg(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                AsegurarTablasCatalogos();
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "INSERT INTO dbo.TiposOrganizacion (Nombre, Activo) VALUES (@Nombre, 1);";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = "Tipo de organización agregado correctamente.";
            }
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTipoOrg(int id, bool activo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.TiposOrganizacion SET Activo = @Activo WHERE IdTipoOrg = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["MensajeExito"] = "Estado de tipo de organización actualizado.";
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearRolEvento(string nombre, string descripcion)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                AsegurarTablasCatalogos();
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "INSERT INTO dbo.RolesEvento (Nombre, Descripcion, Activo, FechaCreacion) VALUES (@Nombre, @Descripcion, 1, GETDATE());";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@Descripcion", (object)descripcion?.Trim() ?? DBNull.Value);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = "Rol/Función de evento agregado correctamente.";
            }
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarRolEvento(int id, string nombre, string descripcion)
        {
            if (id > 0 && !string.IsNullOrWhiteSpace(nombre))
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "UPDATE dbo.RolesEvento SET Nombre = @Nombre, Descripcion = @Descripcion WHERE IdRolEvento = @Id;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@Descripcion", (object)descripcion?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = "Rol/Función de evento modificado correctamente.";
            }
            return RedirectToAction("Catalogos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleRolEvento(int id, bool activo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.RolesEvento SET Activo = @Activo WHERE IdRolEvento = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@Id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["MensajeExito"] = "Estado de rol/función de evento actualizado.";
            return RedirectToAction("Catalogos");
        }
    }

    public class CatalogoItemViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}
