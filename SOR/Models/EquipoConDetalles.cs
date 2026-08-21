using System;

namespace SOR.Models
{
    public class EquipoConDetalles
    {
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int IdNivelEquipo { get; set; }
        public string NombreNivel { get; set; }
        public int RangoJerarquico { get; set; }
        public int? IdEquipoPadre { get; set; }
        public string NombreEquipoPadre { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class NivelEquipo
    {
        public int IdNivelEquipo { get; set; }
        public string NombreNivel { get; set; }
        public int RangoJerarquico { get; set; }
    }
}
