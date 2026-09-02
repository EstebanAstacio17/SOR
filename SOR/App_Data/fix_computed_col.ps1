$conn = New-Object System.Data.SqlClient.SqlConnection("Server=ASTACIO\SQLEXPRESS;Database=DB_SOR;Trusted_Connection=True;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RecepcionesContenedorDetalle') AND name = 'CantidadTotalUnidades')
BEGIN
    ALTER TABLE dbo.RecepcionesContenedorDetalle DROP COLUMN CantidadTotalUnidades;
END
ALTER TABLE dbo.RecepcionesContenedorDetalle ADD CantidadTotalUnidades AS (CantidadEmpaques * UnidadesPorEmpaque) PERSISTED;
"@
$cmd.ExecuteNonQuery()
$conn.Close()
Write-Host "Columna calculada persistida configurada."
