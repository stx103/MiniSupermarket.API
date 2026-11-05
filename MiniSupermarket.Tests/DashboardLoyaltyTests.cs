using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.WinForms;
using Xunit;

namespace MiniSupermarket.Tests;

public class DashboardLoyaltyTests
{
    [Fact]
    public async Task DashboardFindsLatestCompletedInvoiceEvenOutsideCurrentWeek()
    {
        using var app = new ApiFactory(); using var client = await app.LoginAsync("admin");
        await app.QueryAsync(db => db.Orders.Where(o => o.OrderId == 20).ExecuteUpdateAsync(s => s
            .SetProperty(o => o.OrderDate, new DateTime(2028, 1, 1)).SetProperty(o => o.Status, "Đang hủy")));
        var summary = await client.GetFromJsonAsync<JsonElement>("/api/reports/dashboard-summary");
        Assert.Equal(new DateTime(2026, 10, 8), summary.GetProperty("latestOrderDate").GetDateTime().Date);
        var range = await client.GetFromJsonAsync<JsonElement>("/api/reports/daily-revenue?fromDate=2026-10-01&toDate=2026-10-31&period=day");
        Assert.Contains(range.EnumerateArray(), row => row.GetProperty("totalRevenue").GetDecimal() > 0);
    }

    [Fact]
    public async Task DashboardReturnsNullLatestDateWhenThereAreNoCompletedInvoices()
    {
        using var app = new ApiFactory(); using var client = await app.LoginAsync("admin");
        await app.QueryAsync(db => db.Orders.ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Đang hủy")));
        var summary = await client.GetFromJsonAsync<JsonElement>("/api/reports/dashboard-summary");
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("latestOrderDate").ValueKind);
        Assert.Equal(0, summary.GetProperty("totalOrders").GetInt32());
    }

    private static object Sale(int points = 50, string? phone = "0901122334", int qty = 5, decimal cash = 20000) => new
    { CustomerPhone = phone, PointsToRedeem = points, CashReceived = cash, Items = new[] { new { ProductId = 1, Quantity = qty } } };

