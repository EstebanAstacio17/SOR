using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;
using SOR.Helpers;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class EventosController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            return SOR.Helpers.ConnectionHelper.ObtenerCadenaConexion();
        }

        // GET: Eventos
        public ActionResult Index(string tipo = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            List<Evento> lista = new List<Evento>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT e.*, t.NombreTemporada, u.Correo AS CorreoCreador, a.IdEquipo AS IdEquipoCreador,
                           ed.EstadoDespachoEvento,
                           ISNULL(m.TotalAsignadas, 0) AS TotalIglesiasAsignadas,
                           ISNULL(m.TotalDespachadas, 0) AS TotalIglesiasDespachadas
                    FROM dbo.Eventos e
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Usuarios u ON e.IdUsuarioCreacion = u.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                    LEFT JOIN dbo.EventosDespacho ed ON e.IdEvento = ed.IdEvento
                    LEFT JOIN (
                        SELECT d.IdEvento,
                               COUNT(d.IdDespachoIglesia) AS TotalAsignadas,
                               SUM(CASE WHEN d.EstadoDespacho = 'DESPACHADA' THEN 1 ELSE 0 END) AS TotalDespachadas
                        FROM dbo.DespachosIglesia d
                        GROUP BY d.IdEvento
                    ) m ON e.IdEvento = m.IdEvento
                    ORDER BY e.Fecha DESC;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Evento
                        {
                            IdEvento = Convert.ToInt32(dr["IdEvento"]),
                            NombreEvento = dr["NombreEvento"].ToString(),
                            TipoEvento = dr["TipoEvento"].ToString(),
                            IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                            NombreTemporada = dr["NombreTemporada"].ToString(),
                            Fecha = Convert.ToDateTime(dr["Fecha"]),
                            Lugar = dr["Lugar"] != DBNull.Value ? dr["Lugar"].ToString() : "",
                            Responsable = dr["Responsable"] != DBNull.Value ? dr["Responsable"].ToString() : "",
                            TipoLugar = dr["TipoLugar"] != DBNull.Value ? dr["TipoLugar"].ToString() : "",
                            Hora = dr["Hora"] != DBNull.Value ? dr["Hora"].ToString() : "",
                            CantidadAsistentes = dr["CantidadAsistentes"] != DBNull.Value ? Convert.ToInt32(dr["CantidadAsistentes"]) : 0,
                            IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]),
                            IdEquipoCreador = dr["IdEquipoCreador"] != DBNull.Value ? Convert.ToInt32(dr["IdEquipoCreador"]) : (int?)null,
                            CorreoCreador = dr["CorreoCreador"].ToString(),
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"]),
                            RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null,
                            EstadoDespachoEvento = dr["EstadoDespachoEvento"] != DBNull.Value ? dr["EstadoDespachoEvento"].ToString() : (dr["TipoEvento"].ToString() == "Despacho" ? "PROGRAMADO" : null),
                            TotalIglesiasAsignadas = dr["TotalIglesiasAsignadas"] != DBNull.Value ? Convert.ToInt32(dr["TotalIglesiasAsignadas"]) : 0,
                            TotalIglesiasDespachadas = dr["TotalIglesiasDespachadas"] != DBNull.Value ? Convert.ToInt32(dr["TotalIglesiasDespachadas"]) : 0
                        });
                    }
                }
            }

            HashSet<int> equiposPermitidos = new HashSet<int>();
            if (u != null && u.IdEquipo.HasValue)
            {
                equiposPermitidos.Add(u.IdEquipo.Value);
                ObtenerEquiposHijosRecursivo(u.IdEquipo.Value, equiposPermitidos);
            }
            ViewBag.EquiposPermitidos = equiposPermitidos;

            CargarTemporadasYTipos(u);
            ViewBag.UsuarioActual = u;
            ViewBag.FiltroTipoInicial = tipo;
            return View(lista);
        }

        // POST: Eventos/Crear
        [HttpPost]
        public ActionResult Crear(Evento modelo)
        {
            Usuario u = (Usuario)Session["usuario"];

            if (modelo == null || string.IsNullOrWhiteSpace(modelo.NombreEvento) || string.IsNullOrWhiteSpace(modelo.TipoEvento) || modelo.Fecha == DateTime.MinValue)
            {
                TempData["MensajeError"] = "El nombre, tipo de evento y fecha son obligatorios.";
                return RedirectToAction("Index");
            }

            // Si no se asignÃ³ temporada, obtener la activa
            if (modelo.IdTemporada <= 0)
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cn.Open();
                    object valObj = cmd.ExecuteScalar();
                    if (valObj != null)
                    {
                        modelo.IdTemporada = Convert.ToInt32(valObj);
                    }
                    else
                    {
                        TempData["MensajeError"] = "No hay ninguna temporada registrada en el sistema. Configura una primero.";
                        return RedirectToAction("Index");
                    }
                }
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    INSERT INTO dbo.Eventos (NombreEvento, TipoEvento, IdTemporada, Fecha, Lugar, Responsable, IdUsuarioCreacion, TipoLugar, Hora, CantidadAsistentes) 
                    OUTPUT INSERTED.IdEvento
                    VALUES (@Nombre, @Tipo, @IdTemp, @Fecha, @Lugar, @Resp, @IdUsuario, @TipoLugar, @Hora, @Cant);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Nombre", modelo.NombreEvento));
                cmd.Parameters.Add(new SqlParameter("@Tipo", modelo.TipoEvento));
                cmd.Parameters.Add(new SqlParameter("@IdTemp", modelo.IdTemporada));
                cmd.Parameters.Add(new SqlParameter("@Fecha", modelo.Fecha));
                cmd.Parameters.Add(new SqlParameter("@Lugar", modelo.Lugar ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Resp", modelo.Responsable ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                cmd.Parameters.Add(new SqlParameter("@TipoLugar", modelo.TipoLugar ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Hora", modelo.Hora ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Cant", modelo.CantidadAsistentes));

                cn.Open();
                int idNuevoEvento = Convert.ToInt32(cmd.ExecuteScalar());

                if (modelo.TipoEvento == "Despacho")
                {
                    int idEq = u.IdEquipo.GetValueOrDefault(1);
                    int? idAlmacen = null;
                    if (!string.IsNullOrEmpty(modelo.Lugar))
                    {
                        using (SqlCommand cmdFindAlm = new SqlCommand("SELECT TOP 1 IdAlmacen FROM dbo.Almacenes WHERE NombreAlmacen = @Nom OR @Nom LIKE '%' + NombreAlmacen + '%';", cn))
                        {
                            cmdFindAlm.Parameters.Add(new SqlParameter("@Nom", modelo.Lugar));
                            object almObj = cmdFindAlm.ExecuteScalar();
                            if (almObj != null && almObj != DBNull.Value)
                            {
                                idAlmacen = Convert.ToInt32(almObj);
                            }
                        }
                    }

                    string sqlED = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.EventosDespacho WHERE IdEvento = @IdEv)
                        BEGIN
                            INSERT INTO dbo.EventosDespacho (IdEvento, IdEquipo, IdAlmacen, EstadoDespachoEvento)
                            VALUES (@IdEv, @IdEq, @IdAlm, 'PROGRAMADO');
                        END";
                    using (SqlCommand cmdED = new SqlCommand(sqlED, cn))
                    {
                        cmdED.Parameters.Add(new SqlParameter("@IdEv", idNuevoEvento));
                        cmdED.Parameters.Add(new SqlParameter("@IdEq", idEq));
                        cmdED.Parameters.Add(new SqlParameter("@IdAlm", idAlmacen.HasValue ? (object)idAlmacen.Value : DBNull.Value));
                        cmdED.ExecuteNonQuery();
                    }
                }
            }

            TempData["MensajeExito"] = "Evento creado con Ã©xito.";
            return RedirectToAction("Index");
        }

        // GET: Eventos/Detalle/5
        public ActionResult Detalle(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            Evento evento = null;

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT e.*, t.NombreTemporada, u.Correo AS CorreoCreador
                    FROM dbo.Eventos e
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Usuarios u ON e.IdUsuarioCreacion = u.IdUsuario
                    WHERE e.IdEvento = @Id;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Id", id));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        evento = new Evento
                        {
                            IdEvento = Convert.ToInt32(dr["IdEvento"]),
                            NombreEvento = dr["NombreEvento"].ToString(),
                            TipoEvento = dr["TipoEvento"].ToString(),
                            IdTemporada = Convert.ToInt32(dr["IdTemporada"]),
                            NombreTemporada = dr["NombreTemporada"].ToString(),
                            Fecha = Convert.ToDateTime(dr["Fecha"]),
                            Lugar = dr["Lugar"] != DBNull.Value ? dr["Lugar"].ToString() : "",
                            Responsable = dr["Responsable"] != DBNull.Value ? dr["Responsable"].ToString() : "",
                            TipoLugar = dr["TipoLugar"] != DBNull.Value ? dr["TipoLugar"].ToString() : "",
                            Hora = dr["Hora"] != DBNull.Value ? dr["Hora"].ToString() : "",
                            CantidadAsistentes = dr["CantidadAsistentes"] != DBNull.Value ? Convert.ToInt32(dr["CantidadAsistentes"]) : 0,
                            IdUsuarioCreacion = Convert.ToInt32(dr["IdUsuarioCreacion"]),
                            CorreoCreador = dr["CorreoCreador"].ToString(),
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"]),
                            RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null
                        };
                    }
                }
            }

            if (evento == null)
            {
                return HttpNotFound();
            }

            // Si es un evento de Despacho, cargar datos logÃ­sticos
            if (evento.TipoEvento == "Despacho")
            {
                var logisticaSvc = new SOR.Services.LogisticaService();
                var eventoDespacho = logisticaSvc.ObtenerDetalleEventoDespacho(id);
                if (eventoDespacho == null)
                {
                    int idEquipo = u.IdEquipo.GetValueOrDefault(1);
                    logisticaSvc.CrearEventoDespacho(id, idEquipo, null, u.IdUsuario);
                    eventoDespacho = logisticaSvc.ObtenerDetalleEventoDespacho(id);
                }
                ViewBag.EventoDespacho = eventoDespacho;
                int idEqDisp = u.IdEquipo.GetValueOrDefault(1);
                ViewBag.IglesiasDisponibles = logisticaSvc.ObtenerIglesiasDisponiblesDespacho(idEqDisp, evento.IdTemporada);
            }

            // Cargar iglesias que participan en este evento
            List<IglesiaParticipacionViewModel> iglesias = ObtenerIglesiasParticipantes(id, evento.IdTemporada);
            // Cargar maestros de estas iglesias
            List<MaestroAsistenciaViewModel> maestros = ObtenerMaestrosYAsistencia(id, evento.IdTemporada);
            // Cargar asistencia de coordinadores al evento
            List<CoordinadorEventoAsistenciaViewModel> coordinadoresAsistentes = ObtenerCoordinadoresAsistentesEvento(id);
            List<CoordinadorDropdownItem> listaCoordinadoresDisponibles = ObtenerCoordinadoresParaDropdown(id);
            List<string> listaRolesEvento = ObtenerRolesEventoActivos();

            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && (u.NombrePosicion.IndexOf("LogÃ­stica", StringComparison.OrdinalIgnoreCase) >= 0 || u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0)));
            bool esCE = u != null && (u.IdPosicion == 1 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0));

            ViewBag.Evento = evento;
            ViewBag.Iglesias = iglesias;
            ViewBag.Maestros = maestros;
            ViewBag.CoordinadoresAsistentes = coordinadoresAsistentes;
            ViewBag.ListaCoordinadoresDisponibles = listaCoordinadoresDisponibles;
            ViewBag.ListaRolesEvento = listaRolesEvento;
            ViewBag.UsuarioActual = u;
            ViewBag.EsAdmin = esAdmin;
            ViewBag.EsCL = esCL || esCE;
            ViewBag.EsCE = esCE;
            ViewBag.PuedeEditar = PuedeEditarEvento(u, id);

            return View();
        }

        // =====================================================================
        // MÃ‰TODOS DE DESPACHO PRESENCIAL (EVENTOS DE DESPACHO)
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProgramarIglesiaDespacho(int idEvento, int idParticipacion, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                var logisticaSvc = new SOR.Services.LogisticaService();
                int idEquipo = u.IdEquipo.GetValueOrDefault(1);
                int idTemporada = 0;
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT IdTemporada FROM dbo.Eventos WHERE IdEvento=@Id;", cn))
                    {
                        cmd.Parameters.Add(new SqlParameter("@Id", idEvento));
                        idTemporada = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                logisticaSvc.ProgramarIglesiaEnDespacho(idEvento, idParticipacion, idIglesia, idEquipo, idTemporada, u.IdUsuario);
                TempData["MensajeExito"] = "Iglesia agregada exitosamente al evento de despacho.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarDespacho(ConfirmarDespachoViewModel vm, int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
                bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && (u.NombrePosicion.IndexOf("LogÃ­stica", StringComparison.OrdinalIgnoreCase) >= 0 || u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0)));
                bool esCE = u != null && (u.IdPosicion == 1 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0));

                if (!esAdmin && !esCL && !esCE)
                {
                    TempData["MensajeError"] = "Acceso denegado: Ãšnicamente el Coordinador de LogÃ­stica (CL) o el Coordinador de Equipo (CE) tienen autorizaciÃ³n para confirmar y ejecutar el despacho de materiales.";
                    return RedirectToAction("Detalle", new { id = idEvento });
                }

                var logisticaSvc = new SOR.Services.LogisticaService();
                int idEquipo = u.IdEquipo.GetValueOrDefault(1);
                int idTemporada = 0;
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT IdTemporada FROM dbo.Eventos WHERE IdEvento=@Id;", cn))
                    {
                        cmd.Parameters.Add(new SqlParameter("@Id", idEvento));
                        idTemporada = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                string nombre = !string.IsNullOrEmpty(u.PrimerNombre) ? $"{u.PrimerNombre} {u.PrimerApellido}".Trim() : (u.Correo ?? "Coordinador de LogÃ­stica");
                logisticaSvc.ConfirmarDespacho(vm, idEquipo, idTemporada, u.IdUsuario, nombre, u.IdRolSeguridad, u.IdPosicion);
                TempData["MensajeExito"] = "Despacho presencial confirmado exitosamente con cÃ©dula validada.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarNoDespacho(NoDespachoBecauseViewModel vm, int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                var logisticaSvc = new SOR.Services.LogisticaService();
                logisticaSvc.MarcarNoDespacho(vm, u.IdUsuario);
                TempData["MensajeExito"] = "Iglesia registrada como NO DESPACHADA. No se descontÃ³ inventario y queda disponible para reprogramaciÃ³n.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpGet]
        public JsonResult ObtenerDespachoMateriales(int id)
        {
            var logisticaSvc = new SOR.Services.LogisticaService();
            var despacho = logisticaSvc.ObtenerDespachoDetalle(id);
            if (despacho == null) return Json(new { materiales = new List<object>() }, JsonRequestBehavior.AllowGet);
            
            var mats = new List<object>();
            foreach (var m in despacho.Materiales)
            {
                mats.Add(new
                {
                    m.IdMaterial,
                    m.CodigoMaterial,
                    m.NombreMaterial,
                    m.UnidadEntrega,
                    m.CantidadAsignada,
                    m.CantidadDespachada
                });
            }

            return Json(new
            {
                idDespacho = despacho.IdDespachoIglesia,
                nombreIglesia = despacho.NombreIglesia,
                nombrePastor = despacho.NombrePastor ?? "",
                cedulaPastor = despacho.CedulaPastor ?? "",
                telefonoPastor = despacho.TelefonoPastor ?? "",
                nombreLider = despacho.NombreLiderMinisterial ?? "",
                cedulaLider = despacho.CedulaLiderMinisterial ?? "",
                telefonoLider = despacho.TelefonoLiderMinisterial ?? "",
                tipoReceptor = despacho.TipoReceptor ?? "PASTOR",
                nombreReceptor = despacho.NombreReceptor ?? "",
                documentoIdentidadReceptor = despacho.DocumentoIdentidadReceptor ?? "",
                telefonoReceptor = despacho.TelefonoReceptor ?? "",
                materiales = mats
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ComprobanteDespacho(int id)
        {
            var logisticaSvc = new SOR.Services.LogisticaService();
            var modelo = logisticaSvc.ObtenerDespachoDetalle(id);
            if (modelo == null) return HttpNotFound();
            ViewBag.UsuarioActual = (Usuario)Session["usuario"];
            return View("~/Views/Logistica/ComprobanteDespachoIglesia.cshtml", modelo);
        }

        // POST: Eventos/RegistrarAsistenciaIglesia
        [HttpPost]
        public ActionResult RegistrarAsistenciaIglesia(int idEvento, int idParticipacion, bool asistio)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar la asistencia de un evento fuera de su equipo o jurisdicciÃ³n.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Verificar si existe registro
                string sqlCheck = "SELECT COUNT(1) FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn);
                cmdCheck.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                cmdCheck.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                int count = Convert.ToInt32(cmdCheck.ExecuteScalar());

                if (count > 0)
                {
                    string sqlUpdate = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                    SqlCommand cmdUp = new SqlCommand(sqlUpdate, cn);
                    cmdUp.Parameters.Add(new SqlParameter("@Asistio", asistio));
                    cmdUp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cmdUp.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdUp.ExecuteNonQuery();
                }
                else
                {
                    string sqlIns = "INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEvento, @IdPart, @Asistio);";
                    SqlCommand cmdIns = new SqlCommand(sqlIns, cn);
                    cmdIns.Parameters.Add(new SqlParameter("@Asistio", asistio));
                    cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdIns.ExecuteNonQuery();
                }

                // Sincronizar el estado de asistencia y resultado de VisiÃ³n en dbo.ParticipacionesIglesia
                string sqlSync = @"
                    UPDATE p
                    SET p.VisionAsistio = @Asistio,
                        p.VisionResultado = CASE 
                            WHEN @Asistio = 1 AND (p.VisionResultado IS NULL OR p.VisionResultado = '' OR p.VisionResultado = 'Pendiente') THEN 'Continua'
                            WHEN @Asistio = 0 AND (p.VisionResultado = 'Continua') THEN 'Pendiente'
                            ELSE p.VisionResultado
                        END
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.EventosParticipacionIglesia ep ON p.IdParticipacion = ep.IdParticipacion
                    INNER JOIN dbo.Eventos e ON ep.IdEvento = e.IdEvento
                    WHERE ep.IdEvento = @IdEvento AND ep.IdParticipacion = @IdPart AND e.TipoEvento = 'Vision';";
                using (SqlCommand cmdSync = new SqlCommand(sqlSync, cn))
                {
                    cmdSync.Parameters.Add(new SqlParameter("@Asistio", asistio));
                    cmdSync.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cmdSync.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdSync.ExecuteNonQuery();
                }
            }

            TempData["MensajeExito"] = "Asistencia de iglesia actualizada.";
            return RedirectToAction("Detalle", new { id = idEvento });
        }

        // POST: Eventos/RegistrarAsistenciaMaestros
        [HttpPost]
        public ActionResult RegistrarAsistenciaMaestros(int idEvento, List<int> maestrosAsistentes)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar la asistencia de maestros en un evento fuera de su equipo o jurisdicciÃ³n.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            if (maestrosAsistentes == null) maestrosAsistentes = new List<int>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Borrar asistencias anteriores para este evento
                        string sqlDel = "DELETE FROM dbo.AsistenciaMaestro WHERE IdEvento = @IdEvento;";
                        using (SqlCommand cmdDel = new SqlCommand(sqlDel, cn, tran))
                        {
                            cmdDel.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdDel.ExecuteNonQuery();
                        }

                        // 2. Insertar asistencias marcadas
                        string sqlIns = @"
                            INSERT INTO dbo.AsistenciaMaestro (IdMaestro, IdEvento, Asistio, IdUsuarioRegistro) 
                            VALUES (@IdMaestro, @IdEvento, 1, @IdUsuario);";

                        foreach (int idMaestro in maestrosAsistentes)
                        {
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.Add(new SqlParameter("@IdMaestro", idMaestro));
                                cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdIns.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                                cmdIns.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia de maestros guardada correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                    }
                }
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        private List<IglesiaParticipacionViewModel> ObtenerIglesiasParticipantes(int idEvento, int idTemporada)
        {
            List<IglesiaParticipacionViewModel> lista = new List<IglesiaParticipacionViewModel>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT p.IdParticipacion, i.IdIglesia, i.NombreIglesia, e.NombreEquipo,
                           ep.Asistio
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    INNER JOIN dbo.EventosParticipacionIglesia ep ON p.IdParticipacion = ep.IdParticipacion
                    WHERE ep.IdEvento = @IdEvento AND p.IdTemporada = @IdTemporada;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", idTemporada));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new IglesiaParticipacionViewModel
                        {
                            IdParticipacion = Convert.ToInt32(dr["IdParticipacion"]),
                            IdIglesia = Convert.ToInt32(dr["IdIglesia"]),
                            NombreIglesia = dr["NombreIglesia"].ToString(),
                            NombreEquipo = dr["NombreEquipo"].ToString(),
                            Asistio = Convert.ToBoolean(dr["Asistio"])
                        });
                    }
                }
            }

            if (!lista.Any()) return lista;

            var idsIglesias = lista.Select(x => x.IdIglesia).Distinct().ToList();
            var idsParticipaciones = lista.Select(x => x.IdParticipacion).Distinct().ToList();

            // OptimizaciÃ³n de Alto Rendimiento: Cargar pastores, lÃ­deres, maestros y asistentes en consultas por lote
            using (SqlConnection cnBatch = new SqlConnection(ObtenerCadenaConexion()))
            {
                cnBatch.Open();

                // 1. Cargar Pastores y LÃ­deres en una sola consulta
                string sqlPersonas = $@"
                    SELECT IdPersonaIglesia, IdIglesia, TipoPersona, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo
                    FROM dbo.PersonasIglesia
                    WHERE IdIglesia IN ({string.Join(",", idsIglesias)}) 
                      AND TipoPersona IN ('Pastor', 'LiderMinisterial');";

                using (SqlCommand cmdP = new SqlCommand(sqlPersonas, cnBatch))
                using (SqlDataReader drP = cmdP.ExecuteReader())
                {
                    while (drP.Read())
                    {
                        int idIg = Convert.ToInt32(drP["IdIglesia"]);
                        string tipo = drP["TipoPersona"].ToString();
                        var target = lista.FirstOrDefault(x => x.IdIglesia == idIg);
                        if (target != null)
                        {
                            var persona = new PersonaIglesia
                            {
                                IdPersonaIglesia = Convert.ToInt32(drP["IdPersonaIglesia"]),
                                IdIglesia = idIg,
                                TipoPersona = tipo,
                                Nombres = drP["Nombres"].ToString(),
                                Apellidos = drP["Apellidos"].ToString(),
                                DocumentoIdentidad = drP["DocumentoIdentidad"] != DBNull.Value ? drP["DocumentoIdentidad"].ToString() : "",
                                Celular = drP["Celular"] != DBNull.Value ? drP["Celular"].ToString() : "",
                                Correo = drP["Correo"] != DBNull.Value ? drP["Correo"].ToString() : ""
                            };

                            if (tipo == "Pastor") target.Pastor = persona;
                            else if (tipo == "LiderMinisterial") target.LiderMinisterial = persona;
                        }
                    }
                }

                // 2. Cargar Maestros en una sola consulta
                string sqlMaestros = $@"
                    SELECT IdMaestro, IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo
                    FROM dbo.Maestros
                    WHERE IdIglesia IN ({string.Join(",", idsIglesias)}) AND Activo = 1;";

                using (SqlCommand cmdM = new SqlCommand(sqlMaestros, cnBatch))
                using (SqlDataReader drM = cmdM.ExecuteReader())
                {
                    while (drM.Read())
                    {
                        int idIg = Convert.ToInt32(drM["IdIglesia"]);
                        var target = lista.FirstOrDefault(x => x.IdIglesia == idIg);
                        target?.Maestros.Add(new Maestro
                        {
                            IdMaestro = Convert.ToInt32(drM["IdMaestro"]),
                            IdIglesia = idIg,
                            Nombres = drM["Nombres"].ToString(),
                            Apellidos = drM["Apellidos"].ToString(),
                            DocumentoIdentidad = drM["DocumentoIdentidad"] != DBNull.Value ? drM["DocumentoIdentidad"].ToString() : "",
                            Celular = drM["Celular"] != DBNull.Value ? drM["Celular"].ToString() : "",
                            Correo = drM["Correo"] != DBNull.Value ? drM["Correo"].ToString() : "",
                            Activo = Convert.ToBoolean(drM["Activo"])
                        });
                    }
                }

                // 3. Cargar Asistentes en una sola consulta
                string sqlAsist = $@"
                    SELECT IdAsistente, IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo
                    FROM dbo.EventosAsistentes
                    WHERE IdEvento = @IdEvento AND IdParticipacion IN ({string.Join(",", idsParticipaciones)})
                    ORDER BY IdAsistente ASC;";

                using (SqlCommand cmdA = new SqlCommand(sqlAsist, cnBatch))
                {
                    cmdA.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    using (SqlDataReader drA = cmdA.ExecuteReader())
                    {
                        while (drA.Read())
                        {
                            int idPart = Convert.ToInt32(drA["IdParticipacion"]);
                            var target = lista.FirstOrDefault(x => x.IdParticipacion == idPart);
                            target?.AsistentesDetalle.Add(new EventoAsistenteViewModel
                            {
                                IdAsistente = Convert.ToInt32(drA["IdAsistente"]),
                                IdEvento = Convert.ToInt32(drA["IdEvento"]),
                                IdParticipacion = idPart,
                                NombreCompleto = drA["NombreCompleto"].ToString(),
                                Identificacion = drA["Identificacion"] != DBNull.Value ? drA["Identificacion"].ToString() : "",
                                Telefono = drA["Telefono"] != DBNull.Value ? drA["Telefono"].ToString() : "",
                                Correo = drA["Correo"] != DBNull.Value ? drA["Correo"].ToString() : ""
                            });
                        }
                    }
                }
            }

            // Asegurar objetos por defecto no nulos
            foreach (var item in lista)
            {
                item.Pastor = item.Pastor ?? new PersonaIglesia { TipoPersona = "Pastor", Nombres = "", Apellidos = "", DocumentoIdentidad = "", Celular = "", Correo = "" };
                item.LiderMinisterial = item.LiderMinisterial ?? new PersonaIglesia { TipoPersona = "LiderMinisterial", Nombres = "", Apellidos = "", DocumentoIdentidad = "", Celular = "", Correo = "" };
            }

            return lista;
        }

        private List<MaestroAsistenciaViewModel> ObtenerMaestrosYAsistencia(int idEvento, int idTemporada)
        {
            List<MaestroAsistenciaViewModel> lista = new List<MaestroAsistenciaViewModel>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT m.IdMaestro, m.Nombres, m.Apellidos, i.NombreIglesia,
                           IIF(am.IdAsistencia IS NOT NULL, 1, 0) AS Asistio
                    FROM dbo.Maestros m
                    INNER JOIN dbo.Iglesias i ON m.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    LEFT JOIN dbo.AsistenciaMaestro am ON m.IdMaestro = am.IdMaestro AND am.IdEvento = @IdEvento
                    WHERE p.IdTemporada = @IdTemporada AND p.EstadoEvaluacion = 'Aprobado' AND m.Activo = 1;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                cmd.Parameters.Add(new SqlParameter("@IdTemporada", idTemporada));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new MaestroAsistenciaViewModel
                        {
                            IdMaestro = Convert.ToInt32(dr["IdMaestro"]),
                            NombreCompleto = dr["Nombres"].ToString() + " " + dr["Apellidos"].ToString(),
                            NombreIglesia = dr["NombreIglesia"].ToString(),
                            Asistio = Convert.ToBoolean(dr["Asistio"])
                        });
                    }
                }
            }
            return lista;
        }

        private void CargarTemporadasYTipos(Usuario u = null)
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdTemporada, NombreTemporada FROM dbo.Temporadas ORDER BY IdTemporada DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new SelectListItem
                        {
                            Value = dr["IdTemporada"].ToString(),
                            Text = dr["NombreTemporada"].ToString()
                        });
                    }
                }
            }
            ViewBag.ListaTemporadas = lista;

            ViewBag.ListaTipos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Vision", Text = "PresentaciÃ³n de la VisiÃ³n" },
                new SelectListItem { Value = "Taller", Text = "Taller OCC" },
                new SelectListItem { Value = "Despacho", Text = "Despacho de Materiales" },
                new SelectListItem { Value = "Evangelistico", Text = "Evento EvangelÃ­stico" },
                new SelectListItem { Value = "GranAventura", Text = "La Gran Aventura" }
            };

            List<SelectListItem> integrantes = new List<SelectListItem>();
            if (u != null && u.IdEquipo.HasValue)
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sqlInt = @"
                        SELECT u.IdUsuario, u.Correo, p.PrimerNombre, p.PrimerApellido
                        FROM dbo.Usuarios u
                        INNER JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                        LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                        WHERE a.IdEquipo = @IdEquipo AND u.IdEstado = 4;";
                    SqlCommand cmdInt = new SqlCommand(sqlInt, cn);
                    cmdInt.Parameters.Add(new SqlParameter("@IdEquipo", u.IdEquipo.Value));
                    cn.Open();
                    using (SqlDataReader drInt = cmdInt.ExecuteReader())
                    {
                        while (drInt.Read())
                        {
                            string pNombre = drInt["PrimerNombre"] != DBNull.Value ? drInt["PrimerNombre"].ToString() : "";
                            string pApellido = drInt["PrimerApellido"] != DBNull.Value ? drInt["PrimerApellido"].ToString() : "";
                            string nombreComp = (!string.IsNullOrEmpty(pNombre) ? $"{pNombre} {pApellido}" : drInt["Correo"].ToString()).Trim();
                            integrantes.Add(new SelectListItem { Value = nombreComp, Text = nombreComp });
                        }
                    }
                }
            }
            ViewBag.ListaIntegrantesEquipo = integrantes;

            List<SelectListItem> almacenes = new List<SelectListItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sqlAlm = "SELECT IdAlmacen, NombreAlmacen, Direccion FROM dbo.Almacenes WHERE Activo = 1 ORDER BY NombreAlmacen ASC;";
                SqlCommand cmdAlm = new SqlCommand(sqlAlm, cn);
                cn.Open();
                using (SqlDataReader drAlm = cmdAlm.ExecuteReader())
                {
                    while (drAlm.Read())
                    {
                        string nom = drAlm["NombreAlmacen"].ToString();
                        almacenes.Add(new SelectListItem
                        {
                            Value = nom,
                            Text = nom
                        });
                    }
                }
            }
            ViewBag.ListaAlmacenes = almacenes;
        }

        // POST: Eventos/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Evento modelo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (modelo == null || modelo.IdEvento <= 0)
            {
                TempData["MensajeError"] = "Datos de evento invÃ¡lidos.";
                return RedirectToAction("Index");
            }

            if (!PuedeEditarEvento(u, modelo.IdEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar eventos pertenecientes a otro equipo o jurisdicciÃ³n.";
                return RedirectToAction("Index");
            }

            // Verificar que el evento pertenece a la temporada activa
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlCheck = @"SELECT t.Activa FROM dbo.Eventos e INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada WHERE e.IdEvento = @IdEvento;";
                SqlCommand cmdC = new SqlCommand(sqlCheck, cn);
                cmdC.Parameters.Add(new SqlParameter("@IdEvento", modelo.IdEvento));
                object activa = cmdC.ExecuteScalar();
                if (activa == null || !Convert.ToBoolean(activa))
                {
                    TempData["MensajeError"] = "Solo se pueden editar eventos de la temporada activa.";
                    return RedirectToAction("Index");
                }

                string sql = @"
                    UPDATE dbo.Eventos SET
                        NombreEvento = @Nombre,
                        TipoEvento = @Tipo,
                        Fecha = @Fecha,
                        Lugar = @Lugar,
                        Responsable = @Resp,
                        TipoLugar = @TipoLugar,
                        Hora = @Hora,
                        CantidadAsistentes = @Cant,
                        FechaModificacion = GETUTCDATE(),
                        UsuarioModificacion = @IdUsuario
                    WHERE IdEvento = @IdEvento
                      AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Nombre", modelo.NombreEvento));
                cmd.Parameters.Add(new SqlParameter("@Tipo", modelo.TipoEvento));
                cmd.Parameters.Add(new SqlParameter("@Fecha", modelo.Fecha));
                cmd.Parameters.Add(new SqlParameter("@Lugar", modelo.Lugar ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Resp", modelo.Responsable ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@TipoLugar", modelo.TipoLugar ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Hora", modelo.Hora ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Cant", modelo.CantidadAsistentes));
                cmd.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                var pRowVer = new SqlParameter("@RowVersion", SqlDbType.Timestamp);
                pRowVer.Value = (modelo.RowVersion != null && modelo.RowVersion.Length > 0) ? (object)modelo.RowVersion : DBNull.Value;
                cmd.Parameters.Add(pRowVer);
                cmd.Parameters.Add(new SqlParameter("@IdEvento", modelo.IdEvento));

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    TempData["MensajeError"] = "Conflicto de concurrencia: El evento fue modificado concurrentemente por otro usuario. Actualice la informaciÃ³n antes de continuar.";
                    return RedirectToAction("Index");
                }
            }

            SOR.Helpers.AuditoriaHelper.Registrar(u.IdUsuario, u.Correo, "UPDATE", "Evento", modelo.IdEvento.ToString(), "EdiciÃ³n de evento: " + modelo.NombreEvento);
            TempData["MensajeExito"] = "Evento actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // POST: Eventos/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para eliminar eventos pertenecientes a otro equipo o jurisdicciÃ³n.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Verificar que el evento pertenece a temporada activa
                string sqlCheck = @"SELECT t.Activa FROM dbo.Eventos e INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada WHERE e.IdEvento = @IdEvento;";
                SqlCommand cmdC = new SqlCommand(sqlCheck, cn);
                cmdC.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                object activa = cmdC.ExecuteScalar();
                if (activa == null || !Convert.ToBoolean(activa))
                {
                    TempData["MensajeError"] = "Solo se pueden eliminar eventos de la temporada activa.";
                    return RedirectToAction("Index");
                }

                try
                {
                    using (SqlCommand cmdSp = new SqlCommand("dbo.SpEliminarEvento", cn))
                    {
                        cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                        cmdSp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                        cmdSp.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                    return RedirectToAction("Index");
                }
            }

            TempData["MensajeExito"] = "Evento eliminado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult GuardarAsistenciaVision(int idEvento, int idParticipacion, int idIglesia, PersonaIglesia pastor, bool? pastorAsistio, PersonaIglesia lider, bool? liderAsistio)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar este evento.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar Pastor
                        if (pastor != null && !string.IsNullOrWhiteSpace(pastor.Nombres))
                        {
                            ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "Pastor", pastor);
                        }

                        // 2. Actualizar LÃ­der Ministerial
                        if (lider != null && !string.IsNullOrWhiteSpace(lider.Nombres))
                        {
                            ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "LiderMinisterial", lider);
                        }

                        // 3. Limpiar asistentes anteriores de esta iglesia en este evento
                        string sqlDel = "DELETE FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdDel = new SqlCommand(sqlDel, cn, tran))
                        {
                            cmdDel.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdDel.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdDel.ExecuteNonQuery();
                        }

                        int asistieronCount = 0;

                        // 4. Registrar Pastor como asistente si aplica
                        if (pastorAsistio == true)
                        {
                            string sqlIns = @"
                                INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                cmdIns.Parameters.Add(new SqlParameter("@Nombre", $"{pastor.Nombres} {pastor.Apellidos}".Trim()));
                                cmdIns.Parameters.Add(new SqlParameter("@Doc", pastor.DocumentoIdentidad ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Tel", pastor.Celular ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Correo", pastor.Correo ?? (object)DBNull.Value));
                                cmdIns.ExecuteNonQuery();
                            }
                            asistieronCount++;
                        }

                        // 5. Registrar LÃ­der como asistente si aplica
                        if (liderAsistio == true)
                        {
                            string sqlIns = @"
                                INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                cmdIns.Parameters.Add(new SqlParameter("@Nombre", $"{lider.Nombres} {lider.Apellidos}".Trim()));
                                cmdIns.Parameters.Add(new SqlParameter("@Doc", lider.DocumentoIdentidad ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Tel", lider.Celular ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Correo", lider.Correo ?? (object)DBNull.Value));
                                cmdIns.ExecuteNonQuery();
                            }
                            asistieronCount++;
                        }

                        // 6. Actualizar Asistio en EventosParticipacionIglesia
                        bool asistioCualquiera = (asistieronCount > 0);
                        string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdUp = new SqlCommand(sqlUpPart, cn, tran))
                        {
                            cmdUp.Parameters.Add(new SqlParameter("@Asistio", asistioCualquiera ? 1 : 0));
                            cmdUp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdUp.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdUp.ExecuteNonQuery();
                        }

                        // 7. Sincronizar ParticipacionesIglesia
                        string sqlSync = @"
                            UPDATE dbo.ParticipacionesIglesia
                            SET VisionAsistio = @Asistio,
                                VisionResultado = CASE WHEN @Asistio = 1 THEN 'Continua' ELSE 'Pendiente' END
                            WHERE IdParticipacion = @IdPart;";
                        using (SqlCommand cmdSync = new SqlCommand(sqlSync, cn, tran))
                        {
                            cmdSync.Parameters.Add(new SqlParameter("@Asistio", asistioCualquiera ? 1 : 0));
                            cmdSync.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdSync.ExecuteNonQuery();
                        }

                        // 8. Actualizar cantidad de asistentes en evento
                        string sqlUpCant = "UPDATE dbo.Eventos SET CantidadAsistentes = (SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento) WHERE IdEvento = @IdEvento;";
                        using (SqlCommand cmdUpCant = new SqlCommand(sqlUpCant, cn, tran))
                        {
                            cmdUpCant.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdUpCant.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia y datos del Pastor/LÃ­der actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                    }
                }
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        public ActionResult GuardarAsistenciaTaller(int idEvento, int idParticipacion, int idIglesia, PersonaIglesia lider, bool? liderAsistio, List<Maestro> maestros, List<int> maestrosAsistieronIds)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar este evento.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar LÃ­der
                        if (lider != null && !string.IsNullOrWhiteSpace(lider.Nombres))
                        {
                            ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "LiderMinisterial", lider);
                        }

                        // 2. Procesar Maestros (Insertar nuevos o actualizar existentes)
                        List<int> todosLosMaestrosAsistieronIds = new List<int>();
                        if (maestrosAsistieronIds != null)
                        {
                            todosLosMaestrosAsistieronIds.AddRange(maestrosAsistieronIds);
                        }

                        if (maestros != null)
                        {
                            for (int i = 0; i < maestros.Count; i++)
                            {
                                var m = maestros[i];
                                if (string.IsNullOrWhiteSpace(m.Nombres)) continue;

                                if (m.IdMaestro > 0)
                                {
                                    // Actualizar
                                    string sqlUpM = @"
                                        UPDATE dbo.Maestros 
                                        SET Nombres = @Nombres, Apellidos = @Apellidos, DocumentoIdentidad = @Doc, Celular = @Cel, Correo = @Correo
                                        WHERE IdMaestro = @IdM;";
                                    using (SqlCommand cmdUpM = new SqlCommand(sqlUpM, cn, tran))
                                    {
                                        cmdUpM.Parameters.Add(new SqlParameter("@Nombres", m.Nombres.Trim()));
                                        cmdUpM.Parameters.Add(new SqlParameter("@Apellidos", m.Apellidos ?? ""));
                                        cmdUpM.Parameters.Add(new SqlParameter("@Doc", m.DocumentoIdentidad ?? (object)DBNull.Value));
                                        cmdUpM.Parameters.Add(new SqlParameter("@Cel", m.Celular ?? (object)DBNull.Value));
                                        cmdUpM.Parameters.Add(new SqlParameter("@Correo", m.Correo ?? (object)DBNull.Value));
                                        cmdUpM.Parameters.Add(new SqlParameter("@IdM", m.IdMaestro));
                                        cmdUpM.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    // Insertar
                                    string sqlInsM = @"
                                        INSERT INTO dbo.Maestros (IdIglesia, Nombres, Apellidos, DocumentoIdentidad, Celular, Correo, Activo)
                                        VALUES (@IdIglesia, @Nombres, @Apellidos, @Doc, @Cel, @Correo, 1);
                                        SELECT SCOPE_IDENTITY();";
                                    using (SqlCommand cmdInsM = new SqlCommand(sqlInsM, cn, tran))
                                    {
                                        cmdInsM.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                                        cmdInsM.Parameters.Add(new SqlParameter("@Nombres", m.Nombres.Trim()));
                                        cmdInsM.Parameters.Add(new SqlParameter("@Apellidos", m.Apellidos ?? ""));
                                        cmdInsM.Parameters.Add(new SqlParameter("@Doc", m.DocumentoIdentidad ?? (object)DBNull.Value));
                                        cmdInsM.Parameters.Add(new SqlParameter("@Cel", m.Celular ?? (object)DBNull.Value));
                                        cmdInsM.Parameters.Add(new SqlParameter("@Correo", m.Correo ?? (object)DBNull.Value));
                                        int newMId = Convert.ToInt32(cmdInsM.ExecuteScalar());

                                        // Si venÃ­a marcado como asistido en el checkbox correspondiente
                                        // lo agregamos a la lista
                                        string asistKey = Request.Form["maestroNuevoAsistio_" + i];
                                        if (asistKey == "true")
                                        {
                                            todosLosMaestrosAsistieronIds.Add(newMId);
                                        }
                                    }
                                }
                            }
                        }

                        // 3. Limpiar asistentes anteriores de esta iglesia en este evento
                        string sqlDel = "DELETE FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdDel = new SqlCommand(sqlDel, cn, tran))
                        {
                            cmdDel.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdDel.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdDel.ExecuteNonQuery();
                        }

                        int asistieronCount = 0;

                        // 4. Registrar LÃ­der como asistente si aplica
                        if (liderAsistio == true)
                        {
                            string sqlIns = @"
                                INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                cmdIns.Parameters.Add(new SqlParameter("@Nombre", $"{lider.Nombres} {lider.Apellidos}".Trim()));
                                cmdIns.Parameters.Add(new SqlParameter("@Doc", lider.DocumentoIdentidad ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Tel", lider.Celular ?? (object)DBNull.Value));
                                cmdIns.Parameters.Add(new SqlParameter("@Correo", lider.Correo ?? (object)DBNull.Value));
                                cmdIns.ExecuteNonQuery();
                            }
                            asistieronCount++;
                        }

                        // 5. Registrar Maestros que asistieron
                        foreach (int mId in todosLosMaestrosAsistieronIds)
                        {
                            string nomComp = null;
                            string doc = null;
                            string tel = null;
                            string mail = null;
                            bool found = false;

                            // Obtener los datos del maestro
                            string sqlGetM = "SELECT Nombres, Apellidos, DocumentoIdentidad, Celular, Correo FROM dbo.Maestros WHERE IdMaestro = @IdM;";
                            using (SqlCommand cmdGetM = new SqlCommand(sqlGetM, cn, tran))
                            {
                                cmdGetM.Parameters.Add(new SqlParameter("@IdM", mId));
                                using (SqlDataReader dr = cmdGetM.ExecuteReader())
                                {
                                    if (dr.Read())
                                    {
                                        nomComp = $"{dr["Nombres"]} {dr["Apellidos"]}".Trim();
                                        doc = dr["DocumentoIdentidad"] != DBNull.Value ? dr["DocumentoIdentidad"].ToString() : "";
                                        tel = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : "";
                                        mail = dr["Correo"] != DBNull.Value ? dr["Correo"].ToString() : "";
                                        found = true;
                                    }
                                }
                            }

                            if (found)
                            {
                                // Insertar en EventosAsistentes
                                string sqlIns = @"
                                    INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                    VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                                using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                                {
                                    cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                    cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                    cmdIns.Parameters.Add(new SqlParameter("@Nombre", nomComp));
                                    cmdIns.Parameters.Add(new SqlParameter("@Doc", string.IsNullOrWhiteSpace(doc) ? (object)DBNull.Value : doc));
                                    cmdIns.Parameters.Add(new SqlParameter("@Tel", string.IsNullOrWhiteSpace(tel) ? (object)DBNull.Value : tel));
                                    cmdIns.Parameters.Add(new SqlParameter("@Correo", string.IsNullOrWhiteSpace(mail) ? (object)DBNull.Value : mail));
                                    cmdIns.ExecuteNonQuery();
                                }
                                asistieronCount++;
                            }
                        }

                        // 6. Actualizar Asistio en EventosParticipacionIglesia
                        bool asistioCualquiera = (asistieronCount > 0);
                        string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdUp = new SqlCommand(sqlUpPart, cn, tran))
                        {
                            cmdUp.Parameters.Add(new SqlParameter("@Asistio", asistioCualquiera ? 1 : 0));
                            cmdUp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdUp.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdUp.ExecuteNonQuery();
                        }

                        // 7. Sincronizar ParticipacionesIglesia usando Stored Procedure
                        using (SqlCommand cmdSp = new SqlCommand("dbo.SpAvanzarEtapaTaller", cn, tran))
                        {
                            cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                            cmdSp.Parameters.Add(new SqlParameter("@IdParticipacion", idParticipacion));
                            cmdSp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdSp.Parameters.Add(new SqlParameter("@Asistio", asistioCualquiera));
                            cmdSp.Parameters.Add(new SqlParameter("@IdUsuarioResponsable", u.IdUsuario));
                            cmdSp.ExecuteNonQuery();
                        }

                        // 8. Actualizar cantidad de asistentes en evento
                        string sqlUpCant = "UPDATE dbo.Eventos SET CantidadAsistentes = (SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento) WHERE IdEvento = @IdEvento;";
                        using (SqlCommand cmdUpCant = new SqlCommand(sqlUpCant, cn, tran))
                        {
                            cmdUpCant.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdUpCant.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia y datos del LÃ­der/Maestros actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                    }
                }
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EliminarMaestroDeAsistencia(int idEvento, int idAsistente)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null)
            {
                return Json(new { success = false, message = "SesiÃ³n invÃ¡lida o expirada." });
            }

            if (!PuedeEditarEvento(u, idEvento))
            {
                return Json(new { success = false, message = "No tiene permisos para modificar la asistencia de este evento." });
            }

            if (idEvento <= 0 || idAsistente <= 0)
            {
                return Json(new { success = false, message = "ParÃ¡metros invÃ¡lidos para la eliminaciÃ³n." });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (SqlTransaction tran = cn.BeginTransaction())
                    {
                        try
                        {
                            // 0. Validar si el evento ya pasÃ³
                            DateTime fechaEvento = DateTime.MinValue;
                            string sqlFecha = "SELECT Fecha FROM dbo.Eventos WHERE IdEvento = @IdEvento;";
                            using (SqlCommand cmdF = new SqlCommand(sqlFecha, cn, tran))
                            {
                                cmdF.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                object valF = cmdF.ExecuteScalar();
                                if (valF != null && valF != DBNull.Value)
                                {
                                    fechaEvento = Convert.ToDateTime(valF);
                                }
                            }

                            if (fechaEvento != DateTime.MinValue && fechaEvento.Date < DateTime.Today && u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
                            {
                                tran.Rollback();
                                return Json(new { success = false, message = "El evento ya se llevÃ³ a cabo el " + fechaEvento.ToString("dd/MM/yyyy") + ". No se pueden retirar participantes de un evento que ya pasÃ³." });
                            }

                            // 1. Obtener datos del asistente en EventosAsistentes
                            int idParticipacion = 0;
                            string nombreCompleto = "";
                            string identificacion = "";

                            string sqlGet = @"
                                SELECT IdParticipacion, NombreCompleto, Identificacion 
                                FROM dbo.EventosAsistentes 
                                WHERE IdAsistente = @IdAsistente AND IdEvento = @IdEvento;";
                            using (SqlCommand cmdGet = new SqlCommand(sqlGet, cn, tran))
                            {
                                cmdGet.Parameters.Add(new SqlParameter("@IdAsistente", idAsistente));
                                cmdGet.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                using (SqlDataReader dr = cmdGet.ExecuteReader())
                                {
                                    if (dr.Read())
                                    {
                                        idParticipacion = Convert.ToInt32(dr["IdParticipacion"]);
                                        nombreCompleto = dr["NombreCompleto"] != DBNull.Value ? dr["NombreCompleto"].ToString() : "";
                                        identificacion = dr["Identificacion"] != DBNull.Value ? dr["Identificacion"].ToString() : "";
                                    }
                                }
                            }

                            if (idParticipacion <= 0)
                            {
                                tran.Rollback();
                                return Json(new { success = false, message = "El registro de asistencia no fue encontrado en este evento." });
                            }

                            // 2. Obtener IdIglesia e IdMaestro si existe
                            int idIglesia = 0;
                            string sqlIg = "SELECT IdIglesia FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = @IdPart;";
                            using (SqlCommand cmdIg = new SqlCommand(sqlIg, cn, tran))
                            {
                                cmdIg.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                object valIg = cmdIg.ExecuteScalar();
                                if (valIg != null) idIglesia = Convert.ToInt32(valIg);
                            }

                            int idMaestro = 0;
                            if (idIglesia > 0)
                            {
                                string cleanDoc = identificacion.Replace("-", "").Replace(" ", "").Trim();
                                string sqlM = @"
                                    SELECT TOP 1 IdMaestro FROM dbo.Maestros 
                                    WHERE IdIglesia = @IdIg 
                                      AND (
                                          (REPLACE(REPLACE(ISNULL(DocumentoIdentidad, ''), '-', ''), ' ', '') = @Doc AND @Doc <> '')
                                          OR (LTRIM(RTRIM(ISNULL(Nombres, '') + ' ' + ISNULL(Apellidos, ''))) = @Nom AND @Nom <> '')
                                      );";
                                using (SqlCommand cmdM = new SqlCommand(sqlM, cn, tran))
                                {
                                    cmdM.Parameters.Add(new SqlParameter("@IdIg", idIglesia));
                                    cmdM.Parameters.Add(new SqlParameter("@Doc", cleanDoc));
                                    cmdM.Parameters.Add(new SqlParameter("@Nom", nombreCompleto.Trim()));
                                    object valM = cmdM.ExecuteScalar();
                                    if (valM != null) idMaestro = Convert.ToInt32(valM);
                                }
                            }

                            // 3. Eliminar relaciÃ³n de dbo.EventosAsistentes
                            string sqlDelAsist = "DELETE FROM dbo.EventosAsistentes WHERE IdAsistente = @IdAsistente AND IdEvento = @IdEvento;";
                            using (SqlCommand cmdDelA = new SqlCommand(sqlDelAsist, cn, tran))
                            {
                                cmdDelA.Parameters.Add(new SqlParameter("@IdAsistente", idAsistente));
                                cmdDelA.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdDelA.ExecuteNonQuery();
                            }

                            // 4. Si existe relaciÃ³n en dbo.AsistenciaMaestro, eliminar solo de este evento
                            if (idMaestro > 0)
                            {
                                string sqlDelAm = "DELETE FROM dbo.AsistenciaMaestro WHERE IdEvento = @IdEvento AND IdMaestro = @IdMaestro;";
                                using (SqlCommand cmdDelAm = new SqlCommand(sqlDelAm, cn, tran))
                                {
                                    cmdDelAm.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                    cmdDelAm.Parameters.Add(new SqlParameter("@IdMaestro", idMaestro));
                                    cmdDelAm.ExecuteNonQuery();
                                }
                            }

                            // 5. Recalcular cantidad de asistentes en dbo.Eventos
                            string sqlUpCant = @"
                                UPDATE dbo.Eventos 
                                SET CantidadAsistentes = (SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento) 
                                WHERE IdEvento = @IdEvento;";
                            using (SqlCommand cmdUpCant = new SqlCommand(sqlUpCant, cn, tran))
                            {
                                cmdUpCant.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdUpCant.ExecuteNonQuery();
                            }

                            // 6. Verificar si la iglesia aÃºn tiene asistentes en este evento
                            string sqlCountRest = "SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                            int restantes = 0;
                            using (SqlCommand cmdRest = new SqlCommand(sqlCountRest, cn, tran))
                            {
                                cmdRest.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdRest.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                restantes = Convert.ToInt32(cmdRest.ExecuteScalar());
                            }

                            if (restantes == 0)
                            {
                                string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = 0 WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                                using (SqlCommand cmdUpP = new SqlCommand(sqlUpPart, cn, tran))
                                {
                                    cmdUpP.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                    cmdUpP.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                    cmdUpP.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                            return Json(new { success = true, message = "Maestro retirado de la asistencia exitosamente." });
                        }
                        catch (Exception exInner)
                        {
                            tran.Rollback();
                            return Json(new { success = false, message = "Error al retirar de la asistencia: " + exInner.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error de sistema al procesar la solicitud." });
            }
        }

        private void ActualizarOInsertarPersonaInterno(SqlConnection cn, SqlTransaction tran, int idIglesia, string tipoPersona, PersonaIglesia persona)
        {
            string sqlCheck = "SELECT COUNT(1) FROM dbo.PersonasIglesia WHERE IdIglesia = @IdIglesia AND TipoPersona = @Tipo;";
            int count = 0;
            using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn, tran))
            {
                cmdCheck.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                cmdCheck.Parameters.Add(new SqlParameter("@Tipo", tipoPersona));
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
                    cmdUp.Parameters.Add(new SqlParameter("@Nombres", persona.Nombres ?? ""));
                    cmdUp.Parameters.Add(new SqlParameter("@Apellidos", persona.Apellidos ?? ""));
                    cmdUp.Parameters.Add(new SqlParameter("@Doc", persona.DocumentoIdentidad ?? (object)DBNull.Value));
                    cmdUp.Parameters.Add(new SqlParameter("@Celular", persona.Celular ?? (object)DBNull.Value));
                    cmdUp.Parameters.Add(new SqlParameter("@Correo", persona.Correo ?? (object)DBNull.Value));
                    cmdUp.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                    cmdUp.Parameters.Add(new SqlParameter("@Tipo", tipoPersona));
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
                    cmdIns.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                    cmdIns.Parameters.Add(new SqlParameter("@Tipo", tipoPersona));
                    cmdIns.Parameters.Add(new SqlParameter("@Nombres", persona.Nombres ?? ""));
                    cmdIns.Parameters.Add(new SqlParameter("@Apellidos", persona.Apellidos ?? ""));
                    cmdIns.Parameters.Add(new SqlParameter("@Doc", persona.DocumentoIdentidad ?? (object)DBNull.Value));
                    cmdIns.Parameters.Add(new SqlParameter("@Celular", persona.Celular ?? (object)DBNull.Value));
                    cmdIns.Parameters.Add(new SqlParameter("@Correo", persona.Correo ?? (object)DBNull.Value));
                    cmdIns.ExecuteNonQuery();
                }
            }
        }

        // POST: Eventos/GuardarAsistentes
        [HttpPost]
        public ActionResult GuardarAsistentes(int idEvento, int idParticipacion, List<EventoAsistenteViewModel> asistentes)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar los asistentes de un evento fuera de su equipo o jurisdicciÃ³n.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (SqlTransaction tran = cn.BeginTransaction())
                {
                    try
                    {
                        // Limpiar asistentes existentes para esta participacion y evento
                        string sqlDel = "DELETE FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdDel = new SqlCommand(sqlDel, cn, tran))
                        {
                            cmdDel.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdDel.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdDel.ExecuteNonQuery();
                        }

                        // Insertar nuevos asistentes si existen
                        if (asistentes != null && asistentes.Count > 0)
                        {
                            foreach (var a in asistentes)
                            {
                                if (!string.IsNullOrWhiteSpace(a.NombreCompleto))
                                {
                                    string sqlIns = @"
                                        INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                        VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                                    using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                                    {
                                        cmdIns.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                        cmdIns.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                                        cmdIns.Parameters.Add(new SqlParameter("@Nombre", a.NombreCompleto.Trim()));
                                        cmdIns.Parameters.Add(new SqlParameter("@Doc", a.Identificacion ?? (object)DBNull.Value));
                                        cmdIns.Parameters.Add(new SqlParameter("@Tel", a.Telefono ?? (object)DBNull.Value));
                                        cmdIns.Parameters.Add(new SqlParameter("@Correo", a.Correo ?? (object)DBNull.Value));
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Si hay al menos un asistente registrado, asegurar que la iglesia estÃ© marcada como AsistiÃ³
                        bool tieneAsistentes = asistentes != null && asistentes.Exists(x => !string.IsNullOrWhiteSpace(x.NombreCompleto));
                        string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdUp = new SqlCommand(sqlUpPart, cn, tran))
                        {
                            cmdUp.Parameters.Add(new SqlParameter("@Asistio", tieneAsistentes ? 1 : 0));
                            cmdUp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdUp.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdUp.ExecuteNonQuery();
                        }

                        // Sincronizar con VisionAsistio y VisionResultado si es evento de Vision
                        string sqlSync = @"
                            UPDATE p
                            SET p.VisionAsistio = @Asistio,
                                p.VisionResultado = CASE 
                                    WHEN @Asistio = 1 AND (p.VisionResultado IS NULL OR p.VisionResultado = '' OR p.VisionResultado = 'Pendiente') THEN 'Continua'
                                    WHEN @Asistio = 0 AND (p.VisionResultado = 'Continua') THEN 'Pendiente'
                                    ELSE p.VisionResultado
                                END
                            FROM dbo.ParticipacionesIglesia p
                            INNER JOIN dbo.EventosParticipacionIglesia ep ON p.IdParticipacion = ep.IdParticipacion
                            INNER JOIN dbo.Eventos e ON ep.IdEvento = e.IdEvento
                            WHERE ep.IdEvento = @IdEvento AND ep.IdParticipacion = @IdPart AND e.TipoEvento = 'Vision';";
                        using (SqlCommand cmdSync = new SqlCommand(sqlSync, cn, tran))
                        {
                            cmdSync.Parameters.Add(new SqlParameter("@Asistio", tieneAsistentes ? 1 : 0));
                            cmdSync.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            cmdSync.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                            cmdSync.ExecuteNonQuery();
                        }

                        // Sincronizar con TallerParticipo y EtapaActual usando Stored Procedure
                        string tipoEvento = "";
                        using (SqlCommand cmdEv = new SqlCommand("SELECT TipoEvento FROM dbo.Eventos WHERE IdEvento = @IdEvento;", cn, tran))
                        {
                            cmdEv.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                            object typeVal = cmdEv.ExecuteScalar();
                            if (typeVal != null) tipoEvento = typeVal.ToString();
                        }

                        if (tipoEvento == "Taller")
                        {
                            using (SqlCommand cmdSp = new SqlCommand("dbo.SpAvanzarEtapaTaller", cn, tran))
                            {
                                cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                                cmdSp.Parameters.Add(new SqlParameter("@IdParticipacion", idParticipacion));
                                cmdSp.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                                cmdSp.Parameters.Add(new SqlParameter("@Asistio", tieneAsistentes));
                                cmdSp.Parameters.Add(new SqlParameter("@IdUsuarioResponsable", u.IdUsuario));
                                cmdSp.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Datos de asistentes guardados y asistencia actualizada con Ã©xito.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                    }
                }
            }
            return RedirectToAction("Detalle", new { id = idEvento });
        }

        private bool PuedeEditarEvento(Usuario u, int idEvento)
        {
            if (u == null) return false;
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true; // SuperAdmin o Admin

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdUsuarioCreacion FROM dbo.Eventos WHERE IdEvento = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Id", idEvento));
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                {
                    int idUsuarioCreacion = Convert.ToInt32(val);
                    if (u.IdUsuario == idUsuarioCreacion) return true;

                    int? equipoCreador = ObtenerEquipoUsuario(idUsuarioCreacion);
                    if (u.IdEquipo.HasValue && equipoCreador.HasValue)
                    {
                        if (u.IdEquipo.Value == equipoCreador.Value) return true;
                        return EsEquipoHijo(u.IdEquipo.Value, equipoCreador.Value);
                    }
                }
            }
            return false;
        }

        private int? ObtenerEquipoUsuario(int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT TOP 1 IdEquipo FROM dbo.AsignacionesEquipo WHERE IdUsuario = @Id AND Activo = 1;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Id", idUsuario));
                cn.Open();
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) return Convert.ToInt32(val);
            }
            return null;
        }

        private bool EsEquipoHijo(int idEquipoPadre, int idEquipoHijo)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT COUNT(1) FROM dbo.Equipos WHERE IdEquipo = @IdHijo AND IdEquipoPadre = @IdPadre;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdHijo", idEquipoHijo));
                cmd.Parameters.Add(new SqlParameter("@IdPadre", idEquipoPadre));
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void ObtenerEquiposHijosRecursivo(int idEquipoPadre, HashSet<int> set)
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = "SELECT IdEquipo FROM dbo.Equipos WHERE IdEquipoPadre = @Id;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@Id", idEquipoPadre));
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int hId = Convert.ToInt32(dr["IdEquipo"]);
                        set.Add(hId);
                        ObtenerEquiposHijosRecursivo(hId, set);
                    }
                }
            }
        }

        // =====================================================================
        // MÃ‰TODOS DE ASISTENCIA DE COORDINADORES AL EVENTO
        // =====================================================================

        private List<CoordinadorEventoAsistenciaViewModel> ObtenerCoordinadoresAsistentesEvento(int idEvento)
        {
            var lista = new List<CoordinadorEventoAsistenciaViewModel>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT ac.IdAsistenciaCoordinador, ac.IdEvento, ac.IdUsuario, ac.Asistio, ac.RolEnEvento, ac.Observaciones, ac.FechaRegistro,
                           ISNULL(NULLIF(LTRIM(RTRIM(CONCAT(p.PrimerNombre, ' ', p.PrimerApellido))), ''), u.Correo) AS NombreCompleto,
                           u.Correo,
                           p.TelefonoCelularWhatsApp AS Celular,
                           pos.NombrePosicion,
                           eq.NombreEquipo
                    FROM dbo.EventosAsistenciaCoordinadores ac
                    INNER JOIN dbo.Usuarios u ON ac.IdUsuario = u.IdUsuario
                    LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                    LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.PosicionesOCC pos ON a.IdPosicion = pos.IdPosicion
                    WHERE ac.IdEvento = @IdEvento
                    ORDER BY ac.FechaRegistro ASC;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new CoordinadorEventoAsistenciaViewModel
                        {
                            IdAsistenciaCoordinador = Convert.ToInt32(dr["IdAsistenciaCoordinador"]),
                            IdEvento = Convert.ToInt32(dr["IdEvento"]),
                            IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                            NombreCompleto = dr["NombreCompleto"].ToString(),
                            Correo = dr["Correo"].ToString(),
                            Celular = dr["Celular"] != DBNull.Value ? dr["Celular"].ToString() : "",
                            NombrePosicion = dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString() : "Coordinador",
                            NombreEquipo = dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString() : "Sin Equipo",
                            Asistio = Convert.ToBoolean(dr["Asistio"]),
                            RolEnEvento = dr["RolEnEvento"] != DBNull.Value ? dr["RolEnEvento"].ToString() : "",
                            Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : "",
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            return lista;
        }

        private List<CoordinadorDropdownItem> ObtenerCoordinadoresParaDropdown(int idEvento)
        {
            var lista = new List<CoordinadorDropdownItem>();
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                // Traer coordinadores activos que NO estÃ©n ya agregados en este evento
                string sql = @"
                    SELECT u.IdUsuario,
                           ISNULL(NULLIF(LTRIM(RTRIM(CONCAT(p.PrimerNombre, ' ', p.PrimerApellido))), ''), u.Correo) AS NombreCompleto,
                           pos.NombrePosicion,
                           eq.NombreEquipo
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                    LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.PosicionesOCC pos ON a.IdPosicion = pos.IdPosicion
                    WHERE u.IdEstado = 4
                      AND u.IdUsuario NOT IN (SELECT IdUsuario FROM dbo.EventosAsistenciaCoordinadores WHERE IdEvento = @IdEvento)
                    ORDER BY p.PrimerNombre, p.PrimerApellido, u.Correo;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new CoordinadorDropdownItem
                        {
                            IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                            NombreCompleto = dr["NombreCompleto"].ToString(),
                            NombrePosicion = dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString() : "Coordinador",
                            NombreEquipo = dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString() : "Sin Equipo"
                        });
                    }
                }
            }
            return lista;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarCoordinadorEvento(int idEvento, int idUsuario, string rolEnEvento, string observaciones)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para gestionar la asistencia de coordinadores en este evento.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            if (idUsuario <= 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un coordinador vÃ¡lido de la lista desplegable.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        IF EXISTS (SELECT 1 FROM dbo.EventosAsistenciaCoordinadores WHERE IdEvento = @IdEvento AND IdUsuario = @IdUsuario)
                        BEGIN
                            UPDATE dbo.EventosAsistenciaCoordinadores
                            SET Asistio = 1,
                                RolEnEvento = @Rol,
                                Observaciones = @Obs,
                                FechaRegistro = GETDATE(),
                                IdUsuarioRegistro = @IdReg
                            WHERE IdEvento = @IdEvento AND IdUsuario = @IdUsuario;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.EventosAsistenciaCoordinadores (
                                IdEvento, IdUsuario, Asistio, RolEnEvento, Observaciones, FechaRegistro, IdUsuarioRegistro
                            ) VALUES (
                                @IdEvento, @IdUsuario, 1, @Rol, @Obs, GETDATE(), @IdReg
                            );
                        END;";

                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cmd.Parameters.Add(new SqlParameter("@IdUsuario", idUsuario));
                    cmd.Parameters.Add(new SqlParameter("@Rol", (object)rolEnEvento ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Obs", (object)observaciones ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@IdReg", u.IdUsuario));

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                TempData["MensajeExito"] = "Coordinador registrado en el evento con Ã©xito.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleAsistenciaCoordinador(int idAsistenciaCoordinador, int idEvento, bool asistio)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar la asistencia de coordinadores en este evento.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "UPDATE dbo.EventosAsistenciaCoordinadores SET Asistio = @Asistio WHERE IdAsistenciaCoordinador = @Id AND IdEvento = @IdEvento;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.Add(new SqlParameter("@Asistio", asistio));
                    cmd.Parameters.Add(new SqlParameter("@Id", idAsistenciaCoordinador));
                    cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = asistio ? "Asistencia del coordinador confirmada." : "Coordinador marcado como no asistente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoverCoordinadorEvento(int idAsistenciaCoordinador, int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para remover coordinadores en este evento.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "DELETE FROM dbo.EventosAsistenciaCoordinadores WHERE IdAsistenciaCoordinador = @Id AND IdEvento = @IdEvento;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.Add(new SqlParameter("@Id", idAsistenciaCoordinador));
                    cmd.Parameters.Add(new SqlParameter("@IdEvento", idEvento));
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["MensajeExito"] = "Coordinador removido de la lista de asistencia del evento.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idEvento });
        }

        private List<string> ObtenerRolesEventoActivos()
        {
            var lista = new List<string>();
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = @"
                        IF OBJECT_ID('dbo.RolesEvento', 'U') IS NOT NULL
                        BEGIN
                            SELECT Nombre FROM dbo.RolesEvento WHERE Activo = 1 ORDER BY IdRolEvento ASC;
                        END";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(dr["Nombre"].ToString());
                        }
                    }
                }
            }
            catch { }

            if (!lista.Any())
            {
                lista = new List<string>
                {
                    "Coordinador Principal / Encargado",
                    "Facilitador / Expositor",
                    "LogÃ­stica y Despacho",
                    "Registro y Asistencia",
                    "AcompaÃ±amiento y Bienvenida",
                    "IntercesiÃ³n y OraciÃ³n",
                    "Apoyo General"
                };
            }
            return lista;
        }
    }

    public class IglesiaParticipacionViewModel
    {
        public int IdParticipacion { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public string NombreEquipo { get; set; }
        public bool Asistio { get; set; }
        public List<EventoAsistenteViewModel> AsistentesDetalle { get; set; } = new List<EventoAsistenteViewModel>();

        // Propiedades adicionales
        public PersonaIglesia Pastor { get; set; }
        public PersonaIglesia LiderMinisterial { get; set; }
        public List<Maestro> Maestros { get; set; } = new List<Maestro>();
    }

    public class MaestroAsistenciaViewModel
    {
        public int IdMaestro { get; set; }
        public string NombreCompleto { get; set; }
        public string NombreIglesia { get; set; }
        public bool Asistio { get; set; }
    }

    public class EventoAsistenteViewModel
    {
        public int IdAsistente { get; set; }
        public int IdEvento { get; set; }
        public int IdParticipacion { get; set; }
        public string NombreCompleto { get; set; }
        public string Identificacion { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
    }
}
