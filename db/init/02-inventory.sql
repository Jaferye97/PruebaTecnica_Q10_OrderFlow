-- InventoryDb: esquema y datos de prueba de Inventory.
-- Idempotente: se puede ejecutar en cada `docker compose up` sin duplicar nada.
-- Lo ejecuta el servicio db-init de docker-compose.yml (sqlcmd -I -b -f 65001).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF DB_ID(N'InventoryDb') IS NULL
    CREATE DATABASE InventoryDb;
GO

USE InventoryDb;
GO

-- ─── Stock ───
IF OBJECT_ID(N'dbo.Stock', N'U') IS NULL
CREATE TABLE dbo.Stock
(
    Sku        nvarchar(30) NOT NULL,
    Disponible int          NOT NULL,
    RowVersion rowversion   NOT NULL, -- concurrencia optimista: dos reservas del mismo SKU no se pisan
    CONSTRAINT PK_Stock PRIMARY KEY (Sku),
    -- Última defensa: aunque falle el dominio, la BD no acepta stock negativo
    CONSTRAINT CK_Stock_Disponible CHECK (Disponible >= 0)
);
GO

-- ─── ProcessedEvents: idempotencia del consumidor ───
IF OBJECT_ID(N'dbo.ProcessedEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessedEvents
    (
        EventId       uniqueidentifier NOT NULL, -- un mismo mensaje no se registra dos veces
        OrderId       uniqueidentifier NOT NULL,
        Resultado     nvarchar(20)     NOT NULL,
        Motivo        nvarchar(30)     NULL,
        Detalle       nvarchar(200)    NULL,
        ResultEventId uniqueidentifier NOT NULL,
        ProcesadoEn   datetimeoffset   NOT NULL,
        CONSTRAINT PK_ProcessedEvents PRIMARY KEY (EventId)
    );

    -- Dos eventos distintos del mismo pedido no reservan dos veces
    CREATE UNIQUE INDEX IX_ProcessedEvents_OrderId ON dbo.ProcessedEvents (OrderId);
END
GO

-- ─── Datos de prueba ───
-- ⚠️ Mismos SKU y stock inicial que OrdersDb.Products (01-orders.sql)
INSERT INTO dbo.Stock (Sku, Disponible)
SELECT v.Sku, v.Disponible
FROM (VALUES
    (N'SKU-001', 100),
    (N'SKU-002',  10),
    (N'SKU-003',   2),
    (N'SKU-004',   0)
) AS v (Sku, Disponible)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Stock s WHERE s.Sku = v.Sku);
GO
