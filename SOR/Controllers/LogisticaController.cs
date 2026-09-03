using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using SOR.Models;
using SOR.Permisos;
using SOR.Services;

namespace SOR.Controllers
{
    [ValidarSesion]
    public class LogisticaController : Controller
    {
        private readonly LogisticaService _svc = new LogisticaService();

        private static string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        private bool TieneAccesoLogistica(Usuario u)
        {
            if (u == null) return false;
            // 1. SuperAdmin y Administrador
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return true;

            // 2. Coordinador de Equipo (IdPosicion = 1)
            if (u.IdPosicion == 1 || (!string.IsNullOrEmpty(u.NombrePosicion) && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0)) return true;

            // 3. Coordinador de Logística (IdPosicion = 6)
            if (u.IdPosicion == 6 || (!string.IsNullOrEmpty(u.NombrePosicion) && (u.NombrePosicion.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0 || u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0))) return true;

            return false;
        }

        private HashSet<int> ObtenerEquiposPermitidosJerarquico(Usuario u)
        {
            HashSet<int> set = new HashSet<int>();
            if (u == null) return set;
            if (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2) return null; // Admin: todos

            if (u.IdEquipo.HasValue && u.IdEquipo.Value > 0)
            {
                set.Add(u.IdEquipo.Value);
                ObtenerEquiposHijosRecursivo(u.IdEquipo.Value, set);
            }
            return set;
        }

