using System;
using System.Collections.Generic;
using System.Configuration;
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

                // Asegurar Stored Procedure SpEliminarEvento
                string spCheck = "SELECT COUNT(1) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpEliminarEvento]') AND type in (N'P', N'PC');";
                SqlCommand cmdCheck = new SqlCommand(spCheck, cn);
                int spExists = Convert.ToInt32(cmdCheck.ExecuteScalar());
                if (spExists == 0)
                {
                    string spCreate = @"
                        CREATE PROCEDURE dbo.SpEliminarEvento
                            @IdEvento INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;

                            IF EXISTS (SELECT 1 FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento AND Asistio = 1)
                            BEGIN
                                RAISERROR('No se puede eliminar el evento porque tiene iglesias confirmadas.', 16, 1);
                                RETURN;
                            END

                            IF EXISTS (SELECT 1 FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento)
                            BEGIN
                                RAISERROR('No se puede eliminar el evento porque tiene asistentes registrados.', 16, 1);
                                RETURN;
                            END

                            IF EXISTS (SELECT 1 FROM dbo.AsistenciaMaestro WHERE IdEvento = @IdEvento)
                            BEGIN
                                RAISERROR('No se puede eliminar el evento porque tiene asistencia de maestros registrada.', 16, 1);
                                RETURN;
                            END

                            DELETE FROM dbo.EventosParticipacionIglesia WHERE IdEvento = @IdEvento;
                            DELETE FROM dbo.Eventos WHERE IdEvento = @IdEvento;
                        END";
                    SqlCommand cmdCreate = new SqlCommand(spCreate, cn);
                    cmdCreate.ExecuteNonQuery();
                }

                // Asegurar Tabla LogsCambiosEtapa
                string sqlLogsEtapa = @"
                    IF OBJECT_ID('dbo.LogsCambiosEtapa', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.LogsCambiosEtapa (
                            IdLog INT IDENTITY(1,1) PRIMARY KEY,
                            IdIglesia INT NOT NULL,
                            EtapaAnterior INT NOT NULL,
                            EtapaNueva INT NOT NULL,
                            IdUsuarioResponsable INT NOT NULL,
                            FechaHora DATETIME DEFAULT GETDATE(),
                            Detalles NVARCHAR(MAX) NULL
                        );
                    END";
                SqlCommand cmdLogsEtapa = new SqlCommand(sqlLogsEtapa, cn);
                cmdLogsEtapa.ExecuteNonQuery();

                // Asegurar Stored Procedure SpAvanzarEtapaTaller
                string spTallerCheck = "SELECT COUNT(1) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpAvanzarEtapaTaller]') AND type in (N'P', N'PC');";
                SqlCommand cmdTallerCheck = new SqlCommand(spTallerCheck, cn);
                int spTallerExists = Convert.ToInt32(cmdTallerCheck.ExecuteScalar());
                if (spTallerExists == 0)
                {
                    string spTallerCreate = @"
                        CREATE PROCEDURE dbo.SpAvanzarEtapaTaller
                            @IdParticipacion INT,
                            @IdEvento INT,
                            @Asistio BIT,
                            @IdUsuarioResponsable INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            DECLARE @EtapaAnterior INT;
                            DECLARE @IdIglesia INT;

                            SELECT @EtapaAnterior = EtapaActual, @IdIglesia = IdIglesia
                            FROM dbo.ParticipacionesIglesia
                            WHERE IdParticipacion = @IdParticipacion;

                            IF @EtapaAnterior = 5 AND @Asistio = 1
                            BEGIN
                                -- Actualizar etapa
                                UPDATE dbo.ParticipacionesIglesia
                                SET TallerParticipo = 1,
                                    EtapaActual = 6
                                WHERE IdParticipacion = @IdParticipacion;

                                -- Registrar en HistorialParticipacion
                                INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                                VALUES (@IdParticipacion, GETDATE(), 'Completado Taller OCC', 'Taller OCC (Etapa 5)', 'Evaluación Asignación (Etapa 6)', @IdUsuarioResponsable, 'Asistencia al Taller OCC confirmada. Avanza a Evaluación Asignación.');

                                -- Registrar en LogsCambiosEtapa
                                INSERT INTO dbo.LogsCambiosEtapa (IdIglesia, EtapaAnterior, EtapaNueva, IdUsuarioResponsable, FechaHora, Detalles)
                                VALUES (@IdIglesia, 5, 6, @IdUsuarioResponsable, GETDATE(), 'Transición automática tras registrar asistencia en Taller OCC.');
                            END
                            ELSE
                            BEGIN
                                -- Solo actualizar participación
                                UPDATE dbo.ParticipacionesIglesia
                                SET TallerParticipo = @Asistio
                                WHERE IdParticipacion = @IdParticipacion;
                            END
                        END";
                    SqlCommand cmdTallerCreate = new SqlCommand(spTallerCreate, cn);
                    cmdTallerCreate.ExecuteNonQuery();
                }

                // Asegurar Stored Procedure SpAvanzarEtapaRecursos
                string spRecursosCheck = "SELECT COUNT(1) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpAvanzarEtapaRecursos]') AND type in (N'P', N'PC');";
                SqlCommand cmdRecursosCheck = new SqlCommand(spRecursosCheck, cn);
                int spRecursosExists = Convert.ToInt32(cmdRecursosCheck.ExecuteScalar());
                if (spRecursosExists == 0)
                {
                    string spRecursosCreate = @"
                        CREATE PROCEDURE dbo.SpAvanzarEtapaRecursos
                            @IdParticipacion INT,
                            @TallerNombre NVARCHAR(255),
                            @TallerFecha DATETIME,
                            @TallerLugar NVARCHAR(255),
                            @CantNinos INT,
                            @CantMaestrosReg INT,
                            @CantMaestrosAsist INT,
                            @CantMaestrosAus INT,
                            @IdUsuarioResponsable INT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            DECLARE @EtapaAnterior INT;
                            DECLARE @IdIglesia INT;

                            SELECT @EtapaAnterior = EtapaActual, @IdIglesia = IdIglesia
                            FROM dbo.ParticipacionesIglesia
                            WHERE IdParticipacion = @IdParticipacion;

                            -- Actualizar los datos
                            UPDATE dbo.ParticipacionesIglesia SET
                                EtapaActual = 7,
                                EstadoEvaluacion = 'Aprobado',
                                TallerParticipo = 1,
                                TallerNombre = @TallerNombre,
                                TallerFecha = @TallerFecha,
                                TallerLugar = @TallerLugar,
                                TallerCantNinos = @CantNinos,
                                TallerCantMaestrosReg = @CantMaestrosReg,
                                TallerCantMaestrosAsist = @CantMaestrosAsist,
                                TallerCantMaestrosAus = @CantMaestrosAus
                            WHERE IdParticipacion = @IdParticipacion;

                            -- Registrar en HistorialParticipacion
                            INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                            VALUES (@IdParticipacion, GETDATE(), 'Asignación de Recursos Finalizada', 'Evaluación Asignación (Etapa 6)', 'Aprobación Final (Etapa 7)', @IdUsuarioResponsable, 'Se finalizó la asignación de recursos y se completó la participación.');

                            -- Registrar en LogsCambiosEtapa
                            INSERT INTO dbo.LogsCambiosEtapa (IdIglesia, EtapaAnterior, EtapaNueva, IdUsuarioResponsable, FechaHora, Detalles)
                            VALUES (@IdIglesia, @EtapaAnterior, 7, @IdUsuarioResponsable, GETDATE(), 'Transición de asignación final de recursos y cierre de participación.');
                        END";
                    SqlCommand cmdRecursosCreate = new SqlCommand(spRecursosCreate, cn);
                    cmdRecursosCreate.ExecuteNonQuery();
                }
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
                            FechaCreacion = Convert.ToDateTime(dr["FechaCreacion"]),
                            RowVersion = dr.TableHasColumn("RowVersion") && dr["RowVersion"] != DBNull.Value ? (byte[])dr["RowVersion"] : null
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
                int idNuevoEvento = Convert.ToInt32(cmd.ExecuteScalar());

                if (modelo.TipoEvento == "Despacho")
                {
                    int idEq = u.IdEquipo.GetValueOrDefault(1);
                    string sqlED = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.EventosDespacho WHERE IdEvento = @IdEv)
                        BEGIN
                            INSERT INTO dbo.EventosDespacho (IdEvento, IdEquipo, EstadoDespachoEvento)
                            VALUES (@IdEv, @IdEq, 'PROGRAMADO');
                        END";
                    using (SqlCommand cmdED = new SqlCommand(sqlED, cn))
                    {
                        cmdED.Parameters.AddWithValue("@IdEv", idNuevoEvento);
                        cmdED.Parameters.AddWithValue("@IdEq", idEq);
                        cmdED.ExecuteNonQuery();
                    }
                }
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

            // Si es un evento de Despacho, cargar datos logísticos
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

            ViewBag.Evento = evento;
            ViewBag.Iglesias = iglesias;
            ViewBag.Maestros = maestros;
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarEvento(u, id);

            return View();
        }

        // =====================================================================
        // MÉTODOS DE DESPACHO PRESENCIAL (EVENTOS DE DESPACHO)
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
                        cmd.Parameters.AddWithValue("@Id", idEvento);
                        idTemporada = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                logisticaSvc.ProgramarIglesiaEnDespacho(idEvento, idParticipacion, idIglesia, idEquipo, idTemporada, u.IdUsuario);
                TempData["MensajeExito"] = "Iglesia agregada exitosamente al evento de despacho.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
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
                var logisticaSvc = new SOR.Services.LogisticaService();
                int idEquipo = u.IdEquipo.GetValueOrDefault(1);
                int idTemporada = 0;
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT IdTemporada FROM dbo.Eventos WHERE IdEvento=@Id;", cn))
                    {
                        cmd.Parameters.AddWithValue("@Id", idEvento);
                        idTemporada = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                string nombre = u.Correo ?? "Coordinador";
                logisticaSvc.ConfirmarDespacho(vm, idEquipo, idTemporada, u.IdUsuario, nombre);
                TempData["MensajeExito"] = "Despacho presencial confirmado exitosamente con cédula validada.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al confirmar despacho: " + ex.Message;
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
                TempData["MensajeExito"] = "Iglesia registrada como NO DESPACHADA. No se descontó inventario y queda disponible para reprogramación.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
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

            if (!lista.Any()) return lista;

            var idsIglesias = lista.Select(x => x.IdIglesia).Distinct().ToList();
            var idsParticipaciones = lista.Select(x => x.IdParticipacion).Distinct().ToList();

            // Optimización de Alto Rendimiento: Cargar pastores, líderes, maestros y asistentes en consultas por lote
            using (SqlConnection cnBatch = new SqlConnection(ObtenerCadenaConexion()))
            {
                cnBatch.Open();

                // 1. Cargar Pastores y Líderes en una sola consulta
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
                        if (target != null)
                        {
                            target.Maestros.Add(new Maestro
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
                }

                // 3. Cargar Asistentes en una sola consulta
                string sqlAsist = $@"
                    SELECT IdAsistente, IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo
                    FROM dbo.EventosAsistentes
                    WHERE IdEvento = @IdEvento AND IdParticipacion IN ({string.Join(",", idsParticipaciones)})
                    ORDER BY IdAsistente ASC;";

                using (SqlCommand cmdA = new SqlCommand(sqlAsist, cnBatch))
                {
                    cmdA.Parameters.AddWithValue("@IdEvento", idEvento);
                    using (SqlDataReader drA = cmdA.ExecuteReader())
                    {
                        while (drA.Read())
                        {
                            int idPart = Convert.ToInt32(drA["IdParticipacion"]);
                            var target = lista.FirstOrDefault(x => x.IdParticipacion == idPart);
                            if (target != null)
                            {
                                target.AsistentesDetalle.Add(new EventoAsistenteViewModel
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
            }

            // Asegurar objetos por defecto no nulos
            foreach (var item in lista)
            {
                if (item.Pastor == null)
                    item.Pastor = new PersonaIglesia { TipoPersona = "Pastor", Nombres = "", Apellidos = "", DocumentoIdentidad = "", Celular = "", Correo = "" };
                if (item.LiderMinisterial == null)
                    item.LiderMinisterial = new PersonaIglesia { TipoPersona = "LiderMinisterial", Nombres = "", Apellidos = "", DocumentoIdentidad = "", Celular = "", Correo = "" };
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
                new SelectListItem { Value = "Vision", Text = "Presentación de la Visión" },
                new SelectListItem { Value = "Taller", Text = "Taller OCC" },
                new SelectListItem { Value = "Despacho", Text = "Despacho de Materiales" },
                new SelectListItem { Value = "Evangelistico", Text = "Evento Evangelístico" },
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
                    cmdInt.Parameters.AddWithValue("@IdEquipo", u.IdEquipo.Value);
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
        }

        // POST: Eventos/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                        CantidadAsistentes = @Cant,
                        FechaModificacion = GETUTCDATE(),
                        UsuarioModificacion = @IdUsuario
                    WHERE IdEvento = @IdEvento
                      AND (@RowVersion IS NULL OR RowVersion = @RowVersion);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Nombre", modelo.NombreEvento);
                cmd.Parameters.AddWithValue("@Tipo", modelo.TipoEvento);
                cmd.Parameters.AddWithValue("@Fecha", modelo.Fecha);
                cmd.Parameters.AddWithValue("@Lugar", modelo.Lugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Resp", modelo.Responsable ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TipoLugar", modelo.TipoLugar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Hora", modelo.Hora ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cant", modelo.CantidadAsistentes);
                cmd.Parameters.AddWithValue("@IdUsuario", u.IdUsuario);
                cmd.Parameters.AddWithValue("@RowVersion", modelo.RowVersion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IdEvento", modelo.IdEvento);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    TempData["MensajeError"] = "Conflicto de concurrencia: El evento fue modificado concurrentemente por otro usuario. Actualice la información antes de continuar.";
                    return RedirectToAction("Index");
                }
            }

            SOR.Helpers.AuditoriaHelper.Registrar(u.IdUsuario, u.Correo, "UPDATE", "Evento", modelo.IdEvento.ToString(), "Edición de evento: " + modelo.NombreEvento);
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

                try
                {
                    using (SqlCommand cmdSp = new SqlCommand("dbo.SpEliminarEvento", cn))
                    {
                        cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                        cmdSp.Parameters.AddWithValue("@IdEvento", idEvento);
                        cmdSp.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    TempData["MensajeError"] = ex.Message;
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

                        // 2. Actualizar Líder Ministerial
                        if (lider != null && !string.IsNullOrWhiteSpace(lider.Nombres))
                        {
                            ActualizarOInsertarPersonaInterno(cn, tran, idIglesia, "LiderMinisterial", lider);
                        }

                        // 3. Limpiar asistentes anteriores de esta iglesia en este evento
                        string sqlDel = "DELETE FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdDel = new SqlCommand(sqlDel, cn, tran))
                        {
                            cmdDel.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdDel.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                                cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmdIns.Parameters.AddWithValue("@Nombre", $"{pastor.Nombres} {pastor.Apellidos}".Trim());
                                cmdIns.Parameters.AddWithValue("@Doc", pastor.DocumentoIdentidad ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Tel", pastor.Celular ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Correo", pastor.Correo ?? (object)DBNull.Value);
                                cmdIns.ExecuteNonQuery();
                            }
                            asistieronCount++;
                        }

                        // 5. Registrar Líder como asistente si aplica
                        if (liderAsistio == true)
                        {
                            string sqlIns = @"
                                INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmdIns.Parameters.AddWithValue("@Nombre", $"{lider.Nombres} {lider.Apellidos}".Trim());
                                cmdIns.Parameters.AddWithValue("@Doc", lider.DocumentoIdentidad ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Tel", lider.Celular ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Correo", lider.Correo ?? (object)DBNull.Value);
                                cmdIns.ExecuteNonQuery();
                            }
                            asistieronCount++;
                        }

                        // 6. Actualizar Asistio en EventosParticipacionIglesia
                        bool asistioCualquiera = (asistieronCount > 0);
                        string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = @Asistio WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                        using (SqlCommand cmdUp = new SqlCommand(sqlUpPart, cn, tran))
                        {
                            cmdUp.Parameters.AddWithValue("@Asistio", asistioCualquiera ? 1 : 0);
                            cmdUp.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdUp.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                            cmdSync.Parameters.AddWithValue("@Asistio", asistioCualquiera ? 1 : 0);
                            cmdSync.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdSync.ExecuteNonQuery();
                        }

                        // 8. Actualizar cantidad de asistentes en evento
                        string sqlUpCant = "UPDATE dbo.Eventos SET CantidadAsistentes = (SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento) WHERE IdEvento = @IdEvento;";
                        using (SqlCommand cmdUpCant = new SqlCommand(sqlUpCant, cn, tran))
                        {
                            cmdUpCant.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdUpCant.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia y datos del Pastor/Líder actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al registrar asistencia: " + ex.Message;
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
                        // 1. Actualizar Líder
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
                                        cmdUpM.Parameters.AddWithValue("@Nombres", m.Nombres.Trim());
                                        cmdUpM.Parameters.AddWithValue("@Apellidos", m.Apellidos ?? "");
                                        cmdUpM.Parameters.AddWithValue("@Doc", m.DocumentoIdentidad ?? (object)DBNull.Value);
                                        cmdUpM.Parameters.AddWithValue("@Cel", m.Celular ?? (object)DBNull.Value);
                                        cmdUpM.Parameters.AddWithValue("@Correo", m.Correo ?? (object)DBNull.Value);
                                        cmdUpM.Parameters.AddWithValue("@IdM", m.IdMaestro);
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
                                        cmdInsM.Parameters.AddWithValue("@IdIglesia", idIglesia);
                                        cmdInsM.Parameters.AddWithValue("@Nombres", m.Nombres.Trim());
                                        cmdInsM.Parameters.AddWithValue("@Apellidos", m.Apellidos ?? "");
                                        cmdInsM.Parameters.AddWithValue("@Doc", m.DocumentoIdentidad ?? (object)DBNull.Value);
                                        cmdInsM.Parameters.AddWithValue("@Cel", m.Celular ?? (object)DBNull.Value);
                                        cmdInsM.Parameters.AddWithValue("@Correo", m.Correo ?? (object)DBNull.Value);
                                        int newMId = Convert.ToInt32(cmdInsM.ExecuteScalar());

                                        // Si venía marcado como asistido en el checkbox correspondiente
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
                            cmdDel.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdDel.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdDel.ExecuteNonQuery();
                        }

                        int asistieronCount = 0;

                        // 4. Registrar Líder como asistente si aplica
                        if (liderAsistio == true)
                        {
                            string sqlIns = @"
                                INSERT INTO dbo.EventosAsistentes (IdEvento, IdParticipacion, NombreCompleto, Identificacion, Telefono, Correo)
                                VALUES (@IdEvento, @IdPart, @Nombre, @Doc, @Tel, @Correo);";
                            using (SqlCommand cmdIns = new SqlCommand(sqlIns, cn, tran))
                            {
                                cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                                cmdIns.Parameters.AddWithValue("@Nombre", $"{lider.Nombres} {lider.Apellidos}".Trim());
                                cmdIns.Parameters.AddWithValue("@Doc", lider.DocumentoIdentidad ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Tel", lider.Celular ?? (object)DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@Correo", lider.Correo ?? (object)DBNull.Value);
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
                                cmdGetM.Parameters.AddWithValue("@IdM", mId);
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
                                    cmdIns.Parameters.AddWithValue("@IdEvento", idEvento);
                                    cmdIns.Parameters.AddWithValue("@IdPart", idParticipacion);
                                    cmdIns.Parameters.AddWithValue("@Nombre", nomComp);
                                    cmdIns.Parameters.AddWithValue("@Doc", string.IsNullOrWhiteSpace(doc) ? (object)DBNull.Value : doc);
                                    cmdIns.Parameters.AddWithValue("@Tel", string.IsNullOrWhiteSpace(tel) ? (object)DBNull.Value : tel);
                                    cmdIns.Parameters.AddWithValue("@Correo", string.IsNullOrWhiteSpace(mail) ? (object)DBNull.Value : mail);
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
                            cmdUp.Parameters.AddWithValue("@Asistio", asistioCualquiera ? 1 : 0);
                            cmdUp.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdUp.Parameters.AddWithValue("@IdPart", idParticipacion);
                            cmdUp.ExecuteNonQuery();
                        }

                        // 7. Sincronizar ParticipacionesIglesia usando Stored Procedure
                        using (SqlCommand cmdSp = new SqlCommand("dbo.SpAvanzarEtapaTaller", cn, tran))
                        {
                            cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                            cmdSp.Parameters.AddWithValue("@IdParticipacion", idParticipacion);
                            cmdSp.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdSp.Parameters.AddWithValue("@Asistio", asistioCualquiera);
                            cmdSp.Parameters.AddWithValue("@IdUsuarioResponsable", u.IdUsuario);
                            cmdSp.ExecuteNonQuery();
                        }

                        // 8. Actualizar cantidad de asistentes en evento
                        string sqlUpCant = "UPDATE dbo.Eventos SET CantidadAsistentes = (SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento) WHERE IdEvento = @IdEvento;";
                        using (SqlCommand cmdUpCant = new SqlCommand(sqlUpCant, cn, tran))
                        {
                            cmdUpCant.Parameters.AddWithValue("@IdEvento", idEvento);
                            cmdUpCant.ExecuteNonQuery();
                        }

                        tran.Commit();
                        TempData["MensajeExito"] = "Asistencia y datos del Líder/Maestros actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["MensajeError"] = "Error al registrar asistencia: " + ex.Message;
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
                return Json(new { success = false, message = "Sesión inválida o expirada." });
            }

            if (!PuedeEditarEvento(u, idEvento))
            {
                return Json(new { success = false, message = "No tiene permisos para modificar la asistencia de este evento." });
            }

            if (idEvento <= 0 || idAsistente <= 0)
            {
                return Json(new { success = false, message = "Parámetros inválidos para la eliminación." });
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
                            // 0. Validar si el evento ya pasó
                            DateTime fechaEvento = DateTime.MinValue;
                            string sqlFecha = "SELECT Fecha FROM dbo.Eventos WHERE IdEvento = @IdEvento;";
                            using (SqlCommand cmdF = new SqlCommand(sqlFecha, cn, tran))
                            {
                                cmdF.Parameters.AddWithValue("@IdEvento", idEvento);
                                object valF = cmdF.ExecuteScalar();
                                if (valF != null && valF != DBNull.Value)
                                {
                                    fechaEvento = Convert.ToDateTime(valF);
                                }
                            }

                            if (fechaEvento != DateTime.MinValue && fechaEvento.Date < DateTime.Today && u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2)
                            {
                                tran.Rollback();
                                return Json(new { success = false, message = "El evento ya se llevó a cabo el " + fechaEvento.ToString("dd/MM/yyyy") + ". No se pueden retirar participantes de un evento que ya pasó." });
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
                                cmdGet.Parameters.AddWithValue("@IdAsistente", idAsistente);
                                cmdGet.Parameters.AddWithValue("@IdEvento", idEvento);
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
                                cmdIg.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                                    cmdM.Parameters.AddWithValue("@IdIg", idIglesia);
                                    cmdM.Parameters.AddWithValue("@Doc", cleanDoc);
                                    cmdM.Parameters.AddWithValue("@Nom", nombreCompleto.Trim());
                                    object valM = cmdM.ExecuteScalar();
                                    if (valM != null) idMaestro = Convert.ToInt32(valM);
                                }
                            }

                            // 3. Eliminar relación de dbo.EventosAsistentes
                            string sqlDelAsist = "DELETE FROM dbo.EventosAsistentes WHERE IdAsistente = @IdAsistente AND IdEvento = @IdEvento;";
                            using (SqlCommand cmdDelA = new SqlCommand(sqlDelAsist, cn, tran))
                            {
                                cmdDelA.Parameters.AddWithValue("@IdAsistente", idAsistente);
                                cmdDelA.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdDelA.ExecuteNonQuery();
                            }

                            // 4. Si existe relación en dbo.AsistenciaMaestro, eliminar solo de este evento
                            if (idMaestro > 0)
                            {
                                string sqlDelAm = "DELETE FROM dbo.AsistenciaMaestro WHERE IdEvento = @IdEvento AND IdMaestro = @IdMaestro;";
                                using (SqlCommand cmdDelAm = new SqlCommand(sqlDelAm, cn, tran))
                                {
                                    cmdDelAm.Parameters.AddWithValue("@IdEvento", idEvento);
                                    cmdDelAm.Parameters.AddWithValue("@IdMaestro", idMaestro);
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
                                cmdUpCant.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdUpCant.ExecuteNonQuery();
                            }

                            // 6. Verificar si la iglesia aún tiene asistentes en este evento
                            string sqlCountRest = "SELECT COUNT(1) FROM dbo.EventosAsistentes WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                            int restantes = 0;
                            using (SqlCommand cmdRest = new SqlCommand(sqlCountRest, cn, tran))
                            {
                                cmdRest.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdRest.Parameters.AddWithValue("@IdPart", idParticipacion);
                                restantes = Convert.ToInt32(cmdRest.ExecuteScalar());
                            }

                            if (restantes == 0)
                            {
                                string sqlUpPart = "UPDATE dbo.EventosParticipacionIglesia SET Asistio = 0 WHERE IdEvento = @IdEvento AND IdParticipacion = @IdPart;";
                                using (SqlCommand cmdUpP = new SqlCommand(sqlUpPart, cn, tran))
                                {
                                    cmdUpP.Parameters.AddWithValue("@IdEvento", idEvento);
                                    cmdUpP.Parameters.AddWithValue("@IdPart", idParticipacion);
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
                return Json(new { success = false, message = "Error de conexión o permisos: " + ex.Message });
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

                        // Sincronizar con TallerParticipo y EtapaActual usando Stored Procedure
                        string tipoEvento = "";
                        using (SqlCommand cmdEv = new SqlCommand("SELECT TipoEvento FROM dbo.Eventos WHERE IdEvento = @IdEvento;", cn, tran))
                        {
                            cmdEv.Parameters.AddWithValue("@IdEvento", idEvento);
                            object typeVal = cmdEv.ExecuteScalar();
                            if (typeVal != null) tipoEvento = typeVal.ToString();
                        }

                        if (tipoEvento == "Taller")
                        {
                            using (SqlCommand cmdSp = new SqlCommand("dbo.SpAvanzarEtapaTaller", cn, tran))
                            {
                                cmdSp.CommandType = System.Data.CommandType.StoredProcedure;
                                cmdSp.Parameters.AddWithValue("@IdParticipacion", idParticipacion);
                                cmdSp.Parameters.AddWithValue("@IdEvento", idEvento);
                                cmdSp.Parameters.AddWithValue("@Asistio", tieneAsistentes);
                                cmdSp.Parameters.AddWithValue("@IdUsuarioResponsable", u.IdUsuario);
                                cmdSp.ExecuteNonQuery();
                            }
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
