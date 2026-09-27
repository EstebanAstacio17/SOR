using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SOR.Models;

namespace SOR.Permisos
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ValidarRolAttribute : ActionFilterAttribute
    {
        private readonly int[] _rolesPermitidos;

        public ValidarRolAttribute(params int[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;

            if (session["usuario"] != null)
            {
                Usuario uSesion = (Usuario)session["usuario"];

                // Validar si el rol del usuario está dentro de los permitidos
                if (_rolesPermitidos.Length > 0 && !_rolesPermitidos.Contains(uSesion.IdRolSeguridad))
                {
                    // Si la solicitud es AJAX, retornar JSON de error
                    if (filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = new { success = false, message = "No tiene permiso para realizar esta acción." },
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }
                    else
                    {
                        // Redirigir con TempData (o a una página de No Autorizado)
                        filterContext.Controller.TempData["MensajeError"] = "Acceso denegado: No tiene los permisos necesarios para acceder a este recurso.";
                        filterContext.Result = new RedirectResult("~/Home/Index");
                    }
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
