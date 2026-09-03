using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SOR.Models.Reportes
{
    public class EosDashboardViewModel
    {
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int? IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int? IdIglesia { get; set; }
        public string NombreIglesia { get; set; }

        public List<SelectListItem> ListaTemporadas { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaEquipos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaIglesias { get; set; } = new List<SelectListItem>();

        public MovilizacionReporteDTO Movilizacion { get; set; } = new MovilizacionReporteDTO();
        public DiscipuladoReporteDTO Discipulado { get; set; } = new DiscipuladoReporteDTO();
        public OracionReporteDTO Oracion { get; set; } = new OracionReporteDTO();
    }

    // 1. MOVILIZACIÓN
    public class MovilizacionReporteDTO
    {
        public int TotalPresentacionesVision { get; set; }
        public int TotalAsistentesVision { get; set; }
        public int EquiposMinisterialesCapacitados { get; set; }
        public int CajitasEntregadasCompaneros { get; set; }
        public int EventosEvangelisticos { get; set; }
        public int NinosAsistentesEvangelisticos { get; set; }

        public List<IglesiaPlantadaDTO> IglesiasPlantadas { get; set; } = new List<IglesiaPlantadaDTO>();
        public List<GnaDTO> GruposNoAlcanzados { get; set; } = new List<GnaDTO>();
    }

    public class IglesiaPlantadaDTO
    {
        public int IdIglesiaPlantada { get; set; }
        public int IdTemporada { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string NombreIglesia { get; set; }
        public string PastorPrincipal { get; set; }
        public string Ubicacion { get; set; }
        public int CajitasEntregadas { get; set; }
        public int InscritosLGA { get; set; }
        public DateTime? FechaPlantacion { get; set; }
        public string Notas { get; set; }
    }

    public class GnaDTO
    {
        public int IdGNA { get; set; }
        public int IdTemporada { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string NombreGNA { get; set; }
        public string CompaneroMinisterio { get; set; }
        public int CajitasEntregadas { get; set; }
        public int InscritosLGA { get; set; }
        public int NinosCreenJesus { get; set; }
        public int NinosOranComparten { get; set; }
        public int NinosGraduados { get; set; }
        public string Notas { get; set; }
    }

    // 2. DISCIPULADO
    public class DiscipuladoReporteDTO
    {
        public int TotalCapacitacionesOCC { get; set; }
        public int TotalAsistentesCapacitacion { get; set; }

        // La Gran Aventura (LGA)
        public int LgaCursosImpartidos { get; set; }
        public int LgaNinosAsistentes { get; set; }
        public int LgaDecisionesJesus { get; set; }
        public int LgaComprometidosOrarCompartir { get; set; }
        public int LgaGraduadosTotales { get; set; }

        // Valores de Crecimiento (VDC)
        public int VdcAsistieronUnaClase { get; set; }
        public int VdcAsistieronSeisClases { get; set; }
        public int VdcContinuaronLgaODet { get; set; }
    }

    // 3. ORACIÓN
    public class OracionReporteDTO
    {
        public int EventosOracionOrganizados { get; set; }
        public int TotalAsistentesOracion { get; set; }
        public int CompanerosOracionReportados { get; set; }
        public int MiembrosRedOracionLocal { get; set; }
    }
}
