using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniSupermarket.API.Migrations
{
    /// <inheritdoc />
    public partial class DashboardLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RedeemedPoints",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "Orders",
                type: "decimal(19,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] - [DiscountAmount]",
                stored: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Discount",
                table: "Orders",
                sql: "DiscountAmount >= 0 AND DiscountAmount <= TotalAmount");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_RedeemedPoints",
                table: "Orders",
                sql: "RedeemedPoints >= 0");
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetDailyRevenue @FromDate DATETIME2, @ToDate DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- @ToDate là mốc loại trừ; dùng OrderDate đúng tên cột thực tế.
                    SELECT CAST(OrderDate AS DATE) AS PeriodStart, COUNT(*) AS TotalOrders,
                        SUM(FinalAmount) AS TotalRevenue
                    FROM dbo.Orders
                    WHERE Status = N''Hoàn tất'' AND OrderDate >= @FromDate AND OrderDate < @ToDate
                    GROUP BY CAST(OrderDate AS DATE)
                    ORDER BY PeriodStart;
                END;');
                """);
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByCashier
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
                        SUM(o.FinalAmount) AS TotalRevenue, SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
                    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
                    WHERE o.Status = N''Hoàn tất''
                    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
                END;');
                """);
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByDateRange @FromDate DATETIME2, @ToDate DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
                        SUM(o.FinalAmount) AS TotalRevenue, SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
                    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
                    WHERE o.Status = N''Hoàn tất'' AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate
                    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
                END;');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Discount",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_RedeemedPoints",
                table: "Orders");

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetDailyRevenue;");
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByCashier
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
                        SUM(o.TotalAmount) AS TotalRevenue,
                        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
                    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
                    WHERE o.Status = N''Hoàn tất''
                    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
                END;');
                """);
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetRevenueByDateRange
                    @FromDate DATETIME2, @ToDate DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders,
                        SUM(o.TotalAmount) AS TotalRevenue,
                        SUM(CONVERT(BIGINT, o.RewardPoints)) AS TotalPointsAwarded
                    FROM dbo.Orders o INNER JOIN dbo.Users u ON u.UserId = o.UserId
                    WHERE o.Status = N''Hoàn tất'' AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate
                    GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username;
                END;');
                """);
            migrationBuilder.Sql("""
                EXEC(N'CREATE OR ALTER PROCEDURE dbo.sp_GetTop5SellingProducts
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT TOP (5) p.ProductId, p.ProductName,
                        SUM(CONVERT(BIGINT, d.Quantity)) AS TotalQuantity, SUM(d.LineTotal) AS TotalRevenue
                    FROM dbo.OrderDetails d INNER JOIN dbo.Products p ON p.ProductId = d.ProductId
                    INNER JOIN dbo.Orders o ON o.OrderId = d.OrderId
                    WHERE o.Status = N''Hoàn tất''
                    GROUP BY p.ProductId, p.ProductName ORDER BY TotalQuantity DESC, p.ProductId;
                END;');
                """);

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RedeemedPoints",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Customers");
        }
    }
}
