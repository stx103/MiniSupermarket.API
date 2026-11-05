using System.Net;
using System.Data;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using MiniSupermarket.API.Controllers;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using Xunit;

namespace MiniSupermarket.Tests
{
    public class CriteriaTests
    {
        [Fact]
        public void SqlParametersUseAddWithValueWithExplicitTypesAndSizes()
        {
            using var command = new SqlCommand();
            AdoDatabase.Parameter(command, "@Barcode", "' OR 1=1 --", DbType.String, 50);
            AdoDatabase.Parameter(command, "@Price", 123.45m, DbType.Decimal);
            AdoDatabase.Parameter(command, "@Customer", null, DbType.Int32);
            Assert.Equal(SqlDbType.NVarChar, command.Parameters["@Barcode"].SqlDbType);
            Assert.Equal(50, command.Parameters["@Barcode"].Size);
            Assert.Equal("' OR 1=1 --", command.Parameters["@Barcode"].Value);
            Assert.Equal(SqlDbType.Decimal, command.Parameters["@Price"].SqlDbType);
            Assert.Equal(18, command.Parameters["@Price"].Precision);
            Assert.Equal(2, command.Parameters["@Price"].Scale);
            Assert.Equal(DBNull.Value, command.Parameters["@Customer"].Value);
            Assert.Equal(SqlDbType.Int, command.Parameters["@Customer"].SqlDbType);
        }

        [Theory]
        [InlineData("http://localhost:5167", true)]
        [InlineData("https://untrusted.example", false)]
        public async Task CorsPreflightUsesConfiguredOrigins(string origin, bool allowed)
        {
            using var app = new ApiFactory();
            using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/products");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
            if (allowed) Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("0.004")]
        public async Task ProductCreateAndUpdateRejectNonpositiveOrSubCentPrices(string value)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var request = new ProductRequest
            {
                Barcode = "INVALID-PRICE", ProductName = "Invalid price", StockQuantity = 10, CategoryId = 1,
                Price = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
            };
            using var created = await client.PostAsJsonAsync("/api/products", request);
            using var updated = await client.PutAsJsonAsync("/api/products/1", request);
            Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
            Assert.Equal(20, await app.QueryAsync(db => db.Products.CountAsync()));
            Assert.Equal(12000m, await app.QueryAsync(db => db.Products.Where(p => p.ProductId == 1).Select(p => p.Price).SingleAsync()));
        }

        [Fact]
        public async Task DatabaseConstraintAlsoRejectsZeroPrice()
        {
            using var app = new ApiFactory();
            await Assert.ThrowsAsync<DbUpdateException>(() => app.QueryAsync(async db =>
            {
                var product = await db.Products.FirstAsync();
                product.Price = 0;
                return await db.SaveChangesAsync();
            }));
        }

