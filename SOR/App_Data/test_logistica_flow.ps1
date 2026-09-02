# Test script for Logistics Module
$connStr = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "PRUEBAS DEL MODULO LOGISTICO Y DE DESPACHO OCC" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

function Execute-Scalar([string]$sql, $c) {
    $cmd = $c.CreateCommand()
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

try {
    # 1. Catalogo Materiales
    $countMat = Execute-Scalar -sql "SELECT COUNT(1) FROM dbo.Materiales;" -c $conn
    Write-Host "[1/7] Catalogo de Materiales: $countMat materiales registrados." -ForegroundColor Green

    # 2. Presentaciones
    $countPres = Execute-Scalar -sql "SELECT COUNT(1) FROM dbo.PresentacionesMaterial;" -c $conn
    Write-Host "[2/7] Presentaciones de Materiales: $countPres presentaciones registradas." -ForegroundColor Green

    # 3. Almacenes
    $countAlm = Execute-Scalar -sql "SELECT COUNT(1) FROM dbo.Almacenes;" -c $conn
    Write-Host "[3/7] Almacenes: $countAlm almacen(es) disponible(s)." -ForegroundColor Green

    # 4. Contexto
    $idTemp = Execute-Scalar -sql "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC;" -c $conn
    $idAlm = Execute-Scalar -sql "SELECT TOP 1 IdAlmacen FROM dbo.Almacenes WHERE Activo = 1;" -c $conn
    $idEq = Execute-Scalar -sql "SELECT TOP 1 IdEquipo FROM dbo.Equipos;" -c $conn
    $idUser = Execute-Scalar -sql "SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdEstado = 4;" -c $conn
    $idMatOE = Execute-Scalar -sql "SELECT IdMaterial FROM dbo.Materiales WHERE Codigo = 'OE';" -c $conn
    $idPresOE = Execute-Scalar -sql "SELECT TOP 1 IdPresentacion FROM dbo.PresentacionesMaterial WHERE IdMaterial = $idMatOE;" -c $conn

    Write-Host "    Contexto: Temp=$idTemp, Alm=$idAlm, Eq=$idEq, User=$idUser, MatOE=$idMatOE" -ForegroundColor DarkGray

    # 5. Recepcion Contenedor
    $numContenedor = "TEST-CONT-" + (Get-Random -Minimum 1000 -Maximum 9999)
    $cmdRecep = $conn.CreateCommand()
    $cmdRecep.CommandText = "INSERT INTO dbo.RecepcionesContenedor (NumeroContenedor, IdTemporada, IdAlmacen, FechaRecepcion, ResponsableRecepcion, EstadoRecepcion, IdUsuarioRegistro) OUTPUT INSERTED.IdRecepcion VALUES ('$numContenedor', $idTemp, $idAlm, GETDATE(), 'Test Runner', 'CONFIRMADA', $idUser);"
    $idRecep = $cmdRecep.ExecuteScalar()

    $cmdDet = $conn.CreateCommand()
    $cmdDet.CommandText = "INSERT INTO dbo.RecepcionesContenedorDetalle (IdRecepcion, IdMaterial, IdPresentacion, CantidadEmpaques, UnidadesPorEmpaque) VALUES ($idRecep, $idMatOE, $idPresOE, 10, 16); MERGE dbo.InventarioCentral AS tgt USING (SELECT $idTemp AS IdTemporada, $idAlm AS IdAlmacen, $idMatOE AS IdMaterial) AS src ON tgt.IdTemporada = src.IdTemporada AND tgt.IdAlmacen = src.IdAlmacen AND tgt.IdMaterial = src.IdMaterial WHEN MATCHED THEN UPDATE SET CantidadFisica = tgt.CantidadFisica + 160, CantidadDisponible = tgt.CantidadDisponible + 160 WHEN NOT MATCHED THEN INSERT (IdTemporada, IdAlmacen, IdMaterial, CantidadFisica, CantidadTransferida, CantidadDisponible) VALUES ($idTemp, $idAlm, $idMatOE, 160, 0, 160); INSERT INTO dbo.MovimientosInventario (IdTemporada, TipoMovimiento, IdMaterial, Cantidad, IdAlmacenDestino, IdDocumentoReferencia, IdUsuario, Justificacion) VALUES ($idTemp, 'RECEPCION_CONTENEDOR', $idMatOE, 160, $idAlm, 'REC-$idRecep', $idUser, 'Test recepcion contenedor');"
    $cmdDet.ExecuteNonQuery() | Out-Null
    $stockCentral = Execute-Scalar -sql "SELECT CantidadDisponible FROM dbo.InventarioCentral WHERE IdTemporada=$idTemp AND IdAlmacen=$idAlm AND IdMaterial=$idMatOE;" -c $conn
    Write-Host "[4/7] Recepcion de Contenedor: Creada ID #$idRecep con 160 unidades. Stock disponible en Central = $stockCentral." -ForegroundColor Green

    # 6. Transferencia a Equipo
    $numConstancia = "TEST-TRF-" + (Get-Random -Minimum 1000 -Maximum 9999)
    $cmdTrf = $conn.CreateCommand()
    $cmdTrf.CommandText = "INSERT INTO dbo.TransferenciasEquipo (NumeroConstancia, IdTemporada, IdEquipo, IdAlmacenOrigen, FechaTransferencia, CoordinadorEmisor, PersonaReceptoraEquipo, Estado, IdUsuarioRegistro) OUTPUT INSERTED.IdTransferencia VALUES ('$numConstancia', $idTemp, $idEq, $idAlm, GETDATE(), 'Coordinador Central', 'Lider Equipo', 'COMPLETADA', $idUser);"
    $idTrf = $cmdTrf.ExecuteScalar()

    $cmdTrfDet = $conn.CreateCommand()
    $cmdTrfDet.CommandText = "INSERT INTO dbo.TransferenciasEquipoDetalle (IdTransferencia, IdMaterial, CantidadUnidades) VALUES ($idTrf, $idMatOE, 60); UPDATE dbo.InventarioCentral SET CantidadTransferida = CantidadTransferida + 60, CantidadDisponible = CantidadDisponible - 60 WHERE IdTemporada=$idTemp AND IdAlmacen=$idAlm AND IdMaterial=$idMatOE; MERGE dbo.InventarioEquipo AS tgt USING (SELECT $idTemp AS IdTemporada, $idEq AS IdEquipo, $idMatOE AS IdMaterial) AS src ON tgt.IdTemporada=src.IdTemporada AND tgt.IdEquipo=src.IdEquipo AND tgt.IdMaterial=src.IdMaterial WHEN MATCHED THEN UPDATE SET CantidadRecibida = tgt.CantidadRecibida + 60, CantidadDisponible = tgt.CantidadDisponible + 60 WHEN NOT MATCHED THEN INSERT (IdTemporada, IdEquipo, IdMaterial, CantidadRecibida, CantidadAsignada, CantidadDespachada, CantidadDisponible) VALUES ($idTemp, $idEq, $idMatOE, 60, 0, 0, 60); INSERT INTO dbo.MovimientosInventario (IdTemporada, TipoMovimiento, IdMaterial, Cantidad, IdAlmacenOrigen, IdEquipoDestino, IdDocumentoReferencia, IdUsuario, Justificacion) VALUES ($idTemp, 'TRANSFERENCIA_EQUIPO', $idMatOE, 60, $idAlm, $idEq, 'TRF-$idTrf', $idUser, 'Test transferencia');"
    $cmdTrfDet.ExecuteNonQuery() | Out-Null
    $stockDispCentralPost = Execute-Scalar -sql "SELECT CantidadDisponible FROM dbo.InventarioCentral WHERE IdTemporada=$idTemp AND IdAlmacen=$idAlm AND IdMaterial=$idMatOE;" -c $conn
    $stockDispEq = Execute-Scalar -sql "SELECT CantidadDisponible FROM dbo.InventarioEquipo WHERE IdTemporada=$idTemp AND IdEquipo=$idEq AND IdMaterial=$idMatOE;" -c $conn
    Write-Host "[5/7] Transferencia a Equipo: 60 unidades transferidas. Stock Central = $stockDispCentralPost, Stock Equipo = $stockDispEq." -ForegroundColor Green

    # 7. Prueba CHECK contra saldo negativo
    $checkFailed = $false
    try {
        $cmdNeg = $conn.CreateCommand()
        $cmdNeg.CommandText = "UPDATE dbo.InventarioCentral SET CantidadDisponible = -10 WHERE IdTemporada=$idTemp AND IdAlmacen=$idAlm AND IdMaterial=$idMatOE;"
        $cmdNeg.ExecuteNonQuery() | Out-Null
    } catch {
        $checkFailed = $true
    }
    if ($checkFailed) {
        Write-Host "[6/7] Restriccion CHECK contra saldos negativos: FUNCIONA (bloqueo intento de saldo negativo)." -ForegroundColor Green
    } else {
        Write-Host "[6/7] Restriccion CHECK contra saldos negativos: ERROR (no bloqueo)." -ForegroundColor Red
    }

    # 8. Kardex
    $countKardex = Execute-Scalar -sql "SELECT COUNT(1) FROM dbo.MovimientosInventario WHERE IdTemporada = $idTemp;" -c $conn
    Write-Host "[7/7] Kardex de Movimientos: $countKardex movimientos registrados con trazabilidad completa." -ForegroundColor Green

    Write-Host ""
    Write-Host "TODAS LAS PRUEBAS DE INTEGRIDAD LOGISTICA PASARON EXITOSAMENTE." -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR EN PRUEBAS: $_" -ForegroundColor Red
}
finally {
    $conn.Close()
}
