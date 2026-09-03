using System;
using System.Collections.Generic;

namespace SOR.Models
{
    // =========================================================================
    // MATERIALES Y PRESENTACIONES
    // =========================================================================

    public class Material
    {
        public int IdMaterial { get; set; }
        public string Codigo { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public string MomentoEntrega { get; set; }
        public bool Activo { get; set; }
        public List<PresentacionMaterial> Presentaciones { get; set; } = new List<PresentacionMaterial>();
    }

    public class PresentacionMaterial
    {
        public int IdPresentacion { get; set; }
        public int IdMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string TipoEmpaque { get; set; }
        public int UnidadesPorEmpaque { get; set; }
        public int? IdTemporadaVigencia { get; set; }
        public string NombreTemporada { get; set; }
        public DateTime FechaVigenciaInicio { get; set; }
        public bool Activo { get; set; }
        public int TotalMovimientosRegistrados { get; set; }
    }

    // =========================================================================
    // ALMACENES
    // =========================================================================

    public class Almacen
    {
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; }
        public string Direccion { get; set; }
        public string Responsable { get; set; }
        public int? IdUsuarioResponsable { get; set; }
        public string Telefono { get; set; }
        public bool EsCentral { get; set; } = true;
        public bool Activo { get; set; } = true;
        public List<int> IdsEquipos { get; set; } = new List<int>();
        public List<string> NombresEquipos { get; set; } = new List<string>();
    }

    // =========================================================================
    // RECEPCIÓN DE CONTENEDORES
    // =========================================================================

    public class RecepcionContenedor
    {
        public int IdRecepcion { get; set; }
        public string NumeroContenedor { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; }
        public int? IdEquipoReceptor { get; set; }
        public string NombreEquipoReceptor { get; set; }
        public List<string> NombresEquiposAlmacen { get; set; } = new List<string>();
        public DateTime FechaRecepcion { get; set; }
        public string HoraRecepcion { get; set; }
        public string ResponsableRecepcion { get; set; }
        public string Observaciones { get; set; }
        public string EstadoRecepcion { get; set; }
        public int IdUsuarioRegistro { get; set; }
        public DateTime FechaRegistro { get; set; }
        public List<RecepcionContenedorDetalle> Detalles { get; set; } = new List<RecepcionContenedorDetalle>();
        public List<EvidenciaRecepcion> Evidencias { get; set; } = new List<EvidenciaRecepcion>();
    }