        [Theory]
        [InlineData("/api/customers")]
        [InlineData("/api/customers/1")]
        [InlineData("/api/customers/paged")]
        [InlineData("/api/customers/search?keyword=0901")]
        public async Task CashierCanBrowseCustomerManagement(string endpoint)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("cashier01");
            using var response = await client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CashierCanUpdateCustomerAndLookUpPosMember()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("cashier01");
            using var response = await client.PutAsJsonAsync("/api/customers/1", new CustomerRequest
            {
                CustomerName = "Changed", PhoneNumber = "0901122334", RewardPoints = 180, MembershipRank = "Changed",
                Address = "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh"
            });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var member = await client.GetFromJsonAsync<CustomerDto>("/api/customers/phone/0901122334");
            Assert.Equal(180, member!.RewardPoints);
            Assert.Equal("Changed", member.CustomerName);
            Assert.Equal("Changed", member.MembershipRank);
            Assert.Equal("12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh", member.Address);
        }

        [Theory]
        [InlineData("admin01")]
        [InlineData("cashier01")]
        public async Task CustomerCrudUsesDtosAndIgnoresClientAssignedIds(string username)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync(username);
            var request = new { CustomerId = 99999, CustomerName = "New customer", PhoneNumber = "0909999999", Address = "12, Nguyen Hue, Phường Sài Gòn, Thành phố Hồ Chí Minh", RewardPoints = 10, MembershipRank = "Test" };
            using var created = await client.PostAsJsonAsync("/api/customers", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var json = await created.Content.ReadFromJsonAsync<JsonElement>();
            var id = json.GetProperty("customerId").GetInt32();
            Assert.NotEqual(99999, id);
            Assert.False(json.TryGetProperty("orders", out _));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
            var update = new CustomerRequest
            {
                CustomerName = "Updated customer", PhoneNumber = "0909999998",
                Address = "56/2A, Đường số 1, Xã Bình Khánh, Thành phố Hồ Chí Minh",
                RewardPoints = 25, MembershipRank = "Updated rank"
            };
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/customers/{id}", update)).StatusCode);
            var updated = await client.GetFromJsonAsync<CustomerDto>($"/api/customers/{id}");
            Assert.Equal(update.CustomerName, updated!.CustomerName);
            Assert.Equal(update.PhoneNumber, updated.PhoneNumber);
            Assert.Equal(update.Address, updated.Address);
            Assert.Equal(update.RewardPoints, updated.RewardPoints);
            Assert.Equal(update.MembershipRank, updated.MembershipRank);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/customers/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/customers/{id}")).StatusCode);
        }

        [Theory]
        [InlineData("GET", "/api/customers")]
        [InlineData("GET", "/api/customers/1")]
        [InlineData("GET", "/api/customers/paged")]
        [InlineData("GET", "/api/customers/search?keyword=0901")]
        [InlineData("GET", "/api/customers/phone/0901122334")]
        [InlineData("POST", "/api/customers")]
        [InlineData("PUT", "/api/customers/1")]
        [InlineData("DELETE", "/api/customers/1")]
        public async Task CustomerManagementRejectsAnonymousAndWarehouse(string method, string endpoint)
        {
            using var app = new ApiFactory();
            using var anonymous = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var warehouse = await app.LoginAsync("ware01");
            foreach (var entry in new[] { (anonymous, HttpStatusCode.Unauthorized), (warehouse, HttpStatusCode.Forbidden) })
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
                if (method is "POST" or "PUT")
                    request.Content = JsonContent.Create(new CustomerRequest
                    {
                        CustomerName = "Unauthorized customer", PhoneNumber = "0909999999",
                        Address = "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh",
                        RewardPoints = 999
                    });
                using var response = await entry.Item1.SendAsync(request);
                Assert.Equal(entry.Item2, response.StatusCode);
            }
            Assert.Equal(20, await app.QueryAsync(db => db.Customers.CountAsync()));
            Assert.Equal(150, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        }

        [Fact]
        public async Task CashierCustomerManagementRetainsValidationAndInvoiceHistory()
        {
            using var app = new ApiFactory();
            using var cashier = await app.LoginAsync("cashier01");
            var duplicate = new CustomerRequest
            {
                CustomerName = "Duplicate customer", PhoneNumber = "0901122334",
                Address = "12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh"
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await cashier.PostAsJsonAsync("/api/customers", duplicate)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await cashier.PutAsJsonAsync("/api/customers/2", duplicate)).StatusCode);
            var invalid = new CustomerRequest { CustomerName = "Invalid customer", PhoneNumber = "0909999999", Address = "Incomplete address" };
            Assert.Equal(HttpStatusCode.BadRequest, (await cashier.PostAsJsonAsync("/api/customers", invalid)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await cashier.PutAsJsonAsync("/api/customers/1", invalid)).StatusCode);
            using var deleted = await cashier.DeleteAsync("/api/customers/1");
            Assert.Equal(HttpStatusCode.BadRequest, deleted.StatusCode);
            Assert.Contains("đã phát sinh hóa đơn", await deleted.Content.ReadAsStringAsync());
            Assert.Equal(20, await app.QueryAsync(db => db.Customers.CountAsync()));
            Assert.Equal(20, await app.QueryAsync(db => db.Orders.CountAsync()));
            Assert.Equal(150, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.RewardPoints).SingleAsync()));
        }

        [Fact]
        public async Task CategoryCrudUsesDtosAndPreservesStatusCodes()
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("ware01");
            var request = new { CategoryId = 99999, CategoryName = "New category", Description = "DTO test" };
            using var created = await client.PostAsJsonAsync("/api/categories", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var json = await created.Content.ReadFromJsonAsync<JsonElement>();
            var id = json.GetProperty("categoryId").GetInt32();
            Assert.NotEqual(99999, id);
            Assert.False(json.TryGetProperty("products", out _));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/categories/{id}", request)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categories/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/categories/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/categories", new { CategoryName = " " })).StatusCode);
        }

        [Fact]
        public async Task ReadOnlyControllersReturnDtosWithoutTrackingEntities()
        {
            using var app = new ApiFactory();
            await app.QueryAsync(async db =>
            {
                db.ChangeTracker.Clear();
                var category = Assert.IsType<OkObjectResult>(await new CategoriesController(db).GetById(1));
                Assert.IsType<CategoryDto>(category.Value);
                var customer = Assert.IsType<OkObjectResult>(await new CustomersController(db).GetById(1));
                Assert.IsType<CustomerDto>(customer.Value);
                var product = Assert.IsType<OkObjectResult>(await new ProductsController(db).GetById(1));
                Assert.IsType<ProductItemDto>(product.Value);
                Assert.Empty(db.ChangeTracker.Entries());
                return true;
            });
        }

        [Fact]
        public void EveryHttpActionIsAsyncAndDoesNotBindDatabaseEntities()
        {
            var entities = new[] { typeof(User), typeof(Category), typeof(Product), typeof(Customer), typeof(Order), typeof(OrderDetail) };
            var actions = typeof(AuthController).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ControllerBase)))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any()).ToList();
            Assert.NotEmpty(actions);
            foreach (var action in actions)
            {
                Assert.Equal(typeof(Task<IActionResult>), action.ReturnType);
                Assert.DoesNotContain(action.GetParameters(), parameter => entities.Contains(parameter.ParameterType));
            }
        }

        [Fact]
        public async Task SwaggerDescribesDtoContractsAndExcludesTemplateWeatherApi()
        {
            using var app = new ApiFactory("Development");
            using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
            var paths = swagger.GetProperty("paths");
            Assert.DoesNotContain(paths.EnumerateObject(), p => p.Name.Contains("weather", StringComparison.OrdinalIgnoreCase));
            foreach (var resource in new[] { "categories", "customers" })
            {
                var operation = paths.EnumerateObject().Single(p => p.Name.Equals($"/api/{resource}", StringComparison.OrdinalIgnoreCase)).Value.GetProperty("post");
                var schema = operation.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema");
                Assert.EndsWith("Request", schema.GetProperty("$ref").GetString());
            }
        }

        [Fact]
        public void SqlServerMigrationsHaveSeedsProceduresAndValidIdempotentSql()
        {
            var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json")).Build();
            var options = new DbContextOptionsBuilder<SupermarketDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options;
            using var db = new SupermarketDbContext(options);
            Assert.Equal(4, db.Database.GetMigrations().Count());
            Assert.False(db.Database.HasPendingModelChanges());
            var assembly = db.GetService<IMigrationsAssembly>();
            var initial = assembly.CreateMigration(assembly.Migrations.Single(m => m.Key.EndsWith("_InitialCreate")).Value, db.Database.ProviderName!);
            var inserts = initial.UpOperations.OfType<InsertDataOperation>().ToList();
            foreach (var table in new[] { "Users", "Categories", "Products", "Customers", "Orders", "OrderDetails" })
                Assert.Equal(20, inserts.Where(i => i.Table == table).Sum(i => i.Values.GetLength(0)));
            var sql = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            using var reader = new StringReader(sql);
            var fragment = new TSql160Parser(true).Parse(reader, out var errors);
            Assert.Empty(errors);
            Assert.Contains("CK_Products_Price_Positive", sql);
            Assert.Contains("__EFMigrationsHistory", sql);
            var procedures = new ProcedureSqlVisitor();
            fragment.Accept(procedures);
            Assert.Equal(6, procedures.Count);
        }

        private sealed class ProcedureSqlVisitor : TSqlFragmentVisitor
        {
            public int Count { get; private set; }
            public override void ExplicitVisit(StringLiteral node)
            {
                if (!node.Value.StartsWith("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase)) return;
                using var reader = new StringReader(node.Value);
                new TSql160Parser(true).Parse(reader, out var errors);
                Assert.Empty(errors);
                Count++;
            }
        }
    }
}
