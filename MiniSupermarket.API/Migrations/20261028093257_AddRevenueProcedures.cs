using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniSupermarket.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetTop5SellingProducts;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetRevenueByDateRange;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetRevenueByCashier;");
        }
    }
}
