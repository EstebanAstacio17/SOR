using System;
using System.Linq;
using System.Web.Mvc;
using SOR.DAL.ERLE;
using SOR.Models.ERLE;
using SOR.Permisos;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class ErleFinanzasController : Controller
    {
        private readonly ErleFinanzasRepository _repo = new ErleFinanzasRepository();

        [HttpGet]
        public ActionResult Index(int? temporadaId, int? equipoId, string mes = "OCT")
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorInicializacion = ex.Message;
            }

            var equipos = _repo.ObtenerListaEquipos();
            var temporadas = _repo.ObtenerListaTemporadas();

            int tempId = temporadaId.HasValue && temporadaId.Value > 0 
                ? temporadaId.Value 
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int eqId = equipoId.HasValue && equipoId.Value > 0 
                ? equipoId.Value 
                : (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);

            string mesNormalizado = string.IsNullOrWhiteSpace(mes) ? "OCT" : mes.Trim().ToUpper();
            decimal saldoAnterior = _repo.CalcularSaldoInicialMes(tempId, eqId, mesNormalizado);

            var vm = new ErleLibroMensualViewModel
            {
                TemporadaId = tempId,
                NombreTemporada = _repo.ObtenerNombreTemporada(tempId),
                EquipoId = eqId,
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
        public ActionResult ReporteConsolidado(int? temporadaId, int? equipoId)
        {
            try
            {
                _repo.AsegurarEsquema();
            }
            catch { }

            var equipos = _repo.ObtenerListaEquipos();
            var temporadas = _repo.ObtenerListaTemporadas();

            int tempId = temporadaId.HasValue && temporadaId.Value > 0 
                ? temporadaId.Value 
                : (temporadas.Any() ? Convert.ToInt32(temporadas.First().Value) : 1);

            int eqId = equipoId.HasValue && equipoId.Value > 0 
                ? equipoId.Value 
                : (equipos.Any() ? Convert.ToInt32(equipos.First().Value) : 1);

            var filas = _repo.ObtenerReporteConsolidado(tempId, eqId);
            var vm = new ErleReporteConsolidadoViewModel
            {
                TemporadaId = tempId,
                NombreTemporada = _repo.ObtenerNombreTemporada(tempId),
                EquipoId = eqId,
                NombreEquipo = _repo.ObtenerNombreEquipo(eqId),
                TasaCambio = 58.63m,
                ListaEquipos = equipos,
                ListaTemporadas = temporadas,
                Filas = filas
            };

            return View(vm);
        }

        [HttpPost]
        public JsonResult GuardarTransaccion(ErleTransaccionDTO model)
        {
            try
            {
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
        public JsonResult GuardarPresupuesto(ErleGuardarPresupuestoRequest request)
        {
            try
            {
                if (request == null || request.TemporadaId <= 0 || request.EquipoId <= 0 || request.Items == null)
                    return Json(new { success = false, message = "Parámetros de presupuesto inválidos." });

                bool ok = _repo.GuardarPresupuestoAprobado(request.TemporadaId, request.EquipoId, request.Items);
                return Json(new { success = ok, message = ok ? "Presupuesto aprobado guardado correctamente." : "Error al guardar presupuesto." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
