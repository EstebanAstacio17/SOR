using System;
using System.Configuration;
using SOR.Helpers;

namespace SOR.Repositories
{
    public abstract class BaseRepository
    {
        protected string ObtenerCadenaConexion()
        {
            if (ConfigurationManager.ConnectionStrings["ConexionSOR"] != null)
            {
                return ConfigurationManager.ConnectionStrings["ConexionSOR"].ConnectionString;
            }
            return @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";
        }

        protected void EjecutarConReintento(Action operacion, int maxReintentos = 3)
        {
            SqlRetryHelper.Ejecutar(operacion, maxReintentos);
        }

        protected T EjecutarConReintento<T>(Func<T> operacion, int maxReintentos = 3)
        {
            return SqlRetryHelper.Ejecutar(operacion, maxReintentos);
        }
    }
}
