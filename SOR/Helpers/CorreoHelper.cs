using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json;

namespace SOR.Helpers
{
    public static class CorreoHelper
    {
        private static string ObtenerConfig(string clave, string valorPorDefecto)
        {
            string env = Environment.GetEnvironmentVariable(clave);
            if (!string.IsNullOrWhiteSpace(env) && !env.Contains("PLACEHOLDER")) return env.Trim();
            string v = ConfigurationManager.AppSettings[clave];
            if (!string.IsNullOrWhiteSpace(v) && !v.Contains("PLACEHOLDER")) return v.Trim();
            return valorPorDefecto;
        }

        private static string ObtenerApiKeyBrevo()
        {
            return ObtenerConfig("SmtpClave", "");
        }

        private static void RegistrarLog(string destinatario, string asunto, bool exito, string detalle = null)
        {
            try
            {
                string rutaAppData = HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(rutaAppData))
                {
                    rutaAppData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                }
                if (!Directory.Exists(rutaAppData))
                {
                    Directory.CreateDirectory(rutaAppData);
                }

                string archivoLog = Path.Combine(rutaAppData, "logs_correo.txt");
                string linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Destinatario: {destinatario} | Asunto: {asunto} | Estado: {(exito ? "ENVIADO_EXITOSO" : "ERROR")} | Detalle: {detalle ?? "N/A"}{Environment.NewLine}";
                File.AppendAllText(archivoLog, linea, Encoding.UTF8);
            }
            catch { }
        }