    [Fact]
    public async Task RedemptionAndCancellationRestoreMoneyStockAndPoints()
    {
        using var app = new ApiFactory(); using var cashier = await app.LoginAsync("cashier");
        int before = await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync());
        using var response = await cashier.PostAsJsonAsync("/api/checkout", Sale());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<JsonElement>(); int id = receipt.GetProperty("orderId").GetInt32();
        Assert.Equal(60000m, receipt.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(50000m, receipt.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(10000m, receipt.GetProperty("finalAmount").GetDecimal());
        Assert.Equal(10000m, receipt.GetProperty("change").GetDecimal());
        Assert.Equal(1, receipt.GetProperty("rewardPointsAdded").GetInt32());
        Assert.Equal(before - 50 + 1, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        var stored = await cashier.GetFromJsonAsync<JsonElement>($"/api/orders/{id}");
        Assert.Equal(10000m, stored.GetProperty("finalAmount").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await cashier.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
        Assert.Equal(before, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
        Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
    }

    [Theory]
    [InlineData(-1, "0901122334", 5, 20000, HttpStatusCode.BadRequest)]
    [InlineData(50, null, 5, 20000, HttpStatusCode.BadRequest)]
    [InlineData(61, "0901122334", 5, 20000, HttpStatusCode.BadRequest)]
    [InlineData(50, "0901122334", 5, 9999, HttpStatusCode.BadRequest)]
    [InlineData(151, "0901122334", 20, 100000, HttpStatusCode.Conflict)]
    public async Task InvalidRedemptionDoesNotLeavePartialData(int points, string? phone, int qty, decimal cash, HttpStatusCode status)
    {
        using var app = new ApiFactory(); using var cashier = await app.LoginAsync("cashier");
        var before = await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync());
        using var response = await cashier.PostAsJsonAsync("/api/checkout", Sale(points, phone, qty, cash));
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
        Assert.Equal(20, await app.QueryAsync(db => db.OrderDetails.CountAsync()));
        Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
        Assert.Equal(before, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
    }

    [Fact]
    public async Task StockFailureAfterFirstLineAlsoRollsBackRedemption()
    {
        using var app = new ApiFactory(); using var client = await app.LoginAsync("cashier");
        var before = await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync());
        using var result = await client.PostAsJsonAsync("/api/checkout", new { CustomerId = 1, PointsToRedeem = 50,
            Items = new[] { new { ProductId = 1, Quantity = 5 }, new { ProductId = 2, Quantity = 999999 } } });
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal(before, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
        Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
    }

    [Fact]
    public async Task DashboardCountsTodayAndUsesNetRevenue()
    {
        using var app = new ApiFactory(); using var admin = await app.LoginAsync("admin");
        await app.QueryAsync(db => db.Products.Where(p => p.ProductId <= 2).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, 10)));
        var first = await admin.GetFromJsonAsync<JsonElement>("/api/reports/dashboard-summary");
        Assert.Equal(0, first.GetProperty("newCustomers").GetInt32());
        using var customer = await admin.PostAsJsonAsync("/api/customers", new { CustomerName = "Khách kiểm thử", PhoneNumber = "0999988877", Address = "12, Nguyễn Trãi, Phường Bến Thành, Thành phố Hồ Chí Minh", RewardPoints = 0, MembershipRank = "Chuẩn" });
        Assert.Equal(HttpStatusCode.Created, customer.StatusCode);
        using var sale = await admin.PostAsJsonAsync("/api/checkout", Sale()); sale.EnsureSuccessStatusCode();
        var last = await admin.GetFromJsonAsync<JsonElement>("/api/reports/dashboard-summary");
        Assert.Equal(first.GetProperty("todayRevenue").GetDecimal() + 10000, last.GetProperty("todayRevenue").GetDecimal());
        Assert.Equal(first.GetProperty("todayOrders").GetInt32() + 1, last.GetProperty("todayOrders").GetInt32());
        Assert.Equal(first.GetProperty("totalOrders").GetInt32() + 1, last.GetProperty("totalOrders").GetInt32());
        Assert.Equal(1, last.GetProperty("newCustomers").GetInt32());
        Assert.True(last.GetProperty("lowStockProducts").GetInt32() >= 2);
        var invoices = await admin.GetFromJsonAsync<JsonElement>($"/api/reports/orders?fromDate={DateTime.Today:yyyy-MM-dd}&toDate={DateTime.Today:yyyy-MM-dd}");
        Assert.Contains(invoices.EnumerateArray(), o => o.GetProperty("discountAmount").GetDecimal() == 50000);
    }

    [Theory]
    [InlineData("day", 7)] [InlineData("week", 1)] [InlineData("month", 1)]
    public async Task RevenuePeriodsIncludeEmptyDaysAndAgreeWithNetInvoices(string period, int count)
    {
        using var app = new ApiFactory(); using var admin = await app.LoginAsync("admin");
        var rows = await admin.GetFromJsonAsync<JsonElement>($"/api/reports/daily-revenue?fromDate=2026-10-05&toDate=2026-10-11&period={period}");
        Assert.Equal(count, rows.GetArrayLength());
        Assert.Equal(20, rows.EnumerateArray().Sum(x => x.GetProperty("totalOrders").GetInt32()));
        Assert.Equal(await app.QueryAsync(async db => (await db.Orders.Select(o => o.FinalAmount).ToListAsync()).Sum()), rows.EnumerateArray().Sum(x => x.GetProperty("totalRevenue").GetDecimal()));
    }

    [Theory]
    [InlineData("/api/reports/daily-revenue?fromDate=2026-11-05&toDate=2026-10-05")]
    [InlineData("/api/reports/daily-revenue?fromDate=2026-10-05&toDate=2026-10-11&period=year")]
    [InlineData("/api/reports/orders?fromDate=2026-11-05&toDate=2026-10-05")]
    public async Task InvalidFiltersReturn400(string route)
    { using var app = new ApiFactory(); using var admin = await app.LoginAsync("admin"); Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync(route)).StatusCode); }

    [Theory]
    [InlineData("cashier", "dashboard-summary", HttpStatusCode.Forbidden)]
    [InlineData("warehouse01", "dashboard-summary", HttpStatusCode.Forbidden)]
    [InlineData("warehouse01", "top-selling", HttpStatusCode.OK)]
    [InlineData("cashier", "top-selling", HttpStatusCode.Forbidden)]
    [InlineData("cashier", "orders?fromDate=2026-10-08&toDate=2026-10-08", HttpStatusCode.Forbidden)]
    [InlineData("warehouse01", "daily-revenue?fromDate=2026-10-08&toDate=2026-10-08", HttpStatusCode.Forbidden)]
    public async Task ReportPermissionsStayRestricted(string user, string route, HttpStatusCode status)
    { using var app = new ApiFactory(); using var client = await app.LoginAsync(user); Assert.Equal(status, (await client.GetAsync("/api/reports/" + route)).StatusCode); }

    [Fact]
    public void CsvPreservesVietnameseQuotesAndNumericPrecision()
    {
        var csv = CsvExport.Build(new[] { "Tên", "Giá" }, new[] { new object?[] { "Sữa, \"Minh Khang\"\n180ml", 12345.67m }, new object?[] { "=1+1", -5m } });
        Assert.Contains("\"Sữa, \"\"Minh Khang\"\"\n180ml\",\"12345.67\"", csv);
        Assert.Contains("\"'=1+1\",\"-5\"", csv);
    }

    [Fact]
    public async Task WarehouseBestSellersDoesNotExposeFinancialTotals()
    {
        using var app = new ApiFactory(); using var warehouse = await app.LoginAsync("warehouse01");
        var rows = await warehouse.GetFromJsonAsync<JsonElement>("/api/reports/top-selling");
        Assert.Equal(5, rows.GetArrayLength());
        Assert.All(rows.EnumerateArray(), row => Assert.False(row.TryGetProperty("totalRevenue", out _)));
    }
}
