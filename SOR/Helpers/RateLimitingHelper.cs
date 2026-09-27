using System;
using System.Collections.Concurrent;

namespace SOR.Helpers
{
    /// <summary>
    /// Rate limiter en memoria de alto rendimiento y cero dependencias para mitigar ataques de fuerza bruta en endpoints sensibles (Login / Recuperar Clave).
    /// </summary>
    public static class RateLimitingHelper
    {
        private class RegistroIntento
        {
            public int Contador { get; set; }
            public DateTime PrimeraPeticion { get; set; }
        }

        private static readonly ConcurrentDictionary<string, RegistroIntento> Intentos = new ConcurrentDictionary<string, RegistroIntento>();

        /// <summary>
        /// Comprueba si una clave (IP o IP_Usuario) ha excedido el número máximo de intentos en una ventana de tiempo.
        /// </summary>
        /// <param name="clave">Identificador único (ej: IP del cliente)</param>
        /// <param name="maxPeticiones">Máximo de intentos permitidos en la ventana</param>
        /// <param name="ventanaMinutos">Ventana de tiempo en minutos</param>
        /// <returns>True si la petición está permitida, False si se ha excedido el límite</returns>
        public static bool EstaPermitido(string clave, int maxPeticiones = 10, int ventanaMinutos = 5)
        {
            if (string.IsNullOrEmpty(clave)) return true;

            DateTime ahora = DateTime.UtcNow;
            RegistroIntento registro = Intentos.GetOrAdd(clave, k => new RegistroIntento
            {
                Contador = 0,
                PrimeraPeticion = ahora
            });

            lock (registro)
            {
                if (ahora - registro.PrimeraPeticion > TimeSpan.FromMinutes(ventanaMinutos))
                {
                    registro.Contador = 1;
                    registro.PrimeraPeticion = ahora;
                    return true;
                }

                registro.Contador++;
                return registro.Contador <= maxPeticiones;
            }
        }

        /// <summary>
        /// Resetea el contador cuando una operación es exitosa (ej: login correcto).
        /// </summary>
        public static void Resetear(string clave)
        {
            if (!string.IsNullOrEmpty(clave))
            {
                RegistroIntento dummy;
                Intentos.TryRemove(clave, out dummy);
            }
        }
    }
}
