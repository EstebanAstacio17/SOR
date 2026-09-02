using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;
using SOR.Helpers;

namespace SOR.Repositories
{
    public class IglesiaRepository : BaseRepository
    {
        public List<Iglesia> ObtenerIglesias()
        {
            List<Iglesia> lista = new List<Iglesia>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT i.*, e.NombreEquipo,
                           p.IdParticipacion, p.EstadoEvaluacion, p.EstatusEvaluacionReporte, p.EtapaActual, t.NombreTemporada
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    LEFT JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                        AND p.IdTemporada = (SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC)
                    LEFT JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    ORDER BY i.NombreIglesia;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var ig = new Iglesia
                        {
                            IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                            NombreIglesia = dr["NombreIglesia"].ToString(),
                            RNC_Cedula = dr["RNC_Cedula"] != DBNull.Value ? dr["RNC_Cedula"].ToString() : "",
                            Telefono = dr["Telefono"] != DBNull.Value ? dr["Telefono"].ToString() : "",
                            CorreoInstitucion = dr["CorreoInstitucion"] != DBNull.Value ? dr["CorreoInstitucion"].ToString() : "",
                            Calle = dr["Calle"] != DBNull.Value ? dr["Calle"].ToString() : "",
                            Numero = dr["Numero"] != DBNull.Value ? dr["Numero"].ToString() : "",
                            Sector = dr["Sector"] != DBNull.Value ? dr["Sector"].ToString() : "",
                            Ciudad = dr["Ciudad"] != DBNull.Value ? dr["Ciudad"].ToString() : "",
                            Provincia = dr["Provincia"] != DBNull.Value ? dr["Provincia"].ToString() : "",
                            Referencia = dr["Referencia"] != DBNull.Value ? dr["Referencia"].ToString() : "",
                            IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                            NombreEquipo = dr["NombreEquipo"].ToString(),
                            FechaCreacion = dr["FechaCreacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaCreacion"]) : null,

                            // Nuevos campos
                            Denominacion = dr["Denominacion"] != DBNull.Value ? dr["Denominacion"].ToString() : "",
                            TipoOrganizacion = dr["TipoOrganizacion"] != DBNull.Value ? dr["TipoOrganizacion"].ToString() : "",
                            CantidadMaestros = dr["CantidadMaestros"] != DBNull.Value ? (int?)Convert.ToInt32(dr["CantidadMaestros"]) : null,
                            CantidadNinos = dr["CantidadNinos"] != DBNull.Value ? (int?)Convert.ToInt32(dr["CantidadNinos"]) : null,
                            Ref1Nombre = dr["Ref1Nombre"] != DBNull.Value ? dr["Ref1Nombre"].ToString() : "",
                            Ref1Contacto = dr["Ref1Contacto"] != DBNull.Value ? dr["Ref1Contacto"].ToString() : "",
                            Ref2Nombre = dr["Ref2Nombre"] != DBNull.Value ? dr["Ref2Nombre"].ToString() : "",
                            Ref2Contacto = dr["Ref2Contacto"] != DBNull.Value ? dr["Ref2Contacto"].ToString() : ""
                        };

                        if (dr["IdParticipacion"] != DBNull.Value)
                        {
                            ig.ParticipacionActual = new ParticipacionIglesia
                            {
                                IdParticipacion = Convert.ToInt32(dr["IdParticipacion"]),
                                IdIglesia = ig.IdIglesia,
                                EstadoEvaluacion = dr["EstadoEvaluacion"].ToString(),
                                EstatusEvaluacionReporte = dr["EstatusEvaluacionReporte"] != DBNull.Value ? dr["EstatusEvaluacionReporte"].ToString() : "Pendiente",
                                EtapaActual = Convert.ToInt32(dr["EtapaActual"]),
                                NombreTemporada = dr["NombreTemporada"].ToString()
                            };
                        }

