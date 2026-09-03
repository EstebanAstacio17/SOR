using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOR.Models.ERLE
{
    public class ErleTransaccionDTO
    {
        public long TransaccionId { get; set; }
        public int TemporadaId { get; set; }
        public int EquipoId { get; set; }
        public string Mes { get; set; }

        [Required(ErrorMessage = "La fecha es requerida")]
        public DateTime Fecha { get; set; }
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        public string CategoriaId { get; set; }
        public string CategoriaDescripcion { get; set; }

        public decimal GastoDOP { get; set; }
        public decimal IngresoDOP { get; set; }
        public decimal TasaCambio { get; set; }
        public decimal GastoUSD { get; set; }
        public decimal IngresoUSD { get; set; }
        public decimal SaldoDOP { get; set; }
        public decimal SaldoUSD { get; set; }
        public string Notas { get; set; }
    }

    public class ErlePresupuestoVsRealDTO
    {
        public string Grupo { get; set; }
        public string CategoriaId { get; set; }
        public string Descripcion { get; set; }
        public decimal PresupuestoAprobadoUSD { get; set; }
        public decimal PresupuestoAprobadoDOP { get; set; }
        public decimal EjecutadoUSD { get; set; }
        public decimal EjecutadoDOP { get; set; }
        public decimal RemanenteUSD { get; set; }
        public decimal RemanenteDOP { get; set; }
        public decimal PorcentajeEjecucion => PresupuestoAprobadoUSD > 0 
            ? Math.Round((EjecutadoUSD / PresupuestoAprobadoUSD) * 100m, 1) 
            : 0m;
    }

    public class ErleLibroMensualViewModel
    {
        public int TemporadaId { get; set; }
        public string NombreTemporada { get; set; }
        public int EquipoId { get; set; }
        public string NombreEquipo { get; set; }
        public string Mes { get; set; }
        public decimal TasaCambio { get; set; }
        public decimal SaldoMesAnteriorDOP { get; set; }
        public List<System.Web.Mvc.SelectListItem> ListaEquipos { get; set; } = new List<System.Web.Mvc.SelectListItem>();
        public List<System.Web.Mvc.SelectListItem> ListaTemporadas { get; set; } = new List<System.Web.Mvc.SelectListItem>();
        public List<ErleTransaccionDTO> Transacciones { get; set; } = new List<ErleTransaccionDTO>();
        public List<ErleOpcionCategoriaDTO> Categorias { get; set; } = new List<ErleOpcionCategoriaDTO>();
        public List<ErlePresupuestoVsRealDTO> ResumenPresupuestario { get; set; } = new List<ErlePresupuestoVsRealDTO>();
    }

    public class ErleOpcionCategoriaDTO
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public string Tipo { get; set; }
    }

    public class ErleReporteConsolidadoFila
    {
        public string Grupo { get; set; }
        public string CategoriaId { get; set; }
        public string Descripcion { get; set; }
        public string Tipo { get; set; }
        public decimal SEP { get; set; }
        public decimal OCT { get; set; }
        public decimal NOV { get; set; }
        public decimal DIC { get; set; }
        public decimal ENE { get; set; }
        public decimal FEB { get; set; }
        public decimal MAR { get; set; }
        public decimal ABR { get; set; }
        public decimal MAY { get; set; }
        public decimal JUN { get; set; }
        public decimal JUL { get; set; }
        public decimal AGO { get; set; }
        public decimal TotalDOP { get; set; }
        public decimal TotalUSD => TasaCambio > 0 ? Math.Round(TotalDOP / TasaCambio, 2) : 0m;
        public decimal TasaCambio { get; set; } = 58.63m;
    }

    public class ErleReporteConsolidadoViewModel
    {
        public int TemporadaId { get; set; }
        public string NombreTemporada { get; set; }
        public int EquipoId { get; set; }
        public string NombreEquipo { get; set; }
        public decimal TasaCambio { get; set; } = 58.63m;
        public List<System.Web.Mvc.SelectListItem> ListaEquipos { get; set; } = new List<System.Web.Mvc.SelectListItem>();
        public List<System.Web.Mvc.SelectListItem> ListaTemporadas { get; set; } = new List<System.Web.Mvc.SelectListItem>();
        public List<ErleReporteConsolidadoFila> Filas { get; set; } = new List<ErleReporteConsolidadoFila>();
    }

    public class ErlePresupuestoItemDTO
    {
        public string CategoriaId { get; set; }
        public decimal MontoAprobadoUSD { get; set; }
    }

    public class ErleGuardarPresupuestoRequest
    {
        public int TemporadaId { get; set; }
        public int EquipoId { get; set; }
        public List<ErlePresupuestoItemDTO> Items { get; set; } = new List<ErlePresupuestoItemDTO>();
    }
}
