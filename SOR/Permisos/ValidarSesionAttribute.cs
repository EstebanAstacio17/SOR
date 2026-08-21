using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using SOR.Controllers;
using SOR.Models;

namespace SOR.Permisos
{
    public class ValidarSesionAttribute : ActionFilterAttribute
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;

            // 1. Verificar existencia del objeto de sesión
            if (session["usuario"] == null)
            {
                filterContext.Result = new RedirectResult("~/Acceso/Login");
                return;
            }

            Usuario uSesion = (Usuario)session["usuario"];

            // 2. Control de Inactividad por 5 minutos
            if (session["UltimoAcceso"] != null)
            {
                DateTime ultimoAcceso = (DateTime)session["UltimoAcceso"];
                TimeSpan inactividad = DateTime.Now - ultimoAcceso;

                if (inactividad.TotalMinutes >= 5)
                {
                    session.Clear();
                    session.Abandon();
                    filterContext.Result = new RedirectResult("~/Acceso/Login?mensaje=SesionExpirada");
                    return;
                }
            }
            session["UltimoAcceso"] = DateTime.Now;

            // 3. Control de Sesión Única Activa (Un solo dispositivo / sesión concurrente)
            if (session["SesionToken"] != null)
            {
                string tokenSesion = (string)session["SesionToken"];
                string tokenActivo;
                if (AccesoController.SesionesActivas.TryGetValue(uSesion.IdUsuario, out tokenActivo))
                {
                    if (tokenActivo != tokenSesion)
                    {
                        session.Clear();
                        session.Abandon();
                        filterContext.Result = new RedirectResult("~/Acceso/Login?mensaje=SesionDuplicada");
                        return;
                    }
                }
            }

            // 3. Verificar estado actual del usuario en la Base de Datos para invalidación inmediata de permisos/roles
            int dbIdEstado = uSesion.IdEstado;
            int dbIdRol = uSesion.IdRolSeguridad;
            int? dbIdEquipo = uSesion.IdEquipo;
            int? dbIdPosicion = uSesion.IdPosicion;

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        SELECT u.IdEstado, u.IdRolSeguridad, a.IdEquipo, a.IdPosicion 
                        FROM dbo.Usuarios u 
                        LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1 
                        WHERE u.IdUsuario = @IdUsuario;";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@IdUsuario", uSesion.IdUsuario);
                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            dbIdEstado = Convert.ToInt32(dr["IdEstado"]);
                            dbIdRol = Convert.ToInt32(dr["IdRolSeguridad"]);
                            dbIdEquipo = dr["IdEquipo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdEquipo"]) : null;
                            dbIdPosicion = dr["IdPosicion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["IdPosicion"]) : null;
                        }
                    }
                }
            }
            catch
            {
                // Si ocurre algún fallo temporal de BD, mantiene los de sesión
            }

            // Si un administrador cambió el rol, estado, equipo o posición -> cerrar sesión forzosamente
            if (dbIdEstado != uSesion.IdEstado || dbIdRol != uSesion.IdRolSeguridad || dbIdEquipo != uSesion.IdEquipo || dbIdPosicion != uSesion.IdPosicion)
            {
                session.Clear();
                session.Abandon();
                filterContext.Result = new RedirectResult("~/Acceso/Login?mensaje=PermisosModificados");
                return;
            }

            // 4. Restricción de acceso para usuarios nuevos o no activos (IdEstado != 4)
            if (uSesion.IdEstado != 4) // No activo
            {
                string controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                string actionName = filterContext.ActionDescriptor.ActionName;

                bool esCoordinadorPermitido = controllerName.Equals("Coordinador", StringComparison.OrdinalIgnoreCase) &&
                    (actionName.Equals("RegistroPerfil", StringComparison.OrdinalIgnoreCase) ||
                     actionName.Equals("EstadoSolicitud", StringComparison.OrdinalIgnoreCase) ||
                     actionName.Equals("ObtenerPosicionesOcupadas", StringComparison.OrdinalIgnoreCase));

                bool esAccesoPermitido = controllerName.Equals("Acceso", StringComparison.OrdinalIgnoreCase) &&
                    actionName.Equals("CerrarSesion", StringComparison.OrdinalIgnoreCase);

                if (!esCoordinadorPermitido && !esAccesoPermitido)
                {
                    if (uSesion.IdEstado == 2) // CorreoAprobado -> ir a llenar perfil
                    {
                        filterContext.Result = new RedirectResult("~/Coordinador/RegistroPerfil");
                    }
                    else // Estados 1, 3, 5, 6 -> ir a pantalla de estado
                    {
                        filterContext.Result = new RedirectResult("~/Coordinador/EstadoSolicitud");
                    }
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}