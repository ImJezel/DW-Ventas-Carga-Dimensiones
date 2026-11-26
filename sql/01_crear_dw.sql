/* DW de ventas · Jezel Pujols 2025-2067 · Electiva 1
   Crea el almacén, el área de staging y las dimensiones (esquema estrella).
   Se ejecuta una vez en SQL Server Express (.\SQLEXPRESS). */
IF DB_ID('DW_Ventas_Jezel') IS NULL CREATE DATABASE DW_Ventas_Jezel;
GO
USE DW_Ventas_Jezel;
GO
IF SCHEMA_ID('dw')  IS NULL EXEC('CREATE SCHEMA dw');
IF SCHEMA_ID('stg') IS NULL EXEC('CREATE SCHEMA stg');
GO
/* ---------- Dimensiones ---------- */
CREATE TABLE dw.DimFecha (
    FechaKey      INT          NOT NULL PRIMARY KEY,   -- yyyymmdd
    Fecha         DATE         NOT NULL UNIQUE,
    Anio          SMALLINT     NOT NULL,
    Trimestre     TINYINT      NOT NULL,
    Mes           TINYINT      NOT NULL,
    NombreMes     VARCHAR(12)  NOT NULL,
    Dia           TINYINT      NOT NULL,
    DiaSemana     TINYINT      NOT NULL,               -- 1 = lunes
    NombreDia     VARCHAR(10)  NOT NULL,
    EsFinDeSemana BIT          NOT NULL
);
CREATE TABLE dw.DimCliente (
    ClienteKey         INT IDENTITY(1,1) PRIMARY KEY,
    IdClienteOrigen    INT           NOT NULL UNIQUE,
    Nombre             VARCHAR(120)  NOT NULL,
    Email              VARCHAR(150)  NULL,
    DominioEmail       VARCHAR(80)   NULL,
    FechaCarga         DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2(0)  NULL
);
CREATE TABLE dw.DimCategoria (
    CategoriaKey INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       VARCHAR(80) NOT NULL UNIQUE
);
CREATE TABLE dw.DimProducto (
    ProductoKey        INT IDENTITY(1,1) PRIMARY KEY,
    IdProductoOrigen   INT           NOT NULL UNIQUE,
    Nombre             VARCHAR(120)  NOT NULL,
    CategoriaKey       INT           NOT NULL REFERENCES dw.DimCategoria (CategoriaKey),
    FechaCarga         DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    FechaActualizacion DATETIME2(0)  NULL
);
CREATE TABLE dw.DimMetodoPago (
    MetodoPagoKey TINYINT     NOT NULL PRIMARY KEY,
    Codigo        VARCHAR(15) NOT NULL UNIQUE,
    Descripcion   VARCHAR(40) NOT NULL
);
/* ---------- Hecho (se carga en la siguiente entrega) ---------- */
CREATE TABLE dw.FactVentas (
    FactVentaKey    BIGINT IDENTITY(1,1) PRIMARY KEY,
    FechaKey        INT     NOT NULL REFERENCES dw.DimFecha (FechaKey),
    ClienteKey      INT     NOT NULL REFERENCES dw.DimCliente (ClienteKey),
    ProductoKey     INT     NOT NULL REFERENCES dw.DimProducto (ProductoKey),
    MetodoPagoKey   TINYINT NOT NULL REFERENCES dw.DimMetodoPago (MetodoPagoKey),
    IdVentaOrigen   INT     NOT NULL,
    Cantidad        INT     NOT NULL,
    PrecioUnitario  DECIMAL(12,2) NOT NULL,
    Descuento       DECIMAL(12,2) NOT NULL,
    Total           AS (Cantidad * PrecioUnitario - Descuento) PERSISTED
);
/* ---------- Staging ---------- */
CREATE TABLE stg.Cliente  (IdCliente INT NOT NULL, Nombre VARCHAR(120) NOT NULL, Email VARCHAR(150) NULL, DominioEmail VARCHAR(80) NULL);
CREATE TABLE stg.Producto (IdProducto INT NOT NULL, Nombre VARCHAR(120) NOT NULL, Categoria VARCHAR(80) NOT NULL);
CREATE TABLE stg.Fecha    (FechaKey INT NOT NULL, Fecha DATE NOT NULL, Anio SMALLINT NOT NULL, Trimestre TINYINT NOT NULL, Mes TINYINT NOT NULL,
                           NombreMes VARCHAR(12) NOT NULL, Dia TINYINT NOT NULL, DiaSemana TINYINT NOT NULL, NombreDia VARCHAR(10) NOT NULL, EsFinDeSemana BIT NOT NULL);
/* ---------- Bitácora de cargas ---------- */
CREATE TABLE dw.BitacoraCarga (
    IdBitacora   INT IDENTITY(1,1) PRIMARY KEY,
    Dimension    VARCHAR(40)  NOT NULL,
    Leidas       INT          NOT NULL,
    Insertadas   INT          NOT NULL,
    Actualizadas INT          NOT NULL,
    Descartadas  INT          NOT NULL,
    Inicio       DATETIME2(3) NOT NULL,
    Fin          DATETIME2(3) NOT NULL,
    Estado       VARCHAR(10)  NOT NULL,
    Mensaje      VARCHAR(400) NULL
);
GO
/* ---------- Miembros desconocidos ---------- */
INSERT INTO dw.DimFecha VALUES (-1, '1900-01-01', 1900, 1, 1, 'Desconocido', 1, 1, 'N/D', 0);
SET IDENTITY_INSERT dw.DimCliente ON;
INSERT INTO dw.DimCliente (ClienteKey, IdClienteOrigen, Nombre, Email, DominioEmail) VALUES (-1, -1, 'Desconocido', NULL, NULL);
SET IDENTITY_INSERT dw.DimCliente OFF;
SET IDENTITY_INSERT dw.DimCategoria ON;
INSERT INTO dw.DimCategoria (CategoriaKey, Nombre) VALUES (-1, 'Sin categoría');
SET IDENTITY_INSERT dw.DimCategoria OFF;
SET IDENTITY_INSERT dw.DimProducto ON;
INSERT INTO dw.DimProducto (ProductoKey, IdProductoOrigen, Nombre, CategoriaKey) VALUES (-1, -1, 'Desconocido', -1);
SET IDENTITY_INSERT dw.DimProducto OFF;
GO
