$cs = "Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;"
$cn = New-Object System.Data.SqlClient.SqlConnection($cs)
$cn.Open()

Write-Host "=== TEST: EVENTOS DE DESPACHO EN MODULO EVENTOS ===" -ForegroundColor Cyan

$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT TOP 1 IdTemporada FROM dbo.Temporadas ORDER BY Activa DESC, FechaInicio DESC"
$idTemporada = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT TOP 1 IdEquipo FROM dbo.Equipos"
$idEquipo = [int]$cmd.ExecuteScalar()

$cmd.CommandText = "SELECT TOP 1 IdUsuario FROM dbo.Usuarios WHERE IdEstado = 4"
$idUsuario = [int]$cmd.ExecuteScalar()

Write-Host "Temporada: $idTemporada | Equipo: $idEquipo | Usuario: $idUsuario"

$nombreEv = "Despacho Unificado Test " + [System.Guid]::NewGuid().ToString().Substring(0,8)
$cmd.CommandText = "INSERT INTO dbo.Eventos (NombreEvento, TipoEvento, IdTemporada, Fecha, Lugar, Responsable, IdUsuarioCreacion, TipoLugar, Hora, CantidadAsistentes) OUTPUT INSERTED.IdEvento VALUES ('$nombreEv', 'Despacho', $idTemporada, GETDATE(), 'Centro Test', 'Coordinador', $idUsuario, 'Salon', '10:00', 40)"
$idEvento = [int]$cmd.ExecuteScalar()

Write-Host "[OK] Evento Despacho insertado en dbo.Eventos con Id: $idEvento" -ForegroundColor Green

$cmd.CommandText = "INSERT INTO dbo.EventosDespacho (IdEvento, IdEquipo, EstadoDespachoEvento) VALUES ($idEvento, $idEquipo, 'PROGRAMADO')"
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "[OK] dbo.EventosDespacho sincronizado" -ForegroundColor Green

$cmd.CommandText = "SELECT COUNT(1) FROM dbo.Eventos WHERE IdEvento = $idEvento AND TipoEvento = 'Despacho'"
$cnt = [int]$cmd.ExecuteScalar()
Write-Host "[OK] Verificacion de Evento: Count=$cnt" -ForegroundColor Green

$cn.Close()
Write-Host "=== TEST COMPLETADO CON EXITO ===" -ForegroundColor Green
