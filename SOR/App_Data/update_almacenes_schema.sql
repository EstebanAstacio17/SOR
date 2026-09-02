IF COL_LENGTH('dbo.Almacenes', 'IdUsuarioResponsable') IS NULL
BEGIN
    ALTER TABLE dbo.Almacenes ADD IdUsuarioResponsable INT NULL;
END;

IF COL_LENGTH('dbo.Almacenes', 'EsCentral') IS NULL
BEGIN
    ALTER TABLE dbo.Almacenes ADD EsCentral BIT NOT NULL CONSTRAINT DF_Almacenes_EsCentral DEFAULT 1;
END;

IF OBJECT_ID('dbo.AlmacenesEquipos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlmacenesEquipos (
        IdAlmacen INT NOT NULL FOREIGN KEY REFERENCES dbo.Almacenes(IdAlmacen) ON DELETE CASCADE,
        IdEquipo  INT NOT NULL FOREIGN KEY REFERENCES dbo.Equipos(IdEquipo) ON DELETE CASCADE,
        CONSTRAINT PK_AlmacenesEquipos PRIMARY KEY (IdAlmacen, IdEquipo)
    );
END;
