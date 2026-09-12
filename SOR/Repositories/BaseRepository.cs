using System;
using System.Configuration;
using SOR.Helpers;

namespace SOR.Repositories
{
    public abstract class BaseRepository
    {
        protected string ObtenerCadenaConexion()
        {
            return ConnectionHelper.ObtenerCadenaConexion();
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
