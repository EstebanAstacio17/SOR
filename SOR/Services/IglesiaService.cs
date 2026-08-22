using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using SOR.Models;
using SOR.Repositories;

namespace SOR.Services
{
    public class IglesiaService
    {
        private readonly IglesiaRepository _iglesiaRepository;

        public IglesiaService()
        {
            _iglesiaRepository = new IglesiaRepository();
        }

        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        public List<Iglesia> ObtenerIglesias()
        {
            return _iglesiaRepository.ObtenerIglesias();
        }

        public int RegistrarIglesia(Iglesia modelo, int idUsuarioCreacion)
        {
            if (string.IsNullOrWhiteSpace(modelo.NombreIglesia))
            {
                throw new ArgumentException("El nombre de la iglesia es obligatorio.");
            }

            if (modelo.IdEquipo <= 0)
            {
                throw new ArgumentException("Debe asignar la iglesia a un equipo OCC válido.");
            }

            return _iglesiaRepository.RegistrarIglesia(modelo, idUsuarioCreacion);
        }

        public Iglesia ObtenerExpedienteIglesia(int idIglesia)
        {
            if (idIglesia <= 0) return null;
            return _iglesiaRepository.ObtenerExpedienteIglesia(idIglesia);
        }

        public void EvaluarParticipacion(int idParticipacion, bool participara, string justificacion, string estadoEvaluacion, int idEvaluador)
        {
            if (!participara && string.IsNullOrWhiteSpace(justificacion))
            {
                throw new ArgumentException("Debe proporcionar un motivo o justificación en caso de marcar que la iglesia NO participará esta temporada.");
            }

            _iglesiaRepository.EvaluarParticipacion(idParticipacion, participara, justificacion, estadoEvaluacion, idEvaluador);
        }

        public void DespacharRecursos(AsignacionRecursos modelo, int idDespachador)
        {
            if (modelo == null || modelo.IdParticipacion <= 0)
            {
                throw new ArgumentException("Modelo de asignación de recursos inválido.");
            }

            _iglesiaRepository.DespacharRecursos(modelo, idDespachador);
        }

        public void AgregarComentario(int idIglesia, int idUsuario, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                throw new ArgumentException("El contenido del comentario no puede estar vacío.");
            }

            _iglesiaRepository.AgregarComentario(idIglesia, idUsuario, comentario);
        }

        // ============================================================================
        // MÉTODOS DE TRANSICIÓN DE ETAPAS DE LA TEMPORADA ACTIVA (LÓGICA GESTIÓN TEMP)
        // ============================================================================

