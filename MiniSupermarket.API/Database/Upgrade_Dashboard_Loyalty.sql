-- Nâng cấp bổ sung, GIỮ dữ liệu hiện có. Chọn đúng database trước khi Execute.
-- Có thể dùng EF: dotnet ef database update (không cần chạy script này lần nữa).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF COL_LENGTH(N'dbo.Customers', N'CreatedAt') IS NULL
    ALTER TABLE dbo.Customers ADD CreatedAt DATETIME2 NULL;
-- Không gán ngày hôm nay cho khách cũ chưa biết ngày đăng ký.
IF COL_LENGTH(N'dbo.Orders', N'RedeemedPoints') IS NULL
    ALTER TABLE dbo.Orders ADD RedeemedPoints INT NOT NULL CONSTRAINT DF_Orders_RedeemedPoints DEFAULT 0;
IF COL_LENGTH(N'dbo.Orders', N'DiscountAmount') IS NULL
    ALTER TABLE dbo.Orders ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_DiscountAmount DEFAULT 0;
IF COL_LENGTH(N'dbo.Orders', N'FinalAmount') IS NULL
    ALTER TABLE dbo.Orders ADD FinalAmount AS (TotalAmount - DiscountAmount) PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Orders_Discount' AND parent_object_id = OBJECT_ID('dbo.Orders'))
    ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Orders_Discount CHECK (DiscountAmount >= 0 AND DiscountAmount <= TotalAmount);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Orders_RedeemedPoints' AND parent_object_id = OBJECT_ID('dbo.Orders'))
    ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Orders_RedeemedPoints CHECK (RedeemedPoints >= 0);
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetDailyRevenue @FromDate DATETIME2, @ToDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    -- @ToDate là mốc loại trừ; dùng OrderDate đúng tên cột thực tế.
    SELECT CAST(OrderDate AS DATE) AS PeriodStart, COUNT(*) AS TotalOrders,
        SUM(FinalAmount) AS TotalRevenue
    FROM dbo.Orders
    WHERE Status = N'Hoàn tất' AND OrderDate >= @FromDate AND OrderDate < @ToDate
    GROUP BY CAST(OrderDate AS DATE)
    ORDER BY PeriodStart;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByCashier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.FinalAmount) AS TotalRevenue, SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất'
    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByDateRange @FromDate DATETIME2, @ToDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
        SUM(o.FinalAmount) AS TotalRevenue, SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
    WHERE o.Status = N'Hoàn tất' AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate
    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
END;
GO