        private static bool EnviarViaBrevoApi(string destinatario, string asunto, string cuerpoHtml, out string error)
        {
            error = null;
            string apiKey = ObtenerApiKeyBrevo();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                error = "Clave API de Brevo no configurada.";
                return false;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var request = (HttpWebRequest)WebRequest.Create("https://api.brevo.com/v3/smtp/email");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("api-key", apiKey.Trim());
                request.Timeout = 15000;

                string remitenteNombre = ObtenerConfig("NombreRemitente", "Operation Christmas Child (OCC) — Notificaciones");
                string remitenteCorreo = ObtenerConfig("CorreoRemitente", "erlegsd.occrd@gmail.com");

                var payload = new
                {
                    sender = new { name = remitenteNombre, email = remitenteCorreo },
                    to = new[] { new { email = destinatario.Trim() } },
                    replyTo = new { email = "noreply@occ-sor.org", name = "No Responder" },
                    subject = asunto,
                    htmlContent = cuerpoHtml
                };

                string json = JsonConvert.SerializeObject(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string resp = reader.ReadToEnd();
                    return response.StatusCode == HttpStatusCode.OK || 
                           response.StatusCode == HttpStatusCode.Created || 
                           response.StatusCode == HttpStatusCode.Accepted;
                }
            }
            catch (WebException wex)
            {
                string respError = "";
                if (wex.Response != null)
                {
                    try
                    {
                        using (var r = new StreamReader(wex.Response.GetResponseStream()))
                        {
                            respError = r.ReadToEnd();
                        }
                    }
                    catch { }
                }
                error = $"Brevo API HTTP Error: {wex.Message} -> {respError}";
                return false;
            }
            catch (Exception ex)
            {
                error = "Brevo API Exception: " + ex.Message;
                return false;
            }
        }

        private static bool EnviarViaSmtp(string destinatario, string asunto, string cuerpoHtml, out string error)
        {
            error = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                string host = ObtenerConfig("SmtpHost", "smtp-relay.brevo.com");
                int port = int.TryParse(ObtenerConfig("SmtpPort", "587"), out int p) ? p : 587;
                bool enableSsl = bool.TryParse(ObtenerConfig("SmtpEnableSsl", "true"), out bool ssl) ? ssl : true;
                string usuario = ObtenerConfig("SmtpUsuario", "bb3274001@smtp-brevo.com");
                string clave = ObtenerApiKeyBrevo();
                string remitenteCorreo = ObtenerConfig("CorreoRemitente", "erlegsd.occrd@gmail.com");
                string remitenteNombre = ObtenerConfig("NombreRemitente", "Operation Christmas Child (OCC) — Notificaciones");

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(remitenteCorreo, remitenteNombre, Encoding.UTF8);
                    mail.ReplyToList.Add(new MailAddress("noreply@occ-sor.org", "No Responder"));
                    mail.Subject = asunto;
                    mail.Body = cuerpoHtml;
                    mail.IsBodyHtml = true;
                    mail.SubjectEncoding = Encoding.UTF8;
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.To.Add(destinatario.Trim());

                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = enableSsl;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(clave))
                        {
                            client.Credentials = new NetworkCredential(usuario.Trim(), clave.Trim());
                        }
                        client.Timeout = 15000;
                        client.Send(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "SMTP Error: " + ex.Message;
                return false;
            }
        }

        private static string GenerarPlantillaHtml(string titulo, string saludo, string contenidoHtml, string textoBoton, string urlBoton, string pieNota = null)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang=\"es\"><head><meta charset=\"utf-8\"/><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"/>");
            sb.Append("<style>");
            sb.Append("body { font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 20px 10px; color: #1e293b; }");
            sb.Append(".card { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.07); border: 1px solid #e2e8f0; }");
            sb.Append(".header { background: linear-gradient(135deg, #003580 0%, #0056b3 100%); color: #ffffff; padding: 28px 24px; text-align: center; }");
            sb.Append(".header h1 { margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px; }");
            sb.Append(".header p { margin: 6px 0 0 0; font-size: 13px; opacity: 0.9; }");
            sb.Append(".badge-noreply { display: inline-block; background: rgba(255,255,255,0.2); padding: 3px 10px; border-radius: 12px; font-size: 11px; margin-top: 8px; }");
            sb.Append(".content { padding: 28px 24px; line-height: 1.6; font-size: 14px; }");
            sb.Append(".saludo { font-size: 16px; font-weight: 700; color: #003580; margin-bottom: 12px; }");
            sb.Append(".info-box { background: #f8fafc; border-left: 4px solid #008752; padding: 14px 16px; border-radius: 6px; margin: 18px 0; font-size: 13.5px; }");
            sb.Append(".btn-container { text-align: center; margin: 28px 0; }");
            sb.Append(".btn { display: inline-block; background-color: #008752; color: #ffffff !important; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 700; font-size: 14px; letter-spacing: 0.3px; box-shadow: 0 3px 8px rgba(0, 135, 82, 0.3); }");
            sb.Append(".footer { background: #f8fafc; padding: 16px 24px; text-align: center; font-size: 11px; color: #64748b; border-top: 1px solid #e2e8f0; }");
            sb.Append("</style></head><body>");

            sb.Append("<div class=\"card\">");
            sb.Append("<div class=\"header\">");
            sb.Append("<h1>Operation Christmas Child (OCC)</h1>");
            sb.Append("<p>Sistema de Gestión Operativa, Recursos y Logística (SOR)</p>");
            sb.Append("<span class=\"badge-noreply\">Notificación Automática — No Responder</span>");
            sb.Append("</div>");

            sb.Append("<div class=\"content\">");
            sb.Append($"<div class=\"saludo\">{saludo}</div>");
            sb.Append(contenidoHtml);

            if (!string.IsNullOrEmpty(textoBoton) && !string.IsNullOrEmpty(urlBoton))
            {
                sb.Append("<div class=\"btn-container\">");
                sb.Append($"<a href=\"{urlBoton}\" class=\"btn\" target=\"_blank\">{textoBoton}</a>");
                sb.Append("</div>");
                sb.Append($"<p style=\"font-size:11.5px;color:#94a3b8;word-break:break-all;text-align:center;\">Si el botón no abre directamente, accede a este enlace:<br/><a href=\"{urlBoton}\" style=\"color:#003580;\">{urlBoton}</a></p>");
            }

            if (!string.IsNullOrEmpty(pieNota))
            {
                sb.Append($"<div style=\"margin-top:20px;font-size:12px;color:#64748b;\">{pieNota}</div>");
            }

            sb.Append("</div>"); // /content

            sb.Append("<div class=\"footer\">");
            sb.Append($"Este es un correo automático enviado por el Sistema SOR — OCC ({DateTime.Now.Year}).<br/>");
            sb.Append("<strong>Esta cuenta es exclusivamente de emisión automática y no recibe mensajes entrantes.</strong>");
            sb.Append("</div>");

            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        public static void EnviarCorreoSeguro(string destinatario, string asunto, string cuerpoHtml)
        {
            if (string.IsNullOrWhiteSpace(destinatario)) return;
            EnviarCorreoSeguro(new List<string> { destinatario }, asunto, cuerpoHtml);
        }

        public static void EnviarCorreoSeguro(List<string> destinatarios, string asunto, string cuerpoHtml)
        {
            if (destinatarios == null || destinatarios.Count == 0) return;

            Task.Run(() =>
            {
                foreach (var dest in destinatarios)
                {
                    if (string.IsNullOrWhiteSpace(dest) || !dest.Contains("@")) continue;

                    string errorApi = null;
                    string errorSmtp = null;

                    // 1. Intentar vía API REST directa de Brevo
                    bool enviado = EnviarViaBrevoApi(dest, asunto, cuerpoHtml, out errorApi);

                    // 2. Si falla o no aplica, intentar vía SMTP
                    if (!enviado)
                    {
                        enviado = EnviarViaSmtp(dest, asunto, cuerpoHtml, out errorSmtp);
                    }

                    if (enviado)
                    {
                        RegistrarLog(dest, asunto, true, "OK (Entregado vía Brevo)");
                    }
                    else
                    {
                        RegistrarLog(dest, asunto, false, $"Fallo API: {errorApi} | Fallo SMTP: {errorSmtp}");
                    }
                }
            });
        }

        /// <summary>
        /// 1. Notificación al usuario cuando su correo es aprobado por la administración
        /// </summary>
        public static void NotificarCorreoAprobado(string correoDestino, string nombreUsuario, string urlCompletarPerfil)
        {
            string asunto = "[OCC-SOR] Tu correo ha sido aprobado — Completa tu perfil de coordinador";
            string saludo = $"¡Hola, {nombreUsuario ?? "Coordinador"}!";
            string contenido = @"
                <p>Nos complace informarte que tu correo electrónico ha sido <strong>verificado y aprobado exitosamente</strong> por la administración de <strong>Operation Christmas Child (OCC)</strong> en la plataforma <strong>SOR</strong>.</p>
                <div class=""info-box"">
                    <strong>Siguiente Paso Requerido:</strong><br/>
                    Para continuar con tu proceso de incorporación, por favor ingresa al sistema y completa el formulario con tus datos personales, ministeriales y de equipo en tu <strong>Perfil de Coordinador</strong>.
                </div>
                <p>Una vez completado el formulario, tu Coordinador de Equipo revisará tu solicitud para la autorización definitiva.</p>";

            string html = GenerarPlantillaHtml(
                "Correo Aprobado — OCC",
                saludo,
                contenido,
                "Completar Mi Perfil de Coordinador",
                urlCompletarPerfil
            );

            EnviarCorreoSeguro(correoDestino, asunto, html);
        }

        /// <summary>
        /// 2. Notificación al Coordinador de Equipo cuando un nuevo usuario completa su formulario de registro
        /// </summary>
        public static void NotificarPerfilCompletadoACoordinadorEquipo(List<string> correosCoordinadores, string nombreNuevoCoordinador, string nombreEquipo, string nombrePosicion, string urlAdmin)
        {
            string asunto = $"[OCC-SOR] Nuevo perfil de coordinador registrado para revisión — Equipo {nombreEquipo}";
            string saludo = "Estimado/a Coordinador/a de Equipo,";
            string contenido = $@"
                <p>Te informamos que se ha completado un nuevo registro de coordinador en tu equipo que requiere tu revisión y aprobación:</p>
                <div class=""info-box"">
                    <strong>Detalles del Registro:</strong><br/>
                    • <strong>Coordinador:</strong> {nombreNuevoCoordinador}<br/>
                    • <strong>Equipo Asignado:</strong> {nombreEquipo}<br/>
                    • <strong>Posición Solicitada:</strong> {nombrePosicion}<br/>
                    • <strong>Fecha de Registro:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}
                </div>
                <p>Por favor ingresa al módulo de administración de usuarios en la plataforma SOR para revisar sus datos y autorizar su acceso.</p>";

            string html = GenerarPlantillaHtml(
                "Nuevo Registro de Coordinador",
                saludo,
                contenido,
                "Revisar y Aprobar Usuario",
                urlAdmin
            );

            EnviarCorreoSeguro(correosCoordinadores, asunto, html);
        }

        /// <summary>
        /// 3. Notificación al usuario cuando su perfil de coordinador ha sido autorizado y aprobado
        /// </summary>
        public static void NotificarPerfilAutorizado(string correoDestino, string nombreUsuario, string nombreEquipo, string nombrePosicion, string urlLogin)
        {
            string asunto = "[OCC-SOR] ¡Bienvenido/a! Tu perfil de coordinador ha sido autorizado y aprobado";
            string saludo = $"¡Felicidades, {nombreUsuario ?? "Coordinador"}!";
            string contenido = $@"
                <p>Nos alegra comunicarte que tu perfil de coordinador ha sido <strong>revisado y autorizado oficialmente</strong> por la coordinación.</p>
                <div class=""info-box"">
                    <strong>Detalles de tu Asignación Activa:</strong><br/>
                    • <strong>Equipo:</strong> {nombreEquipo ?? "Equipo Asignado"}<br/>
                    • <strong>Posición:</strong> {nombrePosicion ?? "Coordinador"}<br/>
                    • <strong>Estado:</strong> <span style=""color:#008752;font-weight:700;"">ACTIVO</span>
                </div>
                <p>A partir de este momento tienes acceso completo a las funciones operativas, módulos y herramientas correspondientes a tu rol en la plataforma SOR.</p>";

            string html = GenerarPlantillaHtml(
                "Perfil Autorizado — OCC",
                saludo,
                contenido,
                "Iniciar Sesión en el Sistema",
                urlLogin
            );

            EnviarCorreoSeguro(correoDestino, asunto, html);
        }
    }
}
