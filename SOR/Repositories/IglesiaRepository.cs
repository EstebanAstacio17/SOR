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
                    SELECT i.IdIglesia, i.NombreIglesia, i.RNC_Cedula, i.Telefono, i.Ciudad, i.Provincia, i.IdEquipo, e.NombreEquipo
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
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
                            Ciudad = dr["Ciudad"] != DBNull.Value ? dr["Ciudad"].ToString() : "",
                            Provincia = dr["Provincia"] != DBNull.Value ? dr["Provincia"].ToString() : "",
                            IdEquipo = Convert.ToInt32(dr["IdEquipo"]),
                            NombreEquipo = dr["NombreEquipo"].ToString()
                        };
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
                            INSERT INTO dbo.Iglesias (NombreIglesia, RNC_Cedula, Telefono, Calle, Numero, Sector, Ciudad, Provincia, Referencia, IdEquipo, IdUsuarioCreacion)
                            VALUES (@NombreIglesia, @RNC_Cedula, @Telefono, @Calle, @Numero, @Sector, @Ciudad, @Provincia, @Referencia, @IdEquipo, @IdUsuarioCreacion);
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
                        if (idTempObj != null)
                        {
                            int idTemporadaActiva = Convert.ToInt32(idTempObj);
                            string sqlPart = @"
                                INSERT INTO dbo.ParticipacionesIglesia (IdIglesia, IdTemporada, Participara, EstadoEvaluacion)
                                VALUES (@IdIglesia, @IdTemporada, 1, 'Pendiente');
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
                        }

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
                                FechaCreacion = dr["FechaCreacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaCreacion"]) : null
                            };
                        }
                    }
                }

                if (ig == null) return null;

                // 2. Personas (Pastor, Líder, Maestros)
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
                            else if (per.TipoPersona == "Maestro") ig.Maestros.Add(per);
                        }
                    }
                }

                // 3. Participación y Recursos Actuales
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
                                EstadoEvaluacion = drPart["EstadoEvaluacion"].ToString()
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

                // 4. Comentarios Históricos
                string sqlCom = "SELECT c.*, u.Correo FROM dbo.ComentariosObservaciones c INNER JOIN dbo.Usuarios u ON c.IdUsuario = u.IdUsuario WHERE c.IdIglesia = @Id ORDER BY c.FechaCreacion DESC;";
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
                                FechaCreacion = Convert.ToDateTime(drC["FechaCreacion"])
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
    }
}