        public void AvanzarEtapa2(int idParticipacion, string estado, string motivo, string comentario, int idUsuario, int? idEventoVision = null)
        {
            if (string.IsNullOrWhiteSpace(estado)) throw new ArgumentException("El estado de la evaluación es requerido.");
            if (estado == "Rechazada" && string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Debe ingresar un motivo para el rechazo.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        if (estado == "Aprobada")
                        {
                            if (!idEventoVision.HasValue || idEventoVision.Value <= 0)
                            {
                                throw new ArgumentException("Debe seleccionar un evento de Presentación de la Visión para invitar a la iglesia.");
                            }

                            // Vincular la iglesia al evento de Presentación de la Visión
                            string sqlLink = @"
                                IF NOT EXISTS (SELECT 1 FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart)
                                BEGIN
                                    INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEvento, @IdPart, 0);
                                END";
                            using (SqlCommand cmdLink = new SqlCommand(sqlLink, cn, tran))
                            {
                                cmdLink.Parameters.AddWithValue("@IdEvento", idEventoVision.Value);
                                cmdLink.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmdLink.ExecuteNonQuery();
                            }
                        }

                        int etapaNueva = (estado == "Aprobada") ? 3 : 2;
                        string estadoEvaluacion = (estado == "Aprobada") ? "Aprobado" : "Rechazado";

                        string sql = @"
                            UPDATE dbo.ParticipacionesIglesia SET
                                EtapaActual = @Etapa,
                                EstadoEvaluacion = @EstadoEval,
                                EvalInicialEstado = @Estado,
                                EvalInicialMotivo = @Motivo,
                                EvalInicialIdUsuario = @IdUser,
                                EvalInicialFecha = GETDATE(),
                                EvalInicialComentario = @Comentario
                            WHERE IdParticipacion = @IdPart;";

                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Etapa", etapaNueva);
                            cmd.Parameters.AddWithValue("@EstadoEval", estadoEvaluacion);
                            cmd.Parameters.AddWithValue("@Estado", estado);
                            cmd.Parameters.AddWithValue("@Motivo", motivo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            cmd.Parameters.AddWithValue("@Comentario", comentario ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmd.ExecuteNonQuery();
                        }

                        // Registrar Log Historial
                        string logCom = (estado == "Aprobada") 
                            ? "Evaluación inicial APROBADA. Avanza a Presentación de la Visión (Etapa 3)." 
                            : $"Evaluación inicial RECHAZADA. Motivo: {motivo}.";

                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Evaluación Inicial", "Inscrita (Etapa 1)", 
                            (estado == "Aprobada") ? "Presentación Visión (Etapa 3)" : "Rechazado (Etapa 2)", idUsuario, logCom, motivo);

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AvanzarEtapa3(int idParticipacion, bool invitada, DateTime? fecha, string lugar, bool asistio, string resultado, int idUsuario, int? idEventoTaller = null)
        {
            if (string.IsNullOrWhiteSpace(resultado)) throw new ArgumentException("El resultado de la presentación es requerido.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        if (resultado == "Continua")
                        {
                            if (!asistio)
                            {
                                throw new InvalidOperationException("No se puede continuar en el proceso si no se confirma la asistencia al evento de Presentación de la Visión.");
                            }
                            if (!idEventoTaller.HasValue || idEventoTaller.Value <= 0)
                            {
                                throw new ArgumentException("Debe seleccionar un evento de Taller OCC para invitar a la iglesia.");
                            }

                            // Vincular la iglesia al evento de Taller OCC
                            string sqlLinkTaller = @"
                                IF NOT EXISTS (SELECT 1 FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart)
                                BEGIN
                                    INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEvento, @IdPart, 0);
                                END";
                            using (SqlCommand cmdLink = new SqlCommand(sqlLinkTaller, cn, tran))
                            {
                                cmdLink.Parameters.AddWithValue("@IdEvento", idEventoTaller.Value);
                                cmdLink.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmdLink.ExecuteNonQuery();
                            }
                        }

                        // Sincronizar el estado de asistencia de Visión en dbo.EventosParticipacionIglesia
                        string sqlFindVision = @"
                            SELECT ep.IdEvento 
                            FROM dbo.EventosParticipacionIglesia ep
                            INNER JOIN dbo.Eventos e ON ep.IdEvento = e.IdEvento
                            WHERE ep.IdParticipacion = @IdPart AND e.TipoEvento = 'Vision';";
                        using (SqlCommand cmdFind = new SqlCommand(sqlFindVision, cn, tran))
                        {
                            cmdFind.Parameters.AddWithValue("@IdPart", idParticipacion);
                            object valVision = cmdFind.ExecuteScalar();
                            if (valVision != null)
                            {
                                int idEventoVision = Convert.ToInt32(valVision);
                                string sqlUpdateAsist = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                                using (SqlCommand cmdUp = new SqlCommand(sqlUpdateAsist, cn, tran))
                                {
                                    cmdUp.Parameters.AddWithValue("@Asistio", asistio);
                                    cmdUp.Parameters.AddWithValue("@IdEvento", idEventoVision);
                                    cmdUp.Parameters.AddWithValue("@IdPart", idParticipacion);
                                    cmdUp.ExecuteNonQuery();
                                }
                            }
                        }

                        int etapaNueva = (resultado == "Continua") ? 4 : 3;
                        string estadoEvaluacion = (resultado == "Continua") ? "Aprobado" : "Rechazado";

                        string sql = @"
                            UPDATE dbo.ParticipacionesIglesia SET
                                EtapaActual = @Etapa,
                                EstadoEvaluacion = @EstadoEval,
                                VisionInvitada = @Invitada,
                                VisionFecha = @Fecha,
                                VisionLugar = @Lugar,
                                VisionAsistio = @Asistio,
                                VisionResultado = @Resultado
                            WHERE IdParticipacion = @IdPart;";

                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Etapa", etapaNueva);
                            cmd.Parameters.AddWithValue("@EstadoEval", estadoEvaluacion);
                            cmd.Parameters.AddWithValue("@Invitada", invitada);
                            cmd.Parameters.AddWithValue("@Fecha", (object)fecha ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Lugar", lugar ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Asistio", asistio);
                            cmd.Parameters.AddWithValue("@Resultado", resultado);
                            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmd.ExecuteNonQuery();
                        }

                        // Registrar Log Historial
                        string logCom = (resultado == "Continua") 
                            ? "Asistencia a Presentación de la Visión registrada. Elegible para Taller OCC (Etapa 4)." 
                            : "Asistencia a Presentación de la Visión registrada. La iglesia NO continúa en el proceso.";

                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Presentación de la Visión", "Evaluación Inicial (Etapa 2)", 
                            (resultado == "Continua") ? "Elegibilidad Taller (Etapa 4)" : "Rechazado (Etapa 3)", idUsuario, logCom);

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AvanzarEtapa4(int idParticipacion, int idIglesia, string estado, string motivo, string comentario, int idUsuario, int? idEventoTaller = null, int? cantidadAsistentes = null, List<Maestro> maestrosNuevos = null)
        {
            if (string.IsNullOrWhiteSpace(estado)) throw new ArgumentException("La decisión de elegibilidad es requerida.");
            if (estado == "No Aprobada" && string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Debe ingresar un motivo para la no aprobación.");
            if (estado == "Aprobada para Taller OCC" && (!idEventoTaller.HasValue || idEventoTaller.Value <= 0))
                throw new ArgumentException("Debe seleccionar un evento de Taller OCC para asignar a la iglesia.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        if (estado == "Aprobada para Taller OCC")
                        {
                            // Insertar maestros nuevos si se suministraron
                            if (maestrosNuevos != null && maestrosNuevos.Count > 0)
                            {
                                foreach (var m in maestrosNuevos)
                                {
                                    if (!string.IsNullOrWhiteSpace(m.Nombres) && !string.IsNullOrWhiteSpace(m.Apellidos))
                                    {
                                        string sqlInsM = @"
                                            INSERT INTO dbo.Maestros (IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo)
                                            VALUES (@IdIglesia, @Nombres, @Apellidos, @Doc, @Cel, @Correo, 1);";
                                        using (SqlCommand cmdInsM = new SqlCommand(sqlInsM, cn, tran))
                                        {
                                            cmdInsM.Parameters.AddWithValue("@IdIglesia", idIglesia);
                                            cmdInsM.Parameters.AddWithValue("@Nombres", m.Nombres.Trim());
                                            cmdInsM.Parameters.AddWithValue("@Apellidos", m.Apellidos.Trim());
                                            cmdInsM.Parameters.AddWithValue("@Doc", string.IsNullOrWhiteSpace(m.DocumentoIdentidad) ? (object)DBNull.Value : m.DocumentoIdentidad.Trim());
                                            cmdInsM.Parameters.AddWithValue("@Cel", string.IsNullOrWhiteSpace(m.Celular) ? (object)DBNull.Value : m.Celular.Trim());
                                            cmdInsM.Parameters.AddWithValue("@Correo", string.IsNullOrWhiteSpace(m.Correo) ? (object)DBNull.Value : m.Correo.Trim());
                                            cmdInsM.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            // Validar que la iglesia no esté rechazada en la etapa inicial
                            string sqlCheck = "SELECT VisionAsistio, VisionResultado, EtapaActual, EstadoEvaluacion FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = @IdPart;";
                            using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn, tran))
                            {
                                cmdCheck.Parameters.AddWithValue("@IdPart", idParticipacion);
                                using (SqlDataReader dr = cmdCheck.ExecuteReader())
                                {
                                    if (dr.Read())
                                    {
                                        int etapaActual = Convert.ToInt32(dr["EtapaActual"]);
                                        string estadoEval = dr["EstadoEvaluacion"].ToString();
                                        if (etapaActual < 2 || estadoEval == "Rechazado")
                                        {
                                            throw new InvalidOperationException("No se puede aprobar la elegibilidad para el Taller OCC porque la iglesia no ha sido aprobada en los pasos de evaluación inicial anteriores.");
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("No se encontró el registro de participación de la iglesia.");
                                    }
                                }
                            }
                        }

                        int etapaNueva = (estado == "Aprobada para Taller OCC") ? 5 : 4;
                        string estadoEvaluacion = (estado == "Aprobada para Taller OCC") ? "Aprobado" : "Rechazado";

                        string sql = @"
                            UPDATE dbo.ParticipacionesIglesia SET
                                EtapaActual = @Etapa,
                                EstadoEvaluacion = @EstadoEval,
                                EvalTallerEstado = @Estado,
                                EvalTallerMotivo = @Motivo,
                                EvalTallerIdUsuario = @IdUser,
                                EvalTallerFecha = GETDATE(),
                                EvalTallerComentario = @Comentario,
                                VisionAsistio = CASE WHEN @Estado = 'Aprobada para Taller OCC' THEN 1 ELSE VisionAsistio END,
                                VisionResultado = CASE WHEN @Estado = 'Aprobada para Taller OCC' THEN 'Continua' ELSE VisionResultado END
                            WHERE IdParticipacion = @IdPart;";

                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Etapa", etapaNueva);
                            cmd.Parameters.AddWithValue("@EstadoEval", estadoEvaluacion);
                            cmd.Parameters.AddWithValue("@Estado", estado);
                            cmd.Parameters.AddWithValue("@Motivo", motivo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                            cmd.Parameters.AddWithValue("@Comentario", comentario ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmd.ExecuteNonQuery();
                        }

                        // Si se aprueba para taller, registrar la invitación al evento de Taller OCC
                        if (estado == "Aprobada para Taller OCC" && idEventoTaller.HasValue)
                        {
                            // Actualizar la cantidad de asistentes del evento sumando o actualizando
                            string sqlUpCantEv = "UPDATE dbo.Eventos SET CantidadAsistentes = ISNULL(CantidadAsistentes, 0) + @Cant WHERE IdEvento = @IdEv;";
                            using (SqlCommand cmdUCE = new SqlCommand(sqlUpCantEv, cn, tran))
                            {
                                cmdUCE.Parameters.AddWithValue("@Cant", cantidadAsistentes.HasValue ? cantidadAsistentes.Value : 0);
                                cmdUCE.Parameters.AddWithValue("@IdEv", idEventoTaller.Value);
                                cmdUCE.ExecuteNonQuery();
                            }

                            // Verificar que no exista ya la invitación
                            string sqlCheckInv = "SELECT COUNT(1) FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEv AND IdParticipacion = @IdPart;";
                            using (SqlCommand cmdCI = new SqlCommand(sqlCheckInv, cn, tran))
                            {
                                cmdCI.Parameters.AddWithValue("@IdEv", idEventoTaller.Value);
                                cmdCI.Parameters.AddWithValue("@IdPart", idParticipacion);
                                int countInv = Convert.ToInt32(cmdCI.ExecuteScalar());
                                if (countInv == 0)
                                {
                                    string sqlInv = "INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEv, @IdPart, 0);";
                                    using (SqlCommand cmdInv = new SqlCommand(sqlInv, cn, tran))
                                    {
                                        cmdInv.Parameters.AddWithValue("@IdEv", idEventoTaller.Value);
                                        cmdInv.Parameters.AddWithValue("@IdPart", idParticipacion);
                                        cmdInv.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Registrar Log Historial
                        string logCom = (estado == "Aprobada para Taller OCC") 
                            ? "Elegibilidad de Taller aprobada. Habilitada para Taller OCC (Etapa 5)." 
                            : $"Elegibilidad de Taller RECHAZADA. Motivo: {motivo}.";

                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Elegibilidad Taller", "Presentación Visión (Etapa 3)", 
                            (estado == "Aprobada para Taller OCC") ? "Taller OCC (Etapa 5)" : "Rechazado (Etapa 4)", idUsuario, logCom, motivo);

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AvanzarEtapa5(int idParticipacion, string tallerNombre, DateTime? tallerFecha, string tallerLugar, int cantNinos, int cantMaestrosReg, int cantMaestrosAsist, int cantMaestrosAus, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(tallerNombre)) throw new ArgumentException("El nombre del taller es obligatorio.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
                            UPDATE dbo.ParticipacionesIglesia SET
                                EtapaActual = 5,
                                EstadoEvaluacion = 'Aprobado',
                                TallerParticipo = 1,
                                TallerNombre = @Taller,
                                TallerFecha = @Fecha,
                                TallerLugar = @Lugar,
                                TallerCantNinos = @Ninos,
                                TallerCantMaestrosReg = @MaestrosReg,
                                TallerCantMaestrosAsist = @MaestrosAsist,
                                TallerCantMaestrosAus = @MaestrosAus
                            WHERE IdParticipacion = @IdPart;";

                        using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@Taller", tallerNombre);
                            cmd.Parameters.AddWithValue("@Fecha", (object)tallerFecha ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Lugar", tallerLugar ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ninos", cantNinos);
                            cmd.Parameters.AddWithValue("@MaestrosReg", cantMaestrosReg);
                            cmd.Parameters.AddWithValue("@MaestrosAsist", cantMaestrosAsist);
                            cmd.Parameters.AddWithValue("@MaestrosAus", cantMaestrosAus);
                            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmd.ExecuteNonQuery();
                        }

                        // Registrar Log Historial
                        string logCom = $"Taller OCC registrado exitosamente. Taller: {tallerNombre}. Niños: {cantNinos}, Maestros Asistentes: {cantMaestrosAsist}.";

                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Taller OCC Completado", "Elegibilidad Taller (Etapa 4)", 
                            "Taller OCC (Etapa 5)", idUsuario, logCom);

                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void ActualizarIglesia(Iglesia modelo, int idUsuarioEdicion)
        {
            _iglesiaRepository.ActualizarIglesia(modelo, idUsuarioEdicion);
        }
    }
}
