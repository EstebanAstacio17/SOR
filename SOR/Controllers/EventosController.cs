using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using SOR.Models;
using SOR.Permisos;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class EventosController : Controller
    {
        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        private void AsegurarEsquemaEventos()
        {
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    IF OBJECT_ID('dbo.Eventos', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('dbo.Eventos', 'TipoLugar') IS NULL
                        BEGIN
                            ALTER TABLE dbo.Eventos ADD TipoLugar NVARCHAR(255) NULL;
                        END
                        IF COL_LENGTH('dbo.Eventos', 'Hora') IS NULL
                        BEGIN
                            ALTER TABLE dbo.Eventos ADD Hora NVARCHAR(50) NULL;
                        END
                        IF COL_LENGTH('dbo.Eventos', 'CantidadAsistentes') IS NULL
                        BEGIN
                            ALTER TABLE dbo.Eventos ADD CantidadAsistentes INT NULL DEFAULT 0;
                        END
                    END
                    IF OBJECT_ID('dbo.EventosAsistentes', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.EventosAsistentes (
                            IdAsistente INT IDENTITY(1,1) PRIMARY KEY,
                            IdEvento INT NOT NULL,
                            IdParticipacion INT NOT NULL,
                            NombreCompleto NVARCHAR(255) NOT NULL,
                            Identificacion NVARCHAR(50) NULL,
                            Telefono NVARCHAR(50) NULL,
                            Correo NVARCHAR(255) NULL,
                            FechaRegistro DATETIME DEFAULT GETDATE()
                        );
                    END";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // GET: Eventos
        public ActionResult Index()
        {
            AsegurarEsquemaEventos();
            Usuario u = (Usuario)Session["usuario"];
            List<Evento> lista = new List<Evento>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT e.*, t.NombreTemporada, u.Correo AS CorreoCreador, a.IdEquipo AS IdEquipoCreador
                    FROM dbo.Eventos e
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Usuarios u ON e.IdUsuarioCreacion = u.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
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
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"])
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

            CargarTemporadasYTipos();
            ViewBag.UsuarioActual = u;
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

            // Si no se asignó temporada, obtener la activa
            if (modelo.IdTemporada <= 0)
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    string sql = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas WHERE Activa = 1;";
                    SqlCommand cmd = new SqlCommand(sql, cn);
                    cn.Open();
                    object valObj = cmd.ExecuteScalar();
                    if (valObj != null)
                    {
                        modelo.IdTemporada = Convert.ToInt32(valObj);
                    }
                    else
                    {
                        TempData["MensajeError"] = "No hay una temporada activa en el sistema. Configura una primero.";
                        return RedirectToAction("Index");
                    }
                }
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    INSERT INTO dbo.Eventos (NombreEvento, TipoEvento, IdTemporada, Fecha, Lugar, Responsable, IdUsuarioCreacion, TipoLugar, Hora, CantidadAsistentes) 
                    VALUES (@Nombre, @Tipo, @IdTemp, @Fecha, @Lugar, @Resp, @IdUsuario, @TipoLugar, @Hora, @Cant);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreEvento);
                cmd.Parameters.AddWithValue("@Tipo", modelo.TipoEvento);
                cmd.Parameters.AddWithValue("@IdTemp", modelo.IdTemporada);
                cmd.Parameters.AddWithValue("@Fecha", modelo.Fecha);
                cmd.Parameters.AddWithValue("@Lugar", modelo.Lugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Resp", modelo.Responsable ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                cmd.Parameters.AddWithValue("@TipoLugar", modelo.TipoLugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Hora", modelo.Hora ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cant", modelo.CantidadAsistentes);

                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Evento creado con éxito.";
            return RedirectToAction("Index");
        }

        // GET: Eventos/Detalle/5
        public ActionResult Detalle(int id)
        {
            AsegurarEsquemaEventos();
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
                cmd.Parameters.AddWithValue("@Id", id);

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
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"])
                        };
                    }
                }
            }

            if (evento == null)
            {
                return HttpNotFound();
            }

            // Cargar iglesias que participan en este evento
            List<IglesiaParticipacionViewModel> iglesias = ObtenerIglesiasParticipantes(id, evento.IdTemporada);
            // Cargar maestros de estas iglesias
            List<MaestroAsistenciaViewModel> maestros = ObtenerMaestrosYAsistencia(id, evento.IdTemporada);

            ViewBag.Evento = evento;
            ViewBag.Iglesias = iglesias;
            ViewBag.Maestros = maestros;
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarEvento(u, id);

            return View();
        }

        // POST: Eventos/RegistrarAsistenciaIglesia
        [HttpPost]
        public ActionResult RegistrarAsistenciaIglesia(int idEvento, int idParticipacion, bool asistio)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar la asistencia de un evento fuera de su equipo o jurisdicción.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                // Verificar si existe registro
                string sqlCheck = "SELECT COUNT(1) FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn);
                cmdCheck.Parameters.AddWithValue("@IdEvento", idEvento);
                cmdCheck.Parameters.AddWithValue("@IdPart", idParticipacion);
                int count = Convert.ToInt32(cmdCheck.ExecuteScalar());

                if (count > 0)
                {
                    string sqlUpdate = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                    SqlCommand cmdUp = new SqlCommand(sqlUpdate, cn);
                    cmdUp.Parameters.AddWithValue("@Asistio", asistio);
                    cmdUp.Parameters.AddWithValue("@IdEvento", idEvento);
                    cmdUp.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmdUp.ExecuteNonQuery();
                }
                else
                {
                    string sqlIns = "INSERT INTO dbo.EventosParticipacionIglesia (IdEvento, IdParticipacion, Asistio) VALUES (@IdEvento, @IdPart, @Asistio);";
                    SqlCommand cmdIns = new SqlCommand(sqlIns, cn);
                    cmdIns.Parameters.AddWithValue("@Asistio", asistio);
                    cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                    cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                    cmdIns.ExecuteNonQuery();
                }

                // Sincronizar el estado de asistencia y resultado de Visión en dbo.ParticipacionesIglesia
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
                    cmdSync.Parameters.AddWithValue("@Asistio", asistio);
                    cmdSync.Parameters.AddWithValue("@IdEvento", idEvento);
                    cmdSync.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                TempData["MensajeError"] = "No tiene permiso para modificar la asistencia de maestros en un evento fuera de su equipo o jurisdicción.";
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
                            cmdDel.Parameters.AddWithValue("@IdEvento", idEvento);
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
                                cmdIns.Parameters.AddWithValue("@IdMaestro", idMaestro);
                                cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdIns.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                                cmdIns.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia de maestros guardada correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al guardar asistencia de maestros: " + ex.Message;
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
                cmd.Parameters.AddWithValue("@IdEvento", idEvento);
                cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);

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

            // Cargar asistentes registrados por iglesia
            using (SqlConnection cn2 = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn2.Open();
                foreach (var item in lista)
                {
                    string sqlAsist = @"
                        SELECT IdAsistente, IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo
                        FROM dbo.EventosAsistentes
                        WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart
                        ORDER BY IdAsistente ASC;";
                    using (SqlCommand cmdAsist = new SqlCommand(sqlAsist, cn2))
                    {
                        cmdAsist.Parameters.AddWithValue("@IdEvento", idEvento);
                        cmdAsist.Parameters.AddWithValue("@IdPart", item.IdParticipacion);
                        using (SqlDataReader drA = cmdAsist.ExecuteReader())
                        {
                            while (drA.Read())
                            {
                                item.AsistentesDetalle.Add(new EventoAsistenteViewModel
                                {
                                    IdAsistente = Convert.ToInt32(drA["IdAsistente"]),
                                    IdEvento = Convert.ToInt32(drA["IdEvento"]),
                                    IdParticipacion = Convert.ToInt32(drA["IdParticipacion"]),
                                    NombreCompleto = drA["NombreCompleto"].ToString(),
                                    Identificacion = drA["Identificacion"] != DBNull.Value ? drA["Identificacion"].ToString() : "",
                                    Telefono = drA["Telefono"] != DBNull.Value ? drA["Telefono"].ToString() : "",
                                    Correo = drA["Correo"] != DBNull.Value ? drA["Correo"].ToString() : ""
                                });
                            }
                        }
                    }
                }
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
                cmd.Parameters.AddWithValue("@IdEvento", idEvento);
                cmd.Parameters.AddWithValue("@IdTemporada", idTemporada);

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

        private void CargarTemporadasYTipos()
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
                new SelectListItem { Value = "Vision", Text = "Presentación de la Visión" },
                new SelectListItem { Value = "Taller", Text = "Taller OCC" },
                new SelectListItem { Value = "Evangelistico", Text = "Evento Evangelístico" },
                new SelectListItem { Value = "GranAventura", Text = "La Gran Aventura" }
            };
        }

        // POST: Eventos/Editar
        [HttpPost]
        public ActionResult Editar(Evento modelo)
        {
            Usuario u = (Usuario)Session["usuario"];
            AsegurarEsquemaEventos();
            if (modelo == null || modelo.IdEvento <= 0)
            {
                TempData["MensajeError"] = "Datos de evento inválidos.";
                return RedirectToAction("Index");
            }

            if (!PuedeEditarEvento(u, modelo.IdEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar eventos pertenecientes a otro equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            // Verificar que el evento pertenece a la temporada activa
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlCheck = @"SELECT t.Activa FROM dbo.Eventos e INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada WHERE e.IdEvento = @IdEvento;";
                SqlCommand cmdC = new SqlCommand(sqlCheck, cn);
                cmdC.Parameters.AddWithValue("@IdEvento", modelo.IdEvento);
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
                        CantidadAsistentes = @Cant
                    WHERE IdEvento = @IdEvento;";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreEvento);
                cmd.Parameters.AddWithValue("@Tipo", modelo.TipoEvento);
                cmd.Parameters.AddWithValue("@Fecha", modelo.Fecha);
                cmd.Parameters.AddWithValue("@Lugar", modelo.Lugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Resp", modelo.Responsable ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TipoLugar", modelo.TipoLugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Hora", modelo.Hora ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cant", modelo.CantidadAsistentes);
                cmd.Parameters.AddWithValue("@IdEvento", modelo.IdEvento);
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Evento actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // POST: Eventos/Eliminar
        [HttpPost]
        public ActionResult Eliminar(int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para eliminar eventos pertenecientes a otro equipo o jurisdicción.";
                return RedirectToAction("Index");
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();

                // Verificar que el evento pertenece a temporada activa
                string sqlCheck = @"SELECT t.Activa FROM dbo.Eventos e INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada WHERE e.IdEvento = @IdEvento;";
                SqlCommand cmdC = new SqlCommand(sqlCheck, cn);
                cmdC.Parameters.AddWithValue("@IdEvento", idEvento);
                object activa = cmdC.ExecuteScalar();
                if (activa == null || !Convert.ToBoolean(activa))
                {
                    TempData["MensajeError"] = "Solo se pueden eliminar eventos de la temporada activa.";
                    return RedirectToAction("Index");
                }

                // Verificar si hay asistentes confirmados
                string sqlAsist = "SELECT COUNT(1) FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND Asistio = 1;";
                SqlCommand cmdA = new SqlCommand(sqlAsist, cn);
                cmdA.Parameters.AddWithValue("@IdEvento", idEvento);
                int countAsist = Convert.ToInt32(cmdA.ExecuteScalar());
                if (countAsist > 0)
                {
                    TempData["MensajeError"] = $"No se puede eliminar el evento porque tiene {countAsist} iglesia(s) con asistencia confirmada. Primero desmárquelas.";
                    return RedirectToAction("Index");
                }

                // Eliminar invitaciones pendientes y luego el evento
                string sqlDelPart = "DELETE FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento;";
                SqlCommand cmdDP = new SqlCommand(sqlDelPart, cn);
                cmdDP.Parameters.AddWithValue("@IdEvento", idEvento);
                cmdDP.ExecuteNonQuery();

                string sqlDel = "DELETE FROM dbo.Eventos WHERE IdEvento = @IdEvento;";
                SqlCommand cmdD = new SqlCommand(sqlDel, cn);
                cmdD.Parameters.AddWithValue("@IdEvento", idEvento);
                cmdD.ExecuteNonQuery();
            }

            TempData["MensajeExito"] = "Evento eliminado correctamente.";
            return RedirectToAction("Index");
        }

        // POST: Eventos/GuardarAsistentes
        [HttpPost]
        public ActionResult GuardarAsistentes(int idEvento, int idParticipacion, List<EventoAsistenteViewModel> asistentes)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeEditarEvento(u, idEvento))
            {
                TempData["MensajeError"] = "No tiene permiso para modificar los asistentes de un evento fuera de su equipo o jurisdicción.";
                return RedirectToAction("Detalle", new { id = idEvento });
            }

            AsegurarEsquemaEventos();
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
                            cmdDel.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdDel.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                                        cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                        cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                                        cmdIns.Parameters.AddWithValue("@Nombre", a.NombreCompleto.Trim());
                                        cmdIns.Parameters.AddWithValue("@Doc", a.Identificacion ?? (object)DBNull.Value);
                                        cmdIns.Parameters.AddWithValue("@Tel", a.Telefono ?? (object)DBNull.Value);
                                        cmdIns.Parameters.AddWithValue("@Correo", a.Correo ?? (object)DBNull.Value);
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // Si hay al menos un asistente registrado, asegurar que la iglesia esté marcada como Asistió
                        bool tieneAsistentes = asistentes != null && asistentes.Exists(x => !string.IsNullOrWhiteSpace(x.NombreCompleto));
                        string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdUp = new SqlCommand(sqlUpPart, cn, tran))
                        {
                            cmdUp.Parameters.AddWithValue("@Asistio", tieneAsistentes ? 1 : 0);
                            cmdUp.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdUp.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                            cmdSync.Parameters.AddWithValue("@Asistio", tieneAsistentes ? 1 : 0);
                            cmdSync.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdSync.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdSync.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Datos de asistentes guardados y asistencia actualizada con éxito.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al guardar asistentes: " + ex.Message;
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
                cmd.Parameters.AddWithValue("@Id", idEvento);
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
                cmd.Parameters.AddWithValue("@Id", idUsuario);
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
                cmd.Parameters.AddWithValue("@IdHijo", idEquipoHijo);
                cmd.Parameters.AddWithValue("@IdPadre", idEquipoPadre);
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
                cmd.Parameters.AddWithValue("@Id", idEquipoPadre);
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
    }

    public class IglesiaParticipacionViewModel
    {
        public int IdParticipacion { get; set; }
        public int IdIglesia { get; set; }
        public string NombreIglesia { get; set; }
        public string NombreEquipo { get; set; }
        public bool Asistio { get; set; }
        public List<EventoAsistenteViewModel> AsistentesDetalle { get; set; } = new List<EventoAsistenteViewModel>();
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
