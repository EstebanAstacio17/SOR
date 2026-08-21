using System;
using System.Collections.Generic;
using SOR.Models;
using SOR.Repositories;

namespace SOR.Services
{
    public class EquipoService
    {
        private readonly EquipoRepository _equipoRepository;

        public EquipoService()
        {
            _equipoRepository = new EquipoRepository();
        }

        public List<EquipoConDetalles> ListarEquipos()
        {
            return _equipoRepository.ListarEquipos();
        }

        public EquipoConDetalles ObtenerEquipoPorId(int idEquipo)
        {
            if (idEquipo <= 0) return null;
            return _equipoRepository.ObtenerEquipoPorId(idEquipo);
        }

        public List<NivelEquipo> ListarNiveles()
        {
            return _equipoRepository.ListarNiveles();
        }

        public void RegistrarEquipo(EquipoConDetalles equipo)
        {
            ValidarDatosEquipo(equipo);
            _equipoRepository.InsertarEquipo(equipo);
        }

        public void ActualizarEquipo(EquipoConDetalles equipo)
        {
            if (equipo.IdEquipo <= 0)
            {
                throw new ArgumentException("ID de equipo inválido para actualización.");
            }

            ValidarDatosEquipo(equipo);

            // Validar que no se asigne como su propio padre
            if (equipo.IdEquipoPadre.HasValue && equipo.IdEquipoPadre.Value == equipo.IdEquipo)
            {
                throw new InvalidOperationException("Un equipo no puede ser su propio equipo padre.");
            }

            // Validar dependencia circular (bucle recursivo)
            if (equipo.IdEquipoPadre.HasValue && DetectarCicloJerarquico(equipo.IdEquipo, equipo.IdEquipoPadre.Value))
            {
                throw new InvalidOperationException("Se ha detectado un bucle jerárquico. El equipo padre seleccionado es un sub-equipo (descendiente) de este equipo.");
            }

            _equipoRepository.ActualizarEquipo(equipo);
        }

        public string EliminarEquipo(int idEquipo)
        {
            if (idEquipo <= 0)
            {
                throw new ArgumentException("ID de equipo inválido para eliminación.");
            }

            int dependencias = _equipoRepository.ObtenerCantidadDependencias(idEquipo);
            if (dependencias > 0)
            {
                _equipoRepository.DesactivarEquipo(idEquipo);
                return "El equipo posee sub-equipos, iglesias o usuarios asignados. Ha sido inhabilitado (desactivado) para nuevos procesos de registro, preservando el historial de datos existentes.";
            }

            _equipoRepository.EliminarEquipo(idEquipo);
            return "El equipo ha sido eliminado físicamente ya que no poseía registros dependientes.";
        }

        private void ValidarDatosEquipo(EquipoConDetalles equipo)
        {
            if (string.IsNullOrWhiteSpace(equipo.NombreEquipo))
            {
                throw new ArgumentException("El nombre del equipo es obligatorio.");
            }

            if (equipo.IdNivelEquipo <= 0)
            {
                throw new ArgumentException("Debe seleccionar un nivel jerárquico válido.");
            }

            // Validar niveles jerárquicos:
            // RangoJerarquico: 1: ENL, 2: ERLE, 3: ERL
            var niveles = ListarNiveles();
            NivelEquipo nivelEquipo = niveles.Find(n => n.IdNivelEquipo == equipo.IdNivelEquipo);
            
            if (nivelEquipo == null)
            {
                throw new ArgumentException("Nivel jerárquico inexistente.");
            }

            // Si es ENL (Nacional), no debe tener equipo padre
            if (nivelEquipo.RangoJerarquico == 1 && equipo.IdEquipoPadre.HasValue)
            {
                throw new InvalidOperationException("Un equipo de nivel Nacional (ENL) no puede tener un equipo padre.");
            }

            // Si no es ENL, debe tener obligatoriamente un equipo padre (para mantener la estructura descendente ENL -> ERLE -> ERL)
            if (nivelEquipo.RangoJerarquico > 1 && !equipo.IdEquipoPadre.HasValue)
            {
                throw new InvalidOperationException("Los equipos Regionales (ERLE) y Locales (ERL) deben tener obligatoriamente un equipo supervisor/padre asignado.");
            }

            // Si tiene padre, validar que el nivel del padre sea superior
            if (equipo.IdEquipoPadre.HasValue)
            {
                var equipoPadre = ObtenerEquipoPorId(equipo.IdEquipoPadre.Value);
                if (equipoPadre != null)
                {
                    if (equipoPadre.RangoJerarquico >= nivelEquipo.RangoJerarquico)
                    {
                        throw new InvalidOperationException($"El equipo padre ({equipoPadre.NombreEquipo} - {equipoPadre.NombreNivel}) debe poseer un nivel jerárquico superior al del equipo actual ({nivelEquipo.NombreNivel}).");
                    }
                }
            }
        }

        /// <summary>
        /// Comprueba si proponer a proposedParentId como padre de targetIdEquipo causaría una dependencia circular.
        /// Recorre hacia arriba desde el padre propuesto para ver si encuentra a targetIdEquipo.
        /// </summary>
        private bool DetectarCicloJerarquico(int targetIdEquipo, int proposedParentId)
        {
            int? currentParentId = proposedParentId;

            while (currentParentId.HasValue)
            {
                if (currentParentId.Value == targetIdEquipo)
                {
                    return true; // Se encontró al equipo actual en la ascendencia del padre propuesto -> Ciclo detectado
                }

                var parentObj = _equipoRepository.ObtenerEquipoPorId(currentParentId.Value);
                if (parentObj == null)
                {
                    break;
                }

                currentParentId = parentObj.IdEquipoPadre;
            }

            return false;
        }
    }
}
