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
        public string CorreoInstitucion { get; set; }
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

        // Nuevos campos organizativos y ministeriales
        public string Denominacion { get; set; }
        public string TipoOrganizacion { get; set; }
        public int? CantidadMaestros { get; set; }
        public int? CantidadNinos { get; set; }
        public string Ref1Nombre { get; set; }
        public string Ref1Contacto { get; set; }
        public string Ref2Nombre { get; set; }
        public string Ref2Contacto { get; set; }

        // Control de concurrencia optimista y auditoría
        public byte[] RowVersion { get; set; }
        public string RowVersionString
        {
            get => RowVersion != null ? Convert.ToBase64String(RowVersion) : "";
            set => RowVersion = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioModificacion { get; set; }

        // Personas asociadas (Pastor, Líder, Maestros)
        public PersonaIglesia Pastor { get; set; } = new PersonaIglesia { TipoPersona = "Pastor" };
        public PersonaIglesia LiderMinisterial { get; set; } = new PersonaIglesia { TipoPersona = "LiderMinisterial" };
        public List<Maestro> Maestros { get; set; } = new List<Maestro>();

        // Participación en temporada activa
        public ParticipacionIglesia ParticipacionActual { get; set; }
        public AsignacionRecursos RecursosActuales { get; set; }
        public List<ComentarioIglesia> Comentarios { get; set; } = new List<ComentarioIglesia>();
        public List<CompaneroOracion> CompanerosOracion { get; set; } = new List<CompaneroOracion>();
        public List<HistorialParticipacion> Historial { get; set; } = new List<HistorialParticipacion>();

        // Excepción al requisito de 3 años (Doble Aprobación CE + CMI)
        public ExcepcionRegla3Anios ExcepcionActiva { get; set; }
        public List<ExcepcionRegla3Anios> HistorialExcepciones { get; set; } = new List<ExcepcionRegla3Anios>();
        public DesempenoHistoricoIglesia DesempenoHistorico { get; set; }
        public bool RequiereExcepcion3Anios { get; set; }
        public int DiferenciaAniosAntiguedad { get; set; }
        public int IdTemporadaPrevia { get; set; }
        public string NombreTemporadaPrevia { get; set; }
    }

    public class PersonaIglesia
    {
        public int IdPersonaIglesia { get; set; }
        public int IdIglesia { get; set; }
        public string TipoPersona { get; set; } // 'Pastor', 'LiderMinisterial'
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
        public bool TemporadaActiva { get; set; } = true;
        public bool Participara { get; set; } = true;
        public string JustificacionNoParticipacion { get; set; }
        public string EstadoEvaluacion { get; set; } = "Pendiente"; // Pendiente, Aprobado, NoAprobado
        public string EstatusEvaluacionReporte { get; set; } = "Pendiente"; // Pendiente, Reportó, No Reportó, Castigada
        public int? IdUsuarioEvaluador { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public DateTime? FechaEvaluacion { get; set; }

        // Etapas de la temporada
        public int EtapaActual { get; set; } = 1;

        // Etapa 2: Evaluación Inicial
        public string EvalInicialEstado { get; set; } = "Pendiente"; // Pendiente, Aprobada, Rechazada
        public string EvalInicialMotivo { get; set; }
        public int? EvalInicialIdUsuario { get; set; }
        public string EvalInicialNombreUsuario { get; set; }
        public DateTime? EvalInicialFecha { get; set; }
        public string EvalInicialComentario { get; set; }

        // Etapa 3: Presentación de la Visión
        public bool VisionInvitada { get; set; }
        public DateTime? VisionFecha { get; set; }
        public string VisionLugar { get; set; }
        public bool VisionAsistio { get; set; }
        public string VisionResultado { get; set; } // Continua, No Continua

        // Etapa 4: Elegibilidad Taller OCC
        public string EvalTallerEstado { get; set; } = "Pendiente"; // Pendiente, Aprobada para Taller OCC, No Aprobada
        public string EvalTallerMotivo { get; set; }
        public int? EvalTallerIdUsuario { get; set; }
        public string EvalTallerNombreUsuario { get; set; }
        public DateTime? EvalTallerFecha { get; set; }
        public string EvalTallerComentario { get; set; }

        // Etapa 5: Taller OCC
        public bool TallerParticipo { get; set; }
        public string TallerNombre { get; set; }
        public DateTime? TallerFecha { get; set; }
        public string TallerLugar { get; set; }
        public int TallerCantNinos { get; set; }
        public int TallerCantMaestrosReg { get; set; }
        public int TallerCantMaestrosAsist { get; set; }
        public int TallerCantMaestrosAus { get; set; }
    }

    public class Maestro
    {
        public int IdMaestro { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public int IdEquipoIglesia { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string DocumentoIdentidad { get; set; }
        public string Celular { get; set; }
        public string Correo { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class Evento
    {
        public int IdEvento { get; set; }
        public string NombreEvento { get; set; }
        public string TipoEvento { get; set; } // Vision, Taller, Evangelistico, GranAventura
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public DateTime Fecha { get; set; }
        public string Lugar { get; set; }
        public string Responsable { get; set; }
        public string TipoLugar { get; set; }
        public string Hora { get; set; }
        public int CantidadAsistentes { get; set; }
        public int IdUsuarioCreacion { get; set; }
        public int? IdEquipoCreador { get; set; }
        public string CorreoCreador { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Control de concurrencia optimista y auditoría
        public byte[] RowVersion { get; set; }
        public string RowVersionString
        {
            get => RowVersion != null ? Convert.ToBase64String(RowVersion) : "";
            set => RowVersion = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioModificacion { get; set; }

        // Propiedades de Despacho Presencial
        public string EstadoDespachoEvento { get; set; }
        public int TotalIglesiasAsignadas { get; set; }
        public int TotalIglesiasDespachadas { get; set; }
        public int PorcentajeDespachado => TotalIglesiasAsignadas > 0 ? (TotalIglesiasDespachadas * 100 / TotalIglesiasAsignadas) : 0;
    }

    public class AsistenciaMaestro
    {
        public int IdAsistencia { get; set; }
        public int IdMaestro { get; set; }
        public string NombreMaestro { get; set; }
        public int IdEvento { get; set; }
        public bool Asistio { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int IdUsuarioRegistro { get; set; }
    }

    public class CompaneroOracion
    {
        public int IdCompanero { get; set; }
        public string NombreCompleto { get; set; }
        public string ContactoWhatsApp { get; set; }
        public bool EsMayorEdad { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int IdUsuarioRegistro { get; set; }
        public string CorreoRegistrador { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class HistorialParticipacion
    {
        public int IdHistorial { get; set; }
        public int IdParticipacion { get; set; }
        public DateTime FechaHora { get; set; }
        public string AccionRealizada { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }
        public int IdUsuarioResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public string NombreCoordinador { get; set; }
        public string PosicionCoordinador { get; set; }
        public string EquipoCoordinador { get; set; }
        public string NombreTemporada { get; set; }
        public string Comentario { get; set; }
        public string Razon { get; set; }
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
        public string EstadoAsignacion { get; set; }
        public DateTime? FechaDisponibleDespacho { get; set; }
        public int? IdEventoDespachoActual { get; set; }
    }

    public class ComentarioIglesia
    {
        public int IdComentario { get; set; }
        public int IdIglesia { get; set; }
        public int IdUsuario { get; set; }
        public string CorreoUsuario { get; set; }
        public string Comentario { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string NombreCoordinador { get; set; }
        public string PosicionCoordinador { get; set; }
        public string EquipoCoordinador { get; set; }
        public string NombreTemporada { get; set; }
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

    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public int IdUsuarioDestinatario { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Leida { get; set; }
        public DateTime? FechaLectura { get; set; }
        public int? IdUsuarioLectura { get; set; }
    }

    public class ExcepcionRegla3Anios
    {
        public int IdExcepcion { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public int IdTemporada { get; set; }
        public string NombreTemporada { get; set; }
        public int? TemporadaPreviaId { get; set; }
        public string NombreTemporadaPrevia { get; set; }
        public int DiferenciaTemporadas { get; set; }

        public string Motivo { get; set; }
        public string Justificacion { get; set; }
        public string ResultadoDesempeno { get; set; }

        public int SolicitadoPor { get; set; }
        public string NombreSolicitante { get; set; }
        public DateTime FechaSolicitud { get; set; }

        public bool AprobadoCE { get; set; }
        public int? UsuarioAprobacionCE { get; set; }
        public string NombreUsuarioAprobacionCE { get; set; }
        public DateTime? FechaAprobacionCE { get; set; }
        public string ComentarioCE { get; set; }

        public bool AprobadoCMI { get; set; }
        public int? UsuarioAprobacionCMI { get; set; }
        public string NombreUsuarioAprobacionCMI { get; set; }
        public DateTime? FechaAprobacionCMI { get; set; }
        public string ComentarioCMI { get; set; }

        public bool Rechazado { get; set; }
        public int? UsuarioRechazo { get; set; }
        public string NombreUsuarioRechazo { get; set; }
        public DateTime? FechaRechazo { get; set; }
        public string MotivoRechazo { get; set; }

        public string Estado { get; set; } // PENDIENTE, APROBADA, RECHAZADA, CANCELADA
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public byte[] RowVersion { get; set; }
        public string RowVersionString
        {
            get => RowVersion != null ? Convert.ToBase64String(RowVersion) : "";
            set => RowVersion = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }
    }

    public class DesempenoHistoricoIglesia
    {
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public int IdTemporadaPrevia { get; set; }
        public string NombreTemporadaPrevia { get; set; }
        public int IdParticipacionPrevia { get; set; }
        public int EtapaAlcanzada { get; set; }
        public string EstadoEvaluacion { get; set; }
        public string EstatusReporte { get; set; }
        public bool ReportoEvangelismo { get; set; }
        public bool ReportoDiscipulado { get; set; }
        public int TotalNinosAlcanzados { get; set; }
        public int TotalDecisionesFe { get; set; }
        public int TotalGraduados { get; set; }
        public int TotalMaestrosCapacitados { get; set; }
        public int DiferenciaTemporadas { get; set; }
        public string ResumenTexto { get; set; }
    }
}
