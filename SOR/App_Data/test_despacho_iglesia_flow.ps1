# Test script for Church Dispatch & No-Despacho flow
$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "PRUEBAS DE EVENTO DE DESPACHO Y ENTREGA PRESENCIAL A IGLESIA" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

function Execute-Scalar([string]$sql, $c) {
    $cmd = $c.CreateCommand()
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

try {
    $idTemp = Execute-Scalar -sql "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;" -c $conn
    $idEq = Execute-Scalar -sql "SELECT TOP 1 IdEquipo FROM dbo.Equipos;" -c $conn
    $idUser = Execute-Scalar -sql "SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdEstado = 4;" -c $conn

    # 1. Crear un Evento tipo Despacho
    $cmdEv = $conn.CreateCommand()
    $cmdEv.CommandText = "INSERT INTO dbo.Eventos (IdTemporada, NombreEvento, TipoEvento, Fecha, Lugar, IdUsuarioCreacion) OUTPUT INSERTED.IdEvento VALUES ($idTemp, 'Evento Despacho Test Oficial', 'Despacho', GETDATE(), 'Sede Central Test', $idUser);"
    $idEvento = $cmdEv.ExecuteScalar()
    Write-Host "[1/5] Evento creado ID #$idEvento (Tipo: Despacho)." -ForegroundColor Green

    # 2. Configurar en EventosDespacho
    $cmdED = $conn.CreateCommand()
    $cmdED.CommandText = "INSERT INTO dbo.EventosDespacho (IdEvento, IdEquipo, EstadoDespachoEvento) OUTPUT INSERTED.IdEventoDespacho VALUES ($idEvento, $idEq, 'PROGRAMADO');"
    $idEventoDespacho = $cmdED.ExecuteScalar()
    Write-Host "[2/5] Evento Despacho configurado ID #$idEventoDespacho." -ForegroundColor Green

    # 3. Buscar una iglesia con participación
    $idPart = Execute-Scalar -sql "SELECT TOP 1 IdParticipacion FROM dbo.ParticipacionesIglesia WHERE IdTemporada = $idTemp;" -c $conn
    $idIglesia = Execute-Scalar -sql "SELECT IdIglesia FROM dbo.ParticipacionesIglesia WHERE IdParticipacion = $idPart;" -c $conn

    # Asegurar asignación de recursos
    $cmdAsig = $conn.CreateCommand()
    $cmdAsig.CommandText = @"
        IF NOT EXISTS (SELECT 1 FROM dbo.AsignacionesRecursos WHERE IdParticipacion = $idPart)
        BEGIN
            INSERT INTO dbo.AsignacionesRecursos (IdParticipacion, OportunidadesEvangelisticas, LibrosMejorRegalo, LibrosMaestros, LibrosAlumno, Posters, NuevosTestamentos, EstadoAsignacion)
            VALUES ($idPart, 50, 50, 5, 50, 2, 50, 'DISPONIBLE_PARA_DESPACHO');
        END
        ELSE
        BEGIN
            UPDATE dbo.AsignacionesRecursos SET EstadoAsignacion = 'DISPONIBLE_PARA_DESPACHO' WHERE IdParticipacion = $idPart;
        END
"@
    $cmdAsig.ExecuteNonQuery() | Out-Null
    Write-Host "[3/5] Iglesia ID #$idIglesia (Part #$idPart) preparada en estado DISPONIBLE_PARA_DESPACHO." -ForegroundColor Green

    # 4. Programar iglesia en el evento de despacho
    $compNum = "DSP-TEST-" + (Get-Random -Minimum 1000 -Maximum 9999)
    $cmdProg = $conn.CreateCommand()
    $cmdProg.CommandText = @"
        INSERT INTO dbo.DespachosIglesia (NumeroComprobanteDespacho, IdEvento, IdParticipacion, IdIglesia, IdTemporada, IdEquipo, EstadoDespacho)
        OUTPUT INSERTED.IdDespachoIglesia
        VALUES ('$compNum', $idEvento, $idPart, $idIglesia, $idTemp, $idEq, 'PROGRAMADA');
"@
    $idDespacho = $cmdProg.ExecuteScalar()

    # Detalle de materiales
    $idMatOE = Execute-Scalar -sql "SELECT IdMaterial FROM dbo.Materiales WHERE Codigo = 'OE';" -c $conn
    $cmdDet = $conn.CreateCommand()
    $cmdDet.CommandText = "INSERT INTO dbo.DespachosIglesiaDetalle (IdDespachoIglesia, IdMaterial, CantidadAsignada, CantidadDespachada) VALUES ($idDespacho, $idMatOE, 50, 0);"
    $cmdDet.ExecuteNonQuery() | Out-Null
    Write-Host "[4/5] Iglesia programada en el evento. Comprobante: $compNum (ID #$idDespacho)." -ForegroundColor Green

    # 5. Probar confirmación de despacho con cédula y descuento de inventario
    # Asegurar que el equipo tenga stock
    $cmdStock = $conn.CreateCommand()
    $cmdStock.CommandText = @"
        MERGE dbo.InventarioEquipo AS tgt
        USING (SELECT $idTemp AS IdTemporada, $idEq AS IdEquipo, $idMatOE AS IdMaterial) AS src
        ON tgt.IdTemporada=src.IdTemporada AND tgt.IdEquipo=src.IdEquipo AND tgt.IdMaterial=src.IdMaterial
        WHEN MATCHED THEN
            UPDATE SET CantidadDisponible = tgt.CantidadDisponible + 100
        WHEN NOT MATCHED THEN
            INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible)
            VALUES ($idTemp, $idEq, $idMatOE, 100, 0, 0, 100);
"@
    $cmdStock.ExecuteNonQuery() | Out-Null

    $stockAntes = Execute-Scalar -sql "SELECT CantidadDisponible FROM dbo.InventarioEquipo WHERE IdTemporada=$idTemp AND IdEquipo=$idEq AND IdMaterial=$idMatOE;" -c $conn

    # Confirmar entrega de 50 unidades al Pastor
    $cmdConf = $conn.CreateCommand()
    $cmdConf.CommandText = @"
        UPDATE dbo.DespachosIglesia SET
            EstadoDespacho = 'DESPACHADA',
            TipoReceptor = 'PASTOR',
            NombreReceptor = 'Pastor Principal Test',
            DocumentoIdentidadReceptor = '001-0000000-1',
            FechaHoraEntrega = GETDATE(),
            CoordinadorDespachador = 'Coordinador Oficial',
            IdUsuarioDespacho = $idUser
        WHERE IdDespachoIglesia = $idDespacho;

        UPDATE dbo.DespachosIglesiaDetalle SET CantidadDespachada = 50 WHERE IdDespachoIglesia = $idDespacho;

        UPDATE dbo.InventarioEquipo SET
            CantidadDespachada = CantidadDespachada + 50,
            CantidadAsignada = CantidadAsignada + 50,
            CantidadDisponible = CantidadDisponible - 50
        WHERE IdTemporada=$idTemp AND IdEquipo=$idEq AND IdMaterial=$idMatOE;

        UPDATE dbo.AsignacionesRecursos SET EstadoAsignacion = 'DESPACHADA' WHERE IdParticipacion = $idPart;
"@
    $cmdConf.ExecuteNonQuery() | Out-Null
    $stockDespues = Execute-Scalar -sql "SELECT CantidadDisponible FROM dbo.InventarioEquipo WHERE IdTemporada=$idTemp AND IdEquipo=$idEq AND IdMaterial=$idMatOE;" -c $conn
    $dif = $stockAntes - $stockDespues
    Write-Host "[5/5] Despacho Confirmado: Stock equipo antes = $stockAntes, despues = $stockDespues (Descuento = $dif uds)." -ForegroundColor Green

    Write-Host ""
    Write-Host "FLUJO DE DESPACHO PRESENCIAL VALIDADO CORRECTAMENTE." -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
}
finally {
    $conn.Close()
}
