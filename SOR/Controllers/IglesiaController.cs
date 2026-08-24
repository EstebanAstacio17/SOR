using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class IglesiaController : Controller
    {
        private readonly Services.IglesiaService _iglesiaService = new Services.IglesiaService();
        private readonly Repositories.IglesiaRepository _iglesiaRepository = new Repositories.IglesiaRepository();

        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        // GET: Iglesia/Index
        public ActionResult Index(int? idTemporada, string denominacion, string tipoOrg, int? etapaProcess, string estatusEval)
        {
            Usuario u = (Usuario)Session["usuario"];
            List<Iglesia> listaCompleta = _iglesiaService.ObtenerIglesias();

            // Filtrado del lado del servidor
            var listaFiltrada = listaCompleta.AsEnumerable();

            if (idTemporada.HasValue && idTemporada.Value > 0)
            {
                listaFiltrada = listaFiltrada.Where(x => x.ParticipacionActual != null && x.ParticipacionActual.IdTemporada == idTemporada.Value);
            }
            if (!string.IsNullOrEmpty(denominacion))
            {
                listaFiltrada = listaFiltrada.Where(x => x.Denominacion != null && x.Denominacion.IndexOf(denominacion, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrEmpty(tipoOrg))
            {
                listaFiltrada = listaFiltrada.Where(x => x.TipoOrganizacion != null && x.TipoOrganizacion.IndexOf(tipoOrg, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (etapaProcess.HasValue && etapaProcess.Value > 0)
            {
                listaFiltrada = listaFiltrada.Where(x => x.ParticipacionActual != null && x.ParticipacionActual.EtapaActual == etapaProcess.Value);
            }
            if (!string.IsNullOrEmpty(estatusEval))
            {
                listaFiltrada = listaFiltrada.Where(x => x.ParticipacionActual != null && x.ParticipacionActual.EstadoEvaluacion.Equals(estatusEval, StringComparison.OrdinalIgnoreCase));
            }

            CargarCombosFiltros();
            
            // Cargar Equipos para el filtro de la vista
            List<SelectListItem> listaEq = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdEquipo, NombreEquipo FROM dbo.Equipos ORDER BY NombreEquipo;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            listaEq.Add(new SelectListItem { Value = dr["NombreEquipo"].ToString(), Text = dr["NombreEquipo"].ToString() });
                        }
                    }
                }
            }
            ViewBag.ListaEquipos = listaEq;

            ViewBag.UsuarioActual = u;
            return View(listaFiltrada.ToList());
        }

        public ActionResult DescargarPlantillaIglesias()
        {
            string csvHeader = "NombreIglesia,RNC_Cedula,Telefono,Provincia,Sector,Calle,Numero,Referencia,Denominacion,TipoOrganizacion,PastorNombre,PastorCelular,PastorCorreo,LiderNombre,LiderCelular,LiderCorreo,CantMaestros,CantNinos\n";
            string csvEjemplo = "\"Iglesia Ejemplo de Fe\",\"001-0000000-0\",\"809-555-0101\",\"Santo Domingo\",\"Centro\",\"Calle Principal\",\"12\",\"Cerca del Parque\",\"Pentecostal\",\"Iglesia Local\",\"Juan Pérez\",\"809-555-0102\",\"pastor@ejemplo.com\",\"María Gómez\",\"809-555-0103\",\"lider@ejemplo.com\",\"5\",\"50\"\n";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csvHeader + csvEjemplo);
            return File(fileBytes, "text/csv", "Plantilla_Carga_Masiva_Iglesias.csv");
        }

        public ActionResult DescargarCondicionesImportacion()
        {
            string path = Server.MapPath("~/Documentos Descargables/Condiciones_Importacion_Iglesias.txt");
            if (System.IO.File.Exists(path))
            {
                return File(path, "text/plain", "Condiciones_Importacion_Iglesias.txt");
            }
            return HttpNotFound("Archivo de especificaciones no encontrado.");
        }

        // GET: Iglesia/Crear
        public ActionResult Crear()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Tu rol o posición de coordinador no posee permisos para registrar nuevas iglesias.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);
            CargarCatalogosDenominacionesYTipos();
            return View(new Iglesia());
        }

        [HttpPost]
        public ActionResult Crear(Iglesia modelo, HttpPostedFileBase docPastor, HttpPostedFileBase docLider)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Permiso denegado.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles(u);
            CargarCatalogosDenominacionesYTipos();

            string errorValidacion;
            if (!ValidarFormatosDR(modelo, out errorValidacion))
            {
                ViewData["MensajeError"] = errorValidacion;
                return View(modelo);
            }

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

            try
            {
                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
                {
                    modelo.IdEquipo = u.IdEquipo ?? 1;
                }
                else if (modelo.IdEquipo <= 0)
                {
                    modelo.IdEquipo = u.IdEquipo ?? 1;
                }

                List<string> advertencias = new List<string>();
                int idIglesiaNew = _iglesiaService.RegistrarIglesia(modelo, u.IdUsuario, null, advertencias);
                string msg = "Iglesia registrada exitosamente con su expediente inicial.";
                if (advertencias.Any())
                {
                    msg += "<br/><strong>Advertencias:</strong><br/>" + string.Join("<br/>", advertencias);
                }
                TempData["MensajeExito"] = msg;
                return RedirectToAction("Detalle", new { id = idIglesiaNew });
            }
            catch (Exception ex)
            {
                ViewData["MensajeError"] = "Error al registrar la iglesia: " + ex.Message;
                return View(modelo);
            }
        }

        // GET: Iglesia/Detalle/5
        public ActionResult Detalle(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(id);

            if (iglesia == null)
            {
                return HttpNotFound();
            }

            // Cargar eventos de tipo Visión y Taller para la temporada activa filtrados por equipo
            List<SelectListItem> eventosVision = new List<SelectListItem>();
            List<SelectListItem> eventosTaller = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT e.IdEvento, e.NombreEvento, e.Fecha, e.Lugar, e.TipoEvento
                    FROM dbo.Eventos e 
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada 
                    LEFT JOIN dbo.PerfilesCoordinador pc ON e.IdUsuarioCreacion = pc.IdUsuario
                    WHERE e.IdTemporada = (SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY FechaInicio DESC)
                      AND e.TipoEvento IN ('Vision', 'Taller')";

                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue)
                {
                    sql += " AND pc.IdEquipo = @IdEquipo";
                }

                sql += " ORDER BY e.Fecha DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                
                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@IdEquipo", u.IdEquipo.Value);
                }

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var item = new SelectListItem
                        {
                            Value = dr["IdEvento"].ToString(),
                            Text = $"{dr["NombreEvento"]} - {Convert.ToDateTime(dr["Fecha"]):dd/MM/yyyy} ({dr["Lugar"]})"
                        };
                        string tipo = dr["TipoEvento"].ToString();
                        
                        // CMI (2) ve Visión, CD (3) ve Taller, CE (1) / Admin (Rol 1,2) ve ambos
                        bool esCMI = (u.IdPosicion == 2);
                        bool esCD = (u.IdPosicion == 3);
                        
                        if (tipo == "Vision" && !esCD) eventosVision.Add(item);
                        else if (tipo == "Taller" && !esCMI) eventosTaller.Add(item);
                    }
                }
            }
            ViewBag.EventosVision = eventosVision;
            ViewBag.EventosTaller = eventosTaller;

            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarIglesia(u, iglesia.IdEquipo);
            return View(iglesia);
        }

        // ============================================================================
        // TRANSICIONES DE ETAPAS DE LA TEMPORADA ACTIVA
        // ============================================================================

        [HttpPost]
        public ActionResult EvaluarInicial(int idParticipacion, int idIglesia, string estado, string motivo, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.AvanzarEtapa2(idParticipacion, estado, motivo, comentario, u.IdUsuario);
                
                if (!string.IsNullOrWhiteSpace(comentario))
                {
                    _iglesiaService.AgregarComentario(idIglesia, u.IdUsuario, comentario);
                }

                TempData["MensajeExito"] = "Evaluación inicial procesada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult AsignarVision(int idParticipacion, int idIglesia, int idEventoVision, PersonaIglesia pastor, PersonaIglesia lider)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.AsignarEventoVision(idParticipacion, idIglesia, idEventoVision, pastor, lider, u.IdUsuario);
                TempData["MensajeExito"] = "Evento de Presentación de la Visión asignado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al asignar visión: " + ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult AprobarElegibilidadBasica(int idParticipacion, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                // Validar que la asistencia al evento de Presentación de la Visión esté confirmada
                using (SqlConnection cnVal = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cnVal.Open();
                    string sqlCheck = @"
                        SELECT COUNT(1)
                        FROM dbo.EventosParticipacionIglesia ep
                        INNER JOIN dbo.Eventos e ON ep.IdEvento = e.IdEvento
                        WHERE ep.IdParticipacion = @IdPart
                          AND e.TipoEvento = 'Vision'
                          AND ep.Asistio = 1;";
                    using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cnVal))
                    {
                        cmdCheck.Parameters.AddWithValue("@IdPart", idParticipacion);
                        int asistenciaConfirmada = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (asistenciaConfirmada == 0)
                        {
                            TempData["MensajeError"] = "No se puede aprobar la iglesia aún. Primero debe confirmarse la asistencia de la iglesia al evento de Presentación de la Visión. Ingrese al evento correspondiente y confirme la asistencia antes de aprobar.";
                            return RedirectToAction("Detalle", new { id = idIglesia });
                        }
                    }
                }

                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (SqlTransaction tran = cn.BeginTransaction())
                    {
                        try
                        {
                            string sql = "UPDATE dbo.ParticipacionesIglesia SET EtapaActual = 4, EstadoEvaluacion = 'Aprobado', VisionAsistio = 1, VisionResultado = 'Continua' WHERE IdParticipacion = @IdPart;";
                            using (SqlCommand cmd = new SqlCommand(sql, cn, tran))
                            {
                                cmd.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmd.ExecuteNonQuery();
                            }

                            _iglesiaRepository.RegistrarLogHistorial(cn, tran, idParticipacion, "Aprobación Elegibilidad Taller", "Visión (Etapa 3)", "Elegible Taller (Etapa 4)", u.IdUsuario, "El CMI/CE aprobó la elegibilidad de la iglesia. Asistencia a Visión confirmada. Iglesia elegible para Taller OCC.");

                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
                TempData["MensajeExito"] = "Iglesia aprobada como elegible para el Taller OCC.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult ReabrirProceso(int idParticipacion, int idIglesia, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdPosicion != 1 && u.IdPosicion != 2 && u.IdPosicion != 3)
            {
                TempData["MensajeError"] = "Su usuario no tiene autorización para reabrir el proceso.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["MensajeError"] = "Debe proporcionar una justificación para reabrir el proceso.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sqlGet = "SELECT EtapaActual FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = @Id;";
                int etapaActual = 1;
                cn.Open();
                using (SqlCommand cmdGet = new SqlCommand(sqlGet, cn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", idParticipacion);
                    object val = cmdGet.ExecuteScalar();
                    if (val != null) etapaActual = Convert.ToInt32(val);
                }

                int etapaRetorno = etapaActual;
                if (etapaActual == 2) etapaRetorno = 1;
                else if (etapaActual == 3) etapaRetorno = 2;
                else if (etapaActual == 4) etapaRetorno = 3;

                string sql = @"
                    UPDATE dbo.ParticipacionesIglesia SET
                        EstadoEvaluacion = 'Pendiente',
                        EtapaActual = @EtapaRetorno,
                        EvalInicialEstado = CASE WHEN @EtapaRetorno = 1 THEN 'Pendiente' ELSE EvalInicialEstado END,
                        EvalTallerEstado = CASE WHEN @EtapaRetorno <= 3 THEN 'Pendiente' ELSE EvalTallerEstado END
                    WHERE IdParticipacion = @Id;";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@EtapaRetorno", etapaRetorno);
                    cmd.Parameters.AddWithValue("@Id", idParticipacion);
                    cmd.ExecuteNonQuery();
                }

                string sqlLog = @"
                    INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                    VALUES (@IdPart, GETDATE(), 'Reapertura de Proceso', 'Detenido/Rechazado', 'Pendiente', @IdUser, @Cmt);";
                using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                {
                    cmdLog.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmdLog.Parameters.AddWithValue("@IdUser", u.IdUsuario);
                    cmdLog.Parameters.AddWithValue("@Cmt", "Proceso Reabierto. Razón: " + comentario);
                    cmdLog.ExecuteNonQuery();
                }

                string sqlComentario = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                using (SqlCommand cmdCmt = new SqlCommand(sqlComentario, cn))
                {
                    cmdCmt.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmdCmt.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                    cmdCmt.Parameters.AddWithValue("@Comentario", "Reapertura de Proceso: " + comentario);
                    cmdCmt.ExecuteNonQuery();
                }
            }

            TempData["MensajeExito"] = "El proceso ha sido reabierto y restablecido al estado anterior.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult DetenerProceso(int idParticipacion, int idIglesia, string motivo, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlGet = "SELECT EstadoEvaluacion FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = @Id;";
                string estadoAnterior = "Desconocido";
                using (SqlCommand cmdGet = new SqlCommand(sqlGet, cn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", idParticipacion);
                    object val = cmdGet.ExecuteScalar();
                    if (val != null) estadoAnterior = val.ToString();
                }

                string sql = "UPDATE dbo.ParticipacionesIglesia SET EstadoEvaluacion = 'Detenido' WHERE IdParticipacion = @Id;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idParticipacion);
                    cmd.ExecuteNonQuery();
                }

                string histCmt = $"Proceso Detenido. Motivo: {motivo}. " + (!string.IsNullOrWhiteSpace(comentario) ? $"Notas: {comentario}" : "");
                string sqlLog = @"
                    INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                    VALUES (@IdPart, GETDATE(), 'Detención de Proceso', @EstadoAnt, 'Detenido', @IdUser, @Cmt);";
                using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                {
                    cmdLog.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmdLog.Parameters.AddWithValue("@EstadoAnt", estadoAnterior);
                    cmdLog.Parameters.AddWithValue("@IdUser", u.IdUsuario);
                    cmdLog.Parameters.AddWithValue("@Cmt", histCmt);
                    cmdLog.ExecuteNonQuery();
                }

                string sqlComentario = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                using (SqlCommand cmdCmt = new SqlCommand(sqlComentario, cn))
                {
                    cmdCmt.Parameters.AddWithValue("@IdIglesia", idIglesia);
                    cmdCmt.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                    cmdCmt.Parameters.AddWithValue("@Comentario", histCmt);
                    cmdCmt.ExecuteNonQuery();
                }
            }

            TempData["MensajeExito"] = "El proceso de la iglesia ha sido detenido exitosamente.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult RegistrarVision(int idParticipacion, int idIglesia, bool invitada, DateTime? fecha, string lugar, bool asistio, string resultado, int? idEventoTaller)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.AvanzarEtapa3(idParticipacion, invitada, fecha, lugar, asistio, resultado, u.IdUsuario, idEventoTaller);
                TempData["MensajeExito"] = "Datos de Presentación de la Visión guardados.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult EvaluarTallerOCC(int idParticipacion, int idIglesia, string estado, string motivo, string comentario, int? idEventoTaller = null, int? cantidadAsistentes = null, List<Maestro> maestrosNuevos = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.AvanzarEtapa4(idParticipacion, idIglesia, estado, motivo, comentario, u.IdUsuario, idEventoTaller, cantidadAsistentes, maestrosNuevos);
                TempData["MensajeExito"] = "Evaluación de elegibilidad para Taller OCC guardada.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult CompletarTallerOCC(int idParticipacion, int idIglesia, string tallerNombre, DateTime? tallerFecha, string tallerLugar, int cantNinos, int cantMaestrosReg, int cantMaestrosAsist, int cantMaestrosAus)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.AvanzarEtapa5(idParticipacion, tallerNombre, tallerFecha, tallerLugar, cantNinos, cantMaestrosReg, cantMaestrosAsist, cantMaestrosAus, u.IdUsuario);
                TempData["MensajeExito"] = "Taller OCC completado y registrado en el expediente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult AgregarComentario(int idIglesia, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            try
            {
                _iglesiaService.AgregarComentario(idIglesia, u.IdUsuario, comentario);
                TempData["MensajeExito"] = "Observación guardada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // IMPORTACIÓN MASIVA DESDE EXCEL / CSV
        // ============================================================================

        [HttpPost]
        public ActionResult ImportarMasivo(HttpPostedFileBase archivoExcel, int? idTemporadaImportar)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdPosicion != 1 && u.IdPosicion != 2)
            {
                TempData["MensajeError"] = "Tu rol no tiene permisos para realizar importaciones masivas de iglesias.";
                return RedirectToAction("Index");
            }

            if (archivoExcel == null || archivoExcel.ContentLength <= 0)
            {
                TempData["MensajeError"] = "Por favor selecciona un archivo válido.";
                return RedirectToAction("Index");
            }

            if (!idTemporadaImportar.HasValue || idTemporadaImportar.Value <= 0)
            {
                TempData["MensajeError"] = "Por favor selecciona una temporada de destino válida.";
                return RedirectToAction("Index");
            }

            // Validar que la temporada seleccionada exista en el sistema
            bool esTemporadaValida = false;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlCheck = "SELECT COUNT(1) FROM dbo.Temporadas WHERE IdTemporada = @Id;";
                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn))
                {
                    cmdCheck.Parameters.AddWithValue("@Id", idTemporadaImportar.Value);
                    int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (count > 0) esTemporadaValida = true;
                }
            }

            if (!esTemporadaValida)
            {
                TempData["MensajeError"] = "La temporada seleccionada no es válida o no existe en el sistema.";
                return RedirectToAction("Index");
            }

            string ext = Path.GetExtension(archivoExcel.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".csv")
            {
                TempData["MensajeError"] = "Formato de archivo no soportado. Debe ser .xlsx o .csv.";
                return RedirectToAction("Index");
            }

            string uploadPath = Server.MapPath("~/Uploads/Importaciones/");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            string filePath = Path.Combine(uploadPath, Guid.NewGuid().ToString() + ext);
            archivoExcel.SaveAs(filePath);

            int insertados = 0;
            int errores = 0;
            List<string> detalleErrores = new List<string>();

            try
            {
                if (ext == ".csv")
                {
                    // Lector nativo CSV
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string headerLine = reader.ReadLine(); // Saltar cabecera
                        string line;
                        int filaNum = 1;
                        while ((line = reader.ReadLine()) != null)
                        {
                            filaNum++;
                            string[] cols = line.Split(',');
                            if (cols.Length < 1 || string.IsNullOrWhiteSpace(cols[0])) continue;

                            try
                            {
                                Iglesia ig = MapearColumnasImport(cols);
                                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
                                {
                                    ig.IdEquipo = u.IdEquipo ?? 1;
                                }
                                _iglesiaService.RegistrarIglesia(ig, u.IdUsuario, idTemporadaImportar.Value);
                                insertados++;
                            }
                            catch (Exception ex)
                            {
                                errores++;
                                if (detalleErrores.Count < 10)
                                {
                                    detalleErrores.Add($"Fila {filaNum} ({cols[0]}): {ex.Message}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Lector OleDb Excel
                    string conString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1;\"";
                    using (OleDbConnection connExcel = new OleDbConnection(conString))
                    {
                        connExcel.Open();
                        DataTable dtSchema = connExcel.GetSchema("Tables");
                        if (dtSchema.Rows.Count > 0)
                        {
                            string sheetName = dtSchema.Rows[0]["TABLE_NAME"].ToString();
                            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + sheetName + "]", connExcel);
                            int filaNum = 1;
                            using (OleDbDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    filaNum++;
                                    string nombreIg = dr[0] != DBNull.Value ? dr[0].ToString().Trim() : "Iglesia Sin Nombre";
                                    if (string.IsNullOrWhiteSpace(nombreIg)) continue;

                                    try
                                    {
                                        Iglesia ig = MapearDataReaderImport(dr, u.IdEquipo ?? 1);
                                        if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
                                        {
                                            ig.IdEquipo = u.IdEquipo ?? 1;
                                        }
                                        _iglesiaService.RegistrarIglesia(ig, u.IdUsuario, idTemporadaImportar.Value);
                                        insertados++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errores++;
                                        if (detalleErrores.Count < 10)
                                        {
                                            detalleErrores.Add($"Fila {filaNum} ({nombreIg}): {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                string msg = $"Importación completada: {insertados} iglesias registradas exitosamente. Errores: {errores}.";
                if (detalleErrores.Any())
                {
                    msg += "<br/><strong>Detalle de Errores (primeros 10):</strong><br/>" + string.Join("<br/>", detalleErrores);
                }
                TempData["MensajeExito"] = msg;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error crítico durante la importación: " + ex.Message + " (Nota: Si es un archivo XLSX, asegúrate de tener instalado el controlador de Microsoft ACE OLEDB de 64 bits en tu servidor, o sube el archivo en formato CSV).";
            }
            finally
            {
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            return RedirectToAction("Index");
        }

        private Iglesia MapearColumnasImport(string[] cols)
        {
            // Mapeo seguro por índices de columnas
            Iglesia ig = new Iglesia
            {
                NombreIglesia = cols.Length > 0 ? cols[0].Trim() : "Iglesia Importada",
                RNC_Cedula = cols.Length > 1 ? cols[1].Trim() : "",
                Telefono = cols.Length > 2 ? cols[2].Trim() : "",
                Provincia = cols.Length > 3 ? cols[3].Trim() : "",
                Sector = cols.Length > 4 ? cols[4].Trim() : "",
                Calle = cols.Length > 5 ? cols[5].Trim() : "",
                Numero = cols.Length > 6 ? cols[6].Trim() : "",
                Referencia = cols.Length > 7 ? cols[7].Trim() : "",
                Denominacion = cols.Length > 8 ? cols[8].Trim() : "",
                TipoOrganizacion = cols.Length > 9 ? cols[9].Trim() : "",
                IdEquipo = 1 // Default
            };

            ig.Pastor = new PersonaIglesia
            {
                TipoPersona = "Pastor",
                Nombres = cols.Length > 10 ? cols[10].Trim() : "Pastor",
                Apellidos = "",
                Celular = cols.Length > 11 ? cols[11].Trim() : "",
                Correo = cols.Length > 12 ? cols[12].Trim() : ""
            };

            ig.LiderMinisterial = new PersonaIglesia
            {
                TipoPersona = "LiderMinisterial",
                Nombres = cols.Length > 13 ? cols[13].Trim() : "Lider",
                Apellidos = "",
                Celular = cols.Length > 14 ? cols[14].Trim() : "",
                Correo = cols.Length > 15 ? cols[15].Trim() : ""
            };

            ig.CantidadMaestros = cols.Length > 16 && int.TryParse(cols[16], out int m) ? (int?)m : null;
            ig.CantidadNinos = cols.Length > 17 && int.TryParse(cols[17], out int n) ? (int?)n : null;

            return ig;
        }

        private Iglesia MapearDataReaderImport(OleDbDataReader dr, int defaultIdEquipo)
        {
            Iglesia ig = new Iglesia
            {
                NombreIglesia = dr[0] != DBNull.Value ? dr[0].ToString().Trim() : "Iglesia Importada",
                RNC_Cedula = dr[1] != DBNull.Value ? dr[1].ToString().Trim() : "",
                Telefono = dr[2] != DBNull.Value ? dr[2].ToString().Trim() : "",
                Provincia = dr[3] != DBNull.Value ? dr[3].ToString().Trim() : "",
                Sector = dr[4] != DBNull.Value ? dr[4].ToString().Trim() : "",
                Calle = dr[5] != DBNull.Value ? dr[5].ToString().Trim() : "",
                Numero = dr[6] != DBNull.Value ? dr[6].ToString().Trim() : "",
                Referencia = dr[7] != DBNull.Value ? dr[7].ToString().Trim() : "",
                Denominacion = dr[8] != DBNull.Value ? dr[8].ToString().Trim() : "",
                TipoOrganizacion = dr[9] != DBNull.Value ? dr[9].ToString().Trim() : "",
                IdEquipo = defaultIdEquipo
            };

            ig.Pastor = new PersonaIglesia
            {
                TipoPersona = "Pastor",
                Nombres = dr[10] != DBNull.Value ? dr[10].ToString().Trim() : "Pastor",
                Apellidos = "",
                Celular = dr[11] != DBNull.Value ? dr[11].ToString().Trim() : "",
                Correo = dr[12] != DBNull.Value ? dr[12].ToString().Trim() : ""
            };

            ig.LiderMinisterial = new PersonaIglesia
            {
                TipoPersona = "LiderMinisterial",
                Nombres = dr[13] != DBNull.Value ? dr[13].ToString().Trim() : "Lider",
                Apellidos = "",
                Celular = dr[14] != DBNull.Value ? dr[14].ToString().Trim() : "",
                Correo = dr[15] != DBNull.Value ? dr[15].ToString().Trim() : ""
            };

            ig.CantidadMaestros = dr[16] != DBNull.Value && int.TryParse(dr[16].ToString(), out int m) ? (int?)m : null;
            ig.CantidadNinos = dr[17] != DBNull.Value && int.TryParse(dr[17].ToString(), out int n) ? (int?)n : null;

            return ig;
        }

        // ============================================================================
        // MÉTODOS AUXILIARES DE COMPROBACIÓN DE ROLES
        // ============================================================================

        private bool PuedeRegistrarIglesia(Usuario u)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;
            if (u.IdPosicion == 1 || u.IdPosicion == 2) return true; // CE o CMI
            return false;
        }

        private bool PuedeEditarIglesia(Usuario u, int idEquipoIglesia)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin
            
            // Todos los demás usuarios (coordinadores CE, CMI, CD) deben pertenecer al mismo equipo o ser un equipo padre del equipo de la iglesia
            if (u.IdEquipo.HasValue)
            {
                if (u.IdEquipo.Value == idEquipoIglesia) return true;
                return EsEquipoHijo(u.IdEquipo.Value, idEquipoIglesia);
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

        private void CargarEquiposDisponibles(Usuario u)
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT e.IdEquipo, e.NombreEquipo, n.NombreNivel FROM dbo.Equipos e INNER JOIN dbo.NivelesEquipo n ON e.IdNivelEquipo = n.IdNivelEquipo WHERE e.Activo = 1 ORDER BY n.RangoJerarquico, e.NombreEquipo;";
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

        private void CargarCatalogosDenominacionesYTipos()
        {
            List<SelectListItem> denominaciones = new List<SelectListItem>();
            List<SelectListItem> tiposOrg = new List<SelectListItem>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Denominaciones
                string sqlD = @"
                    IF OBJECT_ID('dbo.Denominaciones', 'U') IS NOT NULL
                        SELECT Nombre FROM dbo.Denominaciones WHERE Activo = 1 ORDER BY Nombre;
                    ELSE
                        SELECT 'Bautista' AS Nombre;";
                using (SqlCommand cmdD = new SqlCommand(sqlD, cn))
                using (SqlDataReader drD = cmdD.ExecuteReader())
                {
                    while (drD.Read())
                    {
                        string nom = drD["Nombre"].ToString();
                        denominaciones.Add(new SelectListItem { Value = nom, Text = nom });
                    }
                }

                // Tipos Organización
                string sqlT = @"
                    IF OBJECT_ID('dbo.TiposOrganizacion', 'U') IS NOT NULL
                        SELECT Nombre FROM dbo.TiposOrganizacion WHERE Activo = 1 ORDER BY Nombre;
                    ELSE
                        SELECT 'Iglesia Local' AS Nombre;";
                using (SqlCommand cmdT = new SqlCommand(sqlT, cn))
                using (SqlDataReader drT = cmdT.ExecuteReader())
                {
                    while (drT.Read())
                    {
                        string nom = drT["Nombre"].ToString();
                        tiposOrg.Add(new SelectListItem { Value = nom, Text = nom });
                    }
                }
            }

            ViewBag.ListaDenominaciones = denominaciones;
            ViewBag.ListaTiposOrganizacion = tiposOrg;
        }

        private void CargarCombosFiltros()
        {
            List<SelectListItem> temporadas = new List<SelectListItem>();
            List<SelectListItem> importarTemporadas = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // 1. Obtener la temporada activa y anterior para etiquetarlas
                int idActiva = 0;
                DateTime? fechaInicioActiva = null;
                string sqlActiva = "SELECT TOP 1 IdTemporada, FechaInicio FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                using (SqlCommand cmdActiva = new SqlCommand(sqlActiva, cn))
                {
                    using (SqlDataReader dr = cmdActiva.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            idActiva = Convert.ToInt32(dr["IdTemporada"]);
                            fechaInicioActiva = dr["FechaInicio"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["FechaInicio"]) : null;
                        }
                    }
                }

                int idAnterior = 0;
                if (idActiva > 0 && fechaInicioActiva.HasValue)
                {
                    string sqlAnterior = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas WHERE FechaInicio < @FechaInicioActiva ORDER BY FechaInicio DESC;";
                    using (SqlCommand cmdAnterior = new SqlCommand(sqlAnterior, cn))
                    {
                        cmdAnterior.Parameters.AddWithValue("@FechaInicioActiva", fechaInicioActiva.Value);
                        object val = cmdAnterior.ExecuteScalar();
                        if (val != null) idAnterior = Convert.ToInt32(val);
                    }
                }

                // 2. Obtener TODAS las temporadas de la base de datos
                string sqlAll = "SELECT IdTemporada, NombreTemporada FROM dbo.Temporadas ORDER BY FechaInicio DESC;";
                using (SqlCommand cmdAll = new SqlCommand(sqlAll, cn))
                {
                    using (SqlDataReader dr = cmdAll.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int idTemp = Convert.ToInt32(dr["IdTemporada"]);
                            string nombre = dr["NombreTemporada"].ToString();
                            string label = nombre;

                            if (idTemp == idActiva)
                            {
                                label += " (Actual / En Curso)";
                            }
                            else if (idTemp == idAnterior)
                            {
                                label += " (Anterior)";
                            }

                            // Filtro de iglesias (todas)
                            temporadas.Add(new SelectListItem
                            {
                                Value = idTemp.ToString(),
                                Text = nombre
                            });

                            // Combo de importación (todas con etiquetas de ayuda)
                            importarTemporadas.Add(new SelectListItem
                            {
                                Value = idTemp.ToString(),
                                Text = label
                            });
                        }
                    }
                }
            }
            ViewBag.FiltroTemporadas = temporadas;
            ViewBag.ImportarTemporadas = importarTemporadas;

            ViewBag.FiltroEtapas = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Etapa 1: Inscrita" },
                new SelectListItem { Value = "2", Text = "Etapa 2: Evaluada" },
                new SelectListItem { Value = "3", Text = "Etapa 3: Visión" },
                new SelectListItem { Value = "4", Text = "Etapa 4: Elegible Taller" },
                new SelectListItem { Value = "5", Text = "Etapa 5: Taller OCC" },
                new SelectListItem { Value = "6", Text = "Etapa 6: Asignación" },
                new SelectListItem { Value = "7", Text = "Etapa 7: Aprobación Final" }
            };

            ViewBag.FiltroEstados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente de Aprobación" },
                new SelectListItem { Value = "Aprobado", Text = "Aprobada" },
                new SelectListItem { Value = "Rechazado", Text = "Rechazada / Suspendida" }
            };
        }

        [HttpGet]
        public JsonResult VerificarUnicidadPersona(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                return Json(new { existe = false }, JsonRequestBehavior.AllowGet);
            }

            string cleanCedula = cedula.Replace("-", "").Replace(" ", "").Trim();
            
            int idTemporadaActiva = 0;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null) idTemporadaActiva = Convert.ToInt32(val);
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT 'Lider' AS Rol, i.NombreIglesia, e.NombreEquipo, ne.NombreNivel
                    FROM dbo.PersonasIglesia per
                    INNER JOIN dbo.Iglesias i ON per.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    INNER JOIN dbo.NivelesEquipo ne ON e.IdNivelEquipo = ne.IdNivelEquipo
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    WHERE p.IdTemporada = @IdTemp 
                      AND REPLACE(per.DocumentoIdentidad, '-', '') = @Doc
                    
                    UNION ALL
                    
                    SELECT 'Maestro' AS Rol, i.NombreIglesia, e.NombreEquipo, ne.NombreNivel
                    FROM dbo.Maestros m
                    INNER JOIN dbo.Iglesias i ON m.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    INNER JOIN dbo.NivelesEquipo ne ON e.IdNivelEquipo = ne.IdNivelEquipo
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    WHERE p.IdTemporada = @IdTemp 
                      AND REPLACE(m.DocumentoIdentidad, '-', '') = @Doc AND m.Activo = 1;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdTemp", idTemporadaActiva);
                cmd.Parameters.AddWithValue("@Doc", cleanCedula);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return Json(new {
                            existe = true,
                            rol = dr["Rol"].ToString(),
                            iglesia = dr["NombreIglesia"].ToString(),
                            equipo = dr["NombreEquipo"].ToString(),
                            nivel = dr["NombreNivel"].ToString()
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(new { existe = false }, JsonRequestBehavior.AllowGet);
        }

        // ============================================================================
        // EDICIÓN DE IGLESIAS (GET y POST)
        // ============================================================================

        // GET: Iglesia/Editar/5
        public ActionResult Editar(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(id);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "Su usuario no tiene autorización para editar este expediente.";
                return RedirectToAction("Detalle", new { id = id });
            }

            int idTemporadaActiva = 0;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null) idTemporadaActiva = Convert.ToInt32(val);
            }
            bool esTemporadaActual = (iglesia.ParticipacionActual != null && iglesia.ParticipacionActual.IdTemporada == idTemporadaActiva);

            CargarEquiposDisponibles(u);
            CargarCatalogosDenominacionesYTipos();
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u) && esTemporadaActual;
            return View(iglesia);
        }

        // POST: Iglesia/Editar
        [HttpPost]
        public ActionResult Editar(Iglesia modelo, HttpPostedFileBase docPastor, HttpPostedFileBase docLider)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesiaOriginal = _iglesiaService.ObtenerExpedienteIglesia(modelo.IdIglesia);
            if (iglesiaOriginal == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesiaOriginal.IdEquipo))
            {
                TempData["MensajeError"] = "Su usuario no tiene autorización para realizar esta edición.";
                return RedirectToAction("Detalle", new { id = modelo.IdIglesia });
            }

            int idTemporadaActiva = 0;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null) idTemporadaActiva = Convert.ToInt32(val);
            }
            bool esTemporadaActual = (iglesiaOriginal.ParticipacionActual != null && iglesiaOriginal.ParticipacionActual.IdTemporada == idTemporadaActiva);

            CargarEquiposDisponibles(u);
            CargarCatalogosDenominacionesYTipos();

            // Validar si intentó cambiar de equipo y no tiene permiso
            bool cambioDeEquipo = (modelo.IdEquipo != iglesiaOriginal.IdEquipo);
            if (cambioDeEquipo && (!PuedeCambiarEquipo(u) || !esTemporadaActual))
            {
                // Revertimos al equipo original
                modelo.IdEquipo = iglesiaOriginal.IdEquipo;
                cambioDeEquipo = false;
            }

            // Validar formatos del formulario
            string errorValidacion;
            if (!ValidarFormatosDR(modelo, out errorValidacion))
            {
                TempData["MensajeError"] = errorValidacion;
                CargarEquiposDisponibles(u);
                ViewBag.UsuarioActual = u;
                ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u);
                return View(modelo);
            }

            string uploadPath = Server.MapPath("~/Uploads/Iglesias/");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // Manejo de archivos adjuntos
            if (docPastor != null && docPastor.ContentLength > 0)
            {
                string ext = Path.GetExtension(docPastor.FileName);
                string fileName = $"Pastor_{Guid.NewGuid()}{ext}";
                docPastor.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.Pastor.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }
            else
            {
                modelo.Pastor.DocumentoAdjuntoRuta = iglesiaOriginal.Pastor?.DocumentoAdjuntoRuta;
            }

            if (docLider != null && docLider.ContentLength > 0)
            {
                string ext = Path.GetExtension(docLider.FileName);
                string fileName = $"Lider_{Guid.NewGuid()}{ext}";
                docLider.SaveAs(Path.Combine(uploadPath, fileName));
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = "/Uploads/Iglesias/" + fileName;
            }
            else
            {
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = iglesiaOriginal.LiderMinisterial?.DocumentoAdjuntoRuta;
            }

            try
            {
                // Guardar cambios en BD
                _iglesiaService.ActualizarIglesia(modelo, u.IdUsuario);

                // Si se cambió de equipo, registrar notificaciones para coordinadores/movilizadores de ambos equipos
                if (cambioDeEquipo)
                {
                    using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                    {
                        cn.Open();
                        using (SqlTransaction tran = cn.BeginTransaction())
                        {
                            try
                            {
                                RegistrarNotificacionReasignacion(cn, tran, modelo.IdIglesia, modelo.NombreIglesia, iglesiaOriginal.IdEquipo, modelo.IdEquipo);
                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                            }
                        }
                    }
                }

                TempData["MensajeExito"] = "Expediente de la iglesia actualizado exitosamente.";
                return RedirectToAction("Detalle", new { id = modelo.IdIglesia });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al actualizar el expediente: " + ex.Message;
                CargarEquiposDisponibles(u);
                ViewBag.UsuarioActual = u;
                ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u);
                return View(modelo);
            }
        }

        private bool PuedeCambiarEquipo(Usuario u)
        {
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin
            if (u.RangoJerarquico == 2) return true; // ERLE
            return false;
        }

        private bool ValidarFormatosDR(Iglesia modelo, out string error)
        {
            error = "";
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(809|829|849)[-]?\d{3}[-]?\d{4}$");
            var cedulaRegex = new System.Text.RegularExpressions.Regex(@"^(\d{11}|\d{3}-\d{7}-\d{1})$");
            var rncCedulaRegex = new System.Text.RegularExpressions.Regex(@"^(\d{9}|\d{11}|\d{3}-\d{7}-\d{1})$");

            // Validar Iglesia
            if (string.IsNullOrWhiteSpace(modelo.NombreIglesia)) { error = "El nombre de la iglesia es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.RNC_Cedula) || !rncCedulaRegex.IsMatch(modelo.RNC_Cedula.Trim())) { error = "El RNC/Cédula es requerido y debe tener 9 dígitos (RNC) u 11 dígitos (Cédula)."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Telefono) || !phoneRegex.IsMatch(modelo.Telefono.Trim())) { error = "El teléfono de la iglesia es requerido y debe ser un número dominicano válido (809/829/849)."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Calle)) { error = "La calle de la dirección es obligatoria."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Numero)) { error = "El número de la dirección es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Sector)) { error = "El sector es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Ciudad)) { error = "La ciudad/provincia es obligatoria."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Referencia)) { error = "La referencia de ubicación es obligatoria."; return false; }

            // Validar sección ministerial
            if (!modelo.CantidadMaestros.HasValue || modelo.CantidadMaestros.Value < 0) { error = "La cantidad de maestros es obligatoria y debe ser mayor o igual a 0."; return false; }
            if (!modelo.CantidadNinos.HasValue || modelo.CantidadNinos.Value < 0) { error = "La cantidad proyectada de niños es obligatoria y debe ser mayor o igual a 0."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Denominacion)) { error = "La denominación es obligatoria."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.TipoOrganizacion)) { error = "El tipo de organización es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Ref1Nombre)) { error = "El nombre de la Referencia 1 es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Ref1Contacto) || !phoneRegex.IsMatch(modelo.Ref1Contacto.Trim())) { error = "El contacto de la Referencia 1 debe ser un teléfono dominicano válido (809/829/849)."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Ref2Nombre)) { error = "El nombre de la Referencia 2 es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Ref2Contacto) || !phoneRegex.IsMatch(modelo.Ref2Contacto.Trim())) { error = "El contacto de la Referencia 2 debe ser un teléfono dominicano válido (809/829/849)."; return false; }

            // Validar Pastor
            if (modelo.Pastor == null) { error = "Los datos del Pastor son obligatorios."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Pastor.Nombres) || string.IsNullOrWhiteSpace(modelo.Pastor.Apellidos)) { error = "El nombre del Pastor es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Pastor.DocumentoIdentidad) || !cedulaRegex.IsMatch(modelo.Pastor.DocumentoIdentidad.Trim())) { error = "La cédula del Pastor es obligatoria (11 dígitos)."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Pastor.Celular) || !phoneRegex.IsMatch(modelo.Pastor.Celular.Trim())) { error = "El celular del Pastor debe ser un teléfono dominicano válido."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.Pastor.Correo) || !modelo.Pastor.Correo.Contains("@")) { error = "El correo electrónico del Pastor debe ser válido."; return false; }

            // Validar Líder
            if (modelo.LiderMinisterial == null) { error = "Los datos del Líder Ministerial son obligatorios."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Nombres) || string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Apellidos)) { error = "El nombre del Líder es obligatorio."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.LiderMinisterial.DocumentoIdentidad) || !cedulaRegex.IsMatch(modelo.LiderMinisterial.DocumentoIdentidad.Trim())) { error = "La cédula del Líder es obligatoria (11 dígitos)."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Celular) || !phoneRegex.IsMatch(modelo.LiderMinisterial.Celular.Trim())) { error = "El celular del Líder debe ser un teléfono dominicano válido."; return false; }
            if (string.IsNullOrWhiteSpace(modelo.LiderMinisterial.Correo) || !modelo.LiderMinisterial.Correo.Contains("@")) { error = "El correo electrónico del Líder debe ser válido."; return false; }

            return true;
        }

        private void RegistrarNotificacionReasignacion(SqlConnection cn, SqlTransaction tran, int idIglesia, string nombreIglesia, int idEquipoAnterior, int idEquipoNuevo)
        {
            string nombreEqAnterior = "Equipo Anterior";
            string nombreEqNuevo = "Equipo Nuevo";

            string sqlEq = "SELECT IdEquipo, NombreEquipo FROM dbo.Equipos WHERE IdEquipo IN (@IdAnterior, @IdNuevo);";
            using (SqlCommand cmdEq = new SqlCommand(sqlEq, cn, tran))
            {
                cmdEq.Parameters.AddWithValue("@IdAnterior", idEquipoAnterior);
                cmdEq.Parameters.AddWithValue("@IdNuevo", idEquipoNuevo);
                using (SqlDataReader dr = cmdEq.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int id = Convert.ToInt32(dr["IdEquipo"]);
                        string nombre = dr["NombreEquipo"].ToString();
                        if (id == idEquipoAnterior) nombreEqAnterior = nombre;
                        if (id == idEquipoNuevo) nombreEqNuevo = nombre;
                    }
                }
            }

            string sqlTable = @"
                IF OBJECT_ID('dbo.Notificaciones', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Notificaciones (
                        IdNotificacion INT IDENTITY(1,1) PRIMARY KEY,
                        IdUsuarioDestinatario INT NOT NULL,
                        Mensaje NVARCHAR(MAX) NOT NULL,
                        FechaCreacion DATETIME DEFAULT GETDATE(),
                        Leida BIT DEFAULT 0,
                        FechaLectura DATETIME NULL,
                        IdUsuarioLectura INT NULL
                    );
                END";
            using (SqlCommand cmdTable = new SqlCommand(sqlTable, cn, tran)) { cmdTable.ExecuteNonQuery(); }

            List<int> usuariosNotificar = new List<int>();
            string sqlUsers = "SELECT u.IdUsuario FROM dbo.Usuarios u INNER JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario WHERE a.Activo = 1 AND a.IdEquipo IN (@IdAnterior, @IdNuevo) AND a.IdPosicion IN (1, 2, 3) AND u.IdEstado = 4;";
            using (SqlCommand cmdUsers = new SqlCommand(sqlUsers, cn, tran))
            {
                cmdUsers.Parameters.AddWithValue("@IdAnterior", idEquipoAnterior);
                cmdUsers.Parameters.AddWithValue("@IdNuevo", idEquipoNuevo);
                using (SqlDataReader dr = cmdUsers.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        usuariosNotificar.Add(Convert.ToInt32(dr["IdUsuario"]));
                    }
                }
            }

            string msg = $"Notificación: La iglesia '{nombreIglesia}' ha sido reasignada del equipo '{nombreEqAnterior}' al equipo '{nombreEqNuevo}'.";
            string sqlInsert = "INSERT INTO dbo.Notificaciones (IdUsuarioDestinatario, Mensaje) VALUES (@IdDest, @Msg);";
            foreach (var userId in usuariosNotificar.Distinct())
            {
                using (SqlCommand cmdIns = new SqlCommand(sqlInsert, cn, tran))
                {
                    cmdIns.Parameters.AddWithValue("@IdDest", userId);
                    cmdIns.Parameters.AddWithValue("@Msg", msg);
                    cmdIns.ExecuteNonQuery();
                }
            }
        }

        public static List<Notificacion> ObtenerNotificacionesUsuario(int idUsuario)
        {
            List<Notificacion> lista = new List<Notificacion>();
            try
            {
                string connStr = @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
                if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
                    connStr = ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;

                using (SqlConnection cn = new SqlConnection(connStr))
                {
                    cn.Open();
                    string sqlTable = @"
                        IF OBJECT_ID('dbo.Notificaciones', 'U') IS NULL
                        BEGIN
                            CREATE TABLE dbo.Notificaciones (
                                IdNotificacion INT IDENTITY(1,1) PRIMARY KEY,
                                IdUsuarioDestinatario INT NOT NULL,
                                Mensaje NVARCHAR(MAX) NOT NULL,
                                FechaCreacion DATETIME DEFAULT GETDATE(),
                                Leida BIT DEFAULT 0,
                                FechaLectura DATETIME NULL,
                                IdUsuarioLectura INT NULL
                            );
                        END";
                    using (SqlCommand cmdTable = new SqlCommand(sqlTable, cn)) { cmdTable.ExecuteNonQuery(); }

                    string sql = "SELECT * FROM dbo.Notificaciones WHERE IdUsuarioDestinatario = @IdUser AND Leida = 0 ORDER BY IdNotificacion DESC;";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@IdUser", idUsuario);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new Notificacion
                                {
                                    IdNotificacion = Convert.ToInt32(dr["IdNotificacion"]),
                                    IdUsuarioDestinatario = Convert.ToInt32(dr["IdUsuarioDestinatario"]),
                                    Mensaje = dr["Mensaje"].ToString(),
                                    FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"]),
                                    Leida = Convert.ToBoolean(dr["Leida"])
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return lista;
        }

        [HttpPost]
        public ActionResult MarcarNotificacionLeida(int idNotificacion)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "UPDATE dbo.Notificaciones SET Leida = 1, FechaLectura = GETDATE(), IdUsuarioLectura = @IdUser WHERE IdNotificacion = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@IdUser", u.IdUsuario);
                cmd.Parameters.AddWithValue("@Id", idNotificacion);
                cn.Open();
                cmd.ExecuteNonQuery();
            }

            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }
    }
}
