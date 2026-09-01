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

        public static bool ValidarFormatoRncCedula(string doc)
        {
            if (string.IsNullOrWhiteSpace(doc)) return false;
            string clean = doc.Replace("-", "").Replace(" ", "").Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(clean, @"^\d{9}$|^\d{11}$");
        }

        public static bool ValidarFormatoTelefono(string tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return false;
            string clean = tel.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(clean, @"^\d{10}$");
        }

        public static bool ValidarFormatoCorreo(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public int RegistrarIglesia(Iglesia modelo, int idUsuarioCreacion, int? idTemporada = null, List<string> outAdvertencias = null)
        {
            if (string.IsNullOrWhiteSpace(modelo.NombreIglesia))
            {
                throw new ArgumentException("El nombre de la iglesia es obligatorio.");
            }

            if (modelo.IdEquipo <= 0)
            {
                throw new ArgumentException("Debe asignar la iglesia a un equipo OCC válido.");
            }

            // 1. Determinar temporada destino y temporada activa
            int idTemporadaDestino = idTemporada ?? 0;
            bool esTemporadaCurso = false;
            int idTemporadaActiva = 0;
            int minAniosAntiguedad = 3;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlTemp = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                using (SqlCommand cmdTemp = new SqlCommand(sqlTemp, cn))
                {
                    object idTempObj = cmdTemp.ExecuteScalar();
                    if (idTempObj != null) idTemporadaActiva = Convert.ToInt32(idTempObj);
                }

                if (idTemporadaDestino <= 0)
                {
                    idTemporadaDestino = idTemporadaActiva;
                }
                esTemporadaCurso = (idTemporadaDestino == idTemporadaActiva);

                // Obtener MinAniosAntiguedad
                string sqlCfg = "SELECT Valor FROM dbo.ConfiguracionesSistema WHERE Clave = 'MinAniosAntiguedad';";
                using (SqlCommand cmdCfg = new SqlCommand(sqlCfg, cn))
                {
                    object val = cmdCfg.ExecuteScalar();
                    if (val != null && int.TryParse(val.ToString(), out int parsed))
                    {
                        minAniosAntiguedad = parsed;
                    }
                }
            }

            // 2. Validar Campos Vacíos en la Temporada de Curso
            if (esTemporadaCurso)
            {
                if (string.IsNullOrWhiteSpace(modelo.RNC_Cedula))
                    throw new ArgumentException("El RNC o Cédula de la iglesia es obligatorio para la temporada en curso.");
                if (string.IsNullOrWhiteSpace(modelo.Telefono))
                    throw new ArgumentException("El teléfono de la iglesia es obligatorio para la temporada en curso.");
                if (modelo.Pastor == null || string.IsNullOrWhiteSpace(modelo.Pastor.Nombres))
                    throw new ArgumentException("El nombre del pastor es obligatorio para la temporada en curso.");
                if (modelo.LiderMinisterial == null || string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Nombres))
                    throw new ArgumentException("El nombre del líder discipulado es obligatorio para la temporada en curso.");
            }

            // 3. Validar formatos (si no están vacíos)
            if (!string.IsNullOrWhiteSpace(modelo.RNC_Cedula) && !ValidarFormatoRncCedula(modelo.RNC_Cedula))
                throw new ArgumentException("El formato del RNC o Cédula de la iglesia no es correcto (debe tener 9 u 11 dígitos).");
            if (!string.IsNullOrWhiteSpace(modelo.Telefono) && !ValidarFormatoTelefono(modelo.Telefono))
                throw new ArgumentException("El formato del teléfono institucional no es correcto (debe tener 10 dígitos).");

            if (modelo.Pastor != null)
            {
                if (!string.IsNullOrWhiteSpace(modelo.Pastor.DocumentoIdentidad) && !ValidarFormatoRncCedula(modelo.Pastor.DocumentoIdentidad))
                    throw new ArgumentException("El formato de la cédula del pastor no es correcto.");
                if (!string.IsNullOrWhiteSpace(modelo.Pastor.Celular) && !ValidarFormatoTelefono(modelo.Pastor.Celular))
                    throw new ArgumentException("El formato del celular del pastor no es correcto.");
                if (!string.IsNullOrWhiteSpace(modelo.Pastor.Correo) && !ValidarFormatoCorreo(modelo.Pastor.Correo))
                    throw new ArgumentException("El formato del correo del pastor no es correcto.");
            }

            if (modelo.LiderMinisterial != null)
            {
                if (!string.IsNullOrWhiteSpace(modelo.LiderMinisterial.DocumentoIdentidad) && !ValidarFormatoRncCedula(modelo.LiderMinisterial.DocumentoIdentidad))
                    throw new ArgumentException("El formato de la cédula del líder no es correcto.");
                if (!string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Celular) && !ValidarFormatoTelefono(modelo.LiderMinisterial.Celular))
                    throw new ArgumentException("El formato del celular del líder no es correcto.");
                if (!string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Correo) && !ValidarFormatoCorreo(modelo.LiderMinisterial.Correo))
                    throw new ArgumentException("El formato del correo del líder no es correcto.");
            }

            // 4 y 5. Validar reglas de castigo, antigüedad y estado "No reportó"
            ValidarReglasCastigoYAntiguedad(null, null, modelo.RNC_Cedula, modelo.Pastor?.DocumentoIdentidad, idTemporadaDestino, outAdvertencias);

            // 6 y 7. Validaciones de unicidad vía Stored Procedure
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                
                // Asegurar que SpValidarUnicidadPastorYIglesia exista y devuelva el nombre del equipo
                string spCreate = @"
                    IF OBJECT_ID(N'[dbo].[SpValidarUnicidadPastorYIglesia]', N'P') IS NOT NULL
                        DROP PROCEDURE dbo.SpValidarUnicidadPastorYIglesia;
                ";
                using (SqlCommand cmdDrop = new SqlCommand(spCreate, cn))
                {
                    cmdDrop.ExecuteNonQuery();
                }

                string spDef = @"
                    CREATE PROCEDURE dbo.SpValidarUnicidadPastorYIglesia
                        @IdTemporada INT,
                        @RncCedulaIglesia VARCHAR(50),
                        @CedulaPastor VARCHAR(50),
                        @ExcluirIdIglesia INT = 0
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        DECLARE @NomEquipo NVARCHAR(200);
                        DECLARE @IdIglesiaReg INT;

                        SELECT TOP 1 @NomEquipo = e.NombreEquipo, @IdIglesiaReg = i.IdIglesia
                        FROM dbo.ParticipacionesIglesia p 
                        INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia 
                        INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                        WHERE p.IdTemporada = @IdTemporada 
                          AND (
                              i.RNC_Cedula = @RncCedulaIglesia 
                              OR REPLACE(REPLACE(ISNULL(i.RNC_Cedula, ''), '-', ''), ' ', '') = REPLACE(REPLACE(@RncCedulaIglesia, '-', ''), ' ', '')
                          )
                          AND i.IdIglesia <> @ExcluirIdIglesia;

                        IF @NomEquipo IS NOT NULL
                        BEGIN
                            DECLARE @Msg NVARCHAR(500) = 'Esta iglesia ya está registrada en el equipo: ' + @NomEquipo + '|' + CAST(@IdIglesiaReg AS NVARCHAR(20));
                            RAISERROR(@Msg, 16, 1);
                            RETURN;
                        END

                        IF EXISTS (
                            SELECT 1 
                            FROM dbo.PersonasIglesia per 
                            INNER JOIN dbo.ParticipacionesIglesia p ON per.IdIglesia = p.IdIglesia
                            WHERE p.IdTemporada = @IdTemporada 
                              AND per.TipoPersona = 'Pastor' 
                              AND REPLACE(per.DocumentoIdentidad, '-', '') = REPLACE(@CedulaPastor, '-', '')
                              AND per.IdIglesia <> @ExcluirIdIglesia
                        )
                        BEGIN
                            RAISERROR('El pastor con la cédula indicada ya está registrado en otra iglesia en esta temporada.', 16, 1);
                            RETURN;
                        END
                    END";
                using (SqlCommand cmdCreate = new SqlCommand(spDef, cn))
                {
                    cmdCreate.ExecuteNonQuery();
                }

                // Ejecutar SP
                using (SqlCommand cmdSp = new SqlCommand("dbo.SpValidarUnicidadPastorYIglesia", cn))
                {
                    cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdSp.Parameters.AddWithValue("@IdTemporada", idTemporadaDestino);
                    cmdSp.Parameters.AddWithValue("@RncCedulaIglesia", modelo.RNC_Cedula?.Trim() ?? "");
                    cmdSp.Parameters.AddWithValue("@CedulaPastor", modelo.Pastor?.DocumentoIdentidad?.Trim() ?? "");
                    cmdSp.Parameters.AddWithValue("@ExcluirIdIglesia", modelo.IdIglesia);
                    cmdSp.ExecuteNonQuery();
                }
            }

            // 8. Advertencia de Líder o Maestro ya asignado en la Temporada Activa
            if (modelo.LiderMinisterial != null && !string.IsNullOrWhiteSpace(modelo.LiderMinisterial.DocumentoIdentidad))
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sqlAdvL = @"
                        SELECT i.NombreIglesia, e.NombreEquipo, ne.NombreNivel
                        FROM dbo.PersonasIglesia per 
                        INNER JOIN dbo.Iglesias i ON per.IdIglesia = i.IdIglesia
                        INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                        INNER JOIN dbo.NivelesEquipo ne ON e.IdNivelEquipo = ne.IdNivelEquipo
                        INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                        WHERE p.IdTemporada = @IdTemp AND per.TipoPersona = 'LiderMinisterial' AND per.DocumentoIdentidad = @Doc;";
                    using (SqlCommand cmdAdvL = new SqlCommand(sqlAdvL, cn))
                    {
                        cmdAdvL.Parameters.AddWithValue("@IdTemp", idTemporadaDestino);
                        cmdAdvL.Parameters.AddWithValue("@Doc", modelo.LiderMinisterial.DocumentoIdentidad.Replace("-", "").Trim());
                        using (SqlDataReader dr = cmdAdvL.ExecuteReader())
                        {
                            if (dr.Read() && outAdvertencias != null)
                            {
                                outAdvertencias.Add($"El líder discipulado con identificación '{modelo.LiderMinisterial.DocumentoIdentidad}' ya está registrado en la iglesia '{dr["NombreIglesia"]}', zona/equipo '{dr["NombreEquipo"]}' ({dr["NombreNivel"]}).");
                            }
                        }
                    }
                }
            }

            return _iglesiaRepository.RegistrarIglesia(modelo, idUsuarioCreacion, idTemporadaDestino);
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

        public void AvanzarEtapa2(int idParticipacion, string estado, string motivo, string comentario, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(estado)) throw new ArgumentException("El estado de la evaluación es requerido.");
            if ((estado == "Rechazada" || estado == "Detenido") && string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Debe ingresar un motivo para el rechazo o detención.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        if (estado == "Aprobada")
                        {
                            string rncCedula = "";
                            string pastorCedula = "";
                            int idTemporadaDestino = 0;
                            string sqlGetInfo = @"
                                SELECT i.RNC_Cedula, p.IdTemporada, per.DocumentoIdentidad
                                FROM dbo.ParticipacionesIglesia p
                                INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                                LEFT JOIN dbo.PersonasIglesia per ON i.IdIglesia = per.IdIglesia AND per.TipoPersona = 'Pastor'
                                WHERE p.IdParticipacion = @IdPart;";
                            using (SqlCommand cmdInfo = new SqlCommand(sqlGetInfo, cn, tran))
                            {
                                cmdInfo.Parameters.AddWithValue("@IdPart", idParticipacion);
                                using (SqlDataReader drInfo = cmdInfo.ExecuteReader())
                                {
                                    if (drInfo.Read())
                                    {
                                        rncCedula = drInfo["RNC_Cedula"].ToString();
                                        idTemporadaDestino = Convert.ToInt32(drInfo["IdTemporada"]);
                                        pastorCedula = drInfo["DocumentoIdentidad"] != DBNull.Value ? drInfo["DocumentoIdentidad"].ToString() : "";
                                    }
                                }
                            }

                            ValidarReglasCastigoYAntiguedad(cn, tran, rncCedula, pastorCedula, idTemporadaDestino, null);
                        }

                        int etapaNueva = (estado == "Aprobada") ? 2 : 1;
                        string estadoEvaluacion = (estado == "Aprobada") ? "Aprobado" : ((estado == "Detenido") ? "Detenido" : "Rechazado");

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
                            ? "Evaluación inicial APROBADA. Avanza a Evaluada Inicial (Etapa 2)." 
                            : $"Evaluación inicial {estado.ToUpper()}. Motivo: {motivo}.";

                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Evaluación Inicial", "Inscrita (Etapa 1)", 
                            (estado == "Aprobada") ? "Evaluada (Etapa 2)" : "Rechazado (Etapa 1)", idUsuario, logCom, motivo);

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

        public void AsignarEventoVision(int idParticipacion, int idIglesia, int idEventoVision, PersonaIglesia pastor, PersonaIglesia lider, int idUsuario)
        {
            if (idEventoVision <= 0) throw new ArgumentException("Debe seleccionar un evento de Presentación de la Visión.");
            if (pastor == null || string.IsNullOrWhiteSpace(pastor.Nombres)) throw new ArgumentException("Los nombres del pastor son obligatorios.");
            if (lider == null || string.IsNullOrWhiteSpace(lider.Nombres)) throw new ArgumentException("Los nombres del líder ministerial son obligatorios.");

            // Validar formatos de pastor y líder
            if (!string.IsNullOrWhiteSpace(pastor.DocumentoIdentidad) && !ValidarFormatoRncCedula(pastor.DocumentoIdentidad))
                throw new ArgumentException("El formato de la cédula del pastor no es correcto.");
            if (!string.IsNullOrWhiteSpace(pastor.Celular) && !ValidarFormatoTelefono(pastor.Celular))
                throw new ArgumentException("El formato del celular del pastor no es correcto.");
            if (!string.IsNullOrWhiteSpace(pastor.Correo) && !ValidarFormatoCorreo(pastor.Correo))
                throw new ArgumentException("El formato del correo del pastor no es correcto.");

            if (!string.IsNullOrWhiteSpace(lider.DocumentoIdentidad) && !ValidarFormatoRncCedula(lider.DocumentoIdentidad))
                throw new ArgumentException("El formato de la cédula del líder no es correcto.");
            if (!string.IsNullOrWhiteSpace(lider.Celular) && !ValidarFormatoTelefono(lider.Celular))
                throw new ArgumentException("El formato del celular del líder no es correcto.");
            if (!string.IsNullOrWhiteSpace(lider.Correo) && !ValidarFormatoCorreo(lider.Correo))
                throw new ArgumentException("El formato del correo del líder no es correcto.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar o Insertar Pastor
                        ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "Pastor", pastor);

                        // 2. Actualizar o Insertar Líder
                        ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "LiderMinisterial", lider);

                        // 3. Vincular al evento
                        string sqlLink = @"
                            IF NOT EXISTS (SELECT 1 FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart)
                            BEGIN
                                INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEvento, @IdPart, 0);
                            END";
                        using (SqlCommand cmdLink = new SqlCommand(sqlLink, cn, tran))
                        {
                            cmdLink.Parameters.AddWithValue("@IdEvento", idEventoVision);
                            cmdLink.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdLink.ExecuteNonQuery();
                        }

                        // 4. Actualizar etapa de la iglesia a 3 (Visión)
                        string sqlStage = "UPDATE dbo.ParticipacionesIglesia SET EtapaActual = 3 WHERE IdParticipacion = @IdPart;";
                        using (SqlCommand cmdStage = new SqlCommand(sqlStage, cn, tran))
                        {
                            cmdStage.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdStage.ExecuteNonQuery();
                        }

                        // 5. Registrar Historial
                        _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Asignación de Visión", "Evaluada Inicial (Etapa 2)", "Visión (Etapa 3)", idUsuario, "Se asignó el evento de Presentación de la Visión y se confirmaron/actualizaron los datos del Pastor y Líder.");

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

        private void ActualizarOInsertarPersonaInterno(SqlConnection cn, SqlTransaction tran, int idIglesia, string tipoPersona, PersonaIglesia persona)
        {
            string sqlCheck = "SELECT COUNT(1) FROM dbo.PersonasIglesia WHERE IdIglesia = @IdIglesia AND TipoPersona = @Tipo;";
            int count = 0;
            using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn, tran))
            {
                cmdCheck.Parameters.AddWithValue("@IdIglesia", idIglesia);
                cmdCheck.Parameters.AddWithValue("@Tipo", tipoPersona);
                count = Convert.ToInt32(cmdCheck.ExecuteScalar());
            }

            if (count > 0)
            {
                string sqlUpdate = @"
                    UPDATE dbo.PersonasIglesia SET
                        Nombres = @Nombres,
                        Apellidos = @Apellidos,
                        DocumentoIdentidad = @Doc,
                        Celular = @Celular,
                        Correo = @Correo
                    WHERE IdIglesia = @IdIglesia AND TipoPersona = @Tipo;";
                using (SqlCommand cmdUp = new SqlCommand(sqlUpdate, cn, tran))
                {
                    cmdUp.Parameters.AddWithValue("@Nombres", persona.Nombres ?? "");
                    cmdUp.Parameters.AddWithValue("@Apellidos", persona.Apellidos ?? "");
                    cmdUp.Parameters.AddWithValue("@Doc", persona.DocumentoIdentidad ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@Celular", persona.Celular ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@Correo", persona.Correo ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmdUp.Parameters.AddWithValue("@Tipo", tipoPersona);
                    cmdUp.ExecuteNonQuery();
                }
            }
            else
            {
                string sqlInsert = @"
                    INSERT INTO dbo.PersonasIglesia (IdIglesia, TipoPersona, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo)
                    VALUES (@IdIglesia, @Tipo, @Nombres, @Apellidos, @Doc, @Celular, @Correo);";
                using (SqlCommand cmdIns = new SqlCommand(sqlInsert, cn, tran))
                {
                    cmdIns.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmdIns.Parameters.AddWithValue("@Tipo", tipoPersona);
                    cmdIns.Parameters.AddWithValue("@Nombres", persona.Nombres ?? "");
                    cmdIns.Parameters.AddWithValue("@Apellidos", persona.Apellidos ?? "");
                    cmdIns.Parameters.AddWithValue("@Doc", persona.DocumentoIdentidad ?? (object)DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@Celular", persona.Celular ?? (object)DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@Correo", persona.Correo ?? (object)DBNull.Value);
                    cmdIns.ExecuteNonQuery();
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
                        using (SqlCommand cmdSp = new SqlCommand("dbo.SpAvanzarEtapaRecursos", cn, tran))
                        {
                            cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                            cmdSp.Parameters.AddWithValue("@IdParticipacion", idParticipacion);
                            cmdSp.Parameters.AddWithValue("@TallerNombre", tallerNombre);
                            cmdSp.Parameters.AddWithValue("@TallerFecha", (object)tallerFecha ?? DBNull.Value);
                            cmdSp.Parameters.AddWithValue("@TallerLugar", tallerLugar ?? (object)DBNull.Value);
                            cmdSp.Parameters.AddWithValue("@CantNinos", cantNinos);
                            cmdSp.Parameters.AddWithValue("@CantMaestrosReg", cantMaestrosReg);
                            cmdSp.Parameters.AddWithValue("@CantMaestrosAsist", cantMaestrosAsist);
                            cmdSp.Parameters.AddWithValue("@CantMaestrosAus", cantMaestrosAus);
                            cmdSp.Parameters.AddWithValue("@IdUsuarioResponsable", idUsuario);
                            cmdSp.ExecuteNonQuery();
                        }

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

        private static void ValidarReglasCastigoYAntiguedad(SqlConnection cnParam, SqlTransaction tranParam, string rncCedula, string pastorCedula, int idTemporadaDestino, List<string> outAdvertencias)
        {
            if (string.IsNullOrWhiteSpace(rncCedula)) return;

            SqlConnection cn = cnParam;
            SqlTransaction tran = tranParam;
            bool localConnection = false;

            if (cn == null)
            {
                cn = new SqlConnection(ObtenerCadenaConexion());
                cn.Open();
                localConnection = true;
            }

            try
            {
                int minAniosAntiguedad = 3;
                string sqlCfg = "SELECT Valor FROM dbo.ConfiguracionesSistema WHERE Clave = 'MinAniosAntiguedad';";
                using (SqlCommand cmdCfg = new SqlCommand(sqlCfg, cn, tran))
                {
                    object val = cmdCfg.ExecuteScalar();
                    if (val != null && int.TryParse(val.ToString(), out int parsed))
                    {
                        minAniosAntiguedad = parsed;
                    }
                }

                string sqlCheckAnt = @"
                    SELECT MAX(p.IdTemporada) 
                    FROM dbo.ParticipacionesIglesia p 
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia 
                    WHERE i.RNC_Cedula = @Rnc AND p.IdTemporada < @IdDest;";
                using (SqlCommand cmdAnt = new SqlCommand(sqlCheckAnt, cn, tran))
                {
                    cmdAnt.Parameters.AddWithValue("@Rnc", rncCedula.Trim());
                    cmdAnt.Parameters.AddWithValue("@IdDest", idTemporadaDestino);
                    object valAnt = cmdAnt.ExecuteScalar();
                    if (valAnt != null && valAnt != DBNull.Value)
                    {
                        int idTempPrev = Convert.ToInt32(valAnt);
                        if (idTemporadaDestino - idTempPrev < minAniosAntiguedad)
                        {
                            throw new InvalidOperationException($"La iglesia con RNC/Cédula '{rncCedula}' participó en una temporada reciente (ID {idTempPrev}). Se requiere una antigüedad mínima de {minAniosAntiguedad} temporadas para volver a participar.");
                        }
                    }
                }

                string sqlNoRep = @"
                    SELECT TOP 1 p.IdParticipacion, t.NombreTemporada, per.DocumentoIdentidad, i.NombreIglesia
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    LEFT JOIN dbo.PersonasIglesia per ON i.IdIglesia = per.IdIglesia AND per.TipoPersona = 'Pastor'
                    WHERE t.Activa = 0 
                      AND i.RNC_Cedula = @Rnc 
                      AND p.IdTemporada < @IdDest
                      AND (SELECT COUNT(1) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion) = 0
                    ORDER BY t.IdTemporada DESC;";

                using (SqlCommand cmdNoRep = new SqlCommand(sqlNoRep, cn, tran))
                {
                    cmdNoRep.Parameters.AddWithValue("@Rnc", rncCedula.Trim());
                    cmdNoRep.Parameters.AddWithValue("@IdDest", idTemporadaDestino);
                    using (SqlDataReader drNoRep = cmdNoRep.ExecuteReader())
                    {
                        if (drNoRep.Read())
                        {
                            string docPastorAnterior = drNoRep["DocumentoIdentidad"] != DBNull.Value ? drNoRep["DocumentoIdentidad"].ToString().Replace("-", "").Trim() : "";
                            string docPastorNuevo = pastorCedula?.Replace("-", "").Trim() ?? "";
                            string nTemp = drNoRep["NombreTemporada"].ToString();
                            string nIg = drNoRep["NombreIglesia"].ToString();

                            if (!string.IsNullOrEmpty(docPastorAnterior) && docPastorAnterior == docPastorNuevo)
                            {
                                throw new InvalidOperationException($"La iglesia '{nIg}' no reportó en la temporada '{nTemp}' con el mismo pastor, por lo que tiene prohibida su participación.");
                            }
                            else
                            {
                                if (outAdvertencias != null)
                                {
                                    outAdvertencias.Add($"ADVERTENCIA: La iglesia '{nIg}' no reportó en la temporada '{nTemp}' con su pastor anterior, pero se permite el registro al registrar un pastor diferente.");
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (localConnection && cn != null)
                {
                    cn.Close();
                    cn.Dispose();
                }
            }
        }

        public void ActualizarIglesia(Iglesia modelo, int idUsuarioEdicion)
        {
            _iglesiaRepository.ActualizarIglesia(modelo, idUsuarioEdicion);
        }
    }
}
