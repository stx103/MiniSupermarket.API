using System.Data;
using Microsoft.Data.SqlClient;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using static MiniSupermarket.API.Data.AdoDatabase;

namespace MiniSupermarket.API.Services
{
    public sealed class RevenueReportService
    {
        private readonly AdoDatabase _database;
        public RevenueReportService(AdoDatabase database) => _database = database;

        // [Tiêu chí 34] Báo cáo doanh thu gọi dbo.sp_GetRevenueByCashier hoặc dbo.sp_GetRevenueByDateRange đã tạo trong SQL/migration.
        public async Task<List<CashierRevenueDto>> ByCashierAsync(DateTime? from, DateTime? until, CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            var procedure = from.HasValue ? "dbo.sp_GetRevenueByDateRange" : "dbo.sp_GetRevenueByCashier";
            var fallback = "SELECT u.Username AS CashierUsername, COUNT(*) AS TotalOrders, SUM(o.FinalAmount) AS TotalRevenue, SUM(o.RewardPoints) AS TotalPointsAwarded FROM Orders o INNER JOIN Users u ON u.UserId = o.UserId WHERE o.Status = @Status";
            if (from.HasValue) fallback += " AND o.OrderDate >= @FromDate AND o.OrderDate < @ToDate";
            fallback += " GROUP BY u.Username ORDER BY TotalRevenue DESC, u.Username";
            using var command = Command(lease.Connection, lease.Connection is SqlConnection ? procedure : fallback);
            // [Tiêu chí 34] SQL Server thực thi stored procedure bằng CommandType.StoredProcedure; ngày lọc được truyền qua tham số.
            if (command is SqlCommand) command.CommandType = CommandType.StoredProcedure;
            else Parameter(command, "@Status", "Hoàn tất", DbType.String, 30);
            if (from.HasValue)
            {
                Parameter(command, "@FromDate", from.Value, DbType.DateTime2);
                Parameter(command, "@ToDate", until!.Value, DbType.DateTime2);
            }
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<CashierRevenueDto>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new CashierRevenueDto
                {
                    CashierUsername = reader.GetString(0), TotalOrders = Convert.ToInt32(reader.GetValue(1)),
                    TotalRevenue = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2)),
                    TotalPointsAwarded = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetValue(3))
                });
            return rows;
        }

        // SQL Server GROUP BY CAST(OrderDate AS DATE) chạy trong stored procedure.
        public async Task<List<DailyRevenueDto>> DailyAsync(DateTime from, DateTime until, string period, CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            bool sqlServer = lease.Connection is SqlConnection;
            using var command = Command(lease.Connection, sqlServer ? "dbo.sp_GetDailyRevenue" :
                "SELECT date(OrderDate), COUNT(*), SUM(FinalAmount) FROM Orders WHERE Status = @Status AND OrderDate >= @FromDate AND OrderDate < @ToDate GROUP BY date(OrderDate) ORDER BY date(OrderDate)");
            if (sqlServer) command.CommandType = CommandType.StoredProcedure;
            else Parameter(command, "@Status", "Hoàn tất", DbType.String, 30);
            Parameter(command, "@FromDate", from, DbType.DateTime2);
            Parameter(command, "@ToDate", until, DbType.DateTime2);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var days = new List<DailyRevenueDto>();
            while (await reader.ReadAsync(cancellationToken)) days.Add(new DailyRevenueDto
            {
                PeriodStart = Convert.ToDateTime(reader.GetValue(0)).Date,
                TotalOrders = Convert.ToInt32(reader.GetValue(1)), TotalRevenue = Convert.ToDecimal(reader.GetValue(2))
            });
            // Bổ sung ngày không bán hàng để biểu đồ không che mất các khoảng trống.
            var lookup = days.ToDictionary(x => x.PeriodStart);
            var complete = new List<DailyRevenueDto>();
            for (var day = from.Date; day < until; day = day.AddDays(1))
                complete.Add(lookup.GetValueOrDefault(day) ?? new DailyRevenueDto { PeriodStart = day });
            DateTime Bucket(DateTime day) => period switch
            {
                "month" => new DateTime(day.Year, day.Month, 1),
                "week" => day.AddDays(-((int)day.DayOfWeek + 6) % 7),
                _ => day
            };
            return complete.GroupBy(x => Bucket(x.PeriodStart)).OrderBy(g => g.Key)
                .Select(g => new DailyRevenueDto { PeriodStart = g.Key, TotalOrders = g.Sum(x => x.TotalOrders), TotalRevenue = g.Sum(x => x.TotalRevenue) }).ToList();
        }

        public async Task<List<TopProductDto>> TopProductsAsync(CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            const string fallback = "SELECT p.ProductId, p.ProductName, SUM(d.Quantity) AS TotalQuantity, SUM(d.LineTotal) AS TotalRevenue FROM OrderDetails d INNER JOIN Products p ON p.ProductId = d.ProductId INNER JOIN Orders o ON o.OrderId = d.OrderId WHERE o.Status = @Status GROUP BY p.ProductId, p.ProductName ORDER BY TotalQuantity DESC, p.ProductId LIMIT 5";
            using var command = Command(lease.Connection, lease.Connection is SqlConnection ? "dbo.sp_GetTop5SellingProducts" : fallback);
            if (command is SqlCommand) command.CommandType = CommandType.StoredProcedure;
            else Parameter(command, "@Status", "Hoàn tất", DbType.String, 30);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<TopProductDto>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new TopProductDto
                {
                    ProductId = reader.GetInt32(0), ProductName = reader.GetString(1),
                    TotalQuantity = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                    TotalRevenue = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3))
                });
            return rows;
        }
    }
}
