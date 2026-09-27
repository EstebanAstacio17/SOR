using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using SOR.Helpers;
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
            return SOR.Helpers.ConnectionHelper.ObtenerCadenaConexion();
        }

        // GET: Iglesia/Index
        public ActionResult Index(int? idTemporada, string denominacion, string tipoOrg, int? etapaProcess, string estadoParticipacion, string estatusEvalReporte)
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
            if (!string.IsNullOrEmpty(estadoParticipacion))
            {
                listaFiltrada = listaFiltrada.Where(x => x.ParticipacionActual != null && x.ParticipacionActual.EstadoEvaluacion.Equals(estadoParticipacion, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(estatusEvalReporte))
            {
                listaFiltrada = listaFiltrada.Where(x => x.ParticipacionActual != null && x.ParticipacionActual.EstatusEvaluacionReporte.Equals(estatusEvalReporte, StringComparison.OrdinalIgnoreCase));
            }

            CargarCombosFiltros();
            
            // Cargar Equipos para el filtro de la vista (Caché en RAM de 30 minutos)
            ViewBag.ListaEquipos = CacheHelper.ObtenerOAgregar("Catalogo_Equipos_Select", () =>
            {
                var listaEq = new List<SelectListItem>();
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
                return listaEq;
            }, 30);

            ViewBag.UsuarioActual = u;
            return View(listaFiltrada.ToList());
        }

        public ActionResult DescargarPlantillaIglesias()
        {
            string path = Server.MapPath("~/Documentos Descargables/Plantilla_Carga_Masiva_Iglesias.xlsx");
            if (System.IO.File.Exists(path))
            {
                return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Carga_Masiva_Iglesias.xlsx");
            }

            string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentos Descargables", "Plantilla_Carga_Masiva_Iglesias.xlsx");
            if (System.IO.File.Exists(fallbackPath))
            {
                return File(fallbackPath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Carga_Masiva_Iglesias.xlsx");
            }

            // Fallback garantizado en formato CSV compatible con Excel
            string encabezadosCsv = "NO,NombredeIglesia_Organización,RNC_Cedula,Telefono,CorreoInstitucion,Provincia,Ciudad,Sector,Calle,Numero,Próxima a que esta la Iglesia?,Denominacion,PastorNombre,PastorIdentificacion,PastorCelular,PastorCorreo,LiderNombre,LiderIdentificacion,LiderCelular,LiderCorreo,CantMaestros,CantNinos,Reporto\r\n1,Iglesia Ejemplo Central,00100000000,8095551234,contacto@iglesiaejemplo.com,Santo Domingo,Santo Domingo Este,Ensanche Ozama,Calle Principal,10,Frente al Parque,Bautista,Juan Perez,00111111111,8095551111,juan@iglesiaejemplo.com,Maria Gomez,00122222222,8095552222,maria@iglesiaejemplo.com,5,50,SI\r\n";
            byte[] csvBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(encabezadosCsv)).ToArray();
            return File(csvBytes, "text/csv; charset=utf-8", "Plantilla_Carga_Masiva_Iglesias.csv");
        }

        public ActionResult DescargarCondicionesImportacion()
        {
            string path = Server.MapPath("~/Documentos Descargables/Condiciones_Importacion_Iglesias.txt");
            if (System.IO.File.Exists(path))
            {
                return File(path, "text/plain; charset=utf-8", "Condiciones_Importacion_Iglesias.txt");
            }

            string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentos Descargables", "Condiciones_Importacion_Iglesias.txt");
            if (System.IO.File.Exists(fallbackPath))
            {
                return File(fallbackPath, "text/plain; charset=utf-8", "Condiciones_Importacion_Iglesias.txt");
            }

            string guiaTexto = @"GUÍA Y CONDICIONES PARA CARGA MASIVA DE IGLESIAS (NUEVA PLATAFORMA SOR)
========================================================================

Para procesar adecuadamente la carga masiva mediante la plantilla Excel, el archivo debe cumplir estrictamente con el siguiente orden de columnas (23 Columnas en total). 

1. ESTRUCTURA Y ORDEN DE LAS COLUMNAS:
------------------------------------------------------------
Columna A (1): NO                           - [Se ignora. Puede venir vacío o con números].
Columna B (2): NombredeIglesia_Organización - [Texto] Nombre completo de la iglesia. (Obligatorio).
Columna C (3): RNC_Cedula                   - [Cédula o RNC] (Obligatorio).
Columna D (4): Telefono                     - [Texto numérico, 10 dígitos] (Obligatorio).
Columna E (5): CorreoInstitucion            - [Texto] Correo electrónico oficial de la iglesia.
Columna F (6): Provincia                    - [Texto] Provincia donde se ubica la iglesia (Obligatorio).
Columna G (7): Ciudad                       - [Texto] Ciudad o municipio (Obligatorio).
Columna H (8): Sector                       - [Texto] Barrio o sector (Obligatorio).
Columna I (9): Calle                        - [Texto] Dirección (Obligatorio).
Columna J (10): Numero                      - [Texto/Número]
Columna K (11): Próxima a que esta la Iglesia? - [Texto] Punto de referencia para ubicación.
Columna L (12): Denominacion                - [Texto] Ej: Pentecostal, Bautista, etc.
Columna M (13): PastorNombre                - [Texto] Nombre completo del Pastor. (Obligatorio).
Columna N (14): PastorIdentificacion        - [Cédula del Pastor] (Opcional en la carga masiva).
Columna O (15): PastorCelular               - [Texto numérico, 10 dígitos] (Obligatorio).
Columna P (16): PastorCorreo                - [Texto] Correo del pastor.
Columna Q (17): LiderNombre                 - [Texto] Nombre completo del Líder. (Obligatorio).
Columna R (18): LiderIdentificacion         - [Cédula del Líder] (Opcional en la carga masiva).
Columna S (19): LiderCelular                - [Texto numérico, 10 dígitos] (Obligatorio).
Columna T (20): LiderCorreo                 - [Texto] Correo del líder.
Columna U (21): CantMaestros                - [Número entero].
Columna V (22): CantNinos                   - [Número entero].
Columna W (23): Reporto                     - [Texto: SI o NO]. Si se deja vacío, el sistema asumirá que 'NO' reportó.

2. PROCEDIMIENTO DE PREVISUALIZACIÓN Y CAMPOS VACÍOS:
-----------------------------------------------------------
- Los registros se cargan inicialmente en una pantalla temporal (Grid de Previsualización).
- Si la plantilla trae campos vacíos en columnas obligatorias, se resaltarán en rojo en la pantalla de previsualización.
- EL SISTEMA NO PERMITIRÁ GUARDAR LOS DATOS DEFINITIVAMENTE hasta que se llenen o corrijan manualmente todos los campos requeridos marcados en rojo.

3. ASIGNACIÓN AUTOMÁTICA DE EQUIPOS:
-----------------------------------------------------------
- Las iglesias importadas serán asignadas automáticamente al Equipo OCC al cual pertenezca el usuario que esté realizando la carga masiva.
- Se registrarán con el tipo de institución por defecto: 'Iglesia'.
";
            byte[] txtBytes = System.Text.Encoding.UTF8.GetBytes(guiaTexto);
            return File(txtBytes, "text/plain; charset=utf-8", "Condiciones_Importacion_Iglesias.txt");
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

            CargarEquiposDisponibles();
            CargarCatalogosDenominacionesYTipos();
            return View(new Iglesia());
        }

        // ==========================================
        // VALIDACIÓN DE SEGURIDAD PARA ARCHIVOS
        // ==========================================
        private static readonly HashSet<string> ExtensionesPermitidasAdjuntos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png"
        };

        private static bool ValidarArchivoSeguroIglesia(HttpPostedFileBase archivo, out string error)
        {
            error = string.Empty;
            if (archivo == null || archivo.ContentLength == 0) return true;

            // Límite: 5 MB
            if (archivo.ContentLength > 5 * 1024 * 1024)
            {
                error = "El archivo excede el límite de 5 MB.";
                return false;
            }

            string ext = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrEmpty(ext) || !ExtensionesPermitidasAdjuntos.Contains(ext))
            {
                error = "Tipo de archivo no permitido. Solo se permiten formatos PDF, JPG y PNG.";
                return false;
            }

            try
            {
                byte[] buffer = new byte[8];
                long originalPos = archivo.InputStream.Position;
                archivo.InputStream.Position = 0;
                int bytesLeidos = archivo.InputStream.Read(buffer, 0, 8);
                archivo.InputStream.Position = originalPos;

                if (bytesLeidos < 4)
                {
                    error = "El archivo subido está corrupto o incompleto.";
                    return false;
                }

                bool esPdf = buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46; // %PDF
                bool esJpg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
                bool esPng = buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47; // .PNG

                if (!esPdf && !esJpg && !esPng)
                {
                    error = "La firma binaria del archivo no coincide con su formato legítimo.";
                    return false;
                }
            }
            catch
            {
                error = "No se pudo verificar la integridad del archivo.";
                return false;
            }

            return true;
        }

        private static string SanitizarFormulaExcel(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return valor;
            valor = valor.Trim();
            if (valor.StartsWith("=") || valor.StartsWith("+") || valor.StartsWith("-") || valor.StartsWith("@"))
            {
                return "'" + valor;
            }
            return valor;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Iglesia modelo, HttpPostedFileBase docPastor, HttpPostedFileBase docLider)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (!PuedeRegistrarIglesia(u))
            {
                TempData["MensajeError"] = "Permiso denegado.";
                return RedirectToAction("Index");
            }

            CargarEquiposDisponibles();
            CargarCatalogosDenominacionesYTipos();

            if (!ValidarFormatosDR(modelo, out string errorValidacion))
            {
                ViewData["MensajeError"] = errorValidacion;
                return View(modelo);
            }

            // Validar seguridad de archivos adjuntos
            if (!ValidarArchivoSeguroIglesia(docPastor, out string errPastor))
            {
                ViewData["MensajeError"] = "Cédula del Pastor: " + errPastor;
                return View(modelo);
            }
            if (!ValidarArchivoSeguroIglesia(docLider, out string errLider))
            {
                ViewData["MensajeError"] = "Cédula del Líder: " + errLider;
                return View(modelo);
            }

            // Manejo seguro y permanente de archivos adjuntos en SQL
            if (docPastor != null && docPastor.ContentLength > 0)
            {
                modelo.Pastor.DocumentoAdjuntoRuta = ArchivoStorageHelper.GuardarArchivo("IglesiaPastor", 0, docPastor, "Iglesias");
            }

            if (docLider != null && docLider.ContentLength > 0)
            {
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = ArchivoStorageHelper.GuardarArchivo("IglesiaLider", 0, docLider, "Iglesias");
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
                if (ex.Message.Contains("Esta iglesia ya está registrada en el equipo:"))
                {
                    string raw = ex.Message;
                    int idx = raw.IndexOf("Esta iglesia ya está registrada en el equipo:");
                    string sub = raw.Substring(idx);
                    string[] parts = sub.Split('|');
                    string msgBase = parts[0].Trim();
                    int idExistente = 0;
                    if (parts.Length > 1 && int.TryParse(parts[1], out int parsedId))
                    {
                        idExistente = parsedId;
                    }
                    ViewBag.AlertaIglesiaOtroEquipo = msgBase;
                    ViewBag.IdIglesiaOtroEquipo = idExistente;
                    ViewData["MensajeError"] = msgBase;
                }
                else
                {
                    ViewData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                }
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

            // Mitigación IDOR: Coordinadores solo pueden consultar iglesias de su jurisdicción / equipo
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue && iglesia.IdEquipo != u.IdEquipo.Value)
            {
                TempData["MensajeError"] = "No tienes autorización para acceder al expediente de una iglesia perteneciente a otro equipo.";
                return RedirectToAction("Index");
            }

            // Cargar eventos de tipo Visión, Taller y Despacho para la temporada activa filtrados por equipo
            List<SelectListItem> eventosVision = new List<SelectListItem>();
            List<SelectListItem> eventosTaller = new List<SelectListItem>();
            List<SelectListItem> eventosDespacho = new List<SelectListItem>();
            List<object> eventosDisponiblesDetalle = new List<object>();

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT e.IdEvento, e.NombreEvento, e.Fecha, e.Lugar, e.TipoEvento,
                           ISNULL(ed.EstadoDespachoEvento, 'PROGRAMADO') AS EstadoDespachoEvento,
                           COALESCE(ed.IdEquipo, a.IdEquipo) AS IdEquipoEvento
                    FROM dbo.Eventos e 
                    INNER JOIN dbo.Temporadas t ON e.IdTemporada = t.IdTemporada 
                    LEFT JOIN dbo.EventosDespacho ed ON e.IdEvento = ed.IdEvento
                    LEFT JOIN dbo.Usuarios u_creador ON e.IdUsuarioCreacion = u_creador.IdUsuario
                    LEFT JOIN dbo.AsignacionesEquipo a ON u_creador.IdUsuario = a.IdUsuario AND a.Activo = 1
                    WHERE e.IdTemporada = (SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC)
                      AND e.TipoEvento IN ('Vision', 'Taller', 'Despacho')";

                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue)
                {
                    sql += " AND (COALESCE(ed.IdEquipo, a.IdEquipo) = @IdEquipo OR (e.TipoEvento <> 'Despacho' AND (a.IdEquipo = @IdEquipo OR a.IdEquipo IS NULL)))";
                }

                sql += " ORDER BY e.Fecha DESC;";
                SqlCommand cmd = new SqlCommand(sql, cn);
                
                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue)
                {
                    cmd.Parameters.Add(new SqlParameter("@IdEquipo", u.IdEquipo.Value));
                }

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int idEv = Convert.ToInt32(dr["IdEvento"]);
                        string nom = dr["NombreEvento"].ToString();
                        DateTime f = Convert.ToDateTime(dr["Fecha"]);
                        string lug = dr["Lugar"] != DBNull.Value ? dr["Lugar"].ToString() : "";
                        string tipo = dr["TipoEvento"].ToString();
                        string estadoDesp = dr["EstadoDespachoEvento"] != DBNull.Value ? dr["EstadoDespachoEvento"].ToString() : "PROGRAMADO";

                        var item = new SelectListItem
                        {
                            Value = idEv.ToString(),
                            Text = $"{nom} | {f:dd/MM/yyyy}" + (!string.IsNullOrEmpty(lug) ? $" ({lug})" : "")
                        };
                        
                        // CMI (2) ve Visión, CD (3) ve Taller, CE (1) / Admin (Rol 1,2) ve ambos
                        bool esCMI = (u.IdPosicion == 2);
                        bool esCD = (u.IdPosicion == 3);
                        
                        if (tipo == "Vision" && !esCD) eventosVision.Add(item);
                        else if (tipo == "Taller" && !esCMI) eventosTaller.Add(item);
                        else if (tipo == "Despacho")
                        {
                            if (estadoDesp != "CANCELADO" && estadoDesp != "CERRADO" && estadoDesp != "FINALIZADO")
                            {
                                eventosDespacho.Add(item);
                            }
                        }

                        eventosDisponiblesDetalle.Add(new
                        {
                            id = idEv,
                            nombre = nom,
                            fecha = f.ToString("yyyy-MM-dd"),
                            lugar = lug,
                            tipo
                        });
                    }
                }
            }
            ViewBag.EventosVision = eventosVision;
            ViewBag.EventosTaller = eventosTaller;
            ViewBag.EventosDespacho = eventosDespacho;
            ViewBag.EventosDisponiblesJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventosDisponiblesDetalle);

            ViewBag.UsuarioActual = u;
            ViewBag.PuedeEditar = PuedeEditarIglesia(u, iglesia.IdEquipo);

            bool esAdmin = (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool puedeAprobarCE = esAdmin || (u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));
            bool puedeAprobarCMI = esAdmin || (u.IdPosicion == 2 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo))) || puedeAprobarCE;
            bool puedeSolicitarExcepcion = esAdmin || PuedeEditarIglesia(u, iglesia.IdEquipo);
            bool puedeGestionarDiscipulado = esAdmin || (u.IdPosicion == 3 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo))) || puedeAprobarCE;

            ViewBag.PuedeAprobarCE = puedeAprobarCE;
            ViewBag.PuedeAprobarCMI = puedeAprobarCMI;
            ViewBag.PuedeSolicitarExcepcion = puedeSolicitarExcepcion;
            ViewBag.PuedeGestionarDiscipulado = puedeGestionarDiscipulado;

            // Cargar datos de Discipulado y 5 Contactos LGA si hay participación activa
            if (iglesia.ParticipacionActual != null)
            {
                iglesia.DiscipuladoLGA = _iglesiaService.ObtenerResumenDiscipuladoLGA(iglesia.ParticipacionActual.IdParticipacion, iglesia.IdIglesia);
            }

            return View(iglesia);
        }

        // ============================================================================
        // MÉTODOS DE DISCIPULADO Y ACOMPAÑAMIENTO LGA (5 CONTACTOS & LLAMADA 5 MIN)
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarContactoLGA(ContactoLGAModel vm, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            bool esCD = u.IdPosicion == 3 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo));
            bool esCE = u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo));
            bool puede = esAdmin || esCD || esCE || PuedeEditarIglesia(u, iglesia.IdEquipo);

            if (!puede)
            {
                TempData["MensajeError"] = "Acceso denegado: No tiene permisos para registrar o modificar contactos de discipulado de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                vm.IdIglesia = idIglesia;
                _iglesiaService.GuardarContactoLGA(vm, u.IdUsuario);
                TempData["MensajeExito"] = $"Contacto #{vm.NumeroContacto} ({vm.NombreFase}) actualizado correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarLlamadaAcompanamiento(LlamadaAcompanamientoModel vm, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            bool esAdmin = u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2;
            bool esCD = u.IdPosicion == 3 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo));
            bool esCE = u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo));
            bool puede = esAdmin || esCD || esCE || PuedeEditarIglesia(u, iglesia.IdEquipo);

            if (!puede)
            {
                TempData["MensajeError"] = "Acceso denegado: No tiene permisos para registrar llamadas de acompañamiento para esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                vm.IdIglesia = idIglesia;
                string nombreCoord = !string.IsNullOrEmpty(u.NombreCompleto) ? u.NombreCompleto : (u.Correo ?? "Coordinador");
                _iglesiaService.RegistrarLlamadaAcompanamiento(vm, u.IdUsuario, nombreCoord);
                TempData["MensajeExito"] = "Llamada de acompañamiento de 5 minutos registrada con éxito. Semáforo actualizado.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // GESTIÓN DE EXCEPCIONES A LA REGLA DE 3 AÑOS (DOBLE APROBACIÓN CE + CMI)
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SolicitarExcepcion3Anios(int idIglesia, int idTemporada, int? temporadaPreviaId, int diferenciaTemporadas, string motivo, string justificacion, string resultadoDesempeno)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para solicitar excepciones en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (string.IsNullOrWhiteSpace(motivo) || string.IsNullOrWhiteSpace(justificacion))
            {
                TempData["MensajeError"] = "El motivo y la justificación detallada son obligatorios para solicitar la excepción.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                var excepcion = new ExcepcionRegla3Anios
                {
                    IdIglesia = idIglesia,
                    IdTemporada = idTemporada,
                    TemporadaPreviaId = temporadaPreviaId,
                    DiferenciaTemporadas = diferenciaTemporadas,
                    Motivo = motivo,
                    Justificacion = justificacion,
                    ResultadoDesempeno = resultadoDesempeno
                };

                _iglesiaService.SolicitarExcepcion(excepcion, u.IdUsuario);
                TempData["MensajeExito"] = "Solicitud de excepción registrada exitosamente. Queda pendiente de evaluación independiente por el Coordinador de Equipo (CE) y el Coordinador de Movilización (CMI).";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarExcepcionCE(int idExcepcion, int idIglesia, string comentarioCE, string rowVersionString)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            bool esAdmin = (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCE = (u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));

            if (!esAdmin && !esCE)
            {
                TempData["MensajeError"] = "Acceso denegado: Solo el Coordinador de Equipo (CE) autorizado o un Administrador pueden registrar la aprobación de CE.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                byte[] rowVersion = !string.IsNullOrEmpty(rowVersionString) ? Convert.FromBase64String(rowVersionString) : null;
                _iglesiaService.AprobarExcepcionCE(idExcepcion, u.IdUsuario, comentarioCE, rowVersion);
                TempData["MensajeExito"] = "Aprobación de CE registrada correctamente.";
            }
            catch (DBConcurrencyException)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarExcepcionCMI(int idExcepcion, int idIglesia, string comentarioCMI, string rowVersionString)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            bool esAdmin = (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCMI = (u.IdPosicion == 2 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));
            bool esCE = (u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));

            if (!esAdmin && !esCMI && !esCE)
            {
                TempData["MensajeError"] = "Acceso denegado: Solo el Coordinador de Movilización (CMI), Coordinador de Equipo (CE) autorizado o un Administrador pueden registrar la aprobación de CMI.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                byte[] rowVersion = !string.IsNullOrEmpty(rowVersionString) ? Convert.FromBase64String(rowVersionString) : null;
                _iglesiaService.AprobarExcepcionCMI(idExcepcion, u.IdUsuario, comentarioCMI, rowVersion);
                TempData["MensajeExito"] = "Aprobación de CMI registrada correctamente.";
            }
            catch (DBConcurrencyException)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarExcepcion3Anios(int idExcepcion, int idIglesia, string motivoRechazo, string rowVersionString)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            bool esAdmin = (u.IdRolSeguridad == 1 || u.IdRolSeguridad == 2);
            bool esCE = (u.IdPosicion == 1 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));
            bool esCMI = (u.IdPosicion == 2 && (u.IdEquipo == iglesia.IdEquipo || EsEquipoHijo(u.IdEquipo ?? 0, iglesia.IdEquipo)));

            if (!esAdmin && !esCE && !esCMI)
            {
                TempData["MensajeError"] = "Acceso denegado: Solo CE, CMI o un Administrador pueden rechazar una solicitud de excepción.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (string.IsNullOrWhiteSpace(motivoRechazo))
            {
                TempData["MensajeError"] = "Debe indicar el motivo del rechazo de la excepción.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                byte[] rowVersion = !string.IsNullOrEmpty(rowVersionString) ? Convert.FromBase64String(rowVersionString) : null;
                _iglesiaService.RechazarExcepcion(idExcepcion, u.IdUsuario, motivoRechazo, rowVersion);
                TempData["MensajeExito"] = "La solicitud de excepción fue rechazada.";
            }
            catch (DBConcurrencyException)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
                        cmdCheck.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
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
                                cmd.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
                    cmdGet.Parameters.Add(new SqlParameter("@Id", idParticipacion));
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
                    cmd.Parameters.Add(new SqlParameter("@EtapaRetorno", etapaRetorno));
                    cmd.Parameters.Add(new SqlParameter("@Id", idParticipacion));
                    cmd.ExecuteNonQuery();
                }

                string sqlLog = @"
                    INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                    VALUES (@IdPart, GETDATE(), 'Reapertura de Proceso', 'Detenido/Rechazado', 'Pendiente', @IdUser, @Cmt);";
                using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                {
                    cmdLog.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdLog.Parameters.Add(new SqlParameter("@IdUser", u.IdUsuario));
                    cmdLog.Parameters.Add(new SqlParameter("@Cmt", "Proceso Reabierto. Razón: " + comentario));
                    cmdLog.ExecuteNonQuery();
                }

                string sqlComentario = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                using (SqlCommand cmdCmt = new SqlCommand(sqlComentario, cn))
                {
                    cmdCmt.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                    cmdCmt.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                    cmdCmt.Parameters.Add(new SqlParameter("@Comentario", "Reapertura de Proceso: " + comentario));
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
                    cmdGet.Parameters.Add(new SqlParameter("@Id", idParticipacion));
                    object val = cmdGet.ExecuteScalar();
                    if (val != null) estadoAnterior = val.ToString();
                }

                string sql = "UPDATE dbo.ParticipacionesIglesia SET EstadoEvaluacion = 'Detenido' WHERE IdParticipacion = @Id;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add(new SqlParameter("@Id", idParticipacion));
                    cmd.ExecuteNonQuery();
                }

                string histCmt = $"Proceso Detenido. Motivo: {motivo}. " + (!string.IsNullOrWhiteSpace(comentario) ? $"Notas: {comentario}" : "");
                string sqlLog = @"
                    INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                    VALUES (@IdPart, GETDATE(), 'Detención de Proceso', @EstadoAnt, 'Detenido', @IdUser, @Cmt);";
                using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                {
                    cmdLog.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdLog.Parameters.Add(new SqlParameter("@EstadoAnt", estadoAnterior));
                    cmdLog.Parameters.Add(new SqlParameter("@IdUser", u.IdUsuario));
                    cmdLog.Parameters.Add(new SqlParameter("@Cmt", histCmt));
                    cmdLog.ExecuteNonQuery();
                }

                string sqlComentario = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                using (SqlCommand cmdCmt = new SqlCommand(sqlComentario, cn))
                {
                    cmdCmt.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                    cmdCmt.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                    cmdCmt.Parameters.Add(new SqlParameter("@Comentario", histCmt));
                    cmdCmt.ExecuteNonQuery();
                }
            }

            TempData["MensajeExito"] = "El proceso de la iglesia ha sido detenido exitosamente.";
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        public ActionResult CambiarEstatusReporte(int idParticipacion, int idIglesia, string estatusEvaluacionReporte, string comentario)
        {
            Usuario u = (Usuario)Session["usuario"];
            Iglesia iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();
            
            // Permitir que Administradores, CD y CE puedan cambiar esto
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdPosicion != 1 && u.IdPosicion != 2 && u.IdPosicion != 3)
            {
                TempData["MensajeError"] = "Su usuario no tiene autorización para cambiar el estatus de reportes.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permiso para realizar cambios en esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlGet = "SELECT EstatusEvaluacionReporte FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = @Id;";
                string estadoAnterior = "Desconocido";
                using (SqlCommand cmdGet = new SqlCommand(sqlGet, cn))
                {
                    cmdGet.Parameters.Add(new SqlParameter("@Id", idParticipacion));
                    object val = cmdGet.ExecuteScalar();
                    if (val != null) estadoAnterior = val.ToString();
                }

                string sql = "UPDATE dbo.ParticipacionesIglesia SET EstatusEvaluacionReporte = @Estatus WHERE IdParticipacion = @Id;";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add(new SqlParameter("@Estatus", estatusEvaluacionReporte));
                    cmd.Parameters.Add(new SqlParameter("@Id", idParticipacion));
                    cmd.ExecuteNonQuery();
                }

                string histCmt = $"El Estatus de Evaluación (Reporte) cambió de '{estadoAnterior}' a '{estatusEvaluacionReporte}'. " + (!string.IsNullOrWhiteSpace(comentario) ? $"Notas: {comentario}" : "");
                string sqlLog = @"
                    INSERT INTO dbo.HistorialParticipacion (IdParticipacion, FechaHora, AccionRealizada, EstadoAnterior, EstadoNuevo, IdUsuarioResponsable, Comentario)
                    VALUES (@IdPart, GETDATE(), 'Cambio Estatus Reporte', @EstadoAnt, @EstadoNue, @IdUser, @Cmt);";
                using (SqlCommand cmdLog = new SqlCommand(sqlLog, cn))
                {
                    cmdLog.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                    cmdLog.Parameters.Add(new SqlParameter("@EstadoAnt", estadoAnterior));
                    cmdLog.Parameters.Add(new SqlParameter("@EstadoNue", iglesia.ParticipacionActual.EstadoEvaluacion)); // El estado del proceso sigue igual
                    cmdLog.Parameters.Add(new SqlParameter("@IdUser", u.IdUsuario));
                    cmdLog.Parameters.Add(new SqlParameter("@Cmt", histCmt));
                    cmdLog.ExecuteNonQuery();
                }

                if (!string.IsNullOrWhiteSpace(comentario))
                {
                    string sqlComentario = "INSERT INTO dbo.ComentariosObservaciones (IdIglesia, IdUsuario, Comentario) VALUES (@IdIglesia, @IdUsuario, @Comentario);";
                    using (SqlCommand cmdCmt = new SqlCommand(sqlComentario, cn))
                    {
                        cmdCmt.Parameters.Add(new SqlParameter("@IdIglesia", idIglesia));
                        cmdCmt.Parameters.Add(new SqlParameter("@IdUsuario", u.IdUsuario));
                        cmdCmt.Parameters.Add(new SqlParameter("@Comentario", "Cambio de Estatus de Reporte: " + comentario));
                        cmdCmt.ExecuteNonQuery();
                    }
                }
            }

            TempData["MensajeExito"] = "El estatus de evaluación de reportes ha sido actualizado exitosamente.";
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }
            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompletarTallerOCC(
            int idParticipacion, 
            int idIglesia, 
            string tallerNombre = null, 
            DateTime? tallerFecha = null, 
            string tallerLugar = null, 
            int? cantNinos = null, 
            int? cantMaestrosReg = null, 
            int? cantMaestrosAsist = null, 
            int? cantMaestrosAus = null,
            int? idEventoDespacho = null,
            int? oportunidadesEvangelisticas = null,
            int? librosMejorRegalo = null,
            int? librosMaestros = null,
            int? librosAlumno = null,
            int? posters = null,
            int? nuevosTestamentos = null)
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
                int cantOp = oportunidadesEvangelisticas.GetValueOrDefault(cantNinos.HasValue && cantNinos.Value > 0 ? cantNinos.Value : 50);
                int cantRegalo = librosMejorRegalo.GetValueOrDefault(cantNinos.HasValue && cantNinos.Value > 0 ? cantNinos.Value : 50);
                int cantAlum = librosAlumno.GetValueOrDefault(cantNinos.HasValue && cantNinos.Value > 0 ? cantNinos.Value : 50);
                int cantMaest = librosMaestros.GetValueOrDefault(cantMaestrosAsist.HasValue && cantMaestrosAsist.Value > 0 ? cantMaestrosAsist.Value : (iglesia.Maestros.Any() ? iglesia.Maestros.Count : 5));
                int cantNt = nuevosTestamentos.GetValueOrDefault(cantNinos.HasValue && cantNinos.Value > 0 ? cantNinos.Value : 50);
                int cantPost = posters.GetValueOrDefault(10);

                // Si se seleccionó un evento de despacho opcional, validar exhaustivamente en backend
                if (idEventoDespacho.HasValue && idEventoDespacho.Value > 0)
                {
                    using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                    {
                        cn.Open();
                        string sqlValEv = @"
                            SELECT TOP 1 e.IdEvento, e.IdTemporada, e.TipoEvento, 
                                   COALESCE(ed.IdEquipo, a.IdEquipo) AS IdEquipoEvento,
                                   ISNULL(ed.EstadoDespachoEvento, 'PROGRAMADO') AS EstadoDespachoEvento
                            FROM dbo.Eventos e
                            LEFT JOIN dbo.EventosDespacho ed ON e.IdEvento = ed.IdEvento
                            LEFT JOIN dbo.Usuarios u_cr ON e.IdUsuarioCreacion = u_cr.IdUsuario
                            LEFT JOIN dbo.AsignacionesEquipo a ON u_cr.IdUsuario = a.IdUsuario AND a.Activo = 1
                            WHERE e.IdEvento = @IdEv;";

                        using (var cmdVal = new SqlCommand(sqlValEv, cn))
                        {
                            cmdVal.Parameters.Add(new SqlParameter("@IdEv", idEventoDespacho.Value));
                            using (var drVal = cmdVal.ExecuteReader())
                            {
                                if (!drVal.Read())
                                {
                                    TempData["MensajeError"] = "El evento de despacho seleccionado no existe.";
                                    return RedirectToAction("Detalle", new { id = idIglesia });
                                }

                                string tipoEv = drVal["TipoEvento"].ToString();
                                if (!string.Equals(tipoEv, "Despacho", StringComparison.OrdinalIgnoreCase))
                                {
                                    TempData["MensajeError"] = "El evento seleccionado no es de tipo Despacho de Materiales.";
                                    return RedirectToAction("Detalle", new { id = idIglesia });
                                }

                                int idTempEv = Convert.ToInt32(drVal["IdTemporada"]);
                                int idTempActiva = iglesia.ParticipacionActual != null ? iglesia.ParticipacionActual.IdTemporada : 1;
                                if (idTempEv != idTempActiva)
                                {
                                    TempData["MensajeError"] = "El evento de despacho pertenece a una temporada diferente a la actual.";
                                    return RedirectToAction("Detalle", new { id = idIglesia });
                                }

                                string estadoEv = drVal["EstadoDespachoEvento"].ToString();
                                if (estadoEv == "CANCELADO" || estadoEv == "CERRADO" || estadoEv == "FINALIZADO")
                                {
                                    TempData["MensajeError"] = "El evento de despacho seleccionado se encuentra cerrado o finalizado y no puede recibir nuevas iglesias.";
                                    return RedirectToAction("Detalle", new { id = idIglesia });
                                }

                                // Validación estricta del equipo del usuario autenticado
                                if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdEquipo.HasValue)
                                {
                                    int? idEquipoEvento = drVal["IdEquipoEvento"] != DBNull.Value ? Convert.ToInt32(drVal["IdEquipoEvento"]) : (int?)null;
                                    if (idEquipoEvento.HasValue && idEquipoEvento.Value != u.IdEquipo.Value)
                                    {
                                        TempData["MensajeError"] = "Acceso denegado: El evento de despacho pertenece a otro equipo.";
                                        return RedirectToAction("Detalle", new { id = idIglesia });
                                    }
                                }
                            }
                        }
                    }
                }

                // Preservar datos de participación existente al avanzar de etapa
                var part = iglesia.ParticipacionActual;
                string nombreTaller = !string.IsNullOrEmpty(tallerNombre) ? tallerNombre : ((part != null && !string.IsNullOrEmpty(part.TallerNombre)) ? part.TallerNombre : "Taller OCC");
                DateTime? fechaTaller = tallerFecha.HasValue ? tallerFecha : ((part != null && part.TallerFecha.HasValue) ? part.TallerFecha : DateTime.Today);
                string lugarTaller = !string.IsNullOrEmpty(tallerLugar) ? tallerLugar : ((part != null && !string.IsNullOrEmpty(part.TallerLugar)) ? part.TallerLugar : "Sede Central");
                int ninosVal = (cantNinos.HasValue && cantNinos.Value > 0) ? cantNinos.Value : ((part != null && part.TallerCantNinos > 0) ? part.TallerCantNinos : cantOp);
                int maestrosRegVal = (cantMaestrosReg.HasValue && cantMaestrosReg.Value > 0) ? cantMaestrosReg.Value : ((part != null && part.TallerCantMaestrosReg > 0) ? part.TallerCantMaestrosReg : (iglesia.Maestros.Any() ? iglesia.Maestros.Count : 1));
                int maestrosAsistVal = (cantMaestrosAsist.HasValue && cantMaestrosAsist.Value > 0) ? cantMaestrosAsist.Value : ((part != null && part.TallerCantMaestrosAsist > 0) ? part.TallerCantMaestrosAsist : maestrosRegVal);
                int maestrosAusVal = (cantMaestrosAus.HasValue && cantMaestrosAus.Value >= 0) ? cantMaestrosAus.Value : (part != null ? part.TallerCantMaestrosAus : 0);

                _iglesiaService.AvanzarEtapa5(idParticipacion, nombreTaller, fechaTaller, lugarTaller, ninosVal, maestrosRegVal, maestrosAsistVal, maestrosAusVal, u.IdUsuario);

                // Guardar / actualizar la asignación de materiales para despacho
                using (var cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sqlAsig = @"
                        IF EXISTS (SELECT 1 FROM dbo.AsignacionesRecursos WHERE IdParticipacion = @IdPart)
                        BEGIN
                            UPDATE dbo.AsignacionesRecursos SET
                                OportunidadesEvangelisticas = @Oportunidades,
                                LibrosMejorRegalo = @Regalo,
                                LibrosMaestros = @Maestros,
                                LibrosAlumno = @Alumno,
                                Posters = @Posters,
                                NuevosTestamentos = @Testamentos,
                                EstadoAsignacion = 'DISPONIBLE_PARA_DESPACHO',
                                FechaDisponibleDespacho = GETDATE(),
                                IdEventoDespachoActual = @IdEventoDespacho
                            WHERE IdParticipacion = @IdPart;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.AsignacionesRecursos 
                                (IdParticipacion, OportunidadesEvangelisticas, LibrosMejorRegalo, LibrosMaestros, LibrosAlumno, Posters, NuevosTestamentos, EstadoAsignacion, FechaDisponibleDespacho, IdEventoDespachoActual)
                            VALUES 
                                (@IdPart, @Oportunidades, @Regalo, @Maestros, @Alumno, @Posters, @Testamentos, 'DISPONIBLE_PARA_DESPACHO', GETDATE(), @IdEventoDespacho);
                        END";

                    using (var cmd = new SqlCommand(sqlAsig, cn))
                    {
                        cmd.Parameters.Add(new SqlParameter("@IdPart", idParticipacion));
                        cmd.Parameters.Add(new SqlParameter("@Oportunidades", cantOp));
                        cmd.Parameters.Add(new SqlParameter("@Regalo", cantRegalo));
                        cmd.Parameters.Add(new SqlParameter("@Maestros", cantMaest));
                        cmd.Parameters.Add(new SqlParameter("@Alumno", cantAlum));
                        cmd.Parameters.Add(new SqlParameter("@Posters", cantPost));
                        cmd.Parameters.Add(new SqlParameter("@Testamentos", cantNt));
                        cmd.Parameters.Add(new SqlParameter("@IdEventoDespacho", idEventoDespacho.HasValue && idEventoDespacho.Value > 0 ? (object)idEventoDespacho.Value : DBNull.Value));
                        cmd.ExecuteNonQuery();
                    }

                    // Si se seleccionó un evento de despacho específico, programar la iglesia de forma atómica
                    if (idEventoDespacho.HasValue && idEventoDespacho.Value > 0)
                    {
                        var logisticaSvc = new SOR.Services.LogisticaService();
                        int idEquipo = iglesia.IdEquipo;
                        int idTemporada = iglesia.ParticipacionActual != null ? iglesia.ParticipacionActual.IdTemporada : 1;
                        logisticaSvc.ProgramarIglesiaEnDespacho(idEventoDespacho.Value, idParticipacion, idIglesia, idEquipo, idTemporada, u.IdUsuario);
                        TempData["MensajeExito"] = "Materiales asignados y programación en el Evento de Despacho confirmada exitosamente.";
                    }
                    else
                    {
                        TempData["MensajeExito"] = "Materiales asignados exitosamente. La iglesia queda habilitada y disponible para posterior programación en un Evento de Despacho.";
                    }
                }
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
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
            if (u == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

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
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sqlCheck = "SELECT COUNT(1) FROM dbo.Temporadas WHERE IdTemporada = @Id;";
                    using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, cn))
                    {
                        cmdCheck.Parameters.Add(new SqlParameter("@Id", idTemporadaImportar.Value));
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count > 0) esTemporadaValida = true;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al verificar la temporada: " + ex.Message;
                return RedirectToAction("Index");
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

            List<Iglesia> iglesiasPreview = new List<Iglesia>();
            List<string> detalleErrores = new List<string>();

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    archivoExcel.InputStream.CopyTo(ms);
                    ms.Position = 0;

                    if (ext == ".csv")
                    {
                        // Lector nativo CSV desde MemoryStream
                        using (StreamReader reader = new StreamReader(ms, System.Text.Encoding.UTF8))
                        {
                            string line;
                            int filaNum = 0;
                            while ((line = reader.ReadLine()) != null)
                            {
                                filaNum++;
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                string[] cols = line.Split(',');

                                string col0 = cols.Length > 0 ? cols[0].Trim() : "";
                                string col1 = cols.Length > 1 ? cols[1].Trim() : "";

                                // Ignorar cabeceras y fila de ejemplo
                                if (col0.Equals("NO", StringComparison.OrdinalIgnoreCase) || col0.Equals("Ejemplo", StringComparison.OrdinalIgnoreCase) || col1.StartsWith("NombredeIglesia", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(col1))
                                {
                                    try
                                    {
                                        Iglesia ig = MapearColumnasImport(cols);
                                        ig.IdEquipo = u.IdEquipo ?? 1;
                                        iglesiasPreview.Add(ig);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (detalleErrores.Count < 10) detalleErrores.Add($"Fila {filaNum} ({col1}): {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Lector OpenXML nativo desde MemoryStream con Seek garantizado
                        List<string[]> filasExcel = LeerFilasExcelOpenXml(ms);
                        int filaNum = 0;
                        foreach (var cols in filasExcel)
                        {
                            filaNum++;
                            if (cols == null || cols.Length == 0) continue;

                            string col0 = cols.Length > 0 ? cols[0].Trim() : "";
                            string col1 = cols.Length > 1 ? cols[1].Trim() : "";

                            // Ignorar encabezados y fila de ejemplo
                            if (col0.Equals("NO", StringComparison.OrdinalIgnoreCase) || col0.Equals("Ejemplo", StringComparison.OrdinalIgnoreCase) || col1.StartsWith("NombredeIglesia", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Si tiene nombre de iglesia válido
                            if (!string.IsNullOrWhiteSpace(col1))
                            {
                                try
                                {
                                    Iglesia ig = MapearColumnasImport(cols);
                                    ig.IdEquipo = u.IdEquipo ?? 1;
                                    iglesiasPreview.Add(ig);
                                }
                                catch (Exception ex)
                                {
                                    if (detalleErrores.Count < 10) detalleErrores.Add($"Fila {filaNum} ({col1}): {ex.Message}");
                                }
                            }
                        }
                    }
                }

                if (detalleErrores.Any())
                {
                    TempData["MensajeError"] = "Se encontraron advertencias al leer algunas filas:<br/>" + string.Join("<br/>", detalleErrores);
                }

                if (!iglesiasPreview.Any())
                {
                    TempData["MensajeError"] = "No se encontraron registros de iglesias válidos en el archivo subido. Asegúrese de completar los nombres de las iglesias a partir de la fila de datos de la plantilla.";
                    return RedirectToAction("Index");
                }

                Session["IglesiasImportPreview"] = iglesiasPreview;
                Session["IdTemporadaImportPreview"] = idTemporadaImportar.Value;
                
                return View("PreviewImportacion", iglesiasPreview);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al procesar el archivo de importación: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult ConfirmarImportacionMasiva(List<Iglesia> iglesias)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u.IdRolSeguridad != 1 && u.IdRolSeguridad != 2 && u.IdPosicion != 1 && u.IdPosicion != 2)
            {
                TempData["MensajeError"] = "Tu rol no tiene permisos para realizar importaciones masivas de iglesias.";
                return RedirectToAction("Index");
            }

            int idTemporadaImportar = Session["IdTemporadaImportPreview"] != null ? (int)Session["IdTemporadaImportPreview"] : 0;
            if (idTemporadaImportar <= 0)
            {
                TempData["MensajeError"] = "Sesión de importación caducada. Vuelve a subir el archivo.";
                return RedirectToAction("Index");
            }

            if (iglesias == null || !iglesias.Any())
            {
                TempData["MensajeError"] = "No se recibieron datos para importar.";
                return RedirectToAction("Index");
            }

            int insertados = 0;
            int errores = 0;
            List<string> detalleErrores = new List<string>();

            foreach (var ig in iglesias)
            {
                try
                {
                    ig.IdEquipo = u.IdEquipo ?? 1;
                    _iglesiaService.RegistrarIglesia(ig, u.IdUsuario, idTemporadaImportar);
                    insertados++;
                }
                catch (Exception ex)
                {
                    errores++;
                    if (detalleErrores.Count < 10) detalleErrores.Add($"{ig.NombreIglesia}: {ex.Message}");
                }
            }

            Session.Remove("IglesiasImportPreview");
            Session.Remove("IdTemporadaImportPreview");

            string msg = $"Importación completada: {insertados} iglesias registradas exitosamente. Errores: {errores}.";
            if (detalleErrores.Any())
            {
                msg += "<br/><strong>Detalle de Errores:</strong><br/>" + string.Join("<br/>", detalleErrores);
            }
            TempData["MensajeExito"] = msg;

            return RedirectToAction("Index");
        }

        private void SepararNombresApellidos(string nombreCompleto, out string nombres, out string apellidos)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                nombres = "";
                apellidos = "";
                return;
            }
            string[] partes = nombreCompleto.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1)
            {
                nombres = partes[0];
                apellidos = "";
            }
            else
            {
                nombres = partes[0];
                apellidos = string.Join(" ", partes.Skip(1));
            }
        }

        private static string ObtenerCol(string[] cols, int idx)
        {
            if (cols == null || idx >= cols.Length || cols[idx] == null) return "";
            return cols[idx].Trim();
        }

        private Iglesia MapearColumnasImport(string[] cols)
        {
            string col1 = ObtenerCol(cols, 1);
            Iglesia ig = new Iglesia
            {
                NombreIglesia = !string.IsNullOrWhiteSpace(col1) ? SanitizarFormulaExcel(col1) : "Iglesia Importada",
                RNC_Cedula = SanitizarFormulaExcel(ObtenerCol(cols, 2)) ?? "",
                Telefono = SanitizarFormulaExcel(ObtenerCol(cols, 3)) ?? "",
                CorreoInstitucion = SanitizarFormulaExcel(ObtenerCol(cols, 4)) ?? "",
                Provincia = SanitizarFormulaExcel(ObtenerCol(cols, 5)) ?? "",
                Ciudad = SanitizarFormulaExcel(ObtenerCol(cols, 6)) ?? "",
                Sector = SanitizarFormulaExcel(ObtenerCol(cols, 7)) ?? "",
                Calle = SanitizarFormulaExcel(ObtenerCol(cols, 8)) ?? "",
                Numero = SanitizarFormulaExcel(ObtenerCol(cols, 9)) ?? "",
                Referencia = SanitizarFormulaExcel(ObtenerCol(cols, 10)) ?? "",
                Denominacion = SanitizarFormulaExcel(ObtenerCol(cols, 11)) ?? "",
                TipoOrganizacion = "Iglesia",
                IdEquipo = 1 // Se reasignará luego
            };

            SepararNombresApellidos(ObtenerCol(cols, 12), out string pNombres, out string pApellidos);
            ig.Pastor = new PersonaIglesia
            {
                TipoPersona = "Pastor",
                Nombres = SanitizarFormulaExcel(pNombres) ?? "",
                Apellidos = SanitizarFormulaExcel(pApellidos) ?? "",
                DocumentoIdentidad = SanitizarFormulaExcel(ObtenerCol(cols, 13)) ?? "",
                Celular = SanitizarFormulaExcel(ObtenerCol(cols, 14)) ?? "",
                Correo = SanitizarFormulaExcel(ObtenerCol(cols, 15)) ?? ""
            };

            SepararNombresApellidos(ObtenerCol(cols, 16), out string lNombres, out string lApellidos);
            ig.LiderMinisterial = new PersonaIglesia
            {
                TipoPersona = "LiderMinisterial",
                Nombres = SanitizarFormulaExcel(lNombres) ?? "",
                Apellidos = SanitizarFormulaExcel(lApellidos) ?? "",
                DocumentoIdentidad = SanitizarFormulaExcel(ObtenerCol(cols, 17)) ?? "",
                Celular = SanitizarFormulaExcel(ObtenerCol(cols, 18)) ?? "",
                Correo = SanitizarFormulaExcel(ObtenerCol(cols, 19)) ?? ""
            };

            string col20 = ObtenerCol(cols, 20);
            string col21 = ObtenerCol(cols, 21);
            ig.CantidadMaestros = int.TryParse(col20, out int m) ? (int?)m : null;
            ig.CantidadNinos = int.TryParse(col21, out int n) ? (int?)n : null;

            string reportoVal = ObtenerCol(cols, 22).ToUpper();
            ig.ParticipacionActual = new ParticipacionIglesia
            {
                EstatusEvaluacionReporte = (reportoVal == "SI" || reportoVal == "SÍ") ? "Reportó" : "No Reportó"
            };

            return ig;
        }

        private static int LetrasColumnaAIndice(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return 0;
            string colLetters = "";
            foreach (char c in cellRef)
            {
                if (char.IsLetter(c)) colLetters += c;
                else break;
            }
            int sum = 0;
            foreach (char c in colLetters.ToUpperInvariant())
            {
                sum *= 26;
                sum += (c - 'A' + 1);
            }
            return Math.Max(0, sum - 1);
        }

        private static List<string[]> LeerFilasExcelOpenXml(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return LeerFilasExcelOpenXml(fs);
            }
        }

        private static List<string[]> LeerFilasExcelOpenXml(Stream stream)
        {
            List<string[]> filas = new List<string[]>();
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                // 1. Cargar Shared Strings si existen
                List<string> sharedStrings = new List<string>();
                ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml") 
                    ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("sharedStrings.xml", StringComparison.OrdinalIgnoreCase));
                
                if (sharedStringsEntry != null)
                {
                    using (Stream s = sharedStringsEntry.Open())
                    {
                        XDocument xdoc = XDocument.Load(s);
                        XNamespace ns = xdoc.Root != null ? xdoc.Root.GetDefaultNamespace() : XNamespace.None;
                        foreach (XElement si in xdoc.Descendants(ns + "si"))
                        {
                            string text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                            sharedStrings.Add(text);
                        }
                    }
                }

                // 2. Encontrar la primera hoja (sheet1.xml o primer worksheet disponible)
                ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml") 
                    ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

                if (sheetEntry != null)
                {
                    using (Stream s = sheetEntry.Open())
                    {
                        XDocument xdoc = XDocument.Load(s);
                        XNamespace ns = xdoc.Root != null ? xdoc.Root.GetDefaultNamespace() : XNamespace.None;

                        foreach (XElement row in xdoc.Descendants(ns + "row"))
                        {
                            var cElements = row.Elements(ns + "c").ToList();
                            if (!cElements.Any()) continue;

                            int maxCol = 0;
                            Dictionary<int, string> rowValues = new Dictionary<int, string>();

                            foreach (XElement c in cElements)
                            {
                                string rAttr = (string)c.Attribute("r");
                                int colIndex = !string.IsNullOrEmpty(rAttr) ? LetrasColumnaAIndice(rAttr) : maxCol;
                                if (colIndex > maxCol) maxCol = colIndex;

                                string tAttr = (string)c.Attribute("t");
                                string cellValue = "";

                                if (tAttr == "s") // Shared String
                                {
                                    XElement vElem = c.Element(ns + "v");
                                    if (vElem != null && int.TryParse(vElem.Value, out int sIndex) && sIndex >= 0 && sIndex < sharedStrings.Count)
                                    {
                                        cellValue = sharedStrings[sIndex];
                                    }
                                }
                                else if (tAttr == "inlineStr") // Inline String
                                {
                                    XElement isElem = c.Element(ns + "is");
                                    if (isElem != null)
                                    {
                                        cellValue = string.Concat(isElem.Descendants(ns + "t").Select(t => t.Value));
                                    }
                                }
                                else // Número, fecha o string directo
                                {
                                    XElement vElem = c.Element(ns + "v");
                                    if (vElem != null) cellValue = vElem.Value;
                                }

                                rowValues[colIndex] = cellValue != null ? cellValue.Trim() : "";
                            }

                            string[] cols = new string[maxCol + 1];
                            for (int i = 0; i <= maxCol; i++)
                            {
                                cols[i] = rowValues.ContainsKey(i) ? rowValues[i] : "";
                            }

                            filas.Add(cols);
                        }
                    }
                }
            }
            return filas;
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
                cmd.Parameters.Add(new SqlParameter("@IdHijo", idEquipoHijo));
                cmd.Parameters.Add(new SqlParameter("@IdPadre", idEquipoPadre));
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void CargarEquiposDisponibles()
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
                        cmdAnterior.Parameters.Add(new SqlParameter("@FechaInicioActiva", fechaInicioActiva.Value));
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
                new SelectListItem { Value = "7", Text = "Etapa 7: Entrega / Despacho" }
            };

            ViewBag.FiltroEstados = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Aprobado", Text = "Aprobada" },
                new SelectListItem { Value = "Detenido", Text = "Detenida" },
                new SelectListItem { Value = "Rechazado", Text = "Rechazada / Suspendida" }
            };

            ViewBag.FiltroEstatusReportes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Reportó", Text = "Reportó" },
                new SelectListItem { Value = "No Reportó", Text = "No Reportó" },
                new SelectListItem { Value = "Castigada", Text = "Castigada" }
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
                cmd.Parameters.Add(new SqlParameter("@IdTemp", idTemporadaActiva));
                cmd.Parameters.Add(new SqlParameter("@Doc", cleanCedula));
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

        [HttpGet]
        public JsonResult VerificarIglesiaExistente(string rncCedula, int? idEquipoActual, int? excluirIdIglesia)
        {
            if (string.IsNullOrWhiteSpace(rncCedula))
            {
                return Json(new { existe = false }, JsonRequestBehavior.AllowGet);
            }

            string cleanDoc = rncCedula.Replace("-", "").Replace(" ", "").Trim();
            int idExcluir = excluirIdIglesia ?? 0;

            int idTemporadaActiva = 0;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sqlTemp = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;";
                SqlCommand cmdTemp = new SqlCommand(sqlTemp, cn);
                cn.Open();
                object val = cmdTemp.ExecuteScalar();
                if (val != null) idTemporadaActiva = Convert.ToInt32(val);
            }

            if (idTemporadaActiva <= 0)
            {
                return Json(new { existe = false }, JsonRequestBehavior.AllowGet);
            }

            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                string sql = @"
                    SELECT TOP 1 i.IdIglesia, i.NombreIglesia, i.IdEquipo, e.NombreEquipo, p.IdTemporada
                    FROM dbo.Iglesias i
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    INNER JOIN dbo.ParticipacionesIglesia p ON i.IdIglesia = p.IdIglesia
                    WHERE p.IdTemporada = @IdTemp
                      AND (
                          i.RNC_Cedula = @RncCedula 
                          OR REPLACE(REPLACE(ISNULL(i.RNC_Cedula, ''), '-', ''), ' ', '') = @CleanDoc
                      )
                      AND (@ExcluirId <= 0 OR i.IdIglesia <> @ExcluirId);";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.Add(new SqlParameter("@IdTemp", idTemporadaActiva));
                cmd.Parameters.Add(new SqlParameter("@RncCedula", rncCedula.Trim()));
                cmd.Parameters.Add(new SqlParameter("@CleanDoc", cleanDoc));
                cmd.Parameters.Add(new SqlParameter("@ExcluirId", idExcluir));

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int idIg = Convert.ToInt32(dr["IdIglesia"]);
                        string nombreIg = dr["NombreIglesia"].ToString();
                        int idEq = Convert.ToInt32(dr["IdEquipo"]);
                        string nombreEq = dr["NombreEquipo"].ToString();
                        int idTemp = Convert.ToInt32(dr["IdTemporada"]);

                        bool mismoEquipo = idEquipoActual.HasValue && idEquipoActual.Value > 0 && idEquipoActual.Value == idEq;

                        return Json(new
                        {
                            existe = true,
                            mismoEquipo,
                            idIglesia = idIg,
                            nombreIglesia = nombreIg,
                            idEquipo = idEq,
                            nombreEquipo = nombreEq,
                            idTemporada = idTemp,
                            mensaje = $"Esta iglesia ya está registrada en el equipo: {nombreEq}"
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            // 2. Si no está en la temporada activa, verificar si participó en una temporada previa reciente (< minAnios)
            int minAnios = 3;
            using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
            {
                cn.Open();
                string sqlCfg = "SELECT Valor FROM dbo.ConfiguracionesSistema WHERE Clave = 'MinAniosAntiguedad';";
                using (SqlCommand cmdCfg = new SqlCommand(sqlCfg, cn))
                {
                    object val = cmdCfg.ExecuteScalar();
                    if (val != null && int.TryParse(val.ToString(), out int p)) minAnios = p;
                }

                string sqlAnt = @"
                    SELECT TOP 1 p.IdTemporada, t.NombreTemporada, i.IdIglesia, i.NombreIglesia, e.NombreEquipo
                    FROM dbo.ParticipacionesIglesia p
                    INNER JOIN dbo.Temporadas t ON p.IdTemporada = t.IdTemporada
                    INNER JOIN dbo.Iglesias i ON p.IdIglesia = i.IdIglesia
                    INNER JOIN dbo.Equipos e ON i.IdEquipo = e.IdEquipo
                    WHERE (i.RNC_Cedula = @RncCedula OR REPLACE(REPLACE(ISNULL(i.RNC_Cedula, ''), '-', ''), ' ', '') = @CleanDoc)
                      AND p.IdTemporada < @IdTemp
                    ORDER BY p.IdTemporada DESC;";

                using (SqlCommand cmdAnt = new SqlCommand(sqlAnt, cn))
                {
                    cmdAnt.Parameters.Add(new SqlParameter("@RncCedula", rncCedula.Trim()));
                    cmdAnt.Parameters.Add(new SqlParameter("@CleanDoc", cleanDoc));
                    cmdAnt.Parameters.Add(new SqlParameter("@IdTemp", idTemporadaActiva));

                    using (SqlDataReader drAnt = cmdAnt.ExecuteReader())
                    {
                        if (drAnt.Read())
                        {
                            int idPrev = Convert.ToInt32(drAnt["IdTemporada"]);
                            string nomPrev = drAnt["NombreTemporada"].ToString();
                            int idIgAnt = Convert.ToInt32(drAnt["IdIglesia"]);
                            string nomIgAnt = drAnt["NombreIglesia"].ToString();
                            string nomEqAnt = drAnt["NombreEquipo"].ToString();
                            int diff = idTemporadaActiva - idPrev;

                            if (diff < minAnios)
                            {
                                bool excepcionAprobada = _iglesiaService.TieneExcepcionAprobada(rncCedula, idTemporadaActiva);

                                return Json(new
                                {
                                    existe = false,
                                    requiereExcepcion = true,
                                    excepcionAprobada,
                                    idIglesiaPrevia = idIgAnt,
                                    nombreIglesiaPrevia = nomIgAnt,
                                    nombreEquipoPrevia = nomEqAnt,
                                    temporadaPrevia = nomPrev,
                                    diferenciaTemporadas = diff,
                                    minAnios,
                                    mensaje = excepcionAprobada
                                        ? $"La iglesia '{nomIgAnt}' participó en la temporada '{nomPrev}' ({diff} temp. de diferencia), pero cuenta con una EXCEPCIÓN APROBADA (CE y CMI) para esta temporada."
                                        : $"ATENCIÓN: La iglesia '{nomIgAnt}' participó en la temporada reciente '{nomPrev}' ({diff} temp. de diferencia; mínimo requerido: {minAnios}). Requiere una excepción formal aprobada por CE y CMI para poder participar."
                                }, JsonRequestBehavior.AllowGet);
                            }
                        }
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
                return RedirectToAction("Detalle", new { id });
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

            CargarEquiposDisponibles();
            CargarCatalogosDenominacionesYTipos();
            ViewBag.UsuarioActual = u;
            ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u) && esTemporadaActual;
            return View(iglesia);
        }

        // POST: Iglesia/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            CargarEquiposDisponibles();
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
            if (!ValidarFormatosDR(modelo, out string errorValidacion))
            {
                TempData["MensajeError"] = errorValidacion;
                CargarEquiposDisponibles();
                ViewBag.UsuarioActual = u;
                ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u);
                return View(modelo);
            }

            // Validar seguridad de archivos adjuntos
            if (!ValidarArchivoSeguroIglesia(docPastor, out string errPastor))
            {
                TempData["MensajeError"] = "Cédula del Pastor: " + errPastor;
                CargarEquiposDisponibles();
                ViewBag.UsuarioActual = u;
                ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u);
                return View(modelo);
            }
            if (!ValidarArchivoSeguroIglesia(docLider, out string errLider))
            {
                TempData["MensajeError"] = "Cédula del Líder: " + errLider;
                CargarEquiposDisponibles();
                ViewBag.UsuarioActual = u;
                ViewBag.PuedeCambiarEquipo = PuedeCambiarEquipo(u);
                return View(modelo);
            }

            // Manejo seguro y permanente de archivos adjuntos en SQL
            if (docPastor != null && docPastor.ContentLength > 0)
            {
                modelo.Pastor.DocumentoAdjuntoRuta = ArchivoStorageHelper.GuardarArchivo("IglesiaPastor", modelo.IdIglesia, docPastor, "Iglesias");
            }
            else
            {
                modelo.Pastor.DocumentoAdjuntoRuta = iglesiaOriginal.Pastor?.DocumentoAdjuntoRuta;
            }

            if (docLider != null && docLider.ContentLength > 0)
            {
                modelo.LiderMinisterial.DocumentoAdjuntoRuta = ArchivoStorageHelper.GuardarArchivo("IglesiaLider", modelo.IdIglesia, docLider, "Iglesias");
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
                                RegistrarNotificacionReasignacion(cn, tran, modelo.NombreIglesia, iglesiaOriginal.IdEquipo, modelo.IdEquipo);
                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                            }
                        }
                    }
                }

                SOR.Helpers.AuditoriaHelper.Registrar(u.IdUsuario, u.Correo, "UPDATE", "Iglesia", modelo.IdIglesia.ToString(), "Edición de iglesia: " + modelo.NombreIglesia);
                TempData["MensajeExito"] = "Expediente de la iglesia actualizado exitosamente.";
                return RedirectToAction("Detalle", new { id = modelo.IdIglesia });
            }
            catch (System.Data.DBConcurrencyException exConc)
            {
                TempData["MensajeError"] = exConc.Message;
                return RedirectToAction("Detalle", new { id = modelo.IdIglesia });
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
                CargarEquiposDisponibles();
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

        private void RegistrarNotificacionReasignacion(SqlConnection cn, SqlTransaction tran, string nombreIglesia, int idEquipoAnterior, int idEquipoNuevo)
        {
            string nombreEqAnterior = "Equipo Anterior";
            string nombreEqNuevo = "Equipo Nuevo";

            string sqlEq = "SELECT IdEquipo, NombreEquipo FROM dbo.Equipos WHERE IdEquipo IN (@IdAnterior, @IdNuevo);";
            using (SqlCommand cmdEq = new SqlCommand(sqlEq, cn, tran))
            {
                cmdEq.Parameters.Add(new SqlParameter("@IdAnterior", idEquipoAnterior));
                cmdEq.Parameters.Add(new SqlParameter("@IdNuevo", idEquipoNuevo));
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
                cmdUsers.Parameters.Add(new SqlParameter("@IdAnterior", idEquipoAnterior));
                cmdUsers.Parameters.Add(new SqlParameter("@IdNuevo", idEquipoNuevo));
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
                    cmdIns.Parameters.Add(new SqlParameter("@IdDest", userId));
                    cmdIns.Parameters.Add(new SqlParameter("@Msg", msg));
                    cmdIns.ExecuteNonQuery();
                }
            }
        }

        public static List<Notificacion> ObtenerNotificacionesUsuario(int idUsuario)
        {
            List<Notificacion> lista = new List<Notificacion>();
            try
            {
                string connStr = SOR.Helpers.ConnectionHelper.ObtenerCadenaConexion();

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
                        cmd.Parameters.Add(new SqlParameter("@IdUser", idUsuario));
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
                cmd.Parameters.Add(new SqlParameter("@IdUser", u.IdUsuario));
                cmd.Parameters.Add(new SqlParameter("@Id", idNotificacion));
                cn.Open();
                cmd.ExecuteNonQuery();
            }

            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }

        // ============================================================================
        // ETAPA 7: ENTREGA / DESPACHO DE MATERIALES
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEntregaDespacho(int idParticipacion, int idIglesia, string tipoReceptor, string nombreReceptor, string cedula, string telefono, string observaciones)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para confirmar la entrega de materiales de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (string.IsNullOrWhiteSpace(nombreReceptor))
            {
                TempData["MensajeError"] = "Debe indicar el nombre de la persona o líder que recibe los materiales.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.ConfirmarEntregaDirecta(idParticipacion, idIglesia, tipoReceptor ?? "Pastor / Líder", nombreReceptor.Trim(), cedula?.Trim(), telefono?.Trim(), observaciones?.Trim(), u.IdUsuario);
                TempData["MensajeExito"] = "¡Entrega de materiales confirmada exitosamente! La iglesia ahora figura como Despachada / Entregada y puede proceder con los reportes ministeriales.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarNoEntregaDespacho(int idParticipacion, int idIglesia, string motivoNoEntrega, string observaciones)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para modificar el estado de entrega de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (string.IsNullOrWhiteSpace(motivoNoEntrega))
            {
                TempData["MensajeError"] = "Debe especificar el motivo por el cual no se realizó o no se realizará la entrega.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.MarcarNoEntregaDirecta(idParticipacion, idIglesia, motivoNoEntrega.Trim(), observaciones?.Trim(), u.IdUsuario);
                TempData["MensajeExito"] = "Se ha registrado el estado de NO ENTREGA con el motivo especificado.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReprogramarEntregaDespacho(int idParticipacion, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para modificar la asignación de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.ReprogramarEntregaDirecta(idParticipacion, idIglesia, u.IdUsuario);
                TempData["MensajeExito"] = "La asignación ha sido restablecida a 'Disponible / Pendiente de Despacho'.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // REPORTES MINISTERILES: EVENTOS EVANGELÍSTICOS
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarEventoEvangelistico(EventoEvangelisticoItem item, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para gestionar eventos de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (!item.FechaEvento.HasValue)
            {
                TempData["MensajeError"] = "Debe especificar la fecha de realización del evento evangelístico.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            if (item.CantidadNinosAsistieron < 0)
            {
                TempData["MensajeError"] = "La cantidad de niños asistentes no puede ser negativa.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                item.IdIglesia = idIglesia;
                _iglesiaService.GuardarEventoEvangelistico(item, u.IdUsuario);
                TempData["MensajeExito"] = "Evento evangelístico guardado correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarEventoEvangelistico(int idEventoDetalle, int idParticipacion, int idIglesia)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para eliminar eventos de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.EliminarEventoEvangelistico(idEventoDetalle, idParticipacion, idIglesia, u.IdUsuario);
                TempData["MensajeExito"] = "Evento evangelístico eliminado exitosamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarAnotacionesEvangelisticas(int idParticipacion, int idIglesia, string anotaciones)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para editar anotaciones de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            try
            {
                _iglesiaService.GuardarAnotacionesEventosEvangelisticos(idParticipacion, idIglesia, anotaciones?.Trim(), u.IdUsuario);
                TempData["MensajeExito"] = "Anotaciones de eventos evangelísticos guardadas correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }

        // ============================================================================
        // REPORTE OFICIAL DE DISCIPULADO / GRADUACIÓN LA GRAN AVENTURA (9 PREGUNTAS)
        // ============================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarReporteGraduacionLGA(ReporteGraduacionLGAModel vm, int idIglesia, HttpPostedFileBase adjuntoReporte)
        {
            Usuario u = (Usuario)Session["usuario"];
            if (u == null) return RedirectToAction("Login", "Acceso");

            var iglesia = _iglesiaService.ObtenerExpedienteIglesia(idIglesia);
            if (iglesia == null) return HttpNotFound();

            if (!PuedeEditarIglesia(u, iglesia.IdEquipo))
            {
                TempData["MensajeError"] = "No tiene permisos para enviar el reporte de discipulado de esta iglesia.";
                return RedirectToAction("Detalle", new { id = idIglesia });
            }

            // Manejo de archivo de evidencia opcional
            if (adjuntoReporte != null && adjuntoReporte.ContentLength > 0)
            {
                if (ValidarArchivoSeguroIglesia(adjuntoReporte, out string errAdjunto))
                {
                    try
                    {
                        string uploadPath = Server.MapPath("~/Uploads/Reportes/");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                        string ext = Path.GetExtension(adjuntoReporte.FileName).ToLowerInvariant();
                        string fileName = $"ReporteGrad_{idIglesia}_{Guid.NewGuid():N}{ext}";
                        adjuntoReporte.SaveAs(Path.Combine(uploadPath, fileName));
                        vm.ArchivoEvidenciaRuta = "/Uploads/Reportes/" + fileName;
                    }
                    catch (Exception exAdj)
                    {
                        TempData["MensajeError"] = "Error al guardar el archivo adjunto: " + exAdj.Message;
                        return RedirectToAction("Detalle", new { id = idIglesia });
                    }
                }
                else
                {
                    TempData["MensajeError"] = "Archivo de evidencia inválido: " + errAdjunto;
                    return RedirectToAction("Detalle", new { id = idIglesia });
                }
            }

            try
            {
                vm.IdIglesia = idIglesia;
                _iglesiaService.GuardarReporteGraduacionLGA(vm, u.IdUsuario);
                TempData["MensajeExito"] = "¡Reporte Oficial de Discipulado / Graduación de La Gran Aventura guardado con éxito! Estatus actualizado a 'Reportó'.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error de sistema al procesar la solicitud. Contacte al administrador.";
            }

            return RedirectToAction("Detalle", new { id = idIglesia });
        }
    }
}
