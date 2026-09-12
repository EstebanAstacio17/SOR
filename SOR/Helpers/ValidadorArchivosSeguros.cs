using System;
using System.IO;
using System.Linq;
using System.Web;

namespace SOR.Helpers
{
    public static class ValidadorArchivosSeguros
    {
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf", ".xlsx", ".docx" };
        private static readonly string[] MimeTypesPermitidos = {
            "image/jpeg",
            "image/png",
            "image/pjpeg",
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword"
        };
        private const int PesoMaximoBytes = 15 * 1024 * 1024; // 15 MB

        /// <summary>
        /// Valida que el archivo subido sea seguro, tenga extensión y MIME permitidos, no exceda el límite y genera un nombre seguro aleatorio.
        /// </summary>
        public static bool EsArchivoValido(HttpPostedFileBase archivo, out string mensajeError, out string nombreSeguroGenerado)
        {
            mensajeError = string.Empty;
            nombreSeguroGenerado = string.Empty;

            if (archivo == null || archivo.ContentLength == 0)
            {
                mensajeError = "No se ha recibido ningún archivo o el archivo está vacío.";
                return false;
            }

            if (archivo.ContentLength > PesoMaximoBytes)
            {
                mensajeError = "El archivo excede el tamaño máximo permitido de 15MB.";
                return false;
            }

            string ext = Path.GetExtension(archivo.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !ExtensionesPermitidas.Contains(ext))
            {
                mensajeError = $"Formato no permitido ({ext}). Solo se admiten: PDF, JPG, PNG, XLSX y DOCX.";
                return false;
            }

            string mime = (archivo.ContentType ?? string.Empty).ToLowerInvariant();
            if (!MimeTypesPermitidos.Contains(mime))
            {
                mensajeError = "El tipo de contenido (MIME) del archivo no es reconocido como un formato seguro.";
                return false;
            }

            // Generar identificador UUID único para evitar path traversal y colisiones
            nombreSeguroGenerado = $"{Guid.NewGuid():N}{ext}";
            return true;
        }
    }
}