    public class EvidenciaRecepcion
    {
        public int IdEvidencia { get; set; }
        public int IdRecepcion { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public string TipoContenido { get; set; }
        public long? TamanoBytes { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class RecepcionContenedorDetalle
    {
        public int IdRecepcionDetalle { get; set; }
        public int IdRecepcion { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public int IdPresentacion { get; set; }
        public string TipoEmpaque { get; set; }
        public int CantidadEmpaques { get; set; }
        public int UnidadesPorEmpaque { get; set; }
        public int CantidadTotalUnidades { get; set; }
    }

    // =========================================================================
    // INVENTARIO CENTRAL
    // =========================================================================

    public class ItemInventarioCentral
    {
        public int IdInventarioCentral { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public int CantidadFisica { get; set; }
        public int CantidadTransferida { get; set; }
        public int CantidadDisponible { get; set; }
    }

    public class ItemInventarioMaterial
    {
        public int IdMaterial { get; set; }
        public string Codigo { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public string TipoEmpaque { get; set; }
        public int UnidadesPorEmpaque { get; set; }
        public int CantidadRecibida { get; set; }
        public int CantidadDespachada { get; set; }
        public int CantidadDisponible { get; set; }
    }

    public class EquipoInventarioResumen
    {
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string NombreNivel { get; set; }
        public List<ItemInventarioMaterial> Materiales { get; set; } = new List<ItemInventarioMaterial>();
    }

    public class InventarioCentralViewModel
    {
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public List<ItemInventarioMaterial> MaterialesGlobal { get; set; } = new List<ItemInventarioMaterial>();
        public List<EquipoInventarioResumen> Equipos { get; set; } = new List<EquipoInventarioResumen>();
    }

    // =========================================================================
    // MOVIMIENTOS DE INVENTARIO (KARDEX)
    // =========================================================================

    public class MovimientoInventario
    {
        public int IdMovimiento { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public string TipoMovimiento { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public int Cantidad { get; set; }
        public int? IdAlmacenOrigen { get; set; }
        public string NombreAlmacenOrigen { get; set; }
        public int? IdAlmacenDestino { get; set; }
        public string NombreAlmacenDestino { get; set; }
        public int? IdEquipoDestino { get; set; }
        public string NombreEquipoDestino { get; set; }
        public int? IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public string IdDocumentoReferencia { get; set; }
        public DateTime FechaHora { get; set; }
        public int IdUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string Justificacion { get; set; }
    }

    // =========================================================================
    // TRANSFERENCIAS A EQUIPOS
    // =========================================================================

    public class TransferenciaEquipo
    {
        public int IdTransferencia { get; set; }
        public string NumeroConstancia { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int? IdEquipoEmisor { get; set; }
        public string NombreEquipoEmisor { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int IdAlmacenOrigen { get; set; }
        public string NombreAlmacenOrigen { get; set; }
        public DateTime FechaTransferencia { get; set; }
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public int? IdUsuarioEmisor { get; set; }
        public string CoordinadorEmisor { get; set; }
        public int? IdUsuarioReceptor { get; set; }
        public string PersonaReceptoraEquipo { get; set; }
        public string Observaciones { get; set; }
        public string Estado { get; set; }
        public int IdUsuarioRegistro { get; set; }
        public List<TransferenciaEquipoDetalle> Detalles { get; set; } = new List<TransferenciaEquipoDetalle>();
    }

    public class TransferenciaEquipoDetalle
    {
        public int IdTransferenciaDetalle { get; set; }
        public int IdTransferencia { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public int CantidadUnidades { get; set; }
    }

    // =========================================================================
    // INVENTARIO POR EQUIPO
    // =========================================================================

    public class ItemInventarioEquipo
    {
        public int IdInventarioEquipo { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public string TipoEmpaque { get; set; }
        public int UnidadesPorEmpaque { get; set; }
        public int CantidadRecibida { get; set; }
        public int CantidadAsignada { get; set; }
        public int CantidadDespachada { get; set; }
        public int CantidadDisponible { get; set; }
    }

    public class ResumenInventarioEquipo
    {
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string NombreNivel { get; set; }
        public int? IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; }

        // Coordinador Responsable
        public int? IdUsuarioCoordinador { get; set; }
        public string NombreCoordinador { get; set; }
        public string TelefonoCoordinador { get; set; }
        public string PosicionCoordinador { get; set; }
        public string CorreoCoordinador { get; set; }

        // Totales de inventario
        public int TotalRecibido { get; set; }
        public int TotalAsignado { get; set; }
        public int TotalDespachado { get; set; }
        public int TotalDisponible { get; set; }

        // Desglose de materiales del equipo
        public List<ItemInventarioEquipo> Materiales { get; set; } = new List<ItemInventarioEquipo>();
    }

    // =========================================================================
    // EVENTOS DE DESPACHO
    // =========================================================================

    public class EventoDespacho
    {
        public int IdEventoDespacho { get; set; }
        public int IdEvento { get; set; }
        public string NombreEvento { get; set; }
        public DateTime? FechaEvento { get; set; }
        public string Lugar { get; set; }
        public string Hora { get; set; }
        public int? IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string EstadoDespachoEvento { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int TotalIglesiasAsignadas { get; set; }
        public int TotalIglesiasDespachadas { get; set; }
        public int TotalIglesiasPendientes { get; set; }
        public double PorcentajeDespachado => TotalIglesiasAsignadas > 0 ? ((double)TotalIglesiasDespachadas / TotalIglesiasAsignadas) * 100.0 : 0.0;
        public List<CoordinadorEvento> Coordinadores { get; set; } = new List<CoordinadorEvento>();
        public List<DespachoIglesiaItem> Iglesias { get; set; } = new List<DespachoIglesiaItem>();
    }

    public class CoordinadorEvento
    {
        public int IdCoordinadorEvento { get; set; }
        public int IdEvento { get; set; }
        public int IdUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string NombreCoordinador { get; set; }
        public string HoraEntrada { get; set; }
        public string HoraSalida { get; set; }
        public bool Presente { get; set; }
    }

    // =========================================================================
    // DESPACHOS POR IGLESIA
    // =========================================================================

    public class DespachoIglesiaItem
    {
        public int IdDespachoIglesia { get; set; }
        public string NumeroComprobanteDespacho { get; set; }
        public int IdEvento { get; set; }
        public string NombreEvento { get; set; }
        public int IdParticipacion { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public string DireccionIglesia { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string EstadoDespacho { get; set; }
        public string TipoReceptor { get; set; }
        public string NombreReceptor { get; set; }
        public string DocumentoIdentidadReceptor { get; set; }
        public string TelefonoReceptor { get; set; }
        public DateTime? FechaHoraEntrega { get; set; }
        public string CoordinadorDespachador { get; set; }
        public string MotivoNoDespacho { get; set; }
        public string Observaciones { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Datos del Pastor / Líder de la iglesia (precargados para validación)
        public string NombrePastor { get; set; }
        public string CedulaPastor { get; set; }
        public string TelefonoPastor { get; set; }
        public string NombreLiderMinisterial { get; set; }
        public string CedulaLiderMinisterial { get; set; }
        public string TelefonoLiderMinisterial { get; set; }

        public List<DespachoDetalleMaterial> Materiales { get; set; } = new List<DespachoDetalleMaterial>();
    }

    public class DespachoDetalleMaterial
    {
        public int IdDespachoDetalle { get; set; }
        public int IdDespachoIglesia { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public string UnidadEntrega { get; set; }
        public int CantidadAsignada { get; set; }
        public int CantidadDespachada { get; set; }
        public int CantidadNoDespachada { get; set; }
    }

    // =========================================================================
    // VIEW MODELS / DTOs
    // =========================================================================

    /// <summary>ViewModel para el formulario de recepción de contenedor</summary>
    public class RecepcionFormViewModel
    {
        public int IdAlmacen { get; set; }
        public string NumeroContenedor { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public string ResponsableRecepcion { get; set; }
        public string Observaciones { get; set; }
        // JSON serializado de líneas de detalle
        public string DetallesJson { get; set; }
    }

    /// <summary>ViewModel para el formulario de transferencia a equipo</summary>
    public class TransferenciaFormViewModel
    {
        public int IdEquipo { get; set; }
        public int IdAlmacenOrigen { get; set; }
        public DateTime FechaTransferencia { get; set; }
        public string CoordinadorEmisor { get; set; }
        public string PersonaReceptoraEquipo { get; set; }
        public string Observaciones { get; set; }
        // JSON serializado de líneas de detalle
        public string DetallesJson { get; set; }
    }

    /// <summary>ViewModel para confirmar despacho de una iglesia (con cédula)</summary>
    public class ConfirmarDespachoViewModel
    {
        public int IdDespachoIglesia { get; set; }
        public string TipoReceptor { get; set; }          // PASTOR | LIDER_MINISTERIAL | AMBOS
        public string NombreReceptor { get; set; }
        public string DocumentoCedulaReceptor { get; set; }
        public string DocumentoIdentidadReceptor { get; set; }
        public string TelefonoReceptor { get; set; }
        public string CoordinadorDespachador { get; set; }
        public string Observaciones { get; set; }
        // JSON serializado de cantidades despachadas por IdMaterial
        public string CantidadesJson { get; set; }
        public string MaterialesJson { get; set; }
    }

    /// <summary>ViewModel para marcar iglesia NO despachada</summary>
    public class NoDespachoBecauseViewModel
    {
        public int IdDespachoIglesia { get; set; }
        public string MotivoNoDespacho { get; set; }
        public string CoordinadorDespachador { get; set; }
    }

    /// <summary>Dashboard de inventario resumido</summary>
    public class DashboardLogistico
    {
        public string NombreTemporada { get; set; }
        public InventarioCentralViewModel ResumenCentral { get; set; } = new InventarioCentralViewModel();
        public List<ItemInventarioCentral> InventarioCentral { get; set; } = new List<ItemInventarioCentral>();
        public List<ItemInventarioEquipo> InventarioEquipo { get; set; } = new List<ItemInventarioEquipo>();
    }
}