                        lista.Add(ig);
                    }
                }
            }

            return lista;
        }

        public int RegistrarIglesia(Iglesia modelo, int idUsuarioCreacion, int? idTemporadaDestinoParam = null)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insertar Iglesia
                        string sqlIglesia = @"
                            INSERT INTO dbo.Iglesias (
                                NombreIglesia, RNC_Cedula, Telefono, CorreoInstitucion, Calle, Numero, Sector, Ciudad, Provincia, Referencia, IdEquipo, IdUsuarioCreacion,
                                Denominacion, TipoOrganizacion, CantidadMaestros, CantidadNinos, Ref1Nombre, Ref1Contacto, Ref2Nombre, Ref2Contacto
                            ) VALUES (
                                @NombreIglesia, @RNC_Cedula, @Telefono, @CorreoInstitucion, @Calle, @Numero, @Sector, @Ciudad, @Provincia, @Referencia, @IdEquipo, @IdUsuarioCreacion,
                                @Denominacion, @TipoOrganizacion, @CantidadMaestros, @CantidadNinos, @Ref1Nombre, @Ref1Contacto, @Ref2Nombre, @Ref2Contacto
                            );
                            SELECT SCOPE_IDENTITY();";

                        SqlCommand cmdIg = new SqlCommand(sqlIglesia, cn, tran);
                        cmdIg.Parameters.AddWithValue("@NombreIglesia", modelo.NombreIglesia);
                        cmdIg.Parameters.AddWithValue("@RNC_Cedula", modelo.RNC_Cedula ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Telefono", modelo.Telefono ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@CorreoInstitucion", modelo.CorreoInstitucion ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Calle", modelo.Calle ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Numero", modelo.Numero ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Sector", modelo.Sector ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Ciudad", modelo.Ciudad ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Provincia", modelo.Provincia ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Referencia", modelo.Referencia ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@IdEquipo", modelo.IdEquipo);
                        cmdIg.Parameters.AddWithValue("@IdUsuarioCreacion", idUsuarioCreacion);

                        cmdIg.Parameters.AddWithValue("@Denominacion", modelo.Denominacion ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@TipoOrganizacion", modelo.TipoOrganizacion ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@CantidadMaestros", modelo.CantidadMaestros ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@CantidadNinos", modelo.CantidadNinos ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Ref1Nombre", modelo.Ref1Nombre ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Ref1Contacto", modelo.Ref1Contacto ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Ref2Nombre", modelo.Ref2Nombre ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Ref2Contacto", modelo.Ref2Contacto ?? (object)DBNull.Value);

                        int idIglesiaNew = Convert.ToInt32(cmdIg.ExecuteScalar());

                        // 2. Insertar Pastor
                        if (modelo.Pastor != null && !string.IsNullOrEmpty(modelo.Pastor.Nombres))
                        {
                            InsertarPersona(cn, tran, idIglesiaNew, "Pastor", modelo.Pastor);
                        }

                        // 3. Insertar Líder Ministerial
                        if (modelo.LiderMinisterial != null && !string.IsNullOrEmpty(modelo.LiderMinisterial.Nombres))
                        {
                            InsertarPersona(cn, tran, idIglesiaNew, "LiderMinisterial", modelo.LiderMinisterial);
                        }

                        // 4. Crear Participación Inicial en la Temporada
                        int idTemporadaDestino = 0;
                        if (idTemporadaDestinoParam.HasValue && idTemporadaDestinoParam.Value > 0)
                        {
                            idTemporadaDestino = idTemporadaDestinoParam.Value;
                        }
                        else
                        {
                            string sqlTemp = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY FechaInicio DESC;";
                            SqlCommand cmdTemp = new SqlCommand(sqlTemp, cn, tran);
                            object idTempObj = cmdTemp.ExecuteScalar();
                            if (idTempObj == null)
                            {
                                throw new Exception("No hay temporadas configuradas en el sistema. Contacta a un Administrador.");
                            }
                            idTemporadaDestino = Convert.ToInt32(idTempObj);
                        }

                        string estatusReporte = (modelo.ParticipacionActual != null && !string.IsNullOrEmpty(modelo.ParticipacionActual.EstatusEvaluacionReporte)) 
                                                ? modelo.ParticipacionActual.EstatusEvaluacionReporte 
                                                : "Pendiente";

                        string sqlPart = @"
                            INSERT INTO dbo.ParticipacionesIglesia (IdIglesia, IdTemporada, Participara, EstadoEvaluacion, EstatusEvaluacionReporte, EtapaActual)
                            VALUES (@IdIglesia, @IdTemporada, 1, 'Pendiente', @EstatusReporte, 1);
                            SELECT SCOPE_IDENTITY();";

                        SqlCommand cmdPart = new SqlCommand(sqlPart, cn, tran);
                        cmdPart.Parameters.AddWithValue("@IdIglesia", idIglesiaNew);
                        cmdPart.Parameters.AddWithValue("@IdTemporada", idTemporadaDestino);
                        cmdPart.Parameters.AddWithValue("@EstatusReporte", estatusReporte);
                        int idParticipacionNew = Convert.ToInt32(cmdPart.ExecuteScalar());

                        // Crear registro inicial de asignación de recursos despachados
                        string sqlRec = "INSERT INTO dbo.AsignacionesRecursos (IdParticipacion) VALUES (@IdParticipacion);";
                        SqlCommand cmdRec = new SqlCommand(sqlRec, cn, tran);
                        cmdRec.Parameters.AddWithValue("@IdParticipacion", idParticipacionNew);
                        cmdRec.ExecuteNonQuery();

                        // Registrar Historial de Inscripción (Etapa 1)
                        RegistrarLogHistorial(cn, tran, idParticipacionNew, "Inscripción en Temporada", null, "Inscrita (Etapa 1)", idUsuarioCreacion, "Iglesia inscrita exitosamente en la temporada activa.");

                        tran.Commit();
                        return idIglesiaNew;
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public Iglesia ObtenerExpedienteIglesia(int idIglesia)
        {
            Iglesia ig = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // 1. Datos Principales de Iglesia
                string sqlIg = "SELECT i.*, e.NombreEquipo FROM dbo.Iglesias i INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo WHERE i.IdIglesia = @Id;";
                using (SqlCommand cmd = new SqlCommand(sqlIg, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idIglesia);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ig = new Iglesia
                            {
                                IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                                NombreIglesia = dr["NombreIglesia"].ToString(),
                                RNC_Cedula = dr["RNC_Cedula"] != DBNull.Value ? dr["RNC_Cedula"].ToString() : "",
                                Telefono = dr["Telefono"] != DBNull.Value ? dr["Telefono"].ToString() : "",
                                CorreoInstitucion = dr["CorreoInstitucion"] != DBNull.Value ? dr["CorreoInstitucion"].ToString() : "",
                                Calle = dr["Calle"] != DBNull.Value ? dr["Calle"].ToString() : "",
                                Numero = dr["Numero"] != DBNull.Value ? dr["Numero"].ToString() : "",
                                Sector = dr["Sector"] != DBNull.Value ? dr["Sector"].ToString() : "",
                                Ciudad = dr["Ciudad"] != DBNull.Value ? dr["Ciudad"].ToString() : "",
                                Provincia = dr["Provincia"] != DBNull.Value ? dr["Provincia"].ToString() : "",
                                Referencia = dr["Referencia"] != DBNull.Value ? dr["Referencia"].ToString() : "",
                                IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                                NombreEquipo = dr["NombreEquipo"].ToString(),
                                FechaCreacion = dr["FechaCreacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaCreacion"]) : null,

                                Denominacion = dr["Denominacion"] != DBNull.Value ? dr["Denominacion"].ToString() : "",
                                TipoOrganizacion = dr["TipoOrganizacion"] != DBNull.Value ? dr["TipoOrganizacion"].ToString() : "",
                                CantidadMaestros = dr["CantidadMaestros"] != DBNull.Value ? (int?)Convert.ToInt32(dr["CantidadMaestros"]) : null,
                                CantidadNinos = dr["CantidadNinos"] != DBNull.Value ? (int?)Convert.ToInt32(dr["CantidadNinos"]) : null,
                                Ref1Nombre = dr["Ref1Nombre"] != DBNull.Value ? dr["Ref1Nombre"].ToString() : "",
                                Ref1Contacto = dr["Ref1Contacto"] != DBNull.Value ? dr["Ref1Contacto"].ToString() : "",
                                Ref2Nombre = dr["Ref2Nombre"] != DBNull.Value ? dr["Ref2Nombre"].ToString() : "",
                                Ref2Contacto = dr["Ref2Contacto"] != DBNull.Value ? dr["Ref2Contacto"].ToString() : "",

                                RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null,
                                FechaModificacion = dr.TableHasColumn("FechaModificacion") && dr["FechaModificacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaModificacion"]) : null,
                                UsuarioModificacion = dr.TableHasColumn("UsuarioModificacion") && dr["UsuarioModificacion"] != DBNull.Value ? (int?)Convert.ToInt32(dr["UsuarioModificacion"]) : null
                            };
                        }
                    }
                }

                if (ig == null) return null;

                // 2. Personas (Pastor, Líder)
                string sqlPer = "SELECT * FROM dbo.PersonasIglesia WHERE IdIglesia = @Id;";
                using (SqlCommand cmdPer = new SqlCommand(sqlPer, cn))
                {
                    cmdPer.Parameters.AddWithValue("@Id", idIglesia);
                    using (SqlDataReader drP = cmdPer.ExecuteReader())
                    {
                        while (drP.Read())
                        {
                            var per = new PersonaIglesia
                            {
                                IdPersonaIglesia = Convert.ToInt32(drP["IdPersonaIglesia"]),
                                IdIglesia = Convert.ToInt32(drP["IdIglesia"]),
                                TipoPersona = drP["TipoPersona"].ToString(),
                                Nombres = drP["Nombres"].ToString(),
                                Apellidos = drP["Apellidos"].ToString(),
                                DocumentoIdentidad = drP["DocumentoIdentidad"] != DBNull.Value ? drP["DocumentoIdentidad"].ToString() : "",
                                DocumentoAdjuntoRuta = drP["DocumentoAdjuntoRuta"] != DBNull.Value ? drP["DocumentoAdjuntoRuta"].ToString() : "",
                                Celular = drP["Celular"] != DBNull.Value ? drP["Celular"].ToString() : "",
                                Correo = drP["Correo"] != DBNull.Value ? drP["Correo"].ToString() : ""
                            };

                            if (per.TipoPersona == "Pastor") ig.Pastor = per;
                            else if (per.TipoPersona == "LiderMinisterial") ig.LiderMinisterial = per;
                        }
                    }
                }

                // 2.2 Cargar Maestros independientes de la Iglesia
                string sqlMaestros = "SELECT * FROM dbo.Maestros WHERE IdIglesia = @Id;";
                using (SqlCommand cmdM = new SqlCommand(sqlMaestros, cn))
                {
                    cmdM.Parameters.AddWithValue("@Id", idIglesia);
                    using (SqlDataReader drM = cmdM.ExecuteReader())
                    {
                        while (drM.Read())
                        {
                            ig.Maestros.Add(new Maestro
                            {
                                IdMaestro = Convert.ToInt32(drM["IdMaestro"]),
                                IdIglesia = idIglesia,
                                Nombres = drM["Nombres"].ToString(),
                                Apellidos = drM["Apellidos"].ToString(),
                                DocumentoIdentidad = drM["DocumentoIdentidad"] != DBNull.Value ? drM["DocumentoIdentidad"].ToString() : "",
                                Celular = drM["Celular"] != DBNull.Value ? drM["Celular"].ToString() : "",
                                Correo = drM["Correo"] != DBNull.Value ? drM["Correo"].ToString() : "",
                                Activo = Convert.ToBoolean(drM["Activo"])
                            });
                        }
                    }
                }

                // 3. Participación y Recursos Actuales con soporte de etapas de la temporada
                string sqlPart = @"
                    SELECT p.*, t.NombreTemporada, t.Activa AS TemporadaActiva, r.* 
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    LEFT JOIN dbo.AsignacionesRecursos r ON p.IdParticipacion = r.IdParticipacion
                    WHERE p.IdIglesia = @Id
                      AND p.IdTemporada = (SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC);";

                using (SqlCommand cmdPart = new SqlCommand(sqlPart, cn))
                {
                    cmdPart.Parameters.AddWithValue("@Id", idIglesia);
                    using (SqlDataReader drPart = cmdPart.ExecuteReader())
                    {
                        if (drPart.Read())
                        {
                            ig.ParticipacionActual = new ParticipacionIglesia
                            {
                                IdParticipacion = Convert.ToInt32(drPart["IdParticipacion"]),
                                IdIglesia = Convert.ToInt32(drPart["IdIglesia"]),
                                IdTemporada = Convert.ToInt32(drPart["IdTemporada"]),
                                NombreTemporada = drPart["NombreTemporada"].ToString(),
                                TemporadaActiva = drPart["TemporadaActiva"] != DBNull.Value && Convert.ToBoolean(drPart["TemporadaActiva"]),
                                Participara = Convert.ToBoolean(drPart["Participara"]),
                                JustificacionNoParticipacion = drPart["JustificacionNoParticipacion"] != DBNull.Value ? drPart["JustificacionNoParticipacion"].ToString() : "",
                                EstadoEvaluacion = drPart["EstadoEvaluacion"].ToString(),
                                EstatusEvaluacionReporte = drPart["EstatusEvaluacionReporte"] != DBNull.Value ? drPart["EstatusEvaluacionReporte"].ToString() : "Pendiente",

                                EtapaActual = Convert.ToInt32(drPart["EtapaActual"]),
                                EvalInicialEstado = drPart["EvalInicialEstado"].ToString(),
                                EvalInicialMotivo = drPart["EvalInicialMotivo"] != DBNull.Value ? drPart["EvalInicialMotivo"].ToString() : "",
                                EvalInicialIdUsuario = drPart["EvalInicialIdUsuario"] != DBNull.Value ? (int?)Convert.ToInt32(drPart["EvalInicialIdUsuario"]) : null,
                                EvalInicialFecha = drPart["EvalInicialFecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(drPart["EvalInicialFecha"]) : null,
                                EvalInicialComentario = drPart["EvalInicialComentario"] != DBNull.Value ? drPart["EvalInicialComentario"].ToString() : "",

                                VisionInvitada = Convert.ToBoolean(drPart["VisionInvitada"]),
                                VisionFecha = drPart["VisionFecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(drPart["VisionFecha"]) : null,
                                VisionLugar = drPart["VisionLugar"] != DBNull.Value ? drPart["VisionLugar"].ToString() : "",
                                VisionAsistio = Convert.ToBoolean(drPart["VisionAsistio"]),
                                VisionResultado = drPart["VisionResultado"] != DBNull.Value ? drPart["VisionResultado"].ToString() : "",

                                EvalTallerEstado = drPart["EvalTallerEstado"].ToString(),
                                EvalTallerMotivo = drPart["EvalTallerMotivo"] != DBNull.Value ? drPart["EvalTallerMotivo"].ToString() : "",
                                EvalTallerIdUsuario = drPart["EvalTallerIdUsuario"] != DBNull.Value ? (int?)Convert.ToInt32(drPart["EvalTallerIdUsuario"]) : null,
                                EvalTallerFecha = drPart["EvalTallerFecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(drPart["EvalTallerFecha"]) : null,
                                EvalTallerComentario = drPart["EvalTallerComentario"] != DBNull.Value ? drPart["EvalTallerComentario"].ToString() : "",

                                TallerParticipo = Convert.ToBoolean(drPart["TallerParticipo"]),
                                TallerNombre = drPart["TallerNombre"] != DBNull.Value ? drPart["TallerNombre"].ToString() : "",
                                TallerFecha = drPart["TallerFecha"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(drPart["TallerFecha"]) : null,
                                TallerLugar = drPart["TallerLugar"] != DBNull.Value ? drPart["TallerLugar"].ToString() : "",
                                TallerCantNinos = Convert.ToInt32(drPart["TallerCantNinos"]),
                                TallerCantMaestrosReg = Convert.ToInt32(drPart["TallerCantMaestrosReg"]),
                                TallerCantMaestrosAsist = Convert.ToInt32(drPart["TallerCantMaestrosAsist"]),
                                TallerCantMaestrosAus = Convert.ToInt32(drPart["TallerCantMaestrosAus"])
                            };

                            ig.RecursosActuales = new AsignacionRecursos
                            {
                                IdAsignacionRecurso = drPart["IdAsignacionRecurso"] != DBNull.Value ? Convert.ToInt32(drPart["IdAsignacionRecurso"]) : 0,
                                IdParticipacion = ig.ParticipacionActual.IdParticipacion,
                                OportunidadesEvangelisticas = drPart["OportunidadesEvangelisticas"] != DBNull.Value ? Convert.ToInt32(drPart["OportunidadesEvangelisticas"]) : 0,
                                LibrosMejorRegalo = drPart["LibrosMejorRegalo"] != DBNull.Value ? Convert.ToInt32(drPart["LibrosMejorRegalo"]) : 0,
                                LibrosMaestros = drPart["LibrosMaestros"] != DBNull.Value ? Convert.ToInt32(drPart["LibrosMaestros"]) : 0,
                                LibrosAlumno = drPart["LibrosAlumno"] != DBNull.Value ? Convert.ToInt32(drPart["LibrosAlumno"]) : 0,
                                Posters = drPart["Posters"] != DBNull.Value ? Convert.ToInt32(drPart["Posters"]) : 0,
                                NuevosTestamentos = drPart["NuevosTestamentos"] != DBNull.Value ? Convert.ToInt32(drPart["NuevosTestamentos"]) : 0,
                                EstadoAsignacion = drPart["EstadoAsignacion"] != DBNull.Value ? drPart["EstadoAsignacion"].ToString() : "ASIGNADO",
                                FechaDisponibleDespacho = drPart["FechaDisponibleDespacho"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(drPart["FechaDisponibleDespacho"]) : null,
                                IdEventoDespachoActual = drPart["IdEventoDespachoActual"] != DBNull.Value ? (int?)Convert.ToInt32(drPart["IdEventoDespachoActual"]) : null
                            };
                        }
                    }
                }

                // 3.2 Cargar Compañeros de Oración en la temporada activa
                if (ig.ParticipacionActual != null)
                {
                    string sqlOracion = "SELECT c.*, us.Correo AS CorreoRegistrador FROM dbo.CompanerosOracion c INNER JOIN dbo.Usuarios us ON c.IdUsuarioRegistro = us.IdUsuario WHERE c.IdIglesia = @Id AND c.IdTemporada = @IdTemp;";
                    using (SqlCommand cmdOr = new SqlCommand(sqlOracion, cn))
                    {
                        cmdOr.Parameters.AddWithValue("@Id", idIglesia);
                        cmdOr.Parameters.AddWithValue("@IdTemp", ig.ParticipacionActual.IdTemporada);
                        using (SqlDataReader drOr = cmdOr.ExecuteReader())
                        {
                            while (drOr.Read())
                            {
                                ig.CompanerosOracion.Add(new CompaneroOracion
                                {
                                    IdCompanero = Convert.ToInt32(drOr["IdCompanero"]),
                                    NombreCompleto = drOr["NombreCompleto"].ToString(),
                                    ContactoWhatsApp = drOr["ContactoWhatsApp"] != DBNull.Value ? drOr["ContactoWhatsApp"].ToString() : "",
                                    EsMayorEdad = Convert.ToBoolean(drOr["EsMayorEdad"]),
                                    IdIglesia = idIglesia,
                                    IdTemporada = ig.ParticipacionActual.IdTemporada,
                                    IdUsuarioRegistro = Convert.ToInt32(drOr["IdUsuarioRegistro"]),
                                    CorreoRegistrador = drOr["CorreoRegistrador"].ToString(),
                                    FechaRegistro = Convert.ToDateTime(drOr["FechaRegistro"])
                                });
                            }
                        }
                    }

                    // 3.3 Cargar Historial / Timeline de participación de esta temporada
                    string sqlHist = @"
                        SELECT h.*, us.Correo AS NombreResponsable,
                               COALESCE(LTRIM(RTRIM(ISNULL(pc.PrimerNombre, '') + ' ' + ISNULL(pc.PrimerApellido, ''))), us.Correo) AS NombreCoordinador,
                               COALESCE(pos.NombrePosicion, 'Sin Posición') AS PosicionCoordinador,
                               COALESCE(eq.NombreEquipo, 'Sin Equipo') AS EquipoCoordinador,
                               t.NombreTemporada
                        FROM dbo.HistorialParticipacion h 
                        INNER JOIN dbo.Usuarios us ON h.IdUsuarioResponsable = us.IdUsuario 
                        INNER JOIN dbo.ParticipacionesIglesia part ON h.IdParticipacion = part.IdParticipacion
                        LEFT JOIN dbo.Temporadas t ON part.IdTemporada = t.IdTemporada
                        LEFT JOIN dbo.PerfilesCoordinador pc ON us.IdUsuario = pc.IdUsuario
                        LEFT JOIN dbo.AsignacionesEquipo ae ON us.IdUsuario = ae.IdUsuario AND ae.Activo = 1
                        LEFT JOIN dbo.Equipos eq ON ae.IdEquipo = eq.IdEquipo
                        LEFT JOIN dbo.PosicionesOCC pos ON ae.IdPosicion = pos.IdPosicion
                        WHERE h.IdParticipacion = @IdPart 
                        ORDER BY h.FechaHora DESC;";
                    using (SqlCommand cmdH = new SqlCommand(sqlHist, cn))
                    {
                        cmdH.Parameters.AddWithValue("@IdPart", ig.ParticipacionActual.IdParticipacion);
                        using (SqlDataReader drH = cmdH.ExecuteReader())
                        {
                            while (drH.Read())
                            {
                                ig.Historial.Add(new HistorialParticipacion
                                {
                                    IdHistorial = Convert.ToInt32(drH["IdHistorial"]),
                                    IdParticipacion = ig.ParticipacionActual.IdParticipacion,
                                    FechaHora = Convert.ToDateTime(drH["FechaHora"]),
                                    AccionRealizada = drH["AccionRealizada"].ToString(),
                                    EstadoAnterior = drH["EstadoAnterior"] != DBNull.Value ? drH["EstadoAnterior"].ToString() : "",
                                    EstadoNuevo = drH["EstadoNuevo"] != DBNull.Value ? drH["EstadoNuevo"].ToString() : "",
                                    IdUsuarioResponsable = Convert.ToInt32(drH["IdUsuarioResponsable"]),
                                    NombreResponsable = drH["NombreResponsable"].ToString(),
                                    NombreCoordinador = drH["NombreCoordinador"].ToString(),
                                    PosicionCoordinador = drH["PosicionCoordinador"].ToString(),
                                    EquipoCoordinador = drH["EquipoCoordinador"].ToString(),
                                    NombreTemporada = drH["NombreTemporada"] != DBNull.Value ? drH["NombreTemporada"].ToString() : "",
                                    Comentario = drH["Comentario"] != DBNull.Value ? drH["Comentario"].ToString() : "",
                                    Razon = drH["Razon"] != DBNull.Value ? drH["Razon"].ToString() : ""
                                });
                            }
                        }
                    }
                }

                // 4. Comentarios Históricos
                string sqlCom = @"
                    SELECT c.*, u.Correo,
                           COALESCE(LTRIM(RTRIM(ISNULL(pc.PrimerNombre, '') + ' ' + ISNULL(pc.PrimerApellido, ''))), u.Correo) AS NombreCoordinador,
                           COALESCE(pos.NombrePosicion, 'Sin Posición') AS PosicionCoordinador,
                           COALESCE(eq.NombreEquipo, 'Sin Equipo') AS EquipoCoordinador,
                           (SELECT TOP 1 NombreTemporada FROM dbo.Temporadas ORDER BY FechaInicio DESC) AS NombreTemporada
                    FROM dbo.ComentariosObservaciones c
                    INNER JOIN dbo.Usuarios u ON c.IdUsuario = u.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador pc ON u.IdUsuario = pc.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo ae ON u.IdUsuario = ae.IdUsuario AND ae.Activo = 1
                    LEFT JOIN dbo.Equipos eq ON ae.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.PosicionesOCC pos ON ae.IdPosicion = pos.IdPosicion
                    WHERE c.IdIglesia = @Id
                    ORDER BY c.FechaCreacion DESC;";
                using (SqlCommand cmdCom = new SqlCommand(sqlCom, cn))
                {
                    cmdCom.Parameters.AddWithValue("@Id", idIglesia);
                    using (SqlDataReader drC = cmdCom.ExecuteReader())
                    {
                        while (drC.Read())
                        {
                            ig.Comentarios.Add(new ComentarioIglesia
                            {
                                IdComentario = Convert.ToInt32(drC["IdComentario"]),
                                IdIglesia = Convert.ToInt32(drC["IdIglesia"]),
                                IdUsuario = Convert.ToInt32(drC["IdUsuario"]),
                                CorreoUsuario = drC["Correo"].ToString(),
                                Comentario = drC["Comentario"].ToString(),
                                FechaCreacion = Convert.ToDateTime(drC["FechaCreacion"]),
                                NombreCoordinador = drC["NombreCoordinador"].ToString(),
                                PosicionCoordinador = drC["PosicionCoordinador"].ToString(),
                                EquipoCoordinador = drC["EquipoCoordinador"].ToString(),
                                NombreTemporada = drC["NombreTemporada"] != DBNull.Value ? drC["NombreTemporada"].ToString() : ""
                            });
                        }
                    }
                }

                // 5. Cargar regla de 3 años, desempeño previo y excepciones
                if (ig.ParticipacionActual != null)
                {
                    int idTemporadaActiva = ig.ParticipacionActual.IdTemporada;
                    int minAnios = 3;
                    string sqlCfg = "SELECT Valor FROM dbo.ConfiguracionesSistema WHERE Clave = 'MinAniosAntiguedad';";
                    using (SqlCommand cmdCfg = new SqlCommand(sqlCfg, cn))
                    {
                        object val = cmdCfg.ExecuteScalar();
                        if (val != null && int.TryParse(val.ToString(), out int p)) minAnios = p;
                    }

                    string sqlAnt = @"
                        SELECT TOP 1 p.IdTemporada, t.NombreTemporada
                        FROM dbo.ParticipacionesIglesia p
                        INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                        INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                        WHERE (i.IdIglesia = @Id OR (i.RNC_Cedula IS NOT NULL AND i.RNC_Cedula <> '' AND i.RNC_Cedula = @Rnc))
                          AND p.IdTemporada < @IdTemp
                        ORDER BY p.IdTemporada DESC;";

                    using (SqlCommand cmdAnt = new SqlCommand(sqlAnt, cn))
                    {
                        cmdAnt.Parameters.AddWithValue("@Id", idIglesia);
                        cmdAnt.Parameters.AddWithValue("@Rnc", ig.RNC_Cedula ?? "");
                        cmdAnt.Parameters.AddWithValue("@IdTemp", idTemporadaActiva);
                        using (SqlDataReader drAnt = cmdAnt.ExecuteReader())
                        {
                            if (drAnt.Read())
                            {
                                int idPrev = Convert.ToInt32(drAnt["IdTemporada"]);
                                string nomPrev = drAnt["NombreTemporada"].ToString();
                                int diff = idTemporadaActiva - idPrev;

                                if (diff < minAnios)
                                {
                                    ig.RequiereExcepcion3Anios = true;
                                    ig.DiferenciaAniosAntiguedad = diff;
                                    ig.IdTemporadaPrevia = idPrev;
                                    ig.NombreTemporadaPrevia = nomPrev;
                                }
                            }
                        }
                    }

                    // Cargar Excepción Activa si existe
                    ig.ExcepcionActiva = ObtenerExcepcionActivaInterno(cn, idIglesia, idTemporadaActiva);

                    // Cargar Historial completo de Excepciones
                    ig.HistorialExcepciones = ObtenerHistorialExcepcionesInterno(cn, idIglesia);

                    // Si requiere excepción o tiene historial, calculamos el snapshot de desempeño histórico
                    if (ig.RequiereExcepcion3Anios && ig.IdTemporadaPrevia > 0)
                    {
                        ig.DesempenoHistorico = CalcularDesempenoHistoricoInterno(cn, idIglesia, ig.IdTemporadaPrevia, idTemporadaActiva);
                    }
                }
            }

            return ig;
        }

        public void EvaluarParticipacion(int idParticipacion, bool participara, string justificacion, string estadoEvaluacion, int idEvaluador)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    UPDATE dbo.ParticipacionesIglesia SET
                        Participara = @Participara,
                        JustificacionNoParticipacion = @Justificacion,
                        EstadoEvaluacion = @EstadoEvaluacion,
                        IdUsuarioEvaluador = @IdEvaluador,
                        FechaEvaluacion = GETDATE()
                    WHERE IdParticipacion = @IdParticipacion;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Participara", participara);
                cmd.Parameters.AddWithValue("@Justificacion", justificacion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EstadoEvaluacion", estadoEvaluacion ?? "Pendiente");
                cmd.Parameters.AddWithValue("@IdEvaluador", idEvaluador);
                cmd.Parameters.AddWithValue("@IdParticipacion", idParticipacion);

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DespacharRecursos(AsignacionRecursos modelo, int idDespachador)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    UPDATE dbo.AsignacionesRecursos SET
                        OportunidadesEvangelisticas = @Oportunidades,
                        LibrosMejorRegalo = @Regalo,
                        LibrosMaestros = @Maestros,
                        LibrosAlumno = @Alumno,
                        Posters = @Posters,
                        NuevosTestamentos = @Testamentos,
                        FechaDespacho = GETDATE(),
                        IdUsuarioDespacho = @IdDespacho
                    WHERE IdParticipacion = @IdParticipacion;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Oportunidades", modelo.OportunidadesEvangelisticas);
                cmd.Parameters.AddWithValue("@Regalo", modelo.LibrosMejorRegalo);
                cmd.Parameters.AddWithValue("@Maestros", modelo.LibrosMaestros);
                cmd.Parameters.AddWithValue("@Alumno", modelo.LibrosAlumno);
                cmd.Parameters.AddWithValue("@Posters", modelo.Posters);
                cmd.Parameters.AddWithValue("@Testamentos", modelo.NuevosTestamentos);
                cmd.Parameters.AddWithValue("@IdDespacho", idDespachador);
                cmd.Parameters.AddWithValue("@IdParticipacion", modelo.IdParticipacion);

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void AgregarComentario(int idIglesia, int idUsuario, string comentario)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.Parameters.AddWithValue("@Comentario", comentario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void RegistrarLogHistorial(SqlConnection cn, SqlTransaction tran, int idParticipacion, string accion, string anterior, string nuevo, int idUsuario, string comentario, string razon = null)
        {
            string sql = @"
                INSERT INTO dbo.HistorialParticipacion (IdParticipacion, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario, Razon)
                VALUES (@IdPart, @Accion, @Ant, @Nue, @IdUsuario, @Com, @Raz);";

            SqlCommand cmd = new SqlCommand(sql, cn, tran);
            cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
            cmd.Parameters.AddWithValue("@Accion", accion);
            cmd.Parameters.AddWithValue("@Ant", anterior ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nue", nuevo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@Com", comentario ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Raz", razon ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        private void InsertarPersona(SqlConnection cn, SqlTransaction tran, int idIglesia, string tipoPersona, PersonaIglesia persona)
        {
            string sql = @"
                INSERT INTO dbo.PersonasIglesia (IdIglesia, TipoPersona, Nombres, Apellidos, DocumentoIdentidad, DocumentoAdjuntoRuta, Celular, Correo, Calle, Numero, Sector, Referencia)
                VALUES (@IdIglesia, @TipoPersona, @Nombres, @Apellidos, @DocumentoIdentidad, @DocumentoAdjuntoRuta, @Celular, @Correo, @Calle, @Numero, @Sector, @Referencia);";

            SqlCommand cmd = new SqlCommand(sql, cn, tran);
            cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
            cmd.Parameters.AddWithValue("@TipoPersona", tipoPersona);
            cmd.Parameters.AddWithValue("@Nombres", persona.Nombres ?? "");
            cmd.Parameters.AddWithValue("@Apellidos", persona.Apellidos ?? "");
            cmd.Parameters.AddWithValue("@DocumentoIdentidad", persona.DocumentoIdentidad ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentoAdjuntoRuta", persona.DocumentoAdjuntoRuta ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Celular", persona.Celular ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Correo", persona.Correo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Calle", persona.Calle ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Numero", persona.Numero ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Sector", persona.Sector ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Referencia", persona.Referencia ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void ActualizarIglesia(Iglesia modelo, int idUsuarioEdicion)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar Iglesia con control de concurrencia optimista
                        string sqlIglesia = @"
                            UPDATE dbo.Iglesias SET
                                NombreIglesia = @NombreIglesia,
                                RNC_Cedula = @RNC_Cedula,
                                Telefono = @Telefono,
                                CorreoInstitucion = @CorreoInstitucion,
                                Calle = @Calle,
                                Numero = @Numero,
                                Sector = @Sector,
                                Ciudad = @Ciudad,
                                Provincia = @Provincia,
                                Referencia = @Referencia,
                                IdEquipo = @IdEquipo,
                                Denominacion = @Denominacion,
                                TipoOrganizacion = @TipoOrganizacion,
                                CantidadMaestros = @CantidadMaestros,
                                CantidadNinos = @CantidadNinos,
                                Ref1Nombre = @Ref1Nombre,
                                Ref1Contacto = @Ref1Contacto,
                                Ref2Nombre = @Ref2Nombre,
                                Ref2Contacto = @Ref2Contacto,
                                FechaModificacion = GETUTCDATE(),
                                UsuarioModificacion = @IdUsuarioEdicion
                            WHERE IdIglesia = @IdIglesia
                              AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                        using (SqlCommand cmdIg = new SqlCommand(sqlIglesia, cn, tran))
                        {
                            cmdIg.Parameters.AddWithValue("@NombreIglesia", modelo.NombreIglesia);
                            cmdIg.Parameters.AddWithValue("@RNC_Cedula", modelo.RNC_Cedula ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Telefono", modelo.Telefono ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@CorreoInstitucion", modelo.CorreoInstitucion ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Calle", modelo.Calle ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Numero", modelo.Numero ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Sector", modelo.Sector ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Ciudad", modelo.Ciudad ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Provincia", modelo.Provincia ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Referencia", modelo.Referencia ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@IdEquipo", modelo.IdEquipo);
                            cmdIg.Parameters.AddWithValue("@Denominacion", modelo.Denominacion ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@TipoOrganizacion", modelo.TipoOrganizacion ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@CantidadMaestros", modelo.CantidadMaestros ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@CantidadNinos", modelo.CantidadNinos ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Ref1Nombre", modelo.Ref1Nombre ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Ref1Contacto", modelo.Ref1Contacto ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Ref2Nombre", modelo.Ref2Nombre ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Ref2Contacto", modelo.Ref2Contacto ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@IdUsuarioEdicion", idUsuarioEdicion);
                            cmdIg.Parameters.AddWithValue("@RowVersion", modelo.RowVersion ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);

                            int filas = cmdIg.ExecuteNonQuery();
                            if (filas == 0)
                            {
                                tran.Rollback();
                                throw new System.Data.DBConcurrencyException("El registro de esta iglesia fue modificado concurrentemente por otro usuario. Actualice el expediente antes de continuar.");
                            }
                        }

                        // 2. Actualizar o Insertar Pastor
                        if (modelo.Pastor != null)
                        {
                            ActualizarOInsertarPersona(cn, tran, modelo.IdIglesia, "Pastor", modelo.Pastor);
                        }

                        // 3. Actualizar o Insertar Líder
                        if (modelo.LiderMinisterial != null)
                        {
                            ActualizarOInsertarPersona(cn, tran, modelo.IdIglesia, "LiderMinisterial", modelo.LiderMinisterial);
                        }

                        // Registrar en Historial
                        string sqlLog = @"
                            INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                            SELECT TOP 1 IdParticipacion, GETDATE(), 'Edición de Iglesia', EstadoEvaluacion, EstadoEvaluacion, @IdUser, 'Datos de la iglesia actualizados por el usuario.'
                            FROM dbo.ParticipacionesIglesia WHERE IdIglesia = @IdIglesia ORDER BY IdParticipacion DESC;";
                        using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn, tran))
                        {
                            cmdLog.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);
                            cmdLog.Parameters.AddWithValue("@IdUser", idUsuarioEdicion);
                            cmdLog.ExecuteNonQuery();
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

        private void ActualizarOInsertarPersona(SqlConnection cn, SqlTransaction tran, int idIglesia, string tipoPersona, PersonaIglesia persona)
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
                        Correo = @Correo,
                        DocumentoAdjuntoRuta = COALESCE(@Ruta, DocumentoAdjuntoRuta)
                    WHERE IdIglesia = @IdIglesia AND TipoPersona = @Tipo;";
                using (SqlCommand cmdUp = new SqlCommand(sqlUpdate, cn, tran))
                {
                    cmdUp.Parameters.AddWithValue("@Nombres", persona.Nombres ?? "");
                    cmdUp.Parameters.AddWithValue("@Apellidos", persona.Apellidos ?? "");
                    cmdUp.Parameters.AddWithValue("@Doc", persona.DocumentoIdentidad ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@Celular", persona.Celular ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@Correo", persona.Correo ?? (object)DBNull.Value);
                    cmdUp.Parameters.AddWithValue("@Ruta", string.IsNullOrEmpty(persona.DocumentoAdjuntoRuta) ? (object)DBNull.Value : persona.DocumentoAdjuntoRuta);
                    cmdUp.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmdUp.Parameters.AddWithValue("@Tipo", tipoPersona);
                    cmdUp.ExecuteNonQuery();
                }
            }
            else
            {
                InsertarPersona(cn, tran, idIglesia, tipoPersona, persona);
            }
        }

        // ============================================================================
        // MÉTODOS DE EXCEPCIÓN A LA REGLA DE 3 AÑOS (DOBLE APROBACIÓN CE + CMI)
        // ============================================================================

        private ExcepcionRegla3Anios MapearExcepcion(SqlDataReader dr)
        {
            return new ExcepcionRegla3Anios
            {
                IdExcepcion = Convert.ToInt32(dr["IdExcepcion"]),
                IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                NombreIglesia = dr.TableHasColumn("NombreIglesia") && dr["NombreIglesia"] != DBNull.Value ? dr["NombreIglesia"].ToString() : "",
                IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                NombreTemporada = dr.TableHasColumn("NombreTemporada") && dr["NombreTemporada"] != DBNull.Value ? dr["NombreTemporada"].ToString() : "",
                TemporadaPreviaId = dr["TemporadaPreviaId"] != DBNull.Value ? (int?)Convert.ToInt32(dr["TemporadaPreviaId"]) : null,
                NombreTemporadaPrevia = dr.TableHasColumn("NombreTemporadaPrevia") && dr["NombreTemporadaPrevia"] != DBNull.Value ? dr["NombreTemporadaPrevia"].ToString() : "",
                DiferenciaTemporadas = Convert.ToInt32(dr["DiferenciaTemporadas"]),
                Motivo = dr["Motivo"].ToString(),
                Justificacion = dr["Justificacion"].ToString(),
                ResultadoDesempeno = dr["ResultadoDesempeno"] != DBNull.Value ? dr["ResultadoDesempeno"].ToString() : "",
                SolicitadoPor = Convert.ToInt32(dr["SolicitadoPor"]),
                NombreSolicitante = dr.TableHasColumn("NombreSolicitante") && dr["NombreSolicitante"] != DBNull.Value ? dr["NombreSolicitante"].ToString() : "",
                FechaSolicitud = Convert.ToDateTime(dr["FechaSolicitud"]),
                AprobadoCE = Convert.ToBoolean(dr["AprobadoCE"]),
                UsuarioAprobacionCE = dr["UsuarioAprobacionCE"] != DBNull.Value ? (int?)Convert.ToInt32(dr["UsuarioAprobacionCE"]) : null,
                NombreUsuarioAprobacionCE = dr.TableHasColumn("NombreUsuarioCE") && dr["NombreUsuarioCE"] != DBNull.Value ? dr["NombreUsuarioCE"].ToString() : "",
                FechaAprobacionCE = dr["FechaAprobacionCE"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaAprobacionCE"]) : null,
                ComentarioCE = dr["ComentarioCE"] != DBNull.Value ? dr["ComentarioCE"].ToString() : "",
                AprobadoCMI = Convert.ToBoolean(dr["AprobadoCMI"]),
                UsuarioAprobacionCMI = dr["UsuarioAprobacionCMI"] != DBNull.Value ? (int?)Convert.ToInt32(dr["UsuarioAprobacionCMI"]) : null,
                NombreUsuarioAprobacionCMI = dr.TableHasColumn("NombreUsuarioCMI") && dr["NombreUsuarioCMI"] != DBNull.Value ? dr["NombreUsuarioCMI"].ToString() : "",
                FechaAprobacionCMI = dr["FechaAprobacionCMI"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaAprobacionCMI"]) : null,
                ComentarioCMI = dr["ComentarioCMI"] != DBNull.Value ? dr["ComentarioCMI"].ToString() : "",
                Rechazado = Convert.ToBoolean(dr["Rechazado"]),
                UsuarioRechazo = dr["UsuarioRechazo"] != DBNull.Value ? (int?)Convert.ToInt32(dr["UsuarioRechazo"]) : null,
                NombreUsuarioRechazo = dr.TableHasColumn("NombreUsuarioRechazo") && dr["NombreUsuarioRechazo"] != DBNull.Value ? dr["NombreUsuarioRechazo"].ToString() : "",
                FechaRechazo = dr["FechaRechazo"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaRechazo"]) : null,
                MotivoRechazo = dr["MotivoRechazo"] != DBNull.Value ? dr["MotivoRechazo"].ToString() : "",
                Estado = dr["Estado"].ToString(),
                FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"]),
                FechaModificacion = dr["FechaModificacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaModificacion"]) : null,
                RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null
            };
        }

        private ExcepcionRegla3Anios ObtenerExcepcionActivaInterno(SqlConnection cn, int idIglesia, int idTemporada)
        {
            string sql = @"
                SELECT TOP 1 e.*, 
                       i.NombreIglesia,
                       t.NombreTemporada,
                       tp.NombreTemporada AS NombreTemporadaPrevia,
                       ISNULL(pcSol.PrimerNombre + ' ' + pcSol.PrimerApellido, uSol.Correo) AS NombreSolicitante,
                       ISNULL(pcCE.PrimerNombre + ' ' + pcCE.PrimerApellido, uCE.Correo) AS NombreUsuarioCE,
                       ISNULL(pcCMI.PrimerNombre + ' ' + pcCMI.PrimerApellido, uCMI.Correo) AS NombreUsuarioCMI,
                       ISNULL(pcRech.PrimerNombre + ' ' + pcRech.PrimerApellido, uRech.Correo) AS NombreUsuarioRechazo
                FROM dbo.ExcepcionesRegla3Anios e
                INNER JOIN dbo.Iglesias i ON e.IdIglesia = i.IdIglesia
                INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                LEFT JOIN dbo.Temporadas tp ON e.TemporadaPreviaId = tp.IdTemporada
                LEFT JOIN dbo.Usuarios uSol ON e.SolicitadoPor = uSol.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcSol ON uSol.IdUsuario = pcSol.IdUsuario
                LEFT JOIN dbo.Usuarios uCE ON e.UsuarioAprobacionCE = uCE.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcCE ON uCE.IdUsuario = pcCE.IdUsuario
                LEFT JOIN dbo.Usuarios uCMI ON e.UsuarioAprobacionCMI = uCMI.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcCMI ON uCMI.IdUsuario = pcCMI.IdUsuario
                LEFT JOIN dbo.Usuarios uRech ON e.UsuarioRechazo = uRech.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcRech ON uRech.IdUsuario = pcRech.IdUsuario
                WHERE e.IdIglesia = @IdIglesia AND e.IdTemporada = @IdTemporada AND e.Estado IN ('PENDIENTE', 'APROBADA')
                ORDER BY e.IdExcepcion DESC;";

            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return MapearExcepcion(dr);
                    }
                }
            }
            return null;
        }

        private List<ExcepcionRegla3Anios> ObtenerHistorialExcepcionesInterno(SqlConnection cn, int idIglesia)
        {
            var lista = new List<ExcepcionRegla3Anios>();
            string sql = @"
                SELECT e.*, 
                       i.NombreIglesia,
                       t.NombreTemporada,
                       tp.NombreTemporada AS NombreTemporadaPrevia,
                       ISNULL(pcSol.PrimerNombre + ' ' + pcSol.PrimerApellido, uSol.Correo) AS NombreSolicitante,
                       ISNULL(pcCE.PrimerNombre + ' ' + pcCE.PrimerApellido, uCE.Correo) AS NombreUsuarioCE,
                       ISNULL(pcCMI.PrimerNombre + ' ' + pcCMI.PrimerApellido, uCMI.Correo) AS NombreUsuarioCMI,
                       ISNULL(pcRech.PrimerNombre + ' ' + pcRech.PrimerApellido, uRech.Correo) AS NombreUsuarioRechazo
                FROM dbo.ExcepcionesRegla3Anios e
                INNER JOIN dbo.Iglesias i ON e.IdIglesia = i.IdIglesia
                INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                LEFT JOIN dbo.Temporadas tp ON e.TemporadaPreviaId = tp.IdTemporada
                LEFT JOIN dbo.Usuarios uSol ON e.SolicitadoPor = uSol.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcSol ON uSol.IdUsuario = pcSol.IdUsuario
                LEFT JOIN dbo.Usuarios uCE ON e.UsuarioAprobacionCE = uCE.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcCE ON uCE.IdUsuario = pcCE.IdUsuario
                LEFT JOIN dbo.Usuarios uCMI ON e.UsuarioAprobacionCMI = uCMI.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcCMI ON uCMI.IdUsuario = pcCMI.IdUsuario
                LEFT JOIN dbo.Usuarios uRech ON e.UsuarioRechazo = uRech.IdUsuario
                LEFT JOIN dbo.PerfilesCoordinador pcRech ON uRech.IdUsuario = pcRech.IdUsuario
                WHERE e.IdIglesia = @IdIglesia
                ORDER BY e.IdExcepcion DESC;";

            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(MapearExcepcion(dr));
                    }
                }
            }
            return lista;
        }

        private DesempenoHistoricoIglesia CalcularDesempenoHistoricoInterno(SqlConnection cn, int idIglesia, int idTemporadaPrevia, int idTemporadaActual)
        {
            var des = new DesempenoHistoricoIglesia
            {
                IdIglesia = idIglesia,
                IdTemporadaPrevia = idTemporadaPrevia,
                DiferenciaTemporadas = idTemporadaActual - idTemporadaPrevia
            };

            string sql = @"
                SELECT TOP 1 
                    i.NombreIglesia,
                    t.NombreTemporada,
                    p.IdParticipacion,
                    p.EtapaActual,
                    p.EstadoEvaluacion,
                    p.EstatusEvaluacionReporte,
                    (SELECT COUNT(1) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion AND re.TipoReporte = 'Evangelistico') AS RepEvang,
                    (SELECT COUNT(1) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion AND re.TipoReporte = 'GranAventura') AS RepGA,
                    (SELECT ISNULL(SUM(re.CantidadNinos), 0) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion) AS TotalNinos,
                    (SELECT ISNULL(SUM(re.CuantosAceptaronSenor), 0) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion) AS TotalAceptaron,
                    (SELECT ISNULL(SUM(re.CuantosGraduaron), 0) FROM dbo.ReportesEventos re WHERE re.IdParticipacion = p.IdParticipacion) AS TotalGraduados,
                    (SELECT COUNT(DISTINCT am.IdMaestro) 
                     FROM dbo.AsistenciaMaestro am 
                     INNER JOIN dbo.Eventos ev ON am.IdEvento = ev.IdEvento 
                     INNER JOIN dbo.Maestros m ON am.IdMaestro = m.IdMaestro
                     WHERE m.IdIglesia = i.IdIglesia AND ev.IdTemporada = p.IdTemporada AND am.Asistio = 1) AS TotalMaestros
                FROM dbo.ParticipacionesIglesia p
                INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                WHERE p.IdIglesia = @IdIglesia AND p.IdTemporada = @IdTempPrev;";

            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                cmd.Parameters.AddWithValue("@IdTempPrev", idTemporadaPrevia);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        des.NombreIglesia = dr["NombreIglesia"].ToString();
                        des.NombreTemporadaPrevia = dr["NombreTemporada"].ToString();
                        des.IdParticipacionPrevia = Convert.ToInt32(dr["IdParticipacion"]);
                        des.EtapaAlcanzada = Convert.ToInt32(dr["EtapaActual"]);
                        des.EstadoEvaluacion = dr["EstadoEvaluacion"].ToString();
                        des.EstatusReporte = dr["EstatusEvaluacionReporte"] != DBNull.Value ? dr["EstatusEvaluacionReporte"].ToString() : "Pendiente";
                        des.ReportoEvangelismo = Convert.ToInt32(dr["RepEvang"]) > 0;
                        des.ReportoDiscipulado = Convert.ToInt32(dr["RepGA"]) > 0;
                        des.TotalNinosAlcanzados = Convert.ToInt32(dr["TotalNinos"]);
                        des.TotalDecisionesFe = Convert.ToInt32(dr["TotalAceptaron"]);
                        des.TotalGraduados = Convert.ToInt32(dr["TotalGraduados"]);
                        des.TotalMaestrosCapacitados = Convert.ToInt32(dr["TotalMaestros"]);

                        des.ResumenTexto = $"En la temporada '{des.NombreTemporadaPrevia}', la iglesia completó hasta la Etapa {des.EtapaAlcanzada} (Estatus reporte: {des.EstatusReporte}). " +
                            $"Reportó {des.TotalNinosAlcanzados} niños alcanzados, {des.TotalDecisionesFe} decisiones de fe, {des.TotalGraduados} graduados de La Gran Aventura, y {des.TotalMaestrosCapacitados} maestros con asistencia confirmada a capacitaciones.";
                    }
                    else
                    {
                        des.ResumenTexto = "No se encontraron registros estadísticos en la base de datos para la temporada previa especificada.";
                    }
                }
            }

            return des;
        }

        public ExcepcionRegla3Anios ObtenerExcepcionActiva(int idIglesia, int idTemporada)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                return ObtenerExcepcionActivaInterno(cn, idIglesia, idTemporada);
            }
        }

        public List<ExcepcionRegla3Anios> ObtenerHistorialExcepciones(int idIglesia)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                return ObtenerHistorialExcepcionesInterno(cn, idIglesia);
            }
        }

        public DesempenoHistoricoIglesia CalcularDesempenoHistorico(int idIglesia, int idTemporadaPrevia, int idTemporadaActual)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                return CalcularDesempenoHistoricoInterno(cn, idIglesia, idTemporadaPrevia, idTemporadaActual);
            }
        }

        public ExcepcionRegla3Anios ObtenerExcepcionPorId(int idExcepcion)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = @"
                    SELECT TOP 1 e.*, 
                           i.NombreIglesia,
                           t.NombreTemporada,
                           tp.NombreTemporada AS NombreTemporadaPrevia,
                           ISNULL(pcSol.PrimerNombre + ' ' + pcSol.PrimerApellido, uSol.Correo) AS NombreSolicitante,
                           ISNULL(pcCE.PrimerNombre + ' ' + pcCE.PrimerApellido, uCE.Correo) AS NombreUsuarioCE,
                           ISNULL(pcCMI.PrimerNombre + ' ' + pcCMI.PrimerApellido, uCMI.Correo) AS NombreUsuarioCMI,
                           ISNULL(pcRech.PrimerNombre + ' ' + pcRech.PrimerApellido, uRech.Correo) AS NombreUsuarioRechazo
                    FROM dbo.ExcepcionesRegla3Anios e
                    INNER JOIN dbo.Iglesias i ON e.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                    LEFT JOIN dbo.Temporadas tp ON e.TemporadaPreviaId = tp.IdTemporada
                    LEFT JOIN dbo.Usuarios uSol ON e.SolicitadoPor = uSol.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador pcSol ON uSol.IdUsuario = pcSol.IdUsuario
                    LEFT JOIN dbo.Usuarios uCE ON e.UsuarioAprobacionCE = uCE.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador pcCE ON uCE.IdUsuario = pcCE.IdUsuario
                    LEFT JOIN dbo.Usuarios uCMI ON e.UsuarioAprobacionCMI = uCMI.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador pcCMI ON uCMI.IdUsuario = pcCMI.IdUsuario
                    LEFT JOIN dbo.Usuarios uRech ON e.UsuarioRechazo = uRech.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador pcRech ON uRech.IdUsuario = pcRech.IdUsuario
                    WHERE e.IdExcepcion = @IdExcepcion;";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdExcepcion", idExcepcion);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return MapearExcepcion(dr);
                        }
                    }
                }
            }
            return null;
        }

        public int RegistrarSolicitudExcepcion(ExcepcionRegla3Anios excepcion, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(excepcion.Motivo))
                throw new ArgumentException("El motivo de la excepción es obligatorio.");
            if (string.IsNullOrWhiteSpace(excepcion.Justificacion))
                throw new ArgumentException("La justificación detallada de la excepción es obligatoria.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Validar que no exista ya una excepción activa (PENDIENTE o APROBADA)
                        string sqlCheck = @"
                            SELECT COUNT(1) 
                            FROM dbo.ExcepcionesRegla3Anios 
                            WHERE IdIglesia = @IdIglesia AND IdTemporada = @IdTemp AND Estado IN ('PENDIENTE', 'APROBADA');";
                        using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@IdIglesia", excepcion.IdIglesia);
                            cmdCheck.Parameters.AddWithValue("@IdTemp", excepcion.IdTemporada);
                            int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());
                            if (existe > 0)
                            {
                                throw new InvalidOperationException("Esta iglesia ya cuenta con una solicitud de excepción activa (Pendiente o Aprobada) para esta temporada.");
                            }
                        }

                        // 2. Insertar excepción
                        string sqlInsert = @"
                            INSERT INTO dbo.ExcepcionesRegla3Anios (
                                IdIglesia, IdTemporada, TemporadaPreviaId, DiferenciaTemporadas,
                                Motivo, Justificacion, ResultadoDesempeno,
                                SolicitadoPor, FechaSolicitud,
                                AprobadoCE, AprobadoCMI, Rechazado, Estado,
                                FechaCreacion
                            ) VALUES (
                                @IdIglesia, @IdTemporada, @TemporadaPreviaId, @DiferenciaTemporadas,
                                @Motivo, @Justificacion, @ResultadoDesempeno,
                                @SolicitadoPor, GETDATE(),
                                0, 0, 0, 'PENDIENTE',
                                GETDATE()
                            );
                            SELECT SCOPE_IDENTITY();";

                        int idExcepcionNew;
                        using (SqlCommand cmdIns = new SqlCommand(sqlInsert, cn, tran))
                        {
                            cmdIns.Parameters.AddWithValue("@IdIglesia", excepcion.IdIglesia);
                            cmdIns.Parameters.AddWithValue("@IdTemporada", excepcion.IdTemporada);
                            cmdIns.Parameters.AddWithValue("@TemporadaPreviaId", excepcion.TemporadaPreviaId.HasValue ? (object)excepcion.TemporadaPreviaId.Value : DBNull.Value);
                            cmdIns.Parameters.AddWithValue("@DiferenciaTemporadas", excepcion.DiferenciaTemporadas);
                            cmdIns.Parameters.AddWithValue("@Motivo", excepcion.Motivo.Trim());
                            cmdIns.Parameters.AddWithValue("@Justificacion", excepcion.Justificacion.Trim());
                            cmdIns.Parameters.AddWithValue("@ResultadoDesempeno", (object)excepcion.ResultadoDesempeno ?? DBNull.Value);
                            cmdIns.Parameters.AddWithValue("@SolicitadoPor", idUsuario);

                            idExcepcionNew = Convert.ToInt32(cmdIns.ExecuteScalar());
                        }

                        // 3. Registrar auditoría
                        AuditoriaHelper.Registrar(cn, tran, idUsuario, null, "SOLICITAR_EXCEPCION_3ANIOS", "EXCEPCIONES",
                            idExcepcionNew.ToString(), $"Solicitud de excepción registrada para iglesia #{excepcion.IdIglesia}. Motivo: {excepcion.Motivo}");

                        tran.Commit();
                        return idExcepcionNew;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AprobarExcepcionCE(int idExcepcion, int idUsuarioCE, string comentario, byte[] rowVersion)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Validar estado actual
                        string sqlVal = "SELECT Estado, AprobadoCMI FROM dbo.ExcepcionesRegla3Anios WHERE IdExcepcion = @Id;";
                        bool cmiAprobado = false;
                        using (SqlCommand cmdVal = new SqlCommand(sqlVal, cn, tran))
                        {
                            cmdVal.Parameters.AddWithValue("@Id", idExcepcion);
                            using (SqlDataReader dr = cmdVal.ExecuteReader())
                            {
                                if (!dr.Read()) throw new InvalidOperationException("La solicitud de excepción no existe.");
                                string estadoActual = dr["Estado"].ToString();
                                if (estadoActual == "RECHAZADA") throw new InvalidOperationException("No se puede aprobar una excepción que ha sido rechazada.");
                                cmiAprobado = Convert.ToBoolean(dr["AprobadoCMI"]);
                            }
                        }

                        string nuevoEstado = cmiAprobado ? "APROBADA" : "PENDIENTE";

                        string sqlUp = @"
                            UPDATE dbo.ExcepcionesRegla3Anios SET
                                AprobadoCE = 1,
                                UsuarioAprobacionCE = @IdUser,
                                FechaAprobacionCE = GETDATE(),
                                ComentarioCE = @Comentario,
                                Estado = @NuevoEstado,
                                FechaModificacion = GETDATE()
                            WHERE IdExcepcion = @Id 
                              AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                        using (SqlCommand cmdUp = new SqlCommand(sqlUp, cn, tran))
                        {
                            cmdUp.Parameters.AddWithValue("@IdUser", idUsuarioCE);
                            cmdUp.Parameters.AddWithValue("@Comentario", (object)comentario ?? DBNull.Value);
                            cmdUp.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                            cmdUp.Parameters.AddWithValue("@Id", idExcepcion);
                            cmdUp.Parameters.AddWithValue("@RowVersion", rowVersion ?? (object)DBNull.Value);

                            int rows = cmdUp.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                throw new DBConcurrencyException("Conflicto de concurrencia: La excepción fue modificada por otro usuario mientras se procesaba su aprobación.");
                            }
                        }

                        AuditoriaHelper.Registrar(cn, tran, idUsuarioCE, null, "APROBAR_EXCEPCION_CE", "EXCEPCIONES",
                            idExcepcion.ToString(), $"Aprobación de CE registrada. Estado resultante: {nuevoEstado}. Comentario: {comentario}");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AprobarExcepcionCMI(int idExcepcion, int idUsuarioCMI, string comentario, byte[] rowVersion)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        string sqlVal = "SELECT Estado, AprobadoCE FROM dbo.ExcepcionesRegla3Anios WHERE IdExcepcion = @Id;";
                        bool ceAprobado = false;
                        using (SqlCommand cmdVal = new SqlCommand(sqlVal, cn, tran))
                        {
                            cmdVal.Parameters.AddWithValue("@Id", idExcepcion);
                            using (SqlDataReader dr = cmdVal.ExecuteReader())
                            {
                                if (!dr.Read()) throw new InvalidOperationException("La solicitud de excepción no existe.");
                                string estadoActual = dr["Estado"].ToString();
                                if (estadoActual == "RECHAZADA") throw new InvalidOperationException("No se puede aprobar una excepción que ha sido rechazada.");
                                ceAprobado = Convert.ToBoolean(dr["AprobadoCE"]);
                            }
                        }

                        string nuevoEstado = ceAprobado ? "APROBADA" : "PENDIENTE";

                        string sqlUp = @"
                            UPDATE dbo.ExcepcionesRegla3Anios SET
                                AprobadoCMI = 1,
                                UsuarioAprobacionCMI = @IdUser,
                                FechaAprobacionCMI = GETDATE(),
                                ComentarioCMI = @Comentario,
                                Estado = @NuevoEstado,
                                FechaModificacion = GETDATE()
                            WHERE IdExcepcion = @Id 
                              AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                        using (SqlCommand cmdUp = new SqlCommand(sqlUp, cn, tran))
                        {
                            cmdUp.Parameters.AddWithValue("@IdUser", idUsuarioCMI);
                            cmdUp.Parameters.AddWithValue("@Comentario", (object)comentario ?? DBNull.Value);
                            cmdUp.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                            cmdUp.Parameters.AddWithValue("@Id", idExcepcion);
                            cmdUp.Parameters.AddWithValue("@RowVersion", rowVersion ?? (object)DBNull.Value);

                            int rows = cmdUp.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                throw new DBConcurrencyException("Conflicto de concurrencia: La excepción fue modificada por otro usuario mientras se procesaba su aprobación.");
                            }
                        }

                        AuditoriaHelper.Registrar(cn, tran, idUsuarioCMI, null, "APROBAR_EXCEPCION_CMI", "EXCEPCIONES",
                            idExcepcion.ToString(), $"Aprobación de CMI registrada. Estado resultante: {nuevoEstado}. Comentario: {comentario}");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public void RechazarExcepcion(int idExcepcion, int idUsuario, string motivo, byte[] rowVersion)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Debe especificar el motivo del rechazo.");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        string sqlUp = @"
                            UPDATE dbo.ExcepcionesRegla3Anios SET
                                Rechazado = 1,
                                UsuarioRechazo = @IdUser,
                                FechaRechazo = GETDATE(),
                                MotivoRechazo = @Motivo,
                                Estado = 'RECHAZADA',
                                FechaModificacion = GETDATE()
                            WHERE IdExcepcion = @Id 
                              AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                        using (SqlCommand cmdUp = new SqlCommand(sqlUp, cn, tran))
                        {
                            cmdUp.Parameters.AddWithValue("@IdUser", idUsuario);
                            cmdUp.Parameters.AddWithValue("@Motivo", motivo.Trim());
                            cmdUp.Parameters.AddWithValue("@Id", idExcepcion);
                            cmdUp.Parameters.AddWithValue("@RowVersion", rowVersion ?? (object)DBNull.Value);

                            int rows = cmdUp.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                throw new DBConcurrencyException("Conflicto de concurrencia: La excepción fue modificada por otro usuario.");
                            }
                        }

                        AuditoriaHelper.Registrar(cn, tran, idUsuario, null, "RECHAZAR_EXCEPCION_3ANIOS", "EXCEPCIONES",
                            idExcepcion.ToString(), $"Excepción rechazada. Motivo: {motivo}");

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool TieneExcepcionAprobada(string rncCedula, int idTemporada)
        {
            if (string.IsNullOrWhiteSpace(rncCedula)) return false;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = @"
                    SELECT COUNT(1)
                    FROM dbo.ExcepcionesRegla3Anios e
                    INNER JOIN dbo.Iglesias i ON e.IdIglesia = i.IdIglesia
                    WHERE i.RNC_Cedula = @Rnc 
                      AND e.IdTemporada = @IdTemporada 
                      AND e.Estado = 'APROBADA' 
                      AND e.AprobadoCE = 1 
                      AND e.AprobadoCMI = 1;";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Rnc", rncCedula.Trim());
                    cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }
    }
}
