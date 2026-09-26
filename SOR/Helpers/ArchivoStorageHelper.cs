using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace SOR.Helpers
{
    public class ArchivoDescarga
    {
        public byte[] Contenido { get; set; }
        public string MimeType { get; set; }
        public string NombreArchivo { get; set; }
        public string Extension { get; set; }
    }

    public static class ArchivoStorageHelper
    {
        private static string ObtenerCadenaConexion()
        {
            return ConnectionHelper.ObtenerCadenaConexion();
        }

        public static void AsegurarTablaArchivos()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sql = @"
                        IF OBJECT_ID('dbo.ArchivosAdjuntos', 'U') IS NULL
                        BEGIN
                            CREATE TABLE dbo.ArchivosAdjuntos (
                                IdArchivo INT IDENTITY(1,1) PRIMARY KEY,
                                TipoEntidad VARCHAR(50) NOT NULL,
                                IdEntidad INT NOT NULL,
                                NombreArchivo NVARCHAR(255) NOT NULL,
                                Extension VARCHAR(20) NOT NULL,
                                MimeType VARCHAR(100) NOT NULL,
                                TamanoBytes BIGINT NOT NULL,
                                Contenido VARBINARY(MAX) NOT NULL,
                                FechaCarga DATETIME2 NOT NULL DEFAULT GETDATE()
                            );

                            CREATE NONCLUSTERED INDEX IX_ArchivosAdjuntos_Entidad 
                            ON dbo.ArchivosAdjuntos (TipoEntidad, IdEntidad);
                        END";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error asegurando tabla ArchivosAdjuntos: " + ex.Message);
            }
        }

        /// <summary>
        /// Guarda un archivo tanto en la base de datos SQL (binario persistente) como en el disco del servidor (caché).
        /// </summary>
        public static string GuardarArchivo(string tipoEntidad, int idEntidad, HttpPostedFileBase archivo, string subCarpeta = "Usuarios")
        {
            if (archivo == null || archivo.ContentLength == 0)
                return null;

            AsegurarTablaArchivos();

            string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            string prefijo = tipoEntidad.Replace("Usuario", "").Replace("Iglesia", "");
            string fileName = $"{prefijo}_{idEntidad}_{Guid.NewGuid():N}{ext}";
            string rutaVirtual = $"/Uploads/{subCarpeta}/" + fileName;

            // 1. Leer los bytes en memoria
            byte[] bytes;
            using (var binaryReader = new BinaryReader(archivo.InputStream))
            {
                bytes = binaryReader.ReadBytes(archivo.ContentLength);
            }

            string mimeType = archivo.ContentType;
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                mimeType = MimeMapping.GetMimeMapping(fileName);
            }

            // 2. Guardar en Base de Datos SQL (VARBINARY MAX)
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sql = @"
                        DELETE FROM dbo.ArchivosAdjuntos WHERE TipoEntidad = @TipoEntidad AND IdEntidad = @IdEntidad;
                        INSERT INTO dbo.ArchivosAdjuntos (TipoEntidad, IdEntidad, NombreArchivo, Extension, MimeType, TamanoBytes, Contenido, FechaCarga)
                        VALUES (@TipoEntidad, @IdEntidad, @NombreArchivo, @Extension, @MimeType, @TamanoBytes, @Contenido, GETDATE());";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@TipoEntidad", tipoEntidad);
                        cmd.Parameters.AddWithValue("@IdEntidad", idEntidad);
                        cmd.Parameters.AddWithValue("@NombreArchivo", fileName);
                        cmd.Parameters.AddWithValue("@Extension", ext);
                        cmd.Parameters.AddWithValue("@MimeType", mimeType);
                        cmd.Parameters.AddWithValue("@TamanoBytes", (long)bytes.Length);
                        cmd.Parameters.Add("@Contenido", SqlDbType.VarBinary, -1).Value = bytes;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al persistir archivo en SQL ({tipoEntidad}-{idEntidad}): " + ex.Message);
            }

            // 3. Guardar en disco físico como copia de respaldo / caché
            try
            {
                if (HttpContext.Current != null)
                {
                    string uploadPath = HttpContext.Current.Server.MapPath($"~/Uploads/{subCarpeta}/");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }
                    File.WriteAllBytes(Path.Combine(uploadPath, fileName), bytes);
                }
            }
            catch
            {
                // Si el disco no tiene permisos o falla, el archivo ya está seguro en SQL Server
            }

            return rutaVirtual;
        }

        /// <summary>
        /// Obtiene un archivo binario desde la base de datos SQL o del disco como fallback.
        /// Si está en disco pero no en SQL, lo migra automáticamente a SQL.
        /// </summary>
        public static ArchivoDescarga ObtenerArchivo(string tipoEntidad, int idEntidad, string rutaVirtualFallback = null)
        {
            AsegurarTablaArchivos();

            // 1. Intentar obtener desde SQL Server
            try
            {
                using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                {
                    cn.Open();
                    string sql = @"
                        SELECT TOP 1 NombreArchivo, Extension, MimeType, Contenido 
                        FROM dbo.ArchivosAdjuntos 
                        WHERE TipoEntidad = @TipoEntidad AND IdEntidad = @IdEntidad 
                        ORDER BY IdArchivo DESC;";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@TipoEntidad", tipoEntidad);
                        cmd.Parameters.AddWithValue("@IdEntidad", idEntidad);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                byte[] contenido = (byte[])dr["Contenido"];
                                if (contenido != null && contenido.Length > 0)
                                {
                                    return new ArchivoDescarga
                                    {
                                        NombreArchivo = dr["NombreArchivo"].ToString(),
                                        Extension = dr["Extension"].ToString(),
                                        MimeType = dr["MimeType"].ToString(),
                                        Contenido = contenido
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar archivo en SQL ({tipoEntidad}-{idEntidad}): " + ex.Message);
            }

            // 2. Fallback: Si no estaba en SQL pero existe en disco local, leerlo y migrarlo a SQL
            if (!string.IsNullOrWhiteSpace(rutaVirtualFallback) && HttpContext.Current != null)
            {
                try
                {
                    string rutaFisica = null;
                    if (rutaVirtualFallback.StartsWith("~"))
                        rutaFisica = HttpContext.Current.Server.MapPath(rutaVirtualFallback);
                    else if (rutaVirtualFallback.StartsWith("/"))
                        rutaFisica = HttpContext.Current.Server.MapPath("~" + rutaVirtualFallback);
                    else
                        rutaFisica = HttpContext.Current.Server.MapPath("~/" + rutaVirtualFallback);

                    if (!string.IsNullOrEmpty(rutaFisica) && File.Exists(rutaFisica))
                    {
                        byte[] bytes = File.ReadAllBytes(rutaFisica);
                        string fileName = Path.GetFileName(rutaFisica);
                        string ext = Path.GetExtension(rutaFisica).ToLowerInvariant();
                        string mimeType = MimeMapping.GetMimeMapping(rutaFisica);

                        // Migrar a SQL para que quede permanente de por vida
                        using (SqlConnection cn = new SqlConnection(ObtenerCadenaConexion()))
                        {
                            cn.Open();
                            string sqlMigrate = @"
                                DELETE FROM dbo.ArchivosAdjuntos WHERE TipoEntidad = @TipoEntidad AND IdEntidad = @IdEntidad;
                                INSERT INTO dbo.ArchivosAdjuntos (TipoEntidad, IdEntidad, NombreArchivo, Extension, MimeType, TamanoBytes, Contenido, FechaCarga)
                                VALUES (@TipoEntidad, @IdEntidad, @NombreArchivo, @Extension, @MimeType, @TamanoBytes, @Contenido, GETDATE());";

                            using (SqlCommand cmd = new SqlCommand(sqlMigrate, cn))
                            {
                                cmd.Parameters.AddWithValue("@TipoEntidad", tipoEntidad);
                                cmd.Parameters.AddWithValue("@IdEntidad", idEntidad);
                                cmd.Parameters.AddWithValue("@NombreArchivo", fileName);
                                cmd.Parameters.AddWithValue("@Extension", ext);
                                cmd.Parameters.AddWithValue("@MimeType", mimeType);
                                cmd.Parameters.AddWithValue("@TamanoBytes", (long)bytes.Length);
                                cmd.Parameters.Add("@Contenido", SqlDbType.VarBinary, -1).Value = bytes;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        return new ArchivoDescarga
                        {
                            NombreArchivo = fileName,
                            Extension = ext,
                            MimeType = mimeType,
                            Contenido = bytes
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en migración fallback de disco a SQL: " + ex.Message);
                }
            }

            return null;
        }
    }
}
