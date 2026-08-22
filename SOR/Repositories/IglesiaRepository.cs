using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SOR.Models;

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
                           p.IdParticipacion, p.EstadoEvaluacion, p.EtapaActual, t.NombreTemporada
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    LEFT JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    LEFT JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada AND t.Activa = 1
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

        public int RegistrarIglesia(Iglesia modelo, int idUsuarioCreacion)
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
                                NombreIglesia, RNC_Cedula, Telefono, Calle, Numero, Sector, Ciudad, Provincia, Referencia, IdEquipo, IdUsuarioCreacion,
                                Denominacion, TipoOrganizacion, CantidadMaestros, CantidadNinos, Ref1Nombre, Ref1Contacto, Ref2Nombre, Ref2Contacto
                            ) VALUES (
                                @NombreIglesia, @RNC_Cedula, @Telefono, @Calle, @Numero, @Sector, @Ciudad, @Provincia, @Referencia, @IdEquipo, @IdUsuarioCreacion,
                                @Denominacion, @TipoOrganizacion, @CantidadMaestros, @CantidadNinos, @Ref1Nombre, @Ref1Contacto, @Ref2Nombre, @Ref2Contacto
                            );
                            SELECT SCOPE_IDENTITY();";

                        SqlCommand cmdIg = new SqlCommand(sqlIglesia, cn, tran);
                        cmdIg.Parameters.AddWithValue("@NombreIglesia", modelo.NombreIglesia);
                        cmdIg.Parameters.AddWithValue("@RNC_Cedula", modelo.RNC_Cedula ?? (object)DBNull.Value);
                        cmdIg.Parameters.AddWithValue("@Telefono", modelo.Telefono ?? (object)DBNull.Value);
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

                        // 4. Crear Participación Inicial en la Temporada Activa
                        string sqlTemp = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas WHERE Activa = 1;";
                        SqlCommand cmdTemp = new SqlCommand(sqlTemp, cn, tran);
                        object idTempObj = cmdTemp.ExecuteScalar();
                        if (idTempObj == null)
                        {
                            throw new Exception("No hay ninguna temporada activa configurada en el sistema. Contacta a un Administrador para activar una.");
                        }
                        int idTemporadaActiva = Convert.ToInt32(idTempObj);
                            string sqlPart = @"
                                INSERT INTO dbo.ParticipacionesIglesia (IdIglesia, IdTemporada, Participara, EstadoEvaluacion, EtapaActual)
                                VALUES (@IdIglesia, @IdTemporada, 1, 'Pendiente', 1);
                                SELECT SCOPE_IDENTITY();";

                            SqlCommand cmdPart = new SqlCommand(sqlPart, cn, tran);
                            cmdPart.Parameters.AddWithValue("@IdIglesia", idIglesiaNew);
                            cmdPart.Parameters.AddWithValue("@IdTemporada", idTemporadaActiva);
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
                                Ref2Contacto = dr["Ref2Contacto"] != DBNull.Value ? dr["Ref2Contacto"].ToString() : ""
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
                string sqlMaestros = "SELECT * FROM dbo.Maestros WHERE IdIglesia = @Id AND Activo = 1;";
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
                    SELECT p.*, t.NombreTemporada, r.* 
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    LEFT JOIN dbo.AsignacionesRecursos r ON p.IdParticipacion = r.IdParticipacion
                    WHERE p.IdIglesia = @Id AND t.Activa = 1;";

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
                                Participara = Convert.ToBoolean(drPart["Participara"]),
                                JustificacionNoParticipacion = drPart["JustificacionNoParticipacion"] != DBNull.Value ? drPart["JustificacionNoParticipacion"].ToString() : "",
                                EstadoEvaluacion = drPart["EstadoEvaluacion"].ToString(),

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
                                NuevosTestamentos = drPart["NuevosTestamentos"] != DBNull.Value ? Convert.ToInt32(drPart["NuevosTestamentos"]) : 0
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
                           (SELECT TOP 1 NombreTemporada FROM dbo.Temporadas WHERE Activa = 1) AS NombreTemporada
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
                        // 1. Actualizar Iglesia
                        string sqlIglesia = @"
                            UPDATE dbo.Iglesias SET
                                NombreIglesia = @NombreIglesia,
                                RNC_Cedula = @RNC_Cedula,
                                Telefono = @Telefono,
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
                                Ref2Contacto = @Ref2Contacto
                            WHERE IdIglesia = @IdIglesia;";

                        using (SqlCommand cmdIg = new SqlCommand(sqlIglesia, cn, tran))
                        {
                            cmdIg.Parameters.AddWithValue("@NombreIglesia", modelo.NombreIglesia);
                            cmdIg.Parameters.AddWithValue("@RNC_Cedula", modelo.RNC_Cedula ?? (object)DBNull.Value);
                            cmdIg.Parameters.AddWithValue("@Telefono", modelo.Telefono ?? (object)DBNull.Value);
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
                            cmdIg.Parameters.AddWithValue("@IdIglesia", modelo.IdIglesia);
                            cmdIg.ExecuteNonQuery();
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
    }
}
