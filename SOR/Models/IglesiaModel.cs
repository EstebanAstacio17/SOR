using System;
using System.Collections.Generic;

namespace SOR.Models
{
    public class Iglesia
    {
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public string RNC_Cedula { get; set; }
        public string Telefono { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Sector { get; set; }
        public string Ciudad { get; set; }
        public string Provincia { get; set; }
        public string Referencia { get; set; }
        public int IdEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public int? IdUsuarioCreacion { get; set; }
        public DateTime? FechaCreacion { get; set; }

        // Personas asociadas (Pastor, Líder, Maestros)
        public PersonaIglesia Pastor { get; set; } = new PersonaIglesia { TipoPersona = "Pastor" };
        public PersonaIglesia LiderMinisterial { get; set; } = new PersonaIglesia { TipoPersona = "LiderMinisterial" };
        public List<PersonaIglesia> Maestros { get; set; } = new List<PersonaIglesia>();

        // Participación en temporada activa
        public ParticipacionIglesia ParticipacionActual { get; set; }
        public AsignacionRecursos RecursosActuales { get; set; }
        public List<ComentarioIglesia> Comentarios { get; set; } = new List<ComentarioIglesia>();
    }

    public class PersonaIglesia
    {
        public int IdPersonaIglesia { get; set; }
        public int IdIglesia { get; set; }
        public string TipoPersona { get; set; } // 'Pastor', 'LiderMinisterial', 'Maestro'
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string DocumentoIdentidad { get; set; }
        public string DocumentoAdjuntoRuta { get; set; }
        public string Celular { get; set; }
        public string Correo { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Sector { get; set; }
        public string Referencia { get; set; }
    }

    public class Temporada
    {
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activa { get; set; }
    }

    public class ParticipacionIglesia
    {
        public int IdParticipacion { get; set; }
        public int IdIglesia { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public bool Participara { get; set; } = true;
        public string JustificacionNoParticipacion { get; set; }
        public string EstadoEvaluacion { get; set; } = "Pendiente"; // Pendiente, Aprobado, NoAprobado
        public int? IdUsuarioEvaluador { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public DateTime? FechaEvaluacion { get; set; }
    }

    public class AsignacionRecursos
    {
        public int IdAsignacionRecurso { get; set; }
        public int IdParticipacion { get; set; }
        public int OportunidadesEvangelisticas { get; set; }
        public int LibrosMejorRegalo { get; set; }
        public int LibrosMaestros { get; set; }
        public int LibrosAlumno { get; set; }
        public int Posters { get; set; }
        public int NuevosTestamentos { get; set; }
        public DateTime? FechaDespacho { get; set; }
        public int? IdUsuarioDespacho { get; set; }
    }

    public class ReporteEvento
    {
        public int IdReporteEvento { get; set; }
        public int IdParticipacion { get; set; }
        public string TipoReporte { get; set; } // 'Evangelistico' o 'GranAventura'
        public DateTime? Fecha { get; set; }
        public int CantidadNinos { get; set; }
        public int CantidadClases { get; set; }
        public string AsistenciaPorClase { get; set; }
        public int CuantosAceptaronSenor { get; set; }
        public int CuantosComprometieron { get; set; }
        public int CuantosGraduaron { get; set; }
        public string ReporteAdjuntoRuta { get; set; }
        public string Notas { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }

    public class ComentarioIglesia
    {
        public int IdComentario { get; set; }
        public int IdIglesia { get; set; }
        public int IdUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string Comentario { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }
}
