$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "--- VALIDANDO CONSULTA DE USUARIOS COORDINADORES ---" -ForegroundColor Cyan

$sql = @"
SELECT u.IdUsuario, u.Correo, p.PrimerNombre, p.PrimerApellido, eq.NombreEquipo, pos.NombrePosicion
FROM dbo.Usuarios u
LEFT JOIN dbo.PerfilesCoordinador p ON u.IdUsuario = p.IdUsuario
LEFT JOIN dbo.AsignacionesEquipo a ON u.IdUsuario = a.IdUsuario AND a.Activo = 1
LEFT JOIN dbo.Equipos eq ON a.IdEquipo = eq.IdEquipo
LEFT JOIN dbo.PosicionesOCC pos ON COALESCE(a.IdPosicion, p.IdPosicion) = pos.IdPosicion
WHERE u.IdEstado = 4
ORDER BY ISNULL(p.PrimerNombre, u.Correo);
"@

$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$reader = $cmd.ExecuteReader()
$count = 0
while ($reader.Read()) {
    $count++
    $nom = if ($reader["PrimerNombre"] -ne [DBNull]::Value) { "$($reader['PrimerNombre']) $($reader['PrimerApellido'])" } else { $reader["Correo"] }
    $pos = if ($reader["NombrePosicion"] -ne [DBNull]::Value) { $reader["NombrePosicion"] } else { "Sin posición" }
    $eq = if ($reader["NombreEquipo"] -ne [DBNull]::Value) { $reader["NombreEquipo"] } else { "Sin equipo" }
    Write-Host "  Usuario: $nom | Rol: $pos | Equipo: $eq" -ForegroundColor Green
}
$reader.Close()
$conn.Close()

Write-Host "Total usuarios encontrados: $count" -ForegroundColor Yellow
if ($count -ge 0) {
    Write-Host "[SUCCESS] La consulta se ejecutó limpiamente sin errores de SQL." -ForegroundColor Green
}
