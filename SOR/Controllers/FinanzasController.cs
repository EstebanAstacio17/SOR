using System;
using System.Linq;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;
using SOR.Repositories;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class FinanzasController : Controller
    {
        private readonly FinanzasRepository _repo = new FinanzasRepository();

        private Usuario ObtenerUsuarioActual()
        {
            return Session["usuario"] as Usuario;
        }

        [HttpGet]
        public ActionResult Index(int? idTemporada, int? idEquipo, string mes = "OCT")
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorInicializacion = ex.Message;
            }

            Usuario u = ObtenerUsuarioActual();
            if (u == null)
                return RedirectToAction("Login", "Acceso");

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            int? idEquipoRestringido = esAdmin ? (int?)null : u.IdEquipo;

            var equipos = _repo.ObtenerListaEquipos(idEquipoRestringido);
            var temporadas = _repo.ObtenerListaTemporadas();

            int tempId = idTemporada.HasValue && idTemporada.Value > 0 
                ? idTemporada.Value 
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int eqId;
            if (!esAdmin)
            {
                eqId = u.IdEquipo ?? (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);
            }
            else
            {
                int defaultEqId = (u.IdEquipo.HasValue && u.IdEquipo.Value > 0)
                    ? u.IdEquipo.Value 
                    : (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);

                eqId = (idEquipo.HasValue && idEquipo.Value > 0) ? idEquipo.Value : defaultEqId;
            }

            string mesNormalizado = string.IsNullOrWhiteSpace(mes) ? "OCT" : mes.Trim().ToUpper();
            decimal saldoAnterior = _repo.CalcularSaldoInicialMes(tempId, eqId, mesNormalizado);

            ViewBag.EsAdmin = esAdmin;
            ViewBag.UsuarioActual = u;

            var vm = new LibroMensualViewModel
            {
                IdTemporada = tempId,
                NombreTemporada = _repo.ObtenerNombreTemporada(tempId),
                IdEquipo = eqId,
                NombreEquipo = _repo.ObtenerNombreEquipo(eqId),
                Mes = mesNormalizado,
                TasaCambio = 58.63m,
                SaldoMesAnteriorDOP = saldoAnterior,
                ListaEquipos = equipos,
                ListaTemporadas = temporadas,
                Transacciones = _repo.ObtenerTransaccionesMes(tempId, eqId, mesNormalizado, saldoAnterior),
                Categorias = _repo.ObtenerCategorias(),
                ResumenPresupuestario = _repo.ObtenerPresupuestoVsReal(tempId, eqId)
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult ReporteConsolidado(int? idTemporada, int? idEquipo)
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch { }

            Usuario u = ObtenerUsuarioActual();
            if (u == null)
                return RedirectToAction("Login", "Acceso");

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            int? idEquipoRestringido = esAdmin ? (int?)null : u.IdEquipo;

            var equipos = _repo.ObtenerListaEquipos(idEquipoRestringido);
            var temporadas = _repo.ObtenerListaTemporadas();

            int tempId = idTemporada.HasValue && idTemporada.Value > 0 
                ? idTemporada.Value 
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int eqId;
            if (!esAdmin)
            {
                eqId = u.IdEquipo ?? (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);
            }
            else
            {
                int defaultEqId = (u.IdEquipo.HasValue && u.IdEquipo.Value > 0)
                    ? u.IdEquipo.Value 
                    : (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);

                eqId = (idEquipo.HasValue && idEquipo.Value > 0) ? idEquipo.Value : defaultEqId;
            }

            ViewBag.EsAdmin = esAdmin;
            ViewBag.UsuarioActual = u;

            var filas = _repo.ObtenerReporteConsolidado(tempId, eqId);
            var vm = new ReporteConsolidadoViewModel
            {
                IdTemporada = tempId,
                NombreTemporada = _repo.ObtenerNombreTemporada(tempId),
                IdEquipo = eqId,
                NombreEquipo = _repo.ObtenerNombreEquipo(eqId),
                TasaCambio = 58.63m,
                ListaEquipos = equipos,
                ListaTemporadas = temporadas,
                Filas = filas
            };

            return View(vm);
        }

        [HttpPost]
        public JsonResult GuardarTransaccion(TransaccionFinancieraDTO model)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null)
                    return Json(new { success = false, message = "Sesión expirada." });

                bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
                if (!esAdmin)
                {
                    model.IdEquipo = u.IdEquipo ?? 1;
                }

                if (model == null || string.IsNullOrWhiteSpace(model.Descripcion) || string.IsNullOrWhiteSpace(model.CategoriaId))
                    return Json(new { success = false, message = "Datos incompletos. La descripción y categoría son requeridas." });

                if (model.TasaCambio <= 0)
                    model.TasaCambio = 58.63m;

                long nuevoId = _repo.GuardarTransaccion(model);
                return Json(new { success = true, message = "Movimiento registrado y saldos recalculados.", transaccionId = nuevoId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error en el servidor: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTransaccion(long transaccionId)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null)
                    return Json(new { success = false, message = "Sesión expirada." });

                if (transaccionId <= 0)
                    return Json(new { success = false, message = "Identificador de transacción inválido." });

                bool ok = _repo.EliminarTransaccion(transaccionId);
                return Json(new { success = ok, message = ok ? "Transacción eliminada con éxito." : "Registro no encontrado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarPresupuesto(GuardarPresupuestoRequest request)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null)
                    return Json(new { success = false, message = "Sesión expirada." });

                bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
                if (!esAdmin)
                {
                    request.IdEquipo = u.IdEquipo ?? 1;
                }

                if (request == null || request.IdTemporada <= 0 || request.IdEquipo <= 0 || request.Items == null)
                    return Json(new { success = false, message = "Parámetros de presupuesto inválidos." });

                bool ok = _repo.GuardarPresupuestoAprobado(request.IdTemporada, request.IdEquipo, request.Items);
                return Json(new { success = ok, message = ok ? "Presupuesto aprobado guardado correctamente." : "Error al guardar presupuesto." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ExportarConsolidadoExcel(int? idTemporada, int? idEquipo)
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch { }

            Usuario u = ObtenerUsuarioActual();
            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            int? idEquipoRestringido = esAdmin ? (int?)null : u?.IdEquipo;

            var equipos = _repo.ObtenerListaEquipos(idEquipoRestringido);
            var temporadas = _repo.ObtenerListaTemporadas();

            int tempId = idTemporada.HasValue && idTemporada.Value > 0 
                ? idTemporada.Value 
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int eqId;
            if (!esAdmin && u != null)
            {
                eqId = u.IdEquipo ?? (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);
            }
            else
            {
                int defaultEqId = (u != null && u.IdEquipo.HasValue)
                    ? u.IdEquipo.Value 
                    : (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);

                eqId = (idEquipo.HasValue && idEquipo.Value > 0) ? idEquipo.Value : defaultEqId;
            }

            string nombreEquipo = _repo.ObtenerNombreEquipo(eqId);
            string nombreTemporada = _repo.ObtenerNombreTemporada(tempId);
            var filas = _repo.ObtenerReporteConsolidado(tempId, eqId);

            var sb = new System.Text.StringBuilder();
            sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
            sb.Append("<head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><style>");
            sb.Append("body { font-family: Calibri, Arial, sans-serif; }");
            sb.Append("table { border-collapse: collapse; width: 100%; }");
            sb.Append("th, td { border: 1px solid #d1d5db; padding: 6px 10px; font-size: 11pt; }");
            sb.Append("th { background-color: #1e293b; color: #ffffff; font-weight: bold; text-align: center; }");
            sb.Append(".grupo-header { background-color: #e2e8f0; font-weight: bold; font-size: 11pt; color: #0f172a; text-transform: uppercase; }");
            sb.Append(".num { text-align: right; mso-number-format: '\\#\\,\\#\\#0\\.00'; }");
            sb.Append(".total-ing { background-color: #dcfce7; font-weight: bold; color: #166534; }");
            sb.Append(".total-gas { background-color: #fee2e2; font-weight: bold; color: #991b1b; }");
            sb.Append(".balance-neto { background-color: #dbeafe; font-weight: bold; font-size: 12pt; color: #1e40af; }");
            sb.Append("</style></head><body>");

            sb.Append($"<h2>REPORTE CONSOLIDADO ANUAL DE FINANZAS</h2>");
            sb.Append($"<p><strong>Equipo:</strong> {System.Web.HttpUtility.HtmlEncode(nombreEquipo)} &nbsp;&nbsp;|&nbsp;&nbsp; <strong>Temporada:</strong> {System.Web.HttpUtility.HtmlEncode(nombreTemporada)} &nbsp;&nbsp;|&nbsp;&nbsp; <strong>Tasa Oficial:</strong> RD$ 58.63 DOP/USD</p>");

            sb.Append("<table>");
            sb.Append("<thead><tr>");
            sb.Append("<th>Cat.</th><th>Descripción</th><th>SEP</th><th>OCT</th><th>NOV</th><th>DIC</th><th>ENE</th><th>FEB</th><th>MAR</th><th>ABR</th><th>MAY</th><th>JUN</th><th>JUL</th><th>AGO</th><th>Total DOP</th><th>Total USD</th>");
            sb.Append("</tr></thead><tbody>");

            var grupos = filas.GroupBy(f => f.Grupo).ToList();
            foreach (var g in grupos)
            {
                sb.Append($"<tr class='grupo-header'><td colspan='16'>{System.Web.HttpUtility.HtmlEncode(g.Key)}</td></tr>");
                foreach (var f in g)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td align='center'><strong>{f.CategoriaId}</strong></td>");
                    sb.Append($"<td>{System.Web.HttpUtility.HtmlEncode(f.Descripcion)}</td>");
                    sb.Append($"<td class='num'>{f.SEP:N2}</td>");
                    sb.Append($"<td class='num'>{f.OCT:N2}</td>");
                    sb.Append($"<td class='num'>{f.NOV:N2}</td>");
                    sb.Append($"<td class='num'>{f.DIC:N2}</td>");
                    sb.Append($"<td class='num'>{f.ENE:N2}</td>");
                    sb.Append($"<td class='num'>{f.FEB:N2}</td>");
                    sb.Append($"<td class='num'>{f.MAR:N2}</td>");
                    sb.Append($"<td class='num'>{f.ABR:N2}</td>");
                    sb.Append($"<td class='num'>{f.MAY:N2}</td>");
                    sb.Append($"<td class='num'>{f.JUN:N2}</td>");
                    sb.Append($"<td class='num'>{f.JUL:N2}</td>");
                    sb.Append($"<td class='num'>{f.AGO:N2}</td>");
                    sb.Append($"<td class='num' style='font-weight:bold;'>{f.TotalDOP:N2}</td>");
                    sb.Append($"<td class='num' style='font-weight:bold;'>{f.TotalUSD:N2}</td>");
                    sb.Append("</tr>");
                }
            }

            var ingresos = filas.Where(f => f.Tipo == "INGRESO").ToList();
            var gastos = filas.Where(f => f.Tipo == "GASTO").ToList();
            decimal totIngDOP = ingresos.Sum(i => i.TotalDOP);
            decimal totGasDOP = gastos.Sum(g => g.TotalDOP);
            decimal saldoNetoDOP = totIngDOP - totGasDOP;
            decimal saldoNetoUSD = Math.Round(saldoNetoDOP / 58.63m, 2);

            sb.Append("<tr class='total-ing'>");
            sb.Append("<td colspan='2'><strong>TOTAL INGRESOS (DOP)</strong></td>");
            sb.Append($"<td class='num'>{ingresos.Sum(i => i.SEP):N2}</td><td class='num'>{ingresos.Sum(i => i.OCT):N2}</td><td class='num'>{ingresos.Sum(i => i.NOV):N2}</td><td class='num'>{ingresos.Sum(i => i.DIC):N2}</td><td class='num'>{ingresos.Sum(i => i.ENE):N2}</td><td class='num'>{ingresos.Sum(i => i.FEB):N2}</td><td class='num'>{ingresos.Sum(i => i.MAR):N2}</td><td class='num'>{ingresos.Sum(i => i.ABR):N2}</td><td class='num'>{ingresos.Sum(i => i.MAY):N2}</td><td class='num'>{ingresos.Sum(i => i.JUN):N2}</td><td class='num'>{ingresos.Sum(i => i.JUL):N2}</td><td class='num'>{ingresos.Sum(i => i.AGO):N2}</td>");
            sb.Append($"<td class='num'><strong>{totIngDOP:N2}</strong></td><td class='num'><strong>{(totIngDOP / 58.63m):N2}</strong></td>");
            sb.Append("</tr>");

            sb.Append("<tr class='total-gas'>");
            sb.Append("<td colspan='2'><strong>TOTAL GASTOS (DOP)</strong></td>");
            sb.Append($"<td class='num'>{gastos.Sum(g => g.SEP):N2}</td><td class='num'>{gastos.Sum(g => g.OCT):N2}</td><td class='num'>{gastos.Sum(g => g.NOV):N2}</td><td class='num'>{gastos.Sum(g => g.DIC):N2}</td><td class='num'>{gastos.Sum(g => g.ENE):N2}</td><td class='num'>{gastos.Sum(g => g.FEB):N2}</td><td class='num'>{gastos.Sum(g => g.MAR):N2}</td><td class='num'>{gastos.Sum(g => g.ABR):N2}</td><td class='num'>{gastos.Sum(g => g.MAY):N2}</td><td class='num'>{gastos.Sum(g => g.JUN):N2}</td><td class='num'>{gastos.Sum(g => g.JUL):N2}</td><td class='num'>{gastos.Sum(g => g.AGO):N2}</td>");
            sb.Append($"<td class='num'><strong>{totGasDOP:N2}</strong></td><td class='num'><strong>{(totGasDOP / 58.63m):N2}</strong></td>");
            sb.Append("</tr>");

            sb.Append("<tr class='balance-neto'>");
            sb.Append("<td colspan='2'><strong>BALANCE NETO DISPONIBLE</strong></td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.SEP) - gastos.Sum(g => g.SEP)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.OCT) - gastos.Sum(g => g.OCT)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.NOV) - gastos.Sum(g => g.NOV)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.DIC) - gastos.Sum(g => g.DIC)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.ENE) - gastos.Sum(g => g.ENE)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.FEB) - gastos.Sum(g => g.FEB)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.MAR) - gastos.Sum(g => g.MAR)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.ABR) - gastos.Sum(g => g.ABR)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.MAY) - gastos.Sum(g => g.MAY)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.JUN) - gastos.Sum(g => g.JUN)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.JUL) - gastos.Sum(g => g.JUL)):N2}</td>");
            sb.Append($"<td class='num'>{(ingresos.Sum(i => i.AGO) - gastos.Sum(g => g.AGO)):N2}</td>");
            sb.Append($"<td class='num'><strong>{saldoNetoDOP:N2}</strong></td><td class='num'><strong>{saldoNetoUSD:N2}</strong></td>");
            sb.Append("</tr>");

            sb.Append("</tbody></table></body></html>");

            string fileName = $"Reporte_Consolidado_Finanzas_{nombreEquipo.Replace(" ", "_")}.xls";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileBytes, "application/vnd.ms-excel", fileName);
        }
    }
}
