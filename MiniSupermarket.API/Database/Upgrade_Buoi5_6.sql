-- Nang cap Buoi 5/6, chay sau Upgrade_Buoi4.sql. Khong xoa bang hay du lieu.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
USE MinhKhangMiniSupermarketDb;
GO

IF COL_LENGTH(N'dbo.Orders', N'OrderCode') IS NULL
    ALTER TABLE dbo.Orders ADD OrderCode VARCHAR(30) NOT NULL
        CONSTRAINT DF_Orders_OrderCode DEFAULT '';
IF COL_LENGTH(N'dbo.Orders', N'RewardPoints') IS NULL
    ALTER TABLE dbo.Orders ADD RewardPoints INT NOT NULL
        CONSTRAINT DF_Orders_RewardPoints DEFAULT 0;
GO

-- Hoa don cua ban truoc Buoi 5 dung quy tac 1 diem/1.000d.
-- Chi danh dau hoa don cu chua co ma; chay lai khong ghi de diem cua hoa don moi.
UPDATE dbo.Orders
SET OrderCode = 'HD-LEGACY-' + CONVERT(VARCHAR(20), OrderId),
    RewardPoints = CASE WHEN CustomerId IS NULL THEN 0
        ELSE CONVERT(INT, CASE WHEN FLOOR(TotalAmount / 1000) > 2147483647
            THEN 2147483647 ELSE FLOOR(TotalAmount / 1000) END) END
WHERE OrderCode = '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'IX_Orders_OrderCode')
    CREATE UNIQUE INDEX IX_Orders_OrderCode ON dbo.Orders(OrderCode) WHERE OrderCode <> '';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'IX_Orders_OrderDate')
    CREATE INDEX IX_Orders_OrderDate ON dbo.Orders(OrderDate);
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByCashier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.TotalAmount) AS TotalRevenue,
        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o
    INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất'
    GROUP BY u.Username
    ORDER BY TotalRevenue DESC, u.Username;
END;
GO

-- @ToDate la moc ket thuc khong bao gom (ngay ke tiep neu xem tron ngay).
CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByDateRange
    @FromDate DATETIME2,
    @ToDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.TotalAmount) AS TotalRevenue,
        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o
    INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất' AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate
    GROUP BY u.Username
    ORDER BY TotalRevenue DESC, u.Username;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTop5SellingProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (5) p.ProductId, p.ProductName,
        SUM(CONVERT(BIGINT, d.Quantity)) AS TotalQuantity,
        SUM(d.LineTotal) AS TotalRevenue
    FROM dbo.OrderDetails d
    INNER JOIN dbo.Products p ON p.ProductId = d.ProductId
    INNER JOIN dbo.Orders o ON o.OrderId = d.OrderId
    WHERE o.Status = N'Hoàn tất'
    GROUP BY p.ProductId, p.ProductName
    ORDER BY TotalQuantity DESC, p.ProductId;
END;
GO
