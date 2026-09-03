using System;
using System.Collections.Generic;
using SOR.Models;
using SOR.Repositories;

namespace SOR.Services
{
    public class LogisticaService
    {
        private readonly LogisticaRepository _repo;

        public LogisticaService()
        {
            _repo = new LogisticaRepository();
        }

        // =====================================================================
        // MATERIALES Y PRESENTACIONES
        // =====================================================================

        public List<Material> ObtenerMateriales(bool soloActivos = true) =>
            _repo.ObtenerMateriales(soloActivos);

        public List<PresentacionMaterial> ObtenerPresentaciones(bool soloActivas = true, int? idTemporada = null) =>
            _repo.ObtenerPresentaciones(soloActivas, idTemporada);

        public void GuardarPresentacion(PresentacionMaterial modelo) =>
            _repo.GuardarPresentacion(modelo);

        public void AlternarEstadoPresentacion(int idPresentacion, bool activo) =>
            _repo.AlternarEstadoPresentacion(idPresentacion, activo);

        // =====================================================================
        // ALMACENES
        // =====================================================================

        public List<Almacen> ObtenerAlmacenes(bool soloActivos = true) =>
            _repo.ObtenerAlmacenes(soloActivos);

        public List<Almacen> ObtenerAlmacenesPorEquipo(int? idEquipo, bool soloActivos = true) =>
            _repo.ObtenerAlmacenesPorEquipo(idEquipo, soloActivos);

        public void GuardarAlmacen(Almacen modelo) =>
            _repo.GuardarAlmacen(modelo);

        // =====================================================================
        // RECEPCIONES
        // =====================================================================

        public int RegistrarRecepcion(RecepcionContenedor modelo, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(modelo.NumeroContenedor))
                throw new ArgumentException("El número de contenedor es obligatorio.");
            if (modelo.IdAlmacen <= 0)
                throw new ArgumentException("Debe seleccionar un almacén de destino.");
            if (modelo.Detalles == null || modelo.Detalles.Count == 0)
                throw new ArgumentException("Debe agregar al menos un material al contenedor.");
            foreach (var det in modelo.Detalles)
            {
                if (det.CantidadEmpaques <= 0)
                    throw new ArgumentException($"La cantidad de empaques para el material ID {det.IdMaterial} debe ser mayor a 0.");
                if (det.UnidadesPorEmpaque <= 0)
                    throw new ArgumentException($"Las unidades por empaque para el material ID {det.IdMaterial} deben ser mayores a 0.");
            }
            return _repo.RegistrarRecepcion(modelo, idUsuario);
        }

        public List<RecepcionContenedor> ObtenerRecepciones(int? idTemporada = null, int? idAlmacen = null, int? idEquipo = null) =>
            _repo.ObtenerRecepciones(idTemporada, idAlmacen, idEquipo);

        public RecepcionContenedor ObtenerRecepcionDetalle(int id) =>
            _repo.ObtenerRecepcionDetalle(id);

        // =====================================================================
        // INVENTARIO CENTRAL
        // =====================================================================

        public List<ItemInventarioCentral> ObtenerInventarioCentral(int? idTemporada = null, int? idAlmacen = null) =>
            _repo.ObtenerInventarioCentral(idTemporada, idAlmacen);

        public InventarioCentralViewModel ObtenerResumenInventarioCentral(int? idTemporada = null) =>
            _repo.ObtenerResumenInventarioCentral(idTemporada);

        // =====================================================================
        // TRANSFERENCIAS
        // =====================================================================

        public int RegistrarTransferencia(TransferenciaEquipo modelo, int idUsuario)
        {
            if (modelo.IdEquipo <= 0)
                throw new ArgumentException("Debe seleccionar un equipo de destino.");
            if (modelo.IdEquipoEmisor.HasValue && modelo.IdEquipoEmisor.Value == modelo.IdEquipo)
                throw new ArgumentException("El equipo emisor y el equipo receptor no pueden ser el mismo.");
            if (modelo.IdAlmacenOrigen <= 0)
                throw new ArgumentException("Debe seleccionar el almacén de origen.");
            if (modelo.FechaEmision.HasValue && modelo.FechaRecepcion.HasValue && modelo.FechaRecepcion.Value < modelo.FechaEmision.Value)
                throw new ArgumentException("La fecha de recepción no puede ser anterior a la fecha de emisión.");
            if (modelo.Detalles == null || modelo.Detalles.Count == 0)
                throw new ArgumentException("Debe agregar al menos un material para transferir.");
            foreach (var det in modelo.Detalles)
            {
                if (det.CantidadUnidades <= 0)
                    throw new ArgumentException($"La cantidad de unidades para el material ID {det.IdMaterial} debe ser mayor a 0.");
            }
            return _repo.RegistrarTransferencia(modelo, idUsuario);
        }

