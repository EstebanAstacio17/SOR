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

        // Control de concurrencia optimista y auditoría
        public byte[] RowVersion { get; set; }
        public string RowVersionString
        {
            get => RowVersion != null ? Convert.ToBase64String(RowVersion) : "";
            set => RowVersion = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioModificacion { get; set; }
    }

    public class NivelEquipo
    {
        public int IdNivelEquipo { get; set; }
        public string NombreNivel { get; set; }
        public int RangoJerarquico { get; set; }
    }
}
