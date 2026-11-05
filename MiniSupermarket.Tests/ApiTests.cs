using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniSupermarket.API.Data;
using Xunit;

namespace MiniSupermarket.Tests
{
    public sealed class ApiFactory : WebApplicationFactory<global::Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly string _environment;
        public ApiFactory(string environment = "Testing")
        {
            _environment = environment;
            _connection.Open();
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<SupermarketDbContext>().Database.EnsureCreated();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(SupermarketDbContext)
                    || d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(SupermarketDbContext))).ToList())
                    services.Remove(descriptor);
                services.AddDbContext<SupermarketDbContext>(options => options.UseSqlite(_connection));
            });
        }

        public async Task<HttpClient> LoginAsync(string username)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { Username = username, Password = "123456" });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
            return client;
        }

        public async Task<T> QueryAsync<T>(Func<SupermarketDbContext, Task<T>> query)
        {
            using var scope = Services.CreateScope();
            return await query(scope.ServiceProvider.GetRequiredService<SupermarketDbContext>());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }

    public class ApiTests
    {
        [Fact]
        public async Task SwaggerExposesBearerAuthAndAnonymousApiIsBlocked()
        {
            using var app = new ApiFactory("Development");
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
            var bearer = swagger.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/products")).StatusCode);
        }

        [Theory]
        [InlineData("admin01", "/api/categories", HttpStatusCode.OK)]
        [InlineData("admin01", "/api/customers", HttpStatusCode.OK)]
        [InlineData("admin01", "/api/products", HttpStatusCode.OK)]
        [InlineData("admin01", "/api/users", HttpStatusCode.OK)]
        [InlineData("cashier01", "/api/categories", HttpStatusCode.Forbidden)]
        [InlineData("cashier01", "/api/products", HttpStatusCode.Forbidden)]
        [InlineData("cashier01", "/api/users", HttpStatusCode.Forbidden)]
        [InlineData("cashier01", "/api/reports/quick?date=2026-10-08", HttpStatusCode.Forbidden)]
        [InlineData("cashier01", "/api/customers", HttpStatusCode.OK)]
        [InlineData("cashier01", "/api/customers/phone/0901122334", HttpStatusCode.OK)]
        [InlineData("cashier01", "/api/products/barcode/8935001710011", HttpStatusCode.OK)]
        [InlineData("ware01", "/api/categories", HttpStatusCode.OK)]
        [InlineData("ware01", "/api/products", HttpStatusCode.OK)]
        [InlineData("ware01", "/api/customers", HttpStatusCode.Forbidden)]
        [InlineData("ware01", "/api/users", HttpStatusCode.Forbidden)]
        [InlineData("ware01", "/api/reports/quick?date=2026-10-08", HttpStatusCode.Forbidden)]
        public async Task RoleMatrixIsEnforcedByApi(string user, string endpoint, HttpStatusCode expected)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync(user);
            using var response = await client.GetAsync(endpoint);
            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task CheckoutPersistsOrderDecrementsStockAndAddsPoints()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("cashier01");
            using var response = await client.PostAsJsonAsync("/api/orders/checkout", Sale());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(12000m, result.GetProperty("totalAmount").GetDecimal());
            Assert.Equal(8000m, result.GetProperty("change").GetDecimal());
            Assert.Equal(1, result.GetProperty("rewardPointsAdded").GetInt32());
            Assert.Equal(119, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
            Assert.Equal(151, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
            Assert.Equal(21, await app.QueryAsync(db => db.Orders.CountAsync()));
        }

        [Theory]
        [InlineData(121, 1452000, 1500000, HttpStatusCode.Conflict)]
        [InlineData(1, 12000, 1000, HttpStatusCode.BadRequest)]
        [InlineData(1, 1, 20000, HttpStatusCode.Conflict)]
        [InlineData(0, 0, 20000, HttpStatusCode.BadRequest)]
        public async Task InvalidCheckoutDoesNotChangeStock(int quantity, decimal expectedTotal, decimal cash, HttpStatusCode expected)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("cashier01");
            using var response = await client.PostAsJsonAsync("/api/orders/checkout", new
            {
                CustomerPhone = "0901122334", CashReceived = cash, ExpectedTotalAmount = expectedTotal,
                Items = new[] { new { ProductId = 1, Quantity = quantity } }
            });
            Assert.Equal(expected, response.StatusCode);
            Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
        }

        [Fact]
        public async Task FailureAfterStockUpdateRollsBackTheEntireSale()
        {
            using var app = new ApiFactory();
            await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.RewardPoints, int.MaxValue)));
            using var client = await app.LoginAsync("cashier01");
            using var response = await client.PostAsJsonAsync("/api/orders/checkout", Sale());
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
        }

        [Fact]
        public async Task WarehouseCannotCheckoutAndCashierCannotChangePrices()
        {
            using var app = new ApiFactory();
            using var warehouse = await app.LoginAsync("ware01");
            using var cashier = await app.LoginAsync("cashier01");
            Assert.Equal(HttpStatusCode.Forbidden, (await warehouse.PostAsJsonAsync("/api/orders/checkout", Sale())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cashier.PutAsJsonAsync("/api/products/1", new { Price = 1 })).StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/categories")]
        [InlineData("PUT", "/api/categories/1")]
        [InlineData("DELETE", "/api/categories/1")]
        [InlineData("POST", "/api/products")]
        [InlineData("PUT", "/api/products/1")]
        [InlineData("DELETE", "/api/products/1")]
        [InlineData("POST", "/api/users")]
        [InlineData("PUT", "/api/users/1/password")]
        [InlineData("PUT", "/api/users/1/status")]
        public async Task CashierCannotMutateProtectedResources(string method, string endpoint)
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            using var request = new HttpRequestMessage(new HttpMethod(method), endpoint)
            {
                Content = JsonContent.Create(new
                {
                    CategoryName = "Unauthorized change", Description = "Unauthorized change",
                    Barcode = "UNAUTHORIZED", ProductName = "Unauthorized change", Price = 1,
                    StockQuantity = 99999, CategoryId = 1,
                    Username = "unauthorized", FullName = "Unauthorized user", Password = "changed123",
                    Role = "Admin", IsActive = false
                })
            };
            using var response = await cashier.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(12000m, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.Price).SingleAsync()));
            Assert.Equal(120, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.StockQuantity).SingleAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Users.CountAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Categories.CountAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Products.CountAsync()));
            Assert.Equal(150, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        }

        [Fact]
        public async Task ProductCrudAndFiltersUseDatabaseAndProtectInvoiceHistory()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var payload = new { Barcode = "TEST-NEW", ProductName = "Test product", Price = 15000, StockQuantity = 10, CategoryId = 20 };
            using var created = await client.PostAsJsonAsync("/api/products", payload);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("productId").GetInt32();
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/products", payload)).StatusCode);
            var filtered = await client.GetFromJsonAsync<JsonElement>("/api/products?keyword=TEST-NEW&categoryId=20");
            Assert.Equal(1, filtered.GetArrayLength());
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/products/{id}", payload)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/products/1")).StatusCode);
        }

        [Fact]
        public async Task QuickReportMatchesSeedAndEmptyDates()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("admin01");
            var report = await client.GetFromJsonAsync<JsonElement>("/api/reports/quick?date=2026-10-08");
            Assert.Equal(20, report.GetProperty("totalOrders").GetInt32());
            Assert.Equal(443000m, report.GetProperty("totalRevenue").GetDecimal());
            Assert.Equal(2, report.GetProperty("bestSellerQuantity").GetInt64());
            var empty = await client.GetFromJsonAsync<JsonElement>("/api/reports/quick?date=2020-01-01");
            Assert.Equal(0, empty.GetProperty("totalOrders").GetInt32());
            Assert.Equal(0m, empty.GetProperty("totalRevenue").GetDecimal());
        }

        [Fact]
        public async Task AccountsHidePasswordsAndLocksInvalidateExistingTokens()
        {
            using var app = new ApiFactory();
            using var admin = await app.LoginAsync("admin01");
            using var cashier = await app.LoginAsync("cashier01");
            Assert.DoesNotContain("password", await admin.GetStringAsync("/api/users"), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/users/8/status", new { IsActive = false })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/users/3/status", new { IsActive = false })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await cashier.GetAsync("/api/customers/phone/0901122334")).StatusCode);
            using var response = await cashier.PostAsJsonAsync("/api/auth/login", new { Username = "cashier01", Password = "123456" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/users/3/status", new { IsActive = true })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cashier.GetAsync("/api/customers/phone/0901122334")).StatusCode);
        }

        [Fact]
        public async Task NewAccountAndPasswordResetCanLogin()
        {
            using var app = new ApiFactory();
            using var admin = await app.LoginAsync("admin01");
            using var created = await admin.PostAsJsonAsync("/api/users", new { Username = "newstaff", FullName = "New staff", Password = "initial123", Role = "Cashier" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetInt32();
            using var login = await admin.PostAsJsonAsync("/api/auth/login", new { Username = "newstaff", Password = "initial123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/users/{id}/password", new { Password = "changed123" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await admin.PostAsJsonAsync("/api/auth/login", new { Username = "newstaff", Password = "initial123" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/auth/login", new { Username = "newstaff", Password = "changed123" })).StatusCode);
        }

        private static object Sale() => new
        {
            CustomerPhone = "0901122334", CashReceived = 20000, ExpectedTotalAmount = 12000,
            Items = new[] { new { ProductId = 1, Quantity = 1 } }
        };

        [Fact]
        public async Task UpgradeRetainsLegacySqlPasswords()
        {
            using var app = new ApiFactory();
            await app.QueryAsync(async db =>
            {
                var account = await db.Users.SingleAsync(u => u.Username == "cashier01");
                account.Password = "legacy-password";
                return await db.SaveChangesAsync();
            });
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { Username = "cashier01", Password = "legacy-password" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { Username = "cashier01", Password = "wrong-password" })).StatusCode);
        }
    }
}