        public void ConfirmarRecepcionTransferencia(int idTransferencia, DateTime fechaRecepcion, string personaReceptora, int? idUsuarioReceptor, int idUsuario)
        {
            if (idTransferencia <= 0)
                throw new ArgumentException("Identificador de transferencia no válido.");
            if (string.IsNullOrWhiteSpace(personaReceptora))
                throw new ArgumentException("Debe indicar la persona que recibe en el equipo receptor.");
            _repo.ConfirmarRecepcionTransferencia(idTransferencia, fechaRecepcion, personaReceptora, idUsuarioReceptor, idUsuario);
        }

        public void CancelarTransferencia(int idTransferencia, string motivo, int idUsuario)
        {
            if (idTransferencia <= 0)
                throw new ArgumentException("Identificador de transferencia no válido.");
            _repo.CancelarTransferencia(idTransferencia, motivo, idUsuario);
        }

        public List<TransferenciaEquipo> ObtenerTransferencias(int? idTemporada = null, int? idEquipo = null) =>
            _repo.ObtenerTransferencias(idTemporada, idEquipo);

        public TransferenciaEquipo ObtenerTransferenciaDetalle(int id) =>
            _repo.ObtenerTransferenciaDetalle(id);

        // =====================================================================
        // INVENTARIO EQUIPO
        // =====================================================================

        public List<ItemInventarioEquipo> ObtenerInventarioEquipo(int? idTemporada = null, int? idEquipo = null) =>
            _repo.ObtenerInventarioEquipo(idTemporada, idEquipo);

        public List<ResumenInventarioEquipo> ObtenerResumenInventarioEquipos(int? idTemporada = null, int? idEquipo = null) =>
            _repo.ObtenerResumenInventarioEquipos(idTemporada, idEquipo);

        // =====================================================================
        // EVENTOS DE DESPACHO
        // =====================================================================

        public int CrearEventoDespacho(int idEvento, int idEquipo, int? idAlmacen, int idUsuario) =>
            _repo.CrearEventoDespacho(idEvento, idEquipo, idAlmacen, idUsuario);

        public List<EventoDespacho> ObtenerEventosDespacho(int? idTemporada = null, int? idEquipo = null) =>
            _repo.ObtenerEventosDespacho(idTemporada, idEquipo);

        public EventoDespacho ObtenerDetalleEventoDespacho(int idEvento) =>
            _repo.ObtenerDetalleEventoDespacho(idEvento);

        // =====================================================================
        // PROGRAMAR IGLESIA EN DESPACHO
        // =====================================================================

        public int ProgramarIglesiaEnDespacho(int idEvento, int idParticipacion, int idIglesia, int idEquipo, int idTemporada, int idUsuario) =>
            _repo.ProgramarIglesiaEnDespacho(idEvento, idParticipacion, idIglesia, idEquipo, idTemporada, idUsuario);

        public List<DespachoIglesiaItem> ObtenerIglesiasDisponiblesDespacho(int idEquipo, int idTemporada) =>
            _repo.ObtenerIglesiasDisponiblesDespacho(idEquipo, idTemporada);

        // =====================================================================
        // DESPACHO PRESENCIAL (EXCLUSIVO COORDINADOR DE LOGÍSTICA — CL)
        // =====================================================================

        public void ConfirmarDespacho(ConfirmarDespachoViewModel vm, int idEquipo, int idTemporada, int idUsuario, string nombreCoordinador, int? idRolSeguridad = null, int? idPosicion = null)
        {
            if (string.IsNullOrWhiteSpace(vm.TipoReceptor) || 
                (vm.TipoReceptor != "PASTOR" && vm.TipoReceptor != "LIDER_MINISTERIAL" && vm.TipoReceptor != "AMBOS"))
            {
                throw new ArgumentException("Debe seleccionar el tipo de receptor (Pastor General, Líder Ministerial o Ambos Presentes).");
            }

            if (string.IsNullOrWhiteSpace(vm.DocumentoCedulaReceptor) && !string.IsNullOrWhiteSpace(vm.DocumentoIdentidadReceptor))
            {
                vm.DocumentoCedulaReceptor = vm.DocumentoIdentidadReceptor;
            }

            if (string.IsNullOrWhiteSpace(vm.CantidadesJson) && string.IsNullOrWhiteSpace(vm.MaterialesJson))
            {
                throw new ArgumentException("Debe especificar las cantidades despachadas.");
            }

            _repo.ConfirmarDespacho(vm, idEquipo, idTemporada, idUsuario, nombreCoordinador, idRolSeguridad, idPosicion);
        }

        public void MarcarNoDespacho(NoDespachoBecauseViewModel vm, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(vm.MotivoNoDespacho))
                throw new ArgumentException("El motivo de no despacho es obligatorio.");
            _repo.MarcarNoDespacho(vm, idUsuario);
        }

        public DespachoIglesiaItem ObtenerDespachoDetalle(int idDespachoIglesia) =>
            _repo.ObtenerDespachoDetalle(idDespachoIglesia);

        // =====================================================================
        // KARDEX
        // =====================================================================

        public List<MovimientoInventario> ObtenerKardex(int? idTemporada = null, int? idMaterial = null, int? idEquipo = null) =>
            _repo.ObtenerKardex(idTemporada, idMaterial, idEquipo);
    }
}
