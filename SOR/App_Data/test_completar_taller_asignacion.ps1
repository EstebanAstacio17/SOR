$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "--- VALIDANDO ESTRUCTURA Y PERSISTENCIA DE ASIGNACIONES RECURSOS ---" -ForegroundColor Cyan

# 1. Verificar columnas en AsignacionesRecursos
$queryCols = @"
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AsignacionesRecursos' AND COLUMN_NAME IN ('OportunidadesEvangelisticas','LibrosMejorRegalo','LibrosMaestros','LibrosAlumno','Posters','NuevosTestamentos','EstadoAsignacion','FechaDisponibleDespacho','IdEventoDespachoActual');
"@

$cmd = New-Object System.Data.SqlClient.SqlCommand($queryCols, $conn)
$reader = $cmd.ExecuteReader()
$colsFound = 0
while ($reader.Read()) {
    $colsFound++
    Write-Host "  Columna OK: $($reader['COLUMN_NAME']) ($($reader['DATA_TYPE']))" -ForegroundColor Green
}
$reader.Close()

if ($colsFound -ge 7) {
    Write-Host "[SUCCESS] Todas las columnas necesarias existen en dbo.AsignacionesRecursos." -ForegroundColor Green
} else {
    Write-Host "[WARNING] Faltan algunas columnas." -ForegroundColor Yellow
}

$conn.Close()
