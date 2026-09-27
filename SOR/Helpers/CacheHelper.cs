using System;
using System.Web;
using System.Web.Caching;

namespace SOR.Helpers
{
    /// <summary>
    /// Proveedor de caché en memoria de alto rendimiento y costo cero para catálogos y consultas frecuentes.
    /// Reduce drásticamente las consultas a SQL Server manteniendo la consistencia de datos.
    /// </summary>
    public static class CacheHelper
    {
        private static Cache ObtenerCache()
        {
            return HttpRuntime.Cache ?? (HttpContext.Current != null ? HttpContext.Current.Cache : null);
        }

        /// <summary>
        /// Obtiene un valor de la caché o lo genera mediante la función provista y lo almacena con expiración.
        /// </summary>
        public static T ObtenerOAgregar<T>(string clave, Func<T> creador, int minutosExpiracion = 15) where T : class
        {
            var cache = ObtenerCache();
            if (cache == null)
            {
                return creador();
            }

            var item = cache.Get(clave) as T;
            if (item != null)
            {
                return item;
            }

            T nuevoItem = creador();
            if (nuevoItem != null)
            {
                cache.Insert(
                    clave,
                    nuevoItem,
                    null,
                    DateTime.UtcNow.AddMinutes(minutosExpiracion),
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Normal,
                    null
                );
            }

            return nuevoItem;
        }

        /// <summary>
        /// Invalida una clave específica de la caché cuando se crea, edita o elimina un catálogo.
        /// </summary>
        public static void Invalidar(string clave)
        {
            var cache = ObtenerCache();
            if (cache != null && !string.IsNullOrEmpty(clave))
            {
                cache.Remove(clave);
            }
        }

        /// <summary>
        /// Invalida todas las claves de caché que comiencen con el prefijo especificado.
        /// </summary>
        public static void InvalidarPorPrefijo(string prefijo)
        {
            var cache = ObtenerCache();
            if (cache == null || string.IsNullOrEmpty(prefijo)) return;

            var enumerator = cache.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string key = enumerator.Key.ToString();
                if (key.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                {
                    cache.Remove(key);
                }
            }
        }
    }
}
