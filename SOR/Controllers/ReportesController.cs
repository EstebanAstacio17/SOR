using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SOR.DAL;
using SOR.Models;
using SOR.Models.Reportes;
using SOR.Permisos;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class ReportesController : Controller
    {
        private readonly ReportesRepository _repo = new ReportesRepository();

        private Usuario ObtenerUsuarioActual()
        {
            return Session["usuario"] as Usuario;
        }

        [HttpGet]
        public ActionResult Index(int? temporadaId, int? equipoId, int? iglesiaId)
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
            HashSet<int> equiposPermitidos = _repo.ObtenerEquiposPermitidosJerarquico(u);

            var temporadas = _repo.ObtenerListaTemporadas();
            var equipos = _repo.ObtenerListaEquipos(equiposPermitidos);

            int tempId = temporadaId.HasValue && temporadaId.Value > 0
                ? temporadaId.Value
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int? eqId = null;
            if (esAdmin)
            {
                eqId = (equipoId.HasValue && equipoId.Value > 0) ? equipoId.Value : (int?)null;
            }
            else
            {
                if (equipoId.HasValue && equiposPermitidos != null && equiposPermitidos.Contains(equipoId.Value))
                {
                    eqId = equipoId.Value;
                }
                else
                {
                    eqId = u.IdEquipo;
                }
            }

            var iglesias = _repo.ObtenerListaIglesias(eqId, equiposPermitidos);

            int? igId = (iglesiaId.HasValue && iglesiaId.Value > 0) ? iglesiaId.Value : (int?)null;

            var mov = _repo.ObtenerReporteMovilizacion(tempId, eqId, igId);
            var disc = _repo.ObtenerReporteDiscipulado(tempId, eqId, igId);
            var orac = _repo.ObtenerReporteOracion(tempId, eqId, igId);

            var vm = new EosDashboardViewModel
            {
                IdTemporada = tempId,
                NombreTemporada = temporadas.FirstOrDefault(t => t.Value == tempId.ToString())?.Text ?? "Temporada " + tempId,
                IdEquipo = eqId,
                NombreEquipo = eqId.HasValue ? equipos.FirstOrDefault(e => e.Value == eqId.ToString())?.Text : "Nivel Nacional (Todos los Equipos)",
                IdIglesia = igId,
                NombreIglesia = igId.HasValue ? iglesias.FirstOrDefault(i => i.Value == igId.ToString())?.Text : "Todas las Iglesias",
                ListaTemporadas = temporadas,
                ListaEquipos = equipos,
                ListaIglesias = iglesias,
                Movilizacion = mov,
                Discipulado = disc,
                Oracion = orac
            };

            ViewBag.EsAdmin = esAdmin;
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeCambiarEquipo = esAdmin || (equiposPermitidos != null && equiposPermitidos.Count > 1);

            return View(vm);
        }

        [HttpGet]
        public JsonResult ObtenerIglesiasPorEquipo(int? equipoId)
        {
            Usuario u = ObtenerUsuarioActual();
            HashSet<int> equiposPermitidos = _repo.ObtenerEquiposPermitidosJerarquico(u);
            var list = _repo.ObtenerListaIglesias(equipoId, equiposPermitidos);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public PartialViewResult ObtenerMovilizacionTab(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var mov = _repo.ObtenerReporteMovilizacion(temporadaId, equipoId, iglesiaId);
            return PartialView("_MovilizacionTab", mov);
        }

        [HttpGet]
        public PartialViewResult ObtenerDiscipuladoTab(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var disc = _repo.ObtenerReporteDiscipulado(temporadaId, equipoId, iglesiaId);
            return PartialView("_DiscipuladoTab", disc);
        }

        [HttpGet]
        public PartialViewResult ObtenerOracionTab(int temporadaId, int? equipoId, int? iglesiaId)
        {
            var orac = _repo.ObtenerReporteOracion(temporadaId, equipoId, iglesiaId);
            return PartialView("_OracionTab", orac);
        }

        [HttpPost]
        public JsonResult GuardarIglesiaPlantada(IglesiaPlantadaDTO dto)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                if (string.IsNullOrWhiteSpace(dto.NombreIglesia) || string.IsNullOrWhiteSpace(dto.PastorPrincipal))
                    return Json(new { success = false, message = "Nombre de la iglesia y Pastor son obligatorios." });

                bool ok = _repo.GuardarIglesiaPlantada(dto);
                return Json(new { success = ok, message = ok ? "Iglesia plantada registrada exitosamente." : "Error al registrar." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarIglesiaPlantada(int id)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                bool ok = _repo.EliminarIglesiaPlantada(id);
                return Json(new { success = ok, message = ok ? "Registro eliminado." : "No se encontró el registro." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarGNA(GnaDTO dto)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                if (string.IsNullOrWhiteSpace(dto.NombreGNA) || string.IsNullOrWhiteSpace(dto.CompaneroMinisterio))
                    return Json(new { success = false, message = "Nombre del GNA y Compañero de Ministerio son obligatorios." });

                bool ok = _repo.GuardarGNA(dto);
                return Json(new { success = ok, message = ok ? "Grupo No Alcanzado registrado exitosamente." : "Error al registrar." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarGNA(int id)
        {
            try
            {
                Usuario u = ObtenerUsuarioActual();
                if (u == null) return Json(new { success = false, message = "Sesión expirada." });

                bool ok = _repo.EliminarGNA(id);
                return Json(new { success = ok, message = ok ? "Registro eliminado." : "No se encontró el registro." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ExportarExcel(int temporadaId, int? equipoId, int? iglesiaId)
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch { }

            var mov = _repo.ObtenerReporteMovilizacion(temporadaId, equipoId, iglesiaId);
            var disc = _repo.ObtenerReporteDiscipulado(temporadaId, equipoId, iglesiaId);
            var orac = _repo.ObtenerReporteOracion(temporadaId, equipoId, iglesiaId);

            var sb = new System.Text.StringBuilder();
            sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
            sb.Append("<head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><style>");
            sb.Append("body { font-family: Calibri, Arial, sans-serif; }");
            sb.Append("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
            sb.Append("th, td { border: 1px solid #d1d5db; padding: 6px 10px; font-size: 10pt; }");
            sb.Append("th { background-color: #1e293b; color: #ffffff; font-weight: bold; }");
            sb.Append(".section-title { background-color: #0284c7; color: #ffffff; font-weight: bold; font-size: 12pt; }");
            sb.Append(".num { text-align: right; }");
            sb.Append("</style></head><body>");

            sb.Append("<h2>REPORTES DE TEMPORADA — OCC REPÚBLICA DOMINICANA</h2>");
            sb.Append($"<p><strong>Fecha Generación:</strong> {DateTime.Now:dd/MM/yyyy hh:mm tt}</p>");

            // 1. MOVILIZACIÓN
            sb.Append("<table>");
            sb.Append("<tr><th colspan='2' class='section-title'>1. COORDINADOR DE MOVILIZACIÓN DE IGLESIAS</th></tr>");
            sb.Append($"<tr><td>Total Presentaciones de la Visión</td><td class='num'><strong>{mov.TotalPresentacionesVision}</strong></td></tr>");
            sb.Append($"<tr><td>Total Asistentes a Presentaciones de Visión</td><td class='num'><strong>{mov.TotalAsistentesVision:N0}</strong></td></tr>");
            sb.Append($"<tr><td>Equipos Ministeriales Capacitados</td><td class='num'><strong>{mov.EquiposMinisterialesCapacitados}</strong></td></tr>");
            sb.Append($"<tr><td>Cajitas Entregadas a Compañeros de Ministerio</td><td class='num'><strong>{mov.CajitasEntregadasCompaneros:N0}</strong></td></tr>");
            sb.Append($"<tr><td>Eventos Evangelísticos Realizados</td><td class='num'><strong>{mov.EventosEvangelisticos}</strong></td></tr>");
            sb.Append($"<tr><td>Niños Asistentes a Eventos Evangelísticos</td><td class='num'><strong>{mov.NinosAsistentesEvangelisticos:N0}</strong></td></tr>");
            sb.Append("</table>");

            // 2. DISCIPULADO
            sb.Append("<table>");
            sb.Append("<tr><th colspan='2' class='section-title'>2. COORDINADOR DE DISCIPULADO</th></tr>");
            sb.Append($"<tr><td>Capacitaciones OCC Impartidas</td><td class='num'><strong>{disc.TotalCapacitacionesOCC}</strong></td></tr>");
            sb.Append($"<tr><td>Total Asistentes a Capacitaciones</td><td class='num'><strong>{disc.TotalAsistentesCapacitacion:N0}</strong></td></tr>");
            sb.Append($"<tr><td>LGA: Cursos / Aulas Impartidos</td><td class='num'><strong>{disc.LgaCursosImpartidos}</strong></td></tr>");
            sb.Append($"<tr><td>LGA: Niños Asistentes</td><td class='num'><strong>{disc.LgaNinosAsistentes:N0}</strong></td></tr>");
            sb.Append($"<tr><td>LGA: Decisiones por Jesús</td><td class='num'><strong>{disc.LgaDecisionesJesus:N0}</strong></td></tr>");
            sb.Append($"<tr><td>LGA: Comprometidos a Orar y Compartir</td><td class='num'><strong>{disc.LgaComprometidosOrarCompartir:N0}</strong></td></tr>");
            sb.Append($"<tr><td>LGA: Graduados Totales</td><td class='num'><strong>{disc.LgaGraduadosTotales:N0}</strong></td></tr>");
            sb.Append($"<tr><td>VDC: Asistieron a 1 clase</td><td class='num'><strong>{disc.VdcAsistieronUnaClase:N0}</strong></td></tr>");
            sb.Append($"<tr><td>VDC: Asistieron a 6 clases</td><td class='num'><strong>{disc.VdcAsistieronSeisClases:N0}</strong></td></tr>");
            sb.Append($"<tr><td>VDC: Continuaron en LGA o DET</td><td class='num'><strong>{disc.VdcContinuaronLgaODet:N0}</strong></td></tr>");
            sb.Append("</table>");

            // 3. ORACIÓN
            sb.Append("<table>");
            sb.Append("<tr><th colspan='2' class='section-title'>3. COORDINADOR DE ORACIÓN</th></tr>");
            sb.Append($"<tr><td>Eventos de Oración Organizados</td><td class='num'><strong>{orac.EventosOracionOrganizados}</strong></td></tr>");
            sb.Append($"<tr><td>Total Asistentes a Eventos de Oración</td><td class='num'><strong>{orac.TotalAsistentesOracion:N0}</strong></td></tr>");
            sb.Append($"<tr><td>Compañeros de Oración Reportados</td><td class='num'><strong>{orac.CompanerosOracionReportados}</strong></td></tr>");
            sb.Append($"<tr><td>Miembros en la Red de Oración Local</td><td class='num'><strong>{orac.MiembrosRedOracionLocal}</strong></td></tr>");
            sb.Append("</table>");

            sb.Append("</body></html>");

            string fileName = $"Reportes_Temporada_{DateTime.Now:yyyyMMdd}.xls";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileBytes, "application/vnd.ms-excel", fileName);
        }
    }
}
