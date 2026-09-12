using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace SOR.Helpers
{
    /// <summary>
    /// Proveedor de resiliencia y reintento exponencial para operaciones ADO.NET contra Azure SQL Database.
    /// Maneja desconexiones transitorias, throttling y caídas momentáneas de red.
    /// </summary>
    public static class SqlRetryHelper
    {
        private static readonly HashSet<int> TransientErrorNumbers = new HashSet<int>
        {
            40613, // Database not currently available
            40197, // Service has encountered an error processing your request
            40501, // The service is currently busy
            49918, // Cannot process request. Not enough resources
            49919, // Cannot process create or update request
            49920, // Cannot process request. Too many operations
            4060,  // Cannot open database requested by the login
            10053, // Transport-level error: Connection aborted
            10054, // Transport-level error: Connection reset by peer
            10060, // Network timeout
            -2     // SQL Client Execution Timeout
        };

        public static bool EsErrorTransitorio(SqlException ex)
        {
            if (ex == null) return false;

            foreach (SqlError error in ex.Errors)
            {
                if (TransientErrorNumbers.Contains(error.Number))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Ejecutar(Action operacion, int maxReintentos = 3, int delayInicialMs = 300)
        {
            Ejecutar<bool>(() =>
            {
                operacion();
                return true;
            }, maxReintentos, delayInicialMs);
        }

        public static T Ejecutar<T>(Func<T> operacion, int maxReintentos = 3, int delayInicialMs = 300)
        {
            int intento = 0;
            Random rnd = new Random();

            while (true)
            {
                try
                {
                    intento++;
                    return operacion();
                }
                catch (SqlException ex)
                {
                    if (intento > maxReintentos || !EsErrorTransitorio(ex))
                    {
                        Trace.TraceError(string.Format("[SqlRetryHelper] Fallo no transitorio o limite alcanzado (Intento {0}/{1}): {2}", intento, maxReintentos, ex.Message));
                        throw;
                    }

                    int backoffMs = (int)(delayInicialMs * Math.Pow(2, intento - 1)) + rnd.Next(50, 150);
                    Trace.TraceWarning(string.Format("[SqlRetryHelper] Error transitorio de Azure SQL (Codigo {0}). Reintento {1}/{2} en {3}ms...", ex.Number, intento, maxReintentos, backoffMs));
                    Thread.Sleep(backoffMs);
                }
                catch (TimeoutException ex)
                {
                    if (intento > maxReintentos)
                    {
                        Trace.TraceError(string.Format("[SqlRetryHelper] Timeout permanente tras {0} intentos: {1}", intento, ex.Message));
                        throw;
                    }

                    int backoffMs = (int)(delayInicialMs * Math.Pow(2, intento - 1)) + rnd.Next(50, 150);
                    Trace.TraceWarning(string.Format("[SqlRetryHelper] Timeout transitorio. Reintentando en {0}ms...", backoffMs));
                    Thread.Sleep(backoffMs);
                }
            }
        }
    }

    /// <summary>
    /// Proveedor centralizado y resiliente para la resolución y saneamiento de cadenas de conexión.
    /// Resuelve variables de entorno de Azure App Service, ConfigurationManager y sanitiza comillas/caracteres inválidos.
    /// </summary>
    public static class ConnectionHelper
    {
        public const string DefaultAzureConnectionString = "Server=tcp:svrsor.database.windows.net,1433;Initial Catalog=DB_SOR;Persist Security Info=False;User ID=CloudSA94a05d65;Password=OCC_Sor2026!*;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;Pooling=True;Min Pool Size=5;Max Pool Size=150;ConnectRetryCount=3;ConnectRetryInterval=10;Application Name=SOR_Azure_Prod;";

        public static string ObtenerCadenaConexion()
        {
            string connStr = null;

            // 1. Verificar variables de entorno inyectadas por Azure App Service
            connStr = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_ConexionSOR");
            if (string.IsNullOrWhiteSpace(connStr))
                connStr = Environment.GetEnvironmentVariable("SQLCONNSTR_ConexionSOR");
            if (string.IsNullOrWhiteSpace(connStr))
                connStr = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_ConexionSOR");
            if (string.IsNullOrWhiteSpace(connStr))
                connStr = Environment.GetEnvironmentVariable("ConexionSOR");

            // 2. Verificar ConnectionStrings en Web.config / ConfigurationManager
            if (string.IsNullOrWhiteSpace(connStr))
            {
                try
                {
                    if (System.Configuration.ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
                    {
                        connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
                    }
                }
                catch { }
            }

            // 3. Sanitizar comillas envolventes, backticks, comillas tipográficas y espacios en blanco
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                connStr = connStr.Trim().Trim('"', '\'', '`', ' ', '\t', '\r', '\n', '“', '”', '‘', '’');
            }

            // 4. Si la cadena está vacía o no contiene palabras clave válidas de conexión, usar la de Azure SQL
            if (string.IsNullOrWhiteSpace(connStr) || (!connStr.ToLowerInvariant().Contains("server=") && !connStr.ToLowerInvariant().Contains("data source=")))
            {
                connStr = DefaultAzureConnectionString;
            }

            return connStr;
        }
    }
}
