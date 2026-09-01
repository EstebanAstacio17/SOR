using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SOR
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            InicializarColumnasBloqueo();
            SOR.Helpers.DatabaseSchemaHelper.AsegurarIntegridadYConcurrencia();
        }

        private void InicializarColumnasBloqueo()
        {
            string conexionStr = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionSOR"]?.ConnectionString 
                ?? @"Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;";

            using (System.Data.SqlClient.SqlConnection cn = new System.Data.SqlClient.SqlConnection(conexionStr))
            {
                string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'IntentosFallidosLogin')
                    BEGIN
                        ALTER TABLE dbo.Usuarios ADD IntentosFallidosLogin INT NOT NULL DEFAULT 0;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'FechaUltimoIntentoFallido')
                    BEGIN
                        ALTER TABLE dbo.Usuarios ADD FechaUltimoIntentoFallido DATETIME NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'FechaBloqueo')
                    BEGIN
                        ALTER TABLE dbo.Usuarios ADD FechaBloqueo DATETIME NULL;
                    END
                    
                    -- Agregar estados de cuenta si no existen
                    IF NOT EXISTS (SELECT * FROM dbo.EstadosCuenta WHERE IdEstado = 7)
                    BEGIN
                        INSERT INTO dbo.EstadosCuenta (IdEstado, NombreEstado) VALUES (7, 'Pendiente Restablecimiento');
                    END
                    IF NOT EXISTS (SELECT * FROM dbo.EstadosCuenta WHERE IdEstado = 8)
                    BEGIN
                        INSERT INTO dbo.EstadosCuenta (IdEstado, NombreEstado) VALUES (8, 'Aprobado Restablecimiento');
                    END";
                
                try
                {
                    cn.Open();
                    using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception)
                {
                    // Ignorar errores en caso de que ocurran en el arranque
                }
            }
        }
    }
}
