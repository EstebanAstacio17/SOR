using System;
using System.Security.Cryptography;
using System.Text;

namespace SOR.Helpers
{
    public static class Criptografia
    {
        /// <summary>
        /// Genera una sal (salt) aleatoria de 16 bytes en formato Base64.
        /// </summary>
        public static string GenerarSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Genera el hash SHA-256 combinando la contraseña con la sal.
        /// </summary>
        public static string HashClave(string clave, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(clave + salt);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Crea una cadena formateada "salt:hash" para almacenar en la base de datos.
        /// </summary>
        public static string CrearClaveFormateada(string clave)
        {
            string salt = GenerarSalt();
            string hash = HashClave(clave, salt);
            return $"{salt}:{hash}";
        }

        /// <summary>
        /// Verifica si la contraseña coincide con el formato "salt:hash" guardado.
        /// Soporta retrocompatibilidad con contraseñas SHA-256 antiguas (sin salt, longitud 64 caracteres hex).
        /// </summary>
        public static bool VerificarClave(string claveIngresada, string claveGuardada)
        {
            if (string.IsNullOrEmpty(claveGuardada)) return false;

            // Retrocompatibilidad con contraseñas antiguas hasheadas en SHA-256 hex simple (64 caracteres hexadecimales)
            if (!claveGuardada.Contains(":") && claveGuardada.Length == 64)
            {
                string hashIngresadoHex = ConvertirSha256Hex(claveIngresada);
                return string.Equals(hashIngresadoHex, claveGuardada, StringComparison.OrdinalIgnoreCase);
            }

            // Esquema moderno: salt:hash
            string[] partes = claveGuardada.Split(':');
            if (partes.Length != 2) return false;

            string salt = partes[0];
            string hashEsperado = partes[1];
            string hashIngresado = HashClave(claveIngresada, salt);

            return string.Equals(hashIngresado, hashEsperado);
        }

        /// <summary>
        /// Método auxiliar para mantener compatibilidad con contraseñas SHA-256 hexadecimales antiguas.
        /// </summary>
        private static string ConvertirSha256Hex(string texto)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));
                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
