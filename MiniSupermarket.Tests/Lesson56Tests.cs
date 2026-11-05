using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MiniSupermarket.Tests
{
    public class Lesson56Tests
    {
        [Theory]
        [InlineData(1, 5, 1, 5)]
        [InlineData(4, 5, 4, 5)]
        [InlineData(int.MaxValue, 5, 4, 5)]
        [InlineData(-10, 0, 1, 5)]
        [InlineData(1, 51, 1, 5)]
        [InlineData(1, 50, 1, 20)]
        public async Task ProductsPageOnServerAndClampInvalidOrStalePages(int page, int size, int actualPage, int count)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var result = await client.GetFromJsonAsync<JsonElement>($"/api/products/paged?pageNumber={page}&pageSize={size}");
            Assert.Equal(actualPage, result.GetProperty("currentPage").GetInt32());
            Assert.Equal(20, result.GetProperty("totalItems").GetInt32());
            Assert.Equal(count, result.GetProperty("items").GetArrayLength());
            foreach (var product in result.GetProperty("items").EnumerateArray()) Assert.NotEmpty(product.GetProperty("categoryName").GetString()!);
        }

        [Fact]
        public async Task ProductPagingCombinesSearchCategoryAndStableOrdering()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var result = await client.GetFromJsonAsync<JsonElement>("/api/products/paged?categoryId=3&pageSize=2&pageNumber=2");
            Assert.Equal(4, result.GetProperty("totalItems").GetInt32());
            Assert.Equal(2, result.GetProperty("totalPages").GetInt32());
            Assert.Equal(new[] { 12, 13 }, result.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("productId").GetInt32()));
            var filtered = await client.GetFromJsonAsync<JsonElement>("/api/products/paged?categoryId=3&keyword=8936028940115");
            Assert.Equal(1, filtered.GetProperty("totalItems").GetInt32());
            using var empty = await client.GetAsync("/api/products/paged?keyword=NO-MATCH&pageNumber=200");
            Assert.Equal(HttpStatusCode.NotFound, empty.StatusCode);
            Assert.Contains("Không tìm thấy sản phẩm", await empty.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CustomersPageByRewardDescendingAndSearchRecalculatesTotals()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("admin01");
            var result = await client.GetFromJsonAsync<JsonElement>("/api/customers/paged?pageNumber=1&pageSize=5");
            Assert.Equal(20, result.GetProperty("totalItems").GetInt32());
            var points = result.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("rewardPoints").GetInt32()).ToArray();
            Assert.Equal(points.OrderByDescending(p => p), points);
            Assert.Equal(410, points[0]);
            var one = await client.GetFromJsonAsync<JsonElement>("/api/customers/paged?keyword=0901122334&pageNumber=10");
            Assert.Equal(1, one.GetProperty("totalItems").GetInt32());
            Assert.Equal(1, one.GetProperty("currentPage").GetInt32());
        }

        [Fact]
        public async Task AdoBarcodeHandlesNotFoundAndTreatsInjectionAsData()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            var product = await cashier.GetFromJsonAsync<JsonElement>("/api/ado-products/barcode/8935001710011");
            Assert.Equal(12000m, product.GetProperty("price").GetDecimal());
            Assert.Equal(HttpStatusCode.NotFound, (await cashier.GetAsync("/api/ado-products/barcode/999999999999")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await cashier.GetAsync("/api/ado-products/barcode/" + Uri.EscapeDataString("' OR '1'='1"))).StatusCode);
            Assert.Equal(20, await app.QueryAsync(db => db.Products.CountAsync()));
        }

        [Fact]
        public async Task AdoScalarDataReaderDataTableAndUpdateUseTheSameDatabase()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var count = await client.GetFromJsonAsync<JsonElement>("/api/ado-products/count");
            Assert.Equal(20, count.GetProperty("total").GetInt32());
            var cache = await client.GetFromJsonAsync<JsonElement>("/api/ado-products/disconnected-cache");
            Assert.Equal(20, cache.GetProperty("totalLoaded").GetInt32());
            Assert.Equal(20, cache.GetProperty("data").GetArrayLength());
            var categories = await client.GetFromJsonAsync<JsonElement>("/api/ado-products/category-counts");
            Assert.Equal(20, categories.EnumerateArray().Sum(c => c.GetProperty("total").GetInt32()));
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/ado-products/1/stock", new { StockQuantity = 10 })).StatusCode);
            var low = await client.GetFromJsonAsync<JsonElement>("/api/ado-products/filter-stock?minStock=10");
            Assert.Contains(low.EnumerateArray(), p => p.GetProperty("productId").GetInt32() == 1);
            Assert.All(low.EnumerateArray(), p => Assert.True(p.GetProperty("stockQuantity").GetInt32() <= 10));
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/ado-products/1/stock", new { StockQuantity = -1 })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/ado-products/1/stock", new { })).StatusCode);
            Assert.Equal(10, await Stock(app));
        }

        [Theory]
        [InlineData("/api/products/paged")]
        [InlineData("/api/ado-products/disconnected-cache")]
        [InlineData("/api/ado-products/category-counts")]
        [InlineData("/api/ado-products/filter-stock")]
        [InlineData("/api/ado-products/count")]
        [InlineData("/api/reports/revenue-by-cashier")]
        [InlineData("/api/reports/revenue-by-date-range?fromDate=2026-10-08&toDate=2026-10-08")]
        [InlineData("/api/reports/top-selling")]
        public async Task NewEndpointsRetainCashierRestrictions(string endpoint)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("cashier01");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(endpoint)).StatusCode);
        }

        [Fact]
        public async Task AdoCheckoutAndCancelRestoreStockPointsAndInvoiceRowsExactlyOnce()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            using var sale = await cashier.PostAsJsonAsync("/api/checkout", Request());
            Assert.Equal(HttpStatusCode.OK, sale.StatusCode);
            var receipt = await sale.Content.ReadFromJsonAsync<JsonElement>();
            int id = receipt.GetProperty("orderId").GetInt32();
            Assert.StartsWith("HD-", receipt.GetProperty("orderCode").GetString());
            Assert.Equal(1, receipt.GetProperty("rewardPointsAdded").GetInt32());
            var detail = await cashier.GetFromJsonAsync<JsonElement>($"/api/orders/{id}");
            Assert.Equal(1, detail.GetProperty("items").GetArrayLength());
            Assert.Equal(HttpStatusCode.OK, (await cashier.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
            Assert.Equal(120, await Stock(app));
            Assert.Equal(150, await Points(app));
            Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.OrderDetails.CountAsync()));
            Assert.Equal(HttpStatusCode.NotFound, (await cashier.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
            Assert.Equal(120, await Stock(app));
        }

        [Fact]
        public async Task LaterStockFailureRollsBackHeaderFirstLineAndPoints()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            using var response = await cashier.PostAsJsonAsync("/api/checkout", new
            {
                CustomerId = 1,
                Items = new[] { new { ProductId = 1, Quantity = 1 }, new { ProductId = 2, Quantity = 999 } }
            });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(120, await Stock(app));
            Assert.Equal(150, await Points(app));
            Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.OrderDetails.CountAsync()));
        }

        [Fact]
        public async Task CashierCannotCancelOtherCashiersSaleButAdminCan()
        {
            using var app = new ApiFactory();
            using var first = await app.LoginAsync("cashier01");
            using var second = await app.LoginAsync("cashier02");
            using var admin = await app.LoginAsync("admin01");
            var receipt = await (await first.PostAsJsonAsync("/api/checkout", Request())).Content.ReadFromJsonAsync<JsonElement>();
            int id = receipt.GetProperty("orderId").GetInt32();
            Assert.Equal(HttpStatusCode.Forbidden, (await second.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await second.GetAsync($"/api/orders/{id}")).StatusCode);
            Assert.Equal(119, await Stock(app));
            Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
            Assert.Equal(120, await Stock(app));
        }

        [Fact]
        public async Task FailedPointRefundRollsBackRestockAndKeepsTheSale()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            var receipt = await (await cashier.PostAsJsonAsync("/api/checkout", Request())).Content.ReadFromJsonAsync<JsonElement>();
            int id = receipt.GetProperty("orderId").GetInt32();
            await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).ExecuteUpdateAsync(s => s.SetProperty(c => c.RewardPoints, 0)));
            Assert.Equal(HttpStatusCode.Conflict, (await cashier.DeleteAsync($"/api/checkout/cancel/{id}")).StatusCode);
            Assert.Equal(119, await Stock(app));
            Assert.True(await app.QueryAsync(db => db.Orders.AnyAsync(o => o.OrderId == id && o.Status == "Hoàn tất")));
        }

        [Fact]
        public async Task CheckoutDoesNotTrustClientPriceOrCashierIdentity()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            var badIdentity = new { CashierUsername = "admin01", Items = new[] { new { ProductId = 1, Quantity = 1 } } };
            Assert.Equal(HttpStatusCode.Forbidden, (await cashier.PostAsJsonAsync("/api/checkout", badIdentity)).StatusCode);
            var badPrice = new { Items = new[] { new { ProductId = 1, Quantity = 1, UnitPrice = 1 } } };
            Assert.Equal(HttpStatusCode.Conflict, (await cashier.PostAsJsonAsync("/api/checkout", badPrice)).StatusCode);
            Assert.Equal(120, await Stock(app));
        }

        [Fact]
        public async Task RevenueGroupingDateBoundsAndTopFiveMatchInvoiceData()
        {
            using var app = new ApiFactory();
            using var admin = await app.LoginAsync("admin01");
            var grouped = await admin.GetFromJsonAsync<JsonElement>("/api/reports/revenue-by-cashier");
            Assert.Equal(443000m, grouped.GetProperty("grandTotalRevenue").GetDecimal());
            Assert.Equal(20, grouped.GetProperty("data").EnumerateArray().Sum(r => r.GetProperty("totalOrders").GetInt32()));
            var range = await admin.GetFromJsonAsync<JsonElement>("/api/reports/revenue-by-date-range?fromDate=2026-10-08&toDate=2026-10-08");
            Assert.Equal(443000m, range.GetProperty("grandTotalRevenue").GetDecimal());
            var empty = await admin.GetFromJsonAsync<JsonElement>("/api/reports/revenue-by-date-range?fromDate=2020-01-01&toDate=2020-01-01");
            Assert.Equal(0m, empty.GetProperty("grandTotalRevenue").GetDecimal());
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reports/revenue-by-date-range?fromDate=2026-10-09&toDate=2026-10-08")).StatusCode);
            var top = await admin.GetFromJsonAsync<JsonElement>("/api/reports/top-selling");
            Assert.Equal(5, top.GetArrayLength());
            Assert.Equal(1, top[0].GetProperty("productId").GetInt32());
        }

        private static object Request() => new { CustomerId = 1, Items = new[] { new { ProductId = 1, Quantity = 1 } } };
        private static Task<int> Stock(ApiFactory app) => app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync());
        private static Task<int> Points(ApiFactory app) => app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync());
    }
}
