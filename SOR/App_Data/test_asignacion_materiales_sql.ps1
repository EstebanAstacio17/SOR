$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "--- TEST ASIGNACION DE MATERIALES EN SQL SERVER ---"

$cmdGet = New-Object System.Data.SqlClient.SqlCommand("SELECT TOP 1 IdParticipacion FROM dbo.ParticipacionesIglesia;", $conn)
$idPart = $cmdGet.ExecuteScalar()

if ($idPart -ne $null) {
    Write-Host "Probando con IdParticipacion: $idPart"
    
    $sqlAsig = "IF EXISTS (SELECT 1 FROM dbo.AsignacionesRecursos WHERE IdParticipacion = @IdPart) " +
        "BEGIN " +
        "    UPDATE dbo.AsignacionesRecursos SET " +
        "        OportunidadesEvangelisticas = @Oportunidades, " +
        "        LibrosMejorRegalo = @Regalo, " +
        "        LibrosMaestros = @Maestros, " +
        "        LibrosAlumno = @Alumno, " +
        "        Posters = @Posters, " +
        "        NuevosTestamentos = @Testamentos, " +
        "        EstadoAsignacion = 'DISPONIBLE_PARA_DESPACHO', " +
        "        FechaDisponibleDespacho = GETDATE(), " +
        "        IdEventoDespachoActual = @IdEventoDespacho " +
        "    WHERE IdParticipacion = @IdPart; " +
        "END " +
        "ELSE " +
        "BEGIN " +
        "    INSERT INTO dbo.AsignacionesRecursos " +
        "        (IdParticipacion, OportunidadesEvangelisticas, LibrosMejorRegalo, LibrosMaestros, LibrosAlumno, Posters, NuevosTestamentos, EstadoAsignacion, FechaDisponibleDespacho, IdEventoDespachoActual) " +
        "    VALUES " +
        "        (@IdPart, @Oportunidades, @Regalo, @Maestros, @Alumno, @Posters, @Testamentos, 'DISPONIBLE_PARA_DESPACHO', GETDATE(), @IdEventoDespacho); " +
        "END;"

    $cmdTest = New-Object System.Data.SqlClient.SqlCommand($sqlAsig, $conn)
    $cmdTest.Parameters.AddWithValue("@IdPart", [int]$idPart)
    $cmdTest.Parameters.AddWithValue("@Oportunidades", 50)
    $cmdTest.Parameters.AddWithValue("@Regalo", 50)
    $cmdTest.Parameters.AddWithValue("@Maestros", 5)
    $cmdTest.Parameters.AddWithValue("@Alumno", 50)
    $cmdTest.Parameters.AddWithValue("@Posters", 1)
    $cmdTest.Parameters.AddWithValue("@Testamentos", 50)
    $cmdTest.Parameters.AddWithValue("@IdEventoDespacho", [DBNull]::Value)
    
    $cmdTest.ExecuteNonQuery()
    Write-Host "[SUCCESS] La asignacion se ejecuto correctamente en SQL Server."
}

$conn.Close()