        private void ObtenerEquiposHijosRecursivo(int idEquipoPadre, HashSet<int> set)
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = "SELECT IdEquipo FROM dbo.Equipos WHERE IdEquipoPadre = @Id AND Activo = 1;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEquipoPadre);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int hId = Convert.ToInt32(dr["IdEquipo"]);
                            if (!set.Contains(hId))
                            {
                                set.Add(hId);
                                ObtenerEquiposHijosRecursivo(hId, set);
                            }
                        }
                    }
                }
            }
        }

        private int ObtenerTemporadaActiva()
        {
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;", cn))
                {
                    object val = cmd.ExecuteScalar();
                    return val != null ? Convert.ToInt32(val) : 0;
                }
            }
        }

        // =====================================================================
        // DASHBOARD / INDEX
        // =====================================================================

        public ActionResult Index()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            int idTemporada = ObtenerTemporadaActiva();
            int? idEquipo = u?.IdEquipo;

            var vm = new DashboardLogistico
            {
                ResumenCentral = _svc.ObtenerResumenInventarioCentral(idTemporada),
                InventarioCentral = _svc.ObtenerInventarioCentral(idTemporada),
                InventarioEquipo = _svc.ObtenerInventarioEquipo(idTemporada, idEquipo)
            };

            // Obtener nombre de la temporada activa
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlTemp = "SELECT NombreTemporada FROM dbo.Temporadas WHERE IdTemporada=@Id;";
                using (var cmd = new SqlCommand(sqlTemp, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idTemporada);
                    object val = cmd.ExecuteScalar();
                    vm.NombreTemporada = val != null ? val.ToString() : "";
                }
            }

            ViewBag.UsuarioActual = u;
            return View(vm);
        }

        // =====================================================================
        // ALMACENES (AUTORIZACIÓN ESTRICTA CL / CE)
        // =====================================================================

        public ActionResult Almacenes()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0) || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0));
            bool esCE = u != null && (u.IdPosicion == 1 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0));

            ViewBag.UsuarioActual = u;
            ViewBag.EsAdmin = esAdmin;
            ViewBag.EsCL = esCL;
            ViewBag.EsCE = esCE;
            ViewBag.IdEquipoUsuario = u?.IdEquipo;
            ViewBag.NombreEquipoUsuario = u?.NombreEquipo;
            ViewBag.UsuariosResponsables = ObtenerUsuariosCoordinadoresSelect(esAdmin || esCL ? (int?)null : u?.IdEquipo);
            
            // Cargar Equipos disponibles
            var equipos = new List<SelectListItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT IdEquipo, NombreEquipo FROM dbo.Equipos ORDER BY NombreEquipo;", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        equipos.Add(new SelectListItem 
                        { 
                            Value = dr["IdEquipo"].ToString(), 
                            Text = dr["NombreEquipo"].ToString() 
                        });
                    }
                }
            }
            ViewBag.EquiposDisponibles = equipos;

            return View(_svc.ObtenerAlmacenes(false));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarAlmacen(Almacen modelo, int[] idsEquiposSeleccionados)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
                bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0) || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0));
                bool esCE = u != null && (u.IdPosicion == 1 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0));

                if (!esAdmin && !esCL && !esCE)
                {
                    TempData["MensajeError"] = "Acceso denegado: Únicamente el Coordinador de Logística (CL) o el Coordinador de Equipo (CE) pueden crear o editar almacenes.";
                    return RedirectToAction("Almacenes");
                }

                if (esCE && !esCL && !esAdmin)
                {
                    // Coordinador de Equipo: forzar almacén local de su propio equipo
                    if (!u.IdEquipo.HasValue || u.IdEquipo.Value <= 0)
                    {
                        TempData["MensajeError"] = "Su usuario no tiene un equipo asignado para vincular el almacén.";
                        return RedirectToAction("Almacenes");
                    }
                    modelo.EsCentral = false;
                    modelo.IdsEquipos = new List<int> { u.IdEquipo.Value };
                }
                else
                {
                    // CL o Admin: asignar equipos seleccionados si no es central
                    if (!modelo.EsCentral && idsEquiposSeleccionados != null && idsEquiposSeleccionados.Length > 0)
                    {
                        modelo.IdsEquipos = new List<int>(idsEquiposSeleccionados);
                    }
                }

                _svc.GuardarAlmacen(modelo);
                TempData["MensajeExito"] = modelo.IdAlmacen == 0 ? "Almacén registrado exitosamente." : "Almacén actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al guardar el almacén: " + ex.Message;
            }
            return RedirectToAction("Almacenes");
        }

        // =====================================================================
        // PRESENTACIONES
        // =====================================================================

        public ActionResult Presentaciones(int? idTemporada)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.UsuarioActual = u;
            ViewBag.Materiales = _svc.ObtenerMateriales(false);

            // Cargar lista de Temporadas para el selector
            var temporadas = new List<SelectListItem>();
            var tiposEmpaque = new List<string>();

            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlT = "SELECT IdTemporada, NombreTemporada, Activa FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                using (var cmd = new SqlCommand(sqlT, cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        bool activa = Convert.ToBoolean(dr["Activa"]);
                        temporadas.Add(new SelectListItem
                        {
                            Value = dr["IdTemporada"].ToString(),
                            Text = dr["NombreTemporada"].ToString() + (activa ? " (Activa)" : "")
                        });
                    }
                }

                // Cargar Tipos de Empaque configurados desde el catálogo
                try
                {
                    string sqlE = "SELECT Nombre FROM dbo.TiposEmpaque WHERE Activo = 1 ORDER BY IdTipoEmpaque ASC;";
                    using (var cmdE = new SqlCommand(sqlE, cn))
                    using (var drE = cmdE.ExecuteReader())
                    {
                        while (drE.Read())
                        {
                            tiposEmpaque.Add(drE["Nombre"].ToString());
                        }
                    }
                }
                catch
                {
                    // Fallback si la tabla aún se está inicializando
                }
            }

            if (!tiposEmpaque.Any())
            {
                tiposEmpaque.AddRange(new[] { "Caja", "Paquete", "Bolsa", "Rollo", "Resma", "Atado", "Fardo / Palet", "Unidad Suelta", "Otro" });
            }

            ViewBag.Temporadas = temporadas;
            ViewBag.TiposEmpaque = tiposEmpaque;
            ViewBag.IdTemporadaSeleccionada = idTemporada;

            return View(_svc.ObtenerPresentaciones(false, idTemporada));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarPresentacion(PresentacionMaterial modelo)
        {
            try
            {
                Usuario u = (Usuario)Session["usuario"];
                if (!TieneAccesoLogistica(u))
                {
                    TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                    return RedirectToAction("Index", "Home");
                }
                if (modelo.UnidadesPorEmpaque <= 0)
                {
                    TempData["MensajeError"] = "Las unidades por empaque deben ser mayores a cero.";
                    return RedirectToAction("Presentaciones", new { idTemporada = modelo.IdTemporadaVigencia });
                }

                _svc.GuardarPresentacion(modelo);
                TempData["MensajeExito"] = modelo.IdPresentacion == 0 ? "Presentación registrada correctamente." : "Presentación actualizada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al guardar la presentación: " + ex.Message;
            }
            return RedirectToAction("Presentaciones", new { idTemporada = modelo.IdTemporadaVigencia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlternarEstadoPresentacion(int idPresentacion, bool activo, int? idTemporada)
        {
            try
            {
                Usuario u = (Usuario)Session["usuario"];
                if (!TieneAccesoLogistica(u))
                {
                    TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                    return RedirectToAction("Index", "Home");
                }
                _svc.AlternarEstadoPresentacion(idPresentacion, activo);
                TempData["MensajeExito"] = activo ? "Presentación habilitada exitosamente." : "Presentación inhabilitada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cambiar el estado: " + ex.Message;
            }
            return RedirectToAction("Presentaciones", new { idTemporada = idTemporada });
        }

        // =====================================================================
        // RECEPCIONES DE CONTENEDORES
        // =====================================================================

        public ActionResult Recepciones()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            int idTemp = ObtenerTemporadaActiva();
            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0) || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0));

            ViewBag.UsuarioActual = u;
            ViewBag.EsAdmin = esAdmin;
            ViewBag.EsCL = esCL;
            ViewBag.Materiales = _svc.ObtenerMateriales();
            ViewBag.Presentaciones = _svc.ObtenerPresentaciones();
            ViewBag.Almacenes = _svc.ObtenerAlmacenesPorEquipo(esAdmin || esCL ? (int?)null : u?.IdEquipo);
            ViewBag.UsuariosResponsables = ObtenerUsuariosCoordinadoresSelect();
            ViewBag.IdTemporadaActiva = idTemp;

            // Cargar Equipos para destino opcional
            var equipos = new List<SelectListItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT IdEquipo, NombreEquipo FROM dbo.Equipos ORDER BY NombreEquipo;", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        equipos.Add(new SelectListItem { Value = dr["IdEquipo"].ToString(), Text = dr["NombreEquipo"].ToString() });
                    }
                }
            }
            ViewBag.Equipos = equipos;

            return View(_svc.ObtenerRecepciones(idTemp, null, esAdmin || esCL ? (int?)null : u?.IdEquipo));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarRecepcion(string numeroContenedor, int idAlmacen, DateTime fechaRecepcion,
            string horaRecepcion, int? idEquipoReceptor, string responsableRecepcion, string observaciones, 
            string detallesJson, System.Web.HttpPostedFileBase[] evidenciasArchivos)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                var detalles = string.IsNullOrEmpty(detallesJson)
                    ? new List<RecepcionContenedorDetalle>()
                    : JsonConvert.DeserializeObject<List<RecepcionContenedorDetalle>>(detallesJson);

                var listaEvidencias = new List<EvidenciaRecepcion>();
                if (evidenciasArchivos != null && evidenciasArchivos.Length > 0)
                {
                    string uploadDir = Server.MapPath("~/Uploads/Recepciones/");
                    if (!System.IO.Directory.Exists(uploadDir))
                    {
                        System.IO.Directory.CreateDirectory(uploadDir);
                    }

                    foreach (var file in evidenciasArchivos)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = System.IO.Path.GetFileName(file.FileName);
                            string uniqueName = $"{Guid.NewGuid()}_{fileName}";
                            string fullPath = System.IO.Path.Combine(uploadDir, uniqueName);
                            file.SaveAs(fullPath);

                            listaEvidencias.Add(new EvidenciaRecepcion
                            {
                                NombreArchivo = fileName,
                                RutaArchivo = $"/Uploads/Recepciones/{uniqueName}",
                                TipoContenido = file.ContentType,
                                TamanoBytes = file.ContentLength
                            });
                        }
                    }
                }

                var modelo = new RecepcionContenedor
                {
                    NumeroContenedor = numeroContenedor,
                    IdAlmacen = idAlmacen,
                    FechaRecepcion = fechaRecepcion,
                    HoraRecepcion = !string.IsNullOrWhiteSpace(horaRecepcion) ? horaRecepcion : DateTime.Now.ToString("hh:mm tt"),
                    IdEquipoReceptor = idEquipoReceptor,
                    ResponsableRecepcion = responsableRecepcion,
                    Observaciones = observaciones,
                    Detalles = detalles,
                    Evidencias = listaEvidencias
                };

                int idRec = _svc.RegistrarRecepcion(modelo, u.IdUsuario);
                TempData["MensajeExito"] = $"Contenedor '{numeroContenedor}' registrado y confirmado exitosamente (Recepción #{idRec}).";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al registrar la recepción: " + ex.Message;
            }
            return RedirectToAction("Recepciones");
        }

        public ActionResult ComprobanteRecepcion(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            var modelo = _svc.ObtenerRecepcionDetalle(id);
            if (modelo == null) return HttpNotFound();
            ViewBag.UsuarioActual = u;
            return View(modelo);
        }

        // =====================================================================
        // INVENTARIO CENTRAL
        // =====================================================================

        public ActionResult InventarioCentral()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            int idTemp = ObtenerTemporadaActiva();
            ViewBag.UsuarioActual = u;
            ViewBag.IdTemporadaActiva = idTemp;

            var modelo = _svc.ObtenerResumenInventarioCentral(idTemp);
            return View(modelo);
        }

        // =====================================================================
        // TRANSFERENCIAS A EQUIPOS
        // =====================================================================

        public ActionResult Transferencias()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            int idTemp = ObtenerTemporadaActiva();
            ViewBag.UsuarioActual = u;
            ViewBag.Materiales = _svc.ObtenerMateriales();
            ViewBag.Almacenes = _svc.ObtenerAlmacenes();
            ViewBag.IdTemporadaActiva = idTemp;

            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            HashSet<int> equiposPermitidos = ObtenerEquiposPermitidosJerarquico(u);
            ViewBag.EsAdmin = esAdmin;
            ViewBag.IdEquipoUsuario = u?.IdEquipo;

            // Equipos permitidos por jerarquía
            var equipos = new List<SelectListItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT IdEquipo, NombreEquipo FROM dbo.Equipos WHERE Activo = 1 ORDER BY NombreEquipo;", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int eqId = Convert.ToInt32(dr["IdEquipo"]);
                        if (equiposPermitidos == null || equiposPermitidos.Contains(eqId))
                        {
                            equipos.Add(new SelectListItem { Value = eqId.ToString(), Text = dr["NombreEquipo"].ToString() });
                        }
                    }
                }
            }
            ViewBag.Equipos = equipos;

            // Coordinadores Emisores (del equipo del usuario si tiene equipo asignado, o todos si es admin)
            ViewBag.CoordinadoresEmisores = ObtenerUsuariosCoordinadoresSelect(esAdmin ? (int?)null : u?.IdEquipo);
            // Todos los usuarios/coordinadores para receptores
            ViewBag.UsuariosResponsables = ObtenerUsuariosCoordinadoresSelect();

            return View(_svc.ObtenerTransferencias(idTemp, esAdmin ? (int?)null : u?.IdEquipo));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarTransferencia(int idEquipo, int idAlmacenOrigen, DateTime? fechaTransferencia = null,
            int? idEquipoEmisor = null, DateTime? fechaEmision = null, DateTime? fechaRecepcion = null,
            string coordinadorEmisor = null, string personaReceptoraEquipo = null, string observaciones = null, string detallesJson = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
                
                // Determinar Equipo Emisor server-side
                int? eqEmisorFinal = idEquipoEmisor;
                if (!esAdmin && u != null && u.IdEquipo.HasValue)
                {
                    eqEmisorFinal = u.IdEquipo.Value;
                }

                if (eqEmisorFinal.HasValue && eqEmisorFinal.Value == idEquipo)
                {
                    TempData["MensajeError"] = "El equipo emisor y el equipo receptor no pueden ser el mismo.";
                    return RedirectToAction("Transferencias");
                }

                DateTime fEmision = fechaEmision ?? fechaTransferencia ?? DateTime.Now;
                if (fechaRecepcion.HasValue && fechaRecepcion.Value < fEmision)
                {
                    TempData["MensajeError"] = "La fecha de recepción no puede ser anterior a la fecha de emisión.";
                    return RedirectToAction("Transferencias");
                }

                var detalles = string.IsNullOrEmpty(detallesJson)
                    ? new List<TransferenciaEquipoDetalle>()
                    : JsonConvert.DeserializeObject<List<TransferenciaEquipoDetalle>>(detallesJson);

                if (detalles == null || detalles.Count == 0)
                {
                    TempData["MensajeError"] = "Debe agregar al menos un material para transferir.";
                    return RedirectToAction("Transferencias");
                }

                var modelo = new TransferenciaEquipo
                {
                    IdEquipo = idEquipo, // Destino
                    IdEquipoEmisor = eqEmisorFinal,
                    IdAlmacenOrigen = idAlmacenOrigen,
                    FechaTransferencia = fEmision,
                    FechaEmision = fEmision,
                    FechaRecepcion = fechaRecepcion,
                    CoordinadorEmisor = coordinadorEmisor,
                    PersonaReceptoraEquipo = personaReceptoraEquipo,
                    Observaciones = observaciones,
                    Detalles = detalles
                };

                int idTransf = _svc.RegistrarTransferencia(modelo, u.IdUsuario);
                TempData["MensajeExito"] = $"Transferencia #{idTransf} registrada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al registrar la transferencia: " + ex.Message;
            }
            return RedirectToAction("Transferencias");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarRecepcion(int idTransferencia, DateTime? fechaRecepcion = null, string personaReceptora = null, int? idUsuarioReceptor = null)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                DateTime fRec = fechaRecepcion ?? DateTime.Now;
                _svc.ConfirmarRecepcionTransferencia(idTransferencia, fRec, personaReceptora, idUsuarioReceptor, u.IdUsuario);
                TempData["MensajeExito"] = "Recepción de materiales confirmada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al confirmar la recepción: " + ex.Message;
            }
            return RedirectToAction("Transferencias");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarTransferencia(int idTransferencia, string motivo)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso no autorizado al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            try
            {
                _svc.CancelarTransferencia(idTransferencia, motivo, u.IdUsuario);
                TempData["MensajeExito"] = "Transferencia cancelada exitosamente y el inventario fue reincorporado.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cancelar la transferencia: " + ex.Message;
            }
            return RedirectToAction("Transferencias");
        }

        public ActionResult ConstanciaEquipo(int id)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido al módulo de Logística.";
                return RedirectToAction("Index", "Home");
            }
            var modelo = _svc.ObtenerTransferenciaDetalle(id);
            if (modelo == null) return HttpNotFound();
            ViewBag.UsuarioActual = u;
            return View(modelo);
        }

        // =====================================================================
        // INVENTARIO POR EQUIPO
        // =====================================================================

        public ActionResult InventarioEquipos()
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!TieneAccesoLogistica(u))
            {
                TempData["MensajeError"] = "Acceso restringido: El módulo de Logística solo está disponible para el Coordinador de Logística, Coordinador de Equipo o Administrador.";
                return RedirectToAction("Index", "Home");
            }
            int idTemp = ObtenerTemporadaActiva();
            ViewBag.UsuarioActual = u;
            ViewBag.IdTemporadaActiva = idTemp;

            bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            HashSet<int> equiposPermitidos = ObtenerEquiposPermitidosJerarquico(u);

            var equipos = new List<SelectListItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT IdEquipo, NombreEquipo FROM dbo.Equipos WHERE Activo = 1 ORDER BY NombreEquipo;", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int eqId = Convert.ToInt32(dr["IdEquipo"]);
                        if (equiposPermitidos == null || equiposPermitidos.Contains(eqId))
                        {
                            equipos.Add(new SelectListItem { Value = eqId.ToString(), Text = dr["NombreEquipo"].ToString() });
                        }
                    }
                }
            }
            ViewBag.Equipos = equipos;

            int? idEquipoFiltro = (esAdmin || (equiposPermitidos != null && equiposPermitidos.Count > 1)) ? (int?)null : u?.IdEquipo;
            var resumen = _svc.ObtenerResumenInventarioEquipos(idTemp, idEquipoFiltro);
            if (equiposPermitidos != null)
            {
                resumen = resumen.Where(r => equiposPermitidos.Contains(r.IdEquipo)).ToList();
            }
            return View(resumen);
        }

        // =====================================================================
        // EVENTOS DE DESPACHO (REDIRECCIÓN AL MÓDULO CENTRAL DE EVENTOS)
        // =====================================================================

        public ActionResult EventosDespacho()
        {
            return RedirectToAction("Index", "Eventos");
        }

        public ActionResult DetalleEventoDespacho(int id)
        {
            return RedirectToAction("Detalle", "Eventos", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearEventoDespacho(int idEvento, int? idAlmacen)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                if (!u.IdEquipo.HasValue) throw new InvalidOperationException("El usuario no tiene un equipo asignado.");
                _svc.CrearEventoDespacho(idEvento, u.IdEquipo.Value, idAlmacen, u.IdUsuario);
                TempData["MensajeExito"] = "Evento de despacho configurado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("EventosDespacho");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProgramarIglesiaEnDespacho(int idEvento, int idParticipacion, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                int idTemp = ObtenerTemporadaActiva();
                if (!u.IdEquipo.HasValue) throw new InvalidOperationException("El usuario no tiene un equipo asignado.");
                _svc.ProgramarIglesiaEnDespacho(idEvento, idParticipacion, idIglesia, u.IdEquipo.Value, idTemp, u.IdUsuario);
                TempData["MensajeExito"] = "Iglesia agregada al evento de despacho.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
        }

        // =====================================================================
        // CONFIRMAR DESPACHO (EXCLUSIVO COORDINADOR DE LOGÍSTICA — CL)
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarDespacho(ConfirmarDespachoViewModel vm, int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                bool esAdmin = u != null && (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
                bool esCL = u != null && (u.IdPosicion == 6 || (u.NombrePosicion != null && (u.NombrePosicion.IndexOf("Logística", StringComparison.OrdinalIgnoreCase) >= 0 || u.NombrePosicion.IndexOf("Logistica", StringComparison.OrdinalIgnoreCase) >= 0)));
                bool esCE = u != null && (u.IdPosicion == 1 || (u.NombrePosicion != null && u.NombrePosicion.IndexOf("Equipo", StringComparison.OrdinalIgnoreCase) >= 0));

                if (!esAdmin && !esCL && !esCE)
                {
                    TempData["MensajeError"] = "Acceso denegado: Únicamente el Coordinador de Logística (CL) o el Coordinador de Equipo (CE) tienen autorización para confirmar y ejecutar el despacho de materiales.";
                    return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
                }

                int idTemp = ObtenerTemporadaActiva();
                if (!u.IdEquipo.HasValue) throw new InvalidOperationException("El usuario no tiene un equipo asignado.");
                string nombre = !string.IsNullOrEmpty(u.PrimerNombre) ? $"{u.PrimerNombre} {u.PrimerApellido}".Trim() : (u.Correo ?? "Coordinador de Logística");
                _svc.ConfirmarDespacho(vm, u.IdEquipo.Value, idTemp, u.IdUsuario, nombre, u.IdRolSeguridad, u.IdPosicion);
                TempData["MensajeExito"] = "Despacho presencial confirmado exitosamente con cédula validada.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al confirmar el despacho: " + ex.Message;
            }
            return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarNoDespacho(NoDespachoBecauseViewModel vm, int idEvento)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                _svc.MarcarNoDespacho(vm, u.IdUsuario);
                TempData["MensajeExito"] = "Iglesia registrada como NO DESPACHADA. Queda disponible para reprogramación.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
        }

        public ActionResult ComprobanteDespachoIglesia(int id)
        {
            var modelo = _svc.ObtenerDespachoDetalle(id);
            if (modelo == null) return HttpNotFound();
            ViewBag.UsuarioActual = (Usuario)Session["usuario"];
            return View(modelo);
        }

        // =====================================================================
        // KÁRDEX
        // =====================================================================

        public ActionResult Kardex()
        {
            Usuario u = (Usuario)Session["usuario"];
            int idTemp = ObtenerTemporadaActiva();
            ViewBag.UsuarioActual = u;
            ViewBag.Materiales = _svc.ObtenerMateriales();
            ViewBag.IdTemporadaActiva = idTemp;
            return View(_svc.ObtenerKardex(idTemp));
        }

        // =====================================================================
        // COORDINADORES EN EVENTO DE DESPACHO
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarCoordinadorEvento(int idEvento, int idUsuario)
        {
            Usuario u = (Usuario)Session["usuario"];
            try
            {
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    // Verificar no duplicado
                    using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.CoordinadoresEventoDespacho WHERE IdEvento=@IdEv AND IdUsuario=@IdU;", cn))
                    {
                        cmd.Parameters.AddWithValue("@IdEv", idEvento);
                        cmd.Parameters.AddWithValue("@IdU", idUsuario);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        {
                            TempData["MensajeError"] = "Este coordinador ya está registrado en el evento.";
                            return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
                        }
                    }
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.CoordinadoresEventoDespacho (IdEvento, IdUsuario, Presente) VALUES (@IdEv, @IdU, 1);", cn))
                    {
                        cmd.Parameters.AddWithValue("@IdEv", idEvento);
                        cmd.Parameters.AddWithValue("@IdU", idUsuario);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["MensajeExito"] = "Coordinador registrado en el evento.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("DetalleEventoDespacho", new { id = idEvento });
        }

        // =====================================================================
        // API JSON — OBTENER INVENTARIO DEL EQUIPO (para dinámico en modal)
        // =====================================================================

        [HttpGet]
        public JsonResult ObtenerStockEquipo(int idMaterial)
        {
            Usuario u = (Usuario)Session["usuario"];
            int idTemp = ObtenerTemporadaActiva();
            int stock = 0;
            if (u?.IdEquipo.HasValue == true)
            {
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT ISNULL(CantidadDisponible,0) FROM dbo.InventarioEquipo WHERE IdTemporada=@T AND IdEquipo=@Eq AND IdMaterial=@M;", cn))
                    {
                        cmd.Parameters.AddWithValue("@T", idTemp);
                        cmd.Parameters.AddWithValue("@Eq", u.IdEquipo.Value);
                        cmd.Parameters.AddWithValue("@M", idMaterial);
                        object val = cmd.ExecuteScalar();
                        stock = val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
                    }
                }
            }
            return Json(new { stock }, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // API JSON — IGLESIAS DISPONIBLES PARA DESPACHO (para modal dinámico)
        // =====================================================================

        [HttpGet]
        public JsonResult IglesiasDisponibles()
        {
            Usuario u = (Usuario)Session["usuario"];
            int idTemp = ObtenerTemporadaActiva();
            if (u?.IdEquipo == null) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var lista = _svc.ObtenerIglesiasDisponiblesDespacho(u.IdEquipo.Value, idTemp);
            var result = new List<object>();
            foreach (var item in lista)
            {
                result.Add(new
                {
                    item.IdParticipacion,
                    item.IdIglesia,
                    item.NombreIglesia,
                    item.DireccionIglesia
                });
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // =====================================================================
        // API JSON — MATERIALES DE UN DESPACHO ESPECÍFICO
        // =====================================================================

        [HttpGet]
        public JsonResult ObtenerDespachoMateriales(int id)
        {
            var despacho = _svc.ObtenerDespachoDetalle(id);
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

        // =====================================================================
        // API JSON — EVENTOS DISPONIBLES PARA DESPACHO (tipo Despacho)
        // =====================================================================

        [HttpGet]
        public JsonResult EventosDisponiblesParaDespacho()
        {
            int idTemp = ObtenerTemporadaActiva();
            var result = new List<object>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = @"
                    SELECT e.IdEvento, e.NombreEvento, e.Fecha, e.Lugar
                    FROM dbo.Eventos e
                    WHERE e.IdTemporada = @IdTemp
                      AND e.TipoEvento = 'Despacho'
                      AND NOT EXISTS (SELECT 1 FROM dbo.EventosDespacho ed WHERE ed.IdEvento = e.IdEvento)
                    ORDER BY e.Fecha DESC;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTemp", idTemp);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            result.Add(new
                            {
                                IdEvento = Convert.ToInt32(dr["IdEvento"]),
                                NombreEvento = dr["NombreEvento"].ToString(),
                                Fecha = dr["Fecha"] != System.DBNull.Value ? Convert.ToDateTime(dr["Fecha"]).ToString("dd/MM/yyyy") : "",
                                Lugar = dr["Lugar"] != System.DBNull.Value ? dr["Lugar"].ToString() : ""
                            });
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<SelectListItem> ObtenerUsuariosCoordinadoresSelect(int? idEquipo = null)
        {
            var lista = new List<SelectListItem>();
            using (var cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sql = @"
                    SELECT u.IdUsuario, u.Correo, p.PrimerNombre, p.PrimerApellido, eq.NombreEquipo, pos.NombrePosicion
                    FROM dbo.Usuarios u
                    LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
                    LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
                    LEFT JOIN dbo.PosicionesOCC pos ON COALESCE(a.IdPosicion, p.IdPosicion) = pos.IdPosicion
                    WHERE u.IdEstado = 4 " +
                    (idEquipo.HasValue ? " AND (a.IdEquipo = @IdEquipo OR a.IdEquipo IS NULL) " : "") +
                    @"ORDER BY ISNULL(p.PrimerNombre, u.Correo);";

                using (var cmd = new SqlCommand(sql, cn))
                {
                    if (idEquipo.HasValue) cmd.Parameters.AddWithValue("@IdEquipo", idEquipo.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string correo = dr["Correo"].ToString();
                            string pNombre = dr["PrimerNombre"] != DBNull.Value ? dr["PrimerNombre"].ToString().Trim() : "";
                            string pApellido = dr["PrimerApellido"] != DBNull.Value ? dr["PrimerApellido"].ToString().Trim() : "";
                            string nombreCompleto = (pNombre + " " + pApellido).Trim();
                            string pos = dr["NombrePosicion"] != DBNull.Value ? dr["NombrePosicion"].ToString().Trim() : "";
                            string equipo = dr["NombreEquipo"] != DBNull.Value ? dr["NombreEquipo"].ToString().Trim() : "";

                            string val = !string.IsNullOrEmpty(nombreCompleto) ? nombreCompleto : correo;
                            string label = !string.IsNullOrEmpty(nombreCompleto) ? nombreCompleto : correo;

                            List<string> meta = new List<string>();
                            if (!string.IsNullOrEmpty(pos)) meta.Add(pos);
                            if (!string.IsNullOrEmpty(equipo)) meta.Add(equipo);

                            if (meta.Count > 0)
                            {
                                label += " (" + string.Join(" - ", meta) + ")";
                            }

                            lista.Add(new SelectListItem { Value = val, Text = label });
                        }
                    }
                }
            }
            return lista;
        }
    }
}
