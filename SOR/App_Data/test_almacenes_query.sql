SELECT a.*, 
       p.PrimerNombre, p.PrimerApellido, p.TelefonoCelularWhatsApp AS TelUsuario
FROM dbo.Almacenes a
LEFT JOIN dbo.Usuarios u ON a.IdUsuarioResponsable = u.IdUsuario
LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
WHERE a.Activo = 1 
ORDER BY a.NombreAlmacen;

SELECT ae.IdAlmacen, ae.IdEquipo, e.NombreEquipo
FROM dbo.AlmacenesEquipos ae
INNER JOIN dbo.Equipos e ON ae.IdEquipo = e.IdEquipo
ORDER BY e.NombreEquipo;
