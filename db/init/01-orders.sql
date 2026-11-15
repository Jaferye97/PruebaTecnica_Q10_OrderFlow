-- OrdersDb: esquema y datos de prueba de Orders.
-- Idempotente: se puede ejecutar en cada `docker compose up` sin duplicar nada.
-- Lo ejecuta el servicio db-init de docker-compose.yml (sqlcmd -I -b -f 65001).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON; -- obligatorio para el índice filtrado de OutboxMessages
SET NOCOUNT ON;
GO

IF DB_ID(N'OrdersDb') IS NULL
    CREATE DATABASE OrdersDb;
GO

USE OrdersDb;
GO

-- ─── Products: copia local del catálogo (solo para validar el SKU) ───
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products
(
    Sku    nvarchar(30)  NOT NULL,
    Nombre nvarchar(100) NOT NULL,
    Stock  int           NOT NULL,
    CONSTRAINT PK_Products PRIMARY KEY (Sku)
);
GO

-- ─── Orders ───
-- PK no clustered: el Guid lo genera el dominio; el orden físico va por CreadoEn
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id             uniqueidentifier NOT NULL,
        NombreCliente  nvarchar(100)    NOT NULL,
        Sku            nvarchar(30)     NOT NULL,
        Cantidad       int              NOT NULL,
        Estado         nvarchar(30)     NOT NULL,
        MotivoRechazo  nvarchar(30)     NULL,
        DetalleRechazo nvarchar(200)    NULL,
        CreadoEn       datetimeoffset   NOT NULL,
        RowVersion     rowversion       NOT NULL, -- concurrencia optimista
        CONSTRAINT PK_Orders PRIMARY KEY NONCLUSTERED (Id)
    );

    -- "Más recientes primero" lee en orden físico
    CREATE CLUSTERED INDEX IX_Orders_CreadoEn ON dbo.Orders (CreadoEn);
END
GO

-- ─── OutboxMessages ───
IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessages
    (
        Id          uniqueidentifier NOT NULL,
        Type        nvarchar(100)    NOT NULL,
        Payload     nvarchar(max)    NOT NULL,
        OcurridoEn  datetimeoffset   NOT NULL,
        ProcesadoEn datetimeoffset   NULL,
        Intentos    int              NOT NULL,
        UltimoError nvarchar(1000)   NULL,
        CONSTRAINT PK_OutboxMessages PRIMARY KEY (Id)
    );

    -- Índice filtrado: el relay solo busca pendientes y el índice solo contiene esas filas
    CREATE INDEX IX_OutboxMessages_Pendientes
        ON dbo.OutboxMessages (OcurridoEn)
        WHERE ProcesadoEn IS NULL;
END
GO

-- ─── Datos de prueba ───
-- ⚠️ Mismos SKU y stock inicial que InventoryDb.Stock (02-inventory.sql)
INSERT INTO dbo.Products (Sku, Nombre, Stock)
SELECT v.Sku, v.Nombre, v.Stock
FROM (VALUES
    (N'SKU-001', N'Teclado mecánico',    100),
    (N'SKU-002', N'Mouse inalámbrico',    10),
    (N'SKU-003', N'Monitor 27 pulgadas',   2),
    (N'SKU-004', N'Audífonos',             0)
) AS v (Sku, Nombre, Stock)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Sku = v.Sku);
GO

-- Pedidos de ejemplo ya resueltos (Confirmed y Rejected). Ids fijos para que el script sea idempotente.
-- No hay ninguno Pending: un Pending necesitaría su OrderCreated en OutboxMessages para que Inventory
-- lo procese; sin él se quedaría Pending para siempre (y el panel nunca bajaría el polling a 15 s).
INSERT INTO dbo.Orders (Id, NombreCliente, Sku, Cantidad, Estado, MotivoRechazo, DetalleRechazo, CreadoEn)
SELECT v.Id, v.NombreCliente, v.Sku, v.Cantidad, v.Estado, v.MotivoRechazo, v.DetalleRechazo,
       DATEADD(MINUTE, -v.MinutosAtras, SYSDATETIMEOFFSET())
FROM (VALUES
    ('0193a000-0000-7000-8000-000000000001', N'Ana Gómez',      N'SKU-001', 2, N'Confirmed', NULL,                 NULL,                                         300),
    ('0193a000-0000-7000-8000-000000000002', N'Carlos Pérez',   N'SKU-002', 1, N'Confirmed', NULL,                 NULL,                                         240),
    ('0193a000-0000-7000-8000-000000000003', N'Laura Martínez', N'SKU-003', 5, N'Rejected',  N'InsufficientStock', N'Stock disponible: 2, solicitado: 5.',     180),
    ('0193a000-0000-7000-8000-000000000004', N'Jorge Ramírez',  N'SKU-004', 1, N'Rejected',  N'InsufficientStock', N'Producto agotado.',                        120),
    ('0193a000-0000-7000-8000-000000000005', N'Sofía Torres',   N'SKU-001', 3, N'Confirmed', NULL,                 NULL,                                          60),
    ('0193a000-0000-7000-8000-000000000006', N'Diego Herrera',  N'SKU-002', 4, N'Confirmed', NULL,                 NULL,                                          10)
) AS v (Id, NombreCliente, Sku, Cantidad, Estado, MotivoRechazo, DetalleRechazo, MinutosAtras)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.Id = v.Id);
GO

PRINT N'OrdersDb lista: esquema y datos de prueba cargados.';
GO
