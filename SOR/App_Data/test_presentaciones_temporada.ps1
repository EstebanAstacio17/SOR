$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "--- TEST OBTENER PRESENTACIONES POR TEMPORADA ---" -ForegroundColor Cyan

$sql = @"
SELECT p.*, m.Codigo, m.NombreMaterial, t.NombreTemporada,
       ISNULL((SELECT COUNT(1) FROM dbo.RecepcionesContenedorDetalle rcd WHERE rcd.IdPresentacion = p.IdPresentacion), 0) AS TotalMovimientos
FROM dbo.PresentacionesMaterial p
INNER JOIN dbo.Materiales m ON p.IdMaterial = m.IdMaterial
LEFT JOIN dbo.Temporadas t ON p.IdTemporadaVigencia = t.IdTemporada
WHERE (@SoloActivas = 0 OR p.Activo = 1)
ORDER BY ISNULL(t.FechaInicio, '1900-01-01') DESC, m.Codigo, p.UnidadesPorEmpaque;
"@

$cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
$cmd.Parameters.AddWithValue("@SoloActivas", 0)
$dr = $cmd.ExecuteReader()

$count = 0
while ($dr.Read()) {
    $count++
    $cod = $dr["Codigo"]
    $mat = $dr["NombreMaterial"]
    $tipo = $dr["TipoEmpaque"]
    $uds = $dr["UnidadesPorEmpaque"]
    $temp = if ($dr["NombreTemporada"] -ne [DBNull]::Value) { $dr["NombreTemporada"] } else { "Global / Todas" }
    $movs = $dr["TotalMovimientos"]
    $activo = if ($dr["Activo"]) { "Activa" } else { "Inactiva" }
    Write-Host "[$count] $cod - $mat | $tipo x $uds uds | Temp: $temp | Movs: $movs | Estado: $activo"
}
$dr.Close()
$conn.Close()

Write-Host "--- Total Presentaciones Listadas: $count ---" -ForegroundColor Green
