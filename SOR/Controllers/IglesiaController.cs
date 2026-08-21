using SOR.Models;
using SOR.Permisos;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class IglesiaController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Iglesia/Index
        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
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

            ViewBag.UsuarioActual = u;
            return View(lista);
        }

        // GET: Iglesia/Crear
        public ActionResult Crear()
        {
            Usuario u = (Usuario)Session["usuario"];
            
            // Validar si la posición tiene permiso de registro (Equipo, Movilización o Admin/SuperAdmin)
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Tu rol o posición de coordinador no posee permisos para registrar nuevas iglesias.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);
            return View(new Iglesia());
        }

        [HttpPost]
        public ActionResult Crear(Iglesia modelo, HttpPostedFileBase docPastor, HttpPostedFileBase docLider)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Permiso denegado para registrar iglesias.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);

            // Proceso de carga de archivos (Uploads)
            string uploadPath = Server.MapPath("~/Uploads/Iglesias/");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            if (docPastor != null && docPastor.ContentLength > 0)
            {
                string ext = Path.GetExtension(docPastor.FileName);
                string fileName = $"Pastor_{Guid.NewGuid()}{ext}";
                docPastor.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.Pastor.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }

            if (docLider != null && docLider.ContentLength > 0)
            {
                string ext = Path.GetExtension(docLider.FileName);
                string fileName = $"Lider_{Guid.NewGuid()}{ext}";
                docLider.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                SqlTransaction tran = cn.BeginTransaction();

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
                    cmdIg.Parameters.AddWithValue("@IdEquipo", modelo.IdEquipo > 0 ? modelo.IdEquipo : (u.IdEquipo ?? 1));
                    cmdIg.Parameters.AddWithValue("@IdUsuarioCreacion", u.IdUsuario);

                    int idIglesiaNew = Convert.ToInt32(cmdIg.ExecuteScalar());

                    // 2. Insertar Pastor
                    if (!string.IsNullOrEmpty(modelo.Pastor.Nombres))
                    {
                        InsertarPersona(cn, tran, idIglesiaNew, "Pastor", modelo.Pastor);
                    }

                    // 3. Insertar Líder Ministerial
                    if (!string.IsNullOrEmpty(modelo.LiderMinisterial.Nombres))
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
                    TempData["MensajeExito"] = "Iglesia registrada exitosamente con su expediente inicial.";
                    return RedirectToAction("Detalle", new { id = idIglesiaNew });
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    ViewData["MensajeError"] = "Error al registrar la iglesia: " + ex.Message;
                    return View(modelo);
                }
            }
        }

        // GET: Iglesia/Detalle/5
        public ActionResult Detalle(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = ObtenerExpedienteIglesia(id);

            if (iglesia == null)
            {
                return HttpNotFound();
            }

            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarIglesia(u, iglesia.IdEquipo);
            return View(iglesia);
        }

        [HttpPost]
        public ActionResult EvaluarParticipacion(int idParticipacion, int idIglesia, string estadoEvaluacion, bool participara, string justificacionNoParticipacion)
        {
            Usuario u = (Usuario)Session["usuario"];

            // Regla de Negocio: Si no participará, exige justificación obligatoria
            if (!participara && string.IsNullOrWhiteSpace(justificacionNoParticipacion))
            {
                TempData["MensajeError"] = "Debe proporcionar un motivo o justificación en caso de marcar que la iglesia NO participará esta temporada.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

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
                cmd.Parameters.AddWithValue("@Justificacion", justificacionNoParticipacion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EstadoEvaluacion", estadoEvaluacion ?? "Pendiente");
                cmd.Parameters.AddWithValue("@IdEvaluador", u.IdUsuario);
                cmd.Parameters.AddWithValue("@IdParticipacion", idParticipacion);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Evaluación de participación actualizada correctamente.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult DespacharRecursos(AsignacionRecursos modelo, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];

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
                cmd.Parameters.AddWithValue("@IdDespacho", u.IdUsuario);
                cmd.Parameters.AddWithValue("@IdParticipacion", modelo.IdParticipacion);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Asignación de recursos despachados guardada con éxito.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult AgregarComentario(int idIglesia, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];

            if (string.IsNullOrWhiteSpace(comentario))
            {
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdIglesia", idIglesia);
                cmd.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                cmd.Parameters.AddWithValue("@Comentario", comentario);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Observación guardada en el historial.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // MÉTODOS DE CONTROL DE PERMISOS DE JERARQUÍA Y POSICIÓN
        // ============================================================================

        private bool PuedeRegistrarIglesia(Usuario u)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin
            if (u.RangoJerarquico == 1) return true; // ENL
            if (u.IdPosicion == 1 || u.IdPosicion == 2) return true; // Coord Equipo o Movilización
            return false;
        }

        private bool PuedeEditarIglesia(Usuario u, int idEquipoIglesia)
        {
            // SuperAdmin o Admin -> Puede editar todo
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;

            // ENL (Rango 1) -> Puede editar todo
            if (u.RangoJerarquico == 1) return true;

            // ERLE (Rango 2) -> Puede editar su propio equipo y los ERLs bajo su supervisión
            if (u.RangoJerarquico == 2)
            {
                if (u.IdEquipo.HasValue && u.IdEquipo.Value == idEquipoIglesia) return true;
                return EsEquipoHijo(u.IdEquipo.Value, idEquipoIglesia);
            }

            // ERL (Rango 3) -> Solo puede editar registros de su propio equipo
            if (u.RangoJerarquico == 3)
            {
                return u.IdEquipo.HasValue && u.IdEquipo.Value == idEquipoIglesia;
            }

            return false;
        }

        private bool EsEquipoHijo(int idEquipoPadre, int idEquipoHijo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT COUNT(1) FROM dbo.Equipos WHERE IdEquipo = @IdHijo AND IdEquipoPadre = @IdPadre;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdHijo", idEquipoHijo);
                cmd.Parameters.AddWithValue("@IdPadre", idEquipoPadre);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
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

        private Iglesia ObtenerExpedienteIglesia(int idIglesia)
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

        private void CargarEquiposDisponibles(Usuario u)
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT e.IdEquipo, e.NombreEquipo, n.NombreNivel FROM dbo.Equipos e INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo ORDER BY n.RangoJerarquico, e.NombreEquipo;";
                SqlCommand cmd = new SqlCommand(sql, cn);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = dr["IdEquipo"].ToString(),
                            Text = $"[{dr["NombreNivel"]}] {dr["NombreEquipo"]}"
                        });
                    }
                }
            }

            ViewBag.ListaEquipos = lista;
        }
    }
}
